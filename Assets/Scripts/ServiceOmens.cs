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
  // V22: the Vale hall glimpse (night one)
  public int ValeStage {get;private set;} public int ValeGlimpses {get;private set;} public Vector3 ValeFrom=>valeH; public Vector3 ValeTo=>valeE;
  bool valeDone;float valeDur=2.2f,valeT,valeSaidAt=-1,valeCheck,valeSeenFor;Vector3 valeE,valeH,valeBase;GameObject valeBody;
  public void Initialize(ServiceDirector director){d=director;}
  public void ResetForShift(){
   if(glimpseUntil>0&&d.Scene.Entity){var ag=d.Scene.Entity.GetComponent<UnityEngine.AI.NavMeshAgent>();if(ag)ag.enabled=true;}
   if(ValeStage>0)EndVale(false);valeDone=false;ValeStage=0;ValeGlimpses=0;valeUpFor=valeClearFor=0;valeCreaked=false;valeSaidAt=-1;valeSeenFor=0;
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
    // Vale: footsteps upstairs, and then something at the end of the hall
    Upstairs(false);
    ValeHall();
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
   var ent=d.Scene.Entity;if(treelineDone||!ent)return;var car=d.Scene.Car;
   // a glimpse already showing ends the moment the player is out of the car (it is never there when you go to look)
   if(glimpseUntil>0&&(!d.Player.InCar||d.Horror.Active||d.Horror.Caught)){EndGlimpse();return;}
   if(!d.AllResolved||!d.Player.InCar||d.Horror.Active||d.Horror.Caught)return;
   if(glimpseUntil<=0){
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
   if(Time.time>glimpseUntil||Vector3.Distance(car.position,glimpseAt)<13)EndGlimpse();
  }
  void EndGlimpse(){var ent=d.Scene.Entity;if(ent){ent.SetActive(false);var agent=ent.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=true;}treelineDone=true;glimpseUntil=0;glimpseSaidAt=Time.time+1.4f;}
  // ---- V22 Vale hall glimpse
  public void CancelGlimpses(){if(ValeStage>0)EndVale(false);}
  // Where it steps out to (E) and back to (H): 1.3 m along the walkable path from the room doorway (EntitySpawn) toward
  // the player, on the same floor; H a metre back inside the room. Shared with the editor probe (unity.sh Vale22).
  public static bool ValePoints(ServiceProperty p,Vector3 eye,out Vector3 E,out Vector3 H,Transform observer=null){
   E=H=p&&p.EntitySpawn?p.EntitySpawn.position:Vector3.zero;if(!p||!p.EntitySpawn)return false;var spawn=p.EntitySpawn.position;
   var path=new UnityEngine.AI.NavMeshPath();var foot=eye+Vector3.down*1.6f;
   if(!UnityEngine.AI.NavMesh.SamplePosition(spawn,out var s0,1f,UnityEngine.AI.NavMesh.AllAreas)||!UnityEngine.AI.NavMesh.SamplePosition(foot,out var s1,1.5f,UnityEngine.AI.NavMesh.AllAreas))return false;
   if(!UnityEngine.AI.NavMesh.CalculatePath(s0.position,s1.position,UnityEngine.AI.NavMesh.AllAreas,path)||path.corners.Length<2)return false;
   // it stands where you can see it: the first spot 1.3-4 m out of the end room along its way toward you with a clear
   // line to your eye (1.3 m alone left it behind the end room's door frame from the top of the stairs - V22 tour)
   E=Along(path.corners,1.3f);if(Mathf.Abs(E.y-s0.position.y)>.3f)return false;
   for(float need=1.3f;need<=4.01f;need+=.25f){var at=Along(path.corners,need);if(Mathf.Abs(at.y-s0.position.y)>.3f)break;if(ServiceInteraction.Clear(eye,at+Vector3.up*1.4f,null,observer)){E=at;break;}}
   var back=s0.position-E;back.y=0;H=back.sqrMagnitude>.01f?s0.position+back.normalized*1.0f:s0.position;
   if(UnityEngine.AI.NavMesh.SamplePosition(H,out var hs,.6f,UnityEngine.AI.NavMesh.AllAreas)&&Mathf.Abs(hs.position.y-s0.position.y)<.3f)H=hs.position;else H=s0.position;
   return true;}
  static Vector3 Along(Vector3[] c,float need){var E=c[0];for(int i=1;i<c.Length;i++){var seg=c[i]-c[i-1];float len=seg.magnitude;if(len>=need)return c[i-1]+seg.normalized*need;need-=len;E=c[i];}return E;}
  void ValeHall(){
   var ent=d.Scene.Entity;var p=d.Property(1);if(!ent||!p)return;
   if(ValeStage>0){RunVale(ent,p);return;}
   // walking up the upstairs hall toward the study, the end of the hall is ahead of you (from the desk a shelf hides it)
   if(valeDone||glimpseUntil>0||Time.time<valeCheck)return;valeCheck=Time.time+.1f;
   var w=d.Scene.Walker.transform.position;var view=d.Scene.View;
   if(!Inside(1)||w.y<p.Door.position.y+2.8f||d.Player.InCar||d.Busy||d.NoteOpen>=0||d.PaperOpen||(d.Dialogue&&d.Dialogue.Active)||d.Horror.Active||d.Horror.Caught||(d.Horror.Phase!=PursuitPhase.Dormant&&d.Horror.Phase!=PursuitPhase.Escaped)){valeSeenFor=0;return;}
   valeUpFor+=.1f;if(!ValePoints(p,view.transform.position,out var E,out var H,d.Scene.Walker.transform)){valeSeenFor=0;return;}
   float dist=Vector3.Distance(view.transform.position,E);var vp=view.WorldToViewportPoint(E+Vector3.up*1.2f);
   // the hall's end only comes into sight once you are in the hall, turning off to the study - so anywhere well inside
   // the view counts, and a floorboard goes in the end room to make you look (from the stair head the corner hides it)
   bool inFrame=vp.z>0&&vp.x>.12f&&vp.x<.88f&&vp.y>.1f&&vp.y<.95f;bool clear=dist>=5f&&dist<=16f&&ServiceInteraction.Clear(view.transform.position,E+Vector3.up*1.4f,null,d.Scene.Walker.transform);
   valeClearFor=clear?valeClearFor+.1f:0;if(!valeCreaked&&(valeClearFor>.35f||valeUpFor>8f)){valeCreaked=true;d.Audio.HorrorAt("woodstress",H+Vector3.up*.2f,.35f);}
   if(!clear||!inFrame){valeSeenFor=0;return;}
   valeSeenFor+=.1f;if(valeSeenFor<.25f)return;
   // step out
   var agent=ent.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
   int v=Mathf.Clamp(p.CreatureVariant,0,d.Scene.EntityVariants!=null?d.Scene.EntityVariants.Length-1:0);
   if(d.Scene.EntityVariants!=null)for(int i=0;i<d.Scene.EntityVariants.Length;i++)d.Scene.EntityVariants[i].SetActive(i==v);
   valeE=E;valeH=H;valeWalk=Mathf.Clamp(Vector3.Distance(H,E)/1.4f,1.6f,3.4f);valeBase=view.transform.position;var face=valeBase-H;face.y=0;ent.transform.SetPositionAndRotation(H,Quaternion.LookRotation(face.sqrMagnitude>.01f?face:Vector3.forward));ent.SetActive(true);
   valeBody=ent;ValeStage=1;valeT=Time.time;Play(ent,"Emerging",.2f);
  }
  float valeWalk=1.6f,valeUpFor,valeClearFor;bool valeCreaked;public bool ValeCreaked=>valeCreaked;
  void RunVale(GameObject ent,ServiceProperty p){
   var view=d.Scene.View.transform;var w=d.Scene.Walker.transform.position;
   // gone at once if something else needs it, or you leave
   if(!ent.activeSelf||d.Horror.Active||d.Horror.Caught||d.Player.InCar||!Inside(1)){EndVale(false);return;}
   float t=Time.time-valeT;var at=ent.transform.position;float near=Vector3.Distance(new Vector3(w.x,at.y,w.z),at);
   bool rushed=near<4.5f||(d.Player.Running&&near<9f);
   if(rushed&&ValeStage<3){ValeStage=3;valeT=Time.time-.0001f;valeE=at;valeDur=.9f;Play(ent,"Retreating",.15f);t=0;}
   else if(rushed&&ValeStage==3&&valeDur>.9f){valeE=at;valeT=Time.time-.0001f;valeDur=.9f;t=0;}
   var look=view.position-at;look.y=0;if(look.sqrMagnitude>.01f)ent.transform.rotation=Quaternion.RotateTowards(ent.transform.rotation,Quaternion.LookRotation(look),90*Time.deltaTime);
   if(ValeStage==1){float k=Mathf.Clamp01(t/valeWalk);ent.transform.position=Vector3.Lerp(valeH,valeE,Mathf.SmoothStep(0,1,k));if(k>=1){ValeStage=2;valeT=Time.time;Play(ent,"Watching",.25f);d.Audio.HorrorAt("breath",valeE+Vector3.up*1.4f,.2f);if(d.Dread)d.Dread.Pulse(.35f);}}
   else if(ValeStage==2){if(t>1.8f){ValeStage=3;valeT=Time.time;valeDur=Mathf.Max(2.2f,valeWalk*1.1f);Play(ent,"Retreating",.2f);}}
   else{float k=Mathf.Clamp01(t/valeDur);ent.transform.position=Vector3.Lerp(valeE,valeH,Mathf.SmoothStep(0,1,k));if(k>=1)EndVale(true);}
  }
  void EndVale(bool seen){
   var ent=valeBody?valeBody:d.Scene.Entity;
   if(ent){ent.SetActive(false);var agent=ent.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=true;}
   if(ValeStage>0&&seen){ValeGlimpses++;Fired++;d.Audio.HorrorAt("woodstress",valeH,.2f);valeSaidAt=Time.time+1.2f;}
   ValeStage=0;valeDone=true;valeBody=null;}
  static void Play(GameObject ent,string state,float fade){foreach(var a in ent.GetComponentsInChildren<Animator>())if(a&&a.isActiveAndEnabled&&a.HasState(0,Animator.StringToHash(state)))a.CrossFadeInFixedTime(state,fade);}
  void LateUpdate(){if(valeSaidAt>0&&Time.time>valeSaidAt&&Quiet){valeSaidAt=-1;d.Say(ServiceScript.OmenValeHall);}if(glimpseSaidAt>0&&Time.time>glimpseSaidAt&&Quiet){glimpseSaidAt=-1;d.Say(ServiceScript.OmenTreeline);}}
 }
}
