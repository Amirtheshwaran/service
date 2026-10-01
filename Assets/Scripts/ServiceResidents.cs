using UnityEngine;
namespace ServiceGameV2 {
 // Residents who answer the door. Each one is kept standing on the floor it occupies (see ServiceGrounding).
 public sealed class ServiceResidents:MonoBehaviour {
  public GameObject Correll,Bell;
  ServiceDirector d;
  void Start(){
   d=GetComponentInParent<ServiceDirector>();
   foreach(var a in GetComponentsInChildren<Animator>(true))if(a.runtimeAnimatorController)a.Play(0,0,Random.value);
   foreach(var r in new[]{Correll,Bell}){if(!r)continue;var an=r.GetComponentInChildren<Animator>(true);if(an&&!an.GetComponent<ServiceGrounding>())an.gameObject.AddComponent<ServiceGrounding>();}
  }
  void Update(){if(!d)return;if(Correll)Correll.SetActive(d.IsFriendly(0));if(Bell)Bell.SetActive(d.IsFriendly(4)&&!d.BellGone);}
 }
}
