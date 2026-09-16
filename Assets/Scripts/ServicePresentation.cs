using UnityEngine;
using UnityEngine.InputSystem;
namespace ServiceGameV2 {
 public sealed class ServicePresentation:MonoBehaviour {
  ServiceDirector d;Transform viewpoint;float introElapsed;bool skipped,wasTitle;
  public bool IntroVisible=>!skipped&&d.Phase==ServicePhase.Title&&introElapsed<5.6f;
  public float IntroTextAlpha {get{float t=introElapsed;return Mathf.SmoothStep(0,1,t/1.1f)*(1-Mathf.SmoothStep(0,1,(t-3.4f)/1.4f));}}
  public float IntroBackgroundAlpha=>1-Mathf.SmoothStep(0,1,(introElapsed-4)/1.6f);
  public void Initialize(ServiceDirector director){d=director;viewpoint=transform.Find("Manor title viewpoint");introElapsed=0;}
  void LateUpdate(){
   if(!d)return;
   if(d.Phase==ServicePhase.Title&&viewpoint){
    introElapsed+=Mathf.Min(Time.unscaledDeltaTime,.1f);
    if(!d.IsSmoke&&introElapsed>.7f&&((Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)))skipped=true;
    float t=Time.unscaledTime;d.Scene.View.transform.SetPositionAndRotation(viewpoint.position+viewpoint.right*Mathf.Sin(t*.075f)*.42f+Vector3.up*Mathf.Sin(t*.11f)*.08f,viewpoint.rotation*Quaternion.Euler(0,Mathf.Sin(t*.055f)*.65f,0));d.Scene.View.fieldOfView=57;wasTitle=true;
   }else if(wasTitle){wasTitle=false;skipped=true;d.Scene.View.fieldOfView=68;}
  }
 }
}
