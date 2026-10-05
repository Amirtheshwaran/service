using UnityEngine;
namespace ServiceGameV2 {
 // V23: the hand torch is an inverse-square spot, so a note or a door at arm's length took ~40x the light it gets at
 // five metres and burned to plain white (the playtest: "while the note is on the door, if I use the flashlight nothing
 // is visible"). Like the auto-iris on a real camcorder, the torch eases down as the surface it points at comes closer.
 public sealed class ServiceTorchExposure:MonoBehaviour {
  ServiceDirector d;Light torch;float baseIntensity,scale=1;
  public float Scale=>scale;
  public const float Near=1.8f,Floor=.12f;
  public void Initialize(ServiceDirector director){d=director;torch=d.Scene.Flashlight;if(torch)baseIntensity=torch.intensity;}
  void LateUpdate(){
   if(!torch||!torch.enabled)return;
   float dist=3f;var from=torch.transform.position;var ray=new Ray(from,torch.transform.forward);
   foreach(var h in Physics.SphereCastAll(ray,.05f,3f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)){if(h.collider is CharacterController)continue;if(d.Scene.Walker&&h.collider.transform.IsChildOf(d.Scene.Walker.transform))continue;if(h.distance>0&&h.distance<dist)dist=h.distance;}
   // the note sits on the door: aim at it within reach and it counts as the surface (its quads have no collider)
   float want=Mathf.Clamp((dist*dist)/(Near*Near),Floor,1f);
   scale=Mathf.MoveTowards(scale,want,Time.deltaTime*(want<scale?6f:2.5f));
   torch.intensity=baseIntensity*scale;}
 }
}
