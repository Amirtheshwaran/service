using UnityEngine;
namespace ServiceGameV2 {
 // V20: both hands on the steering wheel while driving. The arms rig carries one pose per wheel angle (Drive_0 = full
 // left lock .. Drive_8 = full right lock, authored in Blender by Sources/V19/arms_pose.py with the fists closed round
 // the rim); the two poses either side of the current wheel angle are blended so the hands turn with the wheel.
 // The rig hangs from the driver's eye position (the seat), not the camera, so looking around does not move the hands.
 public sealed class ServiceDrivingHands:MonoBehaviour {
  public Animation Rig;public string[] Clips;
  ServiceDirector d;Renderer[] parts;bool shown=true;
  void Start(){d=FindAnyObjectByType<ServiceDirector>();parts=GetComponentsInChildren<Renderer>(true);
   if(Rig){Rig.playAutomatically=false;foreach(var c in Clips){var st=Rig[c];if(st==null)continue;st.wrapMode=WrapMode.ClampForever;st.layer=0;st.speed=0;st.time=0;st.weight=0;st.enabled=true;}}}
  // Hands show whenever the driver's seat is taken and the world is being played (not on the title diorama).
  public bool Visible=>shown;
  void LateUpdate(){
   if(!d||!Rig)return;
   bool vis=d.Phase==ServicePhase.Playing&&d.Player.InCar;
   if(vis!=shown){shown=vis;foreach(var r in parts)if(r)r.enabled=vis;}
   if(!vis)return;
   float s=Mathf.Clamp(d.Player.SteeringInput,-1,1),f=(s+1)*.5f*(Clips.Length-1);int a=Mathf.Clamp(Mathf.FloorToInt(f),0,Clips.Length-2);float t=f-a;
   for(int i=0;i<Clips.Length;i++){var st=Rig[Clips[i]];if(st==null)continue;st.enabled=true;st.weight=i==a?1-t:i==a+1?t:0;}
   Rig.Sample();
  }
 }
}
