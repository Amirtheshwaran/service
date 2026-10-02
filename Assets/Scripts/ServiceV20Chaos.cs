using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace ServiceGameV2 {
 // V20 chaos test: plays the game the way people break games, through the real keyboard and mouse path
 // (Input System state events), and watches for anything that goes wrong. Run:
 //   Service.exe -serviceChaos [-chaosMinutes=4] [-chaosSeed=7]   with SERVICE_CAPTURE_DIR set.
 // It uses its own save slot (SERVICE.chaos.*) and puts the player's option preferences back when it finishes.
 // Report: chaos-report.txt (+ a screenshot the first time each kind of problem appears).
 public sealed class ServiceV20Chaos:MonoBehaviour {
  ServiceDirector d;string dir;System.Random rng;readonly List<string> log=new List<string>();readonly Dictionary<string,int> faults=new Dictionary<string,int>();
  readonly Dictionary<string,string> firstSeen=new Dictionary<string,string>();
  readonly HashSet<Key> held=new HashSet<Key>();Vector2 mouseDelta;bool mouseDown;string scenario="start";
  float busySince=-1,blockedSince=-1,insideSince=-1,lastFrame;Vector3 lastWalker;int frames;
  static readonly string[] PrefKeys={"SERVICE.camcorder","SERVICE.brightness","SERVICE.volume","SERVICE.music","SERVICE.sensitivity","SERVICE.motion","SERVICE.blur","SERVICE.lightning"};
  readonly Dictionary<string,(bool has,float f,int i)> prefs=new Dictionary<string,(bool,float,int)>();
  public void Run(ServiceDirector director){
   d=director;dir=Environment.GetEnvironmentVariable("SERVICE_CAPTURE_DIR");if(string.IsNullOrEmpty(dir))dir=Path.Combine(Application.persistentDataPath,"chaos");Directory.CreateDirectory(dir);
   var a=Environment.GetCommandLineArgs();int seed=7;var sa=a.FirstOrDefault(x=>x.StartsWith("-chaosSeed="));if(sa!=null)int.TryParse(sa.Substring(11),out seed);rng=new System.Random(seed);
   foreach(var k in PrefKeys)prefs[k]=(PlayerPrefs.HasKey(k),PlayerPrefs.GetFloat(k,0),PlayerPrefs.GetInt(k,0));
   // the test window is usually not focused: keep running and keep reading the (injected) devices anyway
   Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
   Application.logMessageReceived+=OnLog;StartCoroutine(Main());
  }
  void OnLog(string msg,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Fault("log-"+type,msg.Split('\n')[0]+" | "+(stack??"").Split('\n').FirstOrDefault());}
  void Note(string s){log.Add($"[{Time.realtimeSinceStartup,7:F1}] {scenario}: {s}");}
  void Fault(string kind,string detail){
   var place=detail.IndexOf(" at (");string key=kind+"|"+(place>0?detail.Substring(0,place):(detail.Length>90?detail.Substring(0,90):detail));faults.TryGetValue(key,out int n);faults[key]=n+1;
   if(n==0){Note("FAULT "+kind+": "+detail);firstSeen[key]=detail;try{ScreenCapture.CaptureScreenshot(Path.Combine(dir,$"fault-{faults.Count:00}-{kind}.png"));}catch{}WriteReport(false);}}
  // ---------- input (real devices, so every code path that reads Keyboard.current/Mouse.current is exercised)
  void Push(){var kb=Keyboard.current;if(kb!=null)InputSystem.QueueStateEvent(kb,new KeyboardState(held.ToArray()));var m=Mouse.current;if(m!=null){var st=new MouseState{delta=mouseDelta,position=new Vector2(Screen.width*.5f,Screen.height*.5f)};if(mouseDown)st=st.WithButton(MouseButton.Left,true);InputSystem.QueueStateEvent(m,st);}mouseDelta=Vector2.zero;}
  IEnumerator Tap(Key k,int frames=2){held.Add(k);for(int i=0;i<frames;i++){Push();yield return null;}held.Remove(k);Push();yield return null;}
  IEnumerator Hold(float seconds,params Key[] keys){foreach(var k in keys)held.Add(k);float t=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<t){Push();yield return null;}foreach(var k in keys)held.Remove(k);Push();yield return null;}
  IEnumerator Look(float seconds,Vector2 perFrame){float t=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<t){mouseDelta=perFrame;Push();yield return null;}}
  IEnumerator Real(float s){float t=Time.realtimeSinceStartup+s;while(Time.realtimeSinceStartup<t){Push();yield return null;}}
  // ---------- invariants, every frame
  void Update(){
   if(d==null)return;frames++;float dt=Time.realtimeSinceStartup-lastFrame;lastFrame=Time.realtimeSinceStartup;if(frames>30&&dt>.75f)Fault("hitch",$"{dt:F2} s frame");
   var w=d.Scene.Walker?d.Scene.Walker.transform.position:Vector3.zero;var car=d.Scene.Car;
   if(float.IsNaN(w.x)||float.IsNaN(w.y))Fault("nan","walker position NaN");
   if(car&&(float.IsNaN(car.position.x)||float.IsNaN(car.position.y)))Fault("nan","car position NaN");
   var t=Terrain.activeTerrain;
   if(t&&d.Phase==ServicePhase.Playing){
    if(!d.Player.InCar){float g=t.SampleHeight(w)+t.transform.position.y;if(w.y<g-1.2f)Fault("fell","walker under the ground at "+w);var tb=t.terrainData.size;var to=t.transform.position;if(w.x<to.x-5||w.z<to.z-5||w.x>to.x+tb.x+5||w.z>to.z+tb.z+5)Fault("bounds","walker left the world at "+w);}
    if(car){float gc=t.SampleHeight(car.position)+t.transform.position.y;if(car.position.y<gc-1.5f)Fault("fell","car under the ground at "+car.position);if(Vector3.Dot(car.up,Vector3.up)<.35f)Fault("flipped","car on its side/roof at "+car.position);if(d.Player.Speed>40)Fault("speed","car at "+d.Player.Speed+" m/s");}
    // walker inside solid geometry for more than a second
    if(!d.Player.InCar&&d.Scene.Walker){var cc=d.Scene.Walker;var c0=cc.transform.TransformPoint(cc.center+Vector3.up*(cc.height*.5f-cc.radius));var c1=cc.transform.TransformPoint(cc.center-Vector3.up*(cc.height*.5f-cc.radius-.12f));
     var hits=Physics.OverlapCapsule(c0,c1,cc.radius*.7f,~0,QueryTriggerInteraction.Ignore).Where(h=>h&&h!=cc&&!h.transform.IsChildOf(cc.transform)).ToArray();
     if(hits.Length>0){if(insideSince<0)insideSince=Time.realtimeSinceStartup;else if(Time.realtimeSinceStartup-insideSince>1.2f)Fault("inside",$"walker inside {hits[0].name} at {w}");}else insideSince=-1;}
    if(Time.timeScale<.5f)Fault("timescale","playing with time stopped");
   }
   if(d.Phase==ServicePhase.Paused&&Time.timeScale>.5f)Fault("timescale","paused with time running");
   if(d.Busy){if(busySince<0)busySince=Time.realtimeSinceStartup;else if(Time.realtimeSinceStartup-busySince>70)Fault("stuck","director busy for 70 s");}else busySince=-1;
   if(d.Phase==ServicePhase.Playing&&d.InputBlocked&&!d.PaperOpen&&d.NoteOpen<0&&!(d.Dialogue&&d.Dialogue.Active)){if(blockedSince<0)blockedSince=Time.realtimeSinceStartup;else if(Time.realtimeSinceStartup-blockedSince>45)Fault("stuck","input blocked for 45 s with nothing open");}else blockedSince=-1;
   if(d.NoteOpen>=0&&d.PaperOpen)Fault("overlap","held note and clipboard open together");
   if(d.Horror.Active&&d.Horror.Agent&&d.Horror.Phase==PursuitPhase.Chase&&d.Horror.Agent.isActiveAndEnabled&&!d.Horror.Agent.isOnNavMesh)Fault("agent","chaser off the navmesh");
  }
  // ---------- the run
  IEnumerator Main(){
   yield return Real(2f);var minutes=4f;var ma=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith("-chaosMinutes="));if(ma!=null)float.TryParse(ma.Substring(14),out minutes);
   yield return Run("menus",Menus());
   yield return Run("car abuse",CarAbuse());
   yield return Run("doorstep abuse",Doorstep());
   yield return Run("note and clipboard abuse",Papers());
   yield return Run("encounter abuse",Encounters());
   yield return Run("off the map",OffTheMap());
   yield return Run("monkey",Monkey(minutes*60));
   Finish();
  }
  IEnumerator Run(string name,IEnumerator body){scenario=name;Note("begin");var stack=new Stack<IEnumerator>();stack.Push(body);
   while(stack.Count>0){object next=null;bool more;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){Fault("scenario-exception",e.GetType().Name+": "+e.Message);break;}if(!more){stack.Pop();continue;}if(next is IEnumerator n)stack.Push(n);else yield return next;}
   held.Clear();Push();Note("end");WriteReport(false);}
  IEnumerator Menus(){
   d.Title();yield return Real(7f);
   Key[] keys={Key.W,Key.S,Key.A,Key.D,Key.Enter,Key.Escape,Key.Q,Key.E,Key.Tab,Key.UpArrow,Key.DownArrow,Key.Space};
   for(int i=0;i<60;i++){var k=keys[rng.Next(keys.Length)];if(k==Key.Enter&&rng.Next(4)!=0)k=Key.S;yield return Tap(k);yield return Real(.05f+(float)rng.NextDouble()*.15f);if(d.Phase!=ServicePhase.Title&&d.Phase!=ServicePhase.Playing)break;}
   if(d.Phase==ServicePhase.Title)d.NewRoute();yield return Real(6f);d.PaperOpen=false;d.SetCursor();
   for(int i=0;i<12;i++){yield return Tap(Key.Escape);yield return Real(.08f);}Note("phase after pause mash "+d.Phase);if(d.Phase==ServicePhase.Paused)d.Resume();
  }
  IEnumerator CarAbuse(){
   d.BeginShift(0);yield return Real(6f);d.PaperOpen=false;d.SetCursor();
   yield return Tap(Key.Space);yield return Real(1.5f);
   var carStart=d.Scene.Car.position;yield return Hold(9f,Key.W,Key.D);yield return Hold(4f,Key.W,Key.A);Note("after full throttle into the woods: car "+d.Scene.Car.position);if(Vector3.Distance(carStart,d.Scene.Car.position)<5)Fault("input","13 s of injected throttle moved the car less than 5 m - the test is not driving the game");
   yield return Hold(3f,Key.S);yield return Hold(5f,Key.W);
   for(int i=0;i<10;i++){yield return Tap(Key.E);yield return Real(.12f);}Note("enter/exit spam: in car "+d.Player.InCar);
   for(int i=0;i<20;i++){yield return Tap(Key.F);yield return Tap(Key.V);yield return Tap(Key.B);}
   if(!d.Player.InCar){d.Player.EnterCar();yield return Real(.5f);}
   yield return Hold(2f,Key.W);yield return Tap(Key.E);yield return Real(.3f);Note("tried to leave at speed: in car "+d.Player.InCar+" speed "+d.Player.Speed);
   yield return Hold(1.5f,Key.Tab);yield return Tap(Key.Tab);
  }
  IEnumerator Doorstep(){
   d.BeginShift(0);yield return Real(6f);d.PaperOpen=false;d.SetCursor();var p=d.Property(0);
   if(d.Player.InCar){d.Player.StopEngine();d.Player.TryExitCar();yield return Real(.5f);}
   d.Player.SmokePlaceWalker(p.Door.position-p.Inward*1.6f);yield return Real(.5f);
   yield return Face(p.KnockPoint?p.KnockPoint.position:p.Door.position);
   yield return Tap(Key.E);yield return Real(.4f);
   Key[] mash={Key.Digit1,Key.Digit2,Key.Digit3,Key.E,Key.Space,Key.Tab,Key.R,Key.U,Key.Escape,Key.Escape};
   for(int i=0;i<50;i++){yield return Tap(mash[rng.Next(mash.Length)]);yield return Real(.06f);if(d.Phase==ServicePhase.Paused&&rng.Next(2)==0)d.Resume();}
   if(d.Phase==ServicePhase.Paused)d.Resume();yield return Real(8f);Note("doorstep result "+d.ResultAt(0)+" busy "+d.Busy);
   yield return Hold(3f,Key.W,Key.LeftShift);yield return Hold(1f,Key.Space);yield return Hold(2f,Key.LeftCtrl,Key.W);
  }
  IEnumerator Papers(){
   d.BeginShift(0);yield return Real(6f);var p=d.Property(3);if(d.Player.InCar){d.Player.StopEngine();d.Player.TryExitCar();yield return Real(.5f);}
   d.PaperOpen=false;d.SetCursor();d.Player.SmokePlaceWalker(p.Door.position-p.Inward*1.3f);yield return Real(.4f);if(p.NoticePoint)yield return Face(p.NoticePoint.position);
   for(int i=0;i<6;i++){yield return Tap(Key.E);yield return Real(.25f);yield return Tap(Key.Escape);yield return Real(.25f);if(d.Phase==ServicePhase.Paused)d.Resume();yield return Tap(Key.Tab);yield return Real(.2f);}
   Note($"note {d.NoteOpen} paper {d.PaperOpen} phase {d.Phase}");if(d.Phase==ServicePhase.Paused)d.Resume();
   d.NewRoute();yield return Real(6f);for(int i=0;i<8;i++){yield return Tap(Key.Tab);yield return Real(.2f);yield return Hold(.5f,Key.W);}
   d.PaperOpen=false;d.SetCursor();
  }
  IEnumerator Encounters(){
   // night two at Harrow: the watcher. Do everything you shouldn't.
   d.BeginShift(1);yield return Real(6f);d.PaperOpen=false;d.SetCursor();var h=d.Property(3);if(d.Player.InCar){d.Player.StopEngine();d.Player.TryExitCar();yield return Real(.5f);}
   d.Player.SmokePlaceWalker(h.TableApproach.position);yield return Real(.3f);d.Horror.Begin(3);yield return Real(.5f);
   yield return Look(1.5f,new Vector2(40,0));yield return Hold(1f,Key.Space);yield return Hold(1f,Key.LeftCtrl);yield return Tap(Key.Escape);yield return Real(1f);d.Resume();yield return Hold(2f,Key.W);
   yield return Real(5f);Note("watcher phase "+d.Horror.Phase);
   // night two at Morrow: the chase. Pause, clipboard, walk into it, try the car during the reveal.
   d.BeginShift(1);yield return Real(6f);d.PaperOpen=false;d.SetCursor();var m=d.Property(5);if(d.Player.InCar){d.Player.StopEngine();d.Player.TryExitCar();yield return Real(.5f);}
   d.Player.SmokePlaceWalker(m.TableApproach.position);yield return Real(.3f);d.Horror.Begin(5);yield return Real(.4f);
   yield return Tap(Key.Escape);yield return Real(1.5f);yield return Tap(Key.Escape);yield return Real(.3f);if(d.Phase==ServicePhase.Paused)d.Resume();
   yield return Tap(Key.Tab);yield return Real(.5f);
   if(d.Horror.Agent){yield return Face(d.Horror.Agent.transform.position+Vector3.up);}yield return Hold(4f,Key.W,Key.LeftShift);
   yield return Real(6f);Note("chase phase "+d.Horror.Phase+" captures "+d.Horror.Captures);
  }
  IEnumerator OffTheMap(){
   d.BeginShift(0);yield return Real(6f);d.PaperOpen=false;d.SetCursor();if(d.Player.InCar){d.Player.StopEngine();d.Player.TryExitCar();yield return Real(.5f);}
   var walkStart=d.Scene.Walker.transform.position;yield return Look(.5f,new Vector2(120,0));yield return Hold(40f,Key.W,Key.LeftShift);Note("walked off: "+d.Scene.Walker.transform.position);if(Vector3.Distance(walkStart,d.Scene.Walker.transform.position)<5)Fault("input","40 s of injected sprint moved the walker less than 5 m - the test is not driving the game");
   d.Player.EnterCar();yield return Real(.5f);yield return Tap(Key.Space);yield return Real(1.2f);yield return Hold(25f,Key.W);Note("drove off: "+d.Scene.Car.position);
  }
  IEnumerator Monkey(float seconds){
   d.BeginShift(rng.Next(3));yield return Real(6f);d.PaperOpen=false;d.SetCursor();
   Key[] keys={Key.W,Key.A,Key.S,Key.D,Key.LeftShift,Key.LeftCtrl,Key.Space,Key.E,Key.R,Key.U,Key.F,Key.Tab,Key.V,Key.B,Key.Digit1,Key.Digit2,Key.Digit3,Key.Escape};
   float end=Time.realtimeSinceStartup+seconds;float nextJump=0;
   while(Time.realtimeSinceStartup<end){
    if(Time.realtimeSinceStartup>nextJump){nextJump=Time.realtimeSinceStartup+25;var p=d.Property(rng.Next(6));if(d.Phase==ServicePhase.Paused)d.Resume();if(d.Phase==ServicePhase.Playing&&!d.Horror.Active&&!d.Busy){if(d.Player.InCar){d.Player.StopEngine();d.Player.TryExitCar();}d.Player.SmokePlaceWalker(p.Door.position-p.Inward*2.5f);Note("jumped to property "+p.Index);}}
    if(d.Phase==ServicePhase.Report||d.Phase==ServicePhase.Finished||d.Phase==ServicePhase.Title){d.BeginShift(rng.Next(3));yield return Real(6f);d.PaperOpen=false;d.SetCursor();continue;}
    int combo=rng.Next(1,3);var hold=new List<Key>();for(int i=0;i<combo;i++)hold.Add(keys[rng.Next(keys.Length)]);
    if(hold.Contains(Key.Escape)&&rng.Next(3)!=0)hold.Remove(Key.Escape);
    float len=(float)(.05+rng.NextDouble()*1.2);mouseDelta=new Vector2((float)(rng.NextDouble()*2-1)*30,(float)(rng.NextDouble()*2-1)*10);
    yield return Hold(len,hold.ToArray());if(d.Phase==ServicePhase.Paused&&rng.Next(2)==0)d.Resume();
   }
  }
  IEnumerator Face(Vector3 point){var v=d.Scene.View.transform;for(int i=0;i<40;i++){var to=point-v.position;var yaw=Vector3.SignedAngle(Vector3.ProjectOnPlane(v.forward,Vector3.up),Vector3.ProjectOnPlane(to,Vector3.up),Vector3.up);var pitch=-Mathf.Atan2(to.y,new Vector2(to.x,to.z).magnitude)*Mathf.Rad2Deg-(v.eulerAngles.x>180?v.eulerAngles.x-360:v.eulerAngles.x);if(Mathf.Abs(yaw)<2&&Mathf.Abs(pitch)<2)break;mouseDelta=new Vector2(Mathf.Clamp(yaw*4,-60,60),Mathf.Clamp(-pitch*4,-40,40));Push();yield return null;}}
  void WriteReport(bool done){
   var lines=new List<string>{$"SERVICE chaos test - {DateTime.Now}"+(done?"":"  (in progress)"),"",faults.Count==0?"No faults.":$"{faults.Count} distinct faults:"};
   foreach(var kv in faults.OrderByDescending(x=>x.Value))lines.Add($"  x{kv.Value,-5} {kv.Key}   first: {(firstSeen.TryGetValue(kv.Key,out var f)?f:"")}");
   var edge=FindAnyObjectByType<ServiceWorldEdge>();if(edge)lines.Add($"  world-edge rescues: {edge.Rescues}");
   var menus=FindAnyObjectByType<ServiceMenus>();if(menus)lines.Add($"  quit chosen from the menu {menus.QuitRequests} times (counted, not obeyed)");
   lines.Add("");lines.Add("Log:");lines.AddRange(log);try{File.WriteAllLines(Path.Combine(dir,"chaos-report.txt"),lines);}catch{}}
  void Finish(){
   foreach(var k in PrefKeys){var v=prefs[k];if(!v.has)PlayerPrefs.DeleteKey(k);else if(k=="SERVICE.camcorder"||k=="SERVICE.lightning")PlayerPrefs.SetInt(k,v.i);else PlayerPrefs.SetFloat(k,v.f);}PlayerPrefs.Save();
   WriteReport(true);Application.logMessageReceived-=OnLog;Application.Quit();
  }
 }
}
