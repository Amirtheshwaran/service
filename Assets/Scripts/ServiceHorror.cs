using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2 {
 public enum PursuitPhase { Dormant, Reveal, Chase, Attack, Caught, Escaped }
 public sealed class ServiceHorror:MonoBehaviour {
  public PursuitPhase Phase {get;private set;}
  public bool Active=>Phase==PursuitPhase.Reveal||Phase==PursuitPhase.Chase;
  public bool Caught=>Phase==PursuitPhase.Caught||Phase==PursuitPhase.Attack;
  public bool ForcedLook=>(returnEncounter||seenReveal)&&Phase==PursuitPhase.Reveal&&Elapsed<1.4f&&!d.Player.InCar; // V26: not from the driver's seat (start the car)
  public int ReturnAmbushes {get;private set;}
  public float SpawnPathDistance {get;private set;}
  public float ImpactAlpha=>Phase==PursuitPhase.Attack?Mathf.Max(0,1-Mathf.Abs(Elapsed-strikeAt-.52f)/.16f)*.23f:0;
  // V26 playtest ("236: when we look back at the breathing, the monster should just jump on us"): caught from across the
  // room it no longer swings at the air where it stands - it comes at you, fast and straight into your face, then strikes
  bool lunging;float lungeTime,lungeSpeed,strikeAt;
  public int Lunges {get;private set;} public float LungeGap {get;private set;} public float LungeClosest {get;private set;}=99;public bool Lunging=>lunging;
  bool armedReturn,returnEncounter,impactPlayed,bellShut,seenReveal,stairStalk;float armedAt;
  public bool SeenReveal=>seenReveal;public bool StairStalk=>stairStalk;public void DisarmReturnAmbush(){armedReturn=false;}
  public void ArmReturnAmbush(){armedReturn=true;armedAt=Time.time;bellShut=false;}
  // V28: Bell's door, shut quietly behind you so it can bang open as you go - you can still open it and go back in
  public bool BellShutBehindYou=>armedReturn&&bellShut;
  public string ReturnState{get{var bell=d.Property(4);var w=d.Scene.Walker.transform.position;DriveProgress(bell,w,out float pr,out float lat);
   return $"armed {armedReturn} shut {bellShut} door {d.Life.DoorOpenDegrees(4):F0} deg moving {d.Life.DoorMoving(4)} dist {Vector3.Distance(w,bell.Door.position):F1} progress {pr:F1} lateral {lat:F1} inside {bell.InteriorBounds.Contains(w+Vector3.up*.3f)} busy {d.Busy} blocked {d.InputBlocked} active {Active} caught {Caught}";}}
  // V22: the player pulled a door shut (or opened it). A Bell door shut by hand counts as shut, so the bang still comes;
  // opening it again lets the house shut it in its own time.
  public void DoorChanged(int index,bool open){if(index==4&&armedReturn)bellShut=!open;}

  public float Elapsed {get;private set;}
  public int Captures {get;private set;}
  public NavMeshAgent Agent {get;private set;}
  public int PropertyIndex=>p?p.Index:-1;
  public float LookAwaySeconds {get;private set;}
  // V25: your nerve while it stands behind you - fear climbs, every E press steadies you; full, you turn round
  public const float FearStart=.25f,FearRate=.2f,FearPerPress=.085f;
  public float Fear {get;private set;} public int NervePresses {get;private set;} public int NerveBreaks {get;private set;}
  public bool NerveActive=>Active&&!Caught&&p&&p.Encounter==EncounterKind.LookAway&&Elapsed>=1.8f;
  public void SteadyNerve(){if(!NerveActive)return;NervePresses++;Fear=Mathf.Max(0,Fear-FearPerPress);}
  // V28b (audit): the prompt said HOLD and holding counted once - a held E now steadies you a little (taps still do the work)
  public void HoldNerve(float dt){if(!NerveActive)return;Fear=Mathf.Max(0,Fear-.1f*dt);}
  // Objective (small, top-left) and inner-voice subtitle, in the manner of a found-footage walk-through rather than an arcade prompt.
  public string Headline=>p&&p.Encounter==EncounterKind.LookAway&&!returnEncounter?ServiceScript.LookAwayHeadline:Phase==PursuitPhase.Chase||returnEncounter&&d.Player.InCar?d.Player.InCar?(d.Player.EngineRunning?ServiceScript.ChaseDriveHeadline:ServiceScript.ChaseStartCarHeadline):ServiceScript.ChaseHeadlineOnFoot:"";
  public string Instruction=>p&&p.Encounter==EncounterKind.LookAway?(Elapsed<1.8f?p.RevealLine:ServiceScript.LookAwayLines[Mathf.Clamp((int)((Elapsed-1.8f)/3.2f),0,ServiceScript.LookAwayLines.Length-1)]):Phase==PursuitPhase.Chase||returnEncounter&&d.Player.InCar?d.Player.InCar?(d.Player.EngineRunning?ServiceScript.ChaseDrive:ServiceScript.ChaseStartCar):ServiceScript.ChaseOnFoot:p?p.RevealLine:"";
  public string DeathLine=>p?p.DeathLine:"There is no record of your return.";
  ServiceDirector d;ServiceProperty p;NavMeshDataInstance nav;Animator[] animators;
  float repath,steps,growl,movingFor;Vector3 stillOrigin;bool stillAnchored,ignitionEscape;Vector3 lastWalker;readonly float[] approach=new float[6];readonly int[] cue=new int[6];
  float carContact, gazeSeconds;
  public float GazeSeconds=>gazeSeconds;
  void LateUpdate(){if(Agent&&animators!=null)foreach(var a in animators)if(a&&a.gameObject.activeInHierarchy){if(lunging){a.SetBool("Moving",true);a.speed=1.9f;continue;}a.SetBool("Moving",Phase==PursuitPhase.Chase&&p.Encounter==EncounterKind.Pursuit&&Agent.velocity.sqrMagnitude>.08f);a.speed=Phase==PursuitPhase.Chase?Mathf.Clamp(Agent.velocity.magnitude/(p.CreatureVariant==3?4.8f:2.2f),.8f,p.CreatureVariant==3?1.4f:2.1f):1;}}
  public void Initialize(ServiceDirector director){d=director;nav=NavMesh.AddNavMeshData(d.Scene.Navigation);Agent=d.Scene.Entity.GetComponent<NavMeshAgent>();animators=d.Scene.Entity.GetComponentsInChildren<Animator>(true);ResetEncounter();}
  public void ResetEncounter(){bellShut=false;lunging=false;strikeAt=0;scared=false;if(d&&d.Scare)d.Scare.Finish(false);Phase=PursuitPhase.Dormant;Elapsed=LookAwaySeconds=movingFor=0;stillAnchored=false;ignitionEscape=false;armedReturn=returnEncounter=seenReveal=stairStalk=false;System.Array.Clear(approach,0,6);System.Array.Clear(cue,0,6);if(Agent&&Agent.isOnNavMesh)Agent.ResetPath();d.Scene.Entity.SetActive(false);d.Audio.Pursuit(false);foreach(var h in d.Scene.Properties){if(h.WindowLight)h.WindowLight.enabled=h.Index<2||d.NightIndex==0;if(h.EncounterLights!=null)foreach(var l in h.EncounterLights)if(l)l.enabled=true;}}
  static void DriveProgress(ServiceProperty p,Vector3 at,out float fromDoor,out float lateral){
   var r=p.ApproachRoute;fromDoor=0;lateral=float.MaxValue;if(r==null||r.Length<2){var o=at-p.Door.position;o.y=0;fromDoor=o.magnitude;lateral=0;return;}
   float total=0;for(int i=1;i<r.Length;i++)total+=Vector3.Distance(r[i-1],r[i]);
   float along=0,best=float.MaxValue,bestAlong=0;for(int i=1;i<r.Length;i++){var a=r[i-1];var b=r[i];a.y=b.y=at.y;var ab=b-a;float len=ab.magnitude;if(len<1e-4f)continue;float t=Mathf.Clamp01(Vector3.Dot(at-a,ab)/(len*len));float dd=Vector3.Distance(at,a+ab*t);if(dd<best){best=dd;bestAlong=along+t*len;}along+=len;}
   lateral=best;fromDoor=total-bestAlong;
  }
  public float WatcherDistance {get;private set;}
  Vector3 FartherSpawn(Vector3 spawn){var w=d.Scene.Walker.transform.position;var away=spawn-w;away.y=0;if(away.sqrMagnitude<.01f)away=-d.Scene.View.transform.forward;away.Normalize();
   // V28b (audit): "right behind me, don't turn around" - so behind you first (straight back, then 30 deg either side), and only
   // then towards its own spot
   var back=-d.Scene.View.transform.forward;back.y=0;var dirs=new System.Collections.Generic.List<Vector3>();if(back.sqrMagnitude>.01f){back.Normalize();dirs.Add(back);dirs.Add(Quaternion.Euler(0,30,0)*back);dirs.Add(Quaternion.Euler(0,-30,0)*back);}dirs.Add(away);
   foreach(var dir in dirs)foreach(float k in new[]{6.5f,6f,5.5f,5f}){var c=w+dir*k;c.y=spawn.y;if(NavMesh.SamplePosition(c,out var h,.9f,NavMesh.AllAreas)&&Mathf.Abs(h.position.y-spawn.y)<.6f&&p.InteriorBounds.Contains(h.position+Vector3.up*.5f)&&Vector3.Dot(h.position-p.OpeningCentre,p.Inward)>.5f&&ServiceLife.UnderRoof(h.position)&&ServiceInteraction.Clear(d.Scene.View.transform.position,h.position+Vector3.up*1.4f,null,d.Scene.Walker.transform)){WatcherDistance=k;return h.position;} /* V28b (review): in the house, not on the porch (the bounds take in a strip outside the door) */}
   WatcherDistance=Vector3.Distance(new Vector3(w.x,0,w.z),new Vector3(spawn.x,0,spawn.z));return spawn;}
  public void Begin(){Begin(1);}
  public void Begin(int index){BeginEncounter(index,false);}
  // V27: an encounter from a given spot (Vale's open front door)
  bool spawnSet;Vector3 spawnAt;
  public void BeginAt(int index,Vector3 spawn,bool returning=false){spawnSet=true;spawnAt=spawn;BeginEncounter(index,returning);spawnSet=false;}
  void BeginEncounter(int index,bool returning){
   if(d.Player)d.Player.GiveAdrenaline(.7f); // V22: every pursuit starts with at least 7 s of sprint in you
   if(d.Omens)d.Omens.CancelGlimpses();if(d.Scenes)d.Scenes.CancelOutside();if(Agent&&!Agent.enabled)Agent.enabled=true; // V22: never start while a glimpse holds the creature
   if(Active||Caught||(!returning&&d.IsFriendly(index)))return;returnEncounter=returning;seenReveal=false;p=d.Property(index);if(!p.HasEncounter)return;
   // V28b: come back in to Bell's and it is on the stairs, only metres off - it comes down at you slower than your run for
   // its first seconds (as Morrow's does after you see it), so turning and running straight out still gets you to the car
   stairStalk=returning&&!d.Player.InCar&&ServiceLife.Indoors(p,d.Scene.Walker.transform.position);
   // V28b (audit): you can shut the front door from inside now, and a creature walks through a shut leaf where you cannot -
   // a chase never starts with your way out shut: the door bangs open as it comes
   if((p.Encounter==EncounterKind.Pursuit||returning)&&!d.Player.InCar&&d.Life&&p.DoorPanel&&(d.Life.DoorOpenDegrees(p.Index)<25f&&!d.Life.DoorMoving(p.Index)||d.Life.DoorClosing(p.Index))){d.DoorOpenedByItself(p.Index);d.Life.OpenDoor(p,true,false,.18f); /* V28b (review): a bang (not a door already being banged open by the ambush) */if(d.Audio)d.Audio.HorrorAt("doorslam",p.Door.position,.6f);}
   if(!returning&&p.Encounter==EncounterKind.Pursuit)d.ClearNotice(); // FF-8: the reveal line is not hidden under a leftover subtitle
   d.Scene.Entity.transform.SetPositionAndRotation(p.EntitySpawn.position,p.EntitySpawn.rotation);
   if(d.Scene.EntityVariants!=null)for(int i=0;i<d.Scene.EntityVariants.Length;i++)d.Scene.EntityVariants[i].SetActive(i==p.CreatureVariant);
   d.Scene.Entity.SetActive(true);
   var spawn=returning?p.OpeningCentre+p.Inward*1.1f:p.EntitySpawn.position; // V27: Bell's ambush comes out of his front door (from the back room nobody ever saw it - "monster not coming, maybe stuck")
   if(spawnSet)spawn=spawnAt;
   else if(p.Encounter==EncounterKind.Pursuit&&!returning){spawn=ExtendedSpawn(spawn);
    // V28 ("on day 2 he did not spawn"): at Morrow's it set off out of sight behind you and a player who ran never saw it.
    // It stands where you can see it - on the gallery, never between you and the stairs - and you turn to it
    if(p.Index==5&&SeenSpawn(spawn,out var seen)){spawn=seen;seenReveal=true;}}
   // V23: the watcher stood almost on top of you ("kinda too close"): it now stands 5-6.5 m off, on the same floor
   else if(p.Encounter==EncounterKind.LookAway&&!returning)spawn=FartherSpawn(spawn);
   if(!NavMesh.SamplePosition(spawn,out var hit,2,NavMesh.AllAreas))throw new System.InvalidOperationException("Presence outside navigation at "+index);
   ignitionEscape=returning&&d.Player.InCar;carContact=gazeSeconds=0;Fear=FearStart;lunging=false;strikeAt=0; if(returning&&d.Player.InCar&&!(d.Player.EngineRunning||d.Player.IsStarting)&&Vector3.Distance(d.Scene.Car.position,p.Door.position)>=18f)d.Player.IgnitionDelayPending=true; /* V28b (audit): parked close to his door, a dead engine was certain death */ /* V28b: the engine that will not turn over is for when it comes for you sat in the car - on foot, the run to the car is the escape */ /* V26: not with the engine running or turning over (it hung over to the next stop) */stillAnchored=false;movingFor=0;
   Agent.Warp(hit.position);SpawnPathDistance=PathLength(hit.position,d.Scene.Walker.transform.position);Agent.speed=5.25f;var facing=d.Scene.Walker.transform.position-hit.position;facing.y=0;if(facing.sqrMagnitude>.01f)Agent.transform.rotation=Quaternion.LookRotation(facing);Agent.isStopped=true;Phase=PursuitPhase.Reveal;Elapsed=repath=steps=LookAwaySeconds=0;growl=5;lastWalker=d.Scene.Walker.transform.position;
   if(p.WindowLight)p.WindowLight.enabled=false;if(p.EncounterLights!=null)foreach(var l in p.EncounterLights)if(l)l.enabled=l.transform.position.y<p.TableApproach.position.y-1;
   d.Scene.Flashlight.enabled=true;d.Audio.HorrorAt(p.Encounter==EncounterKind.LookAway?"breath":"reveal",hit.position,.52f);// V23: Bell's return ambush is a chase too (his house is a look-away house, so it ran in silence)
   d.Audio.Pursuit(returning||p.Encounter==EncounterKind.Pursuit);
  }
  void Update(){
   if(d==null||d.Phase!=ServicePhase.Playing||d.PaperOpen)return;
   if(!Active&&!Caught){
    if(armedReturn&&!d.InputBlocked&&!d.Busy&&Time.time-armedAt>2){
     // V26: also when you are already in the car (it used to be called off the moment you got in - a playtester who went
     // straight back to the car had no monster that night): his door bangs open and it comes for the car
     var w=d.Player.InCar?d.Scene.Car.position:d.Scene.Walker.transform.position;var bell=d.Property(4);float dist=Vector3.Distance(w,bell.Door.position);
     // V19 drives curve: measure progress along the drive itself (door end -> gate end), not along a straight line.
     DriveProgress(bell,w,out float progress,out float lateral);
     var eye=d.Scene.View.transform;
     if(!bellShut&&d.Life.DoorOpenDegrees(4)<=5&&!d.Life.DoorMoving(4)&&!bell.InteriorBounds.Contains(w+Vector3.up*.3f)&&dist>4)bellShut=true; // already shut
     if(!bellShut&&d.Life.DoorOpenDegrees(4)>5&&!bell.InteriorBounds.Contains(w+Vector3.up*.3f)&&dist>4&&(Vector3.Dot(eye.forward,(bell.Door.position-eye.position).normalized)<.2f||dist>14)){bellShut=true;d.Life.OpenDoor(bell,false,false,1.6f);}
     // V25: however you leave - down the drive or straight across the lawn to the car (a playtester cut across and nothing came)
     if(bellShut&&d.Life.DoorOpenDegrees(4)<3&&dist>7&&(d.Player.InCar?lateral<8f&&d.Player.Speed<2f:dist<34&&(progress>8||lateral>=5))&&!bell.InteriorBounds.Contains(w+Vector3.up*.3f)){ /* in the car: anywhere on his drive (it is 65 m long), parked */armedReturn=false;ReturnAmbushes++;d.BellGone=true;d.Say(ServiceScript.ReturnAmbush);d.Audio.HorrorAt("doorslam",bell.Door.position,.7f);d.Audio.HorrorAt("metalrattle",bell.SoundPoint.position,.32f);d.Life.OpenDoor(bell,true,true,.18f);BeginEncounter(4,true);return;}
    }
    if(d.Player.InCar)return;foreach(var h in d.Scene.Properties)if(h.HasEncounter&&!d.IsFriendly(h.Index)&&d.ResultAt(h.Index)==ServiceResult.Pending&&Vector3.Distance(d.Scene.Walker.transform.position,h.Door.position)<18){approach[h.Index]+=Time.deltaTime;if(approach[h.Index]>3&&cue[h.Index]==0){cue[h.Index]++;d.Audio.HorrorAt(h.Index%2==0?"metalrattle":"woodstress",h.SoundPoint.position,.16f);}if(approach[h.Index]>11&&cue[h.Index]==1){cue[h.Index]++;d.Audio.HorrorAt("taps",h.SoundPoint.position,.19f);}}return;}
   Elapsed+=Time.deltaTime;
   if(Phase==PursuitPhase.Attack){
    if(lunging)Lunge();
    if(scared){if(Elapsed>.25f&&(!d.Scare.Playing||d.Scare.Black)){Phase=PursuitPhase.Caught;Elapsed=0;}return;}
    if(!impactPlayed&&Elapsed>=strikeAt+.48f){impactPlayed=true;d.Audio.HorrorAt("impact",d.Scene.View.transform.position,.55f);}
   d.Player.FocusOn(CaptureFocus(),1-Mathf.Exp(-Time.deltaTime*15));
    if(Elapsed>=strikeAt+1.25f){Phase=PursuitPhase.Caught;Elapsed=0;}return;
   }
   if(ForcedLook)d.Player.GlanceAt(Agent.transform.position+Vector3.up*1.2f,Elapsed>=1.05f);
   if(Caught){if(Elapsed>3.4f){Captures++;int index=p.Index;if(index==4&&d.NightIndex==1&&d.Scenes)d.Scenes.BellCaught();ResetEncounter();d.RetryProperty(index);d.Player.RestoreApproach(p.Gate.position,p.Gate.eulerAngles.y);d.Say(d.IsFriendly(index)?ServiceScript.RetryAtGateServed:ServiceScript.RetryAtGate);if(d.Timecard)d.Timecard.Rewound();}return;}
   if(p.Encounter==EncounterKind.LookAway){
    if(Elapsed<1){lastWalker=d.Scene.Walker.transform.position;return;}Phase=PursuitPhase.Chase;
    // Horizontal velocity is measured by the controller, independent of Shift, head bob and ground settling.
    var forward=d.Scene.View.transform.forward;forward.y=0;
    var toward=Agent.transform.position-d.Scene.View.transform.position;toward.y=0;
    float facing=Vector3.Dot(forward.normalized,toward.normalized);
    bool moving=d.Player.HorizontalSpeed>.35f;
    movingFor=moving?movingFor+Time.deltaTime:0;
    if(Elapsed>=1.6f&&movingFor>.16f){Catch();return;} /* V28b (audit): a beat to stop dead after the breath */
    bool watching=ServiceInteraction.Watching(d.Scene.View,Agent.transform.position+Vector3.up*1.15f,Agent.transform);
    gazeSeconds=watching?gazeSeconds+Time.deltaTime:Mathf.Max(0,gazeSeconds-Time.deltaTime*2);
    if(gazeSeconds>=1.1f){Catch();return;}
    if(Elapsed>=1.8f){Fear=Mathf.Min(1,Fear+FearRate*Time.deltaTime);if(Fear>=1){NerveBreaks++;d.Say(ServiceScript.NerveBroke);Catch();return;}}
    if(facing<-.2f&&!moving){LookAwaySeconds+=Time.deltaTime;}else LookAwaySeconds=0;
    growl-=Time.deltaTime;if(growl<0){growl=4;d.Audio.HorrorAt("breath",Agent.transform.position,.3f);}
    if(LookAwaySeconds>=8){Complete();d.Say(ServiceScript.LookAwaySurvived);return;}if(Elapsed>28)Catch();return;
   }
   if(d.Player.InCar&&(!ignitionEscape||(d.Player.EngineRunning&&d.Player.Speed>3))){Complete();d.Audio.DoorAt(d.Scene.Car.position);d.Say(ServiceScript.Escaped);return;}
   if(Phase==PursuitPhase.Reveal){var to=d.Scene.Walker.transform.position-Agent.transform.position;to.y=0;if(to.sqrMagnitude>.1f)Agent.transform.rotation=Quaternion.RotateTowards(Agent.transform.rotation,Quaternion.LookRotation(to),80*Time.deltaTime);if(Elapsed<(returnEncounter?2.4f:seenReveal?4.2f:3.1f))return;Phase=PursuitPhase.Chase;Elapsed=0;Agent.isStopped=false;}
   var pursuitTarget=d.Player.InCar?d.Scene.Car.position:d.Scene.Walker.transform.position;float distance=Vector3.Distance(Agent.transform.position,pursuitTarget);
   // The closed car door buys a short, visible attack wind-up. It does not
   // dismiss the pursuer: staying parked still ends in capture.
   if(ignitionEscape&&d.Player.InCar&&distance<2.7f){
    if(carContact==0){foreach(var a in animators)if(a&&a.gameObject.activeInHierarchy){a.SetBool("Moving",false);a.CrossFadeInFixedTime("Attack",.08f,0);}d.Audio.HorrorAt("metalrattle",Agent.transform.position,.45f);d.Say(ServiceScript.CarDoorContact);}
    Agent.isStopped=true;carContact+=Time.deltaTime;if(carContact>=2.2f)Catch();return;
   }
   if(carContact>0){carContact=0;Agent.isStopped=false;}
   float surge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,.85f,Mathf.Sin(Elapsed*1.1f+p.Index)));
   float targetSpeed=Mathf.Lerp(5.65f,6.25f,Mathf.Clamp01(Elapsed/16))+surge*.55f;
   /* V28: after a reveal where you see it (Morrow's night two) it is only metres off - it stalks for a few seconds, slower than your run, faster than your walk; then it runs */if((seenReveal&&Elapsed<6f)||(stairStalk&&Elapsed<6.5f)){targetSpeed=Mathf.Min(targetSpeed,4.2f);if(Agent.speed>4.2f)Agent.speed=4.2f;}
   /* V28b: out of the house it is slower than your run (5.2 against 5.5), faster than your stagger and your walk - running
      straight for the car gets away; stopping, walking or running out of breath does not */if(!d.Player.InCar&&!ServiceLife.Indoors(p,d.Scene.Walker.transform.position))targetSpeed=Mathf.Min(targetSpeed,5.2f);
   if(d.Player.InCar&&ignitionEscape)targetSpeed=4.1f;Agent.acceleration=14;Agent.angularSpeed=380;Agent.speed=Mathf.MoveTowards(Agent.speed,targetSpeed,Time.deltaTime*2.5f);
   repath-=Time.deltaTime;if(repath<=0){repath=.16f;var target=pursuitTarget;if(distance>6&&!d.Player.InCar)target+=Vector3.ClampMagnitude(d.Scene.Walker.velocity,3)*.3f;if(NavMesh.SamplePosition(target,out var goal,2,NavMesh.AllAreas))Agent.SetDestination(goal.position);}
   steps-=Time.deltaTime;if(steps<=0&&Agent.velocity.sqrMagnitude>.2f){steps=Mathf.Clamp(1.65f/Agent.velocity.magnitude,.25f,.48f);d.Audio.MonsterFootstep(Agent.transform.position);}growl-=Time.deltaTime;if(growl<=0){growl=distance<7?3.7f:7.5f;d.Audio.HorrorAt(p.Index%2==0?"growl":"breath",Agent.transform.position,distance<7?.42f:.29f);}
   var delta=pursuitTarget-Agent.transform.position;bool blocked=Physics.Linecast(Agent.transform.position+Vector3.up,d.Scene.View.transform.position,out var obstruction,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)&&obstruction.collider!=d.Scene.Walker;if(d.Player.InCar?delta.magnitude<2.25f:delta.magnitude<1.55f&&!blocked)Catch();
  }
  void Complete(){Phase=PursuitPhase.Escaped;Agent.isStopped=true;d.Audio.Pursuit(false);d.Audio.SilenceThreat();d.Scene.Entity.SetActive(false);}
  Vector3 CaptureFocus(){foreach(var animator in animators)if(animator&&animator.gameObject.activeInHierarchy)foreach(var bone in animator.GetComponentsInChildren<Transform>()){var name=bone.name.ToLowerInvariant();if(name=="head"||name.EndsWith(":head")||name.EndsWith("_head"))return bone.position;}return Agent.transform.position+Vector3.up*1.75f;}
  public void Catch(){if(!Active)return;Phase=PursuitPhase.Attack;Elapsed=0;impactPlayed=false;scared=false;Agent.isStopped=true;Agent.ResetPath();
   var direction=d.Scene.Walker.transform.position-Agent.transform.position;direction.y=0;if(direction.sqrMagnitude>.01f)Agent.transform.rotation=Quaternion.LookRotation(direction);
   LungeGap=direction.magnitude;LungeClosest=LungeGap;lunging=!d.Player.InCar&&LungeGap>1.6f;strikeAt=lunging?99:0;
   if(lunging){Lunges++;lungeTime=Mathf.Clamp(LungeGap/15f,.25f,.45f);lungeSpeed=LungeGap/lungeTime;
    foreach(var a in animators)if(a&&a.gameObject.activeInHierarchy){a.SetBool("Moving",true);a.speed=1.9f;var run=a.HasState(0,Animator.StringToHash("Run"))?"Run":a.HasState(0,Animator.StringToHash("Pursuing"))?"Pursuing":null;if(run!=null)a.CrossFadeInFixedTime(run,.05f,0);}
    d.Audio.HorrorAt("reveal",Agent.transform.position+Vector3.up*1.2f,.62f);}
   else{foreach(var a in animators)if(a&&a.gameObject.activeInHierarchy){a.SetBool("Moving",false);a.speed=1;a.CrossFadeInFixedTime("Attack",.08f,0);}Scare();}
   d.Audio.Pursuit(false);d.Audio.HorrorAt("gasp",d.Scene.View.transform.position,.42f);}
  // V27: the jumpscare - its head in your face, the scream, black (see ServiceJumpscare)
  bool scared;public int Scares {get;private set;}
  Transform ScareHead(){foreach(var animator in animators)if(animator&&animator.gameObject.activeInHierarchy){var h=ServiceJumpscare.HeadOf(animator.transform);if(h)return h;}return Agent.transform;}
  void Scare(){if(!d.Scare||scared)return;scared=true;Scares++;strikeAt=99;d.Scare.Play(Agent.transform,ScareHead(),"roar",p&&p.CreatureVariant==3?.58f:.66f,1.05f,.55f,d.Player.InCar?.75f:.5f);}
  void Lunge(){var w=d.Scene.Walker.transform.position;var to=w-Agent.transform.position;to.y=0;float gap=to.magnitude;LungeClosest=Mathf.Min(LungeClosest,gap);
   if(gap>.95f&&Elapsed<lungeTime+.2f){Agent.Move(to/gap*Mathf.Min(gap-.9f,lungeSpeed*Time.deltaTime));Agent.transform.rotation=Quaternion.LookRotation(to/gap);return;}
   // held up on the way (a chair, the end of the floor): it is there anyway
   if(gap>1.5f&&NavMesh.SamplePosition(w-to/gap*.9f,out var h,.8f,NavMesh.AllAreas)){Agent.Warp(h.position);var g=w-h.position;g.y=0;LungeClosest=Mathf.Min(LungeClosest,g.magnitude);if(g.sqrMagnitude>.01f)Agent.transform.rotation=Quaternion.LookRotation(g);}
   lunging=false;strikeAt=Mathf.Max(0,Elapsed-.22f);
   foreach(var a in animators)if(a&&a.gameObject.activeInHierarchy){a.SetBool("Moving",false);a.speed=1;a.CrossFadeInFixedTime("Attack",.05f,0);}Scare();}
  float PathLength(Vector3 from,Vector3 to){var path=new NavMeshPath();if(!NavMesh.CalculatePath(from,to,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return -1;float length=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);return length;}
  Vector3 ExtendedSpawn(Vector3 initial){
   var walker=d.Scene.Walker.transform.position;var away=initial-walker;away.y=0;away.Normalize();var best=initial;float longest=PathLength(initial,walker);
   foreach(float distance in new[]{2f,4f,6f,8f})foreach(float angle in new[]{0f,-35f,35f}){
    var candidate=initial+Quaternion.Euler(0,angle,0)*away*distance;
    if(!NavMesh.SamplePosition(candidate,out var hit,1.25f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-initial.y)>.5f||!p.InteriorBounds.Contains(hit.position+Vector3.up*.2f))continue;
    float length=PathLength(hit.position,walker);if(length>longest&&length<22){best=hit.position;longest=length;}
   }return best;
  }
  // somewhere in sight, on your floor, not on your way out: the farthest such spot first (strictly behind you, then
  // anywhere off your path to the stairs that may stand up to 4 m nearer the way out than you, but well off your path, held 4.2 s and then stalking slower than your run for 6 s)
  public string SeenInfo {get;private set;}="";
  bool SeenSpawn(Vector3 fallback,out Vector3 best){best=fallback;var w=d.Scene.Walker.transform.position;var eye=d.Scene.View.transform.position;
   if(!NavMesh.SamplePosition(p.OpeningCentre+p.Inward*1.2f,out var ex,1.5f,NavMesh.AllAreas)||!NavMesh.SamplePosition(w,out var me,1.2f,NavMesh.AllAreas)){SeenInfo="no exit/me";return false;}
   var myPath=new NavMeshPath();if(!NavMesh.CalculatePath(me.position,ex.position,NavMesh.AllAreas,myPath)||myPath.corners.Length<2){SeenInfo="no path out";return false;}
   float mine=0;for(int k=1;k<myPath.corners.Length;k++)mine+=Vector3.Distance(myPath.corners[k-1],myPath.corners[k]);
   float OffPath(Vector3 q){float bestD=99;for(int k=1;k<myPath.corners.Length;k++){var a=myPath.corners[k-1];var b=myPath.corners[k];var ab=b-a;float t=ab.sqrMagnitude<1e-4f?0:Mathf.Clamp01(Vector3.Dot(q-a,ab)/ab.sqrMagnitude);bestD=Mathf.Min(bestD,Vector3.Distance(q,a+ab*t));}return bestD;}
   int seenN=0,floorN=0;
   foreach(float margin in new[]{3f,-4f}){ /* the reveal holds it 4.2 s while you are free after 1.4 - a head start of most of that */float longest=-1;
    for(int a=0;a<24;a++)foreach(float r in new[]{4f,5f,6.5f,8f,10f,12f}){var c=w+Quaternion.Euler(0,a*15f,0)*Vector3.forward*r;if(!NavMesh.SamplePosition(c,out var h,.9f,NavMesh.AllAreas)||Mathf.Abs(h.position.y-me.position.y)>.5f||!p.InteriorBounds.Contains(h.position+Vector3.up*.2f))continue;floorN++;
     if(!ServiceInteraction.Clear(eye,h.position+Vector3.up*1.4f,null,d.Scene.Walker.transform))continue;seenN++;
     float toExit=PathLength(h.position,ex.position);if(toExit<0||toExit<mine+margin)continue;if(margin<3f&&OffPath(h.position)<1.6f)continue;
     float len=PathLength(h.position,me.position);if(len>longest&&len<24f){longest=len;best=h.position;}}
    if(longest>0){SeenInfo=$"margin {margin} path {longest:F1} m (on floor {floorN}, in sight {seenN})";return true;}}
   SeenInfo=$"none (on floor {floorN}, in sight {seenN}, my way out {mine:F1} m)";return false;}
  void OnDestroy(){if(nav.valid)nav.Remove();}
 }
}
