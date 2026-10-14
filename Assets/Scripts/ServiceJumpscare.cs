using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace ServiceGameV2 {
 // V27 - the jumpscare ("the jumpscare needs to be a jumpscare"; "I got caught and that wasn't scary"; "I want every
 // character to come to your face like a jumpscare with music and background effect"). Whatever has you is put right in
 // your face: its head held in the middle of the view half a metre from your eyes, a little low, lit hard from in front.
 // A layered scream (a recorded cry pitched down, under the creature's own growl and a stinger), a white hit, the view
 // jolting and the tape tearing - then a cut to black and static. Used by every capture, by Walter and by the apparition.
 [DefaultExecutionOrder(10000)] // after everything else that places things this frame
 public sealed class ServiceJumpscare:MonoBehaviour {
  ServiceDirector d;Canvas canvas;Image flash,black;AudioSource cry,sting,under,hiss;Light face;
  Transform fig,head,cam;Light torch;float torchWas,dropBy=.07f;bool human,agentOn;Quaternion shakeBase;
  readonly System.Collections.Generic.List<(Animator,bool)> rootMotion=new System.Collections.Generic.List<(Animator,bool)>();readonly System.Collections.Generic.List<(Rigidbody,bool,RigidbodyInterpolation)> bodies=new System.Collections.Generic.List<(Rigidbody,bool,RigidbodyInterpolation)>();
  void LetGo(){if(agent){agent.enabled=agentOn;agent.updatePosition=agentPos;agent.updateRotation=agentRot;if(agent.enabled&&agent.isOnNavMesh)agent.Warp(fig.position);}foreach(var (an,on) in rootMotion)if(an)an.applyRootMotion=on;rootMotion.Clear();foreach(var (rb,k,i) in bodies)if(rb){rb.isKinematic=k;rb.interpolation=i;}bodies.Clear();}UnityEngine.AI.NavMeshAgent agent;bool agentPos,agentRot;
  Vector3 camPos;Quaternion camRot;float fov,start,length,gap,blackUntil;bool cutting,releaseBlack;
  public bool Playing {get;private set;} public int Plays {get;private set;} public float FaceGap {get;private set;}=99;public float LastPeak {get;private set;}
  public static float Glitch;public string Pin {get;private set;}=""; // last placement, for the tests // read by the camcorder over the dread's own tape damage
  public void Initialize(ServiceDirector director){d=director;
   var o=new GameObject("V27 jumpscare",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));o.transform.SetParent(transform,false);canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=470;
   flash=Plate(o.transform,"Hit",Color.white);black=Plate(o.transform,"Cut",Color.black);
   cry=Src("Scare cry");sting=Src("Scare sting");under=Src("Scare growl");hiss=Src("Scare static");
   face=new GameObject("Scare face light").AddComponent<Light>();face.type=LightType.Spot;face.spotAngle=70;face.innerSpotAngle=30;face.range=3.2f;face.intensity=0;face.color=new Color(1f,.95f,.88f);face.shadows=LightShadows.None;face.enabled=false;}
  Image Plate(Transform parent,string name,Color c){var i=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();i.transform.SetParent(parent,false);var r=i.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(-50,-50);r.offsetMax=new Vector2(50,50);c.a=0;i.color=c;i.raycastTarget=false;return i;}
  AudioSource Src(string name){var s=new GameObject(name).AddComponent<AudioSource>();s.transform.SetParent(transform,false);s.playOnAwake=false;s.spatialBlend=0;s.loop=false;return s;}
  // the head of a rig: the humanoid head bone, else a bone called head
  static Transform Top(Transform t){while(t.parent&&t.parent.parent)t=t.parent;return t;}
  public static Transform HeadOf(Transform root){if(!root)return null;foreach(var an in root.GetComponentsInChildren<Animator>(true)){if(an.isHuman&&an.avatar){var h=an.GetBoneTransform(HumanBodyBones.Head);if(h)return h;}}
   foreach(var t in root.GetComponentsInChildren<Transform>(true)){var n=t.name.ToLowerInvariant();if(n=="head"||n.EndsWith(":head")||n.EndsWith("_head")||n.EndsWith(".head")||n=="bip001 head"||n.EndsWith(" head"))return t;}return null;}
  // voice: the cry pool ("scream" for her, "roar" for the creatures and Walter); pitch below 1 is deeper and slower
  public void Play(Transform figure,Transform headBone,string voice,float pitch,float seconds=1.1f,float holdBlack=.6f,float faceGap=.5f,float drop=.07f){dropBy=drop;
   if(!figure)return;if(Playing)Finish(false);
   fig=figure;head=headBone?headBone:figure;cam=d.Scene.View.transform;agent=fig.GetComponent<UnityEngine.AI.NavMeshAgent>();
   if(agent){agentPos=agent.updatePosition;agentRot=agent.updateRotation;agentOn=agent.enabled;agent.enabled=false;}
   // nothing else may hold it where it was: root motion and any physics body let go for the scare
   rootMotion.Clear();foreach(var an in fig.GetComponentsInChildren<Animator>()){rootMotion.Add((an,an.applyRootMotion));an.applyRootMotion=false;}
   bodies.Clear();foreach(var rb in fig.GetComponentsInChildren<Rigidbody>()){bodies.Add((rb,rb.isKinematic,rb.interpolation));rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.None;}
   camPos=cam.localPosition;camRot=cam.localRotation;
   // turn to where it actually is (in your face from there - straight ahead could be inside a wall)
   {var to=head.position-cam.position;if(to.sqrMagnitude>.04f){var flat=to;flat.y=0;var look=Quaternion.LookRotation(flat.sqrMagnitude>.01f?Vector3.Lerp(flat.normalized,to.normalized,.4f):to.normalized,Vector3.up);cam.rotation=look;}}
   shakeBase=cam.localRotation;fov=d.Scene.View.fieldOfView;start=Time.time;length=seconds;gap=faceGap;blackUntil=holdBlack;cutting=false;releaseBlack=true;
   Playing=true;Plays++;FaceGap=99;human=false;foreach(var an in fig.GetComponentsInChildren<Animator>(true))if(an.isHuman&&an.avatar)human=true;
   face.transform.SetParent(cam,false);face.transform.localPosition=new Vector3(0,-.12f,.05f);face.transform.localRotation=Quaternion.Euler(-6,0,0);face.enabled=true;face.intensity=2.4f;
   var c=d.Audio.Pool(voice);if(c){cry.clip=c;cry.pitch=pitch;cry.volume=1;cry.Play();}
   var s=d.Audio.Pool("reveal");if(s){sting.pitch=1;sting.PlayOneShot(s,1f);}var sh=d.Audio.Pool("shock");if(sh)sting.PlayOneShot(sh,.9f);
   var g=d.Audio.Pool("growl");if(g){under.clip=g;under.pitch=pitch*.92f;under.volume=.95f;under.Play();}
   flash.color=new Color(1,1,1,.55f);black.color=new Color(0,0,0,0);Glitch=.9f;
   // the torch is too strong at half a metre (it washed the face out to white): down to a glow while it is on you
   torch=d.Scene.Flashlight;torchWas=torch?torch.intensity:0;if(torch)torch.intensity=torchWas*.12f;
   StartCoroutine(Run());}
  IEnumerator Run(){
   while(Playing&&Time.time-start<length)yield return null;
   if(!Playing)yield break;
   // the cut: black, a burst of tape static, the scream cut off mid-breath
   cutting=true;black.color=Color.black;cry.Stop();under.Stop();var st=d.Audio.Pool("static");if(st){hiss.PlayOneShot(st,.85f);}Glitch=0;
   RestoreView();face.enabled=false;if(torch)torch.intensity=torchWas;
   LetGo();
   float t=0;while(t<blackUntil){t+=Time.unscaledDeltaTime;yield return null;}
   Playing=false;
   if(releaseBlack){float a=1;while(a>0){a=Mathf.MoveTowards(a,0,Time.unscaledDeltaTime/.35f);black.color=new Color(0,0,0,a);yield return null;}}
  }
  // the caller keeps the screen black (an ending, a capture card) and lets it go later
  public void HoldBlack(){releaseBlack=false;}
  public void ReleaseBlack(){StartCoroutine(Fade());}
  IEnumerator Fade(){float a=black.color.a;while(a>0){a=Mathf.MoveTowards(a,0,Time.unscaledDeltaTime/.35f);black.color=new Color(0,0,0,a);yield return null;}}
  public bool Black=>black&&black.color.a>.5f;
  void RestoreView(){if(cam){cam.localPosition=camPos;cam.localRotation=camRot;}if(d&&d.Scene.View)d.Scene.View.fieldOfView=fov;}
  public void Finish(bool keepBlack){if(!Playing)return;StopAllCoroutines();Playing=false;cutting=false;cry.Stop();under.Stop();Glitch=0;RestoreView();face.enabled=false;if(torch&&Playing==false&&torchWas>0)torch.intensity=torchWas;flash.color=new Color(1,1,1,0);if(!keepBlack)black.color=new Color(0,0,0,0);
   LetGo();}
  public void ResetAll(){Finish(false);black.color=new Color(0,0,0,0);}
  void LateUpdate(){
   if(!Playing||cutting||!fig||!cam)return;
   float t=Time.time-start;
   // the view: a hard jolt that settles a little, a tight punch in, a roll
   float amp=Mathf.Lerp(.055f,.02f,Mathf.Clamp01(t/length));
   cam.localPosition=camPos+new Vector3(Random.Range(-amp,amp),Random.Range(-amp,amp),Random.Range(-amp*.4f,amp*.4f));
   cam.localRotation=shakeBase*Quaternion.Euler(Random.Range(-1.6f,1.6f),Random.Range(-1.6f,1.6f),Random.Range(-4.5f,4.5f)*Mathf.Clamp01(1.2f-t));
   d.Scene.View.fieldOfView=Mathf.Lerp(fov,fov*.8f,Mathf.Clamp01(t/.08f));
   // its head in the middle of your view, in from wherever it was over the first moment
   var to=cam.position-fig.position;to.y=0;if(to.sqrMagnitude>.0001f)fig.rotation=Quaternion.LookRotation(to.normalized);
   var want=cam.position+cam.forward*gap-cam.up*dropBy;var mover=head.IsChildOf(fig)?fig:Top(head); /* a creature's head bone sits at the base of its skull: its face goes in the middle */var delta=want-head.position;float pull=Mathf.Clamp01(t/.07f);mover.position+=delta*(t<.07f?pull*.6f:1f);
   FaceGap=Vector3.Distance(head.position,cam.position);Pin=$"fig {fig.name} at {fig.position:F2} head {head.name} at {head.position:F2} want {want:F2} agent {(agent?agent.enabled+"/"+agent.updatePosition:"none")} t {t:F2} under fig {head.IsChildOf(fig)} mover {mover.name}";
   // the white hit fades; the face light stutters
   flash.color=new Color(1,1,1,Mathf.Max(0,.55f-t*6f));face.intensity=t<.08f?3.2f:Random.value<.15f?.5f:2.1f;Glitch=t<.15f?.9f:.42f+Random.value*.18f;
   LastPeak=Mathf.Max(LastPeak,cry.isPlaying?cry.volume:0);
  }
 }
}
