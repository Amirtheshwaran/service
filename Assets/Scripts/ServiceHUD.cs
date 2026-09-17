using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace ServiceGameV2 {
 public sealed class ServiceHUD:MonoBehaviour {
  ServiceDirector d;Canvas canvas;RectTransform root,carPin,carHeading;Image introShade;CanvasGroup introWords;Font sans,document;bool options,confirmNew;string last="";int selected=-1;Rect worldBounds,mapPanel;Button firstButton;
  readonly Color white=new Color(.88f,.89f,.86f),muted=new Color(.53f,.58f,.57f),accent=new Color(.72f,.57f,.36f),panel=new Color(.035f,.045f,.048f,.97f),ink=new Color(.16f,.20f,.19f),mapPaper=new Color(.63f,.65f,.59f);
  public int MapPins {get;private set;} public int MapSegments {get;private set;}
  public void Initialize(ServiceDirector director){d=director;sans=Resources.Load<Font>("Fonts/Barlow-Regular");document=Resources.Load<Font>("Fonts/CourierPrime-Regular");
   var o=new GameObject("Service interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));o.transform.SetParent(transform,false);canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;var scaler=o.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
   if(!FindAnyObjectByType<EventSystem>()){var events=new GameObject("Interface input",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
  }
  public void SmokeOptions(bool open){if(d.IsSmoke){options=open;last="";}}
  public bool Back(){if(options||confirmNew){options=confirmNew=false;d.SaveOptions();last="";return true;}return false;}
  RectTransform Area(string name,Rect rect,Transform parent=null){var o=new GameObject(name,typeof(RectTransform));var t=o.GetComponent<RectTransform>();t.SetParent(parent?parent:root,false);t.anchorMin=t.anchorMax=new Vector2(0,1);t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(rect.x,-rect.y);t.sizeDelta=rect.size;return t;}
  Image Block(Rect r,Color color,Transform parent=null){var t=Area("Surface",r,parent);var image=t.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
  Text Label(Rect r,string text,int size,Color? color=null,Transform parent=null,bool mono=false){var t=Area(text,r,parent);var label=t.gameObject.AddComponent<Text>();label.font=mono?document:sans;label.text=text;label.fontSize=size;label.color=color??white;label.alignment=TextAnchor.MiddleLeft;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;label.raycastTarget=false;return label;}
  Button Action(Rect r,string caption,System.Action action,bool prominent=false,Transform parent=null){var t=Area(caption,r,parent);var image=t.gameObject.AddComponent<Image>();image.color=Color.clear;var b=t.gameObject.AddComponent<Button>();var text=Label(new Rect(0,0,r.width,r.height),caption,prominent?27:20,null,t);b.targetGraphic=text;var colors=b.colors;colors.normalColor=white;colors.highlightedColor=Color.white;colors.selectedColor=new Color(1,.8f,.53f);colors.pressedColor=accent;colors.fadeDuration=.13f;b.colors=colors;b.onClick.AddListener(()=>{action();last="";});if(!firstButton)firstButton=b;return b;}
  void Rule(float x,float y,float w,Color? color=null){Block(new Rect(x,y,w,1),color??new Color(.23f,.29f,.28f));}
  void Line(Vector2 a,Vector2 b,float width,Color color){var t=Block(new Rect(a.x,a.y,Vector2.Distance(a,b),width),color).rectTransform;t.pivot=new Vector2(0,.5f);t.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);}
  string Context(){if(d.Busy||d.Horror.Caught)return "";if(d.Player.InCar)return d.CanFinish?"E   File shift report":d.Player.Speed<.8f?(d.Player.IsStarting?"Turning the ignition…":"E   Leave vehicle     /     Space   Ignition"):"";
   if(Vector3.Distance(d.Scene.View.transform.position,d.Scene.Car.position+Vector3.up)<3.5f)return "E   Enter vehicle";
   int front=d.NearbyKnockDoor();if(front>=0)return d.IsFriendly(front)?"E   Knock     /     R   Leave notice     /     U   No service":"E   Knock     /     U   No service";
   if(d.NearbyDoor()>=0)return "E / R   Leave the notice";
   int nearby=d.NearbyProperty();return nearby>=0?d.Property(nearby).Instructions:"";
  }
  void LateUpdate(){if(!d||!canvas)return;bool intro=d.Phase==ServicePhase.Title&&d.Presentation&&d.Presentation.IntroVisible;string context=Context();string state=d.Phase+"|"+d.PaperOpen+"|"+d.MapOpen+"|"+options+"|"+confirmNew+"|"+intro+"|"+d.Notice+"|"+d.Horror.Phase+"|"+context+"|"+d.NightIndex+"|"+(d.Vehicle?d.Vehicle.RadioOn+"/"+d.Vehicle.Station:"")+"|"+string.Join(",",d.Docket.Select(e=>(int)e.Result));
   if(last!=state){last=state;Rebuild(intro,context);}
   if(intro&&introShade){introShade.color=new Color(0,0,0,d.Presentation.IntroBackgroundAlpha);introWords.alpha=d.Presentation.IntroTextAlpha;}
   if(carPin){var pos=ServiceRouteMap.Project(d.Scene.Car.position,worldBounds,mapPanel);carPin.anchoredPosition=new Vector2(pos.x,-pos.y);carHeading.localRotation=Quaternion.Euler(0,0,-d.Scene.Car.eulerAngles.y);}
  }
  void Rebuild(bool intro,string context){if(root){root.gameObject.SetActive(false);Destroy(root.gameObject);}firstButton=null;carPin=carHeading=null;introShade=null;MapPins=MapSegments=0;root=Area("Screen",new Rect(0,0,1280,720),canvas.transform);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
   if(intro){introShade=Block(new Rect(-1000,-1000,3280,2720),Color.black);var words=Area("Headphones",new Rect(0,0,1280,720));introWords=words.gameObject.AddComponent<CanvasGroup>();Label(new Rect(0,290,1280,52),"Headphones recommended",30,null,words).alignment=TextAnchor.MiddleCenter;Label(new Rect(0,348,1280,35),"Listen closely. Not everything announces itself.",18,muted,words).alignment=TextAnchor.MiddleCenter;Label(new Rect(0,636,1280,30),"Press any key to continue",15,muted,words).alignment=TextAnchor.MiddleCenter;return;}
   if(d.Phase==ServicePhase.Title)Title();else if(d.Phase==ServicePhase.Paused)Pause();else if(d.Phase==ServicePhase.Report||d.Phase==ServicePhase.Finished)Report();else if(d.PaperOpen)Documents();else Playing(context);
   if(firstButton&&EventSystem.current)EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
  }
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
   Action(new Rect(95,484,445,34),"Lightning flashes    "+(d.Storm.FlashesEnabled?"On":"Off"),()=>d.Storm.FlashesEnabled=!d.Storm.FlashesEnabled);Action(new Rect(95,531,445,34),"Display    "+(Screen.fullScreen?"Full screen":"Windowed"),()=>Screen.fullScreen=!Screen.fullScreen);
   Label(new Rect(659,152,420,35),"Controls",24);string[] keys={"W A S D","Mouse","Shift","Ctrl","Space","Q / E","E","R / U","F","Tab / M","V / B","Esc"};string[] actions={"Move / drive","Look","Sprint / brake","Hold to crouch","Jump / ignition in car","Look behind while sprinting","Interact / knock","Leave notice / no service","Flashlight","Docket / route map","Radio power / station","Pause / close document"};for(int i=0;i<keys.Length;i++){Label(new Rect(659,201+i*30,112,28),keys[i],17,accent);Label(new Rect(797,201+i*30,390,28),actions[i],18);}
   Rule(95,627,1090);Action(new Rect(95,642,300,40),"Back",()=>{options=false;d.SaveOptions();});Label(new Rect(659,647,520,32),"Camera movement and blur can be disabled independently.",15,muted);
  }
  void Documents(){Shade(.93f);Label(new Rect(64,40,680,60),d.MapOpen?"County road index":"Evening assignments",38);Label(new Rect(66,103,620,26),d.Date.ToLowerInvariant()+"  /  Hollis County civil process",16,muted);Action(new Rect(945,60,270,35),d.MapOpen?"Tab   Field docket":"M   Route map",()=>d.MapOpen=!d.MapOpen);Rule(64,148,1152);
   if(d.MapOpen){Map();return;}
   Label(new Rect(65,177,750,28),"Read the property cues. Record the outcome of each visit.",18,muted);
   for(int i=0;i<d.Docket.Count;i++){var e=d.Docket[i];var p=d.Property(e.Property);float y=225+i*67;Label(new Rect(66,y,58,32),(i+1).ToString("00"),20,accent);Label(new Rect(135,y,650,30),e.Address,22);Label(new Rect(135,y+29,680,27),p.Brief+"  ·  "+(d.IsFriendly(p.Index)?"Knock at the front door":p.Index==1||p.Index==5?"Delivery upstairs":"Delivery inside"),16,muted);Label(new Rect(916,y,295,35),Status(e.Result),17,e.Result==ServiceResult.Pending?muted:white);Rule(135,y+63,1080);}
   Footer(d.AllResolved?"All visits recorded. Return to the depot.":"E  Knock / interact     R  Leave notice     U  Unable to serve");Action(new Rect(66,639,260,36),"Close document",()=>{d.PaperOpen=false;d.SetCursor();});Action(new Rect(924,639,290,36),"End route early",()=>d.RequestEarlyFinish());
  }
  void Footer(string text){Label(new Rect(66,590,1110,28),text,17,muted);Rule(64,627,1152);}
  void Map(){
   mapPanel=new Rect(89,190,530,378);var road=ServiceRouteMap.Road(d.Scene);var points=road.Concat(d.Docket.Select(e=>d.Property(e.Property).Door.position)).Append(d.Scene.Depot.position);worldBounds=ServiceRouteMap.Bounds(points);
   Block(new Rect(65,170,583,407),mapPaper);for(int i=1;i<8;i++)Line(new Vector2(65+i*73,170),new Vector2(65+i*73,577),1,new Color(.3f,.37f,.32f,.11f));for(int i=1;i<6;i++)Line(new Vector2(65,170+i*68),new Vector2(648,170+i*68),1,new Color(.3f,.37f,.32f,.11f));
   Vector2 MapPoint(Vector3 p)=>ServiceRouteMap.Project(p,worldBounds,mapPanel);
   for(int i=1;i<road.Length;i++){Line(MapPoint(road[i-1]),MapPoint(road[i]),3,ink);MapSegments++;}
   for(int i=0;i<d.Docket.Count;i++){var e=d.Docket[i];var p=d.Property(e.Property);var path=p.ApproachRoute;var nearest=road.OrderBy(t=>(t-p.Gate.position).sqrMagnitude).First();Line(MapPoint(nearest),MapPoint(p.Gate.position),1.5f,ink);for(int j=1;j<path.Length;j++)Line(MapPoint(path[j-1]),MapPoint(path[j]),1.5f,ink);Line(MapPoint(path[path.Length-1]),MapPoint(p.Door.position),1.5f,ink);
    var pin=MapPoint(p.Door.position);var color=e.Result==ServiceResult.Pending?ink:new Color(.38f,.43f,.35f);Block(new Rect(pin.x-11,pin.y-11,22,22),color);Label(new Rect(pin.x-11,pin.y-12,22,24),(i+1).ToString(),15,mapPaper).alignment=TextAnchor.MiddleCenter;MapPins++;
    float y=174+i*72;int captured=i;var row=Action(new Rect(695,y,510,63),(i+1).ToString("00")+"   "+e.Address,()=>selected=captured);row.GetComponentInChildren<Text>().fontSize=21;row.GetComponentInChildren<Text>().alignment=TextAnchor.UpperLeft;Label(new Rect(740,y+31,450,28),p.Brief+"  /  "+Status(e.Result),16,muted);if(selected==i){Block(new Rect(680,y+2,2,50),accent);Block(new Rect(pin.x-14,pin.y+14,28,2),new Color(.55f,.24f,.1f));}
   }
   var depot=MapPoint(d.Scene.Depot.position);Block(new Rect(depot.x-4,depot.y-4,8,8),ink);Label(new Rect(depot.x+12,depot.y-12,100,24),"DEPOT",12,ink,null,true);
   Label(new Rect(603,184,30,25),"N",16,ink).alignment=TextAnchor.MiddleCenter;Line(new Vector2(618,215),new Vector2(618,237),2,ink);
   carPin=Area("Vehicle position",new Rect(0,0,10,10));Block(new Rect(-5,-5,10,10),new Color(.65f,.25f,.08f),carPin);carHeading=Area("Vehicle heading",new Rect(0,0,0,0),carPin);Block(new Rect(-1,-19,2,14),new Color(.65f,.25f,.08f),carHeading);
   Label(new Rect(695,554,500,27),"Amber marker: your vehicle and direction",16,accent);Footer("North is up. Numbers correspond to your current docket.");Action(new Rect(66,639,275,36),"Close document",()=>{d.PaperOpen=false;d.SetCursor();});Action(new Rect(928,639,270,36),"View field docket",()=>d.MapOpen=false);
  }
  void Playing(string context){if(context.Length>0)Label(new Rect(160,658,960,35),context,19).alignment=TextAnchor.MiddleCenter;if(d.Player.InCar){Label(new Rect(934,25,302,28),"Tab  Docket     M  Route map",16,muted);if(d.Vehicle&&d.Vehicle.RadioOn)Label(new Rect(925,59,311,27),d.Vehicle.StationName,16,accent);Label(new Rect(975,92,270,26),"V  Radio     B  Tune",14,muted);}
   if(!string.IsNullOrEmpty(d.Notice)&&!d.Horror.Active&&!d.Horror.Caught)Label(new Rect(205,595,870,48),d.Notice,20).alignment=TextAnchor.MiddleCenter;
   if(d.Horror.Active){Label(new Rect(240,67,800,38),d.Horror.Headline,24).alignment=TextAnchor.MiddleCenter;Label(new Rect(220,111,840,50),d.Horror.Instruction,19).alignment=TextAnchor.MiddleCenter;}
   if(d.Horror.Caught){Shade(1);Label(new Rect(230,310,820,65),d.Horror.DeathLine,26).alignment=TextAnchor.MiddleCenter;Label(new Rect(230,388,820,30),"The road brings you back.",17,muted).alignment=TextAnchor.MiddleCenter;}
  }
  void Report(){Shade(.96f);Label(new Rect(95,69,900,65),"Return of service",42);Label(new Rect(98,148,900,35),d.Date.ToLowerInvariant()+"  /  "+(d.EndedEarly?"Route closed early":"Route filed"),19,muted);Rule(95,211,1090);for(int i=0;i<d.Docket.Count;i++){var e=d.Docket[i];Label(new Rect(98,244+i*56,700,40),e.Address,22);Label(new Rect(858,244+i*56,330,40),Status(e.Result),18,e.Result==ServiceResult.Pending?muted:accent);}Rule(95,550,1090);Label(new Rect(98,572,700,32),"Trip recorded   "+d.TripMiles.ToString("0.0")+" mi",17,muted);Action(new Rect(98,631,600,40),d.NightIndex<2?"Continue to the next shift":"Return to title",()=>{if(d.NightIndex<2)d.NextShift();else d.Title();});}
  public static string Status(ServiceResult result)=>result==ServiceResult.Served?"Served directly":result==ServiceResult.LeftAtDoor?"Notice left":result==ServiceResult.Unable?"Unable to serve":"Pending";
 }
}
