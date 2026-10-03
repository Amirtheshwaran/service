using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace ServiceGameV2 {
 // V20 menus: a 1998 camcorder tape playing back over the live county at night, in the manner of Fears to Fathom's
 // title screens. VHS on-screen-display lettering (VT323, SIL OFL), a blinking caret, items that slide and flicker in,
 // a running tape counter and date stamp, dashboard-button clicks (NOX Sound, CC0) and tape-skip transitions.
 // Everything animates on unscaled time, so the pause menu stays alive while the world is frozen.
 // Keyboard (W/S or arrows, A/D or arrows, Enter/Space, Q/E for tabs, Esc) and mouse (hover, click) both drive it.
 public sealed class ServiceMenus:MonoBehaviour {
  enum View{None,Title,Pause,Options,Confirm}
  sealed class Item{
   public RectTransform Root;public Text Label,Caret,Value,Sub;public Action Submit;public Action<int> Shift;public Func<string> ValueText;public string Hint;public float Slide,Reveal;public bool Header;
  }
  sealed class Page{public RectTransform Root;public CanvasGroup Group;public readonly List<Item> Items=new List<Item>();public int Selected;public float Shown;}
  ServiceDirector d;Canvas canvas;Font osd;AudioSource ui;AudioClip tick,confirm,back,begin,staticHiss;
  Page title,pause,options,confirmPage;readonly List<Page> tabs=new List<Page>();int tab;Text[] tabLabels;RectTransform tabBar;Text optionsHint;
  Text titleMain,titleRed,titleCyan,counter,playOsd,dateOsd,timeOsd,pauseOsd,confirmText,pauseSub,continueSub;Image fade;
  View baseView=View.None,overlay=View.None,shown=View.None;View confirmReturn;Action confirmAction;float tapeStart,skip,fadeAlpha,fadeTarget,lastCut;bool busy;Vector2 lastMouse;
  public bool Open=>shown!=View.None;
  public int QuitRequests {get;private set;} // the chaos test counts quits instead of quitting
  // Menu time: real time in play; one frame per captured frame while the review tour records (Time.captureFramerate),
  // so animations in the videos run at their true speed.
  public static float Dt=>Time.captureFramerate>0?1f/Time.captureFramerate:Time.unscaledDeltaTime;
  float clock;
  readonly Color white=new Color(.93f,.94f,.91f),dim=new Color(.56f,.59f,.57f),faint=new Color(.38f,.41f,.40f),amber=new Color(.86f,.68f,.40f);

  public void Initialize(ServiceDirector director){
   d=director;osd=Resources.Load<Font>("Fonts/VT323-Regular");if(!osd)osd=Resources.Load<Font>("Fonts/Barlow-Regular");
   var o=new GameObject("Service menus",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));o.transform.SetParent(transform,false);canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=60;
   var scaler=o.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
   ui=gameObject.AddComponent<AudioSource>();ui.playOnAwake=false;ui.spatialBlend=0;ui.ignoreListenerPause=true;ui.volume=.55f;
   tick=Resources.Load<AudioClip>("Audio/V20/ui/tick");confirm=Resources.Load<AudioClip>("Audio/V20/ui/confirm");back=Resources.Load<AudioClip>("Audio/V20/ui/back");begin=Resources.Load<AudioClip>("Audio/V20/ui/begin");staticHiss=Resources.Load<AudioClip>("Audio/V5/static/static_1");
   BuildTitle();BuildPause();BuildOptions();BuildConfirm();
   fade=Block(canvas.transform as RectTransform,new Rect(-400,-400,2080,1520),Color.black);fade.color=new Color(0,0,0,0);fade.raycastTarget=false;
   {var ft=fade.rectTransform;ft.anchorMin=Vector2.zero;ft.anchorMax=Vector2.one;ft.pivot=new Vector2(.5f,.5f);ft.offsetMin=new Vector2(-40,-40);ft.offsetMax=new Vector2(40,40);}
   tapeStart=0;
   foreach(var p in AllPages())Hide(p,true);
  }
  IEnumerable<Page> AllPages(){yield return title;yield return pause;yield return options;yield return confirmPage;}

  // ---------- building blocks (1280x720 reference, origin top-left)
  RectTransform Area(Transform parent,string name,Rect r){var g=new GameObject(name,typeof(RectTransform));var t=g.GetComponent<RectTransform>();t.SetParent(parent,false);t.anchorMin=t.anchorMax=new Vector2(0,1);t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(r.x,-r.y);t.sizeDelta=r.size;return t;}
  Image Block(RectTransform parent,Rect r,Color c){var t=Area(parent,"Surface",r);var i=t.gameObject.AddComponent<Image>();i.color=c;i.raycastTarget=false;return i;}
  Text Words(RectTransform parent,Rect r,string text,int size,Color c,TextAnchor anchor=TextAnchor.MiddleLeft){var t=Area(parent,text.Length>24?text.Substring(0,24):text,r);var l=t.gameObject.AddComponent<Text>();l.font=osd;l.text=text;l.fontSize=size;l.color=c;l.alignment=anchor;l.horizontalOverflow=HorizontalWrapMode.Overflow;l.verticalOverflow=VerticalWrapMode.Overflow;l.raycastTarget=false;return l;}
  Page NewPage(string name){var p=new Page();p.Root=Area(canvas.transform,name,new Rect(0,0,1280,720));p.Root.anchorMin=p.Root.anchorMax=p.Root.pivot=new Vector2(.5f,.5f);p.Root.anchoredPosition=Vector2.zero;p.Group=p.Root.gameObject.AddComponent<CanvasGroup>();return p;}
  // A soft left-hand darkening so the lettering reads over the moving picture (stacked bands, no generated texture).
  // V21: the darkest band runs out to the screen's left edge and every band spans the full height at any aspect ratio.
  void LeftShade(RectTransform parent,float strength){for(int i=0;i<12;i++){float a=strength*(1-i/12f);float x0=i==0?-3000:i*58;Block(parent,new Rect(x0,-2000,i*58+58-x0,4720),new Color(0,0,0,a*.55f));}}
  Item AddItem(Page p,float x,float y,string label,int size,Action submit,string hint=""){
   var it=new Item{Submit=submit,Hint=hint};it.Root=Area(p.Root,label,new Rect(x,y,760,size+10));
   it.Caret=Words(it.Root,new Rect(0,0,40,size+10),"▶",Mathf.RoundToInt(size*.72f),white);
   it.Label=Words(it.Root,new Rect(36,0,520,size+10),label,size,dim);
   p.Items.Add(it);return it;
  }
  Item AddValue(Page p,float y,string label,Func<string> value,Action<int> shift,string hint){
   var it=AddItem(p,98,y,label,34,()=>shift(1),hint);it.Shift=shift;it.ValueText=value;it.Value=Words(it.Root,new Rect(520,0,240,44),value(),32,amber,TextAnchor.MiddleRight);return it;
  }
  static string Bar(float v){int n=Mathf.RoundToInt(Mathf.Clamp01(v)*10);return new string('▮',n)+new string('▯',10-n)+"  "+Mathf.RoundToInt(Mathf.Clamp01(v)*100).ToString().PadLeft(3)+"%";}
  static string Pick(string v)=>"◀ "+v.ToUpperInvariant()+" ▶";

  // ---------- screens
  void BuildTitle(){
   title=NewPage("Menu - title");LeftShade(title.Root,1);
   playOsd=Words(title.Root,new Rect(58,34,300,40),"PLAY ▶",34,white);
   counter=Words(title.Root,new Rect(880,34,340,40),"SP  0:00:00",34,white,TextAnchor.MiddleRight);
   titleRed=Words(title.Root,new Rect(96,128,800,130),"SERVICE",132,new Color(.95f,.22f,.18f,.32f));
   titleCyan=Words(title.Root,new Rect(96,128,800,130),"SERVICE",132,new Color(.2f,.85f,.95f,.30f));
   titleMain=Words(title.Root,new Rect(96,128,800,130),"SERVICE",132,white);
   Words(title.Root,new Rect(102,262,800,36),"HOLLIS COUNTY  ·  CIVIL PROCESS  ·  NIGHT ROUTE",26,dim);
   var cont=AddItem(title,98,350,"CONTINUE",42,()=>Begin(()=>d.ContinueRoute()),"Pick up the route at the night you last filed.");
   continueSub=Words(cont.Root,new Rect(300,0,420,52),"",26,faint);
   AddItem(title,98,406,"NEW ROUTE",42,()=>{if(d.HasSavedRoute)Ask("START A NEW ROUTE?\nYOUR SAVED NIGHT WILL BE REPLACED.",()=>Begin(()=>d.NewRoute()));else Begin(()=>d.NewRoute());},"Begin at the depot on the first night.");
   AddItem(title,98,462,"OPTIONS",42,()=>OpenOptions(),"Picture, sound and controls.");
   AddItem(title,98,518,"QUIT",42,()=>Ask("QUIT TO DESKTOP?",()=>{if(d.IsChaos){QuitRequests++;return;}Application.Quit();}),"");
   Words(title.Root,new Rect(100,652,700,30),"W S  SELECT      ENTER  CONFIRM",22,faint);
   dateOsd=Words(title.Root,new Rect(820,606,400,40),"",34,white,TextAnchor.MiddleRight);
   timeOsd=Words(title.Root,new Rect(820,644,400,40),"",34,white,TextAnchor.MiddleRight);
  }
  void BuildPause(){
   pause=NewPage("Menu - pause");Block(pause.Root,new Rect(-400,-400,2080,1520),new Color(0,0,0,.45f));LeftShade(pause.Root,.9f);
   pauseOsd=Words(pause.Root,new Rect(58,34,300,40),"‖ PAUSE",34,white);
   Words(pause.Root,new Rect(96,150,800,100),"PAUSED",96,white);
   pauseSub=Words(pause.Root,new Rect(102,250,900,36),"",26,dim);
   AddItem(pause,98,330,"RESUME",42,()=>{Sound(confirm);d.Resume();});
   AddItem(pause,98,386,"OPTIONS",42,()=>OpenOptions());
   AddItem(pause,98,442,"RESTART NIGHT",42,()=>Ask("RESTART THIS NIGHT?\nEVERYTHING SINCE THE DEPOT IS LOST.",()=>Begin(()=>d.BeginShift(d.NightIndex))));
   AddItem(pause,98,498,"QUIT TO TITLE",42,()=>Ask("QUIT TO THE TITLE?\nPROGRESS IS SAVED ONLY WHEN A REPORT IS FILED.",()=>{d.Title();}));
   Words(pause.Root,new Rect(100,652,900,30),"PROGRESS IS SAVED WHEN A SHIFT REPORT IS FILED.",22,faint);
  }
  void BuildOptions(){
   options=NewPage("Menu - options");Block(options.Root,new Rect(-400,-400,2080,1520),new Color(0,0,0,.72f));LeftShade(options.Root,.8f);
   Words(options.Root,new Rect(58,34,300,40),"■ MENU",34,white);
   Words(options.Root,new Rect(96,74,800,90),"OPTIONS",80,white);
   string[] names={"GAME","VIDEO","AUDIO","CONTROLS"};tabLabels=new Text[names.Length];tabBar=Area(options.Root,"Tab bar",new Rect(102,196,10,3));Block(tabBar,new Rect(0,0,10,3),white);
   for(int i=0;i<names.Length;i++){tabLabels[i]=Words(options.Root,new Rect(102+i*170,160,160,40),names[i],34,dim);}
   Words(options.Root,new Rect(790,160,400,40),"Q  E   TABS",24,faint,TextAnchor.MiddleRight);
   for(int i=0;i<names.Length;i++){var p=new Page();p.Root=Area(options.Root,"Tab "+names[i],new Rect(0,0,1280,720));p.Group=p.Root.gameObject.AddComponent<CanvasGroup>();tabs.Add(p);}
   // GAME
   var g=tabs[0];float y=236;
   AddValue(g,y,"LOOK SENSITIVITY",()=>Bar(Mathf.InverseLerp(.03f,.2f,d.Player.Sensitivity)),s=>d.Player.Sensitivity=Mathf.Clamp(d.Player.Sensitivity+s*.017f,.03f,.2f),"How far the view turns for each movement of the mouse.");y+=52;
   AddValue(g,y,"CAMERA MOVEMENT",()=>Bar(d.Player.CameraMotion),s=>d.Player.CameraMotion=Mathf.Clamp01(d.Player.CameraMotion+s*.1f),"Head bob and sway while walking and driving.");y+=52;
   AddValue(g,y,"MOTION BLUR",()=>Bar(d.Player.MotionBlurAmount/.35f),s=>d.Player.MotionBlurAmount=Mathf.Clamp(d.Player.MotionBlurAmount+s*.035f,0,.35f),"Smearing of the picture on fast turns.");y+=52;
   AddValue(g,y,"FLASHES",()=>Pick(d.Storm&&d.Storm.FlashesEnabled?"On":"Off"),s=>{if(d.Storm)d.Storm.FlashesEnabled=!d.Storm.FlashesEnabled;},"Lightning and impact flashes. Turn off if flashing light troubles you.");
   // VIDEO
   var v=tabs[1];y=236;
   AddValue(v,y,"DISPLAY",()=>Pick(Screen.fullScreen?"Full screen":"Windowed"),s=>Screen.fullScreen=!Screen.fullScreen,"Full screen or a window.");y+=52;
   AddValue(v,y,"CAMERA FILTER",()=>Pick(d.Camcorder?ServiceCamcorder.Names[d.Camcorder.Level]:"Off"),s=>{if(d.Camcorder)d.Camcorder.Set((d.Camcorder.Level+s+4)%4);},"The camcorder look. STRONG is the intended picture; EXTREME is lower still.");y+=52;
   AddValue(v,y,"BRIGHTNESS",()=>Bar(Mathf.InverseLerp(.6f,1.6f,ServiceCamcorder.Brightness)),s=>{if(d.Camcorder)d.Camcorder.SetBrightness(ServiceCamcorder.Brightness+s*.1f);},"Raise it until the darkest corner of the road is just visible.");
   // AUDIO
   var a=tabs[2];y=236;
   AddValue(a,y,"MASTER VOLUME",()=>Bar(d.Audio.Volume),s=>d.Audio.Volume=Mathf.Clamp01(d.Audio.Volume+s*.1f),"Everything you hear.");y+=52;
   AddValue(a,y,"MUSIC",()=>Bar(d.Audio.MusicVolume),s=>d.Audio.MusicVolume=Mathf.Clamp01(d.Audio.MusicVolume+s*.1f),"The score on the title and in quiet stretches.");
   // CONTROLS (read-only)
   var c=tabs[3];string[] keys={"W A S D","MOUSE","SHIFT","CTRL","SPACE","E","1 2 3","R","U","F","TAB","V  B","ESC"};
   string[] acts={"WALK / DRIVE","LOOK","SPRINT / BRAKE","CROUCH","JUMP / IGNITION","USE / KNOCK / READ","CHOOSE A REPLY","LEAVE THE PAPERS","UNABLE TO SERVE","FLASHLIGHT","CLIPBOARD","RADIO / TUNE","PAUSE / BACK"};
   for(int i=0;i<keys.Length;i++){Words(c.Root,new Rect(134,228+i*27,180,27),keys[i],26,amber);Words(c.Root,new Rect(330,228+i*27,600,27),acts[i],26,white);}
   optionsHint=Words(options.Root,new Rect(100,604,1080,32),"",26,dim);
   Words(options.Root,new Rect(100,652,1100,30),"ESC  BACK      A D  CHANGE      W S  SELECT",22,faint);
   SetTab(0);
  }
  void BuildConfirm(){
   confirmPage=NewPage("Menu - confirm");Block(confirmPage.Root,new Rect(-400,-400,2080,1520),new Color(0,0,0,.35f));
   Block(confirmPage.Root,new Rect(102,322,520,2),white);
   confirmText=Words(confirmPage.Root,new Rect(102,336,900,80),"",34,white,TextAnchor.UpperLeft);
   AddItem(confirmPage,98,430,"NO",42,()=>Close(View.Confirm));
   AddItem(confirmPage,98,486,"YES",42,()=>{var act=confirmAction;Close(View.Confirm);act?.Invoke();});
  }
  void SetTab(int i){tab=(i+tabs.Count)%tabs.Count;for(int k=0;k<tabs.Count;k++){bool on=k==tab;tabs[k].Group.alpha=on?1:0;tabs[k].Root.gameObject.SetActive(on);tabLabels[k].color=on?white:dim;}
   var lbl=tabLabels[tab].rectTransform;tabBar.anchoredPosition=new Vector2(lbl.anchoredPosition.x,-200);tabBar.sizeDelta=new Vector2(tabLabels[tab].preferredWidth,3);tabBar.GetChild(0).GetComponent<RectTransform>().sizeDelta=new Vector2(tabLabels[tab].preferredWidth,3);
   var p=tabs[tab];p.Selected=0;foreach(var it in p.Items){it.Reveal=0;it.Slide=0;}}

  // ---------- flow
  public bool Back(){
   if(busy)return true;
   if(overlay==View.Confirm){Close(View.Confirm);return true;}
   if(overlay==View.Options){Close(View.Options);d.SaveOptions();return true;}
   if(baseView==View.Pause){Sound(back);return false;} // let the director resume
   return baseView==View.Title;
  }
  // Test hooks for the review tour (the same paths a key press takes).
  public void SmokeMove(int dir){var page=Active();if(page==null||page.Items.Count==0)return;int n=page.Items.Count,s=page.Selected;for(int k=0;k<n;k++){s=(s+dir+n)%n;if(!(page==title&&s==0&&!d.HasSavedRoute))break;}page.Selected=s;Sound(tick,.7f);}
  public void SmokeShift(int dir){var page=Active();if(page==null||page.Items.Count==0)return;var it=page.Items[page.Selected];if(it.Shift!=null){it.Shift(dir);Sound(tick,.8f);}}
  public void SmokeSubmit(){var page=Active();if(page==null||page.Items.Count==0)return;var it=page.Items[page.Selected];if(it.Shift!=null)it.Shift(1);else{Sound(confirm);it.Submit?.Invoke();}}
  public void SmokeTab(int dir){if(shown==View.Options){SetTab(tab+dir);Sound(tick,.8f);}}
  public bool SmokeSelect(string label){var page=Active();if(page==null)return false;for(int i=0;i<page.Items.Count;i++)if(page.Items[i].Label.text==label){page.Selected=i;Sound(tick,.7f);return true;}return false;}
  public string SmokeState=>shown+"/"+(Active()!=null?Active().Selected:-1);
  public void SmokeOptions(bool open){if(open)OpenOptions();else if(overlay==View.Options)Close(View.Options);}
  void OpenOptions(){Sound(confirm);overlay=View.Options;SetTab(tab);Skip();}
  void Ask(string question,Action yes){Sound(confirm);confirmReturn=overlay;confirmAction=yes;confirmText.text=question;overlay=View.Confirm;confirmPage.Selected=0;Skip();}
  void Close(View v){Sound(back);if(v==View.Confirm){var u=confirmReturn==View.Options?(tabs.Count>tab?tabs[tab]:null):PageOf(baseView);if(u!=null)foreach(var it in u.Items)it.Reveal=0;}if(v==View.Confirm)overlay=confirmReturn==View.Options?View.Options:View.None;else if(v==View.Options)overlay=View.None;Skip();}
  void Begin(Action act){if(busy)return;busy=true;Sound(begin);StartCoroutine(BeginRoutine(act));}
  System.Collections.IEnumerator BeginRoutine(Action act){playOsd.text="PLAY ▶";fadeTarget=1;float t=0;while(t<.75f){t+=Dt;yield return null;}overlay=View.None;act();yield return null;fadeTarget=0;busy=false;}
  void Skip(){skip=.16f;if(staticHiss&&shown!=View.None)ui.PlayOneShot(staticHiss,.08f);}
  void Sound(AudioClip c,float v=1){if(c)ui.PlayOneShot(c,v);}

  Page PageOf(View v)=>v==View.Title?title:v==View.Pause?pause:v==View.Options?options:v==View.Confirm?confirmPage:null;
  Page Active(){var p=PageOf(shown);return p==options?tabs[tab]:p;}
  void Hide(Page p,bool instant){p.Group.alpha=0;p.Group.interactable=false;p.Group.blocksRaycasts=false;p.Root.gameObject.SetActive(false);}
  void Show(Page p){p.Root.gameObject.SetActive(true);p.Group.alpha=0;p.Shown=0;foreach(var it in p.Items){it.Reveal=0;it.Slide=0;}if(p==options)foreach(var t in tabs)foreach(var it in t.Items){it.Reveal=0;it.Slide=0;}}

  void Update(){
   if(!d||!canvas)return;float dt=Dt;clock+=dt;
   bool intro=d.Phase==ServicePhase.Title&&d.Presentation&&d.Presentation.IntroVisible;
   var nb=d.Phase==ServicePhase.Title&&!intro?View.Title:d.Phase==ServicePhase.Paused?View.Pause:View.None;
   if(nb!=baseView){baseView=nb;overlay=View.None;if(nb!=View.None&&shown!=View.None)Skip();}
   if(baseView==View.None&&!busy)overlay=View.None;
   var want=overlay!=View.None?overlay:baseView;
   if(want!=shown){
    // Base screens stay visible under an overlay (pause behind its confirm; title behind options and confirm).
    foreach(var p in AllPages()){bool keep=(p==PageOf(want))||(p==PageOf(baseView)&&want==View.Confirm&&confirmReturn!=View.Options)||(p==options&&want==View.Confirm&&confirmReturn==View.Options);if(!keep&&p.Root.gameObject.activeSelf)Hide(p,false);else if(keep&&!p.Root.gameObject.activeSelf)Show(p);}
    if(want==View.Title||want==View.Pause){var p=PageOf(want);p.Selected=p==title&&!d.HasSavedRoute?1:0;}
    shown=want;
   }
   fadeAlpha=Mathf.MoveTowards(fadeAlpha,fadeTarget,dt*(fadeTarget>fadeAlpha?2.2f:1.4f));fade.color=new Color(0,0,0,fadeAlpha);
   if(shown==View.None)return;
   // a cut in the title tape jumps the lettering too
   if(d.Presentation&&d.Presentation.ShotChangedAt!=lastCut){if(lastCut>0&&shown==View.Title)Skip();lastCut=d.Presentation.ShotChangedAt;}
   // tape skip: the whole picture of lettering jumps sideways for a moment
   skip=Mathf.Max(0,skip-dt);float jx=skip>0?UnityEngine.Random.Range(-7f,7f):0;
   foreach(var p in AllPages())if(p.Root.gameObject.activeSelf){p.Shown=Mathf.Min(1,p.Shown+dt*4.5f);p.Group.alpha=Mathf.SmoothStep(0,1,p.Shown)*(skip>0?UnityEngine.Random.Range(.55f,1f):1);p.Root.anchoredPosition=new Vector2(jx,0);p.Group.interactable=p.Group.blocksRaycasts=PageOf(shown)==p;}
   Animate(dt);
   if(!busy)Navigate();
  }
  void Animate(float dt){
   float now=clock;bool blink=Mathf.Repeat(now,1.1f)<.62f;
   // OSD clock and counter
   var tape=TimeSpan.FromSeconds(now-tapeStart);counter.text=$"SP  {(int)tape.TotalHours}:{tape.Minutes:00}:{tape.Seconds:00}";
   int night=d.HasSavedRoute?Mathf.Clamp(PlayerPrefs.GetInt("SERVICE.v5.night",0),0,2):0;
   string[] dates={"OCT. 01 1998","OCT. 04 1998","OCT. 09 1998"};string[] times={"PM 9:48","PM 10:21","PM 11:57"};
   dateOsd.text=dates[night];int m=(int)(tape.TotalMinutes);timeOsd.text=times[night].Substring(0,times[night].Length)+(blink?" ":" ");
   continueSub.text=d.HasSavedRoute?"NIGHT "+(night+1)+"  ·  "+dates[night]:"";
   title.Items[0].Label.color=d.HasSavedRoute?title.Items[0].Label.color:faint;
   playOsd.color=new Color(1,1,1,(busy?(blink?1:.2f):1));
   pauseOsd.color=new Color(1,1,1,blink?1:.25f);
   if(pauseSub)pauseSub.text="NIGHT "+(d.NightIndex+1)+"  ·  "+d.Date.ToUpperInvariant();
   // chroma fringing on the title, jittering now and then like a worn tape
   float jit=Mathf.PerlinNoise(now*2.3f,.3f)>.72f?UnityEngine.Random.Range(-2.5f,2.5f):0;
   titleRed.rectTransform.anchoredPosition=new Vector2(96-2.2f+jit,-128);titleCyan.rectTransform.anchoredPosition=new Vector2(96+2.2f-jit*.5f,-128);
   titleMain.color=new Color(white.r,white.g,white.b,Mathf.PerlinNoise(now*5f,1.7f)>.86f?.82f:1);
   var under=shown==View.Confirm?(confirmReturn==View.Options?tabs[tab]:PageOf(baseView)):null;
   foreach(var bp in new[]{title,pause,tabs.Count>tab?tabs[tab]:null})if(bp!=null)foreach(var it in bp.Items){var g=it.Root.GetComponent<CanvasGroup>();if(g&&bp==under)g.alpha=Mathf.MoveTowards(g.alpha,0,dt*6);}
   var page=Active();if(page==null)return;
   for(int i=0;i<page.Items.Count;i++){var it=page.Items[i];bool sel=i==page.Selected;
    it.Reveal=Mathf.Min(1,it.Reveal+dt*5f*(i<2?1:Mathf.Clamp01(Mathf.Max(0,(PageAge(page)-i*.06f))*8)));
    it.Slide=Mathf.Lerp(it.Slide,sel?10:0,1-Mathf.Exp(-dt*14));
    it.Label.rectTransform.anchoredPosition=new Vector2(36+it.Slide,0);
    bool disabled=page==title&&i==0&&!d.HasSavedRoute;
    it.Label.color=Color.Lerp(it.Label.color,disabled?faint:sel?white:dim,1-Mathf.Exp(-dt*16));
    it.Caret.color=new Color(1,1,1,sel&&!disabled&&(blink||it.Shift!=null)?1:0);
    if(it.Value!=null){it.Value.text=it.ValueText();it.Value.color=sel?amber:new Color(amber.r,amber.g,amber.b,.7f);}
    var cg=it.Root.GetComponent<CanvasGroup>();if(!cg)cg=it.Root.gameObject.AddComponent<CanvasGroup>();cg.alpha=it.Reveal;}
   if(shown==View.Options&&optionsHint){var s=page.Items.Count>0?page.Items[Mathf.Clamp(page.Selected,0,page.Items.Count-1)].Hint:"";optionsHint.text=s;}
  }
  float PageAge(Page p){var root=PageOf(shown);return root!=null?root.Shown*0.6f+.1f:1;}
  bool Down(Key k)=>Keyboard.current!=null&&Keyboard.current[k].wasPressedThisFrame;
  void Navigate(){
   var page=Active();if(page==null)return;int n=page.Items.Count;
   if(shown==View.Options){if(Down(Key.Q)){SetTab(tab-1);Sound(tick,.8f);}if(Down(Key.E)||Down(Key.Tab)){SetTab(tab+1);Sound(tick,.8f);}page=Active();n=page.Items.Count;
    var mouseTab=MouseOver(tabLabels);if(mouseTab>=0&&Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame){SetTab(mouseTab);Sound(tick,.8f);return;}}
   if(n==0)return;
   int move=(Down(Key.S)||Down(Key.DownArrow)?1:0)-(Down(Key.W)||Down(Key.UpArrow)?1:0);
   if(move!=0){int s=page.Selected;for(int k=0;k<n;k++){s=(s+move+n)%n;if(!(page==title&&s==0&&!d.HasSavedRoute))break;}if(s!=page.Selected){page.Selected=s;Sound(tick,.7f);}}
   var it=page.Items[Mathf.Clamp(page.Selected,0,n-1)];
   int shift=(Down(Key.D)||Down(Key.RightArrow)?1:0)-(Down(Key.A)||Down(Key.LeftArrow)?1:0);
   if(shift!=0&&it.Shift!=null){it.Shift(shift);Sound(tick,.8f);}
   if(Down(Key.Enter)||Down(Key.NumpadEnter)||Down(Key.Space)||(shown!=View.Options&&Down(Key.E))){if(it.Shift!=null)it.Shift(1);else if(!(page==title&&page.Selected==0&&!d.HasSavedRoute)){Sound(confirm);it.Submit?.Invoke();}}
   // mouse: hover selects, click activates (values: left half lowers, right half raises)
   if(Mouse.current==null)return;var mp=Mouse.current.position.ReadValue();bool moved=(mp-lastMouse).sqrMagnitude>4;lastMouse=mp;
   for(int i=0;i<n;i++){var r=page.Items[i].Root;if(!RectTransformUtility.RectangleContainsScreenPoint(r,mp,null))continue;
    if(moved&&page.Selected!=i&&!(page==title&&i==0&&!d.HasSavedRoute)){page.Selected=i;Sound(tick,.5f);}
    if(Mouse.current.leftButton.wasPressedThisFrame&&page.Selected==i&&!(page==title&&i==0&&!d.HasSavedRoute)){var x=page.Items[i];if(x.Shift!=null){RectTransformUtility.ScreenPointToLocalPointInRectangle(r,mp,null,out var local);x.Shift(local.x<560?-1:1);Sound(tick,.8f);}else{Sound(confirm);x.Submit?.Invoke();}}
    break;}
  }
  int MouseOver(Text[] labels){if(Mouse.current==null)return -1;var mp=Mouse.current.position.ReadValue();for(int i=0;i<labels.Length;i++)if(RectTransformUtility.RectangleContainsScreenPoint(labels[i].rectTransform,mp,null))return i;return -1;}
 }
}
