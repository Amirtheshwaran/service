using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2.Editor {
 // V23 census (batch mode with graphics; nothing is saved). Measures what the playtest named, so every fix has a list:
 //  - foliage: every grass/bush/fern object, its base against the ground under it (floating), whether it stands on a
 //    road surface, and a 10 m grass-density grid of the county (Audit/census23/foliage.csv, grass-grid.csv);
 //  - furniture: every object in and around each house, its support (collider below, or the top of another object),
 //    wall/ceiling contact, sinking, and flat coplanar pieces that z-fight (carpets) (units.csv);
 //  - houses: each building's lowest edge against the terrain round its footprint (buildings.txt);
 //  - roads: every road/drive ribbon sampled on a 0.5 m grid for terrain showing through or the surface standing
 //    proud of the ground, plus each drive's two ends (roads.txt);
 //  - renders in flat neutral light: top views of each property, eye-level views up and down every drive, the house
 //    from four sides, interior views from points on the navmesh, and a close-up of every flagged object.
 public static partial class ServiceV19Rebuild {
  static bool FoliageName(string n)=>n.StartsWith("Grass_")||n.StartsWith("TreeCreator_Bush")||n.StartsWith("fern")||n.StartsWith("DecoBush")||n.Contains("thicket")||n.StartsWith("Bush")||n.StartsWith("Shrub");
  static Transform FoliageRoot(Transform t){var u=t;while(u&&!FoliageName(u.name))u=u.parent;return u;}
  static string Kind(Collider c,Transform roads,Transform late){if(!c)return "none";if(c is TerrainCollider)return "terrain";if(roads&&c.transform.IsChildOf(roads)||late&&c.transform.IsChildOf(late))return "road";return "other:"+c.name;}
  static bool GroundHit(Vector3 at,float from,Transform self,out RaycastHit best){best=default;bool any=false;
   foreach(var h in Physics.RaycastAll(new Vector3(at.x,from,at.z),Vector3.down,from-at.y+6,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)){
    if(self&&h.collider.transform.IsChildOf(self))continue;if(h.collider is CharacterController)continue;if(county.Car&&h.collider.transform.IsChildOf(county.Car))continue;
    if(!any||h.point.y>best.point.y){best=h;any=true;}}
   return any;}
  static Bounds RBounds(Transform u){Bounds b=default;bool any=false;foreach(var r in u.GetComponentsInChildren<Renderer>(false)){if(!r.enabled||r is ParticleSystemRenderer)continue;if(!any){b=r.bounds;any=true;}else b.Encapsulate(r.bounds);}return any?b:new Bounds(u.position,Vector3.zero);}

  static bool dataOnly;
  public static void Census23Data(){dataOnly=true;Census23();}
  public static void Census23(){
   Open();var dir=Path.Combine(Work,"Audit","census23");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   Physics.SyncTransforms();
   var t=county.GetComponentInChildren<Terrain>();var roads=county.transform.Find("V19 county roads");var late=county.LateRoad?county.LateRoad.transform:null;
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   var summary=new StringBuilder();

   // ---------- foliage ----------
   var fol=new StringBuilder("name,x,y,z,height,gapCentre,gapMax,ground,onRoad,path\n");var seen=new HashSet<Transform>();int fn=0,ffloat=0,froad=0;
   var td=t.terrainData;var tpos=t.transform.position;int gw=Mathf.CeilToInt(td.size.x/10),gh=Mathf.CeilToInt(td.size.z/10);var grid=new int[gw,gh];var bgrid=new int[gw,gh];
   foreach(var r in county.GetComponentsInChildren<Renderer>(false)){
    if(!r.enabled)continue;var u=FoliageRoot(r.transform);if(!u||!seen.Add(u))continue;fn++;
    var b=RBounds(u);var p=u.position;float top=b.max.y+3;
    float gc=float.NaN,gm=float.NegativeInfinity;string kind="none";bool onRoad=false;
    if(GroundHit(new Vector3(b.center.x,b.min.y,b.center.z),top,u,out var hc)){gc=b.min.y-hc.point.y;kind=Kind(hc.collider,roads,late);if(kind=="road")onRoad=true;}
    float rad=Mathf.Min(Mathf.Max(b.extents.x,b.extents.z)*.6f,.7f);
    foreach(var o in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back}){var q=new Vector3(b.center.x,b.min.y,b.center.z)+o*rad;if(GroundHit(q,top,u,out var h)){gm=Mathf.Max(gm,b.min.y-h.point.y);if(Kind(h.collider,roads,late)=="road")onRoad=true;}}
    bool grass=u.name.StartsWith("Grass_")||u.name.Contains("grass")||u.name.Contains("Grass");
    int gx=Mathf.FloorToInt((p.x-tpos.x)/10),gz=Mathf.FloorToInt((p.z-tpos.z)/10);if(gx>=0&&gz>=0&&gx<gw&&gz<gh){if(grass)grid[gx,gz]++;else bgrid[gx,gz]++;}
    if(gc>.12f||gm>.25f)ffloat++;if(onRoad)froad++;
    fol.AppendLine($"{u.name.Replace(","," ")},{p.x:F2},{p.y:F2},{p.z:F2},{b.size.y:F2},{gc:F3},{gm:F3},{kind.Replace(","," ")},{(onRoad?1:0)},{PathOf(u).Replace(","," ")}");
   }
   File.WriteAllText(Path.Combine(dir,"foliage.csv"),fol.ToString());
   var gs=new StringBuilder($"# 10 m cells from terrain origin {tpos}, size {td.size}; value = grass objects / bush objects\n");
   for(int z=gh-1;z>=0;z--){for(int x=0;x<gw;x++)gs.Append(x>0?",":"").Append($"{grid[x,z]}/{bgrid[x,z]}");gs.AppendLine();}
   File.WriteAllText(Path.Combine(dir,"grass-grid.csv"),gs.ToString());
   summary.AppendLine($"FOLIAGE {fn} objects, {ffloat} floating (centre gap > 0.12 m or edge gap > 0.25 m), {froad} on a road surface");

   // ---------- furniture / props in and around each house ----------
   var units=new StringBuilder("property,zone,flag,name,cx,cy,cz,sx,sy,sz,bottom,supportGap,supportBy,sunk,wall,ceiling,coplanar,path\n");
   var flagged=new List<(int p,Transform u,Bounds b,string flag)>();
   foreach(var p in county.Properties){
    var ib=p.InteriorBounds;var yard=new Bounds(ib.center,ib.size+new Vector3(40,10,40));
    // units: items of the V19 interior containers, else the nearest prefab instance root, else the renderer's object
    var us=new HashSet<Transform>();
    foreach(var r in county.GetComponentsInChildren<Renderer>(false)){
     if(!r.enabled||r is ParticleSystemRenderer)continue;var tr=r.transform;if(!yard.Contains(r.bounds.center))continue;
     if(roads&&tr.IsChildOf(roads)||tr.name.StartsWith("V19 County Route 9")||tr.name.StartsWith("V19 drive"))continue;if(tr.IsChildOf(county.Car))continue;if(county.Walker&&tr.IsChildOf(county.Walker.transform))continue;if(county.View&&tr.IsChildOf(county.View.transform))continue;
     if(FoliageRoot(tr))continue;if(county.Entity&&tr.IsChildOf(county.Entity.transform))continue;
     Transform u=null;for(var a=tr;a;a=a.parent){if(a.parent&&a.parent.name.StartsWith("V19 interior")){u=a;break;}}
     if(!u){var pr=PrefabUtility.GetNearestPrefabInstanceRoot(tr.gameObject);u=pr?pr.transform:tr;}
     var bb=RBounds(u);if(Mathf.Max(bb.size.x,bb.size.y,bb.size.z)>6f){u=tr;bb=r.bounds;}
     if(Mathf.Max(bb.size.x,bb.size.y,bb.size.z)>6f)continue; // walls, floors, roofs, terrain-scale things
     us.Add(u);}
    var list=us.Select(u=>(u,b:RBounds(u))).ToList();
    foreach(var (u,b) in list){
     string zone=ib.Contains(b.center)?"inside":"yard";
     // support: the highest collider under five points of the base, or the top of another object's bounds
     float best=float.NegativeInfinity;string by="";
     foreach(var o in new[]{Vector2.zero,new Vector2(.6f,.6f),new Vector2(-.6f,.6f),new Vector2(.6f,-.6f),new Vector2(-.6f,-.6f)}){
      var q=new Vector3(b.center.x+o.x*b.extents.x,b.min.y,b.center.z+o.y*b.extents.z);
      if(GroundHit(q,b.min.y+.03f,u,out var h)&&h.point.y>best){best=h.point.y;by=h.collider is TerrainCollider?"terrain":h.collider.name;}}
     float gap=float.IsNegativeInfinity(best)?99:b.min.y-best;
     foreach(var (o,ob) in list){if(o==u)continue;if(Mathf.Abs(ob.max.y-b.min.y)<.05f){float ox=Mathf.Min(b.max.x,ob.max.x)-Mathf.Max(b.min.x,ob.min.x),oz=Mathf.Min(b.max.z,ob.max.z)-Mathf.Max(b.min.z,ob.min.z);
       if(ox>0&&oz>0&&ox*oz>.3f*Mathf.Max(b.size.x*b.size.z,1e-4f)&&Mathf.Abs(ob.max.y-b.min.y)<Mathf.Abs(gap)){gap=b.min.y-ob.max.y;by="top of "+o.name;}}}
     // sinking into the surface under it (the floor itself below the base by more than 5 cm)
     float sunk=0;if(GroundHit(new Vector3(b.center.x,b.max.y,b.center.z),b.max.y+.02f,u,out var hs)&&hs.point.y>b.min.y+.05f)sunk=hs.point.y-b.min.y;
     // wall / ceiling contact (wall-hung and hanging things are not floating)
     bool wall=false;foreach(var d in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back}){float reach=Mathf.Abs(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(d.x),0,Mathf.Abs(d.z))))+.12f;
      foreach(var h in Physics.RaycastAll(b.center,d,reach,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))if(!h.collider.transform.IsChildOf(u)&&!(h.collider is CharacterController)){wall=true;break;}if(wall)break;}
     float wallGap=-1;if(Mathf.Min(b.size.x,b.size.z)<.14f&&b.size.y>.12f){bool alongX=b.size.x<b.size.z;var ax=alongX?Vector3.right:Vector3.forward;float half=(alongX?b.extents.x:b.extents.z);float bestd=9;
      foreach(var dd in new[]{ax,-ax})foreach(var h in Physics.RaycastAll(b.center,dd,1.5f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))if(!h.collider.transform.IsChildOf(u)&&!(h.collider is CharacterController)&&h.distance-half<bestd)bestd=h.distance-half;wallGap=bestd;}
     bool ceil=false;foreach(var h in Physics.RaycastAll(b.center,Vector3.up,b.extents.y+.15f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))if(!h.collider.transform.IsChildOf(u)){ceil=true;break;}
     // flat pieces lying within 4 mm of the surface under them flicker (z-fighting)
     string cop="";if(b.size.y<.03f&&gap>-.01f&&gap<.004f)cop="FLAT-ON-SURFACE";
     string flag=wallGap>.03f&&gap>.05f?"OFF-WALL":gap>.05f&&!wall&&!ceil?"FLOATING":gap>.05f&&(wall||ceil)?"hung":sunk>.05f?"SUNK":cop!=""?"ZFIGHT":"ok";
     if(gap>.05f&&gap<99&&(wall||ceil)&&b.size.y>.6f&&b.min.y<p.Door.position.y+.6f+(b.min.y>p.Door.position.y+2.4f?3.2f:0))flag="FLOATING?"; // tall things off the floor that only touch a wall
     units.AppendLine($"{p.Index},{zone},{flag},{u.name.Replace(","," ")},{b.center.x:F2},{b.center.y:F2},{b.center.z:F2},{b.size.x:F2},{b.size.y:F2},{b.size.z:F2},{b.min.y:F3},{gap:F3},{by.Replace(","," ")},{sunk:F3},{(wall?1:0)},{(ceil?1:0)},{cop} wallGap {wallGap:F3},{PathOf(u).Replace(","," ")}");
     if(flag!="ok"&&flag!="hung")flagged.Add((p.Index,u,b,flag));
    }
    summary.AppendLine($"PROPERTY {p.Index} {p.Address}: {list.Count} units, flagged {flagged.Count(f=>f.p==p.Index)} ({string.Join(", ",flagged.Where(f=>f.p==p.Index).GroupBy(f=>f.flag).Select(g=>g.Key+" "+g.Count()))})");
   }
   File.WriteAllText(Path.Combine(dir,"units.csv"),units.ToString());

   // ---------- buildings against the ground ----------
   var bt=new StringBuilder();
   foreach(var p in county.Properties){var host=p.Building?p.Building:p.transform;var b=RBounds(host);
    var gaps=new List<float>();for(float s=0;s<1;s+=.02f){Vector3 q;float per=s*4;int side=(int)per;float f=per-side;
      q=side==0?new Vector3(Mathf.Lerp(b.min.x,b.max.x,f),0,b.min.z):side==1?new Vector3(b.max.x,0,Mathf.Lerp(b.min.z,b.max.z,f)):side==2?new Vector3(Mathf.Lerp(b.max.x,b.min.x,f),0,b.max.z):new Vector3(b.min.x,0,Mathf.Lerp(b.max.z,b.min.z,f));
      gaps.Add(b.min.y-TerrainY(t,q));}
    bt.AppendLine($"{p.Index} {p.Address}: building {PathOf(host)} bounds min.y {b.min.y:F2} door {p.Door.position.y:F2}; ground under the footprint edge {gaps.Select(g=>b.min.y-g).Min():F2}..{gaps.Select(g=>b.min.y-g).Max():F2}; edge gap (base above ground) min {gaps.Min():F2} max {gaps.Max():F2}, {gaps.Count(g=>g>.1f)}/{gaps.Count} points float > 0.1 m");
    // lowest renderer pieces of the shell, to see what forms the base
    foreach(var r in host.GetComponentsInChildren<Renderer>(false).OrderBy(r=>r.bounds.min.y).Take(4))bt.AppendLine($"    low piece {r.name} min.y {r.bounds.min.y:F2} size {r.bounds.size}");}
   File.WriteAllText(Path.Combine(dir,"buildings.txt"),bt.ToString());summary.Append(bt);

   // ---------- road ribbons: terrain through the surface, surface proud of the ground ----------
   var rt=new StringBuilder();
   foreach(var mc in new[]{roads,late}.Where(x=>x).SelectMany(x=>x.GetComponentsInChildren<MeshCollider>(true)).Distinct()){
    var b=mc.bounds;int samples=0,poke=0,proud=0;var pokes=new List<Vector3>();var prouds=new List<Vector3>();
    for(float x=b.min.x;x<=b.max.x;x+=.5f)for(float z=b.min.z;z<=b.max.z;z+=.5f){
     var ray=new Ray(new Vector3(x,b.max.y+2,z),Vector3.down);if(!mc.Raycast(ray,out var h,b.size.y+4))continue;samples++;float ty=TerrainY(t,h.point);
     if(ty>h.point.y-.002f){poke++;if(pokes.Count<3000)pokes.Add(h.point);}else if(h.point.y-ty>.12f){proud++;if(prouds.Count<3000)prouds.Add(h.point);}}
    rt.AppendLine($"{PathOf(mc.transform)}: {samples} samples, terrain through the surface at {poke} ({(samples>0?100f*poke/samples:0):F1}%), surface > 12 cm above the ground at {proud}");
    foreach(var c in Cluster(pokes,2f).Take(25))rt.AppendLine($"    terrain shows through near {c.at} ({c.n} samples)");
    foreach(var c in Cluster(prouds,2f).Take(10))rt.AppendLine($"    surface proud near {c.at} ({c.n} samples)");}
   foreach(var p in county.Properties){var r=p.ApproachRoute;if(r==null||r.Length<2)continue;var steps=p.Door.position;
    rt.AppendLine($"drive {p.Index}: route start {r[0]} end {r[r.Length-1]}, end to door {Vector3.Distance(new Vector3(r[r.Length-1].x,0,r[r.Length-1].z),new Vector3(steps.x,0,steps.z)):F1} m");}
   File.WriteAllText(Path.Combine(dir,"roads.txt"),rt.ToString());
   summary.AppendLine(string.Join("\n",rt.ToString().Split('\n').Where(l=>!l.StartsWith("    "))));
   File.WriteAllText(Path.Combine(dir,"summary.txt"),summary.ToString());

   if(dataOnly){if(county.LateRoad)county.LateRoad.SetActive(lateWas);return;}
   // ---------- renders ----------
   var n0=NeutralLight();var cam=AuditCam();var lamp=new GameObject("audit lamp").AddComponent<Light>();lamp.type=LightType.Point;lamp.range=14;lamp.intensity=3.5f;lamp.shadows=LightShadows.None;lamp.transform.SetParent(cam.transform,false);lamp.enabled=false;
   try{
    foreach(var p in county.Properties){var r=p.ApproachRoute;var hb=RBounds(p.Building?p.Building:p.transform);
     // top views: the whole drive with the house, and the house with its yard
     var all=new Bounds(hb.center,hb.size);if(r!=null)foreach(var v in r)all.Encapsulate(v);
     cam.orthographic=true;cam.nearClipPlane=1;cam.farClipPlane=300;cam.orthographicSize=Mathf.Max(all.extents.x,all.extents.z)+6;cam.transform.position=all.center+Vector3.up*120;cam.transform.rotation=Quaternion.Euler(90,0,0);Shoot(cam,Path.Combine(dir,$"p{p.Index}-top-drive.jpg"),1400,1400);
     cam.orthographicSize=Mathf.Max(hb.extents.x,hb.extents.z)+5;cam.transform.position=hb.center+Vector3.up*120;Shoot(cam,Path.Combine(dir,$"p{p.Index}-top-house.jpg"),1200,1200);
     cam.orthographic=false;cam.nearClipPlane=.05f;cam.farClipPlane=400;cam.fieldOfView=62;
     // the house from four sides at eye level
     for(int k=0;k<4;k++){var d=Quaternion.Euler(0,k*90+30,0)*Vector3.forward;float reach=Mathf.Max(hb.extents.x,hb.extents.z)+9;var eye=hb.center+d*reach;eye.y=TerrainY(t,eye)+1.6f;cam.transform.position=eye;var look=hb.center;look.y=eye.y-1;cam.transform.LookAt(look);Shoot(cam,Path.Combine(dir,$"p{p.Index}-side-{k}.jpg"),960,540);}
     // up the drive and back, every 6 m, from the player's eye
     if(r!=null&&r.Length>1){float acc=0;int k=0;for(int i=1;i<r.Length;i++){acc+=Vector3.Distance(r[i-1],r[i]);if(acc<6&&i<r.Length-1)continue;acc=0;var f=r[Mathf.Min(i+1,r.Length-1)]-r[Mathf.Max(i-1,0)];f.y=0;if(f.sqrMagnitude<.01f)continue;f.Normalize();
       var eye=r[i]+Vector3.up*1.6f;eye.y=TerrainY(t,eye)+1.6f;cam.transform.position=eye;cam.transform.rotation=Quaternion.LookRotation(f+Vector3.down*.18f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-drive-{k:00}-up.jpg"),960,540);
       cam.transform.rotation=Quaternion.LookRotation(-f+Vector3.down*.18f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-drive-{k:00}-down.jpg"),960,540);k++;}}
     // interiors from points on the navmesh, four ways each, lit from the camera
     lamp.enabled=true;cam.fieldOfView=78;var ib=p.InteriorBounds;var pts=new List<Vector3>();
     for(float y=ib.min.y;y<ib.max.y;y+=1.4f)for(float x=ib.min.x+1;x<ib.max.x;x+=3f)for(float z=ib.min.z+1;z<ib.max.z;z+=3f){
      if(NavMesh.SamplePosition(new Vector3(x,y,z),out var hit,1f,NavMesh.AllAreas)&&ib.Contains(hit.position+Vector3.up*.3f)&&!pts.Any(q=>Vector3.Distance(q,hit.position)<2.2f))pts.Add(hit.position);}
     int pi=0;foreach(var q in pts.Take(30)){for(int k=0;k<4;k++){cam.transform.position=q+Vector3.up*1.6f;cam.transform.rotation=Quaternion.Euler(12,k*90,0);Shoot(cam,Path.Combine(dir,$"p{p.Index}-in-{pi:00}-{k}.jpg"),640,360);}pi++;}
     lamp.enabled=false;
    }
    // a close-up of every flagged object (lit from the camera when it stands inside)
    int fi=0;foreach(var (pidx,u,b,flag) in flagged.Take(260)){var p=county.Properties.First(x=>x.Index==pidx);bool inside=p.InteriorBounds.Contains(b.center);lamp.enabled=inside;
     float reach=Mathf.Clamp(b.size.magnitude*1.6f,1.6f,7);var away=b.center-p.InteriorBounds.center;away.y=0;if(away.sqrMagnitude<.01f)away=Vector3.forward;
     var eye=b.center-(-away.normalized)*0+Vector3.zero;eye=b.center+Quaternion.Euler(0,35,0)*away.normalized*(-reach)+Vector3.up*Mathf.Max(.5f,b.extents.y);
     // keep the eye inside the room: pull it toward the object until nothing stands between
     var dirTo=(b.center-eye);if(Physics.Raycast(b.center,-dirTo.normalized,out var block,dirTo.magnitude,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)&&!block.collider.transform.IsChildOf(u))eye=block.point+dirTo.normalized*.15f;
     cam.fieldOfView=70;cam.transform.position=eye;cam.transform.LookAt(b.center);Shoot(cam,Path.Combine(dir,$"flag-{fi:000}-p{pidx}-{flag.Replace('?','Q')}.jpg"),640,400);
     File.AppendAllText(Path.Combine(dir,"flagged.txt"),$"{fi:000} p{pidx} {flag} {u.name} at {b.center} size {b.size}  {PathOf(u)}\n");fi++;}
   }finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
  }
  static List<(Vector3 at,int n)> Cluster(List<Vector3> pts,float radius){var res=new List<(Vector3 at,int n)>();var used=new bool[pts.Count];
   for(int i=0;i<pts.Count;i++){if(used[i])continue;var sum=pts[i];int n=1;used[i]=true;for(int j=i+1;j<pts.Count;j++){if(used[j])continue;if(Vector3.Distance(pts[i],pts[j])<radius){used[j]=true;sum+=pts[j];n++;}}res.Add((sum/n,n));}
   return res.OrderByDescending(c=>c.n).ToList();}
 }
}
