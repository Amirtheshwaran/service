using UnityEngine;
namespace ServiceGameV2 {
 public static class ServiceInteraction {
  // Both road shoulders and the depot apron belong to the same reporting area.
  public static bool InDepot(Vector3 car,Vector3 depot){var delta=car-depot;return delta.x>=-38&&delta.x<=15&&Mathf.Abs(delta.z)<=19&&Mathf.Abs(delta.y)<5;}
  public static bool InView(Vector3 position,Vector3 forward,Vector3 target,float range,float cone=.65f){var delta=target-position;return delta.sqrMagnitude<=range*range&&delta.sqrMagnitude>.0025f&&Vector3.Dot(forward.normalized,delta.normalized)>=cone;}
  public static bool Clear(Vector3 from,Vector3 to,Transform allowed,Transform observer=null){var delta=to-from;foreach(var hit in Physics.RaycastAll(from,delta.normalized,delta.magnitude-.04f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)){if(observer&&(hit.transform==observer||hit.transform.IsChildOf(observer)))continue;if(allowed&&(hit.transform==allowed||hit.transform.IsChildOf(allowed)))continue;if(hit.distance<delta.magnitude-.12f)return false;}return true;}
  static Transform Observer(Camera view){var controller=view.GetComponentInParent<CharacterController>();return controller?controller.transform:null;}
  public static bool Reachable(Camera view,Vector3 point,float range,Transform allowed)=>InView(view.transform.position,view.transform.forward,point,range,.72f)&&Clear(view.transform.position,point,allowed,Observer(view));
  public static bool Watching(Camera view,Vector3 torso,Transform monster)=>InView(view.transform.position,view.transform.forward,torso,45,.66f)&&Clear(view.transform.position,torso,monster,Observer(view));
 }
}
