using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V28 ("some furnitures were not properly placed. random places that make no sense, some not even attached to the
 // wall"): Sense28 found case pieces standing 5-46 cm off their walls (they had been set 3 cm from the walls' collision
 // boxes, which stand proud of the drawn wall) and, at Vale's, pieces standing in openings. Arrange28:
 //  1. moves Vale's hall console (with its lamp, vase and letter) and the mirror over it off the opening to the north
 //     corridor onto the solid east wall of her hall, her hall clock along that wall, her study bookcase out of the opening
 //     to the corridor onto the study's east wall, and the bedroom chair from the middle of the room against the wall;
 //  2. puts every case piece in every house (cabinets, dressers, shelves, wardrobes, beds, stoves, consoles...) back
 //     against the drawn wall behind it, 1 cm off it and square to it, with everything standing on it;
 //  3. rebakes the navigation (the creatures and the tours walk where the furniture is not).
 // Audit/v28/sense/arrange28.txt lists every move.
 public static partial class ServiceV19Rebuild {
  static readonly string[] SeatWords={"chair","sofa","stool","rocker","settee","couch"};
  static Dictionary<string,string> Fronts(){var cat=new Dictionary<string,string>();
   try{foreach(var e in JsonUtility.FromJson<CatalogList>("{\"items\":"+File.ReadAllText(Path.Combine(Work,"Audit","prop-catalog-oriented.json"))+"}").items)if(e!=null&&!string.IsNullOrEmpty(e.name))cat[e.name]=e.front;}catch{}
   return cat;}
  static string PrefabOf(Transform t){var id=IdOf(t);return t.name.Length>id.Length?t.name.Substring(id.Length).Trim(' ','—'):t.name;}
  // the piece's frame (front = local +z) and the extents of what is drawn in it, relative to its root
  static bool PieceFrame(Transform t,Dictionary<string,string> cat,out Quaternion q,out Vector3 lo,out Vector3 hi,out bool known){
   var front=cat.TryGetValue(PrefabOf(t),out var fr)?fr:null;known=!string.IsNullOrEmpty(front)&&front!="none";
   q=Quaternion.Euler(0,t.eulerAngles.y+(known?FrontYaw(front):0),0);var iq=Quaternion.Inverse(q);lo=Vector3.one*1e9f;hi=-lo;bool any=false;
   foreach(var v in DrawnVerts(t,20000)){var l=iq*(v-t.position);l.y=v.y;lo=Vector3.Min(lo,l);hi=Vector3.Max(hi,l);any=true;}
   return any;}
  // what goes with a piece when it moves: what the layout stands on it, its shelf books, anything resting on its top,
  // and the papers' spot when it is the delivery table
  static List<Transform> Riders(ServiceProperty p,Transform t,Quaternion q,Vector3 lo,Vector3 hi,List<Transform> roots,Dictionary<string,LayoutItem> lay){
   var id=IdOf(t);var list=new List<Transform>();var iq=Quaternion.Inverse(q);
   bool OnTop(Vector3 w){var l=iq*(w-t.position);return l.x>lo.x-.03f&&l.x<hi.x+.03f&&l.z>lo.z-.03f&&l.z<hi.z+.03f&&w.y>hi.y-.15f&&w.y<hi.y+.8f;}
   var cands=roots.Concat(p.transform.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("V28 readable"))).Distinct().ToList();
   void Add(Transform r){if(r==t||list.Contains(r))return;list.Add(r);var rid=IdOf(r);foreach(var c in cands)if(lay.TryGetValue(IdOf(c),out var li)&&li.on==rid)Add(c);}
   foreach(var c in cands){if(c==t)continue;var cid=IdOf(c);if(lay.TryGetValue(cid,out var li)&&li.on==id){Add(c);continue;}
    if(c.name.Contains("— "+id+" —")){Add(c);continue;}
    var k=KindOf(cid,c.name,lay);if((k=="floor"||k=="flat"||k=="wall"||k=="ceiling")&&!c.name.StartsWith("V28 readable"))continue;
    if(OnTop(c.position))Add(c);}
   if(p.DeliveryPoint&&OnTop(p.DeliveryPoint.position)){list.Add(p.DeliveryPoint);if(p.TableApproach)list.Add(p.TableApproach);}
   return list;}
  static void MoveGroup(Transform t,List<Transform> riders,Vector3 pivot,Quaternion turn,Vector3 shift){
   foreach(var m in new[]{t}.Concat(riders)){Undo.RecordObject(m,"arrange");var rel=m.position-pivot;var np=pivot+turn*rel+shift;np.y=m.position.y+shift.y;m.position=np;m.rotation=turn*m.rotation;}}
  // what is behind a piece, ray by ray (SERVICE_LOOK="5:study-daybed,..."): rows are heights over its bottom, columns
  // across its back; each cell the gap in cm and a letter for what was hit (w wall/house, p prop, - nothing in 85 cm)
  public static void Gaps28(){Open();Physics.SyncTransforms();var cat=Fronts();var sb=new StringBuilder();
   var list=(System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1])).ToList();
   foreach(var grp in list.GroupBy(x=>x.Item1)){var p=county.Properties.First(x=>x.Index==grp.Key);var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var (_,id) in grp){var t=roots.FirstOrDefault(r=>IdOf(r)==id);if(!t||!PieceFrame(t,cat,out var q,out var lo,out var hi,out var known)){sb.AppendLine($"p{p.Index} {id}: not found");continue;}
      var riders=Riders(p,t,q,lo,hi,roots,lay);var tree=new HashSet<Transform>(riders){t};var wn=q*Vector3.back;sb.AppendLine($"p{p.Index} {id} (front known {known}), height {hi.y-lo.y:F2} m:");
      for(int b=9;b>=0;b--){float hh=.03f+b*.94f/9f;float y=lo.y+(hi.y-lo.y)*hh;sb.Append($"   {y-lo.y:F2} m |");
       for(int a=0;a<9;a++){float u=.05f+a*.9f/8f;var s=t.position+q*new Vector3(Mathf.Lerp(lo.x,hi.x,u),0,lo.z);s.y=y;s-=wn*.03f;string cell="   - ";
        foreach(var hit in Physics.RaycastAll(s,wn,.85f,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(hit.collider,out var ow)||tree.Contains(ow))continue;if(FlatWords.Any(ow.name.ToLowerInvariant().Contains))continue;
         cell=$"{(hit.distance-.03f)*100,4:F0}{(ow==p.Building?"w":"p")}";if(ow==p.Building&&b==0&&a==4)cell+="";break;}
        sb.Append(cell);}
       sb.AppendLine();}}}
    finally{Unvisual(vis);}}
   File.WriteAllText(Path.Combine(Work,"Audit","v28","sense","gaps28.txt"),sb.ToString());}
  // put listed wall pieces back out along their facing: SERVICE_LOOK="0:bed-painting:2.1,..." (cm)
  public static void Unsink28(){Open();Physics.SyncTransforms();var cat=Fronts();var sb=new StringBuilder();
   foreach(var a in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Count(c=>c==':')==2).Select(x=>x.Split(':'))){var p=county.Properties.First(x=>x.Index==int.Parse(a[0]));var t=PropRoots(p).FirstOrDefault(r=>IdOf(r)==a[1]);if(!t){sb.AppendLine(a[1]+": not found");continue;}
    var fr=cat.TryGetValue(PrefabOf(t),out var f)?f:"+Z";var fw=Quaternion.Euler(0,t.eulerAngles.y+(f!=null&&f!="none"?FrontYaw(f):0),0)*Vector3.forward;Undo.RecordObject(t,"unsink");t.position+=fw*(float.Parse(a[2])/100f);sb.AppendLine($"p{a[0]} {a[1]}: out {a[2]} cm along {fw:F2}");}
   log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","v28","sense","unsink28.txt"),sb.ToString());Save("Unsink28");}
  // how deep a piece is through the drawn house (Touch27's test: from its middle out to each vertex; a vertex beyond a
  // house surface by more than 6 mm counts)
  static float Through28(ServiceProperty p,Transform t,Dictionary<Collider,Transform> owner,HashSet<Transform> tree){
   if(!OOB(t,out var c,out _,out _,out var rs))return 0;float bottom=rs.Min(x=>x.bounds.min.y);float worst=0;
   foreach(var v in DrawnVerts(t,4000)){if(v.y<bottom+.025f)continue;var o=new Vector3(c.x,v.y,c.z);var dv=v-o;float len=dv.magnitude;if(len<.02f)continue;
    foreach(var h in Physics.RaycastAll(o,dv/len,len,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(h.collider,out var who)||tree.Contains(who))continue;if(who!=p.Building)break;
     float depth=Vector3.Dot(v-h.point,-h.normal);if(depth>worst)worst=depth;break;}}
   return worst;}
  // V28: pieces Arrange28 slid onto thin things between its rays (a corner pilaster, a post, a stair stringer) come back
  // out, forward, a little at a time, until nothing of them is in the house (at most 12 cm)
  public static void Settle28(){Open();Physics.SyncTransforms();var cat=Fronts();var sb=new StringBuilder();
   var list=(System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1])).ToList();
   foreach(var grp in list.GroupBy(x=>x.Item1)){var p=county.Properties.First(x=>x.Index==grp.Key);var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var (_,id) in grp){var t=roots.FirstOrDefault(r=>IdOf(r)==id);if(!t||!PieceFrame(t,cat,out var q,out var lo,out var hi,out var known)){sb.AppendLine($"p{p.Index} {id}: not found");continue;}
      var riders=Riders(p,t,q,lo,hi,roots,lay);var tree=new HashSet<Transform>(riders){t};var fw=q*Vector3.forward;float d0=Through28(p,t,owner,tree),d=d0,out_=0;
      while(d>.006f&&out_<.12f){float st=Mathf.Clamp(d+.004f,.01f,.04f);MoveGroup(t,riders,t.position,Quaternion.identity,fw*st);out_+=st;Physics.SyncTransforms();d=Through28(p,t,owner,tree);}
      sb.AppendLine($"p{p.Index} {id}: {d0*100:F1} cm in the house -> {d*100:F1} cm, came out {out_*100:F1} cm");}}
    finally{Unvisual(vis);}}
   log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","v28","sense","settle28.txt"),sb.ToString());Save("Settle28");}
  // shift listed pieces (and what stands on them) in the world: SERVICE_LOOK="2:kitchen-hutch:0.136:0.05,..." (m), then
  // say how deep each is in the house
  public static void Nudge28(){Open();Physics.SyncTransforms();var cat=Fronts();var sb=new StringBuilder();
   foreach(var grp in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Count(c=>c==':')==3).Select(x=>x.Split(':')).GroupBy(a=>int.Parse(a[0]))){
    var p=county.Properties.First(x=>x.Index==grp.Key);var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var a in grp){var t=roots.FirstOrDefault(r=>IdOf(r)==a[1]);if(!t||!PieceFrame(t,cat,out var q,out var lo,out var hi,out _)){sb.AppendLine(a[1]+": not found");continue;}
      var riders=Riders(p,t,q,lo,hi,roots,lay);var tree=new HashSet<Transform>(riders){t};MoveGroup(t,riders,t.position,Quaternion.identity,new Vector3(float.Parse(a[2]),0,float.Parse(a[3])));Physics.SyncTransforms();
      sb.AppendLine($"p{p.Index} {a[1]}: shifted {a[2]},{a[3]} m; now {Through28(p,t,owner,tree)*100:F1} cm in the house");}}
    finally{Unvisual(vis);}}
   log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","v28","sense","nudge28.txt"),sb.ToString());Save("Nudge28");}
  // side-on pictures along the wall at a piece's back: low (its feet) and at half its height (the gap to the wall)
  // SERVICE_LOOK="5:hall-cabinet,..." -> Audit/v28/sense/side-p<N>-<id>-<k>.jpg
  public static void Side28(){Open();Physics.SyncTransforms();var cat=Fronts();var dir=Path.Combine(Work,"Audit","v28","sense");
   var n0=NeutralLight();var muted=MuteFeatures();var cam=AuditCam();cam.aspect=4f/3f;cam.fieldOfView=55;cam.nearClipPlane=.02f;
   try{foreach(var a in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':'))){var p=county.Properties.First(x=>x.Index==int.Parse(a[0]));var t=PropRoots(p).FirstOrDefault(r=>IdOf(r)==a[1]);
     if(!t||!PieceFrame(t,cat,out var q,out var lo,out var hi,out _))continue;var fw=q*Vector3.forward;var rt=q*Vector3.right;var c=t.position+q*new Vector3((lo.x+hi.x)*.5f,0,(lo.z+hi.z)*.5f);float hw=(hi.x-lo.x)*.5f;var back=t.position+q*new Vector3((lo.x+hi.x)*.5f,0,lo.z);
     int k=0;foreach(float sd in new[]{-1f,1f})foreach(float hh in new[]{.1f,.55f}){float y=lo.y+Mathf.Max(.12f,(hi.y-lo.y)*hh);var from=c+rt*sd*(hw+.75f)+fw*.25f;from.y=y+.05f;var at=back+rt*sd*hw;at.y=y;
      cam.transform.position=from;cam.transform.LookAt(at);Shoot(cam,Path.Combine(dir,$"side-p{p.Index}-{a[1]}-{k++}.jpg"),640,480);}}}
   finally{EndNeutral(n0);Restore(muted);Object.DestroyImmediate(cam.gameObject);}}
  public static void Arrange28(){Open();Physics.SyncTransforms();var cat=Fronts();var sb=new StringBuilder();int moved=0;
   // 1. Vale's: off the openings, onto solid wall (the east wall of the hall and of the study above it is drawn at x 98.59)
   //    Harrow's: his north hutch stood 21 cm off the wall in front of a post (the wall has four windows and the post; the
   //    only stretch long enough for it is the east wall by the dining table), and his clock goes between the post and
   //    the next window
   var vale=county.Properties.First(x=>x.Index==1);
   {var roots=PropRoots(vale).ToList();var lay=LayoutIds(1);
    foreach(var (pi,id,front,x,z) in new[]{(1,"vest-console",270f,98.22f,232.85f),(1,"vest-clock",270f,98.30f,229.0f),(1,"study-bookcase",270f,98.22f,232.4f),(1,"bed-chair",270f,98.26f,225.62f),(3,"north-hutch",270f,90.62f,157.61f),(3,"ne-clock",180f,85.8f,158.3f)}){
     var pp=county.Properties.First(xx=>xx.Index==pi);if(pi!=1){roots=PropRoots(pp).ToList();lay=LayoutIds(pi);}else{roots=PropRoots(vale).ToList();lay=LayoutIds(1);}
     var t=roots.FirstOrDefault(r=>IdOf(r)==id);if(!t||!PieceFrame(t,cat,out var q,out var lo,out var hi,out _)){sb.AppendLine($"p{pi} {id}: not found");continue;}
     var riders=Riders(pp,t,q,lo,hi,roots,lay);var c=t.position+q*new Vector3((lo.x+hi.x)*.5f,0,(lo.z+hi.z)*.5f);c.y=0;
     var turn=Quaternion.Euler(0,Mathf.DeltaAngle(q.eulerAngles.y,front),0);var from=c;MoveGroup(t,riders,c,turn,new Vector3(x-c.x,0,z-c.z));moved++;
     sb.AppendLine($"p{pi} {id}: from {from.x:F2},{from.z:F2} to {x:F2},{z:F2}, facing {front:F0} (turned {Mathf.DeltaAngle(q.eulerAngles.y,front):F0} deg), with {string.Join(", ",riders.Select(r=>IdOf(r)))}");}
    // her hall mirror goes over the console, on the same wall
    roots=PropRoots(vale).ToList();var mir=roots.FirstOrDefault(r=>IdOf(r)=="vest-mirror");
    if(mir){var front=cat.TryGetValue(PrefabOf(mir),out var fr)?fr:"+Z";float cur=mir.eulerAngles.y+(fr!=null&&fr!="none"?FrontYaw(fr):0);Undo.RecordObject(mir,"arrange");mir.rotation=Quaternion.Euler(0,Mathf.DeltaAngle(cur,270f),0)*mir.rotation;Physics.SyncTransforms();
     var b=mir.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,bb)=>{a.Encapsulate(bb);return a;});var want=new Vector3(98.59f-b.extents.x-.01f,0.56f+1.72f,232.85f);mir.position+=want-b.center;moved++;
     sb.AppendLine($"p1 vest-mirror: over the console, on the east wall at {want.x:F2},{want.y:F2},{want.z:F2}");}}
   Physics.SyncTransforms();
   // 2. every case piece back to its wall
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var t in roots){var id=IdOf(t);if(KindOf(id,t.name,lay)!="floor")continue;var nm=t.name.ToLowerInvariant();if(SenseSkip.Any(nm.Contains))continue;var pl=PrefabOf(t).ToLowerInvariant();
      bool forced=p.Index==1&&id=="bed-chair";if(SeatWords.Any(pl.Contains)&&!forced)continue;
      bool caseGood=(lay.TryGetValue(id,out var li)&&li.againstWall)||CaseWords.Any(pl.Contains)||forced;bool loose=pl.Contains("trash")||pl.Contains("crate")||pl.Contains("box");
      if(!caseGood&&!loose)continue;
      for(int pass=0;pass<2;pass++){
       if(!PieceFrame(t,cat,out var q,out var lo,out var hi,out var known))break;if(!known&&!loose)break;
       var riders=Riders(p,t,q,lo,hi,roots,lay);var tree=new HashSet<Transform>(riders){t};
       // the side that goes to the wall: the back, or for a loose box the side nearest a wall
       var sides=known?new[]{Vector3.back}:new[]{Vector3.back,Vector3.forward,Vector3.left,Vector3.right};
       (Vector3 n,float min,int house,int rays,float ang) best=(Vector3.back,9f,0,0,0f);
       foreach(var ln in sides){var wn=q*ln;bool spansX=Mathf.Abs(ln.z)>.5f;float mn=9f;int hs=0,rs=0;var angs=new List<float>();
        for(int a=0;a<7;a++)for(int b=0;b<6;b++){float u=.08f+a*.84f/6f,hh=.06f+b*.88f/5f;rs++;
         float lx=spansX?Mathf.Lerp(lo.x,hi.x,u):(ln.x>0?hi.x:lo.x),lz=spansX?(ln.z>0?hi.z:lo.z):Mathf.Lerp(lo.z,hi.z,u);float y=Mathf.Max(lo.y+.03f,lo.y+(hi.y-lo.y)*hh);
         var s=t.position+q*new Vector3(lx,0,lz);s.y=y;s-=wn*.03f;
         foreach(var hit in Physics.RaycastAll(s,wn,.85f,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(hit.collider,out var ow)||tree.Contains(ow))continue;if(FlatWords.Any(ow.name.ToLowerInvariant().Contains))continue;
          float dd=hit.distance-.03f;mn=Mathf.Min(mn,dd);if(ow==p.Building){hs++;angs.Add(Vector3.SignedAngle(new Vector3(wn.x,0,wn.z),-new Vector3(hit.normal.x,0,hit.normal.z),Vector3.up));}break;}}
        float ang=angs.Count>0?angs.OrderBy(x=>x).ElementAt(angs.Count/2):0;
        if(hs>=rs/2&&mn<best.min)best=(wn,mn,hs,rs,ang);}
       if(best.house==0){if(pass==0)sb.AppendLine($"p{p.Index} {id}: no wall behind it within 85 cm - left");break;}
       var c=t.position+q*new Vector3((lo.x+hi.x)*.5f,0,(lo.z+hi.z)*.5f);c.y=0;
       if(pass==0&&Mathf.Abs(best.ang)>2f&&Mathf.Abs(best.ang)<25f){MoveGroup(t,riders,c,Quaternion.Euler(0,best.ang,0),Vector3.zero);Physics.SyncTransforms();sb.AppendLine($"p{p.Index} {id}: squared to its wall ({best.ang:F1} deg)");continue;}
       if(best.min>.012f&&best.min<.6f){var shift=best.n*(best.min-.01f);shift.y=0;MoveGroup(t,riders,c,Quaternion.identity,shift);Physics.SyncTransforms();moved++;
        sb.AppendLine($"p{p.Index} {id}: back to the wall, {best.min*100:F1} cm -> 1 cm ({best.house}/{best.rays} rays on the wall){(riders.Count>0?" with "+string.Join(", ",riders.Select(r=>IdOf(r))):"")}");}
       else if(best.min>=.6f)sb.AppendLine($"p{p.Index} {id}: {best.min*100:F0} cm from the wall - left");
       break;}}}
    finally{Unvisual(vis);}}
   ServiceBuild.RebakeOpenDoors(county);
   sb.Insert(0,$"Arrange28: {moved} moves"+System.Environment.NewLine);Directory.CreateDirectory(Path.Combine(Work,"Audit","v28","sense"));File.WriteAllText(Path.Combine(Work,"Audit","v28","sense","arrange28.txt"),sb.ToString());log.Append(sb.ToString());Save("Arrange28");}
 }
}
