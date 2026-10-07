using System.Collections;
using UnityEngine;
namespace ServiceGameV2 {
 // Residents who answer the door. A resident is only in the house while answering, facing out, on the floor at
 // threshold height (ServiceGrounding keeps the soles there).
 // V21: the door leaf sweeps the space just inside the door (Audit/doorstep21.txt: it passed through a resident at the
 // latch side from 10° to 50° both ways). So the resident waits back in the hall, outside the leaf's arc, while the
 // door swings open; steps up into the doorway once the leaf is fully open; and steps back into the hall before it
 // closes again.
 public sealed class ServiceResidents:MonoBehaviour {
  public GameObject Correll,Bell;
  public const float LeafClear=.36f; // body radius plus margin, kept between a resident and the leaf's arc
  ServiceDirector d;readonly bool[] answering=new bool[6],inDoorway=new bool[6];readonly Coroutine[] moves=new Coroutine[6];
  public float LastClearance {get;private set;}=99; // smallest leaf-to-body gap seen while a resident was answering
  void Start(){
   d=GetComponentInParent<ServiceDirector>();if(!d)d=FindAnyObjectByType<ServiceDirector>();
   foreach(var a in GetComponentsInChildren<Animator>(true))if(a.runtimeAnimatorController)a.Play(0,0,Random.value);
   foreach(var r in new[]{Correll,Bell}){if(!r)continue;var an=r.GetComponentInChildren<Animator>(true);if(an){an.applyRootMotion=false;if(!an.GetComponent<ServiceGrounding>())an.gameObject.AddComponent<ServiceGrounding>();}}
  }
  GameObject For(int index)=>index==0?Correll:index==4?Bell:null;
  public bool Answering(int index)=>index>=0&&index<6&&answering[index];
  public bool InDoorway(int index)=>index>=0&&index<6&&inDoorway[index];
  // Standing back in the hall on the latch side, far enough from the hinge that the whole leaf passes in front.
  public static Vector3 HallPoint(ServiceProperty p){
   float w=p.DoorWidth,latch=.62f,reach=w+LeafClear+.06f,along=latch*w;float depth=Mathf.Sqrt(Mathf.Max(.25f,reach*reach-along*along));
   return p.AnswerPoint(depth,latch);
  }
  public void Answer(ServiceProperty p,bool show){
   if(!p)return;answering[p.Index]=show;inDoorway[p.Index]=false;if(moves[p.Index]!=null){StopCoroutine(moves[p.Index]);moves[p.Index]=null;}
   var r=For(p.Index);if(!r)return;
   if(!show){Animate(r,"Idle",1);return;}
   var look=-p.Inward;look.y=0;
   r.transform.SetPositionAndRotation(Floor(HallPoint(p),p),Quaternion.LookRotation(look.sqrMagnitude>.01f?look:-p.Door.forward));
   foreach(var g in r.GetComponentsInChildren<ServiceGrounding>(true))g.Ignore=p.DoorPanel;
   moves[p.Index]=StartCoroutine(StepIn(p,r));
  }
  IEnumerator StepIn(ServiceProperty p,GameObject r){
   // wait for the leaf to finish opening (it reaches the latch-side space last), then two unhurried steps forward
   float wait=0;while(wait<2.5f&&(!d||!d.Life||d.Life.DoorOpenDegrees(p.Index)<ServiceLife.FriendlyOpen-4)){wait+=Time.deltaTime;yield return null;}
   yield return new WaitForSeconds(.15f);
   yield return Walk(r,Floor(HallPoint(p),p),Floor(p.AnswerPoint(),p),.95f,"Walk");
   inDoorway[p.Index]=true;moves[p.Index]=null;
  }
  // Called before the door closes: the resident steps back into the hall, clear of the leaf's arc.
  public IEnumerator StepBack(ServiceProperty p){
   if(!p)yield break;var r=For(p.Index);if(!r||!answering[p.Index])yield break;
   if(moves[p.Index]!=null){StopCoroutine(moves[p.Index]);moves[p.Index]=null;}
   inDoorway[p.Index]=false;yield return Walk(r,r.transform.position,Floor(HallPoint(p),p),.8f,"WalkBack");
  }
  // V23: feet that match the floor (Bell "walked weird"): the body moved up to twice as fast as the walk cycle's stride
  // (an eased 0.8-0.95 s slide with the clip at 0.72 speed). Now the step takes as long as the stride needs, at an even
  // pace with short ease-in/out, the cycle is played at the speed that matches it, and root motion is off.
  public static float LastWalkPace,LastWalkPlayback;
  IEnumerator Walk(GameObject r,Vector3 from,Vector3 to,float seconds,string state){
   float dist=Vector3.Distance(new Vector3(from.x,0,from.z),new Vector3(to.x,0,to.z));float natural=NaturalSpeed(r,state);
   float duration=Mathf.Max(seconds,dist/Mathf.Max(.2f,natural*.95f));float pace=dist/Mathf.Max(.01f,duration);float playback=Mathf.Clamp(pace/Mathf.Max(.2f,natural),.55f,1.1f);
   LastWalkPace=pace;LastWalkPlayback=playback;Animate(r,state,playback);float t=0;
   while(t<duration){t+=Time.deltaTime;float u=Mathf.Clamp01(t/duration);float k=Ease(u);r.transform.position=Vector3.Lerp(from,to,k);yield return null;}
   r.transform.position=to;Animate(r,"Idle",1);
  }
  // even pace with a short ease at each end (the first and last 15% of the step)
  static float Ease(float u){const float e=.15f;float v=1f/(1f-e);if(u<e)return v*u*u/(2*e);if(u>1-e){float w=1-u;return 1-v*w*w/(2*e);}return v*(u-e*.5f);}
  public static float NaturalSpeed(GameObject r,string state){float best=0;
   foreach(var a in r.GetComponentsInChildren<Animator>(true)){if(!a||!a.runtimeAnimatorController)continue;a.applyRootMotion=false;
    foreach(var c in a.runtimeAnimatorController.animationClips){if(!c)continue;bool back=state.Contains("Back");bool isBack=c.name.ToLowerInvariant().Contains("back");if(back!=isBack||!c.name.ToLowerInvariant().Contains("walk"))continue;
     float v=new Vector2(c.averageSpeed.x,c.averageSpeed.z).magnitude*Mathf.Max(.01f,a.transform.lossyScale.y);if(v>best)best=v;}}
   return best>.2f&&best<3f?best:(state.Contains("Back")?.8f:1.2f);}
  public static void Animate(GameObject r,string state,float speed){
   foreach(var a in r.GetComponentsInChildren<Animator>(true)){if(!a||!a.isActiveAndEnabled||!a.runtimeAnimatorController)continue;int h=Animator.StringToHash(state);if(!a.HasState(0,h))continue;a.speed=speed;a.CrossFadeInFixedTime(h,.25f,0);}
  }
  static Vector3 Floor(Vector3 at,ServiceProperty p){
   float best=float.NegativeInfinity;
   foreach(var h in Physics.RaycastAll(at+Vector3.up*1.1f,Vector3.down,2.6f,~0,QueryTriggerInteraction.Ignore)){
    if(p.DoorPanel&&h.transform.IsChildOf(p.DoorPanel))continue;if(h.collider.GetComponentInParent<CharacterController>())continue;
    if(Mathf.Abs(h.point.y-p.Door.position.y)>.45f)continue;if(h.point.y>best)best=h.point.y;}
   if(!float.IsNegativeInfinity(best))at.y=best;return at;
  }
  // Distance from a resident's body axis to the door leaf (for the regression test and the tour log).
  public static float LeafGap(ServiceProperty p,Vector3 body){
   if(!p||!p.DoorPanel)return 99;var rs=p.DoorPanel.GetComponentsInChildren<Renderer>();float best=99;
   foreach(var rd in rs){var mf=rd.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;var lb=mf.sharedMesh.bounds;var m=rd.transform.worldToLocalMatrix;var w=rd.transform.localToWorldMatrix;
    for(float h=.2f;h<1.7f;h+=.25f){var q=body+Vector3.up*h;var cl=w.MultiplyPoint3x4(lb.ClosestPoint(m.MultiplyPoint3x4(q)));var dd=cl-q;dd.y=0;best=Mathf.Min(best,dd.magnitude);}}
   return best;
  }
  void Update(){
   if(!d)return;if(Correll)Correll.SetActive(d.IsFriendly(0)&&(answering[0]||held[0]));if(Bell)Bell.SetActive(d.IsFriendly(4)&&!d.BellGone&&answering[4]);
   foreach(int i in new[]{0,4}){var r=For(i);if(r&&r.activeInHierarchy&&answering[i])LastClearance=Mathf.Min(LastClearance,LeafGap(d.Property(i),r.transform.position)-.26f);}
  }
  public void ResetClearance(){LastClearance=99;}
  // V25: hand a resident over (Walter coming out after you): stop whatever step he was taking
  public void Release(int index){if(index<0||index>5)return;if(moves[index]!=null){StopCoroutine(moves[index]);moves[index]=null;}inDoorway[index]=false;var r=For(index);if(r)foreach(var g in r.GetComponentsInChildren<ServiceGrounding>(true))g.Ignore=null;}
  // V25: keep a resident out of the house regardless of the door (Walter after you; a staged look in the tests)
  readonly bool[] held=new bool[6];public void Hold(int index,bool on){if(index>=0&&index<6)held[index]=on;}
  public void ResetAll(){System.Array.Clear(held,0,6);for(int i=0;i<6;i++){if(moves[i]!=null){StopCoroutine(moves[i]);moves[i]=null;}answering[i]=inDoorway[i]=false;}if(Correll)Correll.SetActive(false);if(Bell)Bell.SetActive(false);}
 }
}
