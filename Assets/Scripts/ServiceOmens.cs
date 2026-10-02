using UnityEngine;
namespace ServiceGameV2 {
 // V20 slow burn (the proposal: "something feels off before you can explain why"). Night one has no monsters, only
 // things a tired process server could explain away:
 //  - Harrow Lodge's porch light goes out behind you once the papers are down,
 //  - someone walks about upstairs at Vale House while you are inside (night two: a door slams up there),
 //  - a radio is playing static somewhere in Morrow House, and is off when you reach the room,
 //  - on the drive back to the depot, a figure stands at the treeline in the headlights, and then doesn't.
 // Night two the fog rolls in as the docket empties; night three it is thicker still.
 public sealed class ServiceOmens:MonoBehaviour {
  ServiceDirector d;AudioSource radio;bool porchDone,wasAtHarrow,upstairsSaid,radioDone,radioHeard,treelineDone,fogSaid;float insideVale,glimpseUntil,glimpseSaidAt=-1,fogNow;int upstairsBeats;Vector3 glimpseAt;
  public int Fired {get;private set;} // for the regression test
  public void Initialize(ServiceDirector director){d=director;}
  public void ResetForShift(){
   wasAtHarrow=porchDone=upstairsSaid=radioDone=radioHeard=treelineDone=fogSaid=false;insideVale=0;upstairsBeats=0;glimpseUntil=0;glimpseSaidAt=-1;Fired=0;fogNow=RenderSettings.fogDensity;
   if(radio)radio.Stop();
   if(d.NightIndex==0){var p=d.Property(5);var clip=Resources.Load<AudioClip>("Audio/V5/static/static_1");if(clip){if(!radio){var g=new GameObject("Morrow radio (static)");radio=g.AddComponent<AudioSource>();}
     radio.transform.position=(p.SoundPoint?p.SoundPoint.position:p.InteriorBounds.center)+Vector3.up*.2f;radio.clip=clip;radio.loop=true;radio.spatialBlend=1;radio.rolloffMode=AudioRolloffMode.Linear;radio.minDistance=1.5f;radio.maxDistance=16;radio.volume=.32f;radio.dopplerLevel=0;radio.Play();}}
  }
  bool Quiet=>string.IsNullOrEmpty(d.Notice)&&!d.Busy&&!d.Horror.Active&&!(d.Dialogue&&d.Dialogue.Active);
  bool Inside(int i){var p=d.Property(i);return p.InteriorBounds.Contains(d.Scene.Walker.transform.position+Vector3.up*.9f);}
  void Update(){
   if(!d||d.Phase!=ServicePhase.Playing)return;var walker=d.Scene.Walker.transform.position;
   if(d.NightIndex==0){
    // Harrow: the porch light goes out behind you
    if(Vector3.Distance(walker,d.Property(3).Door.position)<8)wasAtHarrow=true;
    if(!porchDone&&wasAtHarrow&&d.ResultAt(3)!=ServiceResult.Pending){var p=d.Property(3);if(!d.Player.InCar&&Vector3.Distance(walker,p.Door.position)>14){porchDone=true;Fired++;if(p.PorchLight)p.PorchLight.enabled=false;if(p.WindowLight)p.WindowLight.enabled=false;d.Audio.HorrorAt("woodstress",p.Door.position,.18f);if(Quiet)d.Say(ServiceScript.LightsChanged);}}
    // Vale: footsteps upstairs
    Upstairs(false);
    // Morrow: the radio in another room, off by the time you get there
    if(radio&&!radioDone){if(Inside(5))radioHeard=true;if(radioHeard&&(Vector3.Distance(walker,radio.transform.position)<5||d.ResultAt(5)!=ServiceResult.Pending)){radioDone=true;Fired++;radio.Stop();if(Quiet)d.Say(ServiceScript.OmenRadio);}}
    // The drive home: a figure at the treeline
    Treeline();
   }else if(d.NightIndex==1){Upstairs(true);Fog(.031f,.047f);}
   else Fog(.035f,.052f);
  }
  void Upstairs(bool hard){
   if(d.ResultAt(1)!=ServiceResult.Pending&&upstairsBeats>=2)return;
   if(!Inside(1)){return;}insideVale+=Time.deltaTime;var p=d.Property(1);var above=(p.SoundPoint?p.SoundPoint.position:p.InteriorBounds.center)+Vector3.up*3.2f;
   if(upstairsBeats==0&&insideVale>3.5f){upstairsBeats=1;d.Audio.HorrorAt(hard?"doorslam":"taps",above,hard?.5f:.24f);}
   if(upstairsBeats==1&&insideVale>6.5f){upstairsBeats=2;Fired++;d.Audio.HorrorAt("woodstress",above,.26f);if(!upstairsSaid&&Quiet){upstairsSaid=true;d.Say(hard?ServiceScript.OmenUpstairsNight2:ServiceScript.OmenUpstairs);}}
  }
  void Fog(float from,float to){
   int total=0,done=0;foreach(var e in d.Docket){total++;if(e.Result!=ServiceResult.Pending)done++;}float k=total>0?(float)done/total:0;
   float want=Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,k));fogNow=Mathf.MoveTowards(fogNow<=0?from:fogNow,want,Time.deltaTime*.0008f);RenderSettings.fogDensity=fogNow;
   if(!fogSaid&&k>0&&d.Player.InCar&&Quiet&&d.NightIndex==1){fogSaid=true;Fired++;d.Say(ServiceScript.OmenFog);}
  }
  void Treeline(){
   var ent=d.Scene.Entity;if(treelineDone||!ent||!d.AllResolved||!d.Player.InCar||d.Horror.Active||d.Horror.Caught)return;
   var car=d.Scene.Car;if(glimpseUntil<=0){
    if(d.Player.Speed<4||d.Scene.Route==null||d.Scene.Route.Length<4)return;
    // heading back toward the depot, in the woods between the houses
    var start=d.Scene.Route[0].position;float fromDepot=Vector3.Distance(car.position,start);if(fromDepot<110||fromDepot>300)return;
    if(Vector3.Dot(car.forward,(start-car.position).normalized)<.4f)return;
    var ahead=car.position+car.forward*26;var right=Vector3.Cross(Vector3.up,car.forward).normalized;var at=ahead+right*2.2f; // at the lane edge, in the middle of the low beams, clear of the passenger mirror
    if(Physics.Raycast(at+Vector3.up*20,Vector3.down,out var hit,40,~0,QueryTriggerInteraction.Ignore))at.y=hit.point.y;
    var agent=ent.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
    if(d.Scene.EntityVariants!=null)for(int i=0;i<d.Scene.EntityVariants.Length;i++)d.Scene.EntityVariants[i].SetActive(i==0);
    var face=car.position-at;face.y=0;ent.transform.SetPositionAndRotation(at,Quaternion.LookRotation(face));ent.SetActive(true);glimpseAt=at;glimpseUntil=Time.time+2.0f;Fired++;
    foreach(var a in ent.GetComponentsInChildren<Animator>())if(a&&a.isActiveAndEnabled){a.SetBool("Moving",false);a.speed=.6f;}
    return;}
   // gone before you're level with it
   if(Time.time>glimpseUntil||Vector3.Distance(car.position,glimpseAt)<13){ent.SetActive(false);var agent=ent.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=true;treelineDone=true;glimpseSaidAt=Time.time+1.4f;}
  }
  void LateUpdate(){if(glimpseSaidAt>0&&Time.time>glimpseSaidAt&&Quiet){glimpseSaidAt=-1;d.Say(ServiceScript.OmenTreeline);}}
 }
}
