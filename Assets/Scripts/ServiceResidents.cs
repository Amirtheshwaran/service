using UnityEngine;
namespace ServiceGameV2 {
 // Residents who answer the door. V20: a resident is only in the house while answering. They stand on the latch side
 // just inside the opened door (ServiceProperty.AnswerPoint), facing out, on the floor at threshold height, so the leaf
 // never passes through them and they never stand on anything else (ServiceGrounding keeps the soles there).
 public sealed class ServiceResidents:MonoBehaviour {
  public GameObject Correll,Bell;
  ServiceDirector d;readonly bool[] answering=new bool[6];
  void Start(){
   d=GetComponentInParent<ServiceDirector>();if(!d)d=FindAnyObjectByType<ServiceDirector>();
   foreach(var a in GetComponentsInChildren<Animator>(true))if(a.runtimeAnimatorController)a.Play(0,0,Random.value);
   foreach(var r in new[]{Correll,Bell}){if(!r)continue;var an=r.GetComponentInChildren<Animator>(true);if(an&&!an.GetComponent<ServiceGrounding>())an.gameObject.AddComponent<ServiceGrounding>();}
  }
  GameObject For(int index)=>index==0?Correll:index==4?Bell:null;
  public bool Answering(int index)=>index>=0&&index<6&&answering[index];
  public void Answer(ServiceProperty p,bool show){
   if(!p)return;answering[p.Index]=show;var r=For(p.Index);if(!r||!show)return;
   var at=Floor(p.AnswerPoint(),p);var look=-p.Inward;look.y=0;
   r.transform.SetPositionAndRotation(at,Quaternion.LookRotation(look.sqrMagnitude>.01f?look:-p.Door.forward));
   foreach(var g in r.GetComponentsInChildren<ServiceGrounding>(true))g.Ignore=p.DoorPanel;
  }
  static Vector3 Floor(Vector3 at,ServiceProperty p){
   float best=float.NegativeInfinity;
   foreach(var h in Physics.RaycastAll(at+Vector3.up*1.1f,Vector3.down,2.6f,~0,QueryTriggerInteraction.Ignore)){
    if(p.DoorPanel&&h.transform.IsChildOf(p.DoorPanel))continue;if(h.collider.GetComponentInParent<CharacterController>())continue;
    if(Mathf.Abs(h.point.y-p.Door.position.y)>.45f)continue;if(h.point.y>best)best=h.point.y;}
   if(!float.IsNegativeInfinity(best))at.y=best;return at;
  }
  void Update(){if(!d)return;if(Correll)Correll.SetActive(d.IsFriendly(0)&&answering[0]);if(Bell)Bell.SetActive(d.IsFriendly(4)&&!d.BellGone&&answering[4]);}
 }
}
