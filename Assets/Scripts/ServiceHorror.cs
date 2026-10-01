using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2 {
 public enum PursuitPhase { Dormant, Reveal, Chase, Attack, Caught, Escaped }
 public sealed class ServiceHorror:MonoBehaviour {
  public PursuitPhase Phase {get;private set;}
  public bool Active=>Phase==PursuitPhase.Reveal||Phase==PursuitPhase.Chase;
  public bool Caught=>Phase==PursuitPhase.Caught||Phase==PursuitPhase.Attack;
  public bool ForcedLook=>returnEncounter&&Phase==PursuitPhase.Reveal&&Elapsed<1.4f;
  public int ReturnAmbushes {get;private set;}
  public float SpawnPathDistance {get;private set;}
  public float ImpactAlpha=>Phase==PursuitPhase.Attack?Mathf.Max(0,1-Mathf.Abs(Elapsed-.52f)/.16f)*.23f:0;
  bool armedReturn,returnEncounter,impactPlayed;float armedAt;
  public void ArmReturnAmbush(){armedReturn=true;armedAt=Time.time;}

  public float Elapsed {get;private set;}
  public int Captures {get;private set;}
  public NavMeshAgent Agent {get;private set;}
  public int PropertyIndex=>p?p.Index:-1;
  public float LookAwaySeconds {get;private set;}
  // Objective (small, top-left) and inner-voice subtitle, in the manner of a found-footage walk-through rather than an arcade prompt.
  public string Headline=>p&&p.Encounter==EncounterKind.LookAway?ServiceScript.LookAwayHeadline:Phase==PursuitPhase.Chase?d.Player.InCar?(d.Player.EngineRunning?ServiceScript.ChaseDriveHeadline:ServiceScript.ChaseStartCarHeadline):ServiceScript.ChaseHeadlineOnFoot:"";
  public string Instruction=>p&&p.Encounter==EncounterKind.LookAway?(Elapsed<1.8f?p.RevealLine:ServiceScript.LookAwayLines[Mathf.Clamp((int)((Elapsed-1.8f)/3.2f),0,ServiceScript.LookAwayLines.Length-1)]):Phase==PursuitPhase.Chase?d.Player.InCar?(d.Player.EngineRunning?ServiceScript.ChaseDrive:ServiceScript.ChaseStartCar):ServiceScript.ChaseOnFoot:p?p.RevealLine:"";
  public string DeathLine=>p?p.DeathLine:"There is no record of your return.";
  ServiceDirector d;ServiceProperty p;NavMeshDataInstance nav;Animator[] animators;
  float repath,steps,growl,movingFor;Vector3 stillOrigin;bool stillAnchored,ignitionEscape;Vector3 lastWalker;readonly float[] approach=new float[6];readonly int[] cue=new int[6];
  float carContact, gazeSeconds;
  public float GazeSeconds=>gazeSeconds;
  void LateUpdate(){if(Agent&&animators!=null)foreach(var a in animators)if(a&&a.gameObject.activeInHierarchy){a.SetBool("Moving",Phase==PursuitPhase.Chase&&p.Encounter==EncounterKind.Pursuit&&Agent.velocity.sqrMagnitude>.08f);a.speed=Phase==PursuitPhase.Chase?Mathf.Clamp(Agent.velocity.magnitude/(p.CreatureVariant==3?4.8f:2.2f),.8f,p.CreatureVariant==3?1.4f:2.1f):1;}}
  public void Initialize(ServiceDirector director){d=director;nav=NavMesh.AddNavMeshData(d.Scene.Navigation);Agent=d.Scene.Entity.GetComponent<NavMeshAgent>();animators=d.Scene.Entity.GetComponentsInChildren<Animator>(true);ResetEncounter();}
  public void ResetEncounter(){Phase=PursuitPhase.Dormant;Elapsed=LookAwaySeconds=movingFor=0;stillAnchored=false;ignitionEscape=false;armedReturn=returnEncounter=false;System.Array.Clear(approach,0,6);System.Array.Clear(cue,0,6);if(Agent&&Agent.isOnNavMesh)Agent.ResetPath();d.Scene.Entity.SetActive(false);d.Audio.Pursuit(false);foreach(var h in d.Scene.Properties){if(h.WindowLight)h.WindowLight.enabled=h.Index<2||d.NightIndex==0;if(h.EncounterLights!=null)foreach(var l in h.EncounterLights)if(l)l.enabled=true;}}
  static void DriveProgress(ServiceProperty p,Vector3 at,out float fromDoor,out float lateral){
   var r=p.ApproachRoute;fromDoor=0;lateral=float.MaxValue;if(r==null||r.Length<2){var o=at-p.Door.position;o.y=0;fromDoor=o.magnitude;lateral=0;return;}
   float total=0;for(int i=1;i<r.Length;i++)total+=Vector3.Distance(r[i-1],r[i]);
   float along=0,best=float.MaxValue,bestAlong=0;for(int i=1;i<r.Length;i++){var a=r[i-1];var b=r[i];a.y=b.y=at.y;var ab=b-a;float len=ab.magnitude;if(len<1e-4f)continue;float t=Mathf.Clamp01(Vector3.Dot(at-a,ab)/(len*len));float dd=Vector3.Distance(at,a+ab*t);if(dd<best){best=dd;bestAlong=along+t*len;}along+=len;}
   lateral=best;fromDoor=total-bestAlong;
  }
  public void Begin(){Begin(1);}
  public void Begin(int index){BeginEncounter(index,false);}
  void BeginEncounter(int index,bool returning){
   if(Active||Caught||(!returning&&d.IsFriendly(index)))return;returnEncounter=returning;p=d.Property(index);if(!p.HasEncounter)return;
   d.Scene.Entity.transform.SetPositionAndRotation(p.EntitySpawn.position,p.EntitySpawn.rotation);
   if(d.Scene.EntityVariants!=null)for(int i=0;i<d.Scene.EntityVariants.Length;i++)d.Scene.EntityVariants[i].SetActive(i==p.CreatureVariant);
   d.Scene.Entity.SetActive(true);
   var spawn=returning?p.TableApproach.position:p.EntitySpawn.position;
   if(p.Encounter==EncounterKind.Pursuit&&!returning)spawn=ExtendedSpawn(spawn);
   if(!NavMesh.SamplePosition(spawn,out var hit,2,NavMesh.AllAreas))throw new System.InvalidOperationException("Presence outside navigation at "+index);
   ignitionEscape=returning;carContact=gazeSeconds=0; if(returning)d.Player.IgnitionDelayPending=true;stillAnchored=false;movingFor=0;
   Agent.Warp(hit.position);SpawnPathDistance=PathLength(hit.position,d.Scene.Walker.transform.position);Agent.speed=5.25f;var facing=d.Scene.Walker.transform.position-hit.position;facing.y=0;if(facing.sqrMagnitude>.01f)Agent.transform.rotation=Quaternion.LookRotation(facing);Agent.isStopped=true;Phase=PursuitPhase.Reveal;Elapsed=repath=steps=LookAwaySeconds=0;growl=5;lastWalker=d.Scene.Walker.transform.position;
   if(p.WindowLight)p.WindowLight.enabled=false;if(p.EncounterLights!=null)foreach(var l in p.EncounterLights)if(l)l.enabled=l.transform.position.y<p.TableApproach.position.y-1;
   d.Scene.Flashlight.enabled=true;d.Audio.HorrorAt(p.Encounter==EncounterKind.LookAway?"breath":"reveal",hit.position,.52f);d.Audio.Pursuit(p.Encounter==EncounterKind.Pursuit);
  }
  void Update(){
   if(d==null||d.Phase!=ServicePhase.Playing||d.PaperOpen)return;
   if(!Active&&!Caught){
    if(armedReturn&&d.Player.InCar)armedReturn=false;
    if(armedReturn&&!d.InputBlocked&&!d.Busy&&Time.time-armedAt>2){
     var bell=d.Property(4);float dist=Vector3.Distance(d.Scene.Walker.transform.position,bell.Door.position);
     // V19 drives curve: measure progress along the drive itself (door end -> gate end), not along a straight line.
     DriveProgress(bell,d.Scene.Walker.transform.position,out float progress,out float lateral);
     if(dist>9&&dist<30&&progress>8&&lateral<5){armedReturn=false;ReturnAmbushes++;d.BellGone=true;d.Say(ServiceScript.ReturnAmbush);d.Audio.HorrorAt("doorslam",bell.Door.position,.7f);d.Audio.HorrorAt("metalrattle",bell.SoundPoint.position,.32f);d.Life.OpenDoor(bell,true,true);BeginEncounter(4,true);return;}
    }
    if(d.Player.InCar)return;foreach(var h in d.Scene.Properties)if(h.HasEncounter&&!d.IsFriendly(h.Index)&&d.ResultAt(h.Index)==ServiceResult.Pending&&Vector3.Distance(d.Scene.Walker.transform.position,h.Door.position)<18){approach[h.Index]+=Time.deltaTime;if(approach[h.Index]>3&&cue[h.Index]==0){cue[h.Index]++;d.Audio.HorrorAt(h.Index%2==0?"metalrattle":"woodstress",h.SoundPoint.position,.16f);}if(approach[h.Index]>11&&cue[h.Index]==1){cue[h.Index]++;d.Audio.HorrorAt("taps",h.SoundPoint.position,.19f);}}return;}
   Elapsed+=Time.deltaTime;
   if(Phase==PursuitPhase.Attack){
    if(!impactPlayed&&Elapsed>=.48f){impactPlayed=true;d.Audio.HorrorAt("impact",d.Scene.View.transform.position,.55f);}
   d.Player.FocusOn(CaptureFocus(),1-Mathf.Exp(-Time.deltaTime*15));
    if(Elapsed>=1.25f){Phase=PursuitPhase.Caught;Elapsed=0;}return;
   }
   if(ForcedLook)d.Player.GlanceAt(Agent.transform.position+Vector3.up*1.2f,Elapsed>=1.05f);
   if(Caught){if(Elapsed>3.4f){Captures++;int index=p.Index;ResetEncounter();d.RetryProperty(index);d.Player.RestoreApproach(p.Gate.position,p.Gate.eulerAngles.y);d.Say(d.IsFriendly(index)?ServiceScript.RetryAtGateServed:ServiceScript.RetryAtGate);}return;}
   if(p.Encounter==EncounterKind.LookAway){
    if(Elapsed<1){lastWalker=d.Scene.Walker.transform.position;return;}Phase=PursuitPhase.Chase;
    // Horizontal velocity is measured by the controller, independent of Shift, head bob and ground settling.
    var forward=d.Scene.View.transform.forward;forward.y=0;
    var toward=Agent.transform.position-d.Scene.View.transform.position;toward.y=0;
    float facing=Vector3.Dot(forward.normalized,toward.normalized);
    bool moving=d.Player.HorizontalSpeed>.35f;
    movingFor=moving?movingFor+Time.deltaTime:0;
    if(movingFor>.16f){Catch();return;}
    bool watching=ServiceInteraction.Watching(d.Scene.View,Agent.transform.position+Vector3.up*1.15f,Agent.transform);
    gazeSeconds=watching?gazeSeconds+Time.deltaTime:Mathf.Max(0,gazeSeconds-Time.deltaTime*2);
    if(gazeSeconds>=1.1f){Catch();return;}
    if(facing<-.2f&&!moving){LookAwaySeconds+=Time.deltaTime;}else LookAwaySeconds=0;
    growl-=Time.deltaTime;if(growl<0){growl=4;d.Audio.HorrorAt("breath",Agent.transform.position,.3f);}
    if(LookAwaySeconds>=8){Complete();d.Say(ServiceScript.LookAwaySurvived);return;}if(Elapsed>28)Catch();return;
   }
   if(d.Player.InCar&&(!ignitionEscape||(d.Player.EngineRunning&&d.Player.Speed>3))){Complete();d.Audio.DoorAt(d.Scene.Car.position);d.Say(ServiceScript.Escaped);return;}
   if(Phase==PursuitPhase.Reveal){var to=d.Scene.Walker.transform.position-Agent.transform.position;to.y=0;if(to.sqrMagnitude>.1f)Agent.transform.rotation=Quaternion.RotateTowards(Agent.transform.rotation,Quaternion.LookRotation(to),80*Time.deltaTime);if(Elapsed<(returnEncounter?2.4f:3.1f))return;Phase=PursuitPhase.Chase;Elapsed=0;Agent.isStopped=false;}
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
   if(d.Player.InCar&&ignitionEscape)targetSpeed=4.1f;Agent.acceleration=14;Agent.angularSpeed=380;Agent.speed=Mathf.MoveTowards(Agent.speed,targetSpeed,Time.deltaTime*2.5f);
   repath-=Time.deltaTime;if(repath<=0){repath=.16f;var target=pursuitTarget;if(distance>6&&!d.Player.InCar)target+=Vector3.ClampMagnitude(d.Scene.Walker.velocity,3)*.3f;if(NavMesh.SamplePosition(target,out var goal,2,NavMesh.AllAreas))Agent.SetDestination(goal.position);}
   steps-=Time.deltaTime;if(steps<=0&&Agent.velocity.sqrMagnitude>.2f){steps=Mathf.Clamp(1.65f/Agent.velocity.magnitude,.25f,.48f);d.Audio.MonsterFootstep(Agent.transform.position);}growl-=Time.deltaTime;if(growl<=0){growl=distance<7?3.7f:7.5f;d.Audio.HorrorAt(p.Index%2==0?"growl":"breath",Agent.transform.position,distance<7?.42f:.29f);}
   var delta=pursuitTarget-Agent.transform.position;bool blocked=Physics.Linecast(Agent.transform.position+Vector3.up,d.Scene.View.transform.position,out var obstruction,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)&&obstruction.collider!=d.Scene.Walker;if(d.Player.InCar?delta.magnitude<2.25f:delta.magnitude<1.55f&&!blocked)Catch();
  }
  void Complete(){Phase=PursuitPhase.Escaped;Agent.isStopped=true;d.Audio.Pursuit(false);d.Audio.SilenceThreat();d.Scene.Entity.SetActive(false);}
  Vector3 CaptureFocus(){foreach(var animator in animators)if(animator&&animator.gameObject.activeInHierarchy)foreach(var bone in animator.GetComponentsInChildren<Transform>()){var name=bone.name.ToLowerInvariant();if(name=="head"||name.EndsWith(":head")||name.EndsWith("_head"))return bone.position;}return Agent.transform.position+Vector3.up*1.75f;}
  public void Catch(){if(!Active)return;Phase=PursuitPhase.Attack;Elapsed=0;impactPlayed=false;Agent.isStopped=true;Agent.ResetPath();
   var direction=d.Scene.Walker.transform.position-Agent.transform.position;direction.y=0;if(direction.sqrMagnitude>.01f)Agent.transform.rotation=Quaternion.LookRotation(direction);
   foreach(var a in animators)if(a&&a.gameObject.activeInHierarchy){a.SetBool("Moving",false);a.speed=1;a.CrossFadeInFixedTime("Attack",.08f,0);}
   d.Audio.Pursuit(false);d.Audio.HorrorAt("gasp",d.Scene.View.transform.position,.42f);}
  float PathLength(Vector3 from,Vector3 to){var path=new NavMeshPath();if(!NavMesh.CalculatePath(from,to,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return -1;float length=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);return length;}
  Vector3 ExtendedSpawn(Vector3 initial){
   var walker=d.Scene.Walker.transform.position;var away=initial-walker;away.y=0;away.Normalize();var best=initial;float longest=PathLength(initial,walker);
   foreach(float distance in new[]{2f,4f,6f,8f})foreach(float angle in new[]{0f,-35f,35f}){
    var candidate=initial+Quaternion.Euler(0,angle,0)*away*distance;
    if(!NavMesh.SamplePosition(candidate,out var hit,1.25f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-initial.y)>.5f||!p.InteriorBounds.Contains(hit.position+Vector3.up*.2f))continue;
    float length=PathLength(hit.position,walker);if(length>longest&&length<22){best=hit.position;longest=length;}
   }return best;
  }
  void OnDestroy(){if(nav.valid)nav.Remove();}
 }
}
