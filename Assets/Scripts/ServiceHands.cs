using System.Collections;
using UnityEngine;
namespace ServiceGameV2 {
 // First-person hands: the right hand always carries the torch on foot; the left knocks and sets papers down.
 // The whole viewmodel lags the camera slightly and bobs with the walk, as in Fears to Fathom.
 public sealed class ServiceHands:MonoBehaviour {
  public Animation Rig;public Transform Sway,Torch;
  ServiceDirector d;Coroutine action;Renderer[] parts;bool shown=true;
  Vector3 restPos;Quaternion restRot,lastCam;Vector2 lag;float bob,bobWeight;
  public bool Gesturing {get;private set;}
  public bool Visible=>shown;
  void Start(){d=FindAnyObjectByType<ServiceDirector>();parts=GetComponentsInChildren<Renderer>(true);if(Torch)parts=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(parts,Torch.GetComponentsInChildren<Renderer>(true)));
   if(Sway){restPos=Sway.localPosition;restRot=Sway.localRotation;}Play("Hold",true);if(d)lastCam=d.Scene.View.transform.rotation;}
  void Play(string name,bool loop){if(!Rig||Rig.GetClip(name)==null)return;var s=Rig[name];s.wrapMode=loop?WrapMode.Loop:WrapMode.Once;Rig.CrossFade(name,.15f);}
  void LateUpdate(){
   if(!d)return;
   bool visible=d.Phase==ServicePhase.Playing&&!d.Player.InCar&&!d.PaperOpen&&!d.Horror.Caught;
   if(visible!=shown){shown=visible;foreach(var r in parts)if(r)r.enabled=visible;}
   if(!Sway)return;
   var cam=d.Scene.View.transform;var delta=Quaternion.Inverse(cam.rotation)*lastCam;lastCam=cam.rotation;
   var e=delta.eulerAngles;float yaw=Mathf.DeltaAngle(0,e.y),pitch=Mathf.DeltaAngle(0,e.x);
   float dt=Mathf.Max(Time.deltaTime,1e-4f);
   lag=Vector2.Lerp(lag,new Vector2(Mathf.Clamp(yaw,-6,6),Mathf.Clamp(pitch,-6,6))*.9f,1-Mathf.Exp(-dt*9));
   float speed=d.Player.InCar?0:d.Player.HorizontalSpeed;bobWeight=Mathf.MoveTowards(bobWeight,Mathf.Clamp01(speed/3.2f),dt*3);bob+=dt*Mathf.Lerp(1.2f,2.1f,Mathf.Clamp01(speed/5f))*Mathf.PI*2*.5f;
   var bobOffset=new Vector3(Mathf.Sin(bob)*.0035f,-Mathf.Abs(Mathf.Cos(bob))*.0045f,0)*bobWeight;
   var breathe=new Vector3(0,Mathf.Sin(Time.time*1.3f)*.0008f,0);
   Sway.localPosition=restPos+bobOffset+breathe;
   Sway.localRotation=restRot*Quaternion.Euler(lag.y*.6f,lag.x*.6f,-lag.x*.35f);
  }
  // Knock: the left fist raps three times; Reach: the left hand sets the papers down.
  public void Interact(bool knock){if(action!=null)StopCoroutine(action);action=StartCoroutine(Gesture(knock?"Knock":"Reach"));}
  public float KnockLead=>.5f;
  IEnumerator Gesture(string name){if(!Rig||Rig.GetClip(name)==null)yield break;Gesturing=true;Play(name,false);yield return new WaitForSeconds(Rig[name].length*.92f);Play("Hold",true);Gesturing=false;}
 }
}
