using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace ServiceGameV2 {
 public sealed class ServiceHUD:MonoBehaviour {
  ServiceDirector d;Canvas canvas;RectTransform root,carPin,carHeading;Image introShade,contactShade,cardShade;CanvasGroup introWords,cardWords;Font sans,document;bool options,confirmNew;string last="";int selected=-1;Rect worldBounds,mapPanel;Button firstButton;
  readonly Color white=new Color(.88f,.89f,.86f),muted=new Color(.53f,.58f,.57f),accent=new Color(.72f,.57f,.36f),panel=new Color(.035f,.045f,.048f,.97f),ink=new Color(.16f,.20f,.19f),mapPaper=new Color(.63f,.65f,.59f);
  public int MapPins {get;private set;} public int MapSegments {get;private set;}
  ServiceMenus menus;bool vt;
  public void Initialize(ServiceDirector director){d=director;sans=Resources.Load<Font>("Fonts/VT323-Regular");vt=sans;if(!sans)sans=Resources.Load<Font>("Fonts/Barlow-Regular");document=Resources.Load<Font>("Fonts/CourierPrime-Regular");
   var o=new GameObject("Service interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));o.transform.SetParent(transform,false);canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;var scaler=o.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
   if(!FindAnyObjectByType<EventSystem>()){var events=new GameObject("Interface input",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
   // V20: title, pause and options are drawn by ServiceMenus (animated, persistent); the HUD keeps the in-game layer.
   menus=gameObject.AddComponent<ServiceMenus>();menus.Initialize(d);
  }
  public void SmokeOptions(bool open){if(d.IsSmoke){if(menus)menus.SmokeOptions(open);else{options=open;last="";}}}
  public bool Back(){if(menus)return menus.Back();if(options||confirmNew){options=confirmNew=false;d.SaveOptions();last="";return true;}return false;}
  RectTransform Area(string name,Rect rect,Transform parent=null){var o=new GameObject(name,typeof(RectTransform));var t=o.GetComponent<RectTransform>();t.SetParent(parent?parent:root,false);t.anchorMin=t.anchorMax=new Vector2(0,1);t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(rect.x,-rect.y);t.sizeDelta=rect.size;return t;}
  Image Block(Rect r,Color color,Transform parent=null){var t=Area("Surface",r,parent);var image=t.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
  Text Label(Rect r,string text,int size,Color? color=null,Transform parent=null,bool mono=false){var t=Area(text,r,parent);var label=t.gameObject.AddComponent<Text>();label.font=mono?document:sans;label.text=text;label.fontSize=vt&&!mono?Mathf.RoundToInt(size*1.38f):size;label.color=color??white;label.alignment=TextAnchor.MiddleLeft;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;label.raycastTarget=false;return label;}
  Button Action(Rect r,string caption,System.Action action,bool prominent=false,Transform parent=null){var t=Area(caption,r,parent);var image=t.gameObject.AddComponent<Image>();image.color=Color.clear;var b=t.gameObject.AddComponent<Button>();var text=Label(new Rect(0,0,r.width,r.height),caption,prominent?27:20,null,t);b.targetGraphic=text;var colors=b.colors;colors.normalColor=white;colors.highlightedColor=Color.white;colors.selectedColor=new Color(1,.8f,.53f);colors.pressedColor=accent;colors.fadeDuration=.13f;b.colors=colors;b.onClick.AddListener(()=>{action();last="";});if(!firstButton)firstButton=b;return b;}
  void Rule(float x,float y,float w,Color? color=null){Block(new Rect(x,y,w,1),color??new Color(.23f,.29f,.28f));}
  void Line(Vector2 a,Vector2 b,float width,Color color){var t=Block(new Rect(a.x,a.y,Vector2.Distance(a,b),width),color).rectTransform;t.pivot=new Vector2(0,.5f);t.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);}
  string Context(){if(d.Busy||d.Horror.Caught)return "";if(d.Player.InCar)return d.CanFinish?"E   File shift report":d.Player.Speed<.8f?(d.Player.IsStarting?"Turning the ignition…":"E   Leave vehicle     /     Space   Ignition"):"";
   if(Vector3.Distance(d.Scene.View.transform.position,d.Scene.Car.position+Vector3.up)<3.5f)return "E   Enter vehicle";
   if(d.NoteOpen>=0)return "E   Stop reading";
   if(d.NearbyNotice()>=0)return "E   Read the note";
   int front=d.NearbyKnockDoor();if(front>=0)return "E   Knock     /     U   No service";
   if(d.NearbyDoor()>=0)return "E / R   Leave the notice";
   return "";
  }
  void LateUpdate(){if(!d||!canvas)return;bool intro=d.Phase==ServicePhase.Title&&d.Presentation&&d.Presentation.IntroVisible;string context=Context();bool card=ShiftCard;string state=card+"|"+(d.Horror.Active?d.Horror.Instruction+d.Horror.Headline:"")+"|"+d.Phase+"|"+d.PaperOpen+"|"+d.MapOpen+"|"+options+"|"+confirmNew+"|"+intro+"|"+d.Notice+"|"+d.Horror.Phase+"|"+context+"|"+d.NightIndex+"|"+(d.Vehicle?d.Vehicle.RadioOn+"/"+d.Vehicle.Station:"")+"|"+string.Join(",",d.Docket.Select(e=>(int)e.Result))+"|"+(d.Dialogue?d.Dialogue.StateKey:"")+"|"+d.NoteOpen+"|"+(d.Guide?d.Guide.Objective+d.Guide.Direction:"")+"|"+(d.Player.InCar&&d.NightIndex==0&&d.ShiftCardTime<28);
   if(last!=state){last=state;Rebuild(intro,context,card);}
   if(card&&cardShade){float t=d.ShiftCardTime;cardShade.color=new Color(0,0,0,1-Mathf.SmoothStep(0,1,(t-3.1f)/1.5f));cardWords.alpha=Mathf.SmoothStep(0,1,t/.9f)*(1-Mathf.SmoothStep(0,1,(t-2.5f)/.9f));}
   if(intro&&introShade){introShade.color=new Color(0,0,0,d.Presentation.IntroBackgroundAlpha);introWords.alpha=d.Presentation.IntroTextAlpha;}
   if(contactShade)contactShade.color=new Color(.65f,.62f,.56f,d.Storm.FlashesEnabled?d.Horror.ImpactAlpha:0);
   if(carPin){var pos=ServiceRouteMap.Project(d.Scene.Car.position,worldBounds,mapPanel);carPin.anchoredPosition=new Vector2(pos.x,-pos.y);carHeading.localRotation=Quaternion.Euler(0,0,-d.Scene.Car.eulerAngles.y);}
  }
  bool ShiftCard=>d.Phase==ServicePhase.Playing&&d.ShiftCardTime<4.6f;
  void Rebuild(bool intro,string context,bool card){if(root){root.gameObject.SetActive(false);Destroy(root.gameObject);}firstButton=null;carPin=carHeading=null;introShade=contactShade=cardShade=null;cardWords=null;MapPins=MapSegments=0;root=Area("Screen",new Rect(0,0,1280,720),canvas.transform);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
   if(intro){introShade=Block(new Rect(-1000,-1000,3280,2720),Color.black);var words=Area("Headphones",new Rect(0,0,1280,720));introWords=words.gameObject.AddComponent<CanvasGroup>();Label(new Rect(0,290,1280,52),"Headphones recommended",30,null,words).alignment=TextAnchor.MiddleCenter;Label(new Rect(0,348,1280,35),"For directional sound, use headphones.",18,muted,words).alignment=TextAnchor.MiddleCenter;Label(new Rect(0,636,1280,30),"Press any key to continue",15,muted,words).alignment=TextAnchor.MiddleCenter;return;}
   if(d.Phase==ServicePhase.Title){if(!menus)Title();}else if(d.Phase==ServicePhase.Paused){if(!menus)Pause();}else if(d.Phase==ServicePhase.Finished)Epilogue();else if(d.Phase==ServicePhase.Report)Report();else if(d.PaperOpen)Documents();else Playing(context);
   if(card)ShiftTitle();
   if(firstButton&&EventSystem.current)EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
  }
  void ShiftTitle(){cardShade=Block(new Rect(-1000,-1000,3280,2720),Color.black);cardShade.raycastTarget=true;var words=Area("Shift title",new Rect(0,0,1280,720));cardWords=words.gameObject.AddComponent<CanvasGroup>();cardWords.blocksRaycasts=false;cardWords.alpha=0;
   string[] nights={"NIGHT ONE","NIGHT TWO","NIGHT THREE"};string[] stamps={"OCT. 01 1998","OCT. 04 1998","OCT. 09 1998"};
   Label(new Rect(58,34,300,40),"PLAY ▶",25,null,words);
   Label(new Rect(96,250,1000,90),nights[Mathf.Clamp(d.NightIndex,0,2)],66,null,words);
   Label(new Rect(102,340,1000,34),d.LongDate.ToUpperInvariant(),20,muted,words);
   Label(new Rect(102,374,1000,34),"HOLLIS COUNTY  ·  "+d.ShiftTime.ToUpperInvariant(),20,muted,words);
   var stamp=Label(new Rect(800,606,420,40),stamps[Mathf.Clamp(d.NightIndex,0,2)],25,null,words);stamp.alignment=TextAnchor.MiddleRight;
   var clock=Label(new Rect(800,644,420,40),d.ShiftTime.ToUpperInvariant(),25,null,words);clock.alignment=TextAnchor.MiddleRight;}
  void Epilogue(){Shade(1);Label(new Rect(190,150,900,40),"Hollis County, 1998",18,muted);Rule(190,204,58,accent);
   var text=Label(new Rect(190,232,900,300),ServiceScript.Epilogue,20);text.alignment=TextAnchor.UpperLeft;text.lineSpacing=1.15f;
   Action(new Rect(190,600,320,40),"Return to title",()=>d.Title());}
  void Shade(float alpha=.86f){Block(new Rect(-1000,-1000,3280,2720),new Color(.015f,.021f,.026f,alpha));}
  void Title(){if(options){Settings();return;}Shade(.1f);Block(new Rect(0,0,490,720),new Color(.015f,.022f,.026f,.67f));
   Label(new Rect(78,160,450,85),"S E R V I C E",56);Label(new Rect(82,244,320,30),"Hollis County, October 1998",18,muted);Rule(82,306,58,accent);
   if(confirmNew){Label(new Rect(82,346,330,65),"Begin a new route? Your saved shift will be replaced.",20);Action(new Rect(82,433,330,40),"Begin new route",()=>{confirmNew=false;d.NewRoute();});Action(new Rect(82,487,330,40),"Go back",()=>confirmNew=false);return;}
   Action(new Rect(82,352,340,48),d.HasSavedRoute?"Continue":"Begin route",()=>{if(d.HasSavedRoute)d.ContinueRoute();else d.NewRoute();},true);
   Action(new Rect(82,419,340,40),"Settings",()=>options=true);if(d.HasSavedRoute)Action(new Rect(82,472,340,40),"New route",()=>confirmNew=true);
   Action(new Rect(82,d.HasSavedRoute?525:472,340,40),"Quit game",()=>Application.Quit());Label(new Rect(82,646,330,30),"A notice must reach every door.",16,muted);
  }
  void Pause(){if(options){Settings();return;}Shade(.85f);Label(new Rect(95,98,620,65),"Paused",48);Label(new Rect(98,172,500,30),"Evening route  /  "+d.Date.ToLowerInvariant(),18,muted);Rule(98,234,68,accent);
   Action(new Rect(98,279,360,45),"Return to shift",()=>d.Resume(),true);Action(new Rect(98,343,360,40),"Settings",()=>options=true);Action(new Rect(98,400,360,40),"Restart current shift",()=>d.BeginShift(d.NightIndex));Action(new Rect(98,457,360,40),"Return to title",()=>d.Title());Label(new Rect(98,639,670,28),"Progress is saved when a shift report is filed.",17,muted);
  }
  void Setting(float y,string caption,float value,float min,float max,System.Action<float> change,string format){Label(new Rect(95,y,280,30),caption,19);var number=Label(new Rect(424,y,105,30),format=="%"?Mathf.RoundToInt(value/max*100)+"%":value.ToString("0.00"),17,muted);number.alignment=TextAnchor.MiddleRight;
   var t=Area(caption+" slider",new Rect(95,y+34,434,18));var hit=t.gameObject.AddComponent<Image>();hit.color=Color.clear;var slider=t.gameObject.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;var back=Block(new Rect(0,7,434,3),new Color(.2f,.26f,.26f),t);var fillArea=Area("Slider track",new Rect(0,7,434,3),t);var fill=Block(new Rect(0,0,0,0),accent,fillArea);fill.rectTransform.anchorMin=new Vector2(0,.5f);fill.rectTransform.anchorMax=new Vector2(1,.5f);fill.rectTransform.pivot=new Vector2(0,.5f);fill.rectTransform.anchoredPosition=Vector2.zero;var handle=Block(new Rect(0,0,10,18),white,t);handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(0,.5f);handle.rectTransform.pivot=new Vector2(.5f,.5f);slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=value;slider.onValueChanged.AddListener(v=>{change(v);number.text=format=="%"?Mathf.RoundToInt(v/max*100)+"%":v.ToString("0.00");});}
  void Settings(){Shade(.96f);Label(new Rect(95,50,900,60),"Settings",42);Rule(95,128,1090);Setting(151,"Master volume",d.Audio.Volume,0,1,v=>d.Audio.Volume=v,"%");Setting(216,"Music",d.Audio.MusicVolume,0,1,v=>d.Audio.MusicVolume=v,"%");Setting(281,"Look sensitivity",d.Player.Sensitivity,.03f,.2f,v=>d.Player.Sensitivity=v,"");Setting(346,"Camera movement",d.Player.CameraMotion,0,1,v=>d.Player.CameraMotion=v,"%");Setting(411,"Motion blur",d.Player.MotionBlurAmount,0,.35f,v=>d.Player.MotionBlurAmount=v,"%");
   Action(new Rect(95,484,445,34),"Lightning / impact flashes    "+(d.Storm.FlashesEnabled?"On":"Off"),()=>d.Storm.FlashesEnabled=!d.Storm.FlashesEnabled);Action(new Rect(95,531,445,34),"Display    "+(Screen.fullScreen?"Full screen":"Windowed"),()=>Screen.fullScreen=!Screen.fullScreen);if(d.Camcorder)Action(new Rect(95,578,445,34),"Camera filter    "+ServiceCamcorder.Names[d.Camcorder.Level],()=>d.Camcorder.Cycle());
   Label(new Rect(659,152,420,35),"Controls",24);string[] keys={"W A S D","Mouse","Shift","Ctrl","Space","E","1 / 2 / 3","R / U","F","Tab / M","V / B","Esc"};string[] actions={"Move / drive","Look","Sprint / brake","Hold to crouch","Jump / ignition in car","Interact / knock / read","Choose a reply","Leave papers / no service","Flashlight","Docket / route map","Radio power / station","Pause / close document"};for(int i=0;i<keys.Length;i++){Label(new Rect(659,201+i*30,112,28),keys[i],17,accent);Label(new Rect(797,201+i*30,390,28),actions[i],18);}
   Rule(95,627,1090);Action(new Rect(95,642,300,40),"Back",()=>{options=false;d.SaveOptions();});Label(new Rect(659,647,520,32),"Camera filter: Strong is the intended look.",15,muted);
  }
  // V20: the docket is a clipboard - typed county paperwork with the dispatcher's directions written under each stop.
  void Documents(){Shade(.86f);
   Block(new Rect(318,14,644,698),new Color(.30f,.22f,.15f));Block(new Rect(340,54,600,646),new Color(.92f,.90f,.84f));
   Block(new Rect(565,4,150,58),new Color(.55f,.56f,.53f));Block(new Rect(590,18,100,16),new Color(.32f,.33f,.32f));
   var ink=new Color(.12f,.13f,.15f);var faded=new Color(.33f,.33f,.33f);var pen=new Color(.13f,.2f,.48f);var stamp=new Color(.62f,.12f,.1f);
   var hand=Resources.Load<Font>("Fonts/Notes/Kalam-Regular");
   Label(new Rect(366,76,560,24),"HOLLIS COUNTY  CIVIL PROCESS",16,ink,null,true);Label(new Rect(366,100,560,22),"EVENING ROUTE  /  "+d.Date.ToUpperInvariant(),14,faded,null,true);Block(new Rect(366,128,548,2),ink);
   for(int i=0;i<d.Docket.Count;i++){var e=d.Docket[i];var p=d.Property(e.Property);float y=146+i*88;bool current=d.Guide&&d.Guide.Target==e.Property;
    Label(new Rect(366,y,540,26),(i+1).ToString("00")+"  "+e.Address.ToUpperInvariant(),19,ink,null,true);
    Label(new Rect(402,y+24,500,20),p.Brief,13,faded,null,true);
    var dir=Label(new Rect(402,y+44,500,30),ServiceRouteGuide.Directions[Mathf.Clamp(p.Index,0,5)],17,pen);if(hand)dir.font=hand;dir.fontSize=17;
    if(e.Result!=ServiceResult.Pending){var st=Label(new Rect(770,y+4,150,30),e.Result==ServiceResult.Served?"SERVED":e.Result==ServiceResult.LeftAtDoor?"LEFT":"UNABLE",20,stamp,null,true);st.alignment=TextAnchor.MiddleCenter;st.rectTransform.localRotation=Quaternion.Euler(0,0,8);st.fontStyle=FontStyle.Bold;Block(new Rect(366,y+13,300,2),new Color(.2f,.2f,.2f,.55f));}
    else if(current){var mark=Label(new Rect(344,y-2,24,30),"▶",18,stamp,null,true);mark.alignment=TextAnchor.MiddleCenter;}
    Block(new Rect(366,y+80,548,1),new Color(.62f,.68f,.78f));}
   var foot=Label(new Rect(366,640,548,40),d.AllResolved?"All visits recorded. Back to the depot.":"E  knock     R  leave the papers     U  unable to serve",13,faded,null,true);
   Action(new Rect(40,600,260,36),"CLOSE  (TAB)",()=>{d.PaperOpen=false;d.SetCursor();});Action(new Rect(40,646,260,36),"END THE ROUTE EARLY",()=>d.RequestEarlyFinish());
  }
  void Footer(string text){Label(new Rect(66,590,1110,28),text,17,muted);Rule(64,627,1152);}
  void Map(){
   mapPanel=new Rect(89,190,530,378);var road=ServiceRouteMap.Road(d.Scene);var points=road.Concat(d.Docket.Select(e=>d.Property(e.Property).Door.position)).Append(d.Scene.Depot.position);worldBounds=ServiceRouteMap.Bounds(points);
   Block(new Rect(65,170,583,407),mapPaper);for(int i=1;i<8;i++)Line(new Vector2(65+i*73,170),new Vector2(65+i*73,577),1,new Color(.3f,.37f,.32f,.11f));for(int i=1;i<6;i++)Line(new Vector2(65,170+i*68),new Vector2(648,170+i*68),1,new Color(.3f,.37f,.32f,.11f));
   Vector2 MapPoint(Vector3 p)=>ServiceRouteMap.Project(p,worldBounds,mapPanel);
   for(int i=1;i<road.Length;i++){Line(MapPoint(road[i-1]),MapPoint(road[i]),3,ink);MapSegments++;}var back=d.Scene.GetComponentInChildren<ServiceReturnRoad>();if(back)for(int i=1;i<back.Points.Length;i++){Line(MapPoint(back.Points[i-1]),MapPoint(back.Points[i]),2,ink);MapSegments++;}
   for(int i=0;i<d.Docket.Count;i++){var e=d.Docket[i];var p=d.Property(e.Property);var path=p.ApproachRoute;var nearest=road.OrderBy(t=>(t-p.Gate.position).sqrMagnitude).First();Line(MapPoint(nearest),MapPoint(p.Gate.position),1.5f,ink);if(path!=null&&path.Length>0){for(int j=1;j<path.Length;j++)Line(MapPoint(path[j-1]),MapPoint(path[j]),1.5f,ink);Line(MapPoint(path[path.Length-1]),MapPoint(p.Door.position),1.5f,ink);}
    var pin=MapPoint(p.Door.position);var color=e.Result==ServiceResult.Pending?ink:new Color(.38f,.43f,.35f);Block(new Rect(pin.x-11,pin.y-11,22,22),color);Label(new Rect(pin.x-11,pin.y-12,22,24),(i+1).ToString(),15,mapPaper).alignment=TextAnchor.MiddleCenter;MapPins++;
    float y=174+i*72;int captured=i;var row=Action(new Rect(695,y,510,63),(i+1).ToString("00")+"   "+e.Address,()=>selected=captured);row.GetComponentInChildren<Text>().fontSize=21;row.GetComponentInChildren<Text>().alignment=TextAnchor.UpperLeft;Label(new Rect(740,y+31,450,28),p.Brief+"  /  "+Status(e.Result),16,muted);if(selected==i){Block(new Rect(680,y+2,2,50),accent);Block(new Rect(pin.x-14,pin.y+14,28,2),new Color(.55f,.24f,.1f));}
   }
   var depot=MapPoint(d.Scene.Depot.position);Block(new Rect(depot.x-4,depot.y-4,8,8),ink);Label(new Rect(depot.x+12,depot.y-12,100,24),"DEPOT",12,ink,null,true);
   Label(new Rect(603,184,30,25),"N",16,ink).alignment=TextAnchor.MiddleCenter;Line(new Vector2(618,215),new Vector2(618,237),2,ink);
   carPin=Area("Vehicle position",new Rect(0,0,10,10));Block(new Rect(-5,-5,10,10),new Color(.65f,.25f,.08f),carPin);carHeading=Area("Vehicle heading",new Rect(0,0,0,0),carPin);Block(new Rect(-1,-19,2,14),new Color(.65f,.25f,.08f),carHeading);
   Label(new Rect(695,554,500,27),"Amber marker: your vehicle and direction",16,accent);Footer("North is up. Numbers correspond to your current docket.");Action(new Rect(66,639,275,36),"Close document",()=>{d.PaperOpen=false;d.SetCursor();});Action(new Rect(928,639,270,36),"View field docket",()=>d.MapOpen=false);
  }
  void Playing(string context){bool talking=d.Dialogue&&d.Dialogue.Active;
   // Fears to Fathom style: a small centre dot on foot, prompts just under it, named subtitles and numbered replies while talking.
   if(!d.Player.InCar&&!talking&&!d.Horror.Caught){Block(new Rect(638,358,4,4),new Color(1,1,1,.55f));if(context.Length>0&&d.NoteOpen<0)Ring(640,360,20);}
   if(d.NoteOpen>=0&&!d.Player.InCar){HeldNote(d.NoteOpen);return;}
   if(context.Length>0&&!talking&&(d.Player.InCar||!d.Notice.Contains('\n'))){if(d.Player.InCar)Label(new Rect(160,658,960,35),context,19).alignment=TextAnchor.MiddleCenter;else Label(new Rect(340,376,600,30),context,17,new Color(.93f,.93f,.9f,.92f)).alignment=TextAnchor.MiddleCenter;}
   if(talking){var dl=d.Dialogue;Label(new Rect(205,552,870,26),dl.Speaker.ToUpperInvariant(),15,accent).alignment=TextAnchor.MiddleCenter;Label(new Rect(185,578,910,64),dl.Line,21).alignment=TextAnchor.MiddleCenter;
    if(dl.Choices.Length>0){Block(new Rect(360,412,560,dl.Choices.Length*38+22),new Color(0,0,0,.62f));for(int i=0;i<dl.Choices.Length;i++)Label(new Rect(384,423+i*38,520,34),(i+1)+"    "+dl.Choices[i],18);}
    return;}if(d.Player.InCar){bool hints=d.NightIndex==0&&d.ShiftCardTime<28;if(hints)Label(new Rect(724,25,512,28),"TAB  CLIPBOARD      V  RADIO      B  TUNE",13,muted).alignment=TextAnchor.MiddleRight;if(d.Vehicle&&d.Vehicle.RadioOn)Label(new Rect(724,hints?55:25,512,27),d.Vehicle.StationName,15,accent).alignment=TextAnchor.MiddleRight;}
   // V20 objective (top left, Fears to Fathom style): the next address and where it lies from here.
   if(!d.Horror.Active&&d.Guide&&d.Guide.Objective.Length>0&&d.NoteOpen<0&&!d.Horror.Caught){Label(new Rect(44,28,760,26),d.Guide.Objective,15,new Color(.93f,.93f,.89f,.86f));if(d.Guide.Direction.Length>0)Label(new Rect(44,55,760,24),d.Guide.Direction,13,accent);}
   if(d.Horror.Active){
    if(!string.IsNullOrEmpty(d.Horror.Headline)){Label(new Rect(44,34,500,26),d.Horror.Headline.ToUpperInvariant(),15,accent);Rule(44,62,40,accent);}
    var line=Label(new Rect(205,568,870,78),string.IsNullOrEmpty(d.Notice)?d.Horror.Instruction:d.Notice,21);line.alignment=TextAnchor.MiddleCenter;line.fontStyle=FontStyle.Italic;
   }
   else if(!string.IsNullOrEmpty(d.Notice)&&!d.Horror.Caught){
    bool posted=d.Notice.Contains("\n");
    if(posted){Block(new Rect(298,399,684,230),panel);Rule(322,419,636,accent);Label(new Rect(328,433,624,178),d.Notice,22).alignment=TextAnchor.MiddleLeft;}
    else Label(new Rect(205,568,870,78),d.Notice,20).alignment=TextAnchor.MiddleCenter;
   }
   if(d.Horror.Phase==PursuitPhase.Attack){contactShade=Block(new Rect(-1000,-1000,3280,2720),Color.clear);}
   if(d.Horror.Phase==PursuitPhase.Caught){Shade(1);Label(new Rect(58,34,400,40),"◀◀ REWIND",25);Label(new Rect(190,290,900,90),d.Horror.DeathLine,26).alignment=TextAnchor.MiddleCenter;Label(new Rect(190,392,900,30),"RETRYING FROM THE PROPERTY GATE",15,muted).alignment=TextAnchor.MiddleCenter;}
  }
  // Fears to Fathom style: a ring around the dot when something can be used.
  static Texture2D ringTex;
  void Ring(float cx,float cy,float size){
   if(!ringTex){const int n=64;ringTex=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var px=new Color[n*n];for(int y=0;y<n;y++)for(int x=0;x<n;x++){float r=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(n*.5f,n*.5f));px[y*n+x]=new Color(1,1,1,Mathf.Clamp01(1-Mathf.Abs(r-27f)/2.4f));}ringTex.SetPixels(px);ringTex.Apply();}
   var t=Area("Use ring",new Rect(cx-size*.5f,cy-size*.5f,size,size));var img=t.gameObject.AddComponent<RawImage>();img.texture=ringTex;img.color=new Color(1,1,1,.62f);img.raycastTarget=false;
  }
  // The handwritten note taped to a door, held up to read: each writer has their own paper, hand and ink.
  static readonly string[] noteFont={"","Fonts/Notes/NothingYouCouldDo","Fonts/Notes/IndieFlower-Regular","Fonts/Notes/ReenieBeanie","Fonts/Notes/Kalam-Regular","Fonts/Notes/ShadowsIntoLight"};
  static readonly Color[] notePaper={Color.white,new Color(.925f,.898f,.831f),new Color(.925f,.898f,.831f),new Color(.867f,.808f,.667f),new Color(.941f,.941f,.918f),new Color(.957f,.953f,.933f)};
  static readonly Color[] noteInk={Color.black,new Color(.15f,.16f,.27f),new Color(.16f,.13f,.12f),new Color(.27f,.26f,.24f),new Color(.12f,.16f,.43f),new Color(.09f,.09f,.1f)};
  static string Flow(string text){var parts=text.Split('\n');var sb=new System.Text.StringBuilder();foreach(var raw in parts){if(sb.Length==0)sb.Append(raw);else if(raw.StartsWith("- "))sb.Append("\n").Append(raw);else sb.Append(' ').Append(raw);}return sb.ToString();}
  void HeldNote(int i){
   i=Mathf.Clamp(i,0,5);var text=d.Property(i).NoticeText;if(string.IsNullOrEmpty(text))return;
   Block(new Rect(-1000,-1000,3280,2720),new Color(0,0,0,.5f));
   var shadow=Block(new Rect(466,78,360,476),new Color(0,0,0,.35f));shadow.rectTransform.localRotation=Quaternion.Euler(0,0,1.4f);
   var paper=Block(new Rect(458,70,360,476),notePaper[i]);paper.rectTransform.localRotation=Quaternion.Euler(0,0,1.4f);
   if(i==4){for(int y=62;y<470;y+=34)Block(new Rect(0,y,360,1.5f),new Color(.59f,.7f,.83f),paper.transform);Block(new Rect(40,0,1.5f,476),new Color(.81f,.47f,.47f),paper.transform);}
   if(i==5){Block(new Rect(0,40,360,2),new Color(.82f,.43f,.43f),paper.transform);for(int y=74;y<470;y+=34)Block(new Rect(0,y,360,1.5f),new Color(.63f,.75f,.87f),paper.transform);}
   var label=Label(new Rect(i==4?52:26,i==5||i==4?44:30,i==4?290:310,410),Flow(text),30,noteInk[i],paper.transform);
   var hand=Resources.Load<Font>(noteFont[i]);if(hand)label.font=hand;label.alignment=TextAnchor.UpperLeft;label.resizeTextForBestFit=true;label.resizeTextMinSize=16;label.resizeTextMaxSize=i==3?30:34;label.lineSpacing=i==4||i==5?1.12f:1.02f;
   Label(new Rect(340,586,600,26),"E   Stop reading",15,muted).alignment=TextAnchor.MiddleCenter;
  }
  void Report(){Shade(.96f);Label(new Rect(95,69,900,65),"Return of service",42);Label(new Rect(98,148,900,35),d.Date.ToLowerInvariant()+"  /  "+(d.EndedEarly?"Route closed early":"Route filed"),19,muted);Rule(95,211,1090);for(int i=0;i<d.Docket.Count;i++){var e=d.Docket[i];Label(new Rect(98,244+i*56,700,40),e.Address,22);Label(new Rect(858,244+i*56,330,40),Status(e.Result),18,e.Result==ServiceResult.Pending?muted:accent);}Rule(95,550,1090);Label(new Rect(98,572,700,32),"Trip recorded   "+d.TripMiles.ToString("0.0")+" mi",17,muted);Action(new Rect(98,631,600,40),d.NightIndex<2?"Continue to the next shift":"Go home",()=>d.NextShift());}
  public static string Status(ServiceResult result)=>result==ServiceResult.Served?"Served directly":result==ServiceResult.LeftAtDoor?"Notice left":result==ServiceResult.Unable?"Unable to serve":"Pending";
 }
}
