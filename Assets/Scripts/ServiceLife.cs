using System.Collections;
using System.Linq;
using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceLife:MonoBehaviour {
  ServiceDirector d;Transform dog,model;Animator dogAnim;Vector3 home;float nextIdle,barkUntil,turnSpeed;bool playing;bool[] seen=new bool[6];Quaternion[] rest=new Quaternion[6];float[] porch=new float[6];Coroutine[] doors=new Coroutine[6];
  public bool DogPresent=>dog&&model;public float DogTravel {get;private set;}
  // V22: the charge (see Sources/V22/patch_dog.py)
  public enum DogMode{Home,Charge,Escort,Porch,Hushed,Return}
  public DogMode Mode {get;private set;}=DogMode.Home;
  public bool DogBusy=>Mode!=DogMode.Home;
  public int Barks {get;private set;}
  public float ClosestApproach {get;private set;}=99;
  public Vector3 DogPosition=>dog?dog.position:Vector3.zero;
  bool charged;float nextBark,nextPath,modeSince,porchSince,weaveFlip;int weaveSide=1;Vector3 porchSpot,goalCache;bool porchPicked;string curState;
  readonly UnityEngine.AI.NavMeshPath path=new UnityEngine.AI.NavMeshPath();Vector3[] corners=new Vector3[0];int corner;
  const float RunRef=4f; // ground speed the run loop shows at speed 1: Audit/dog22.txt measures the paw's reach (0.53 m per 0.4 s cycle); a gallop covers about three times that
  public Vector3 DogHome=>home;
  // V20: friendly residents swing the door well open and stand clear of it (ServiceResidents).
  public const float FriendlyOpen=85;
  // V22: when each door's current swing finishes (a door is not offered to the player while it is still moving)
  readonly float[] settleAt=new float[6];
  public bool DoorMoving(int i)=>i>=0&&i<6&&Time.time<settleAt[i];
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
  public void ResetForShift(){StopAllCoroutines();Mode=DogMode.Home;charged=false;Barks=0;ClosestApproach=99;porchPicked=false;curState=null;if(dogAnim)dogAnim.speed=1;System.Array.Clear(settleAt,0,6);System.Array.Clear(seen,0,seen.Length);if(dog){dog.position=home;FaceWalk(true);}for(int i=0;i<6;i++){if(d.Property(i).DoorPanel)d.Property(i).DoorPanel.localRotation=rest[i];if(d.Property(i).PorchLight)d.Property(i).PorchLight.intensity=porch[i];}}
  void State(string name){if(name==curState)return;if(dogAnim&&dogAnim.isActiveAndEnabled&&dogAnim.HasState(0,Animator.StringToHash(name))){curState=name;dogAnim.CrossFadeInFixedTime(name,name=="Run"?.15f:.35f);}}
  public void Bark(){if(!dog||DogBusy)return;d.Audio.DogAt(dog.position+Vector3.up*.4f,.32f);barkUntil=Time.time+2.2f;if(!playing){playing=true;State("Playing");}}
  void Update(){if(!d||d.Phase!=ServicePhase.Playing||d.PaperOpen)return;
   bool rex=dog&&model&&Rex();
   if(dog&&model&&!rex){
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
  // V22: true while the charge owns the dog this frame.
  bool Rex(){
   var p=d.Property(0);var w=d.Scene.Walker.transform.position;var door=p.Door.position;var dw=Flat(w-door).magnitude;
   bool pending=d.Docket.Exists(e=>e.Property==0&&e.Result==ServiceResult.Pending);
   bool calm=!d.Player.InCar&&!d.Horror.Active&&!d.Horror.Caught&&!(d.Dialogue&&d.Dialogue.Active)&&d.NoteOpen<0;
   if(Mode==DogMode.Home){
    if(charged||d.NightIndex>1||!pending||!calm)return false;
    if((dw<32&&dw>5)||Flat(w-dog.position).magnitude<14){charged=true;Go(DogMode.Charge);Bark(true);d.SayDogLine();}else return false;}
   // walking or calling it off
   if(d.Horror.Active||d.Horror.Caught)Go(DogMode.Return);
   if(d.Dialogue&&d.Dialogue.Active&&d.Dialogue.Speaker.StartsWith("Walter"))Hush();
   if((Mode==DogMode.Charge||Mode==DogMode.Escort||Mode==DogMode.Porch)&&(d.Player.InCar||dw>40))Go(DogMode.Return);
   Vector3 goal=dog.position;float speed=0;var toW=Flat(w-dog.position);float dist=toW.magnitude;
   switch(Mode){
    case DogMode.Charge:goal=w+Flat3(dog.position-w).normalized*2.6f;speed=6.5f;if(dist<3.4f)Go(DogMode.Escort);break;
    case DogMode.Escort:{if(Time.time>weaveFlip){weaveSide=-weaveSide;weaveFlip=Time.time+Random.Range(2.2f,3.4f);}
     var fwd=Flat3(d.Scene.Walker.transform.forward);if(fwd.sqrMagnitude<.01f)fwd=Flat3(door-w);goal=w+Quaternion.AngleAxis(weaveSide*25,Vector3.up)*fwd.normalized*4f; // in front of you, in the torch (at 55 degrees he ran at the edge of the view)
     // your pace plus the gap: by the gap alone he trailed his spot by 1.5 m at a walk, under the bottom of the view
     speed=Mathf.Clamp(d.Player.HorizontalSpeed+Flat(goal-dog.position).magnitude*2.5f,0,7.5f);if(dw<4.5f)Go(DogMode.Porch);break;}
    case DogMode.Porch:{if(!porchPicked){porchPicked=true;porchSpot=PorchSpot(p,w);porchSince=Time.time;}goal=porchSpot;speed=3f;
     if(dw>8)Go(DogMode.Escort);break;}
    case DogMode.Hushed:goal=dog.position;speed=0;
     if(!pending&&(d.Player.InCar||dw>20)&&!AnyVisible()){dog.position=home;FaceWalk(true);Go(DogMode.Home);return false;}break;
    case DogMode.Return:goal=home;speed=4.5f;if(Flat(home-dog.position).magnitude<.4f){dog.position=home;FaceWalk(true);Go(DogMode.Home);return false;}break;
   }
   // move along the navmesh toward the goal, on the ground, never onto the porch or into the house, never into you
   float moved=0;
   if(speed>.05f){
    // repath on the navmesh four times a second, and at once when the goal has moved (the weave, a turn of the head)
    if(Time.time>nextPath||Vector3.Distance(goalCache,goal)>1.5f){nextPath=Time.time+.25f;goalCache=goal;
     if(UnityEngine.AI.NavMesh.SamplePosition(dog.position,out var s0,.6f,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.SamplePosition(goal,out var s1,1.5f,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.CalculatePath(s0.position,s1.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.corners.Length>1){corners=path.corners;corner=1;}
     else if(corners.Length<2){corners=new[]{dog.position,dog.position};corner=1;}} // no path: keep the last one, or stand
    var target=corners.Length>corner?corners[corner]:dog.position;if(Flat(target-dog.position).magnitude<.35f&&corner<corners.Length-1){corner++;target=corners[corner];}
    var step=Vector3.ClampMagnitude(Flat3(target-dog.position),speed*Time.deltaTime);var next=dog.position+step;
    bool g=Ground(ref next),ins=g&&(Indoors(p,next)||OnBuilt),hi=g&&next.y>=door.y-.4f,jump=g&&Mathf.Abs(next.y-dog.position.y)>=.35f,edge=false; // the step runs between navmesh path corners, so it is on the mesh already; a NavMesh.Raycast from a corner
    // (corners sit on the mesh edge) grazed the boundary and refused every step - Rex froze mid-charge (V22 tour)
    if(g&&!ins&&!hi&&!jump&&!edge){moved=step.magnitude;dog.position=next;}else BlockedBy[!g?0:ins?1:hi?2:jump?3:4]++;
   }
   // keep 1.6 m off the walker
   var away=Flat3(dog.position-w);if(away.magnitude<1.6f){var push=w+(away.sqrMagnitude>.01f?away.normalized:-Flat3(d.Scene.Walker.transform.forward).normalized)*1.6f;if(Ground(ref push)&&!OnBuilt&&!Indoors(p,push)&&push.y<door.y-.4f&&Mathf.Abs(push.y-dog.position.y)<.35f&&!UnityEngine.AI.NavMesh.Raycast(dog.position,push,out _,UnityEngine.AI.NavMesh.AllAreas))dog.position=push;}
   ClosestApproach=Mathf.Min(ClosestApproach,Flat(dog.position-w).magnitude);
   float v=moved/Mathf.Max(Time.deltaTime,1e-4f);DogTravel+=moved;
   // face where he runs, or you when he stands
   var faceDir=v>.4f&&moved>0?Flat3(goal-dog.position):Flat3(w-dog.position);if(faceDir.sqrMagnitude>.01f)dog.rotation=Quaternion.RotateTowards(dog.rotation,Quaternion.LookRotation(faceDir),(v>.4f?540:220)*Time.deltaTime);
   if(v>.6f){State("Run");if(dogAnim)dogAnim.speed=Mathf.Clamp(v/RunRef,.6f,1.5f);}
   else{if(dogAnim)dogAnim.speed=1;State(Mode==DogMode.Hushed||Mode==DogMode.Return?"Breathing":"Playing");}
   // barking: a volley on night one until Walter answers (or a while at the steps); twice on night two
   bool barking=Mode==DogMode.Charge||Mode==DogMode.Escort||Mode==DogMode.Porch&&Time.time-porchSince<30;
   if(barking&&Time.time>nextBark&&(d.NightIndex==0||Barks<2))Bark(true);
   d.Audio.DogFollow(dog.position+Vector3.up*.45f);
   return true;
  }
  void Go(DogMode m){if(Mode==m)return;Mode=m;modeSince=Time.time;nextPath=0;if(m!=DogMode.Porch)porchPicked=false;if(m==DogMode.Escort)weaveFlip=Time.time+Random.Range(1.2f,2.2f);if(m==DogMode.Home){if(dogAnim)dogAnim.speed=1;State("Breathing");nextIdle=Time.time+Random.Range(3f,6f);}}
  public void Hush(){if(Mode==DogMode.Home||Mode==DogMode.Hushed)return;Go(DogMode.Hushed);d.Audio.StopDog();}
  public bool DogSeen=>AnyVisible();
  public string PathInfo=>$"path {path.status} {corners.Length} corners, at {corner}, end {(corners.Length>0?corners[corners.Length-1]:Vector3.zero)}, goal {goalCache}, refused {string.Join("/",BlockedBy)}";
  public readonly int[] BlockedBy=new int[5]; // steps refused: no ground, indoors or on a porch/steps, too high, a jump, off the navmesh
  bool AnyVisible(){if(!model)return false;foreach(var r in model.GetComponentsInChildren<Renderer>())if(r.isVisible)return true;return false;}
  void Bark(bool loud){d.Audio.DogAt(dog.position+Vector3.up*.45f,loud?.45f:.32f,Random.Range(.93f,1.07f));Barks++;nextBark=Time.time+(d.NightIndex==0?Random.Range(1.0f,1.9f):Random.Range(2.5f,4f));barkUntil=Time.time+1.2f;}
  // by the bottom of the steps, off to the side of the walk to the door, facing you
  Vector3 PorchSpot(ServiceProperty p,Vector3 w){var outw=-p.Inward;outw.y=0;outw.Normalize();var side=Vector3.Cross(Vector3.up,outw);var c=p.OpeningCentre;Vector3 best=c+outw*3.4f;float score=-1;
   foreach(float s in new[]{1f,-1f})foreach(float o in new[]{3.2f,4.2f}){var at=c+outw*o+side*s*2.4f;var test=at;if(!Ground(ref test)||OnBuilt||test.y>p.Door.position.y-.4f||Indoors(p,test))continue;float sc=Flat(test-w).magnitude;if(sc>score){score=sc;best=test;}}
   return best;}
  bool Ground(ref Vector3 at){var hits=Physics.RaycastAll(at+Vector3.up*1.5f,Vector3.down,4f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore);float best=float.NegativeInfinity;
   Collider top=null;foreach(var h in hits){if(h.collider is CharacterController||h.collider.transform.IsChildOf(d.Scene.Walker.transform))continue;if(h.point.y>best){best=h.point.y;top=h.collider;}}
   if(float.IsNegativeInfinity(best)){var t=Terrain.activeTerrain;if(!t)return false;best=t.SampleHeight(at)+t.transform.position.y;}
   // a porch, its steps or a house floor: wood (or unmarked) built ground, not the yard, the drive or the terrain
   OnBuilt=top!=null&&!(top is TerrainCollider)&&(top.GetComponentInParent<ServiceSurface>() is var sf&&(sf==null||sf.Kind=="wood"));
   at.y=best;return true;}
  bool OnBuilt;
  // inside the house = within its bounds and not out in front of the door's wall (the bounds take in the porch and yard)
  public static bool Indoors(ServiceProperty p,Vector3 at)=>p.InteriorBounds.Contains(at+Vector3.up*.3f)&&Vector3.Dot(at-p.OpeningCentre,-p.Inward)<.8f;
  static Vector2 Flat(Vector3 v)=>new Vector2(v.x,v.z);
  static Vector3 Flat3(Vector3 v)=>new Vector3(v.x,0,v.z);
  IEnumerator Flicker(ServiceProperty p){if(!p.PorchLight)yield break;var light=p.PorchLight;float baseline=porch[p.Index];foreach(float value in new[]{.15f,1f,.1f,.2f,1f}){light.intensity=baseline*value;yield return new WaitForSeconds(.12f);}light.intensity=baseline;}
  public float DoorOpenDegrees(int index){var p=d?d.Property(index):null;return p&&p.DoorPanel?Quaternion.Angle(rest[index],p.DoorPanel.localRotation):0;}
  public void OpenDoor(ServiceProperty p,bool open,bool wide=false,float seconds=.65f){if(p.DoorPanel){settleAt[p.Index]=Time.time+Mathf.Max(.05f,seconds);if(doors[p.Index]!=null)StopCoroutine(doors[p.Index]);doors[p.Index]=StartCoroutine(Door(p,open,wide,seconds));}}
  // A bang (short seconds) is thrown open and decelerates; an ordinary swing eases in and out.
  IEnumerator Door(ServiceProperty p,bool open,bool wide,float seconds){var start=p.DoorPanel.localRotation;var goal=rest[p.Index]*Quaternion.Euler(0,open?(d.IsFriendly(p.Index)&&!wide?Mathf.Sign(p.DoorSwing)*FriendlyOpen:p.DoorSwing):0,0);seconds=Mathf.Max(.05f,seconds);float time=0;while(time<seconds){time+=Time.deltaTime;float t=Mathf.Clamp01(time/seconds);t=seconds<.3f?1-(1-t)*(1-t):Mathf.SmoothStep(0,1,t);p.DoorPanel.localRotation=Quaternion.Slerp(start,goal,t);yield return null;}p.DoorPanel.localRotation=goal;}
 }
}
