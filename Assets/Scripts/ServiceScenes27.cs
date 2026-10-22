using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace ServiceGameV2 {
 // V27 - the playtesters' fourth round, as scenes:
 //  - the story: your sister Sofia is dying in St. Brigid's; the hospital pages you through the nights ("lost in the
 //    story, thrown into the middle of something without any context")
 //  - Harrow's (236), night two: footsteps come up behind you first, then the breathing ("no idea what triggered that,
 //    felt like out of nowhere - a bit more buildup")
 //  - Vale's (77), night one: leave the front door open and something comes in after you ("add a way to die, like if
 //    you don't close the door")
 //  - Bell's (91), night two: at the foot of his stairs it stands on the landing and you cannot move, the house goes
 //    gloomy; it goes, and his book comes down the stairs to your feet - "don't come back" in his hand ("when the monster
 //    appears you will be stun-locked... after it leaves the book comes down to his feet... don't come again or you die")
 //  - 1 County Route 9, night three: the house you grew up in, and the ending - the jumpscare ("add a Fears to Fathom
 //    like jumpscare at the end of the game... and I mean a really scary one")
 [DefaultExecutionOrder(10)] // V28b (audit): after the director, so the E that puts a page down never reopens it
 public sealed class ServiceScenes27:MonoBehaviour {
  ServiceDirector d;Canvas canvas;Image gloom,pagePaper,pageShade;Text pageText,pageHint;Font hand;
  public static bool RulesInTests; // tests switch the Vale door rule on deliberately
  public int Pages {get;private set;} public int Freezes {get;private set;} public int DoorDeaths {get;private set;} public int DoorWarnings {get;private set;} public int Finales {get;private set;} public int Buildups {get;private set;}
  public bool PageOpen {get;private set;} public bool Frozen {get;private set;} public bool FinaleRunning {get;private set;} public string LastPager {get;private set;}="";
  float valeOpenFor;bool valeWarned,valeKnocked,valeCame,valeOutside,freezeDone,pagerShown;float gloomTarget;public int ValeOutsides {get;private set;}GameObject borrowed;public int Summons {get;private set;}
  bool bellWarned,bellLeft,bellKept;public bool BellKept=>bellKept;
  // V28b: caught at Bell's on night two (his door, his stairs): that was the night's chase - the walk back in to leave the
  // papers again is not "coming back", and his door does not bang again
  public void BellCaught(){bellKept=true;bellLeft=false;}public int BellReturns {get;private set;}
  public void Initialize(ServiceDirector director){d=director;hand=Resources.Load<Font>("Fonts/Notes/Kalam-Regular");
   var o=new GameObject("V27 scenes",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));o.transform.SetParent(transform,false);canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=75;
   var sc=o.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1280,720);sc.matchWidthOrHeight=1;
   gloom=Img(o.transform,"Gloom",new Color(0,0,0,0),true);pageShade=Img(o.transform,"Page shade",new Color(0,0,0,0),true);
   pagePaper=Img(o.transform,"Page",new Color(.87f,.83f,.72f,0),false);var pr=pagePaper.rectTransform;pr.sizeDelta=new Vector2(400,520);pr.localRotation=Quaternion.Euler(0,0,-1.8f);
   pageText=new GameObject("Page words",typeof(RectTransform),typeof(Text)).GetComponent<Text>();pageText.transform.SetParent(pagePaper.transform,false);var tr=pageText.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(34,34);tr.offsetMax=new Vector2(-30,-30);
   pageText.font=hand?hand:Resources.Load<Font>("Fonts/CourierPrime-Regular");pageText.fontSize=34;pageText.color=new Color(.16f,.11f,.09f,0);pageText.alignment=TextAnchor.UpperLeft;pageText.resizeTextForBestFit=true;pageText.resizeTextMaxSize=36;pageText.resizeTextMinSize=18;
   pageHint=new GameObject("Page hint",typeof(RectTransform),typeof(Text)).GetComponent<Text>();pageHint.transform.SetParent(o.transform,false);var hr=pageHint.rectTransform;hr.anchorMin=hr.anchorMax=new Vector2(.5f,0);hr.anchoredPosition=new Vector2(0,40);hr.sizeDelta=new Vector2(800,30);
   pageHint.font=Resources.Load<Font>("Fonts/CourierPrime-Regular");typedFont=pageHint.font;pageHint.fontSize=16;pageHint.alignment=TextAnchor.MiddleCenter;pageHint.color=new Color(1,1,1,0);pageHint.text="E   PUT IT DOWN";}
  Image Img(Transform parent,string name,Color c,bool full){var i=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();i.transform.SetParent(parent,false);if(full){var r=i.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(-60,-60);r.offsetMax=new Vector2(60,60);}i.color=c;i.raycastTarget=false;return i;}
  // ---- V28 story: papers you can read (ServiceV28Readables places them; each shows on its own night)
  sealed class Readable{public string key,prompt,text,thought;public int night;public bool typed;public Transform t;public bool read;}
  readonly System.Collections.Generic.List<Readable> readables=new System.Collections.Generic.List<Readable>();Readable reading;bool pageHolds=true;
  public int ReadablesRead {get;private set;} public string LastReadable {get;private set;}="";
  static Readable Def(string key){switch(key){
   case "harrow-photo":return new Readable{key=key,night=0,typed=false,prompt="E   Look behind the photograph",text=ServiceScript.ReadHarrowPhoto,thought=ServiceScript.ThoughtHarrowPhoto};
   case "vale-letter":return new Readable{key=key,night=1,typed=false,prompt="E   Read the letter",text=ServiceScript.ReadValeLetter,thought=ServiceScript.ThoughtValeLetter};
   case "morrow-bulletin":return new Readable{key=key,night=1,typed=true,prompt="E   Read the bulletin",text=ServiceScript.ReadMorrowBulletin,thought=ServiceScript.ThoughtMorrowBulletin};
   case "vale-clipping":return new Readable{key=key,night=2,typed=true,prompt="E   Read the clipping",text=ServiceScript.ReadValeClipping,thought=ServiceScript.ThoughtValeClipping};
   default:return null;}}
  void FindReadables(){if(readables.Count>0||!d||!d.Scene)return;foreach(var t in d.Scene.GetComponentsInChildren<Transform>(true)){if(!t.name.StartsWith("V28 readable"))continue;int k=t.name.IndexOf("— ");if(k<0)continue;var r=Def(t.name.Substring(k+2).Trim());if(r==null)continue;r.t=t;readables.Add(r);}}
  void ShowReadables(){FindReadables();foreach(var r in readables){r.read=false;if(r.t&&r.t.GetComponent<Renderer>())r.t.gameObject.SetActive(r.night==d.NightIndex);}}
  Readable NearReadable(){if(!d||PageOpen||Frozen||d.Busy||d.Player.InCar||d.Horror.Active||d.Horror.Caught||FinaleRunning)return null;var cam=d.Scene.View.transform;Readable best=null;float bd=9;
   foreach(var r in readables){if(!r.t||r.night!=d.NightIndex||!r.t.gameObject.activeInHierarchy&&r.t.GetComponent<Renderer>())continue;var to=r.t.position-cam.position;float dist=to.magnitude;if(dist>(r.key=="harrow-photo"?2.6f:2.1f)||dist>=bd)continue;
    if(Vector3.Dot(cam.forward,to.normalized)<.82f)continue;if(!ServiceInteraction.Clear(cam.position,r.t.position+Vector3.up*.02f,r.key=="harrow-photo"?r.t.parent:r.t,d.Scene.Walker.transform))continue;
    {var hp=r.t.GetComponentInParent<ServiceProperty>();if(hp&&!(ServiceLife.Indoors(hp,d.Scene.Walker.transform.position)&&ServiceLife.UnderRoof(d.Scene.Walker.transform.position)))continue;} // V28b (audit): not through her front wall from the porch
    best=r;bd=dist;}return best;}
  public float ReadAim{get{var cam=d.Scene.View.transform;var b=d.Books?d.Books.StairBook:null;Vector3? at=BookInReach&&b?b.position:NearReadable() is Readable r?r.t.position:(Vector3?)null;return at.HasValue?Vector3.Dot(cam.forward,(at.Value-cam.position).normalized):-2f;}}
  public string ReadPrompt=>BookInReach?"E   Read the book":NearReadable() is Readable r?r.prompt:"";
  public int ClosedFrame {get;private set;}float pageMinHold=.35f;
  void OpenText(string text,bool typed,bool hold){PageOpen=true;pageHolds=hold;pageAt=Time.unscaledTime;pageMinHold=hold?.35f:1.2f;if(hold)d.Hold(true);d.Audio.Paper();pageText.font=typed?(typedFont?typedFont:pageText.font):(hand?hand:pageText.font);pageText.fontSize=typed?28:34;pageText.resizeTextMaxSize=typed?30:36;pageText.text=text;
   pageShade.color=new Color(0,0,0,.62f);pagePaper.color=typed?new Color(.90f,.88f,.80f,1):new Color(.87f,.83f,.72f,1);pageText.color=new Color(.18f,.1f,.08f,1);pageHint.color=new Color(1,1,1,hold?.7f:0);}
  Font typedFont;
  public void ResetForShift(){StopAllCoroutines();CancelOutside();reading=null;valeOpenFor=0;valeWarned=valeKnocked=valeCame=valeOutside=freezeDone=pagerShown=false;bellWarned=bellLeft=bellKept=false;Frozen=FinaleRunning=false;ClosePage();ShowReadables();gloomTarget=0;ServiceJumpscare.Glitch=0; /* V28b (review): a freeze cut short by quitting no longer leaves its tape glitch on */if(gloom)gloom.color=new Color(0,0,0,0);
   if(d&&d.Audio)d.Audio.Hush=false;if(d&&d.Scene.Flashlight)d.Scene.Flashlight.intensity=d.TorchExposure?ServiceTorchExposure.Throw:d.Scene.Flashlight.intensity;}
  void Update(){
   if(!d)return;if(gloom)gloom.color=new Color(0,0,0,Mathf.MoveTowards(gloom.color.a,gloomTarget,Time.unscaledDeltaTime*1.4f));
   if(canvas)canvas.enabled=d.Phase!=ServicePhase.Paused; // V28b (review): the pause menu is not drawn under an open page
   if(PageOpen){if(d.Phase==ServicePhase.Paused)return;if(d.Phase!=ServicePhase.Playing){ClosePage();return;}var k=Keyboard.current;var m=Mouse.current;if(Time.unscaledTime-pageAt>pageMinHold&&d.ResumedFrame!=Time.frameCount&&(k!=null&&(k.eKey.wasPressedThisFrame||k.escapeKey.wasPressedThisFrame)||m!=null&&m.leftButton.wasPressedThisFrame))PutDown();return;}
   if(d.Phase!=ServicePhase.Playing)return;
   if(!pagerShown&&d.ShiftCardTime>9f&&d.Player.InCar&&!(d.Timecard&&d.Timecard.Blocking)&&!d.PaperOpen&&d.DepotIntroDone&&string.IsNullOrEmpty(d.Notice)&&!(d.Blink&&d.Blink.Running)){ /* V28b (review): after the clipboard and the depot line, not under or over them */pagerShown=true;StartCoroutine(Pager(d.NightIndex));}
   ValeDoor();BellReturn();}
  // ---- the hospital, on the pager (one page at the start of each night, the last in the last house)
  public static string Clock12(System.DateTime t)=>t.ToString("h:mm tt",System.Globalization.CultureInfo.InvariantCulture);
  IEnumerator Pager(int night){var line=new[]{ServiceScript.Pager0,ServiceScript.Pager1,ServiceScript.Pager2}[Mathf.Clamp(night,0,2)];var thought=new[]{ServiceScript.PagerThought0,ServiceScript.PagerThought1,ServiceScript.PagerThought2}[Mathf.Clamp(night,0,2)];
   d.Audio.HorrorAt("pager",d.Scene.View.transform.position+d.Scene.View.transform.right*.3f-Vector3.up*.3f,.5f);yield return new WaitForSeconds(.6f);{float w0=0;while(!string.IsNullOrEmpty(d.Notice)&&w0<12f){w0+=Time.deltaTime;yield return null;}}LastPager=line;d.Say(line);
   yield return new WaitForSeconds(7.5f);{float w=0;while(!string.IsNullOrEmpty(d.Notice)&&d.Notice!=line&&w<10f){w+=Time.deltaTime;yield return null;}}d.Say(thought);}
  // ---- Harrow's, night two: footsteps behind you before the breathing
  public IEnumerator HarrowBuildup(ServiceProperty p){Buildups++;var cam=d.Scene.View.transform;var lamps=p.GetComponentsInChildren<Light>().Where(l=>l.enabled&&Vector3.Distance(l.transform.position,p.TableApproach.position)<6f).ToList();
   foreach(var on in new[]{false,true,false,true,false}){foreach(var l in lamps)l.enabled=on;if(!on)d.Audio.HorrorAt("lightsout",p.TableApproach.position+Vector3.up,.25f);yield return new WaitForSeconds(on?.12f:.2f);}
   yield return new WaitForSeconds(.7f);
   foreach(var dist in new[]{7f,5.2f,3.6f,2.4f}){var back=-cam.forward;back.y=0;back.Normalize();d.Audio.HorrorAt("floorstep",cam.position+back*dist-Vector3.up*1.4f,Mathf.Lerp(.55f,.3f,dist/7f),.82f);yield return new WaitForSeconds(.78f);}
   d.Say(ServiceScript.HarrowSteps);d.Audio.HorrorAt("woodstress",cam.position-cam.forward*1.6f-Vector3.up*1.2f,.3f);yield return new WaitForSeconds(1.1f);}
  // ---- Vale's: shut the door behind you. V28: the rule holds on night two as well, quicker (the note says so again), and
  // the door can be shut from her hall. Night two, door shut and the papers down: it knocks on the outside of her door,
  // and when you open it to leave it is standing on her walk - night one it tries the open door, night two it waits
  // outside the shut one, night three it is in the hall (the chase).
  void ValeDoor(){
   if(d.NightIndex>1||valeCame||d.Player.InCar||d.Horror.Active||d.Horror.Caught||d.Busy||(d.IsSmoke&&!RulesInTests))return;var p=d.Property(1);if(!p)return;
   float warnAt=d.NightIndex==0?8f:5f,growlAt=d.NightIndex==0?15.5f:10f,comeAt=d.NightIndex==0?21f:14f;
   var w=d.Scene.Walker.transform.position;bool inside=ServiceLife.Indoors(p,w)&&ServiceLife.UnderRoof(w);float open=d.Life?d.Life.DoorOpenDegrees(1):0;
   if(!inside){valeOpenFor=Mathf.Max(0,valeOpenFor-Time.deltaTime*2);return;}
   if(open<6f){if(valeWarned&&!valeKnocked){valeKnocked=true;StartCoroutine(Knocked(p));}
    else if(d.NightIndex==1&&!valeOutside&&(!valeKnocked||Time.time>knockedEnd+1.5f)&&d.ResultAt(1)!=ServiceResult.Pending&&Vector3.Distance(w,p.Door.position)<7f){valeOutside=true;StartCoroutine(Outside(p));}
    valeOpenFor=0;return;}
   valeOpenFor+=Time.deltaTime;
   if(!valeWarned&&valeOpenFor>warnAt){valeWarned=true;DoorWarnings++;d.Audio.HorrorAt("howl",p.Door.position+(p.Door.position-p.InteriorBounds.center).normalized*3f,.3f);d.Audio.HorrorAt("woodstress",p.Door.position,.35f);d.Say(ServiceScript.ValeDoorWarn);}
   if(valeWarned&&valeOpenFor>growlAt&&valeOpenFor-Time.deltaTime<=growlAt){var o=(p.Door.position-p.InteriorBounds.center);o.y=0;o.Normalize();d.Audio.HorrorAt("growl",p.Door.position+o*2.5f,.45f);d.Audio.MonsterFootstep(p.Door.position+o*1.5f);}
   if(valeOpenFor>comeAt){valeCame=true;DoorDeaths++;d.Horror.BeginAt(1,p.OpeningCentre+p.Inward*.9f);}}
  IEnumerator Outside(ServiceProperty p){ValeOutsides++;var o=-p.Inward;o.y=0;o.Normalize();yield return new WaitForSeconds(1.2f);
   for(int k=0;k<3;k++){d.Audio.KnockAt(p.Door.position+o*.25f,.7f);yield return new WaitForSeconds(.95f);}
   d.Audio.HorrorAt("metalrattle",p.Door.position+o*.2f,.4f);yield return new WaitForSeconds(.6f);d.Narrate(ServiceScript.ValeKnocksOutside);
   float until=Time.time+75f;
   while(true){if(Time.time>until||d.Horror.Active||d.Horror.Caught||d.Phase!=ServicePhase.Playing)yield break;var w=d.Scene.Walker.transform.position;
    if((d.Life&&d.Life.DoorOpenDegrees(1)>25f)||(!ServiceLife.Indoors(p,w)&&Vector3.Distance(w,p.Door.position)<16f))break;yield return null;}
   var ent=d.Scene.Entity;if(!ent)yield break;var at=p.OpeningCentre+o*5.5f;if(UnityEngine.AI.NavMesh.SamplePosition(at,out var nh,2.5f,UnityEngine.AI.NavMesh.AllAreas))at=nh.position;
   var agent=ent.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
   if(d.Scene.EntityVariants!=null)for(int i=0;i<d.Scene.EntityVariants.Length;i++)d.Scene.EntityVariants[i].SetActive(i==Mathf.Clamp(p.CreatureVariant,0,d.Scene.EntityVariants.Length-1));
   var face=p.OpeningCentre-at;face.y=0;ent.transform.SetPositionAndRotation(at,Quaternion.LookRotation(face.sqrMagnitude>.01f?face:Vector3.forward));ent.SetActive(true);borrowed=ent;
   foreach(var a in ent.GetComponentsInChildren<Animator>())if(a&&a.isActiveAndEnabled){a.SetBool("Moving",false);a.speed=.6f;}
   d.Audio.HorrorAt("breath",at+Vector3.up*1.5f,.4f);if(d.Dread)d.Dread.Pulse(.6f);
   float seen=0,t=0;var cam=d.Scene.View;while(t<2.6f&&seen<.45f){t+=Time.deltaTime;var vp=cam.WorldToViewportPoint(at+Vector3.up*1.4f);if(vp.z>0&&vp.x>.1f&&vp.x<.9f&&vp.y>.1f&&vp.y<.95f&&ServiceInteraction.Clear(cam.transform.position,at+Vector3.up*1.4f,null,d.Scene.Walker.transform))seen+=Time.deltaTime;
    if(d.Horror.Active||d.Horror.Caught){CancelOutside();yield break;}yield return null;}
   var torch=d.Scene.Flashlight;bool was=torch&&torch.enabled;if(torch)torch.enabled=false;yield return new WaitForSeconds(.3f);CancelOutside();if(torch)torch.enabled=was;
   yield return new WaitForSeconds(.8f);d.Say(ServiceScript.ValeOutsideSeen);}
  public bool OutsideShowing=>borrowed;
  public void CancelOutside(){if(!borrowed)return;borrowed.SetActive(false);var agent=borrowed.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=true;borrowed=null;}
  float knockedEnd=1e9f;
  IEnumerator Knocked(ServiceProperty p){knockedEnd=Time.time+4.5f;yield return new WaitForSeconds(1.6f);var o=(p.Door.position-p.InteriorBounds.center);o.y=0;o.Normalize();d.Audio.HorrorAt("doorslam",p.Door.position+o*.4f,.6f);yield return new WaitForSeconds(.5f);d.Audio.HorrorAt("taps",p.Door.position+o*.4f,.35f);d.Narrate(ServiceScript.ValeDoorShut);}
  // ---- Bell's, night two (V28: "I ran, then came back to sneak a peek and searched the whole house - no monster. It would
  // have been good if it came here"): his page says don't come back. Leave the house after it, come back in, and it is on
  // the stairs - and it comes for you (one chase a night: his door does not bang open for you after that).
  void BellReturn(){if(!(bellWarned||(freezeDone&&d.Books&&d.Books.StairFalls>0))||bellKept||d.NightIndex!=1||d.Player.InCar||d.Horror.Active||d.Horror.Caught||d.Busy||Frozen||PageOpen||(d.IsSmoke&&!RulesInTests))return;
   var p=d.Property(4);if(!p)return;var w=d.Scene.Walker.transform.position;
   if(!bellLeft){var o=w-p.Door.position;o.y=0;if(!ServiceLife.Indoors(p,w)&&o.magnitude>6f)bellLeft=true;return;}
   if(!(ServiceLife.Indoors(p,w)&&ServiceLife.UnderRoof(w))||Vector3.Dot(w-p.OpeningCentre,p.Inward)<1.5f)return;
   bellKept=true;BellReturns++;d.Horror.DisarmReturnAmbush();StartCoroutine(BellKeeps(p));}
  IEnumerator BellKeeps(ServiceProperty p){gloomTarget=.3f;d.Audio.HorrorAt("lightsout",d.Scene.View.transform.position,.35f);d.Say(ServiceScript.BellCameBack);
   var top=d.Books&&d.Books.StairKnown?d.Books.StairTop:p.EntitySpawn.position;d.Audio.HorrorAt("floorstep",top+Vector3.up,.5f,.8f);yield return new WaitForSeconds(1.1f);
   if(d.Horror.Active||d.Horror.Caught||d.Phase!=ServicePhase.Playing){gloomTarget=0;yield break;}
   var spawn=UnityEngine.AI.NavMesh.SamplePosition(top,out var h,1.5f,UnityEngine.AI.NavMesh.AllAreas)?h.position:p.EntitySpawn.position;d.Horror.BeginAt(4,spawn,true);
   yield return new WaitForSeconds(4f);gloomTarget=0;}
  // ---- Bell's, night two: it stands on the landing; you cannot move; then the book
  public bool Freezing=>Frozen;
  public void BellFreeze(ServiceProperty p){if(freezeDone)return;freezeDone=true;StartCoroutine(Freeze(p));}
  IEnumerator Freeze(ServiceProperty p){Freezes++;Frozen=true;d.Hold(true);var cam=d.Scene.View.transform;var torch=d.Scene.Flashlight;
   var lamps=p.GetComponentsInChildren<Light>().Where(l=>l.enabled).ToList();foreach(var l in lamps)l.enabled=false;d.Audio.HorrorAt("lightsout",cam.position,.35f);
   if(torch){torch.enabled=false;}yield return new WaitForSeconds(.5f);
   // it is on the landing when the torch comes back
   var e=d.Scene.Entity;var agent=e.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
   if(d.Scene.EntityVariants!=null)for(int i=0;i<d.Scene.EntityVariants.Length;i++)d.Scene.EntityVariants[i].SetActive(i==p.CreatureVariant);
   var top=d.Books.StairTop;var face=cam.position-top;face.y=0;e.transform.SetPositionAndRotation(top,Quaternion.LookRotation(face.sqrMagnitude>.01f?face.normalized:Vector3.forward));e.SetActive(true);
   foreach(var a in e.GetComponentsInChildren<Animator>())if(a.gameObject.activeInHierarchy){a.SetBool("Moving",false);a.speed=.6f;}
   if(torch){torch.enabled=true;}d.Audio.HorrorAt("breath",top+Vector3.up*1.5f,.55f,.8f);if(d.Dread)d.Dread.Pulse(.9f);gloomTarget=.42f;ServiceJumpscare.Glitch=.35f;
   float t=0;bool said=false;while(t<4.6f){t+=Time.deltaTime;d.Player.FocusOn(top+Vector3.up*1.45f,1-Mathf.Exp(-Time.deltaTime*3f));if(!said&&t>1f){said=true;d.Say(ServiceScript.BellFreeze);}
    if(torch)torch.intensity=ServiceTorchExposure.Throw*(Random.value<.08f?.15f:1f);yield return null;}
   // it goes: the torch stutters out, and when it comes back the landing is empty
   if(torch)torch.enabled=false;d.Audio.HorrorAt("floorstep",top+Vector3.up*2.4f,.4f,.8f);yield return new WaitForSeconds(.45f);e.SetActive(false);if(agent)agent.enabled=true;
   yield return new WaitForSeconds(.35f);if(torch){torch.enabled=true;torch.intensity=ServiceTorchExposure.Throw;}ServiceJumpscare.Glitch=0;gloomTarget=.18f;
   yield return new WaitForSeconds(.8f);d.Hold(false);Frozen=false;
   d.Books.BeginStairFall(p);
   yield return new WaitForSeconds(6f);gloomTarget=0;}
  // the book at your feet - "E Read the book"
  bool BookInReach{get{var b=d.Books?d.Books.StairBook:null;if(!b||PageOpen||Frozen||d.Busy||d.Player.InCar||d.Horror.Active)return false;var to=b.position-d.Scene.View.transform.position;return to.magnitude<2.4f&&Vector3.Dot(d.Scene.View.transform.forward,to.normalized)>.8f;}}
  public bool PagerShown=>pagerShown;
  public bool CanReadPage=>BookInReach||NearReadable()!=null;
  float pageAt;
  public void ReadPage(){if(BookInReach){Pages++;bellWarned=true;reading=null;OpenText(ServiceScript.BellPage,false,true);return;}
   var r=NearReadable();if(r==null)return;reading=r;ReadablesRead++;LastReadable=r.key;OpenText(r.text,r.typed,true);}
  public void SmokeClosePage(){ClosePage();}
  // put the page down as the player does (E): the thought after it, the first time
  public void PutDown(){if(!PageOpen)return;var was=reading;ClosePage();if(was!=null){if(!was.read){was.read=true;d.Say(was.thought);}}else if(pageHolds)d.Say(d.ResultAt(4)==ServiceResult.Pending?ServiceScript.BellPageThoughtPending:ServiceScript.BellPageThought);}
  void ClosePage(){if(!PageOpen)return;PageOpen=false;ClosedFrame=Time.frameCount;if(pageHolds)d.Hold(false);reading=null;if(d.Audio)d.Audio.Paper();pageShade.color=new Color(0,0,0,0);pagePaper.color=new Color(.87f,.83f,.72f,0);pageText.color=new Color(.16f,.11f,.09f,0);pageHint.color=new Color(1,1,1,0);}
  // ---- 1 County Route 9: the last house
  public void Finale(ServiceProperty p){if(FinaleRunning)return;StartCoroutine(FinaleRun(p));}
  IEnumerator FinaleRun(ServiceProperty p){FinaleRunning=true;Finales++;d.Hold(true);var cam=d.Scene.View.transform;var torch=d.Scene.Flashlight;
   var clock=d.Timecard?d.Timecard.Clock:System.DateTime.Now;string now=Clock12(clock);int day=clock.Day;string date=clock.ToString("MMMM",System.Globalization.CultureInfo.InvariantCulture)+" "+day+(day%10==1&&day!=11?"st":day%10==2&&day!=12?"nd":day%10==3&&day!=13?"rd":"th");
   yield return new WaitForSeconds(1.3f);d.Say(ServiceScript.FinaleTable);yield return new WaitForSeconds(3.4f);
   // V28: the post is a county summons - the whole road, in order, and last of all the Lowells (read it, or it closes itself)
   reading=null;OpenText(ServiceScript.FinaleSummons,true,false);pageHint.color=new Color(1,1,1,.6f);Summons++;{float until=Time.unscaledTime+(d.IsSmoke&&!d.IsTour?2.5f:Mathf.Clamp(1.4f+ServiceScript.FinaleSummons.Length*.072f,12f,30f)); /* V28b (review): at the game's reading pace, not 11 s */while(PageOpen&&Time.unscaledTime<until){if(d.Phase==ServicePhase.Paused||d.ResumedFrame==Time.frameCount){until+=Time.unscaledDeltaTime;pageAt+=Time.unscaledDeltaTime;yield return null;continue;} /* V28b (review): paused, the summons waits */var k=Keyboard.current;if(Time.unscaledTime-pageAt>1.2f&&k!=null&&(k.eKey.wasPressedThisFrame||k.escapeKey.wasPressedThisFrame))break;yield return null;}}
   ClosePage();d.Say(ServiceScript.FinaleSummonsThought);yield return new WaitForSeconds(3.2f);
   d.Audio.HorrorAt("pager",cam.position+cam.right*.3f-Vector3.up*.3f,.55f);yield return new WaitForSeconds(.7f);var page=ServiceScript.PagerFinal.Replace("{TIME}",now);LastPager=page;d.Say(page);
   yield return new WaitForSeconds(7f);d.Say(ServiceScript.FinaleNow.Replace("{TIME}",now));yield return new WaitForSeconds(3.6f);
   // the house goes dark and quiet
   foreach(var l in p.GetComponentsInChildren<Light>())l.enabled=false;d.Audio.HorrorAt("lightsout",cam.position,.4f);d.Audio.Hush=true;if(torch)torch.enabled=false;d.Hold(false);
   yield return new WaitForSeconds(2.2f);
   d.Audio.HorrorAt("herbreath",cam.position-cam.forward*1.1f,.5f);yield return new WaitForSeconds(1.1f);d.Narrate(ServiceScript.FinaleVoice);
   yield return new WaitForSeconds(2.6f);if(torch)torch.enabled=true;
   // between blinks she is nearer each time
   foreach(var near in new[]{3.6f,1.5f}){var at=AheadOnFloor(near);yield return d.Blink.BlinkTo(at,true);yield return new WaitForSeconds(near>2?1.7f:.9f);}
   yield return d.Blink.BlinkTo(Vector3.zero,false,1.5f); // the lids stay down - nothing - and then
   d.Hold(true);var e=d.Scene.Entity;var agent=e.GetComponent<UnityEngine.AI.NavMeshAgent>();if(agent)agent.enabled=false;
   if(d.Scene.EntityVariants!=null)for(int i=0;i<d.Scene.EntityVariants.Length;i++)d.Scene.EntityVariants[i].SetActive(i==Mathf.Min(3,d.Scene.EntityVariants.Length-1));
   e.transform.position=cam.position+cam.forward*1.2f-Vector3.up*1.5f;e.SetActive(true);foreach(var a in e.GetComponentsInChildren<Animator>())if(a.gameObject.activeInHierarchy){a.speed=1;a.CrossFadeInFixedTime("Attack",.02f,0);}
   d.Scare.Play(e.transform,ServiceJumpscare.HeadOf(e.transform),"scream",.6f,1.4f,1.4f,.46f,.17f); /* its attack pose throws the head back: lower, so the face is in the middle */d.Scare.HoldBlack();d.Audio.HorrorAt("roar",cam.position,1f,.55f);
   float t=0;while(t<4f&&!d.Scare.Black){t+=Time.deltaTime;yield return null;}
   e.SetActive(false);if(agent)agent.enabled=true;yield return new WaitForSeconds(1.2f);
   d.FinishWith(ServiceScript.EndingMain.Replace("{TIME}",now).Replace("{DATE}",date));yield return new WaitForSeconds(.5f);d.Scare.ReleaseBlack();d.Audio.Hush=false;d.Hold(false);FinaleRunning=false;}
  Vector3 AheadOnFloor(float dist){var cam=d.Scene.View.transform;var f=cam.forward;f.y=0;if(f.sqrMagnitude<.01f)f=Vector3.forward;f.Normalize();var want=cam.position+f*dist;
   if(UnityEngine.AI.NavMesh.SamplePosition(want-Vector3.up*1.6f,out var h,1.5f,UnityEngine.AI.NavMesh.AllAreas))return h.position;return new Vector3(want.x,d.Scene.Walker.transform.position.y-.9f,want.z);}
 }
}
