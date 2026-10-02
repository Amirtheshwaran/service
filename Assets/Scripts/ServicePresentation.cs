using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace ServiceGameV2 {
 // Title presentation. V20: the title is a tape of the county at night that cuts between slow-drifting shots: the
 // depot, a porch light down a drive, Vale House in the rain, Harrow Lodge, the road running north to the survey
 // barricade. The order follows the saved night. Each cut is a tape skip (ServiceMenus watches ShotChangedAt).
 public sealed class ServicePresentation:MonoBehaviour {
  ServiceDirector d;Transform viewpoint;float introElapsed;bool skipped,wasTitle;
  struct Shot{public Vector3 From,To,Look;public float Fov;}
  readonly List<Shot> shots=new List<Shot>();int shot=-1;float shotStart,clock;int orderNight=-1;
  public const float ShotSeconds=22;
  public float ShotChangedAt {get;private set;}
  public int ShotIndex=>shot;public int ShotCount=>shots.Count;
  public bool IntroVisible=>!skipped&&d.Phase==ServicePhase.Title&&introElapsed<5.6f;
  public float IntroTextAlpha {get{float t=introElapsed;return Mathf.SmoothStep(0,1,t/1.1f)*(1-Mathf.SmoothStep(0,1,(t-3.4f)/1.4f));}}
  public float IntroBackgroundAlpha=>1-Mathf.SmoothStep(0,1,(introElapsed-4)/1.6f);
  public void Initialize(ServiceDirector director){d=director;viewpoint=transform.Find("Manor title viewpoint");introElapsed=0;}
  static float Ground(Vector3 p){var t=Terrain.activeTerrain;float g=t?t.SampleHeight(p)+t.transform.position.y:p.y;if(Physics.Raycast(new Vector3(p.x,g+30,p.z),Vector3.down,out var hit,60,~0,QueryTriggerInteraction.Ignore))g=Mathf.Max(g,hit.point.y);return g;}
  Shot Toward(Vector3 target,Vector3 back,float distance,float height,float push,float fov,float lookUp=1.2f){
   back.y=0;back.Normalize();var from=target+back*distance;from.y=Ground(from)+height;
   for(int i=0;i<6&&Physics.CheckSphere(from,.45f,~0,QueryTriggerInteraction.Ignore);i++){from+=back*.8f;from.y=Ground(from)+height;}
   var to=from-back*push;to.y=Ground(to)+height;return new Shot{From=from,To=to,Look=target+Vector3.up*lookUp,Fov=fov};}
  void BuildShots(int night){
   shots.Clear();orderNight=night;var s=d.Scene;
   Shot? vale=null,depot=null,correll=null,harrow=null,road=null;
   if(viewpoint)vale=new Shot{From=viewpoint.position,To=viewpoint.position+viewpoint.forward*1.4f,Look=viewpoint.position+viewpoint.forward*20,Fov=55};
   try{var p0=d.Property(0);correll=Toward(p0.Door.position,-p0.Inward+p0.Door.right*.25f,17,1.55f,2.2f,50);}catch{}
   try{var p3=d.Property(3);harrow=Toward(p3.Door.position,-p3.Inward-p3.Door.right*.35f,15,1.6f,1.8f,52,1.6f);}catch{}
   if(s.Depot){var c=s.Car?s.Car.position:s.Depot.position;depot=Toward(c,(s.Car?s.Car.right:Vector3.right)*1+(s.Car?-s.Car.forward:Vector3.back)*.8f,7.5f,1.45f,1.1f,48,.9f);}
   if(s.Route!=null&&s.Route.Length>3&&s.Route[s.Route.Length-1]){var end=s.Route[s.Route.Length-1].position;var back=s.Route[s.Route.Length-4].position-end;road=Toward(end,back,30,1.7f,3.5f,46,1.4f);}
   var order=night>=2?new[]{road,vale,harrow,depot,correll}:night==1?new[]{vale,harrow,road,correll,depot}:new[]{depot,correll,vale,harrow,road};
   foreach(var o in order)if(o.HasValue)shots.Add(o.Value);
  }
  void LateUpdate(){
   if(!d)return;
   if(d.Phase==ServicePhase.Title){
    float dt=ServiceMenus.Dt;introElapsed+=Mathf.Min(dt,.1f);clock+=dt;
    if(!d.IsSmoke&&introElapsed>.7f&&((Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame)||(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)))skipped=true;
    int night=d.HasSavedRoute?Mathf.Clamp(PlayerPrefs.GetInt(d.IsSmoke?"SERVICE.test.night":"SERVICE.v5.night",0),0,2):0;
    if(shots.Count==0||night!=orderNight){BuildShots(night);shot=-1;}
    if(shots.Count==0)return;
    float now=clock;
    if(shot<0||now-shotStart>ShotSeconds){shot=(shot+1)%shots.Count;shotStart=now;ShotChangedAt=now;}
    var sh=shots[shot];float k=Mathf.SmoothStep(0,1,(now-shotStart)/ShotSeconds);
    var pos=Vector3.Lerp(sh.From,sh.To,k)+Vector3.up*Mathf.Sin(now*.11f)*.05f;var dir=sh.Look-pos;
    var rot=Quaternion.LookRotation(dir)*Quaternion.Euler(Mathf.Sin(now*.07f)*.35f,Mathf.Sin(now*.055f)*.7f,0);
    d.Scene.View.transform.SetPositionAndRotation(pos,rot);d.Scene.View.fieldOfView=sh.Fov;wasTitle=true;
   }else if(wasTitle){wasTitle=false;skipped=true;d.Scene.View.fieldOfView=68;}
  }
 }
}
