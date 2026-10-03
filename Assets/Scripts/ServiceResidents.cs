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
  IEnumerator Walk(GameObject r,Vector3 from,Vector3 to,float seconds,string state){
   Animate(r,state,.72f);float t=0;
   while(t<seconds){t+=Time.deltaTime;float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/seconds));r.transform.position=Vector3.Lerp(from,to,k);yield return null;}
   r.transform.position=to;Animate(r,"Idle",1);
  }
  static void Animate(GameObject r,string state,float speed){
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
   if(!d)return;if(Correll)Correll.SetActive(d.IsFriendly(0)&&answering[0]);if(Bell)Bell.SetActive(d.IsFriendly(4)&&!d.BellGone&&answering[4]);
   foreach(int i in new[]{0,4}){var r=For(i);if(r&&r.activeInHierarchy&&answering[i])LastClearance=Mathf.Min(LastClearance,LeafGap(d.Property(i),r.transform.position)-.26f);}
  }
  public void ResetClearance(){LastClearance=99;}
  public void ResetAll(){for(int i=0;i<6;i++){if(moves[i]!=null){StopCoroutine(moves[i]);moves[i]=null;}answering[i]=inDoorway[i]=false;}if(Correll)Correll.SetActive(false);if(Bell)Bell.SetActive(false);}
 }
}
