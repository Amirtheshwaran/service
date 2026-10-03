using UnityEngine;
namespace ServiceGameV2 {
 // V20: both hands on the steering wheel while driving. The arms rig carries one pose per wheel angle (Drive_0 = full
 // left lock .. Drive_8 = full right lock, authored in Blender by Sources/V19/arms_pose.py with the fists closed round
 // the rim); the two poses either side of the current wheel angle are blended so the hands turn with the wheel.
 // The rig hangs from the driver's eye position (the seat), not the camera, so looking around does not move the hands.
 public sealed class ServiceDrivingHands:MonoBehaviour {
  public Animation Rig;public string[] Clips;
  ServiceDirector d;Renderer[] parts;bool shown=true;
  // V21: the dashboard spill is a small point light beside the right arm. Looking out of a side window brings the upper
  // arm into view right next to it, where it lit the sleeve into a flat, glaring sheet, so the spill dims as the head
  // turns away from the dash (the dash is out of view by then).
  Light spill;float spillBase;
  void Start(){d=FindAnyObjectByType<ServiceDirector>();parts=GetComponentsInChildren<Renderer>(true);
   if(d&&d.Scene&&d.Scene.Car)foreach(var l in d.Scene.Car.GetComponentsInChildren<Light>(true))if(l.name=="Dashboard ambient spill"){spill=l;spillBase=l.intensity;}
   if(Rig){Rig.playAutomatically=false;foreach(var c in Clips){var st=Rig[c];if(st==null)continue;st.wrapMode=WrapMode.ClampForever;st.layer=0;st.speed=0;st.time=0;st.weight=0;st.enabled=true;}}}
  // Hands show whenever the driver's seat is taken and the world is being played (not on the title diorama).
  public bool Visible=>shown;
  void LateUpdate(){
   if(!d||!Rig)return;
   bool vis=(d.Phase==ServicePhase.Playing||d.Phase==ServicePhase.Paused)&&d.Player.InCar;
   if(vis!=shown){shown=vis;foreach(var r in parts)if(r)r.enabled=vis;}
   if(!vis)return;
   if(spill&&d.Scene.DriverSeat){var look=Quaternion.Inverse(d.Scene.DriverSeat.rotation)*d.Scene.View.transform.rotation;float yaw=Mathf.Abs(Mathf.DeltaAngle(0,look.eulerAngles.y));
    spill.intensity=spillBase*Mathf.Lerp(1,.15f,Mathf.SmoothStep(0,1,(yaw-18)/27));}
   float s=Mathf.Clamp(d.Player.SteeringInput,-1,1),f=(s+1)*.5f*(Clips.Length-1);int a=Mathf.Clamp(Mathf.FloorToInt(f),0,Clips.Length-2);float t=f-a;
   for(int i=0;i<Clips.Length;i++){var st=Rig[Clips[i]];if(st==null)continue;st.enabled=true;st.weight=i==a?1-t:i==a+1?t:0;}
   Rig.Sample();
  }
 }
}
