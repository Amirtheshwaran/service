using System.Collections;
using System.Linq;
using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceLife:MonoBehaviour {
  ServiceDirector d;Transform dog,model;Animator dogAnim;Vector3 home;float nextIdle,barkUntil,turnSpeed;bool playing;bool[] seen=new bool[6];Quaternion[] rest=new Quaternion[6];float[] porch=new float[6];Coroutine[] doors=new Coroutine[6];
  public bool DogPresent=>dog&&model;public float DogTravel {get;private set;}
  public Vector3 DogHome=>home;
  // V20: friendly residents swing the door well open and stand clear of it (ServiceResidents).
  public const float FriendlyOpen=85;
  public void Initialize(ServiceDirector director){d=director;
   for(int i=0;i<6;i++){var p=d.Property(i);rest[i]=p.DoorPanel?p.DoorPanel.localRotation:Quaternion.identity;porch[i]=p.PorchLight?p.PorchLight.intensity:0;p.MeasureDoorway();}
   dog=transform.Find("Correll yard dog");
   if(dog){
    // The old static seated model (V16) duplicated the animated V17 shepherd on the same spot: retire it.
    foreach(Transform c in dog)if(c.name.Contains("seated"))c.gameObject.SetActive(false);
    dogAnim=dog.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.runtimeAnimatorController);model=dogAnim?dogAnim.transform:(dog.childCount>0?dog.GetChild(0):null);
    home=YardSpot(d.Property(0));dog.position=home;FaceWalk(true);
   }
  }
  // Off to the side of Correll's porch, on the grass, away from the walk from the drive to the door.
  Vector3 YardSpot(ServiceProperty p){
   var c=p.OpeningCentre;var outw=-p.Inward;outw.y=0;outw.Normalize();var side=Vector3.Cross(Vector3.up,outw);
   Vector3 best=dog.position;float score=-1;
   foreach(float s in new[]{1f,-1f})foreach(float o in new[]{3.4f,4.4f}){
    var at=c+outw*o+side*s*3.3f;float clear=9;
    if(p.ApproachRoute!=null)foreach(var q in p.ApproachRoute){var a=q;a.y=at.y;clear=Mathf.Min(clear,Vector3.Distance(a,at));}
    if(Physics.CheckSphere(at+Vector3.up*.5f,.45f,~0,QueryTriggerInteraction.Ignore))clear-=4;
    if(clear>score){score=clear;best=at;}}
   if(Physics.Raycast(best+Vector3.up*3,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore))best.y=hit.point.y;
   else{var t=Terrain.activeTerrain;if(t)best.y=t.SampleHeight(best)+t.transform.position.y;}
   return best;
  }
  void FaceWalk(bool snap){var p=d.Property(0);var target=p.ApproachRoute!=null&&p.ApproachRoute.Length>0?p.ApproachRoute[p.ApproachRoute.Length/2]:p.Door.position;var f=target-dog.position;f.y=0;if(f.sqrMagnitude>.01f&&snap)dog.rotation=Quaternion.LookRotation(f);}
  public void ResetForShift(){StopAllCoroutines();System.Array.Clear(seen,0,seen.Length);if(dog){dog.position=home;FaceWalk(true);}for(int i=0;i<6;i++){if(d.Property(i).DoorPanel)d.Property(i).DoorPanel.localRotation=rest[i];if(d.Property(i).PorchLight)d.Property(i).PorchLight.intensity=porch[i];}}
  void State(string name){if(dogAnim&&dogAnim.isActiveAndEnabled&&dogAnim.HasState(0,Animator.StringToHash(name)))dogAnim.CrossFadeInFixedTime(name,.35f);}
  public void Bark(){if(!dog)return;d.Audio.DogAt(dog.position+Vector3.up*.4f,.32f);barkUntil=Time.time+2.2f;if(!playing){playing=true;State("Playing");}}
  void Update(){if(!d||d.Phase!=ServicePhase.Playing||d.PaperOpen)return;
   if(dog&&model){
    // The dog watches whoever is walking up: it turns its body toward the player in short, unhurried turns and
    // shifts between its breathing and playful idles, rather than sitting in the path like a statue.
    var to=d.Scene.View.transform.position-dog.position;to.y=0;float distance=to.magnitude;
    if(distance<34&&to.sqrMagnitude>.01f){float off=Vector3.SignedAngle(dog.forward,to,Vector3.up);
     if(Mathf.Abs(off)>28)turnSpeed=Mathf.MoveTowards(turnSpeed,85,Time.deltaTime*160);else if(Mathf.Abs(off)<6)turnSpeed=Mathf.MoveTowards(turnSpeed,0,Time.deltaTime*200);
     if(turnSpeed>0){var before=dog.rotation;dog.rotation=Quaternion.RotateTowards(dog.rotation,Quaternion.LookRotation(to),turnSpeed*Time.deltaTime);DogTravel+=Quaternion.Angle(before,dog.rotation)*.01f;}}
    if(Time.time>nextIdle&&Time.time>barkUntil){playing=!playing&&distance<20&&Random.value<.6f;State(playing?"Playing":"Breathing");nextIdle=Time.time+(playing?3.2f:Random.Range(6f,11f));}
   }
   if(d.Player.InCar)return;
   foreach(var p in d.Scene.Properties){if(d.IsFriendly(p.Index)||seen[p.Index]||Vector3.Distance(d.Scene.Walker.transform.position,p.Door.position)>14)continue;seen[p.Index]=true;StartCoroutine(Flicker(p));}
  }
  IEnumerator Flicker(ServiceProperty p){if(!p.PorchLight)yield break;var light=p.PorchLight;float baseline=porch[p.Index];foreach(float value in new[]{.15f,1f,.1f,.2f,1f}){light.intensity=baseline*value;yield return new WaitForSeconds(.12f);}light.intensity=baseline;}
  public void OpenDoor(ServiceProperty p,bool open,bool wide=false){if(p.DoorPanel){if(doors[p.Index]!=null)StopCoroutine(doors[p.Index]);doors[p.Index]=StartCoroutine(Door(p,open,wide));}}
  IEnumerator Door(ServiceProperty p,bool open,bool wide){var start=p.DoorPanel.localRotation;var goal=rest[p.Index]*Quaternion.Euler(0,open?(d.IsFriendly(p.Index)&&!wide?Mathf.Sign(p.DoorSwing)*FriendlyOpen:p.DoorSwing):0,0);float time=0;while(time<.65f){time+=Time.deltaTime;p.DoorPanel.localRotation=Quaternion.Slerp(start,goal,Mathf.SmoothStep(0,1,time/.65f));yield return null;}p.DoorPanel.localRotation=goal;}
 }
}
