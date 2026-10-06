using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace ServiceGameV2 {
 // V25 playtest: "need it more insidious - like a person standing, then your character blinks" / "closing eyes and
 // opening to see her appear first, then disappear after blinking". In a few places your character blinks: the lids
 // close, and when they open someone is standing there - a woman (Renderpeople's Sophia, held nearly still), facing you.
 // A few seconds later you blink again and she is gone. Once a night each, where she can be seen:
 //   night one, Morrow's: at the desk when you come up to the landing
 //   night two, Bell's: in the hall between you and the front door once the papers are down
 //   night three, Route 9: at the roadside in the headlights
 public sealed class ServiceBlink:MonoBehaviour {
  ServiceDirector d;GameObject figure;Animator figureAnim;Canvas canvas;RectTransform top,bottom;float closed;bool running;
  readonly bool[] done=new bool[3];Vector3 bellSpot;bool bellSpotSet;float nextCheck;
  public int Blinks {get;private set;} public int Shows {get;private set;} public int LastSpot {get;private set;}=-1;
  public bool FigureShown=>figure&&figure.activeSelf;public Vector3 FigureAt=>figure?figure.transform.position:Vector3.zero;public float Closed=>closed;
  public static bool Force; // tests: a spot fires as soon as it can be seen
  public void Initialize(ServiceDirector director){d=director;
   var t=d.Scene.transform.Find("V25 apparition");figure=t?t.gameObject:null;if(figure){figure.SetActive(false);figureAnim=figure.GetComponentInChildren<Animator>(true);}
   var go=new GameObject("V25 eyelids");go.transform.SetParent(transform,false);canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=480;
   var scaler=go.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
   top=Lid("Upper lid",go.transform,true);bottom=Lid("Lower lid",go.transform,false);Apply();}
  RectTransform Lid(string name,Transform parent,bool upper){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
   r.anchorMin=new Vector2(0,upper?1:0);r.anchorMax=new Vector2(1,upper?1:0);r.pivot=new Vector2(.5f,upper?1:0);r.sizeDelta=new Vector2(0,0);
   // a soft edge: the lid itself, and a fainter band ahead of it
   var img=r.gameObject.AddComponent<Image>();img.color=Color.black;img.raycastTarget=false;
   var edge=new GameObject("edge",typeof(RectTransform)).GetComponent<RectTransform>();edge.SetParent(r,false);edge.anchorMin=new Vector2(0,upper?0:1);edge.anchorMax=new Vector2(1,upper?0:1);edge.pivot=new Vector2(.5f,upper?1:0);edge.sizeDelta=new Vector2(0,46);
   var ei=edge.gameObject.AddComponent<Image>();ei.color=new Color(0,0,0,.55f);ei.raycastTarget=false;return r;}
  void Apply(){float h=Mathf.SmoothStep(0,1,closed)*372f;if(top)top.sizeDelta=new Vector2(0,h);if(bottom)bottom.sizeDelta=new Vector2(0,h);if(canvas)canvas.enabled=closed>.001f&&d&&d.Phase==ServicePhase.Playing;}
  void LateUpdate(){if(canvas&&d)canvas.enabled=closed>.001f&&d.Phase==ServicePhase.Playing;} // never over the pause menu or a card
  public void ResetForShift(){StopAllCoroutines();running=false;closed=0;Apply();System.Array.Clear(done,0,3);bellSpotSet=false;if(figure)figure.SetActive(false);}
  IEnumerator Lids(float to,float seconds){float from=closed;float t=0;while(t<seconds){t+=Time.deltaTime;closed=Mathf.Lerp(from,to,t/seconds);Apply();yield return null;}closed=to;Apply();}
  public IEnumerator Blink(float hold=.08f){Blinks++;yield return Lids(1,.09f);yield return new WaitForSeconds(hold);yield return Lids(0,.16f);}
  bool Calm=>d.Phase==ServicePhase.Playing&&!d.Busy&&!d.PaperOpen&&d.NoteOpen<0&&!(d.Dialogue&&d.Dialogue.Active)&&!d.Horror.Active&&!d.Horror.Caught&&!(d.Timecard&&d.Timecard.Blocking);
  bool Seen(Vector3 feet,float near,float far,bool legs=true){var cam=d.Scene.View;var head=feet+Vector3.up*1.5f;float dist=Vector3.Distance(cam.transform.position,head);if(dist<near||dist>far)return false;
   var vp=cam.WorldToViewportPoint(head);if(vp.z<=0||vp.x<.2f||vp.x>.8f||vp.y<.15f||vp.y>.95f)return false;
   return ServiceInteraction.Clear(cam.transform.position,head,null,d.Scene.Walker.transform)&&(!legs||ServiceInteraction.Clear(cam.transform.position,feet+Vector3.up*.6f,null,d.Scene.Walker.transform));}
  void Update(){
   if(!d||!figure||running||Time.time<nextCheck||!Calm)return;nextCheck=Time.time+.1f;
   var w=d.Scene.Walker.transform.position;
   // night one, Morrow's: at the desk as you come up to the landing (before the papers are down)
   if(!done[0]&&d.NightIndex==0&&d.ResultAt(5)==ServiceResult.Pending){var p=d.Property(5);if(p&&p.TableApproach&&ServiceLife.Indoors(p,w)&&w.y>p.TableApproach.position.y-.8f){var at=p.TableApproach.position;
     if(Seen(at,3.5f,12f)){done[0]=true;StartCoroutine(Show(0,at,3.2f,ServiceScript.ApparitionMorrow));return;}}}
   // night two, Bell's: in the hall between you and the front door once the papers are down
   if(!done[1]&&d.NightIndex==1&&d.ResultAt(4)!=ServiceResult.Pending){var p=d.Property(4);if(p&&ServiceLife.Indoors(p,w)){
     if(!bellSpotSet){bellSpotSet=HallSpot(p,w,out bellSpot);}
     if(bellSpotSet&&Seen(bellSpot,3.2f,11f)){done[1]=true;StartCoroutine(Show(1,bellSpot,2.8f,ServiceScript.ApparitionBell));return;}}}
   // night three, Route 9: at the roadside in the headlights
   if(!done[2]&&d.NightIndex==2&&d.Player.InCar&&d.Scene.LateRoad&&d.Scene.LateRoad.activeInHierarchy&&d.Scene.LateRoute!=null&&d.Scene.LateRoute.Length>5){
    var spot=RoadSpot();if(Seen(spot,14f,60f,false)){ // from the road, the verge grass can hide her legs: her head and shoulders in the lights are enough
     done[2]=true;StartCoroutine(Show(2,spot,1.7f,ServiceScript.ApparitionRoad));}}
  }
  public Vector3 RoadSpot(){var a=d.Scene.LateRoute[4].position;var b=d.Scene.LateRoute[5].position;var f=b-a;f.y=0;f.Normalize();var spot=a+Vector3.Cross(Vector3.up,f)*3.6f;
   if(UnityEngine.AI.NavMesh.SamplePosition(spot,out var s,2f,UnityEngine.AI.NavMesh.AllAreas))spot=s.position;else{var tr=Terrain.activeTerrain;if(tr)spot.y=tr.SampleHeight(spot)+tr.transform.position.y;}return spot;}
  static string Blocker(Vector3 from,Vector3 to,Transform observer){var delta=to-from;foreach(var h in Physics.RaycastAll(from,delta.normalized,delta.magnitude-.04f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore).OrderBy(x=>x.distance)){if(observer&&(h.transform==observer||h.transform.IsChildOf(observer)))continue;if(h.distance<delta.magnitude-.12f)return h.collider.name+"@"+h.distance.ToString("F1")+" layer "+h.collider.gameObject.layer;}return "clear";}
  public string RoadDebug{get{if(!d||d.Scene.LateRoute==null||d.Scene.LateRoute.Length<6)return "no late route";var spot=RoadSpot();var cam=d.Scene.View;var head=spot+Vector3.up*1.5f;var vp=cam.WorldToViewportPoint(head);
   return $"spot {spot:F1} dist {Vector3.Distance(cam.transform.position,head):F1} vp ({vp.x:F2},{vp.y:F2},{vp.z:F1}) head {Blocker(cam.transform.position,head,d.Scene.Walker.transform)} legs {Blocker(cam.transform.position,spot+Vector3.up*.6f,d.Scene.Walker.transform)} done {done[2]} night {d.NightIndex} inCar {d.Player.InCar} calm {Calm}";}}
  // the hall spot: on the walkable way from where you stand to the front door, a little over half way and not on top of you
  public static bool HallSpot(ServiceProperty p,Vector3 from,out Vector3 spot){spot=Vector3.zero;var path=new UnityEngine.AI.NavMeshPath();
   if(!UnityEngine.AI.NavMesh.SamplePosition(from,out var a,1.5f,UnityEngine.AI.NavMesh.AllAreas)||!UnityEngine.AI.NavMesh.SamplePosition(p.Door.position+(p.InteriorBounds.center-p.Door.position).normalized*1.2f,out var b,2f,UnityEngine.AI.NavMesh.AllAreas))return false;
   if(!UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,UnityEngine.AI.NavMesh.AllAreas,path)||path.corners.Length<2)return false;
   float total=0;for(int i=1;i<path.corners.Length;i++)total+=Vector3.Distance(path.corners[i-1],path.corners[i]);if(total<4.5f)return false;
   float need=Mathf.Max(3.6f,total*.58f);for(int i=1;i<path.corners.Length;i++){float len=Vector3.Distance(path.corners[i-1],path.corners[i]);if(len>=need){spot=Vector3.Lerp(path.corners[i-1],path.corners[i],need/len);return true;}need-=len;}
   spot=path.corners[path.corners.Length-1];return true;}
  IEnumerator Show(int which,Vector3 at,float hold,string line){running=true;Shows++;LastSpot=which;
   yield return Lids(1,.09f);Blinks++;
   var face=d.Scene.View.transform.position-at;face.y=0;figure.transform.SetPositionAndRotation(at,Quaternion.LookRotation(face.sqrMagnitude>.01f?face.normalized:Vector3.forward));figure.SetActive(true);
   // held in her idle, barely breathing - a person standing where no one should be
   if(figureAnim){figureAnim.Play(0,0,Random.Range(.2f,.6f));figureAnim.Update(0);figureAnim.speed=.06f;}
   yield return new WaitForSeconds(.1f);yield return Lids(0,.18f);if(d.Dread)d.Dread.Pulse(.45f);
   float t=0;while(t<hold){t+=Time.deltaTime;var dd=d.Scene.View.transform.position-at;dd.y=0;if(dd.magnitude<2.6f)break;
    if(dd.sqrMagnitude>.01f)figure.transform.rotation=Quaternion.RotateTowards(figure.transform.rotation,Quaternion.LookRotation(dd.normalized),40*Time.deltaTime);yield return null;}
   Blinks++;yield return Lids(1,.08f);figure.SetActive(false);yield return new WaitForSeconds(.12f);yield return Lids(0,.2f);
   d.Audio.HorrorAt("woodstress",at+Vector3.up*.3f,.22f);
   yield return new WaitForSeconds(.9f);if(!string.IsNullOrEmpty(line)&&string.IsNullOrEmpty(d.Notice))d.Say(line);
   running=false;}
 }
}
