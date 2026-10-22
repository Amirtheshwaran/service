using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V28b (audit: "Morrow's and Vale's rooms off the route are empty", "a tall cabinet / wardrobe / four-poster across a window",
 // "suitcases parked in front of a door"): per-house plans Audit/layouts/p<N>-v28b.json - pieces to remove, to move and to
 // add (the V19 layout item format: id, prefab from the catalogue, x, z, floor, yaw = the way its front faces, againstWall,
 // wall + height, on, ceiling, light). Added pieces stand on what is drawn under them; case pieces go back to the drawn wall
 // (1 cm, or the skirting) with what stands on them; anything that then cuts into the house comes back out. The layout
 // loader (LayoutIds) reads these files too, so every audit knows the new pieces. Audit/v28/sense/furnish28.txt lists it all.
 public static partial class ServiceV19Rebuild {
  [System.Serializable] public class Furnish28Plan{public int property;public LayoutItem[] items;public LayoutItem[] moves;public string[] removes;}
  static string FlushOne(ServiceProperty p,Transform t,List<Transform> roots,Dictionary<string,LayoutItem> lay,Dictionary<Collider,Transform> owner,Dictionary<string,string> cat){
   for(int pass=0;pass<2;pass++){
    if(!PieceFrame(t,cat,out var q,out var lo,out var hi,out var known)||!known)return "front unknown - not slid";
    var riders=Riders(p,t,q,lo,hi,roots,lay);var tree=new HashSet<Transform>(riders){t};var wn=q*Vector3.back;
    float mn=9f;int hs=0,rs=0;var angs=new List<float>();
    for(int a=0;a<7;a++)for(int b=0;b<6;b++){float u=.08f+a*.84f/6f,hh=.06f+b*.88f/5f;rs++;float lx=Mathf.Lerp(lo.x,hi.x,u);float y=Mathf.Max(lo.y+.03f,lo.y+(hi.y-lo.y)*hh);
     var s=t.position+q*new Vector3(lx,0,lo.z);s.y=y;s-=wn*.03f;
     foreach(var hit in Physics.RaycastAll(s,wn,.85f,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(hit.collider,out var ow)||tree.Contains(ow))continue;if(FlatWords.Any(ow.name.ToLowerInvariant().Contains))continue;
      float dd=hit.distance-.03f;mn=Mathf.Min(mn,dd);if(ow==p.Building){hs++;angs.Add(Vector3.SignedAngle(new Vector3(wn.x,0,wn.z),-new Vector3(hit.normal.x,0,hit.normal.z),Vector3.up));}break;}}
    if(hs<rs/2)return $"NO WALL BEHIND IT ({hs}/{rs} rays on the house)";
    float ang=angs.OrderBy(x=>x).ElementAt(angs.Count/2);var c=t.position+q*new Vector3((lo.x+hi.x)*.5f,0,(lo.z+hi.z)*.5f);c.y=0;
    if(pass==0&&Mathf.Abs(ang)>2f&&Mathf.Abs(ang)<25f){MoveGroup(t,riders,c,Quaternion.Euler(0,ang,0),Vector3.zero);Physics.SyncTransforms();continue;}
    if(mn>.012f&&mn<.8f){var sh=wn*(mn-.01f);sh.y=0;MoveGroup(t,riders,c,Quaternion.identity,sh);Physics.SyncTransforms();return $"back to the wall {mn*100:F1} cm -> 1 cm";}
    return mn>=.8f?$"{mn*100:F0} cm from the nearest wall behind - left":"at the wall";}
   return "";}
  // the first thing a ray meets that is not part of the given object
  static bool RayNotSelf(Vector3 o,Vector3 dir,float len,Transform self,out RaycastHit hit){hit=default;foreach(var h in Physics.RaycastAll(o,dir,len,~0,QueryTriggerInteraction.Ignore).OrderBy(x=>x.distance)){if(self&&h.collider.transform.IsChildOf(self))continue;hit=h;return true;}return false;}
  // set listed pieces down on what is drawn right under their middle: SERVICE_LOOK="1:dining-jug,..."
  // V28b hand fix: take out a piece that has no sound spot (SERVICE_LOOK="1:landing-chair,..."); its plan lists it under removes
  public static void Remove28(){Open();var sb=new StringBuilder();
   foreach(var a in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':'))){
    var p=county.Properties.First(x=>x.Index==int.Parse(a[0]));var t=PropRoots(p).FirstOrDefault(r=>IdOf(r)==a[1]);if(!t){sb.AppendLine($"p{p.Index} {a[1]}: not found");continue;}
    sb.AppendLine($"p{p.Index} {a[1]}: removed ({t.name})");Undo.DestroyObjectImmediate(t.gameObject);}
   log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","v28","sense","remove28.txt"),sb.ToString());Save("Remove28");}
  public static void Drop28(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var grp in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).GroupBy(a=>int.Parse(a[0]))){
    var p=county.Properties.First(x=>x.Index==grp.Key);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var a in grp){var t=roots.FirstOrDefault(r=>IdOf(r)==a[1]);if(!t){sb.AppendLine(a[1]+": not found");continue;}var vs=DrawnVerts(t,8000).ToList();float bottom=vs.Min(v=>v.y);var c=new Vector3(vs.Average(v=>v.x),bottom,vs.Average(v=>v.z));
      bool floorOnly=System.Environment.GetEnvironmentVariable("SERVICE_FLOOR")=="1";float top=vs.Max(v=>v.y);
      float best=float.NaN;foreach(var h in Physics.RaycastAll(floorOnly?new Vector3(c.x,top+.05f,c.z):c+Vector3.up*.05f,Vector3.down,floorOnly?3f:1.5f,floorOnly?(1<<VisHouse):VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(owner.TryGetValue(h.collider,out var ow)&&ow==t)continue;best=h.point.y;break;}
      if(float.IsNaN(best)){sb.AppendLine($"p{p.Index} {a[1]}: nothing under it");continue;}Undo.RecordObject(t,"drop");t.position+=Vector3.up*(best+.002f-bottom);Physics.SyncTransforms();sb.AppendLine($"p{p.Index} {a[1]}: set down {(bottom-best)*100:F1} cm");}}
    finally{Unvisual(vis);}}
   log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","v28","sense","drop28.txt"),sb.ToString());Save("Drop28");}
  public static void Furnish28(){Open();Physics.SyncTransforms();var cat=Fronts();var map=CatalogMap();var sb=new StringBuilder();int added=0,moved=0,removed=0;
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   try{
   var only=(System.Environment.GetEnvironmentVariable("SERVICE_HOUSES")??"").Split(',').Where(x=>x.Trim().Length>0).Select(x=>int.Parse(x.Trim())).ToList();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(only.Count>0&&!only.Contains(p.Index))continue;var f=Path.Combine(Work,"Audit","layouts",$"p{p.Index}-v28b.json");if(!File.Exists(f)||!p.Building)continue;
    var plan=JsonUtility.FromJson<Furnish28Plan>(File.ReadAllText(f));var root=p.transform.Find("V19 interior");if(!root){sb.AppendLine($"p{p.Index}: no V19 interior");continue;}
    var touched=new List<Transform>();
    if(plan.removes!=null)foreach(var id in plan.removes){var t=PropRoots(p).FirstOrDefault(r=>IdOf(r)==id);
     if(t){foreach(var sbk in PropRoots(p).Where(r=>r.name.Contains("— "+id+" —")).ToList())Object.DestroyImmediate(sbk.gameObject);Object.DestroyImmediate(t.gameObject);removed++;sb.AppendLine($"p{p.Index} {id}: removed");}else sb.AppendLine($"p{p.Index} {id}: (remove) NOT FOUND");}
    Physics.SyncTransforms();var lay=LayoutIds(p.Index);
    if(plan.moves!=null)foreach(var m in plan.moves){var roots=PropRoots(p).ToList();var t=roots.FirstOrDefault(r=>IdOf(r)==m.id);
     if(!t||!PieceFrame(t,cat,out var q,out var lo,out var hi,out _)){sb.AppendLine($"p{p.Index} {m.id}: (move) NOT FOUND");continue;}
     var riders=Riders(p,t,q,lo,hi,roots,lay);var c=t.position+q*new Vector3((lo.x+hi.x)*.5f,0,(lo.z+hi.z)*.5f);c.y=0;var turn=Quaternion.Euler(0,Mathf.DeltaAngle(q.eulerAngles.y,m.yaw),0);
     var from=c;MoveGroup(t,riders,c,turn,new Vector3(m.x-c.x,0,m.z-c.z));Physics.SyncTransforms();
     // stand it on the floor at the new spot
     var vs=DrawnVerts(t,8000).ToList();float bottom=vs.Min(v=>v.y);var cc=new Vector3(m.x,bottom+1f,m.z);
     if(RayNotSelf(cc,Vector3.down,1.6f,t,out var fh)){float dy=fh.point.y-bottom;if(Mathf.Abs(dy)<.3f)MoveGroup(t,riders,t.position,Quaternion.identity,Vector3.up*dy);}
     moved++;touched.Add(t);sb.AppendLine($"p{p.Index} {m.id}: moved from {from.x:F2},{from.z:F2} to {m.x:F2},{m.z:F2} facing {m.yaw:F0}{(riders.Count>0?" with "+string.Join(", ",riders.Select(IdOf)):"")}");}
    var placed=new Dictionary<string,GameObject>();
    if(plan.items!=null)foreach(var it in plan.items){
     if(!map.TryGetValue(it.prefab,out var entry)){sb.AppendLine($"p{p.Index} {it.id}: UNKNOWN PREFAB {it.prefab}");continue;}
     var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(entry.path);if(!prefab){sb.AppendLine($"p{p.Index} {it.id}: MISSING ASSET {entry.path}");continue;}
     foreach(var old in PropRoots(p).Where(r=>IdOf(r)==it.id).ToList())Object.DestroyImmediate(old.gameObject); // re-running re-places it
     var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);g.name=$"{it.id} — {it.prefab}";
     g.transform.rotation=Quaternion.Euler(0,it.yaw-(entry.front=="none"?0:FrontYaw(entry.front)),0);float sc=(it.scale>0.01f?it.scale:1f)*entry.scale;if(Mathf.Abs(sc-1)>.001f)g.transform.localScale=Vector3.one*sc;
     var face=Quaternion.Euler(0,it.yaw,0)*Vector3.forward;float y=it.floor;bool onTop=!string.IsNullOrEmpty(it.on);
     g.transform.position=new Vector3(it.x,it.floor,it.z);Physics.SyncTransforms();
     if(onTop){Transform par=placed.TryGetValue(it.on,out var pg)?pg.transform:PropRoots(p).FirstOrDefault(r=>IdOf(r)==it.on);
      if(par){var pv=DrawnVerts(par,12000).Where(v=>Mathf.Abs(v.x-it.x)<.25f&&Mathf.Abs(v.z-it.z)<.25f).Select(v=>v.y).ToList();y=pv.Count>0?pv.Max():BoundsOf(par.gameObject).max.y;}else sb.AppendLine($"   {it.id}: PARENT {it.on} NOT FOUND");}
     else if(it.ceiling){var hb=BoundsOf(g);if(RayNotSelf(new Vector3(it.x,it.floor+1.2f,it.z),Vector3.up,6f,g.transform,out var ch))y=ch.point.y-(hb.max.y-it.floor)-.005f;else y=it.floor+2.4f-(hb.max.y-it.floor);}
     else if(it.wall){y=it.floor+it.height;}
     else if(RayNotSelf(new Vector3(it.x,it.floor+1.0f,it.z),Vector3.down,1.6f,g.transform,out var fh2))y=fh2.point.y; // V28b: not its own collider (every new piece stood its own height in the air)
     g.transform.position=new Vector3(it.x,y,it.z);Physics.SyncTransforms();
     if(it.wall){var b0=BoundsOf(g);g.transform.position+=Vector3.up*(it.floor+it.height-b0.center.y);}
     else if(!it.ceiling){var gv=DrawnVerts(g.transform,8000).ToList();if(gv.Count>0){float bot=gv.Min(v=>v.y);g.transform.position+=Vector3.up*(y-bot);}}
     if(it.light){var top=BoundsOf(g);var l=new GameObject("Lamp light").AddComponent<Light>();l.transform.SetParent(g.transform,false);l.transform.position=it.ceiling?new Vector3(top.center.x,top.min.y+.06f,top.center.z):it.wall?top.center+face*.12f:new Vector3(top.center.x,top.max.y-.08f,top.center.z);l.type=LightType.Point;l.color=new Color(1f,.72f,.42f);l.range=4.2f;l.intensity=1.4f;l.shadows=LightShadows.None;}
     g.isStatic=true;placed[it.id]=g;added++;touched.Add(g.transform);sb.AppendLine($"p{p.Index} {it.id}: added {it.prefab} at {it.x:F2},{g.transform.position.y:F2},{it.z:F2} facing {it.yaw:F0}{(onTop?" on "+it.on:"")}{(it.wall?" on the wall":"")}");}
    Physics.SyncTransforms();lay=LayoutIds(p.Index);
    // to the walls, then out of anything they cut into
    {var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
     try{foreach(var t in touched){if(!t)continue;var id=IdOf(t);var kind=KindOf(id,t.name,lay);
       if(kind=="wall"){var face=-t.forward;var b=BoundsOf(t.gameObject);var tree=new HashSet<Transform>{t};lay.TryGetValue(id,out var wl);
        if(wl!=null){var fw=Quaternion.Euler(0,wl.yaw,0)*Vector3.forward;float best=9;foreach(var hit in Physics.RaycastAll(b.center+fw*.05f,-fw,.8f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){best=hit.distance;break;}
         float back=Vector3.Dot(b.extents,new Vector3(Mathf.Abs(fw.x),0,Mathf.Abs(fw.z)));if(best<9){float gap=best-.05f-back;t.position+=-fw*(gap-.008f);sb.AppendLine($"   {id}: hung flush ({gap*100:F1} cm -> 0.8)");}else sb.AppendLine($"   {id}: NO WALL BEHIND THE WALL PIECE");}
        continue;}
       if(kind!="floor")continue;bool caseGood=(lay.TryGetValue(id,out var li)&&li.againstWall)||CaseWords.Any(PrefabOf(t).ToLowerInvariant().Contains);
       if(caseGood)sb.AppendLine($"   {id}: {FlushOne(p,t,roots,lay,owner,cat)}");
       if(PieceFrame(t,cat,out var q,out var lo,out var hi,out var known)){var riders=Riders(p,t,q,lo,hi,roots,lay);var tree=new HashSet<Transform>(riders){t};float d0=Through28(p,t,owner,tree),dd=d0,outBy=0;var fw=q*Vector3.forward;
        while(known&&dd>.006f&&outBy<.12f){float st=Mathf.Clamp(dd+.004f,.01f,.04f);MoveGroup(t,riders,t.position,Quaternion.identity,fw*st);outBy+=st;Physics.SyncTransforms();dd=Through28(p,t,owner,tree);}
        if(d0>.006f)sb.AppendLine($"   {id}: was {d0*100:F1} cm into the house -> {dd*100:F1} cm (came out {outBy*100:F1} cm){(dd>.006f?"  STILL IN THE HOUSE":"")}");}}}
     finally{Unvisual(vis);}}}
   }finally{if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
   ServiceBuild.RebakeOpenDoors(county);
   sb.Insert(0,$"Furnish28: {added} added, {moved} moved, {removed} removed"+System.Environment.NewLine);Directory.CreateDirectory(Path.Combine(Work,"Audit","v28","sense"));File.WriteAllText(Path.Combine(Work,"Audit","v28","sense","furnish28.txt"),sb.ToString());log.Append(sb.ToString());Save("Furnish28");}
 }
}
