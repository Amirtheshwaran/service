using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2 {
 public enum PursuitPhase { Dormant, Reveal, Chase, Caught, Escaped }
 public sealed class ServiceHorror : MonoBehaviour {
  public PursuitPhase Phase {get;private set;}
  public bool Active => Phase==PursuitPhase.Reveal || Phase==PursuitPhase.Chase;
  public bool Caught => Phase==PursuitPhase.Caught;
  public float Elapsed {get;private set;}
  public int Captures {get;private set;}
  public NavMeshAgent Agent {get;private set;}
  ServiceDirector d; ServiceProperty p; NavMeshDataInstance nav; float repath,steps,growl;
  Animator[] animators; float approachTime; bool upstairsSound,secondSound;
  void LateUpdate(){if(Agent&&animators!=null)foreach(var a in animators){a.SetBool("Moving",Phase==PursuitPhase.Chase&&Agent.velocity.sqrMagnitude>.08f);a.speed=Phase==PursuitPhase.Chase?Mathf.Clamp(Agent.velocity.magnitude/2.2f,.8f,1.6f):1;}}
  public void Initialize(ServiceDirector director){d=director;p=d.Property(1);nav=NavMesh.AddNavMeshData(d.Scene.Navigation);Agent=d.Scene.Entity.GetComponent<NavMeshAgent>();animators=d.Scene.Entity.GetComponentsInChildren<Animator>(true);ResetEncounter();}
  public void ResetEncounter(){Phase=PursuitPhase.Dormant;Elapsed=0;approachTime=0;upstairsSound=secondSound=false;if(Agent&&Agent.isOnNavMesh)Agent.ResetPath();d.Scene.Entity.SetActive(false);d.Audio.Pursuit(false);if(p.EncounterLights!=null)foreach(var l in p.EncounterLights)l.enabled=true;}
  public void Begin(){
   if(Phase!=PursuitPhase.Dormant)return;
   d.Scene.Entity.transform.SetPositionAndRotation(p.EntitySpawn.position,p.EntitySpawn.rotation);d.Scene.Entity.SetActive(true);
   if(!NavMesh.SamplePosition(p.EntitySpawn.position,out var hit,2,NavMesh.AllAreas))throw new System.InvalidOperationException("Entity spawn is outside navigation");
   Agent.Warp(hit.position);Agent.isStopped=true;Phase=PursuitPhase.Reveal;Elapsed=repath=steps=0;growl=4.7f;
   p.WindowLight.enabled=false;if(p.EncounterLights!=null)foreach(var l in p.EncounterLights)l.enabled=l.transform.position.y<p.TableApproach.position.y-1;d.Scene.Flashlight.enabled=true;d.Audio.HorrorAt("reveal",p.EntitySpawn.position,.65f);d.Audio.Pursuit(true);d.Say("Something moved in the upstairs hall.");
  }
  void Update(){
   if(d==null||d.Phase!=ServicePhase.Playing)return;
   if(Phase==PursuitPhase.Dormant){if(d.InsideVilla){approachTime+=Time.deltaTime;if(approachTime>3&&!upstairsSound){upstairsSound=true;d.Audio.HorrorAt("footstep",p.SoundPoint.position,.35f);}if(approachTime>7&&!secondSound){secondSound=true;d.Audio.KnockAt(p.SoundPoint.position,.25f);}}return;}if(Phase==PursuitPhase.Escaped)return;
   Elapsed+=Time.deltaTime;
   if(Caught){if(Elapsed>2.3f){Captures++;ResetEncounter();d.RetryVilla();p.WindowLight.enabled=true;d.Player.RestoreApproach(p.Gate.position,p.Gate.eulerAngles.y);d.Say("Back at the gate. Keep the car close. Hold SHIFT to run.");}return;}
   if(d.Player.InCar){Phase=PursuitPhase.Escaped;Agent.isStopped=true;d.Audio.Pursuit(false);d.Audio.DoorAt(d.Scene.Car.position);d.Say("Lock the doors. Return to the depot.");return;}
   if(Phase==PursuitPhase.Reveal){
    Vector3 to=d.Scene.Walker.transform.position-Agent.transform.position;to.y=0;if(to.sqrMagnitude>.1f)Agent.transform.rotation=Quaternion.RotateTowards(Agent.transform.rotation,Quaternion.LookRotation(to),60*Time.deltaTime);
    if(Elapsed<3.1f)return;Phase=PursuitPhase.Chase;Elapsed=0;Agent.isStopped=false;d.Say("RUN. Get back to your car.");
   }
   Agent.speed=Mathf.Lerp(2.4f,4.7f,Mathf.Clamp01(Elapsed/18));
   repath-=Time.deltaTime;if(repath<=0){repath=.22f;if(NavMesh.SamplePosition(d.Scene.Walker.transform.position,out var goal,2,NavMesh.AllAreas))Agent.SetDestination(goal.position);}
   steps-=Time.deltaTime;if(steps<=0&&Agent.velocity.sqrMagnitude>.2f){steps=.58f;d.Audio.HorrorAt("footstep",Agent.transform.position,.3f);}
   growl-=Time.deltaTime;if(growl<=0){growl=7;d.Audio.HorrorAt("growl",Agent.transform.position,.42f);}
   Vector3 delta=d.Scene.Walker.transform.position-Agent.transform.position;
   bool blocked=Physics.Linecast(Agent.transform.position+Vector3.up,d.Scene.View.transform.position,out var obstruction,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)&&obstruction.collider!=d.Scene.Walker;
   if(delta.magnitude<1.15f&&!blocked)Catch();
  }
  public void Catch(){if(!Active)return;Phase=PursuitPhase.Caught;Elapsed=0;Agent.isStopped=true;d.Audio.Pursuit(false);d.Audio.HorrorAt("reveal",d.Scene.View.transform.position,.48f);}
  void OnDestroy(){if(nav.valid)nav.Remove();}
 }
}

