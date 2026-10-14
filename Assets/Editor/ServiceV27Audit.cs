using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V27 placement audit ("you still have horrible issues with asset placements... a lot of misplacements"). Every prop
 // in and around the houses is measured, not eyeballed: floating above or sunk into what holds it, pushed into a wall
 // or another prop, off its wall, under/clear of its ceiling, tipped over, doubled, standing in a doorway. Each finding
 // gets a picture. Audit/placement27/report.txt (+ issues.json for the fix stage).
 public static partial class ServiceV19Rebuild {
  class Prop{public Transform root;public ServiceProperty house;public string kind;public string on;public Vector3 c,e;public Quaternion r;public List<Renderer> rs;public HashSet<Collider> own;}
  [System.Serializable] public class Issue27{public string path,house,kind,problem;public float amount;public Vector3 at,size;public string other;}
  [System.Serializable] class Issues27{public List<Issue27> items=new List<Issue27>();}
  static readonly string[] WallWords={"painting","picture","frame","mirror","sconce","wall_lamp","wall_clock","bull_head","dartboard","telephone_wall","industrial_wall","wall-"};
  static readonly string[] CeilWords={"chandelier","ceiling","pendant","hanging_light","caged_hanging","pull_chain","lantern_chandelier","cord"};
  static readonly string[] FlatWords={"rug","mat","runner","doormat","blood"};
  static string KindOf(string id,string name,Dictionary<string,LayoutItem> lay){
   var n=(id+" "+name).ToLowerInvariant();if(lay.TryGetValue(id,out var li)){if(li.ceiling)return "ceiling";if(li.wall)return "wall";if(!string.IsNullOrEmpty(li.on))return "top";}
   if(CeilWords.Any(n.Contains))return "ceiling";if(WallWords.Any(n.Contains))return "wall";if(FlatWords.Any(n.Contains))return "flat";if(n.Contains("shelf books")||n.Contains("v23 shelf"))return "top";return "floor";}
  // the prop's box: the union of its renderers' world bounds, turned into the root's yaw (exact for the quarter-turn
  // layouts; the local-bounds route over-measured scaled imports by up to 1.6x and made false "into wall" findings)
  static bool OOB(Transform root,out Vector3 c,out Vector3 e,out Quaternion r,out List<Renderer> rs){
   rs=root.GetComponentsInChildren<Renderer>(false).Where(x=>x.enabled&&x.gameObject.activeInHierarchy&&!(x is ParticleSystemRenderer)&&!(x is LineRenderer)).ToList();c=e=Vector3.zero;
   float yaw=Mathf.Round(root.eulerAngles.y/90f)*90f;r=Quaternion.Euler(0,yaw,0);if(rs.Count==0)return false;
   var bb=rs[0].bounds;foreach(var x in rs)bb.Encapsulate(x.bounds);c=bb.center;var ext=bb.extents;
   bool quarter=Mathf.Abs(Mathf.DeltaAngle(yaw,0))>45&&Mathf.Abs(Mathf.DeltaAngle(yaw,180))>45;e=quarter?new Vector3(ext.z,ext.y,ext.x):ext;
   return e.x>.005f||e.y>.005f||e.z>.005f;}
  static Dictionary<string,LayoutItem> LayoutIds(int house){var f=Path.Combine(Work,"Audit","layouts",$"p{house}.json");var d=new Dictionary<string,LayoutItem>();if(!File.Exists(f))return d;foreach(var i in JsonUtility.FromJson<Layout>(File.ReadAllText(f)).items)d[i.id]=i;return d;}
  static string IdOf(Transform t){var n=t.name;int k=n.IndexOf(" — ");return k>0?n.Substring(0,k):n;}
  static IEnumerable<Transform> PropRoots(ServiceProperty p){
   var interior=p.transform.Find("V19 interior");if(interior)foreach(Transform t in interior)yield return t;
   foreach(Transform t in p.transform){if(t==interior||(p.Building&&t==p.Building))continue;var n=t.name;
    if(n.StartsWith("V2")||n.StartsWith("Prop_")||n.Contains(" — "))yield return t;}}
  public static void Probe27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   void L(Light l)=>sb.AppendLine($"  light {PathOf(l.transform).Replace("SERVICE — Hollis County/","")} {l.type} int {l.intensity} range {l.range} spot {l.spotAngle}/{l.innerSpotAngle} col {l.color} shadows {l.shadows} enabled {l.enabled} active {l.gameObject.activeInHierarchy}");
   sb.AppendLine("== torch");if(county.Flashlight)L(county.Flashlight);
   sb.AppendLine("== car lights");foreach(var l in county.Car.GetComponentsInChildren<Light>(true))L(l);
   sb.AppendLine($"== render: fog {RenderSettings.fog} {RenderSettings.fogMode} density {RenderSettings.fogDensity} col {RenderSettings.fogColor} ambient {RenderSettings.ambientMode} {RenderSettings.ambientLight} sky {RenderSettings.ambientSkyColor} view far {county.View.farClipPlane} fov {county.View.fieldOfView}");
   var w=county.Walker;sb.AppendLine($"== walker cc h {w.height} r {w.radius} step {w.stepOffset} slope {w.slopeLimit} camera local y {county.View.transform.localPosition.y}");
   var dog=county.transform.Find("Correll yard dog");if(dog){var rs=dog.GetComponentsInChildren<Renderer>(true).Where(r=>r.gameObject.activeSelf);var bb=new Bounds(dog.position,Vector3.zero);foreach(var r in rs)bb.Encapsulate(r.bounds);sb.AppendLine($"== dog bounds {bb.size:F2} scale {dog.lossyScale} kids {string.Join(",",dog.Cast<Transform>().Select(t=>t.name+"("+t.localScale.ToString("F2")+")"))}");}
   sb.AppendLine("== doors");foreach(var p in county.Properties.OrderBy(x=>x.Index))sb.AppendLine($"  p{p.Index} panel {(p.DoorPanel?PathOf(p.DoorPanel).Replace("SERVICE — Hollis County/",""):"-")} swing {p.DoorSwing} width {p.DoorWidth:F2} inward {p.Inward:F2} panel fwd {(p.DoorPanel?p.DoorPanel.forward.ToString("F2"):"-")} local rot {(p.DoorPanel?p.DoorPanel.localEulerAngles.ToString("F0"):"-")}");
   sb.AppendLine("== rails (renderer -> has collider)");foreach(var p in county.Properties.OrderBy(x=>x.Index)){var rails=p.GetComponentsInChildren<MeshRenderer>(true).Where(r=>{var n=r.name.ToLowerInvariant();return n.Contains("rail")||n.Contains("balust")||n.Contains("banister")||n.Contains("fence");}).ToList();
    sb.AppendLine($"  p{p.Index}: {rails.Count} rail parts, {rails.Count(r=>r.GetComponentInChildren<Collider>()||r.GetComponentInParent<Collider>())} with colliders; names {string.Join(",",rails.Select(r=>r.name).Distinct().Take(8))}");}
   sb.AppendLine("== mailboxes / roadside props");foreach(var t in county.GetComponentsInChildren<Transform>(true).Where(t=>{var n=t.name.ToLowerInvariant();return n.Contains("mailbox")||n.StartsWith("street sign")||n.Contains("barricade")||n.Contains("barrier");}))sb.AppendLine($"  {PathOf(t).Replace("SERVICE — Hollis County/","")} colliders {t.GetComponentsInChildren<Collider>(true).Length} at {t.position:F1}");
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","probe27.txt"),sb.ToString());}
  // can each house's monster get from where it starts to the yard (and the drive) on the navmesh?
  public static void NavOut27(){Open();Physics.SyncTransforms();var nav=UnityEngine.AI.NavMesh.AddNavMeshData(county.Navigation);var sb=new StringBuilder();
   try{foreach(var p in county.Properties.OrderBy(x=>x.Index)){var o=p.Door.position-p.InteriorBounds.center;o.y=0;o.Normalize();
    foreach(var (label,from) in new[]{("spawn",p.EntitySpawn.position),("table",p.TableApproach.position),("door-in",p.OpeningCentre+p.Inward*1.2f)})
     foreach(var (tl,to) in new[]{("porch+3",p.Door.position+o*3f),("yard+8",p.Door.position+o*8f),("drive-mid",p.ApproachRoute!=null&&p.ApproachRoute.Length>2?p.ApproachRoute[p.ApproachRoute.Length/2]:p.Gate.position),("gate",p.Gate.position)}){
      var path=new UnityEngine.AI.NavMeshPath();bool a=UnityEngine.AI.NavMesh.SamplePosition(from,out var ha,2f,UnityEngine.AI.NavMesh.AllAreas),b2=UnityEngine.AI.NavMesh.SamplePosition(to,out var hb,3f,UnityEngine.AI.NavMesh.AllAreas);
      string st="no sample";float len=0;Vector3 end=Vector3.zero;if(a&&b2){UnityEngine.AI.NavMesh.CalculatePath(ha.position,hb.position,UnityEngine.AI.NavMesh.AllAreas,path);st=path.status.ToString();for(int i=1;i<path.corners.Length;i++)len+=Vector3.Distance(path.corners[i-1],path.corners[i]);end=path.corners.Length>0?path.corners[path.corners.Length-1]:Vector3.zero;}
      sb.AppendLine($"p{p.Index} {label}->{tl}: {st} len {len:F1} end {end:F1} (wanted {hb.position:F1}) gap {Vector3.Distance(end,hb.position):F1}");}}}
   finally{nav.Remove();}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","navout27.txt"),sb.ToString());}
  // each front door swung open as the game swings it (DoorSwing about the panel's local up), seen from out and in
  public static void Doors27(){Open();var dir=Path.Combine(Work,"Audit","placement27","doors");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=70;cam.aspect=16f/9f;bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   try{foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.DoorPanel)continue;var rest=p.DoorPanel.localRotation;
     foreach(var (tag,deg) in new[]{("shut",0f),("half",.5f),("open",1f)}){p.DoorPanel.localRotation=rest*Quaternion.Euler(0,p.DoorSwing*deg,0);
      var oc=p.OpeningCentre;var inw=p.Inward;inw.y=0;inw.Normalize();
      cam.transform.position=oc-inw*3.2f+Vector3.up*1.6f;cam.transform.LookAt(oc+Vector3.up*1.1f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-{tag}-out.jpg"),640,360);
      cam.transform.position=oc+inw*3.2f+Vector3.up*1.6f;cam.transform.LookAt(oc+Vector3.up*1.1f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-{tag}-in.jpg"),640,360);}
     p.DoorPanel.localRotation=rest;}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}}
  public static void DoorLeaves27(){Open();var sb=new StringBuilder();
   foreach(var p in county.Properties.OrderBy(x=>x.Index))foreach(var r in p.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.ToLowerInvariant().Contains("door")&&(!p.DoorPanel||!r.transform.IsChildOf(p.DoorPanel)))){
    var b=r.bounds;sb.AppendLine($"p{p.Index} {PathOf(r.transform).Replace("SERVICE — Hollis County/","")} active {r.gameObject.activeInHierarchy} at {b.center:F2} size {b.size:F2} up {r.transform.up:F2} dist-to-door {Vector3.Distance(b.center,p.OpeningCentre):F1}");}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","doorleaves.txt"),sb.ToString());}
  // V27 fix: props pushed back out of the walls they are set into (rays from the prop's middle find the wall surface
  // inside its footprint; it moves back by exactly that much), sunk ones raised and floating ones set down onto what
  // is under them. Whatever stands on a prop (a pot on the stove, glasses on the table) moves with it. The doubled
  // Vale clock is taken away. Hearths, fires, shelf rows and wall/ceiling pieces are left alone.
  public static void Fix27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();int moved=0;
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){var lay=LayoutIds(p.Index);
    var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var byId=new Dictionary<string,Transform>();foreach(var t in roots)byId[IdOf(t)]=t;
    var deps=new Dictionary<Transform,List<Transform>>();foreach(var t in roots)if(lay.TryGetValue(IdOf(t),out var li)&&!string.IsNullOrEmpty(li.on)&&byId.TryGetValue(li.on,out var sup)){if(!deps.ContainsKey(sup))deps[sup]=new List<Transform>();deps[sup].Add(t);}
    void Move(Transform t,Vector3 delta){t.position+=delta;if(deps.TryGetValue(t,out var ds))foreach(var c in ds)Move(c,delta);}
    bool Skip(Transform t){var n=t.name.ToLowerInvariant();return n.Contains("hearth")||n.Contains("fire")||n.Contains("shelf books")||n.Contains("blood")||n.Contains("apparition")||n.Contains("stair book")||n.Contains("doormat");}
    foreach(var t in roots){if(Skip(t))continue;var kind=KindOf(IdOf(t),t.name,lay);if(kind!="floor")continue;if(!OOB(t,out var c,out var e,out var r,out var rs))continue;
     var own=new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));Vector3 push=Vector3.zero;bool squeezed=false;
     float bottom=float.MaxValue,top=float.MinValue;foreach(var x in rs){bottom=Mathf.Min(bottom,x.bounds.min.y);top=Mathf.Max(top,x.bounds.max.y);}
     // from the room side: a ray starting past the prop's far side from this wall, through the prop (ignored), finds the
     // wall's inner face - however deep in (or right through) the wall the prop's back has gone
     foreach(var (dv,half) in new[]{(r*Vector3.forward,e.z),(r*Vector3.back,e.z),(r*Vector3.right,e.x),(r*Vector3.left,e.x)}){float hitAt=99;
      foreach(float hy in new[]{bottom+.25f,(bottom+top)*.5f,top-.15f}){var o=new Vector3(c.x,hy,c.z)-dv*(half+.35f);if(!ServiceGameV2.ServiceLife.UnderRoof(o-Vector3.up*.9f))continue;
       foreach(var h in Physics.RaycastAll(o,dv,half*2+.4f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(own.Contains(h.collider)||h.collider.transform.IsChildOf(t))continue;
        if(!(p.Building&&h.collider.transform.IsChildOf(p.Building)))break;if(Mathf.Abs(h.normal.y)>.5f||Vector3.Dot(h.normal,dv)>0)continue;hitAt=Mathf.Min(hitAt,h.distance-(half+.35f));break;}}
      if(hitAt<half-.015f&&hitAt>-half){var need=-dv*(half-hitAt+.012f);if(Vector3.Dot(push,dv)>.001f){squeezed=true;}push+=need;}}
     if(squeezed){sb.AppendLine($"p{p.Index} {IdOf(t)} touches walls on both sides - left");continue;}
     if(push.sqrMagnitude>1e-6f){if(push.magnitude>.6f){sb.AppendLine($"p{p.Index} {IdOf(t)} needs {push.magnitude:F2} m - left for a hand fix");continue;}Move(t,push);moved++;sb.AppendLine($"p{p.Index} out of the wall: {IdOf(t)} by {push:F3} ({push.magnitude:F2} m)");Physics.SyncTransforms();}
     // set it on what is under it (floors only: the building)
     if(!OOB(t,out c,out e,out r,out rs))continue;bottom=float.MaxValue;foreach(var x in rs)bottom=Mathf.Min(bottom,x.bounds.min.y);var gaps=new List<float>();
     for(int a=-1;a<=1;a++)for(int b=-1;b<=1;b++){var o=c+r*new Vector3(e.x*.6f*a,0,e.z*.6f*b);o.y=bottom+.3f;foreach(var h in Physics.RaycastAll(o,Vector3.down,1.5f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(own.Contains(h.collider)||h.collider.transform.IsChildOf(t))continue;gaps.Add(bottom-h.point.y);break;}}
     if(gaps.Count>=5){gaps.Sort();float med=gaps[gaps.Count/2],lo=gaps[0];if(lo>.02f&&lo<.5f){Move(t,Vector3.down*lo);moved++;sb.AppendLine($"p{p.Index} set down: {IdOf(t)} by {lo:F3}");}else if(med<-.03f&&med>-.3f){Move(t,Vector3.up*(-med));moved++;sb.AppendLine($"p{p.Index} raised: {IdOf(t)} by {-med:F3}");}}}
   }
   {var clocks=county.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("vintage_grandfather_clock_01")&&t.parent&&t.parent.name=="V19 interior").ToList();
    foreach(var a2 in clocks)foreach(var b2 in clocks)if(a2!=b2&&a2.gameObject.activeSelf&&b2.gameObject.activeSelf&&Vector3.Distance(a2.position,b2.position)<1.6f&&string.CompareOrdinal(a2.name,b2.name)<0){b2.gameObject.SetActive(false);moved++;sb.AppendLine($"doubled clock hidden: {PathOf(b2)} (beside {a2.name})");}}
   sb.Insert(0,$"Fix27: {moved} changes"+System.Environment.NewLine);log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","placement27","fix27.txt"),sb.ToString());Save("Fix27");}
  // close looks at named props: from the room (navmesh side) and from outside the wall behind them, plus their sizes
  public static void Look27(){Open();Physics.SyncTransforms();var nav=UnityEngine.AI.NavMesh.AddNavMeshData(county.Navigation);var dir=Path.Combine(Work,"Audit","placement27","look");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var want=new[]{(0,"kitchen-stove"),(2,"kitchen-stove"),(4,"kitchen-stove"),(3,"ne-clock"),(3,"dining-lantern"),(1,"landing-clock"),(5,"hall-clock")};var sb=new StringBuilder();
   var envList=System.Environment.GetEnvironmentVariable("SERVICE_LOOK");if(!string.IsNullOrEmpty(envList))want=envList.Split(',').Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1])).ToArray();
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=62;cam.aspect=4f/3f;
   try{foreach(var (pi,id) in want){var p=county.Properties.First(x=>x.Index==pi);var t=PropRoots(p).FirstOrDefault(x=>IdOf(x)==id);if(!t||!OOB(t,out var c,out var e,out var r,out var rs))continue;
     sb.AppendLine($"p{pi} {id} at {c:F2} half {e:F2} rot {r.eulerAngles:F0} renderers {string.Join(",",rs.Select(x=>x.name+" "+x.bounds.size.ToString("F2")))}");
     // room side: nearest navmesh spot within 3 m
     Vector3 eye=c+Vector3.up;float best=99;for(int a=0;a<16;a++){var dv=Quaternion.Euler(0,a*22.5f,0)*Vector3.forward;if(UnityEngine.AI.NavMesh.SamplePosition(c+dv*2.2f,out var h,.9f,UnityEngine.AI.NavMesh.AllAreas)&&Mathf.Abs(h.position.y-(c.y-e.y))<1f&&ServiceGameV2.ServiceLife.UnderRoof(h.position)){float dd=Vector3.Distance(h.position,c+dv*2.2f);if(dd<best){best=dd;eye=h.position+Vector3.up*1.5f;}}}
     cam.transform.position=eye;cam.transform.LookAt(c);Shoot(cam,Path.Combine(dir,$"p{pi}-{id}-room.jpg"),640,480);
     var away=c-eye;away.y=0;away.Normalize();cam.transform.position=c+away*3f+Vector3.up*.6f;cam.transform.LookAt(c);Shoot(cam,Path.Combine(dir,$"p{pi}-{id}-behind.jpg"),640,480);
     /* grazing: along the wall from either side, close in */var side=Vector3.Cross(Vector3.up,away);foreach(var sg in new[]{1f,-1f}){cam.transform.position=c-away*.45f+side*sg*1.1f+Vector3.up*.15f;cam.transform.LookAt(c);Shoot(cam,Path.Combine(dir,$"p{pi}-{id}-graze{(sg>0?"L":"R")}.jpg"),640,480);}}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);nav.Remove();}
   File.WriteAllText(Path.Combine(dir,"look.txt"),sb.ToString());}
  public static void Roots27(){Open();var sb=new StringBuilder();
   void Walk(Transform t,int depth){var rs=t.GetComponentsInChildren<Renderer>(true);sb.AppendLine($"{new string(' ',depth*2)}{t.name} ({t.childCount} kids, {rs.Length} renderers, {rs.Count(r=>r.gameObject.activeInHierarchy)} active)");if(depth<1)foreach(Transform c in t)if(c.GetComponentsInChildren<Renderer>(true).Length>0)Walk(c,depth+1);}
   Walk(county.transform,0);File.WriteAllText(Path.Combine(Work,"Audit","placement27","roots.txt"),sb.ToString());}
  // V27 outside: every tree, thicket, rock, log, scan, pole and sign measured against the ground under it, the houses
  // and the roads/drives. Exterior27 reports; Exterior27Fix sets floaters down, raises the sunk, and hides what stands
  // in a house or in the road. Grass clumps are counted, not listed.
  public static void Exterior27(){Exterior(false);}
  public static void Exterior27Fix(){Exterior(true);}
  static float DistToLine(Vector3 at,IList<Vector3> pts){float best=1e9f;for(int i=1;i<pts.Count;i++){var a=pts[i-1];var b=pts[i];a.y=b.y=at.y;var ab=b-a;float len=ab.magnitude;if(len<1e-4f)continue;float k=Mathf.Clamp01(Vector3.Dot(at-a,ab)/(len*len));best=Mathf.Min(best,Vector3.Distance(at,a+ab*k));}return best;}
  static void Exterior(bool fix){Open();Physics.SyncTransforms();var terr=county.GetComponentsInChildren<Terrain>().FirstOrDefault();var sb=new StringBuilder();var dir=Path.Combine(Work,"Audit","placement27");
   var groups=new[]{"Winter woodland","Forest understory thickets","Scanned woodland details","V17 forest floor","V20 utility poles","Street sign","Depot sign","V23 open grass"};
   var roads=new List<List<Vector3>>();if(county.Route!=null)roads.Add(county.Route.Where(x=>x).Select(x=>x.position).ToList());if(county.LateRoute!=null)roads.Add(county.LateRoute.Where(x=>x).Select(x=>x.position).ToList());
   var drives=county.Properties.Where(p=>p.ApproachRoute!=null&&p.ApproachRoute.Length>1).Select(p=>p.ApproachRoute.ToList()).ToList();
   var houses=county.Properties.Select(p=>{var bb=p.InteriorBounds;if(p.Building){var rs=p.Building.GetComponentsInChildren<Renderer>();if(rs.Length>0){bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);}}return bb;}).ToList();
   int floating=0,sunk=0,inHouse=0,onRoad=0,checkedN=0,grassOff=0;var shots=new List<(string,Vector3,float)>();
   foreach(var g in county.transform.Cast<Transform>().Where(t=>groups.Any(n=>t.name.StartsWith(n)))){bool grass=g.name.StartsWith("V23 open grass");
    var units=g.childCount>0?g.Cast<Transform>().ToList():new List<Transform>{g};
    foreach(var u in units){if(!u.gameObject.activeInHierarchy)continue;var rs=u.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToList();if(rs.Count==0)continue;checkedN++;
     var bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);var c=bb.center;float ground=terr?terr.SampleHeight(c)+terr.transform.position.y:0;
     // the lowest ground under a quarter of its footprint (a tree on a slope)
     float lo=ground,hi=ground;foreach(var o in new[]{new Vector3(bb.extents.x*.25f,0,0),new Vector3(-bb.extents.x*.25f,0,0),new Vector3(0,0,bb.extents.z*.25f),new Vector3(0,0,-bb.extents.z*.25f)}){float h=terr?terr.SampleHeight(c+o)+terr.transform.position.y:0;lo=Mathf.Min(lo,h);hi=Mathf.Max(hi,h);}
     // what it stands on might be a road mesh or a rock rather than terrain: the first collider under its base
     float support=lo;if(Physics.Raycast(new Vector3(c.x,bb.min.y+1.5f,c.z),Vector3.down,out var hit,6f,~0,QueryTriggerInteraction.Ignore)&&!hit.collider.transform.IsChildOf(u))support=Mathf.Max(lo,hit.point.y);
     float gap=bb.min.y-support;bool tall=bb.size.y>1.5f;string name=PathOf(u).Replace("SERVICE — Hollis County/","");
     if(grass){if(gap>.15f){grassOff++;if(fix)u.position+=Vector3.down*gap;}continue;}
     float limit=tall?.12f:.08f;
     if(gap>limit&&gap<6f){floating++;if(floating<=60)sb.AppendLine($"floating {gap:F2} m: {name} at {c:F1} size {bb.size:F1}");if(shots.Count<24)shots.Add(("float",c,bb.size.magnitude));if(fix)u.position+=Vector3.down*(gap-.02f);}
     else if(bb.min.y<hi-Mathf.Max(.6f,bb.size.y*.45f)&&!tall){sunk++;if(sunk<=30)sb.AppendLine($"sunk {hi-bb.min.y:F2} m: {name} at {c:F1} size {bb.size:F1}");}
     var flat=new Vector3(c.x,c.y,c.z);
     for(int h=0;h<houses.Count;h++){var hb=houses[h];var core=new Bounds(hb.center,hb.size-new Vector3(1.2f,0,1.2f));if(core.Contains(new Vector3(c.x,hb.center.y,c.z))&&bb.max.y>hb.min.y+.5f&&!name.Contains("Delivery")){inHouse++;sb.AppendLine($"in a house (p{county.Properties[h].Index}): {name} at {c:F1} size {bb.size:F1}");if(shots.Count<30)shots.Add(("house",c,bb.size.magnitude));if(fix)u.gameObject.SetActive(false);break;}}
     float rd=roads.Count>0?roads.Min(r=>DistToLine(flat,r)):99,dv=drives.Count>0?drives.Min(r=>DistToLine(flat,r)):99;float half=Mathf.Max(bb.extents.x,bb.extents.z);
     if(!name.StartsWith("Street sign")&&!name.StartsWith("Depot")&&!name.StartsWith("V20 utility")&&(rd<3.4f+half*.4f||dv<1.5f+half*.4f)&&bb.size.y>.25f){onRoad++;if(onRoad<=40)sb.AppendLine($"on the {(rd<3.4f+half*.4f?"road":"drive")}: {name} at {c:F1} size {bb.size:F1} ({Mathf.Min(rd,dv):F1} m off the centre line)");if(shots.Count<36)shots.Add(("road",c,bb.size.magnitude));if(fix)u.gameObject.SetActive(false);}
    }}
   sb.Insert(0,$"checked {checkedN}: floating {floating}, sunk {sunk}, in a house {inHouse}, on a road/drive {onRoad}, grass off the ground {grassOff}{(fix?"  (FIXED)":"")}"+System.Environment.NewLine);
   if(!fix){var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=60;cam.aspect=4f/3f;int k=0;
    try{foreach(var (tag,at,size) in shots){var back=Mathf.Clamp(size*1.2f,3f,14f);cam.transform.position=at+new Vector3(.7f,0,-.7f).normalized*back+Vector3.up*Mathf.Clamp(size*.3f,1f,4f);cam.transform.LookAt(at);Shoot(cam,Path.Combine(dir,$"x{k:00}-{tag}.jpg"),480,360);k++;}}
    finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}}
   File.WriteAllText(Path.Combine(dir,fix?"exterior-fix.txt":"exterior.txt"),sb.ToString());if(fix){log.Append(sb.ToString());Save("Exterior27Fix");}}
  public static void Shelves27(){Open();var dir=Path.Combine(Work,"Audit","placement27","shelves");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=50;cam.aspect=4f/3f;int k=0;var sb=new StringBuilder();
   try{foreach(var row in county.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("V23 shelf books")&&t.gameObject.activeInHierarchy)){
     var rs=row.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;var bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);
     // the board under the row: first collider below its lowest book that is not a book
     float board=float.NaN;foreach(var h in Physics.RaycastAll(bb.center+Vector3.up*.02f,Vector3.down,1.2f,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(h.collider.transform.IsChildOf(row))continue;board=h.point.y;break;}
     sb.AppendLine($"{PathOf(row).Replace("SERVICE — Hollis County/","")} books bottom {bb.min.y:F3} first collider below {board:F3} gap {(bb.min.y-board):F3} fw {row.forward:F2}");
     foreach(var (tag,sgn) in new[]{("fw",1f),("back",-1f)}){var fwd=row.forward;fwd.y=0;fwd.Normalize();cam.transform.position=bb.center+fwd*sgn*1.1f+Vector3.up*.12f;cam.transform.LookAt(bb.center);Shoot(cam,Path.Combine(dir,$"s{k:00}-{tag}.jpg"),400,300);}k++;}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}File.WriteAllText(Path.Combine(dir,"shelves.txt"),sb.ToString());}
  // V27 artifacting: 31 shadow-casting lights did not fit the shadow atlas, so URP halved every shadow (blocky, crawling
  // edges). Interior lamps and room fills now light without casting; the torch, headlights, porch lights and the hearth
  // fires keep their shadows. Harrow's dining lantern comes down out of the sloped ceiling.
  public static void Lights27(){Open();var sb=new StringBuilder();int off=0,kept=0;
   foreach(var l in county.GetComponentsInChildren<Light>(true)){if(l.shadows==LightShadows.None)continue;var n=(l.name+" "+(l.transform.parent?l.transform.parent.name:"")).ToLowerInvariant();
    bool keep=l==county.Flashlight||n.Contains("beam")||n.Contains("porch")||n.Contains("fire")||n.Contains("moon")||l.type==LightType.Directional;
    if(keep){kept++;sb.AppendLine($"kept {PathOf(l.transform).Replace("SERVICE — Hollis County/","")} {l.type} {l.shadows}");continue;}
    l.shadows=LightShadows.None;off++;sb.AppendLine($"no shadows: {PathOf(l.transform).Replace("SERVICE — Hollis County/","")} {l.type}");}
   var lantern=county.Properties.First(x=>x.Index==3).transform.Find("V19 interior");var lt=lantern?lantern.Cast<Transform>().FirstOrDefault(t=>t.name.StartsWith("dining-lantern")):null;
   if(lt){lt.position+=Vector3.down*.24f;sb.AppendLine("Harrow dining lantern lowered 0.24 m");}
   sb.Insert(0,$"Lights27: {off} lights stop casting shadows, {kept} keep them"+System.Environment.NewLine);log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","placement27","lights27.txt"),sb.ToString());Save("Lights27");}
  public static void Dog27(){Open();var dog=county.transform.Find("Correll yard dog");var sb=new StringBuilder();
   foreach(Transform c in dog){var rs=c.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)continue;var bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);
    var bones=c.GetComponentsInChildren<Transform>(true).Where(t=>{var n=t.name.ToLowerInvariant();return n.Contains("spine")||n.Contains("neck")||n.Contains("head")||n.Contains("shoulder")||n.Contains("clav")||n.Contains("pelvis");}).Select(t=>t.name+" y"+(t.position.y-bb.min.y).ToString("F2"));
    sb.AppendLine($"{c.name} active {c.gameObject.activeSelf} scale {c.localScale:F2} bounds {bb.size:F2} bones {string.Join(", ",bones.Take(14))}");}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","dog27.txt"),sb.ToString());}
  // V27 ("I keep missing the driveways... maybe more light"): a lamp post at the mouth of every drive (Poly Haven
  // street_lamp_01, CC0, already in the project) with a warm sodium light over the mailbox. Not at the Route 9 house.
  public static void DriveLamps27(){Open();Physics.SyncTransforms();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ServiceArt/V19/Props/street_lamp_01/street_lamp_01.prefab");var terr=county.GetComponentsInChildren<Terrain>().FirstOrDefault();
   var sb=new StringBuilder();var dir=Path.Combine(Work,"Audit","placement27","lamps");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var shots=new List<(Vector3,Vector3,string)>();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){var old=p.transform.Find("V27 drive lamp");if(old)Object.DestroyImmediate(old.gameObject);if(p.Index==2||p.ApproachRoute==null||p.ApproachRoute.Length<3)continue;
    var mouth=p.ApproachRoute[0];var into=p.ApproachRoute[2]-mouth;into.y=0;into.Normalize();var side=Vector3.Cross(Vector3.up,into);
    // the side of the drive away from the mailbox, two and a half metres off the centre line, a step back from the road
    var mb=p.Mailbox?p.Mailbox.position:mouth+side*3;if(Vector3.Dot(mb-mouth,side)>0)side=-side;var at=mouth+side*2.6f+into*1.2f;if(terr)at.y=terr.SampleHeight(at)+terr.transform.position.y;
    var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,p.transform);g.name="V27 drive lamp";g.transform.position=at;g.transform.rotation=Quaternion.LookRotation(-side,Vector3.up);
    var rs=g.GetComponentsInChildren<Renderer>();var bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);float sink=bb.min.y-at.y;g.transform.position-=Vector3.up*(sink+.03f);
    bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);
    var lg=new GameObject("Drive lamp light");lg.transform.SetParent(g.transform,false);lg.transform.position=new Vector3(bb.center.x,bb.max.y-.35f,bb.center.z);var l=lg.AddComponent<Light>();l.type=LightType.Point;l.range=15f;l.intensity=1.7f;l.color=new Color(1f,.76f,.46f);l.shadows=LightShadows.None;
    var col=g.AddComponent<CapsuleCollider>();col.center=g.transform.InverseTransformPoint(new Vector3(at.x,at.y+bb.size.y*.5f,at.z));col.height=bb.size.y;col.radius=.12f;
    sb.AppendLine($"p{p.Index} lamp at {at:F1}, {bb.size.y:F1} m tall, light at {lg.transform.position:F1}");
    shots.Add((mouth-into*14f+Vector3.up*1.3f,at+Vector3.up*2,$"p{p.Index}-road"));}
   var muted=MuteFeatures();var cam=AuditCam();cam.fieldOfView=60;cam.aspect=16f/9f;try{foreach(var (eye,look,tag) in shots){cam.transform.position=eye;cam.transform.LookAt(look);Shoot(cam,Path.Combine(dir,tag+".jpg"),640,360);}}finally{Object.DestroyImmediate(cam.gameObject);Restore(muted);}
   log.Append(sb.ToString());File.WriteAllText(Path.Combine(dir,"lamps.txt"),sb.ToString());Save("DriveLamps27");}
  public static void Magenta27(){Open();var sb=new StringBuilder();
   foreach(var at in new[]{new Vector3(-70.67f,1.28f,89.9f),new Vector3(-75.5f,1.2f,300.5f)})
    foreach(var r in county.GetComponentsInChildren<Renderer>(true).Where(r=>Vector3.Distance(r.bounds.center,at)<3.2f)){
     sb.AppendLine($"{PathOf(r.transform)} {r.GetType().Name} active {r.gameObject.activeInHierarchy} en {r.enabled} at {r.bounds.center:F2} size {r.bounds.size:F2}");
     foreach(var m in r.sharedMaterials){if(!m){sb.AppendLine("    null material");continue;}var sh=m.shader;sb.AppendLine($"    {m.name} shader {(sh?sh.name:"-")} supported {(sh&&sh.isSupported)} passes {(sh?sh.passCount:0)} tag {m.GetTag("RenderPipeline",false)} queue {m.renderQueue} kw {string.Join(",",m.shaderKeywords)}");}}
   foreach(var l in county.GetComponentsInChildren<Light>(true).Where(l=>Vector3.Distance(l.transform.position,new Vector3(-70.67f,1.28f,89.9f))<4f))sb.AppendLine($"LIGHT {PathOf(l.transform)} {l.type} col {l.color} int {l.intensity} range {l.range} flare {(l.flare?l.flare.name:"-")} halo? cookie {(l.cookie?l.cookie.name:"-")}");
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","magenta.txt"),sb.ToString());}
  public static void Placement27(){Open();Physics.SyncTransforms();var navData=UnityEngine.AI.NavMesh.AddNavMeshData(county.Navigation);var dir=Path.Combine(Work,"Audit","placement27");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   var props=new List<Prop>();var sb=new StringBuilder();var all=new Issues27();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){var lay=LayoutIds(p.Index);int n=0;
    foreach(var t in PropRoots(p)){if(!t.gameObject.activeInHierarchy)continue;if(!OOB(t,out var c,out var e,out var r,out var rs))continue;var id=IdOf(t);
     props.Add(new Prop{root=t,house=p,kind=KindOf(id,t.name,lay),on=lay.TryGetValue(id,out var li)?li.on:null,c=c,e=e,r=r,rs=rs,own=new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true))});n++;}
    sb.AppendLine($"p{p.Index} {p.Address}: {n} props, building {(p.Building?p.Building.name:"-")}");}
   // V27 redo: the houses as drawn (temporary mesh colliders), not their colliders - a wall's box can stand 6 cm proud of
   // its paper. Walls, ceilings and what hangs on them are measured against these; a box overlap with a wall only counts
   // if the prop's own vertices go through the drawn wall.
   var vis=new List<GameObject>();var visOwner=new Dictionary<Collider,Transform>();foreach(var hp in county.Properties)if(hp.Building)vis.AddRange(Visuals(hp,props.Where(x=>x.house==hp).Select(x=>x.root).ToList(),visOwner));
   float Through(Prop pr){float worst=0;foreach(var mf in pr.root.GetComponentsInChildren<MeshFilter>(false)){if(!mf.sharedMesh)continue;var vs=mf.sharedMesh.vertices;int step=Mathf.Max(1,vs.Length/2500);float bot=pr.rs.Min(x=>x.bounds.min.y);bool st=pr.kind=="floor"||pr.kind=="top";
     for(int i=0;i<vs.Length;i+=step){var v=mf.transform.TransformPoint(vs[i]);if(st&&v.y<bot+.025f)continue;var o=new Vector3(pr.c.x,v.y,pr.c.z);var dv=v-o;float len=dv.magnitude;if(len<.02f)continue;
      if(Physics.Raycast(o,dv/len,out var h,len,1<<VisHouse,QueryTriggerInteraction.Collide)){float dd=Vector3.Dot(v-h.point,-h.normal);if(dd>worst)worst=dd;}}}return worst;}
   var probe=new GameObject("audit box");var box=probe.AddComponent<BoxCollider>();
   void Add(Prop pr,string problem,float amount,string other=null){var i=new Issue27{path=PathOf(pr.root),house="p"+pr.house.Index,kind=pr.kind,problem=problem,amount=amount,at=pr.c,size=pr.e*2,other=other};all.items.Add(i);}
   bool IsMine(Prop pr,Collider c)=>pr.own.Contains(c)||c.transform.IsChildOf(pr.root);
   Prop OwnerOf(Collider c)=>props.FirstOrDefault(x=>c.transform==x.root||c.transform.IsChildOf(x.root));
   try{
   foreach(var pr in props){
    var up=pr.r*Vector3.up;
    // 1. tipped over (floor furniture and table things should stand up)
    if((pr.kind=="floor"||pr.kind=="top")&&Vector3.Dot(up,Vector3.up)<.7f&&!pr.root.name.ToLowerInvariant().Contains("book"))Add(pr,"tipped",Vector3.Angle(up,Vector3.up));
    // 2. what holds it: a 3x3 of rays down through its footprint
    float bottom=float.MaxValue;foreach(var x in pr.rs)bottom=Mathf.Min(bottom,x.bounds.min.y);
    if(pr.kind=="floor"||pr.kind=="top"||pr.kind=="flat"){var gaps=new List<float>();
     for(int a=-1;a<=1;a++)for(int b=-1;b<=1;b++){var o=pr.c+pr.r*new Vector3(pr.e.x*.7f*a,0,pr.e.z*.7f*b);o.y=bottom+.3f;
      var hits=Physics.RaycastAll(o,Vector3.down,2.5f,~0,QueryTriggerInteraction.Collide).Where(h=>(h.collider is TerrainCollider)||(visOwner.TryGetValue(h.collider,out var ow)&&ow!=pr.root)).OrderBy(h=>h.distance).ToList();
      if(hits.Count>0)gaps.Add(bottom-hits[0].point.y);}
     if(gaps.Count>=3){gaps.Sort();float med=gaps[gaps.Count/2],lo=gaps[0];
      if(lo>.025f)Add(pr,"floating",lo);
      else if(med<-.05f&&pr.kind!="flat")Add(pr,"sunk",-med);}}
    // 3. into walls, floors, doors and other props (penetration depth from the physics engine)
    probe.transform.SetPositionAndRotation(pr.c,pr.r);box.size=Vector3.Max(pr.e*2-Vector3.one*.04f,Vector3.one*.01f);Physics.SyncTransforms();
    foreach(var c in Physics.OverlapBox(pr.c,Vector3.Max(pr.e-Vector3.one*.02f,Vector3.one*.005f),pr.r,~0,QueryTriggerInteraction.Collide)){if(c.isTrigger&&c.gameObject.layer!=VisProp&&c.gameObject.layer!=VisHouse)continue;
     if(IsMine(pr,c)||c==box||c is TerrainCollider||c is CharacterController)continue;
     if(!Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,c,c.transform.position,c.transform.rotation,out var dirn,out var depth))continue;
     var other=OwnerOf(c);
     if(other!=null&&c.gameObject.layer!=VisProp)continue;
     if(other!=null){if(other==pr||IdOf(other.root)==pr.on||IdOf(pr.root)==other.on)continue;if(other.kind=="flat"||pr.kind=="flat")continue;if(depth>.05f&&string.CompareOrdinal(PathOf(pr.root),PathOf(other.root))<0)Add(pr,"into prop",depth,PathOf(other.root));}
     else if(pr.house.Building&&c.transform.IsChildOf(pr.house.Building)){if(c.gameObject.layer!=VisHouse)continue;var nm=c.transform.parent.name.ToLowerInvariant();bool floorish=nm.Contains("floor")||nm.Contains("stairs")||nm.Contains("ground")||nm.Contains("base");
      if(depth>(floorish?.06f:.035f)){if(floorish){Add(pr,"into floor/stairs",depth,c.transform.parent.name);}else if(pr.kind!="ceiling"){float th=Through(pr);if(th>.012f)Add(pr,"into wall",th,c.transform.parent.name);}}}
     else if(depth>.05f)Add(pr,"into other",depth,PathOf(c.transform));}
    // 4. wall things: the wall behind them, closest of the four sides
    if(pr.kind=="wall"){float best=9;foreach(var (dv,half) in new[]{(pr.r*Vector3.forward,pr.e.z),(pr.r*Vector3.back,pr.e.z),(pr.r*Vector3.right,pr.e.x),(pr.r*Vector3.left,pr.e.x)}){
      foreach(var h in Physics.RaycastAll(pr.c,dv,1.5f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){best=Mathf.Min(best,Mathf.Max(0,h.distance-half));break;}}
     if(best>.06f&&best<9)Add(pr,"off its wall",best);else if(best>=9)Add(pr,"no wall behind",0);}
    // 5. ceiling things: the ceiling over them
    if(pr.kind=="ceiling"){float top=float.MinValue;foreach(var x in pr.rs)top=Mathf.Max(top,x.bounds.max.y);var o=pr.c;o.y=top-.05f;
     var hits=Physics.RaycastAll(o,Vector3.up,4f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).ToList();
     if(hits.Count==0)Add(pr,"no ceiling over it",0);else{float gap=hits[0].point.y-top;if(gap>.12f)Add(pr,"hangs clear of the ceiling",gap);else if(gap<-.06f)Add(pr,"through the ceiling",-gap);}}
    // 6. in the front doorway (a leaf's sweep and a step either side)
    {var hp=pr.house;var oc=hp.OpeningCentre;var inw=hp.Inward;inw.y=0;inw.Normalize();var along=Vector3.Cross(Vector3.up,inw);float w=hp.DoorWidth;
     var d=pr.c-oc;float dIn=Vector3.Dot(d,inw),dAl=Vector3.Dot(d,along);var ex=pr.r*new Vector3(pr.e.x,0,0);var ez=pr.r*new Vector3(0,0,pr.e.z);
     float rIn=Mathf.Abs(Vector3.Dot(ex,inw))+Mathf.Abs(Vector3.Dot(ez,inw)),rAl=Mathf.Abs(Vector3.Dot(ex,along))+Mathf.Abs(Vector3.Dot(ez,along));
     if(pr.kind!="flat"&&pr.kind!="wall"&&pr.kind!="ceiling"&&dIn+rIn>-1.0f&&dIn-rIn<w+.15f&&Mathf.Abs(dAl)-rAl<w*.5f&&Mathf.Abs(pr.c.y-oc.y)<1.5f&&!pr.root.name.Contains("doormat"))Add(pr,"in the front doorway",w);}
   }
   // 7. doubles: the same mesh drawn twice in the same place
   var seen=new Dictionary<string,Prop>();foreach(var pr in props)foreach(var x in pr.rs){var mf=x.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;var key=AssetDatabase.GetAssetPath(mf.sharedMesh)+mf.sharedMesh.name+"@"+Vector3Int.RoundToInt(x.transform.position*50);
    if(seen.TryGetValue(key,out var first)&&first!=pr)Add(pr,"doubled with",0,PathOf(first.root));else seen[key]=pr;}
   }finally{Object.DestroyImmediate(probe);Unvisual(vis);}
   // pictures of each finding (neutral light, from a step back and above, the prop in the middle)
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=60;cam.aspect=4f/3f;int k=0;
   try{foreach(var i in all.items){var size=Mathf.Max(.6f,i.size.magnitude);var flat=new Vector3(i.at.x,0,i.at.z);
     // stand where it can be seen: try eight directions, take the first with a clear line
     Vector3 eye=i.at+new Vector3(1,1,1).normalized*size*2;bool found=false;
     // from the walkable side: a navmesh spot 1.4-3.2 m off on the same floor with a clear line to it
     foreach(float rad in new[]{2.2f,1.6f,3f})for(int a=0;a<12&&!found;a++){var dv=Quaternion.Euler(0,a*30,0)*Vector3.forward;if(!UnityEngine.AI.NavMesh.SamplePosition(i.at+dv*rad,out var nh,.8f,UnityEngine.AI.NavMesh.AllAreas)||Mathf.Abs(nh.position.y-(i.at.y-i.size.y*.5f))>1.2f)continue;
      var e2=nh.position+Vector3.up*1.55f;if(!Physics.Linecast(e2,i.at,out var hh2,~0,QueryTriggerInteraction.Ignore)||Vector3.Distance(hh2.point,i.at)<Mathf.Max(.3f,i.size.magnitude*.5f)){eye=e2;found=true;}}
     if(!found)for(int a=0;a<8;a++){var dv=Quaternion.Euler(0,a*45,0)*Vector3.forward;var e2=i.at+dv*Mathf.Clamp(size*1.6f,1.2f,4f)+Vector3.up*Mathf.Clamp(size*.6f,.35f,1.2f);
      if(!Physics.Linecast(i.at+Vector3.up*.05f,e2,out var hh,~0,QueryTriggerInteraction.Ignore)||hh.distance>Vector3.Distance(i.at,e2)-.1f){eye=e2;break;}}
     cam.transform.position=eye;cam.transform.LookAt(i.at);Shoot(cam,Path.Combine(dir,$"i{k:000}.jpg"),640,480);k++;}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
   k=0;foreach(var g in all.items.GroupBy(x=>x.problem))sb.AppendLine($"{g.Key}: {g.Count()}");sb.AppendLine();
   foreach(var i in all.items){sb.AppendLine($"i{k:000} {i.house} [{i.kind}] {i.problem} {i.amount:F3} {i.path.Replace("SERVICE — Hollis County/","")} at {i.at:F2} size {i.size:F2}{(i.other!=null?" vs "+i.other.Replace("SERVICE — Hollis County/",""):"")}");k++;}
   File.WriteAllText(Path.Combine(dir,"report.txt"),sb.ToString());File.WriteAllText(Path.Combine(dir,"issues.json"),JsonUtility.ToJson(all,true));navData.Remove();}
 }
}
