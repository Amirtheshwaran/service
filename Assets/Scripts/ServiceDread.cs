using UnityEngine;
namespace ServiceGameV2 {
 // V21: the feeling that something is right behind you, in the manner of Fears to Fathom's pursuits - not louder
 // roars, but the picture and the sound coming apart as it closes.
 //  - Threat (0..1) follows the pursuer's distance; while it is behind you it reads higher than in front.
 //  - Picture: camcorder tape damage (ServiceCamcorder.Glitch) - torn bands, a rolling tracking line, slipping colour.
 //  - Sound: the pursuit score (Kevin MacLeod "Anxiety", CC-BY) under a recorded eerie drone (NOX, CC0) that rises
 //    and distorts as it closes; your own panicked breathing (NOX, CC0); its breath placed just behind your head when
 //    it is at your back; and short bursts of tape static with the worst tears.
 //  - The stand-still watcher gets a slow dark bed instead (Kevin MacLeod "Penumbra", CC-BY).
 public sealed class ServiceDread:MonoBehaviour {
  ServiceDirector d;AudioSource drone,panic,watch,tape;AudioDistortionFilter grit;AudioClip[] panicClips,shockClips,breathClips;
  float threat,spike,nextBehind,staticFor;PursuitPhase lastPhase;
  public float Threat=>threat;public float GlitchLevel {get;private set;}public int BehindCues {get;private set;}public int Stings {get;private set;}
  public void Initialize(ServiceDirector director){
   d=director;var cam=d.Scene.View.transform;
   drone=Loop("Dread drone",cam,Resources.Load<AudioClip>("Audio/V21/drone/drone_0"));grit=drone.gameObject.AddComponent<AudioDistortionFilter>();grit.distortionLevel=0;
   panic=Loop("Panicked breathing",cam,null);watch=Loop("Watcher score",cam,Resources.Load<AudioClip>("Audio/V21/dread/Penumbra"));
   tape=Loop("Tape static",cam,Resources.Load<AudioClip>("Audio/V5/static/static_0"));
   panicClips=Resources.LoadAll<AudioClip>("Audio/V21/panic");shockClips=Resources.LoadAll<AudioClip>("Audio/V21/shock");breathClips=Resources.LoadAll<AudioClip>("Audio/V5/breath");
  }
  AudioSource Loop(string name,Transform parent,AudioClip clip){var g=new GameObject(name);g.transform.SetParent(parent,false);var s=g.AddComponent<AudioSource>();s.playOnAwake=false;s.loop=true;s.spatialBlend=0;s.volume=0;s.clip=clip;s.dopplerLevel=0;return s;}
  static void Fade(AudioSource s,float target,float rate){if(!s||!s.clip)return;s.volume=Mathf.MoveTowards(s.volume,target,rate*Time.unscaledDeltaTime);if(s.volume>0.001f&&!s.isPlaying){s.time=Random.Range(0,s.clip.length*.5f);s.Play();}else if(s.volume<=0.001f&&s.isPlaying)s.Stop();}
  void Update(){
   if(!d||d.Horror==null)return;var h=d.Horror;bool playing=d.Phase==ServicePhase.Playing;
   if(!playing){ServiceCamcorder.Glitch=d.Phase==ServicePhase.Paused?0:ServiceCamcorder.Glitch*.9f;Fade(drone,0,2);Fade(panic,0,2);Fade(watch,0,1);Fade(tape,0,4);return;}
   var view=d.Scene.View.transform;float want=0;bool behind=false;float dist=99;
   if(h.Phase!=lastPhase){
    if(h.Phase==PursuitPhase.Reveal){spike=1;Sting(.55f);}
    if(h.Phase==PursuitPhase.Attack){spike=1.4f;}
    lastPhase=h.Phase;
   }
   if((h.Active||h.Caught)&&h.Agent){
    var to=h.Agent.transform.position-view.position;dist=to.magnitude;to.y=0;var f=view.forward;f.y=0;
    behind=Vector3.Dot(f.normalized,to.normalized)<-.15f;
    float near=1-Mathf.Clamp01((dist-1.5f)/(d.Player.InCar?12f:17f));
    want=Mathf.Max(.3f,near*(behind?1:.82f));
    if(h.Caught)want=1;
   }
   threat=Mathf.MoveTowards(threat,want,Time.deltaTime*(want>threat?1.6f:.5f));spike=Mathf.Max(0,spike-Time.deltaTime*1.8f);
   // picture
   float glitch=Mathf.Clamp01(threat*threat*.8f+spike*.6f);if(d.Storm&&!d.Storm.FlashesEnabled)glitch*=.5f;
   GlitchLevel=glitch;ServiceCamcorder.Glitch=glitch;
   // sound
   bool chase=h.Phase==PursuitPhase.Chase&&h.Agent&&d.Property(Mathf.Max(0,h.PropertyIndex)).Encounter==EncounterKind.Pursuit;
   bool watcher=h.Active&&h.PropertyIndex>=0&&d.Property(h.PropertyIndex).Encounter==EncounterKind.LookAway;
   Fade(drone,(h.Active||h.Caught)?Mathf.Lerp(.08f,.42f,threat)*d.Audio.MusicVolume:0,(h.Active?.5f:.35f));
   if(drone.isPlaying){drone.pitch=Mathf.Lerp(1f,.86f,threat);grit.distortionLevel=Mathf.Clamp01((threat-.55f)*1.6f)*.55f;}
   Fade(watch,watcher?.32f*d.Audio.MusicVolume:0,watcher?.25f:.4f);
   // your own breathing: fast and ragged while it is after you, settling after
   if(chase&&panicClips.Length>0&&(!panic.isPlaying||panic.clip==null)){panic.clip=panicClips[Random.Range(0,panicClips.Length)];}
   Fade(panic,chase&&!d.Player.InCar?Mathf.Lerp(.16f,.34f,Mathf.Clamp01(d.Player.HorizontalSpeed/5f)):h.Active?.08f:0,chase?.6f:.12f);
   // tape static rides the worst tears
   staticFor-=Time.deltaTime;if(glitch>.45f&&Random.value<Time.deltaTime*glitch*3)staticFor=Random.Range(.08f,.22f);
   Fade(tape,staticFor>0?.14f*glitch:0,6);
   // something at your back: its breath, just behind your head
   if(chase&&behind&&dist<9&&Time.time>nextBehind&&breathClips.Length>0&&!d.Player.InCar){
    nextBehind=Time.time+Random.Range(2.6f,4.8f);var at=view.position-view.forward*1.1f+Vector3.up*.05f;
    var g=new GameObject("Breath at your back");g.transform.position=at;var s=g.AddComponent<AudioSource>();s.clip=breathClips[Random.Range(0,breathClips.Length)];s.spatialBlend=1;s.rolloffMode=AudioRolloffMode.Linear;s.minDistance=.5f;s.maxDistance=6;s.volume=Mathf.Lerp(.16f,.3f,1-dist/9f);s.dopplerLevel=0;s.Play();Destroy(g,Mathf.Min(s.clip.length,4)+.1f);
    BehindCues++;spike=Mathf.Max(spike,.35f);}
  }
  void Sting(float volume){if(shockClips==null||shockClips.Length==0)return;var clip=shockClips[Random.Range(0,shockClips.Length)];var src=d.Scene.View.GetComponent<AudioSource>();if(!src){src=d.Scene.View.gameObject.AddComponent<AudioSource>();src.spatialBlend=0;src.playOnAwake=false;}src.PlayOneShot(clip,volume);Stings++;}
  public void ResetForShift(){threat=spike=0;ServiceCamcorder.Glitch=0;foreach(var s in new[]{drone,panic,watch,tape})if(s){s.Stop();s.volume=0;}}
 }
}
