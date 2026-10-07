using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2 {
 // V25, a playtester's idea for 214 Millbrook: night one, tell Walter somebody ought to do something about that dog.
 // Night two Rex is gone, there is blood in the hall, and Walter says it's your turn - and comes out after you. He starts
 // at a walk and works up to a fast, stiff stride (3.3 m/s: a walk loses, a run gets away). Reach the car and drive and
 // he stops in the road watching you go; let him reach you on foot and the route ends there (its own ending).
 public sealed class ServiceWalter:MonoBehaviour {
  ServiceDirector d;GameObject walter;ServiceResidents residents;readonly NavMeshPath path=new NavMeshPath();Vector3[] corners=new Vector3[0];int ci;float repath,speed,holdUntil;
  public const float Stare=2.2f; // he stands in the doorway looking at you first - long enough to turn and run (the tour, slow to react, was caught in a second)
  public const float TopSpeed=3.8f,Reach=1.15f;
  // V25: he runs (the Starter Assets Run_N on his own humanoid rig) once he is up to speed - "cook a running animation
  // that looks fair". Its stride matches about 5.3 m/s at full playback. The state is set once per change: setting it
  // every frame restarted the crossfade every frame, which froze the walk (he glided).
  const float RunStride=5.3f,RunFrom=2.1f;string animState;Animator anim;public string AnimState=>animState;
  void Play(string state,float rate){if(!anim&&walter)anim=walter.GetComponentInChildren<Animator>();if(state!=animState){animState=state;ServiceResidents.Animate(walter,state,rate);}else if(anim)anim.speed=rate;}
  public bool Chasing {get;private set;} public bool Escaped {get;private set;} public int Catches {get;private set;} public float Closest {get;private set;}=99;
  public float Speed=>speed;public Vector3 Position=>walter?walter.transform.position:Vector3.zero;
  public void Initialize(ServiceDirector director){d=director;}
  public void ResetForShift(){Chasing=false;Escaped=false;Closest=99;corners=new Vector3[0];}
  public void Begin(ServiceResidents r){residents=r;walter=r?r.Correll:null;if(!walter)return;r.Release(0);r.Hold(0,true);Chasing=true;Escaped=false;speed=.9f;Closest=99;repath=0;corners=new Vector3[0];holdUntil=Time.time+Stare;animState=null;anim=null;
   d.Audio.Pursuit(true);if(d.Dread)d.Dread.Pulse(.6f);}
  void Update(){
   if(!Chasing||!walter||d.Phase!=ServicePhase.Playing)return;
   var w=d.Scene.Walker.transform.position;var at=walter.transform.position;
   if(Time.time<holdUntil){var look=w-at;look.y=0;if(look.sqrMagnitude>.01f)walter.transform.rotation=Quaternion.RotateTowards(walter.transform.rotation,Quaternion.LookRotation(look),180*Time.deltaTime);Play("Idle",1);return;}
   // away in the car: he stops and watches you go
   if(d.Player.InCar){float away=Vector3.Distance(d.Scene.Car.position,at);if(away>20f||(d.Player.Speed>3f&&away>9f)){End(true);return;}}
   float gap=new Vector2(w.x-at.x,w.z-at.z).magnitude;if(!d.Player.InCar)Closest=Mathf.Min(Closest,gap);
   if(!d.Player.InCar&&gap<Reach&&!d.Busy){Caught();return;}
   speed=Mathf.MoveTowards(speed,TopSpeed,Time.deltaTime*.8f);
   var goal=d.Player.InCar?d.Scene.Car.position+(at-d.Scene.Car.position).normalized*2.6f:w;
   if(Time.time>repath){repath=Time.time+.3f;
    if(NavMesh.SamplePosition(at,out var a,1.5f,NavMesh.AllAreas)&&NavMesh.SamplePosition(goal,out var b,2.5f,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.corners.Length>1){corners=path.corners;ci=1;}}
   if(corners.Length>ci){var tgt=corners[ci];var step=tgt-at;step.y=0;if(step.magnitude<.3f&&ci<corners.Length-1){ci++;tgt=corners[ci];step=tgt-at;step.y=0;}
    var mv=Vector3.ClampMagnitude(step,speed*Time.deltaTime);var next=at+mv;if(NavMesh.SamplePosition(next,out var s,1f,NavMesh.AllAreas))next.y=s.position.y;walter.transform.position=next;
    if(mv.sqrMagnitude>1e-7f)walter.transform.rotation=Quaternion.RotateTowards(walter.transform.rotation,Quaternion.LookRotation(new Vector3(mv.x,0,mv.z)),360*Time.deltaTime);
    if(speed>=RunFrom)Play("Run",Mathf.Clamp(speed/RunStride,.62f,1.05f));else Play("Walk",Mathf.Clamp(speed/Mathf.Max(.3f,ServiceResidents.NaturalSpeed(walter,"Walk")),.8f,1.5f));}
   else Play("Idle",1);
  }
  void Caught(){Chasing=false;Catches++;d.Audio.Pursuit(false);d.Audio.HorrorAt("reveal",walter.transform.position+Vector3.up*1.5f,.6f);Play("Idle",1);
   d.EndWith(ServiceScript.EndingWalter,walter.transform.position+Vector3.up*1.6f);}
  void End(bool escaped){Chasing=false;Escaped=escaped;d.Audio.Pursuit(false);if(walter)Play("Idle",1);if(escaped)d.Say(ServiceScript.WalterEscaped);}
 }
}
