using UnityEngine;
using UnityEngine.UI;
namespace ServiceGameV2 {
 // V27 ("I find myself missing the driveways a lot... could use little waypoints, something to point you where you need
 // to turn other than a little tool tip in the top left"): a small mark over the mouth of the next drive while you are
 // on the road, and over the door once you are up the drive on foot. Fades with distance; off inside houses and during
 // anything frightening. Options > Game > GUIDE MARKER turns it off.
 public sealed class ServiceGuideMarker27:MonoBehaviour {
  ServiceDirector d;Canvas canvas;Text mark,label;CanvasGroup group;
  public const string Pref="SERVICE.guidemarker27";
  public static bool On=true;
  public bool Showing=>group&&group.alpha>.05f;public Vector3 Point {get;private set;}
  public void Initialize(ServiceDirector director){d=director;On=PlayerPrefs.GetInt(Pref,1)==1;
   var o=new GameObject("V27 guide marker",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));o.transform.SetParent(transform,false);canvas=o.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=40;
   var sc=o.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1280,720);sc.matchWidthOrHeight=1;group=o.GetComponent<CanvasGroup>();group.alpha=0;group.interactable=group.blocksRaycasts=false;
   var font=Resources.Load<Font>("Fonts/VT323-Regular");if(!font)font=Resources.Load<Font>("Fonts/CourierPrime-Regular");
   mark=Word(o.transform,"◇",30,font);label=Word(o.transform,"",16,font);}
  Text Word(Transform parent,string s,int size,Font f){var t=new GameObject("Mark",typeof(RectTransform),typeof(Text)).GetComponent<Text>();t.transform.SetParent(parent,false);var r=t.rectTransform;r.anchorMin=r.anchorMax=Vector2.zero;r.sizeDelta=new Vector2(200,40);t.font=f;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=new Color(.95f,.92f,.82f,1);t.raycastTarget=false;var sh=t.gameObject.AddComponent<Outline>();sh.effectColor=new Color(0,0,0,.8f);return t;}
  public static void Set(bool on){On=on;PlayerPrefs.SetInt(Pref,on?1:0);}
  void LateUpdate(){
   if(!d||!canvas)return;float want=0;
   if(On&&d.Phase==ServicePhase.Playing&&!d.PaperOpen&&d.NoteOpen<0&&!d.Horror.Active&&!d.Horror.Caught&&!(d.Scenes&&(d.Scenes.Frozen||d.Scenes.FinaleRunning||d.Scenes.PageOpen))&&d.Guide&&d.Guide.Target>=0&&!d.InsideVilla&&!(d.Timecard&&d.Timecard.Blocking)){
    var p=d.Property(d.Guide.Target);var cam=d.Scene.View;var eye=cam.transform.position;
    var mouth=p.ApproachRoute!=null&&p.ApproachRoute.Length>0?p.ApproachRoute[0]:p.Gate.position;
    bool onDrive=!d.Player.InCar&&p.ApproachRoute!=null&&Vector3.Distance(eye,p.Door.position)<Vector3.Distance(mouth,p.Door.position)+4f;
    Point=onDrive?p.Door.position+Vector3.up*2.3f:mouth+Vector3.up*2.2f;float dist=Vector3.Distance(eye,Point);
    var vp=cam.WorldToViewportPoint(Point);
    if(vp.z>0&&dist>(onDrive?5f:14f)&&dist<220f){var r=(RectTransform)canvas.transform;var size=r.sizeDelta;var pos=new Vector2(Mathf.Clamp01(vp.x)*size.x,Mathf.Clamp01(vp.y)*size.y);
     mark.rectTransform.anchoredPosition=pos;label.rectTransform.anchoredPosition=pos+new Vector2(0,-22);label.text=(p.Address.Split(' ')[0])+"   "+Mathf.RoundToInt(dist)+" M";
     want=Mathf.Clamp01(Mathf.InverseLerp(220f,120f,dist))*.85f;if(vp.x<0||vp.x>1||vp.y<0||vp.y>1)want*=.5f;}}
   group.alpha=Mathf.MoveTowards(group.alpha,want,Time.unscaledDeltaTime*2.5f);}
 }
}
