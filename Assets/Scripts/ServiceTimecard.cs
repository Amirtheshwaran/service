using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace ServiceGameV2 {
 // V21: time cards in the manner of Fears to Fathom - the screen goes black and the date and time are typed out a
 // key at a time ("9:48 PM"), hold, then the picture fades up. Used when a night begins, when the report is filed
 // (the time you got back), and after a capture rewinds you to the gate. Arriving at an address types its number and
 // the time small in the corner instead, over the picture.
 // The clock runs from each night's scripted start at seven minutes for every real minute.
 public sealed class ServiceTimecard:MonoBehaviour {
  ServiceDirector d;Canvas canvas;Image black;Text upper,lower,cornerA,cornerB;CanvasGroup cardWords,cornerWords;Font font;AudioSource keys;AudioClip[] clicks;
  Coroutine card,stamp;readonly bool[] stamped=new bool[6];
  public bool Blocking {get;private set;}
  public bool CardUp=>card!=null;
  public string LastCard {get;private set;}="";public string LastStamp {get;private set;}="";public int Cards {get;private set;}public int Stamps {get;private set;}
  public const float ClockRate=7;
  public RectTransform StampRect=>cornerA?cornerA.rectTransform:null;public RectTransform CardRect=>lower?lower.rectTransform:null;
  public void Initialize(ServiceDirector director){
   d=director;font=Resources.Load<Font>("Fonts/CourierPrime-Regular");clicks=Resources.LoadAll<AudioClip>("Audio/V21/type");
   var o=new GameObject("Time cards",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));o.transform.SetParent(transform,false);canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;
   var sc=o.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1280,720);sc.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
   black=new GameObject("Black",typeof(RectTransform),typeof(Image)).GetComponent<Image>();black.transform.SetParent(o.transform,false);Stretch(black.rectTransform);black.color=new Color(0,0,0,0);black.raycastTarget=false;
   var words=new GameObject("Card words",typeof(RectTransform),typeof(CanvasGroup));words.transform.SetParent(o.transform,false);Stretch((RectTransform)words.transform);cardWords=words.GetComponent<CanvasGroup>();cardWords.alpha=0;
   upper=Line(words.transform,new Vector2(0,34),27,TextAnchor.MiddleCenter,new Color(.8f,.8f,.78f));lower=Line(words.transform,new Vector2(0,-30),58,TextAnchor.MiddleCenter,new Color(.93f,.93f,.9f));
   var corner=new GameObject("Corner stamp",typeof(RectTransform),typeof(CanvasGroup));corner.transform.SetParent(o.transform,false);{var ct=(RectTransform)corner.transform;ct.anchorMin=Vector2.zero;ct.anchorMax=Vector2.one;ct.offsetMin=ct.offsetMax=Vector2.zero;}/* exactly the screen: the stamp is placed from its top-left corner */cornerWords=corner.GetComponent<CanvasGroup>();cornerWords.alpha=0;
   cornerA=Line(corner.transform,new Vector2(0,0),22,TextAnchor.LowerLeft,new Color(.9f,.9f,.86f));cornerB=Line(corner.transform,new Vector2(0,0),20,TextAnchor.LowerLeft,new Color(.78f,.78f,.74f));
   Place(cornerA.rectTransform,new Vector2(58,-618));Place(cornerB.rectTransform,new Vector2(58,-650));
   keys=gameObject.AddComponent<AudioSource>();keys.playOnAwake=false;keys.spatialBlend=0;keys.ignoreListenerPause=false;
  }
  static void Stretch(RectTransform t){t.anchorMin=Vector2.zero;t.anchorMax=Vector2.one;t.offsetMin=new Vector2(-400,-400);t.offsetMax=new Vector2(400,400);}
  Text Line(Transform parent,Vector2 offset,int size,TextAnchor anchor,Color color){
   var t=new GameObject("Line",typeof(RectTransform),typeof(Text)).GetComponent<Text>();t.transform.SetParent(parent,false);var r=t.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(1100,60);r.anchoredPosition=offset;
   t.font=font;t.fontSize=size;t.alignment=anchor;t.color=color;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;t.text="";return t;}
  static void Place(RectTransform r,Vector2 topLeft){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.sizeDelta=new Vector2(900,30);r.anchoredPosition=topLeft;}
  // ---- clock
  // counts only time spent playing (not paused, not on the title), from the moment the night began
  float played;
  public System.DateTime Clock{get{var start=StartOf(d.NightIndex);return start.AddMinutes(played*ClockRate/60f);}}
  static System.DateTime StartOf(int night){var date=new System.DateTime(1998,10,new[]{1,4,9}[Mathf.Clamp(night,0,2)]);var t=new[]{"9:48 PM","10:21 PM","11:57 PM"}[Mathf.Clamp(night,0,2)];
   System.DateTime.TryParse(date.ToString("yyyy-MM-dd")+" "+t,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var at);return at==default?date.AddHours(21.8):at;}
  public static string Time12(System.DateTime t)=>t.ToString("h:mm tt",System.Globalization.CultureInfo.InvariantCulture);
  float Dt=>d&&d.Phase==ServicePhase.Paused?0:ServiceMenus.Dt;
  // ---- full-screen cards
  public void NightStart(){for(int i=0;i<6;i++)stamped[i]=false;Card(d.LongDate,d.ShiftTime,1.5f,true,null);}
  public void NightEnd(){var t=Clock;var floor=StartOf(d.NightIndex).AddMinutes(new[]{124,131,96}[Mathf.Clamp(d.NightIndex,0,2)]);if(t<floor)t=floor;Card("Hollis County depot",Time12(t),1.3f,false,null);}
  public void Rewound(){Card("",Time12(Clock),.9f,true,null);}
  public void Card(string top,string time,float hold,bool fromBlack,System.Action atBlack){
   if(card!=null)StopCoroutine(card);LastCard=(top+" "+time).Trim();Cards++;card=StartCoroutine(Run(top,time,hold,fromBlack,atBlack));}
  IEnumerator Run(string top,string time,float hold,bool fromBlack,System.Action atBlack){
   Blocking=true;upper.text=lower.text="";cardWords.alpha=1;
   // to black (instantly when a night begins, quickly otherwise)
   float a=fromBlack?1:black.color.a;while(a<1){a=Mathf.MoveTowards(a,1,Dt/.35f);black.color=new Color(0,0,0,a);yield return null;}black.color=Color.black;
   atBlack?.Invoke();
   yield return Wait(.55f);
   if(!string.IsNullOrEmpty(top)){yield return Type(upper,top,.045f);yield return Wait(.4f);}
   yield return Type(lower,time,.11f);
   yield return Wait(hold);
   // the words go, then the picture comes up
   float w=1;while(w>0){w=Mathf.MoveTowards(w,0,Dt/.45f);cardWords.alpha=w;yield return null;}
   float b=1;while(b>0){b=Mathf.MoveTowards(b,0,Dt/1.25f);black.color=new Color(0,0,0,b);if(b<.55f)Blocking=false;yield return null;}
   Blocking=false;upper.text=lower.text="";card=null;
  }
  IEnumerator Wait(float s){float t=0;while(t<s){t+=Dt;yield return null;}}
  IEnumerator Type(Text label,string text,float per){
   for(int i=1;i<=text.Length;i++){
    label.text=text.Substring(0,i)+(i<text.Length?"_":"");char c=text[i-1];
    if(c!=' '&&clicks.Length>0){keys.pitch=Random.Range(.92f,1.12f);keys.PlayOneShot(clicks[Random.Range(0,clicks.Length)],Random.Range(.32f,.45f));}
    yield return Wait(c==' '?per*.6f:per*Random.Range(.7f,1.35f));}
   label.text=text;
   // the caret blinks twice at the end of the line
   for(int k=0;k<2;k++){label.text=text+"_";yield return Wait(.22f);label.text=text;yield return Wait(.22f);}
  }
  // ---- corner stamps on arrival
  public void Stamp(string top,string time){if(stamp!=null)StopCoroutine(stamp);LastStamp=(top+" "+time).Trim();Stamps++;stamp=StartCoroutine(RunStamp(top,time));}
  IEnumerator RunStamp(string top,string time){
   cornerA.text=cornerB.text="";cornerWords.alpha=1;yield return Type(cornerA,top,.05f);yield return Type(cornerB,time,.07f);yield return Wait(3.2f);
   float w=1;while(w>0){w=Mathf.MoveTowards(w,0,Dt/.8f);cornerWords.alpha=w;yield return null;}cornerA.text=cornerB.text="";stamp=null;}
  void Update(){
   if(d&&d.Phase==ServicePhase.Playing)played+=Time.unscaledDeltaTime;
   // the corner stamp never sits over an encounter, a capture, a card or a menu
   if(stamp!=null&&d&&(d.Horror.Active||d.Horror.Caught||Blocking||d.NoteOpen>=0||d.Phase!=ServicePhase.Playing&&d.Phase!=ServicePhase.Paused)){StopCoroutine(stamp);stamp=null;cornerWords.alpha=0;cornerA.text=cornerB.text="";}
   if(!d||d.Phase!=ServicePhase.Playing||d.Player.InCar||Blocking||d.Horror.Active||d.Horror.Caught)return;
   var at=d.Scene.Walker.transform.position;
   foreach(var e in d.Docket){var p=d.Property(e.Property);if(stamped[p.Index]||!p.Door)continue;var dd=p.Door.position-at;dd.y=0;if(dd.magnitude>24)continue;stamped[p.Index]=true;Stamp(e.Address.ToUpperInvariant(),Time12(Clock));break;}
  }
  public void Clear(){played=0;if(card!=null)StopCoroutine(card);if(stamp!=null)StopCoroutine(stamp);card=stamp=null;Blocking=false;black.color=new Color(0,0,0,0);cardWords.alpha=0;cornerWords.alpha=0;upper.text=lower.text=cornerA.text=cornerB.text="";}
 }
}
