using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2 {
 public enum PursuitPhase { Dormant, Reveal, Chase, Caught, Escaped }
 public sealed class ServiceHorror:MonoBehaviour {
  public PursuitPhase Phase {get;private set;}
  public bool Active=>Phase==PursuitPhase.Reveal||Phase==PursuitPhase.Chase;
  public bool Caught=>Phase==PursuitPhase.Caught;
  public float Elapsed {get;private set;}
  public int Captures {get;private set;}
  public NavMeshAgent Agent {get;private set;}
  public int PropertyIndex=>p?p.Index:-1;
  public float LookAwaySeconds {get;private set;}
  public string Headline=>p&&p.Encounter==EncounterKind.LookAway?"DON'T LOOK BACK":Phase==PursuitPhase.Chase?"IT'S FOLLOWING YOU":"";
  public string Instruction=>p&&p.Encounter==EncounterKind.LookAway?"Turn away. Stand completely still until the breathing stops.":Phase==PursuitPhase.Chase?"Run to the car. Keep moving.":p?p.RevealLine:"";
  public string DeathLine=>p?p.DeathLine:"There is no record of your return.";
  ServiceDirector d;ServiceProperty p;NavMeshDataInstance nav;Animator[] animators;
  float repath,steps,growl;Vector3 lastWalker;readonly float[] approach=new float[6];readonly int[] cue=new int[6];
  void LateUpdate(){if(Agent&&animators!=null)foreach(var a in animators)if(a&&a.gameObject.activeInHierarchy){a.SetBool("Moving",Phase==PursuitPhase.Chase&&p.Encounter==EncounterKind.Pursuit&&Agent.velocity.sqrMagnitude>.08f);a.speed=Phase==PursuitPhase.Chase?Mathf.Clamp(Agent.velocity.magnitude/2.2f,.8f,1.8f):1;}}
  public void Initialize(ServiceDirector director){d=director;nav=NavMesh.AddNavMeshData(d.Scene.Navigation);Agent=d.Scene.Entity.GetComponent<NavMeshAgent>();animators=d.Scene.Entity.GetComponentsInChildren<Animator>(true);ResetEncounter();}
  public void ResetEncounter(){Phase=PursuitPhase.Dormant;Elapsed=LookAwaySeconds=0;System.Array.Clear(approach,0,6);System.Array.Clear(cue,0,6);if(Agent&&Agent.isOnNavMesh)Agent.ResetPath();d.Scene.Entity.SetActive(false);d.Audio.Pursuit(false);foreach(var h in d.Scene.Properties){if(h.WindowLight)h.WindowLight.enabled=true;if(h.EncounterLights!=null)foreach(var l in h.EncounterLights)if(l)l.enabled=true;}}
  public void Begin(){Begin(1);}
  public void Begin(int index){
   if(Active||Caught)return;p=d.Property(index);if(!p.HasEncounter)return;
   d.Scene.Entity.transform.SetPositionAndRotation(p.EntitySpawn.position,p.EntitySpawn.rotation);
   if(d.Scene.EntityVariants!=null)for(int i=0;i<d.Scene.EntityVariants.Length;i++)d.Scene.EntityVariants[i].SetActive(i==p.CreatureVariant);
   d.Scene.Entity.SetActive(true);
   if(!NavMesh.SamplePosition(p.EntitySpawn.position,out var hit,2,NavMesh.AllAreas))throw new System.InvalidOperationException("Presence outside navigation at "+index);
   Agent.Warp(hit.position);Agent.isStopped=true;Phase=PursuitPhase.Reveal;Elapsed=repath=steps=LookAwaySeconds=0;growl=5;lastWalker=d.Scene.Walker.transform.position;
   if(p.WindowLight)p.WindowLight.enabled=false;if(p.EncounterLights!=null)foreach(var l in p.EncounterLights)if(l)l.enabled=l.transform.position.y<p.TableApproach.position.y-1;
   d.Scene.Flashlight.enabled=true;d.Audio.HorrorAt(p.Encounter==EncounterKind.LookAway?"breath":"reveal",hit.position,.52f);d.Audio.Pursuit(p.Encounter==EncounterKind.Pursuit);
  }
  void Update(){
   if(d==null||d.Phase!=ServicePhase.Playing)return;
   if(!Active&&!Caught){if(d.Player.InCar)return;foreach(var h in d.Scene.Properties)if(h.HasEncounter&&d.ResultAt(h.Index)==ServiceResult.Pending&&Vector3.Distance(d.Scene.Walker.transform.position,h.Door.position)<18){approach[h.Index]+=Time.deltaTime;if(approach[h.Index]>3&&cue[h.Index]==0){cue[h.Index]++;d.Audio.HorrorAt(h.Index%2==0?"rattle":"breath",h.SoundPoint.position,.16f);}if(approach[h.Index]>11&&cue[h.Index]==1){cue[h.Index]++;d.Audio.HorrorAt("taps",h.SoundPoint.position,.19f);}}return;}
   Elapsed+=Time.deltaTime;
   if(Caught){if(Elapsed>3.8f){Captures++;int index=p.Index;ResetEncounter();d.RetryProperty(index);d.Player.RestoreApproach(p.Gate.position,p.Gate.eulerAngles.y);d.Say("The notice is still in your hand.");}return;}
   if(p.Encounter==EncounterKind.LookAway){
    if(Elapsed<1){lastWalker=d.Scene.Walker.transform.position;return;}Phase=PursuitPhase.Chase;
    float motion=Vector3.Distance(lastWalker,d.Scene.Walker.transform.position);lastWalker=d.Scene.Walker.transform.position;
    if(d.Player.Sprinting&&motion>.001f){Catch();return;}
    float facing=Vector3.Dot(d.Scene.View.transform.forward,(Agent.transform.position+Vector3.up-d.Scene.View.transform.position).normalized);
    LookAwaySeconds=facing<-.15f&&motion<.008f?LookAwaySeconds+Time.deltaTime:0;
    growl-=Time.deltaTime;if(growl<0){growl=4;d.Audio.HorrorAt("breath",Agent.transform.position,.3f);}
    if(LookAwaySeconds>=8){Complete();d.Say("The breathing has stopped. You can leave.");return;}if(Elapsed>28)Catch();return;
   }
   if(d.Player.InCar){Complete();d.Audio.DoorAt(d.Scene.Car.position);d.Say("Keep the doors locked.");return;}
   if(Phase==PursuitPhase.Reveal){var to=d.Scene.Walker.transform.position-Agent.transform.position;to.y=0;if(to.sqrMagnitude>.1f)Agent.transform.rotation=Quaternion.RotateTowards(Agent.transform.rotation,Quaternion.LookRotation(to),80*Time.deltaTime);if(Elapsed<3.1f)return;Phase=PursuitPhase.Chase;Elapsed=0;Agent.isStopped=false;}
   Agent.speed=Mathf.Lerp(3.1f,5.05f,Mathf.Clamp01(Elapsed/30));repath-=Time.deltaTime;if(repath<=0){repath=.24f;if(NavMesh.SamplePosition(d.Scene.Walker.transform.position,out var goal,2,NavMesh.AllAreas))Agent.SetDestination(goal.position);}
   steps-=Time.deltaTime;if(steps<=0&&Agent.velocity.sqrMagnitude>.2f){steps=.46f;d.Audio.HorrorAt("monsterstep",Agent.transform.position,.23f);}growl-=Time.deltaTime;if(growl<=0){growl=7.5f;d.Audio.HorrorAt(p.Index%2==0?"growl":"breath",Agent.transform.position,.36f);}
   var delta=d.Scene.Walker.transform.position-Agent.transform.position;bool blocked=Physics.Linecast(Agent.transform.position+Vector3.up,d.Scene.View.transform.position,out var obstruction,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)&&obstruction.collider!=d.Scene.Walker;if(delta.magnitude<1.1f&&!blocked)Catch();
  }
  void Complete(){Phase=PursuitPhase.Escaped;Agent.isStopped=true;d.Audio.Pursuit(false);d.Audio.SilenceThreat();d.Scene.Entity.SetActive(false);}
  public void Catch(){if(!Active)return;Phase=PursuitPhase.Caught;Elapsed=0;Agent.isStopped=true;d.Audio.Pursuit(false);d.Audio.HorrorAt("gasp",d.Scene.View.transform.position,.5f);}
  void OnDestroy(){if(nav.valid)nav.Remove();}
 }
}
