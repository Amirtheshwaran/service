using System.Collections;
using UnityEngine;
namespace ServiceGameV2 {
 // First-person hands: the right hand always carries the torch on foot; the left knocks, hands papers over, sets them
 // down and pushes doors. The whole viewmodel lags the camera slightly and bobs with the walk, as in Fears to Fathom.
 // V21: one clip per interaction (Sources/V19/arms_pose.py): Knock (three raps, wrist-led, with anticipation), Give
 // (the envelope held out to the resident and let go), Place (set down on a table), Push (palm on the door), Torch
 // (thumb on the switch). A missing clip falls back to Reach so an older rig still gestures.
 public sealed class ServiceHands:MonoBehaviour {
  public Animation Rig;public Transform Sway,Torch;
  public Transform Envelope;     // V21: papers in the left hand while setting them down (Place)
  public Transform EnvelopeGive; // V21: papers held out to a resident (Give)
  ServiceDirector d;Coroutine action;Renderer[] parts,envelopeParts,giveParts;bool shown=true;
  Vector3 restPos;Quaternion restRot,lastCam;Vector2 lag;float bob,bobWeight;
  public bool Gesturing {get;private set;}
  bool released; // V21: the papers have left the hand for the table (ServiceDirector.Place)
  public void ReleasePaper(){released=true;foreach(var r in envelopeParts)if(r)r.enabled=false;}
  public string Current {get;private set;}="Hold";
  public int Gestures {get;private set;}
  public bool Visible=>shown;
  void Start(){d=FindAnyObjectByType<ServiceDirector>();parts=GetComponentsInChildren<Renderer>(true);if(Torch)parts=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(parts,Torch.GetComponentsInChildren<Renderer>(true)));
   envelopeParts=Envelope?Envelope.GetComponentsInChildren<Renderer>(true):new Renderer[0];foreach(var r in envelopeParts)r.enabled=false;
   giveParts=EnvelopeGive?EnvelopeGive.GetComponentsInChildren<Renderer>(true):new Renderer[0];foreach(var r in giveParts)r.enabled=false;
   if(Sway){restPos=Sway.localPosition;restRot=Sway.localRotation;}Play("Hold",true);if(d&&d.Scene&&d.Scene.View)lastCam=d.Scene.View.transform.rotation;}
  void Play(string name,bool loop){if(!Rig||Rig.GetClip(name)==null)return;var s=Rig[name];s.wrapMode=loop?WrapMode.Loop:WrapMode.Once;Rig.CrossFade(name,.15f);}
  void LateUpdate(){
   if(!d||!d.Scene)return;
   bool visible=d.Phase==ServicePhase.Playing&&!d.Player.InCar&&!d.PaperOpen&&!d.Horror.Caught;
   if(visible!=shown){shown=visible;foreach(var r in parts)if(r)r.enabled=visible;}
   // the envelope is in the hand from the moment it is taken out until it is let go
   float nt=(Current=="Give"||Current=="Place")&&Rig&&Rig[Current]!=null&&Rig.IsPlaying(Current)?Rig[Current].normalizedTime:-1;
   bool give=shown&&Current=="Give"&&nt>.24f&&nt<.62f,place=shown&&Current=="Place"&&nt>.1f&&!released;
   foreach(var r in envelopeParts)if(r)r.enabled=place;foreach(var r in giveParts)if(r)r.enabled=give;
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
  // Older call sites: knock, or set the papers down.
  public void Interact(bool knock){Play(knock?"Knock":"Place");}
  public void Play(string gesture){
   if(!Rig)return;var clip=Rig.GetClip(gesture)!=null?gesture:gesture=="Torch"?null:"Reach";if(clip==null||Rig.GetClip(clip)==null)return;
   if(action!=null)StopCoroutine(action);action=StartCoroutine(Gesture(clip));Gestures++;}
  // Seconds from the start of a gesture to its moment of contact (the first rap, the hand-over, the push).
  // frames from Sources/V19/arms_pose.py at 30 fps: first rap 14 (the knock recording's first beat is 0.08 s in), arm
  // fully out 18, palm on the door 12
  public float KnockLead=>Lead("Knock",.35f);
  public float GiveLead=>Lead("Give",.57f);
  public float PushLead=>Lead("Push",.37f);
  public float PlaceLead=>Lead("Place",.72f);
  float Lead(string clip,float fallback){if(!Rig||Rig.GetClip(clip)==null)return fallback;var ev=Rig.GetClip(clip).events;foreach(var x in ev)if(x.functionName=="Contact")return x.time;return fallback;}
  void Contact(){} // animation event marker (the time of contact); nothing to do at runtime
  IEnumerator Gesture(string name){Gesturing=true;Current=name;released=false;Play(name,false);yield return new WaitForSeconds(Rig[name].length*.92f);Play("Hold",true);Current="Hold";Gesturing=false;}
 }
}
