using UnityEngine;
namespace ServiceGameV2 {
 // V23: the hand torch is an inverse-square spot, so a note or a door at arm's length took ~40x the light it gets at
 // five metres and burned to plain white (the playtest: "while the note is on the door, if I use the flashlight nothing
 // is visible"). Like the auto-iris on a real camcorder, the torch eases down as the surface it points at comes closer.
 public sealed class ServiceTorchExposure:MonoBehaviour {
  ServiceDirector d;Light torch;float baseIntensity,scale=1,nextScan;readonly System.Collections.Generic.List<SkinnedMeshRenderer> figures=new System.Collections.Generic.List<SkinnedMeshRenderer>();
  public float Scale=>scale;public float LastDistance {get;private set;} public int Figures=>figures.Count;public int Updates {get;private set;} public static bool Trace;public string Why {get;private set;}="";
  // V27 ("the flashlight does not flash unless you're against a surface"): a real torch throw - six times the light,
  // further and a little wider; the auto-iris (Near) now holds everything inside four metres at one exposure, so a
  // door at arm's length is no brighter than the hall four metres off, and past that it carries down the hall.
  public const float Near=4f,Floor=.03f,Throw=26f;
  public void Initialize(ServiceDirector director){d=director;torch=d.Scene.Flashlight;
   if(torch){torch.intensity=Throw;torch.range=42f;torch.spotAngle=56f;torch.innerSpotAngle=24f;baseIntensity=torch.intensity;}}
  void LateUpdate(){
   if(!torch||!torch.enabled)return;Updates++;
   float dist=Near+.5f;var from=torch.transform.position;var ray=new Ray(from,torch.transform.forward);
   // V27 redo: the bright middle of the beam, not one thread down its centre (one ray went past Walter's ear into the
   // empty house behind him and the iris stayed wide open on his face): the centre and four rays 9 degrees out
   var fwd=torch.transform.forward;var up=torch.transform.up;var right=torch.transform.right;
   for(int k=0;k<5;k++){var dir=k==0?fwd:Quaternion.AngleAxis(9f,k==1?up:k==2?-up:k==3?right:-right)*fwd;
    foreach(var h in Physics.SphereCastAll(new Ray(from,dir),.05f,Near+.5f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)){if(h.collider is CharacterController)continue;if(d.Scene.Walker&&h.collider.transform.IsChildOf(d.Scene.Walker.transform))continue;if(h.distance>0&&h.distance<dist)dist=h.distance;}}
   // people and creatures. They have no ordinary colliders, so the beam went straight through them to whatever was behind;
   // anyone drawn (a skinned figure: residents, Rex, the presence, the woman) inside the bright cone counts as the surface
   if(Time.time>nextScan){nextScan=Time.time+.4f;figures.Clear();var eye=d.Scene.View.transform;
    foreach(var r in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None)){if(!r.enabled||!r.gameObject.activeInHierarchy||r.transform.IsChildOf(eye)||(d.Scene.Walker&&r.transform.IsChildOf(d.Scene.Walker.transform)))continue;if((r.bounds.center-eye.position).sqrMagnitude<(Near+3f)*(Near+3f))figures.Add(r);}}
   foreach(var r in figures){if(!r||!r.enabled||!r.gameObject.activeInHierarchy)continue;var b=r.bounds;var to=b.center-from;float fd=to.magnitude;if(fd>Near+.5f||fd<.01f)continue;
    if(Vector3.Angle(fwd,to)<15f||b.IntersectRay(ray)){fd=Mathf.Max(.3f,Vector3.Distance(b.ClosestPoint(from),from));if(fd<dist)dist=fd;}}
   // the note sits on the door: aim at it within reach and it counts as the surface (its quads have no collider)
   if(Trace){var sb=new System.Text.StringBuilder();sb.Append($"from {from:F2} dir {ray.direction:F2}; hits:");foreach(var h in Physics.SphereCastAll(ray,.05f,Near+.5f,~0,QueryTriggerInteraction.Collide))sb.Append($" {h.collider.name}(L{h.collider.gameObject.layer},{h.distance:F2},{h.collider.GetType().Name})");sb.Append("; near figures:");foreach(var r in figures){if((r.bounds.center-from).sqrMagnitude<9f){var b=r.bounds;b.Expand(-.08f);sb.Append($" {r.name} {b.center:F2}/{b.size:F2} hit {b.IntersectRay(ray,out float q)} {q:F2};");}}Why=sb.ToString();}
   LastDistance=dist;float want=Mathf.Clamp((dist*dist)/(Near*Near),Floor,1f);
   scale=Mathf.MoveTowards(scale,want,Time.deltaTime*(want<scale?6f:2.5f));
   torch.intensity=baseIntensity*scale;}
 }
}
