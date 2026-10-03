using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEngine.AI;
namespace ServiceGameV2 {
 // V19 review tour: plays every section of the game the way a player would (real driving and walking, notes, knocks,
 // dialogue at reading pace, deliveries, each encounter survived), streaming each section straight into an MP4 through
 // ffmpeg so nothing large lands on disk. Run: Service.exe -serviceSmoke -serviceTour with SERVICE_CAPTURE_DIR and
 // SERVICE_FFMPEG set. Optional -tourOnly=03 to record one section.
 public sealed class ServiceV19Tour:MonoBehaviour {
  ServiceDirector d;string dir,ffmpegPath,only;System.Diagnostics.Process ff;Stream ffIn;bool recording;int frames;byte[] buffer;
  // V21: -tourAudio records in real time with the game's own sound (ServiceAudioTap), muxed into each section's MP4.
  bool realtime;ServiceAudioTap tap;float recStart;string recName;
  readonly List<string> log=new List<string>();
  public void Run(ServiceDirector director){d=director;dir=Environment.GetEnvironmentVariable("SERVICE_CAPTURE_DIR");ffmpegPath=Environment.GetEnvironmentVariable("SERVICE_FFMPEG");Directory.CreateDirectory(dir);
   var a=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith("-tourOnly="));only=a==null?null:a.Substring(10);
   realtime=ServiceAudio.TourAudio;if(realtime){var listener=FindAnyObjectByType<AudioListener>();if(listener)tap=listener.gameObject.AddComponent<ServiceAudioTap>();}else Time.captureFramerate=24;
   StartCoroutine(Capture());StartCoroutine(Guard());}
  void Note(string s){log.Add($"[{Time.time:F1}] {s}");File.WriteAllLines(Path.Combine(dir,"tour-log.txt"),log);}
  IEnumerator Guard(){var stack=new Stack<IEnumerator>();stack.Push(Main());
   while(stack.Count>0){object next=null;bool more=false;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){Note("ERROR "+e);more=false;stack.Clear();break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
   End();Note("tour finished");Application.Quit();}
  // ---- recording
  bool Want(string id)=>only==null||id.StartsWith(only);
  void Begin(string name){End();if(string.IsNullOrEmpty(ffmpegPath)||!File.Exists(ffmpegPath)){Note("no ffmpeg: "+ffmpegPath);return;}
   int w=Screen.width&~1,h=Screen.height&~1;buffer=new byte[w*h*4];
   var psi=new System.Diagnostics.ProcessStartInfo(ffmpegPath,$"-y -loglevel error -f rawvideo -pix_fmt rgba -s {w}x{h} -r 24 -i - -vf vflip,scale=960:540 -c:v libx264 -preset {(realtime?"veryfast":"medium")} -crf 31 -pix_fmt yuv420p -movflags +faststart \"{Path.Combine(dir,name+(realtime?".video.mp4":".mp4"))}\""){UseShellExecute=false,RedirectStandardInput=true,CreateNoWindow=true};
   ff=System.Diagnostics.Process.Start(psi);ffIn=ff.StandardInput.BaseStream;recording=true;frames=0;recName=name;recStart=Time.realtimeSinceStartup;if(realtime&&tap)tap.StartFile(Path.Combine(dir,name+".wav"));Note("REC "+name+$" {w}x{h}"+(realtime?" (real time, with sound)":""));}
  void End(){if(!recording)return;recording=false;if(realtime&&tap)tap.StopFile();try{ffIn.Flush();ffIn.Close();ff.WaitForExit(60000);}catch(Exception e){Note("ffmpeg close "+e.Message);}Note($"END {frames} frames ({frames/24f:F1}s)");ff=null;
   if(realtime){var v=Path.Combine(dir,recName+".video.mp4");var a=Path.Combine(dir,recName+".wav");var o=Path.Combine(dir,recName+".mp4");
    try{var mux=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ffmpegPath,$"-y -loglevel error -i \"{v}\" -i \"{a}\" -c:v copy -c:a aac -b:a 128k -shortest -movflags +faststart \"{o}\""){UseShellExecute=false,CreateNoWindow=true});mux.WaitForExit(120000);
     if(File.Exists(o)&&new FileInfo(o).Length>0){File.Delete(v);File.Delete(a);Note("muxed sound into "+recName+".mp4");}else Note("mux failed for "+recName);}catch(Exception e){Note("mux "+e.Message);}}}
  IEnumerator Capture(){var eof=new WaitForEndOfFrame();while(true){yield return eof;if(!recording||ff==null)continue;
    var tex=ScreenCapture.CaptureScreenshotAsTexture();int w=Screen.width&~1,h=Screen.height&~1;
    if(tex.width>=w&&tex.height>=h){var px=tex.GetPixels32();int k=0;for(int y=0;y<h;y++){int row=y*tex.width;for(int x=0;x<w;x++){var c=px[row+x];buffer[k++]=c.r;buffer[k++]=c.g;buffer[k++]=c.b;buffer[k++]=255;}}
     // in real time the video keeps pace with the clock (and the sound): a slow frame is held for as long as it took
     int want=realtime?Mathf.Max(frames+(frames==0?1:0),Mathf.FloorToInt((Time.realtimeSinceStartup-recStart)*24)):frames+1;
     try{while(frames<want){ffIn.Write(buffer,0,buffer.Length);frames++;}}catch(Exception e){Note("pipe "+e.Message);recording=false;}}
    Destroy(tex);}}
  // Frame-counted so it also runs while the game is paused (one frame = 1/24 s of video).
  IEnumerator Wait(float s){if(realtime){float t=Time.realtimeSinceStartup+s;while(Time.realtimeSinceStartup<t)yield return null;yield break;}int n=Mathf.CeilToInt(s*24);for(int i=0;i<n;i++)yield return null;}
  // V21: the typed time card that opens a night (kept in the first section of each night, skipped elsewhere)
  IEnumerator CardDone(){float t=Time.realtimeSinceStartup+15;while(d.Timecard&&d.Timecard.Blocking&&Time.realtimeSinceStartup<t)yield return null;}
  void NoCard(){if(d.Timecard)d.Timecard.Clear();}
  // ---- looking and walking (smooth, like a player with a mouse)
  float Yaw=>d.Scene.Walker.transform.eulerAngles.y;
  float Pitch{get{var x=d.Scene.View.transform.localEulerAngles.x;return x>180?x-360:x;}}
  IEnumerator Look(Vector3 target,float seconds){float y0=Yaw,p0=Pitch;var delta=target-d.Scene.View.transform.position;float y1=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,p1=-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg;
   float t=0;while(t<seconds){t+=Time.deltaTime;float k=Mathf.SmoothStep(0,1,t/seconds);d.Player.SmokeLook(Mathf.LerpAngle(y0,y1,k),Mathf.Lerp(p0,p1,k));yield return null;}}
  IEnumerator Walk(Vector3 target,string label,bool sprint=false,float giveUp=40,Func<bool> until=null){
   d.Player.SmokeWalk=Vector2.zero;
   if(!NavMesh.SamplePosition(d.Scene.Walker.transform.position,out var a,2.5f,NavMesh.AllAreas)||!NavMesh.SamplePosition(target,out var b,2.5f,NavMesh.AllAreas)){Note("WALK no navmesh "+label);yield break;}
   var path=new NavMeshPath();if(!NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)||path.corners.Length<2){Note("WALK no path "+label);yield break;}
   float end=Time.time+giveUp;
   for(int i=1;i<path.corners.Length;i++){var c=path.corners[i];bool last=i==path.corners.Length-1;
    while(true){var pos=d.Scene.Walker.transform.position;var to=c-pos;to.y=0;if(to.magnitude<(last?.25f:.45f))break;
     if(until!=null&&until()){d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeSprint=false;Note("interrupted "+label);yield break;}
     if(Time.time>end){d.Player.SmokeWalk=Vector2.zero;var w=d.Scene.Walker;var hits=Physics.OverlapCapsule(pos+Vector3.up*.35f,pos+Vector3.up*1.5f,.32f,~0,QueryTriggerInteraction.Ignore).Where(x=>x!=w).Select(x=>x.name+"@"+x.gameObject.layer).ToArray();Note("WALK STUCK "+label+" at "+pos+" toward "+c+$" | blocked {d.InputBlocked} busy {d.Busy} incar {d.Player.InCar} walker enabled {w.enabled} grounded {w.isGrounded} speed {d.Player.HorizontalSpeed:F2} overlaps: "+string.Join(", ",hits));yield break;}
     float want=Mathf.Atan2(to.x,to.z)*Mathf.Rad2Deg;float yaw=Mathf.MoveTowardsAngle(Yaw,want,(sprint?260:150)*Time.deltaTime);
     d.Player.SmokeLook(yaw,Mathf.Lerp(Pitch,4,Time.deltaTime*3));float off=Mathf.Abs(Mathf.DeltaAngle(yaw,want));
     d.Player.SmokeSprint=sprint&&off<25;d.Player.SmokeWalk=new Vector2(0,off<40?1:.25f);yield return null;}}
   d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeSprint=false;Note("walked "+label);
  }
  // ---- driving: pure pursuit along a polyline in the right-hand lane
  IEnumerator Drive(List<Vector3> pts,float maxSpeed,string label,bool stop=true,float giveUp=90){
   var car=d.Scene.Car;float end=Time.time+giveUp;int idx=0;
   while(true){
    var pos=car.position;while(idx<pts.Count-1&&Vector3.Distance(Flat(pos),Flat(pts[idx]))<6)idx++;
    // look-ahead point 7 m along the remaining polyline
    Vector3 target=pts[idx];float need=7;var from=Flat(pos);for(int i=idx;i<pts.Count;i++){var seg=Flat(pts[i])-from;if(seg.magnitude>=need){target=from+seg.normalized*need;break;}need-=seg.magnitude;from=Flat(pts[i]);target=pts[i];}
    var fwd=car.forward;fwd.y=0;var dir2=Flat(target)-Flat(pos);float ang=Vector3.SignedAngle(fwd,dir2,Vector3.up);
    float remain=Vector3.Distance(Flat(pos),Flat(pts[pts.Count-1]));
    d.Player.SmokeSteering=Mathf.Clamp(ang/28f,-1,1);
    float cap=stop?Mathf.Min(maxSpeed,1.2f+remain*.55f):maxSpeed;cap*=Mathf.Lerp(1,.45f,Mathf.Clamp01(Mathf.Abs(ang)/50f));
    d.Player.SmokeThrottle=d.Player.Speed<cap?.85f:.05f;d.Player.SmokeBrake=d.Player.Speed>cap+2.5f;
    if(remain<(stop?1.6f:4f)&&idx>=pts.Count-1){break;}
    if(Time.time>end){Note("DRIVE timeout "+label+" at "+pos);break;}
    yield return null;}
   d.Player.SmokeThrottle=0;d.Player.SmokeSteering=0;
   if(stop){d.Player.SmokeBrake=true;float t=Time.time+4;while(d.Player.Speed>.2f&&Time.time<t)yield return null;d.Player.SmokeBrake=false;}
   Note("drove "+label);
  }
  static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
  List<Vector3> roadLine;
  List<Vector3> RoadLine(){if(roadLine!=null)return roadLine;roadLine=d.Scene.Route.Where(t=>t).Select(t=>t.position).ToList();Note($"route {roadLine.Count} points from {roadLine.FirstOrDefault()} to {roadLine.LastOrDefault()}");return roadLine;}
  // Right-hand lane: offset the centre line 1.6 m to the right of travel.
  static List<Vector3> Lane(List<Vector3> c,bool reverse){var l=reverse?Enumerable.Reverse(c).ToList():new List<Vector3>(c);var o=new List<Vector3>();for(int i=0;i<l.Count;i++){var t=(i<l.Count-1?l[i+1]-l[i]:l[i]-l[i-1]);t.y=0;var r=Vector3.Cross(Vector3.up,t.normalized);o.Add(l[i]+r*1.6f);}return o;}
  static int Nearest(List<Vector3> l,Vector3 p){int best=0;float bd=float.MaxValue;for(int i=0;i<l.Count;i++){float dd=Vector3.Distance(Flat(l[i]),Flat(p));if(dd<bd){bd=dd;best=i;}}return best;}
  // Late road (County Route 9): reconstruct its centre line from the road mesh, bucketed along z.
  List<Vector3> LateLine(){var pts=new List<Vector3>();if(!d.Scene.LateRoad)return pts;var verts=new List<Vector3>();
   foreach(var mf in d.Scene.LateRoad.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh&&mf.sharedMesh.isReadable)foreach(var v in mf.sharedMesh.vertices)verts.Add(mf.transform.TransformPoint(v));
   if(verts.Count==0)return pts;foreach(var g in verts.GroupBy(v=>Mathf.RoundToInt(v.z/3f)).OrderBy(g=>g.Key))pts.Add(new Vector3(g.Average(v=>v.x),g.Average(v=>v.y),g.Average(v=>v.z)));Note($"late line {pts.Count} points");return pts;}
  // Get from the road into a property's parking spot: approach along the main road, then the pull-off, then the gate.
  IEnumerator ArriveAt(ServiceProperty p,string label){
   var line=RoadLine();var gate=p.Gate.position;int gi=Nearest(line,gate);bool north=gi>=Nearest(line,d.Scene.Car.position);
   int start=Mathf.Clamp(north?gi-6:gi+6,0,line.Count-1);var lane=Lane(line,!north);int s=Nearest(lane,line[start]),e=Nearest(lane,line[gi]);
   var seg=new List<Vector3>();if(s<=e)for(int i=s;i<=e;i++)seg.Add(lane[i]);else for(int i=s;i>=e;i--)seg.Add(lane[i]);
   var first=seg[0];var next=seg.Count>1?seg[1]:gate;d.Player.TeleportCar(first+Vector3.up*.3f,Quaternion.LookRotation(Flat(next-first)));yield return Wait(.5f);
   seg.Add(Vector3.Lerp(seg[seg.Count-1],gate,.5f));seg.Add(gate);
   yield return Drive(seg,9,label);
  }
  IEnumerator ExitCar(){if(d.Player.InCar){d.Player.StopEngine();yield return Wait(.6f);if(!d.Player.TryExitCar())Note("could not exit car");yield return Wait(1f);}}
  IEnumerator ToDoor(ServiceProperty p){var c=DoorCentre(p);yield return Walk(c+DoorNormal(p)*1.7f,"to the door "+p.Index);yield return Look(c+Vector3.up*1.45f,1.2f);}
  // The leaf's own geometry (shut) gives the opening: its bounds centre, and the normal of the hinge-to-centre line.
  static Vector3 DoorCentre(ServiceProperty p){if(!p.DoorPanel)return p.Door.position;var rs=p.DoorPanel.GetComponentsInChildren<Renderer>();if(rs.Length==0)return p.Door.position;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);var c=b.center;c.y=p.Door.position.y;return c;}
  static Vector3 DoorNormal(ServiceProperty p){if(!p.DoorPanel)return Outward(p);var leaf=DoorCentre(p)-p.DoorPanel.position;leaf.y=0;if(leaf.sqrMagnitude<.01f)return Outward(p);var n=Vector3.Cross(Vector3.up,leaf.normalized);return Vector3.Dot(n,Outward(p))<0?-n:n;}
  // A player who hears something behind them looks. Only when it is far enough back to afford the glance.
  IEnumerator LookBack(float minDistance){var a=d.Horror.Agent;if(!a||!d.Horror.Active)yield break;var head=a.transform.position+Vector3.up*1.5f;float dist=Vector3.Distance(head,d.Scene.View.transform.position);
   if(dist<minDistance||dist>22){Note($"no look back ({dist:F1} m)");yield break;}d.Player.SmokeWalk=Vector2.zero;yield return Look(head,.45f);yield return Wait(.5f);Note($"looked back at {dist:F1} m");}
  static Vector3 Outward(ServiceProperty p){var o=p.Door.position-p.InteriorBounds.center;o.y=0;return o.sqrMagnitude<.01f?p.Door.forward:o.normalized;}
  IEnumerator ReadNote(ServiceProperty p){if(!p.NoticePoint||!p.NoticePoint.gameObject.activeInHierarchy)yield break;yield return Look(p.NoticePoint.position,1f);yield return Wait(.6f);int n=d.NearbyNotice();if(n==p.Index){d.ReadNotice(n);yield return Wait(5.5f);d.CloseNote();yield return Wait(.6f);}else Note("note not readable at "+p.Index+" (NearbyNotice="+n+")");}
  IEnumerator Knock(ServiceProperty p){yield return Look(p.KnockPoint.position,.8f);yield return Wait(.3f);if(d.NearbyKnockDoor()!=p.Index)Note("cannot knock at "+p.Index);var face=DoorCentre(p)+Vector3.up*1.55f;var shut=p.DoorPanel?p.DoorPanel.localRotation:Quaternion.identity;
   d.Attempt(p.Index,ServiceResult.Served);float t=Time.time+60;yield return Wait(.2f);
   if(d.IsFriendly(p.Index)&&p.DoorPanel){float open=Time.time+8;while(d.Busy&&Time.time<open&&Quaternion.Angle(p.DoorPanel.localRotation,shut)<20)yield return null;if(d.Busy)yield return Look(face,.9f);}
   while(d.Busy&&Time.time<t)yield return null;yield return Wait(1.2f);}
  IEnumerator Deliver(ServiceProperty p){yield return Walk(p.TableApproach.position,"door to table "+p.Index);yield return Look(p.DeliveryPoint.position,1.2f);yield return Wait(1.2f);
   if(d.NearbyDoor()!=p.Index)Note("table not reachable at "+p.Index);d.Attempt(p.Index,ServiceResult.LeftAtDoor);{float until=Time.time+4;while(d.Busy&&Time.time<until)yield return null;}yield return Wait(.4f);}
  IEnumerator Flee(ServiceProperty p){var o=Outward(p);yield return Walk(p.Door.position+o*1.8f,"flee to the door "+p.Index,true,25);yield return LookBack(7f);yield return Walk(d.Scene.Car.position-d.Scene.Car.right*2.2f,"flee to the car "+p.Index,true,30,()=>d.Horror.Caught);
   d.Player.EnterCar();yield return Wait(.4f);float t=Time.time+6;while(Time.time<t&&!d.Horror.Caught){d.Player.SmokeThrottle=-1;d.Player.SmokeSteering=.3f;yield return null;}d.Player.SmokeThrottle=0;d.Player.SmokeSteering=0;d.Player.SmokeBrake=true;yield return Wait(1.5f);d.Player.SmokeBrake=false;Note($"after flight: caught={d.Horror.Caught} active={d.Horror.Active} phase={d.Horror.Phase}");}
  IEnumerator Main(){
   yield return Wait(1.5f);var ui=d.transform.Find("Service interface");if(ui)ui.gameObject.SetActive(true);
   ServiceDialogue.TourPacing=true;
   var P=new Func<int,ServiceProperty>(i=>d.Property(i));
   // 00 (only on request): headlights on a straight stretch, from the driver's seat and from beside the car.
   if(only=="00"){d.BeginShift(0);Begin("00-headlights");yield return Wait(7f);d.PaperOpen=false;d.SetCursor();var line=RoadLine();int i=line.Count/3;var lane=Lane(line,false);
    d.Player.TeleportCar(lane[i]+Vector3.up*.3f,Quaternion.LookRotation(Flat(lane[i+1]-lane[i])));yield return Wait(4f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"beam-driver.png"));yield return Wait(.5f);
    // V20 driving hands at the three wheel extremes, on the dark road
    var dh=FindAnyObjectByType<ServiceDrivingHands>();
    foreach(var (tag,st) in new[]{("left",-1f),("centre",0f),("right",1f)}){float until=Time.time+1.6f;while(Time.time<until){d.Player.SmokeSteering=st;d.Player.SmokeThrottle=0;d.Player.SmokeBrake=true;yield return null;}
     ScreenCapture.CaptureScreenshot(Path.Combine(dir,$"steer-{tag}.png"));yield return Wait(.3f);
     Note($"steer {tag}: input {d.Player.SteeringInput:F2} hands {(dh?dh.Visible.ToString():"missing")} renderers {(dh?string.Join(",",dh.GetComponentsInChildren<Renderer>(true).Select(r=>r.enabled+"/"+r.gameObject.activeInHierarchy+"/"+r.isVisible)):"-")}");}
    d.Player.SmokeSteering=0;
    // V21: the cabin as the head turns (mirror, dice, the driving arms at the edge of view), wheel straight and on lock
    foreach(var (yv,sv) in new[]{(0f,0f),(20f,0f),(45f,0f),(65f,0f),(90f,0f),(-45f,0f),(-75f,0f),(60f,1f),(60f,-1f),(-60f,1f)}){float until2=Time.time+1.1f;while(Time.time<until2){d.Player.SmokeLook(yv,4);d.Player.SmokeSteering=sv;d.Player.SmokeBrake=true;yield return null;}
     ScreenCapture.CaptureScreenshot(Path.Combine(dir,$"look{yv:+0;-0;0}-steer{sv:+0;-0;0}.png"));yield return Wait(.25f);Note($"look {yv} steer {sv}: driving hands {(dh?dh.Visible:false)}");}
    d.Player.SmokeLook(0,4);d.Player.SmokeSteering=0;d.Player.SmokeBrake=false;
    yield return ExitCar();var car=d.Scene.Car;d.Player.SmokePlaceWalker(car.position-car.forward*3.5f-car.right*2.6f);yield return Wait(.5f);yield return Look(car.position+car.forward*14,.3f);yield return Wait(1f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"beam-side.png"));yield return Wait(.5f);
    // Hand lighting test: torch on/off, filter on/off.
    var cc=FindAnyObjectByType<ServiceCamcorder>();yield return Look(car.position+car.forward*14+Vector3.down*1.2f,.3f);yield return Wait(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"hand-torch-on.png"));yield return Wait(.3f);
    d.Scene.Flashlight.enabled=false;yield return Wait(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"hand-torch-off.png"));yield return Wait(.3f);
    d.Scene.Flashlight.enabled=true;if(cc)cc.Preview(0);yield return Wait(.5f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"hand-raw.png"));yield return Wait(.3f);if(cc)cc.Preview(2);
    Note($"hand light test: torch {d.Scene.Flashlight.name} at {d.Scene.Flashlight.transform.position} view {d.Scene.View.transform.position} angle {d.Scene.Flashlight.spotAngle}");
    foreach(var l in FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.enabled&&Vector3.Distance(l.transform.position,d.Scene.View.transform.position)<4)Note($"near light {l.name} {l.type} int {l.intensity} at {l.transform.position} parent {(l.transform.parent?l.transform.parent.name:"-")}");
    End();yield break;}
   if(Want("01")){d.BeginShift(0);Begin("01-shift-start-and-road");yield return Wait(1f);yield return CardDone();yield return Wait(1f);d.PaperOpen=false;d.SetCursor();yield return Wait(1.5f);yield return ArriveAt(P(0),"road to Correll");End();}
   if(Want("02")){if(only!=null){d.BeginShift(0);d.PaperOpen=false;NoCard();yield return ArriveAt(P(0),"road to Correll");}Begin("02-correll-doorstep");yield return ExitCar();if(d.Life&&d.Life.DogPresent){yield return Look(d.Life.DogHome+Vector3.up*.5f,1f);yield return Wait(2f);Note("dog at "+d.Life.DogHome);}yield return ToDoor(P(0));yield return Knock(P(0));yield return Wait(1f);yield return Walk(d.Scene.Car.position-d.Scene.Car.right*2.2f,"back to the car");d.Player.EnterCar();yield return Wait(1.5f);End();}
   // ---- NIGHT ONE: nothing happens. Things a tired process server can explain away.
   if(Want("03")){d.BeginShift(0);d.PaperOpen=false;NoCard();d.Docket.Find(e=>e.Property==0).Result=ServiceResult.Served;Begin("03-night1-harrow-porch-light");yield return ArriveAt(P(3),"Harrow drive");yield return ExitCar();yield return Wait(.8f);d.ToggleTorch();yield return Wait(1.4f);d.ToggleTorch();yield return Wait(.8f);var p=P(3);yield return ToDoor(p);yield return ReadNote(p);yield return Knock(p);
    yield return Deliver(p);yield return Wait(2f);Note("harrow encounter active="+d.Horror.Active);yield return Walk(p.Door.position+Outward(p)*1.8f,"out of Harrow");yield return Walk(p.Door.position+Outward(p)*16f,"down the drive");yield return Wait(.6f);
    yield return Look(p.Door.position+Vector3.up*1.6f,1.2f);yield return Wait(3f);Note("porch light on="+(p.PorchLight&&p.PorchLight.enabled)+" omens "+(d.Omens?d.Omens.Fired:-1));yield return Walk(d.Scene.Car.position-d.Scene.Car.right*2.2f,"Harrow car");End();}
   if(Want("04")){d.BeginShift(0);d.PaperOpen=false;NoCard();foreach(var i in new[]{0,3})d.Docket.Find(e=>e.Property==i).Result=ServiceResult.Served;Begin("04-night1-vale-upstairs");var p=P(1);yield return ArriveAt(p,"Vale drive");yield return ExitCar();yield return ToDoor(p);yield return ReadNote(p);yield return Knock(p);yield return Deliver(p);
    yield return Wait(2f);yield return Look(p.InteriorBounds.center+Vector3.up*3f,1.5f);yield return Wait(4f);Note("vale omens "+(d.Omens?d.Omens.Fired:-1)+" encounter "+d.Horror.Active);yield return Walk(p.Door.position+Outward(p)*1.8f,"out of Vale");yield return Walk(d.Scene.Car.position-d.Scene.Car.right*2.2f,"Vale car");End();}
   if(Want("05")){d.BeginShift(0);d.PaperOpen=false;NoCard();foreach(var i in new[]{0,3,1,4})d.Docket.Find(e=>e.Property==i).Result=ServiceResult.Served;Begin("05-night1-morrow-radio-and-the-drive-home");var p=P(5);yield return ArriveAt(p,"Morrow drive");yield return ExitCar();yield return ToDoor(p);yield return ReadNote(p);yield return Knock(p);yield return Deliver(p);yield return Wait(2.5f);
    Note("morrow omens "+(d.Omens?d.Omens.Fired:-1));yield return Walk(p.Door.position+Outward(p)*1.8f,"out of Morrow");yield return Walk(d.Scene.Car.position-d.Scene.Car.right*2.2f,"Morrow car");d.Player.EnterCar();yield return Wait(1f);
    // home: south along the lane to the depot
    var line=RoadLine();var south=Lane(line,true);int from=Nearest(south,d.Scene.Car.position);var seg=south.Skip(from).ToList();seg.Add(new Vector3(-8,0,-1));
    var rev=Time.time+4;while(Time.time<rev&&Vector3.Dot(d.Scene.Car.forward,Flat(seg[Mathf.Min(3,seg.Count-1)]-d.Scene.Car.position).normalized)<.5f){d.Player.SmokeThrottle=-.6f;d.Player.SmokeSteering=.9f;yield return null;}d.Player.SmokeThrottle=0;d.Player.SmokeSteering=0;
    yield return Drive(seg,11,"home to the depot",true,120);Note("drive home omens "+(d.Omens?d.Omens.Fired:-1));yield return Wait(3f);End();}
   // ---- NIGHT TWO: the same five addresses, and they are wrong now.
   if(Want("06")){d.BeginShift(1);d.PaperOpen=false;d.Docket.Find(e=>e.Property==0).Result=ServiceResult.Served;Begin("06-night2-harrow-watcher");yield return CardDone();yield return ArriveAt(P(3),"Harrow drive");yield return ExitCar();var p=P(3);yield return ToDoor(p);yield return ReadNote(p);yield return Knock(p);
    yield return Deliver(p);yield return Wait(1.5f);
    if(d.Horror.Active&&d.Horror.Agent){var e=d.Horror.Agent.transform.position;var awayP=d.Scene.View.transform.position*2-e;awayP.y=d.Scene.View.transform.position.y;yield return Look(awayP,.9f);float t=Time.time+16;while(d.Horror.Active&&!d.Horror.Caught&&Time.time<t)yield return null;Note($"watcher: caught={d.Horror.Caught} active={d.Horror.Active}");}
    yield return Wait(2f);if(!d.Horror.Caught){yield return Walk(p.Door.position+Outward(p)*1.8f,"out of Harrow");yield return Walk(d.Scene.Car.position-d.Scene.Car.right*2.2f,"Harrow car");}yield return Wait(1f);End();}
   if(Want("07")){d.BeginShift(1);d.PaperOpen=false;NoCard();foreach(var i in new[]{0,3,1})d.Docket.Find(e=>e.Property==i).Result=ServiceResult.Served;Begin("07-night2-bell-back-room-and-the-door");var p=P(4);yield return ArriveAt(p,"Bell drive");yield return ExitCar();yield return ToDoor(p);yield return ReadNote(p);yield return Knock(p);yield return Deliver(p);yield return Wait(1.5f);
    yield return Walk(p.Door.position+Outward(p)*1.8f,"out of the house");yield return Walk(d.Scene.Car.position-d.Scene.Car.right*2.2f,"Bell drive back",false,45,()=>d.Horror.Active);
    if(d.Horror.Active){Note("Bell return ambush");yield return Wait(.3f);var a=d.Horror.Agent;yield return Look((a?a.transform.position:p.Door.position)+Vector3.up*1.5f,.5f);yield return Wait(.9f);yield return Walk(d.Scene.Car.position-d.Scene.Car.right*2.2f,"run to the car",true,25,()=>d.Horror.Caught);}
    d.Player.EnterCar();yield return Wait(.4f);float t=Time.time+5;while(Time.time<t&&!d.Horror.Caught){d.Player.SmokeThrottle=-1;d.Player.SmokeSteering=.3f;yield return null;}d.Player.SmokeThrottle=0;d.Player.SmokeSteering=0;Note($"Bell end: caught={d.Horror.Caught}");yield return Wait(1.5f);End();}
   if(Want("08")){d.BeginShift(1);d.PaperOpen=false;NoCard();foreach(var i in new[]{0,3,1,4})d.Docket.Find(e=>e.Property==i).Result=ServiceResult.Served;Begin("08-night2-morrow-chase");var p=P(5);yield return ArriveAt(p,"Morrow drive");yield return ExitCar();yield return ToDoor(p);yield return ReadNote(p);yield return Knock(p);yield return Deliver(p);yield return Wait(1.0f);
    if(d.Horror.Agent&&d.Horror.Active)yield return Look(d.Horror.Agent.transform.position+Vector3.up*1.4f,.5f);yield return Flee(p);yield return Wait(2f);End();}
   if(Want("09")){d.BeginShift(0);d.PaperOpen=false;NoCard();foreach(var e in d.Docket)e.Result=ServiceResult.Served;Begin("09-depot-report");var line=RoadLine();int dep=Nearest(line,new Vector3(-8,0,-5));var lane=Lane(line,true);int s0=Mathf.Clamp(Nearest(lane,line[Mathf.Min(dep+8,line.Count-1)]),0,lane.Count-1);
    var seg=new List<Vector3>();for(int i=s0;i<lane.Count&&seg.Count<12;i++)seg.Add(lane[i]);seg.Add(new Vector3(-8,0,-1));d.Player.TeleportCar(seg[0]+Vector3.up*.3f,Quaternion.LookRotation(Flat(seg[1]-seg[0])));yield return Wait(.5f);yield return Drive(seg,9,"into the depot");yield return Wait(1f);
    if(d.CanFinish){d.TryFinishShift();Note("report filed");}else Note("cannot file the report here: "+d.Scene.Car.position);yield return Wait(11f);End();}
   // ---- NIGHT THREE: Vale, and an address that does not exist.
   if(Want("10")){d.BeginShift(2);d.PaperOpen=false;Begin("10-night3-vale-chase");yield return CardDone();var p=P(1);yield return ArriveAt(p,"Vale drive");yield return ExitCar();yield return ToDoor(p);yield return ReadNote(p);yield return Knock(p);yield return Deliver(p);yield return Wait(1.0f);
    if(d.Horror.Agent&&d.Horror.Active)yield return Look(d.Horror.Agent.transform.position+Vector3.up*1.4f,.5f);yield return Flee(p);yield return Wait(2f);End();}
   if(Want("11")){d.BeginShift(2);NoCard();Begin("11-night3-the-parcel");yield return Wait(1f);d.PaperOpen=false;d.SetCursor();d.Docket.Find(e=>e.Property==1).Result=ServiceResult.Served;
    var late=LateLine();var line=RoadLine();var north=Lane(line,false);int n0=Mathf.Max(0,north.Count-10);var seg=north.Skip(n0).ToList();
    if(late.Count>4)seg.AddRange(Lane(late,false));var p=P(2);int cut=Nearest(seg,p.Gate.position);seg=seg.Take(cut+1).ToList();seg.Add(p.Gate.position);
    d.Player.TeleportCar(seg[0]+Vector3.up*.3f,Quaternion.LookRotation(Flat(seg[1]-seg[0])));yield return Wait(.5f);yield return Drive(seg,10,"Route 9 to the parcel",true,140);
    yield return ExitCar();yield return ToDoor(p);yield return ReadNote(p);yield return Knock(p);yield return Deliver(p);yield return Wait(5f);yield return Look(p.Door.position+Vector3.up*1.4f,1.2f);yield return Wait(3f);
    foreach(var e in d.Docket)e.Result=ServiceResult.Served;d.Player.TeleportCar(new Vector3(-10,.3f,-1),Quaternion.Euler(0,0,0));d.Player.EnterCar();yield return Wait(.5f);d.Player.StopEngine();yield return Wait(.5f);if(d.CanFinish)d.TryFinishShift();yield return Wait(4f);d.NextShift();yield return Wait(14f);End();}
   // ---- V21: the county road end to end (verges, drive aprons, the closure) with the dice on the mirror; then grass and the torch.
   if(Want("14")){d.BeginShift(0);d.PaperOpen=false;NoCard();Begin("14-road-survey-and-dice");var line=RoadLine();var north=Lane(line,false);var seg=north.Skip(1).ToList();seg=seg.Take(seg.Count-2).ToList();
    d.Player.TeleportCar(seg[0]+Vector3.up*.3f,Quaternion.LookRotation(Flat(seg[1]-seg[0])));yield return Wait(.6f);d.Player.StartEngine();yield return Wait(1.2f);
    // a look up at the dice, then away up the road: hard pull-away, a stop, and on
    yield return Look(d.Scene.View.transform.position+d.Scene.Car.forward*2+d.Scene.Car.right*.9f+Vector3.up*.25f,1f);yield return Wait(.5f);
    {float t=Time.time+2.2f;while(Time.time<t){d.Player.SmokeThrottle=1;yield return null;}t=Time.time+1.4f;while(Time.time<t){d.Player.SmokeThrottle=0;d.Player.SmokeBrake=true;yield return null;}d.Player.SmokeBrake=false;yield return Wait(1.6f);}
    var dice=d.Scene.GetComponentInChildren<ServiceDice>(true);Note("dice swing peak during pull-away/brake: "+(dice?dice.Swing.ToString("F1"):"none"));
    yield return Look(d.Scene.View.transform.position+d.Scene.Car.forward*10,1f);
    int from=Nearest(seg,d.Scene.Car.position);yield return Drive(seg.Skip(from).ToList(),10,"county road to the closure",true,180);yield return Wait(1f);
    // stop on the road a few car lengths short of the closure, facing it square, headlights on it (the closure's root sits
    // at the county origin; the barrier blocks are where the road ends)
    var closure=d.Scene.transform.Find("North survey closure");var blocks=closure?closure.GetComponentsInChildren<Renderer>(true):new Renderer[0];
    if(blocks.Length>0){var bb=blocks[0].bounds;foreach(var r in blocks)bb.Encapsulate(r.bounds);var at=bb.center;int ci=Nearest(line,at),bi=ci;while(bi>0&&Vector3.Distance(Flat(line[bi]),Flat(at))<14)bi--;
     d.Player.SmokeBrake=true;d.Player.TeleportCar(line[bi]+Vector3.up*.3f,Quaternion.LookRotation(Flat(at-line[bi])));yield return Wait(1.2f);d.Player.SmokeBrake=false;
     yield return Look(at+Vector3.up*.3f,1f);Note($"closure {Vector3.Distance(Flat(d.Scene.Car.position),Flat(at)):F1} m ahead");}
    yield return Wait(3.5f);End();}
   if(Want("15")){d.BeginShift(0);d.PaperOpen=false;NoCard();Begin("15-grass-and-torch");var verge=d.Scene.transform.Find("V21 verge grass");var p=P(3);
    d.Player.TeleportCar(p.Gate.position+Vector3.up*.3f,p.Gate.rotation);yield return Wait(.5f);yield return ExitCar();
    // step clear of the car (it is solid now), then the densest verge clump on this side of it, through it and along the verge
    var car=d.Scene.Car;var side=Vector3.Dot(d.Scene.Walker.transform.position-car.position,car.right)<0?-car.right:car.right;
    yield return Walk(car.position+side*2.4f-car.forward*3.2f,"clear of the car");
    Vector3 here=d.Scene.Walker.transform.position,dense=here;if(verge){float best=0;foreach(Transform c in verge){if(Vector3.Distance(c.position,here)>30||Vector3.Dot(c.position-car.position,side)<0)continue;float k=0;foreach(Transform o in verge)if((o.position-c.position).sqrMagnitude<4)k++;if(k>best){best=k;dense=c.position;}}}
    yield return Walk(dense,"into the verge grass");var along=Vector3.Cross(Vector3.up,side);yield return Walk(dense+along*6,"along the verge");
    d.ToggleTorch();yield return Wait(1.2f);d.ToggleTorch();yield return Wait(1f);Note("grass rustles "+(d.Foliage?d.Foliage.Rustles:-1));
    yield return Walk(car.position+side*2.2f,"back to the car",true);yield return Wait(1f);End();}
   if(Want("12")){Begin("12-menus-title-options-pause");var menus=FindAnyObjectByType<ServiceMenus>();d.Title();Note("title, intro card");yield return Wait(7f);Note("menus "+(menus?menus.SmokeState:"missing"));
    if(menus){yield return Wait(3f);
     menus.SmokeSelect("NEW ROUTE");yield return Wait(.9f);menus.SmokeSelect("OPTIONS");yield return Wait(.9f);menus.SmokeSelect("QUIT");yield return Wait(.9f);menus.SmokeSelect("OPTIONS");yield return Wait(.7f);menus.SmokeSubmit();Note("options "+menus.SmokeState);yield return Wait(2f);
     menus.SmokeSelect("CAMERA MOVEMENT");yield return Wait(.6f);menus.SmokeShift(-1);yield return Wait(.6f);menus.SmokeShift(1);yield return Wait(1f);
     menus.SmokeTab(1);yield return Wait(1.6f);menus.SmokeSelect("CAMERA FILTER");yield return Wait(.6f);menus.SmokeShift(1);Note("filter extreme");yield return Wait(2.4f);menus.SmokeShift(-1);yield return Wait(1.2f);
     menus.SmokeTab(1);yield return Wait(1.5f);menus.SmokeTab(1);yield return Wait(2.2f);menus.Back();Note("back "+menus.SmokeState);yield return Wait(1.5f);
     menus.SmokeSelect("QUIT");yield return Wait(.8f);menus.SmokeSubmit();Note("quit asked "+menus.SmokeState);yield return Wait(2.2f);menus.SmokeSelect("YES");yield return Wait(.8f);menus.SmokeSelect("NO");yield return Wait(.8f);menus.Back();Note("cancelled "+menus.SmokeState);
     yield return Wait(16f);Note("title shot index "+(d.Presentation?d.Presentation.ShotIndex:-1));
     menus.SmokeSelect("NEW ROUTE");yield return Wait(1f);menus.SmokeSubmit();yield return Wait(1.6f);if(menus.SmokeSelect("YES")){Note("new route confirm shown");yield return Wait(1f);menus.SmokeSubmit();}yield return Wait(8f);Note("phase "+d.Phase);
     d.PaperOpen=false;d.SetCursor();yield return Wait(2f);d.Pause();yield return Wait(2.5f);Note("pause "+menus.SmokeState);menus.SmokeSelect("RESTART NIGHT");yield return Wait(.8f);menus.SmokeSubmit();Note("restart asked "+menus.SmokeState);yield return Wait(2f);menus.Back();yield return Wait(1f);
     menus.SmokeSelect("OPTIONS");yield return Wait(.8f);menus.SmokeSubmit();yield return Wait(2f);menus.Back();yield return Wait(1f);menus.SmokeSelect("RESUME");yield return Wait(.6f);menus.SmokeSubmit();yield return Wait(2f);Note("resumed "+d.Phase);}
    End();}
   if(Want("13")){d.BeginShift(1);d.PaperOpen=false;NoCard();Begin("13-clipboard-pause-and-a-death");yield return Wait(4f);d.ToggleDocument(false);yield return Wait(5f);d.PaperOpen=false;d.MapOpen=false;d.SetCursor();yield return Wait(1f);
    d.Pause();yield return Wait(3f);var hud=d.GetComponentInChildren<ServiceHUD>();if(hud){hud.SmokeOptions(true);yield return Wait(4f);hud.SmokeOptions(false);}d.Resume();yield return Wait(1f);
    var p=P(3);yield return ExitCar();d.Player.SmokePlaceWalker(p.TableApproach.position);yield return Wait(.6f);d.Horror.Begin(3);yield return Wait(.3f);if(d.Horror.Agent)yield return Look(d.Horror.Agent.transform.position+Vector3.up*1.3f,.8f);yield return Wait(9f);Note($"stare: caught={d.Horror.Caught}");End();}
  }
 }
}
