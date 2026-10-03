using UnityEngine;
namespace ServiceGameV2 {
 // V21: fuzzy dice on the rear-view mirror (jediscoob, CC-BY). Each die hangs on its cord from a pivot and swings like
 // a pendulum: it lags behind when the car pulls away, swings forward under braking, out to the side in a turn, and
 // jiggles over the road surface. Angles are about the car's right axis (pitch) and forward axis (roll).
 public sealed class ServiceDice:MonoBehaviour {
  public Transform[] Pivots;public float[] Lengths;
  ServiceDirector d;Quaternion[] rest;Vector2[] ang,vel;Vector3 lastPos,lastVel;bool primed;float bump;
  public float Swing {get;private set;} // largest angle this frame (degrees), for the test
  void Start(){d=FindAnyObjectByType<ServiceDirector>();int n=Pivots!=null?Pivots.Length:0;rest=new Quaternion[n];ang=new Vector2[n];vel=new Vector2[n];for(int i=0;i<n;i++)rest[i]=Pivots[i]?Pivots[i].localRotation:Quaternion.identity;}
  void OnEnable(){primed=false;}
  void LateUpdate(){
   if(Pivots==null||!d||!d.Scene||!d.Scene.Car)return;float dt=Time.deltaTime;if(dt<=0)return;var car=d.Scene.Car;
   // re-primed after the cabin was hidden or the car was moved without driving (a teleport would read as a huge jolt)
   var v=(car.position-lastPos)/dt;if(!primed||v.sqrMagnitude>60*60){lastPos=car.position;lastVel=Vector3.zero;primed=true;return;}
   var a=(v-lastVel)/dt;lastPos=car.position;lastVel=v;a=Vector3.ClampMagnitude(a,30);
   var local=car.InverseTransformDirection(a);bump=Mathf.Lerp(bump,(Mathf.PerlinNoise(Time.time*7.3f,.4f)-.5f)*Mathf.Clamp01(d.Player.Speed/10f),dt*6);
   Swing=0;
   for(int i=0;i<Pivots.Length;i++){if(!Pivots[i])continue;float len=Lengths!=null&&i<Lengths.Length?Lengths[i]:.1f;float w2=9.81f/Mathf.Max(.05f,len);
    // A pendulum in an accelerating car settles at atan(a/g) from vertical, trailing the acceleration: pulling away
    // swings it back toward the driver (+pitch about the car's right axis), a left turn swings it out to the right.
    var eq=new Vector2(Mathf.Atan2(local.z,9.81f),Mathf.Atan2(-local.x,9.81f))*Mathf.Rad2Deg+new Vector2(bump*9,bump*-6)*(1+i*.4f);
    vel[i]+=-(ang[i]-eq)*w2*dt;vel[i]*=Mathf.Exp(-dt*(1.4f+i*.3f));ang[i]+=vel[i]*dt;ang[i]=Vector2.ClampMagnitude(ang[i],38);
    Pivots[i].localRotation=rest[i]*Quaternion.Euler(ang[i].x,0,ang[i].y);Swing=Mathf.Max(Swing,ang[i].magnitude);}
  }
 }
}
