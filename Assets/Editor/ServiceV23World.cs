using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 // V23 world pass, from the playtest of V22 (Audit/census23 measured each item first):
 //  props    - Flooded Grounds furniture off the car-exterior layer 8 (hidden from the car, skipped by every raycast); rugs
 //             lifted off the floor they z-fought with ("the carpet is spasming"); things that sat past the edge of the
 //             nightstand they belonged on put back on it; pull-chain bulbs up against the ceiling or roof above them;
 //             pictures and clocks hung flush on their wall (a sloped roof is no wall); lamp heads left hanging in the
 //             air by the Route 9 drive taken away ("literal furniture floating in the air").
 //  stairs   - the porch steps' convex-hull colliders were 1 m solid blocks from the side ("an imaginary wall"): the
 //             steps' own mesh now, so they can be climbed from any side.
 //  books    - rows of encyclopedia volumes on the empty bookshelves ("V23 shelf books"; ServiceBooks drops one).
 //  doormat  - a mat on Morrow's porch ("V23 doormat"; ServiceDirector.WipeFeet).
 //  hearths  - a fire in each cabin's brick fireplace, found by its brick in the shell's own texture.
 //  drives   - every drive rebuilt: the old line smoothed (Route 9's S-hook replaced by a straight run), a parking pad with
 //             a rounded end short of the house, a gravel footpath to the steps; terrain levelled and painted, the old
 //             band painted back; Gate, ApproachRoute and DriveLength updated ("roads stop and start abruptly").
 //  cabins   - the two pier cabins: ground raised into a low mound under them ("the house itself is floating").
 //  grass    - the V21 verge rows (1,094 clumps on the road surface) replaced by tall grass spread evenly over the open
 //             ground near the roads and houses, never on a road, a drive, a path or a porch; all foliage settled onto
 //             the ground under it ("some of the bushes are floating").
 public static partial class ServiceV19Rebuild {
  const string V23="Assets/ServiceArt/V23";
  public static void World23(){Open();World23Stage();Save("world23");}
  public static void World23Look(){Open();World23Shots();}
  static void World23Stage(){
   AssetDatabase.Refresh();Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,V23,"Road"));
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   var t=county.GetComponentInChildren<Terrain>();
   Props23();Physics.SyncTransforms();
   Stairs23Fix();Physics.SyncTransforms();
   Books23();Doormat23();
   Drives23(t);Physics.SyncTransforms();
   Hearths23();
   Grass23(t);SnapFoliage23(t);
   Physics.SyncTransforms();ServiceBuild.RebakeOpenDoors(county);log.AppendLine("WORLD23 navmesh rebaked");
   if(county.LateRoad)county.LateRoad.SetActive(lateWas);
  }

  // ---------------------------------------------------------------- props
  static IEnumerable<Transform> InteriorItems(ServiceProperty p)=>p.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("V19 interior")).SelectMany(c=>c.Cast<Transform>());
  static bool Mine(Collider c,Transform u)=>(u&&c.transform.IsChildOf(u))||c is CharacterController;
  static bool Hit(Vector3 from,Vector3 dir,float dist,Transform self,out RaycastHit best){best=default;bool any=false;
   foreach(var h in Physics.RaycastAll(from,dir,dist,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)){if(Mine(h.collider,self))continue;if(county.Car&&h.collider.transform.IsChildOf(county.Car))continue;if(!any||h.distance<best.distance){best=h;any=true;}}
   return any;}
  static void Props23(){
   // layer 8 is the county car's exterior; furniture was imported on it
   int relayered=0;var shelf=county.transform.Find("V19 catalog shelf");
   foreach(var tr in county.GetComponentsInChildren<Transform>(true)){if(tr.gameObject.layer!=8||tr.IsChildOf(county.Car)||(shelf&&tr.IsChildOf(shelf)))continue;tr.gameObject.layer=0;relayered++;}
   log.AppendLine($"PROPS23 {relayered} objects moved off the car-exterior layer 8");Physics.SyncTransforms();
   int rugs=0,set=0,bulbs=0,walls=0,gone=0;
   foreach(var p in county.Properties){var items=InteriorItems(p).ToList();
    var boxes=items.ToDictionary(x=>x,x=>RBounds(x));
    foreach(var it in items){var b=boxes[it];string n=it.name.ToLowerInvariant();
     bool ceilingThing=n.Contains("chandelier")||n.Contains("ceiling")||n.Contains("fan")||n.Contains("pendant")||n.Contains("hanging_light")||n.Contains("caged")||n.Contains("pull_chain")||n.Contains("bulb")||n.Contains("-light —")||n.Contains("lantern");
     bool wallThing=!n.Contains("standing")&&(n.Contains("telephone")||n.Contains("phone")||n.Contains("wall")||n.Contains("sconce")||n.Contains("picture")||n.Contains("painting")||n.Contains("portrait")||n.Contains("frame")||n.Contains("mirror")||n.Contains("clock")&&!n.Contains("alarm")&&!n.Contains("mantel")||n.Contains("dartboard")||n.Contains("bull_head")||n.Contains("calendar")||n.Contains("shelf —")&&b.min.y>p.Door.position.y+.5f);
     // rugs and mats: a few millimetres above the floor they flickered against
     if(n.Contains("rug")||n.Contains("carpet")){if(Hit(new Vector3(b.center.x,b.max.y+.3f,b.center.z),Vector3.down,1.2f,it,out var fh)){it.position+=Vector3.up*(fh.point.y+.007f-b.min.y);rugs++;}continue;}
     // pull-chain bulbs: up against whatever is above them
     if(ceilingThing){if((n.Contains("pull_chain")||n.Contains("bulb"))&&Hit(new Vector3(b.center.x,b.max.y-.02f,b.center.z),Vector3.up,7f,it,out var ch)&&ch.point.y-b.max.y>.01f){it.position+=Vector3.up*(ch.point.y-b.max.y);bulbs++;}continue;}
     // pictures, clocks, mirrors, telephones on walls: flush on a vertical wall
     if(wallThing){bool alongX=b.size.x<b.size.z;var ax=alongX?Vector3.right:Vector3.forward;float half=alongX?b.extents.x:b.extents.z;
      Vector3? bestMove=null;float bestD=9;
      for(float drop=0;drop<=1.6f&&bestMove==null;drop+=.2f){var c=b.center+Vector3.down*drop;
       foreach(var dd in new[]{ax,-ax})if(Hit(c,dd,1.2f,it,out var wh)&&Mathf.Abs(wh.normal.y)<.25f&&wh.distance-half<bestD){bestD=wh.distance-half;bestMove=dd*(wh.distance-half-.006f)+Vector3.down*drop;}}
      if(bestMove.HasValue&&bestMove.Value.magnitude>.01f&&bestMove.Value.magnitude<1.0f){it.position+=bestMove.Value;walls++;log.AppendLine($"PROPS23 p{p.Index} {it.name} hung flush ({bestMove.Value})");}
      continue;}
     // anything resting on furniture past its edge: back onto the top it belongs to
     if(Mathf.Max(b.size.x,b.size.y,b.size.z)<.85f&&Hit(new Vector3(b.center.x,b.min.y+.03f,b.center.z),Vector3.down,3f,it,out var under)&&b.min.y-under.point.y>.05f){
      bool onTop=boxes.Any(o=>o.Key!=it&&Mathf.Abs(o.Value.max.y-b.min.y)<.05f&&b.center.x>o.Value.min.x&&b.center.x<o.Value.max.x&&b.center.z>o.Value.min.z&&b.center.z<o.Value.max.z);
      if(onTop)continue;
      var host=boxes.Where(o=>o.Key!=it&&Mathf.Abs(o.Value.max.y-b.min.y)<.06f&&Vector2.Distance(new Vector2(o.Value.center.x,o.Value.center.z),new Vector2(b.center.x,b.center.z))<.9f).OrderBy(o=>Vector2.Distance(new Vector2(o.Value.center.x,o.Value.center.z),new Vector2(b.center.x,b.center.z))).Select(o=>(KeyValuePair<Transform,Bounds>?)o).FirstOrDefault();
      if(host.HasValue){var hb=host.Value.Value;float mx=Mathf.Max(.02f,hb.extents.x-b.extents.x-.03f),mz=Mathf.Max(.02f,hb.extents.z-b.extents.z-.03f);
       var target=new Vector3(Mathf.Clamp(b.center.x,hb.center.x-mx,hb.center.x+mx),b.center.y,Mathf.Clamp(b.center.z,hb.center.z-mz,hb.center.z+mz));it.position+=target-b.center;set++;
       log.AppendLine($"PROPS23 p{p.Index} {it.name} moved onto {host.Value.Key.name} ({(target-b.center).magnitude:F2} m)");}
      else{it.position+=Vector3.down*(b.min.y-under.point.y);set++;log.AppendLine($"PROPS23 p{p.Index} {it.name} set down onto {under.collider.name}");}}
    }}
   // lamp heads left in the air when their posts went (V19), and the odd hovering yard prop
   foreach(var tr in county.GetComponentsInChildren<Transform>(true).Where(x=>x&&(x.name.StartsWith("Prop_Lamp_E")||x.name.StartsWith("lo_Prop_Lamp_E"))).ToList()){if(!tr)continue;
    var u=tr;while(u.parent&&(u.parent.name.StartsWith("Prop_Lamp_E")||u.parent.name.StartsWith("lo_Prop_Lamp_E")))u=u.parent;var b=RBounds(u);
    if(Hit(new Vector3(b.center.x,b.min.y,b.center.z),Vector3.down,3f,u,out var gh)&&b.min.y-gh.point.y>.4f&&!county.Properties.Any(p=>p.InteriorBounds.Contains(b.center))){Kill(u,"a lamp head hanging in the air (its post went in V19)");gone++;}}
   foreach(var tr in county.GetComponentsInChildren<Transform>(true).Where(x=>x&&x.name.StartsWith("Prop_Car_A")).ToList()){var b=RBounds(tr);if(Hit(new Vector3(b.center.x,b.min.y+.5f,b.center.z),Vector3.down,3f,tr,out var gh)&&b.min.y-gh.point.y>.02f){tr.position+=Vector3.down*(b.min.y-gh.point.y-.02f);log.AppendLine($"PROPS23 {tr.name} set on the ground");}}
   log.AppendLine($"PROPS23 rugs lifted {rugs}, items set on their furniture {set}, bulbs to the ceiling {bulbs}, wall pieces hung flush {walls}, floating heads removed {gone}");
  }

  // ---------------------------------------------------------------- stairs
  static void Stairs23Fix(){int n=0;
   foreach(var mc in county.GetComponentsInChildren<MeshCollider>(true).Where(c=>c.name.ToLowerInvariant().Contains("stair")&&c.convex)){var mf=mc.GetComponent<MeshFilter>();if(mf&&mf.sharedMesh)mc.sharedMesh=mf.sharedMesh;mc.convex=false;n++;log.AppendLine($"STAIRS23 {PathOf(mc.transform)}: the steps' own mesh instead of a solid hull");}
   log.AppendLine($"STAIRS23 {n} stair colliders");}

  // ---------------------------------------------------------------- books
  static void Books23(){
   var set=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ServiceArt/V19/Props/book_encyclopedia_set_01/book_encyclopedia_set_01.prefab");if(!set){log.AppendLine("BOOKS23 no book set prefab");return;}
   int rows=0;
   foreach(var p in county.Properties)foreach(var sh in InteriorItems(p).Where(x=>x.name.ToLowerInvariant().Contains("bookshelf")||x.name.ToLowerInvariant().Contains("bookcase")).ToList()){
    foreach(var old in sh.parent.Cast<Transform>().Where(x=>x.name.StartsWith("V23 shelf books")&&x.name.EndsWith(sh.name)).ToList())Object.DestroyImmediate(old.gameObject);
    // the shelf boards: a collider on the case so books (and you) meet it, then cast down its middle
    var probes=new List<GameObject>();foreach(var mf in sh.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh){var pc=new GameObject("V23 board probe");pc.transform.SetParent(mf.transform,false);pc.AddComponent<MeshCollider>().sharedMesh=mf.sharedMesh;probes.Add(pc);}
    Physics.SyncTransforms();var b=RBounds(sh);bool wideX=b.size.x>b.size.z;var along=wideX?Vector3.right:Vector3.forward;var depth=wideX?Vector3.forward:Vector3.right;
    // the open side faces the room
    var room=p.InteriorBounds.center-b.center;room.y=0;var front=Vector3.Dot(room,depth)>=0?depth:-depth;
    float half2=Vector3.Dot(b.extents,new Vector3(Mathf.Abs(depth.x),0,Mathf.Abs(depth.z)));
    bool wallBehind(Vector3 d)=>Physics.RaycastAll(b.center,d,half2+.45f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore).Any(h=>!h.collider.transform.IsChildOf(sh)&&!(h.collider is CharacterController));
    if(wallBehind(front)&&!wallBehind(-front))front=-front;
    var boards=new List<float>();
    // RaycastAll gives one hit per collider, so step down the case board by board
    foreach(var pc in probes){var mc=pc.GetComponent<MeshCollider>();float yy=b.max.y+.2f;
     for(int guard=0;guard<14;guard++){var ray=new Ray(new Vector3(b.center.x,yy,b.center.z)+front*.06f,Vector3.down);if(!mc.Raycast(ray,out var h,yy-b.min.y+.1f))break;
      if(h.normal.y>.8f&&h.point.y>b.min.y+.25f&&h.point.y<b.max.y-.25f&&!boards.Any(y=>Mathf.Abs(y-h.point.y)<.22f))boards.Add(h.point.y);yy=h.point.y-.012f;}}
    foreach(var pc in probes)Object.DestroyImmediate(pc);Physics.SyncTransforms();
    var pick=boards.OrderBy(y=>Mathf.Abs(y-(b.min.y+1.25f))).Take(2).ToList();log.AppendLine($"BOOKS23 p{p.Index} {sh.name}: case {b.size} boards at {string.Join(",",boards.Select(y=>(y-b.min.y).ToString("F2")))} colliders {sh.GetComponentsInChildren<Collider>().Length}");
    foreach(var y in pick){var row=(GameObject)PrefabUtility.InstantiatePrefab(set,sh.parent);row.name="V23 shelf books — "+sh.name;row.isStatic=false;
     row.transform.rotation=Quaternion.identity;var rb=RBounds(row.transform);bool longX=rb.size.x>rb.size.z;
     // run the row along the shelf, spines out: row.right along the shelf, row.forward out of it
     row.transform.rotation=Quaternion.LookRotation(front,Vector3.up)*Quaternion.Euler(0,longX?0:90,0);
     rb=RBounds(row.transform);var depthAx=Mathf.Abs(Vector3.Dot(rb.size,new Vector3(Mathf.Abs(front.x),0,Mathf.Abs(front.z))));
     var spot=new Vector3(b.center.x,y+.004f,b.center.z)-front*(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(front.x),0,Mathf.Abs(front.z)))-depthAx*.5f-.06f)+along*((p.Index%2==0?1:-1)*.12f);
     row.transform.position+=spot-new Vector3(rb.center.x,rb.min.y,rb.center.z);
     // the row's own axes for ServiceBooks: right along the shelf, forward out of it
     var holder=new GameObject("V23 shelf books — "+sh.name).transform;holder.SetParent(sh.parent,false);holder.SetPositionAndRotation(row.transform.position,Quaternion.LookRotation(front,Vector3.up));
     row.transform.SetParent(holder,true);row.name="Volumes";foreach(var r in row.GetComponentsInChildren<Renderer>())r.gameObject.isStatic=false;
     rows++;log.AppendLine($"BOOKS23 p{p.Index} {sh.name}: a row at {y:F2} m");}}
   log.AppendLine($"BOOKS23 {rows} rows of books on the shelves");}

  // ---------------------------------------------------------------- Morrow's doormat
  static void Doormat23(){var p=county.Properties.FirstOrDefault(x=>x.Index==5);if(!p)return;
   var old=p.transform.Find("V23 doormat");if(old)Object.DestroyImmediate(old.gameObject);
   var pf=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ServiceArt/V19/Props/Flooded/Prop_Rug_D.prefab")??AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ServiceArt/V19/Props/Flooded/Prop_Rug_A.prefab");if(!pf){log.AppendLine("DOORMAT23 no rug prefab");return;}
   var outward=-p.Inward;outward.y=0;outward.Normalize();var at=p.OpeningCentre+outward*.62f;
   var g=(GameObject)PrefabUtility.InstantiatePrefab(pf,p.transform);g.name="V23 doormat";g.transform.SetPositionAndRotation(at,Quaternion.LookRotation(outward,Vector3.up));
   var b=RBounds(g.transform);var size=new Vector2(Mathf.Max(b.size.x,.01f),Mathf.Max(b.size.z,.01f));
   // about 0.95 x 0.6 m, the long side across the door
   var right=Vector3.Cross(Vector3.up,outward);float sx=.95f/Mathf.Max(.01f,Mathf.Abs(Vector3.Dot(b.size,new Vector3(Mathf.Abs(right.x),0,Mathf.Abs(right.z))))),sz=.6f/Mathf.Max(.01f,Mathf.Abs(Vector3.Dot(b.size,new Vector3(Mathf.Abs(outward.x),0,Mathf.Abs(outward.z)))));
   g.transform.localScale=Vector3.Scale(g.transform.localScale,new Vector3(sx,1,sz));Physics.SyncTransforms();b=RBounds(g.transform);
   if(Hit(new Vector3(at.x,at.y+1,at.z),Vector3.down,2.5f,g.transform,out var fh))g.transform.position+=Vector3.up*(fh.point.y+.007f-b.min.y);
   foreach(var c in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
   log.AppendLine($"DOORMAT23 Morrow's doormat at {g.transform.position} size {RBounds(g.transform).size}");}

  // ---------------------------------------------------------------- hearths
  static Texture2D Readable(Texture src){if(!src)return null;var rt=RenderTexture.GetTemporary(src.width,src.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Graphics.Blit(src,rt);var prev=RenderTexture.active;RenderTexture.active=rt;
   var tex=new Texture2D(src.width,src.height,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,src.width,src.height),0,0);tex.Apply();RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);return tex;}
  static bool Brick(Color c)=>c.r>.22f&&c.r>c.g*1.22f&&c.r>c.b*1.32f;
  static void Hearths23(){
   foreach(var h in county.GetComponentsInChildren<ServiceGameV2.ServiceHearth>(true).ToList())Object.DestroyImmediate(h.gameObject);
   var lamp=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ServiceArt/V19/Props/vintage_oil_lamp/vintage_oil_lamp.prefab");
   var flameMf=lamp?lamp.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m=>m.name.Contains("flame")):null;var flameMr=flameMf?flameMf.GetComponent<MeshRenderer>():null;
   var kindling=county.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name.StartsWith("V17 dry_branches_medium_01"));
   foreach(var p in county.Properties.Where(x=>x.Index==0||x.Index==2||x.Index==3)){
    var shells=(p.Building?p.Building:p.transform).GetComponentsInChildren<MeshCollider>().Where(c=>c.name.StartsWith("Cabin")&&!c.name.Contains("Stair")&&!c.name.Contains("Door")).ToList();if(shells.Count==0){log.AppendLine($"HEARTH23 p{p.Index} no shell collider");continue;}
    var tex=new Dictionary<Collider,Texture2D>();foreach(var c in shells){var mr=c.GetComponent<MeshRenderer>();var m=mr?mr.sharedMaterial:null;var src=m?(m.HasProperty("_BaseMap")?m.GetTexture("_BaseMap"):m.mainTexture):null;tex[c]=Readable(src);}
    float floor=p.Door.position.y;var ib=p.InteriorBounds;var hits=new List<(Vector3 pos,Vector3 n,float d)>();var shellHits=new List<(Vector3 pos,Vector3 n,float d)>();var soot=new List<(Vector3 pos,Vector3 n,float d)>();
    var origins=new List<Vector3>();for(float fx=-.35f;fx<=.36f;fx+=.175f)for(float fz=-.35f;fz<=.36f;fz+=.175f){var o=new Vector3(ib.center.x+fx*ib.size.x,floor+.35f,ib.center.z+fz*ib.size.z);if(Vector3.Dot(o-p.OpeningCentre,-p.Inward)<-.5f&&!Physics.CheckSphere(o,.2f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))origins.Add(o);}
    if(origins.Count==0){log.AppendLine($"HEARTH23 p{p.Index}: no clear spot inside");continue;}
    foreach(var o in origins)for(int a=0;a<720;a++){var dir=Quaternion.Euler(0,a*.5f,0)*Vector3.forward;
     foreach(var h in Physics.RaycastAll(o,dir,14f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){
      if(!shells.Contains(h.collider)){if(h.collider.transform.IsChildOf(p.transform)&&!(h.collider is TerrainCollider))break;continue;}
      var tx=tex[h.collider];if(!tx)break;var uv=h.textureCoord;var col=tx.GetPixelBilinear(uv.x,uv.y);if(Brick(col))hits.Add((h.point,h.normal,h.distance));else{shellHits.Add((h.point,h.normal,h.distance));if(col.r<.17f&&col.g<.15f&&col.b<.15f)soot.Add((h.point,h.normal,h.distance));}break;}}
    if(hits.Count<20){log.AppendLine($"HEARTH23 p{p.Index}: no brick hearth found ({hits.Count} brick hits)");continue;}
    // the chimney face: the dominant inward normal; the firebox: brick set back from that face
    var nrm=hits.Select(h=>new Vector3(h.n.x,0,h.n.z).normalized).Aggregate(Vector3.zero,(s,v)=>s+v).normalized;
    var face=hits.Where(h=>Vector3.Dot(new Vector3(h.n.x,0,h.n.z).normalized,nrm)>.85f).Select(h=>Vector3.Dot(h.pos,nrm)).OrderByDescending(v=>v).ToList();if(face.Count==0)continue;float plane=face[face.Count/4];
    var lat=Vector3.Cross(Vector3.up,nrm);var faceHits=hits.Where(h=>Mathf.Abs(Vector3.Dot(h.pos,nrm)-plane)<.15f).ToList();float l0=faceHits.Count>0?faceHits.Min(h=>Vector3.Dot(h.pos,lat)):0,l1=faceHits.Count>0?faceHits.Max(h=>Vector3.Dot(h.pos,lat)):0;
    var recess=hits.Concat(shellHits).Where(h=>{float dl=Vector3.Dot(h.pos,lat);float dp=plane-Vector3.Dot(h.pos,nrm);return dl>l0+.12f&&dl<l1-.12f&&dp>.18f&&dp<1.3f&&Vector3.Dot(new Vector3(h.n.x,0,h.n.z).normalized,nrm)>.6f;}).ToList();
    var anchor=(recess.Count>=5?recess:hits).Aggregate(Vector3.zero,(s,h)=>s+h.pos)/(recess.Count>=5?recess.Count:hits.Count);
    // second look: straight into the face from just in front of it, through the furniture, a hand's width at a time
    var deep=new List<(float l,float depth,Vector3 at)>();var facePt=nrm*plane;
    if(faceHits.Count>0)for(float l=l0;l<=l1;l+=.08f)foreach(float hy in new[]{.22f,.38f,.55f}){var from=lat*l+facePt+nrm*1.3f;from.y=floor+hy;
     foreach(var h in Physics.RaycastAll(from,-nrm,3.2f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(!shells.Contains(h.collider))continue;float dp=plane-Vector3.Dot(h.point,nrm);deep.Add((l,dp,h.point));break;}}
    var box=deep.Where(x=>x.depth>.22f&&x.depth<1.4f).ToList();
    if(box.Count>=4){float lm=box.Average(x=>x.l);float dm=box.Average(x=>x.depth);anchor=lat*lm+facePt-nrm*dm;recess=hits.Take(box.Count).ToList();log.AppendLine($"HEARTH23 p{p.Index}: firebox {box.Count} deep probes, {box.Min(x=>x.l)-l0:F2}..{box.Max(x=>x.l)-l0:F2} m along a {l1-l0:F2} m face, {dm:F2} m deep");}
    var spot=anchor+nrm*(box.Count>=4?Mathf.Min(.3f,box.Average(x=>x.depth)*.5f):.45f);
    // third look, needing no face plane: along a line in front of the brick, a knee-high ray that runs 20 cm deeper than
    // a head-high one has gone into the firebox
    var knee=hits.Where(h=>Vector3.Dot(new Vector3(h.n.x,0,h.n.z).normalized,nrm)>.8f).ToList();bool archFound=false;
    if(knee.Count>0){var bc=knee.Aggregate(Vector3.zero,(acc,h)=>acc+h.pos)/knee.Count;var arch=new List<(float l,float d)>();
     float D(Vector3 from){foreach(var h in Physics.RaycastAll(from,-nrm,4.5f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))if(shells.Contains(h.collider))return h.distance;return 99;}
     for(float l=-1.8f;l<=1.8f;l+=.06f){var basePt=bc+nrm*2.2f+lat*l;float dh=D(new Vector3(basePt.x,floor+2.1f,basePt.z));float dl=Mathf.Min(D(new Vector3(basePt.x,floor+.66f,basePt.z)),Mathf.Min(D(new Vector3(basePt.x,floor+.82f,basePt.z)),D(new Vector3(basePt.x,floor+1.0f,basePt.z))));
      if(dh<9&&dl<9&&dl-dh>.2f&&dl-dh<1.6f)arch.Add((l,dl));}
     if(arch.Count>=3){float lm=arch.Average(x=>x.l);float dm=arch.Average(x=>x.d);var front2=bc+nrm*2.2f+lat*lm;spot=front2-nrm*(dm-.25f);archFound=true;
      log.AppendLine($"HEARTH23 p{p.Index}: arch {arch.Count} samples, {arch.Min(x=>x.l)-lm:F2}..{arch.Max(x=>x.l)-lm:F2} m wide, back wall {dm-2.2f:F2} m behind the face line");}}
    if(!archFound){var near=soot.Where(sh=>hits.Any(bh=>Vector3.Distance(bh.pos,sh.pos)<.9f)&&Vector3.Dot(new Vector3(sh.n.x,0,sh.n.z).normalized,nrm)>.7f).ToList();
     if(near.Count>=6){var sc=near.Aggregate(Vector3.zero,(acc,h)=>acc+h.pos)/near.Count;spot=sc+nrm*.2f;archFound=true;log.AppendLine($"HEARTH23 p{p.Index}: painted firebox, {near.Count} soot-dark hits around {sc}");}
     else if(faceHits.Count>0){spot=lat*((l0+l1)*.5f)+nrm*(plane+.3f);archFound=true;log.AppendLine($"HEARTH23 p{p.Index}: fire at the middle of the chimney face ({l1-l0:F2} m wide), no recess in the mesh");}
     else log.AppendLine($"HEARTH23 p{p.Index}: no arch found by depth or soot ({soot.Count} dark hits, {near.Count} by the brick)");}
    if(Hit(new Vector3(spot.x,floor+1.2f,spot.z),Vector3.down,1.6f,null,out var fh))spot.y=fh.point.y;else spot.y=floor;
    var root=new GameObject("V23 hearth").transform;root.SetParent(p.transform,false);root.SetPositionAndRotation(spot,Quaternion.LookRotation(nrm,Vector3.up));
    // the cabins' fireplaces are closed iron fireboxes in the brick (no opening in the mesh): the fire is its light on the
    // room and its crackle; visible flames wait for a licensed fire texture (see README-V23)
    var flames=new List<Transform>();

    log.AppendLine($"HEARTH23 p{p.Index}: flames {string.Join(",",flames.Select(x=>{var r=x.GetComponent<Renderer>();return r?r.bounds.size.y.ToString("F2"):"-";}))} m tall, mesh unit {(flameMf?flameMf.sharedMesh.bounds.size.y:0):F4}");
    var lg=new GameObject("Fire glow").AddComponent<Light>();lg.transform.SetParent(root,false);lg.transform.localPosition=new Vector3(0,.85f,.4f);lg.type=LightType.Point;lg.color=new Color(1f,.5f,.2f);lg.range=5.5f;lg.intensity=1.3f;lg.shadows=LightShadows.None;
    var hearth=root.gameObject.AddComponent<ServiceGameV2.ServiceHearth>();hearth.Property=p.Index;hearth.Glow=lg;hearth.Flames=flames.ToArray();hearth.NightsLit=p.Index==0?7:p.Index==2?4:1;
    log.AppendLine($"HEARTH23 p{p.Index}: fire at {spot} facing {nrm} ({hits.Count} brick hits, {recess.Count} in the firebox), lit nights mask {hearth.NightsLit}");
    foreach(var tx in tex.Values)if(tx)Object.DestroyImmediate(tx);}
  }

  // ---------------------------------------------------------------- drives
  sealed class Drive23{public ServiceProperty p;public Line line;public Line path;public Vector3 foot;public float padEnd;public Line old;}
  static Line FromPoints(List<Vector3> pts){var l=new Line();float s=0;for(int i=0;i<pts.Count;i++){if(i>0)s+=Vector2.Distance(new Vector2(pts[i-1].x,pts[i-1].z),new Vector2(pts[i].x,pts[i].z));l.P.Add(pts[i]);l.S.Add(s);}return l;}
  static List<Vector2> Resample(List<Vector2> pts,float step){var res=new List<Vector2>{pts[0]};float carry=0;for(int i=1;i<pts.Count;i++){var a=pts[i-1];var b=pts[i];float seg=Vector2.Distance(a,b);float d=step-carry;while(d<=seg){res.Add(Vector2.Lerp(a,b,d/seg));d+=step;}carry=seg-(d-step);}if(Vector2.Distance(res[res.Count-1],pts[pts.Count-1])>.2f)res.Add(pts[pts.Count-1]);return res;}
  static List<Vector2> Smooth(List<Vector2> pts,int radius,int passes){var cur=pts.ToList();for(int k=0;k<passes;k++){var next=cur.ToList();for(int i=1;i<cur.Count-1;i++){Vector2 sum=Vector2.zero;float w=0;for(int j=-radius;j<=radius;j++){int q=Mathf.Clamp(i+j,0,cur.Count-1);float ww=1f-Mathf.Abs(j)/(radius+1f);sum+=cur[q]*ww;w+=ww;}next[i]=sum/w;}cur=next;}return cur;}
  // the apron flare at the road, the lane, and a parking pad widening over the last few metres (a short drive - Route 9's -
  // is all apron and lane: a pad there would merge with the road into one blob)
  static float PadHalf(float s,float len)=>len<12f?1.55f+1.1f*Mathf.Exp(-s/1.7f):Mathf.Lerp(1.55f+1.1f*Mathf.Exp(-s/1.7f),2.7f,Mathf.SmoothStep(0,1,(s-(len-7.5f))/4f));
  static void Drives23(Terrain t){
   var gravel=AssetDatabase.LoadAssetAtPath<Material>($"{V19}/Road/V19 gravel drive.mat");var late=Centre(LateRoadPath,t,.8f,9);var main=Centre(MainRoad,t,.8f,9);
   var drives=new List<Drive23>();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){
    var mf=county.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m=>m.name.StartsWith($"V19 drive {p.Index} "));if(!mf||p.ApproachRoute==null||p.ApproachRoute.Length<2){log.AppendLine($"DRIVES23 p{p.Index}: no drive");continue;}
    var v=mf.sharedMesh.vertices;int n=v.Length/2;var oldPts=new List<Vector3>();for(int i=0;i<n;i++)oldPts.Add((mf.transform.TransformPoint(v[i*2])+mf.transform.TransformPoint(v[i*2+1]))*.5f);
    var old=FromPoints(oldPts);var foot=p.ApproachRoute[p.ApproachRoute.Length-1];var mouth=oldPts[0];
    // the true foot of the steps: out from the door until the ground under you is the ground (not the porch or a step)
    var stair=(p.Building?p.Building:p.transform).GetComponentsInChildren<Renderer>().Where(r=>r.name.Contains("Stair")&&!r.name.Contains("Int")&&Vector3.Distance(r.bounds.center,p.OpeningCentre)<14f).OrderBy(r=>Vector3.Distance(r.bounds.center,p.OpeningCentre)).FirstOrDefault();
    bool footFound=false;
    if(stair){var sb2=stair.bounds;float bestDrop=-1;Vector3 low=sb2.center,dirLow=Vector3.zero;
     // the way down is the axis along which the treads drop the most; the foot is at its low end
     foreach(var ax in new[]{Vector3.right,Vector3.forward}){float ext=Mathf.Abs(Vector3.Dot(sb2.extents,ax))-.12f;var a=sb2.center+ax*ext;var b=sb2.center-ax*ext;
      float ha=Hit(new Vector3(a.x,sb2.max.y+1,a.z),Vector3.down,sb2.size.y+3,null,out var hA)?hA.point.y:sb2.min.y;float hb=Hit(new Vector3(b.x,sb2.max.y+1,b.z),Vector3.down,sb2.size.y+3,null,out var hB)?hB.point.y:sb2.min.y;
      if(Mathf.Abs(ha-hb)>bestDrop){bestDrop=Mathf.Abs(ha-hb);low=ha<hb?a:b;dirLow=ha<hb?ax:-ax;}}
     var q=low+dirLow*.55f;if(bestDrop>.15f&&Hit(q+Vector3.up*2.5f,Vector3.down,6f,null,out var sgh)){foot=q;foot.y=sgh.point.y;footFound=true;log.AppendLine($"DRIVES23 p{p.Index} foot of {stair.name} at {foot} (drops {bestDrop:F2} m)");}}
    if(!footFound){var outw=-p.Inward;outw.y=0;outw.Normalize();for(float dd=.5f;dd<9f;dd+=.2f){var q=p.OpeningCentre+outw*dd;if(Hit(q+Vector3.up*2.5f,Vector3.down,6f,null,out var gh)&&gh.collider is TerrainCollider){foot=q+outw*.35f;foot.y=gh.point.y;break;}}}
    // the house end: a pad 7 m short of the foot of the steps, the footpath the rest of the way
    List<Vector2> ctrl;
    var into=new Vector2(oldPts[Mathf.Min(6,n-1)].x-mouth.x,oldPts[Mathf.Min(6,n-1)].z-mouth.z).normalized;
    if(p.Index==2){var f2=new Vector2(foot.x,foot.z);var m2=new Vector2(mouth.x,mouth.z);var dir=(f2-m2).normalized;ctrl=new List<Vector2>{m2,m2+into*3.5f,m2+into*3.5f+((f2-dir*3.5f)-(m2+into*3.5f))*.5f,f2-dir*3.5f};}
    else{var xz=oldPts.Select(q=>new Vector2(q.x,q.z)).ToList();xz.Add(new Vector2(foot.x,foot.z));var rs=Resample(xz,1f);var sm=Smooth(rs,6,3);
     // keep the first 6 m as built (the apron meets the road square) and stop 6.5 m short of the foot
     float acc=0;var keep=new List<Vector2>();for(int i=0;i<sm.Count;i++){if(i>0)acc+=Vector2.Distance(sm[i-1],sm[i]);var q=i<6?rs[Mathf.Min(i,rs.Count-1)]:sm[i];if(Vector2.Distance(q,new Vector2(foot.x,foot.z))<6.5f)break;keep.Add(q);}
     ctrl=keep.Count>=3?keep:new List<Vector2>{new Vector2(mouth.x,mouth.z),new Vector2(foot.x,foot.z)};}
    var line=Centre(ctrl,t,.6f,3,mouth.y);var padEnd=line.Length;
    var endP=line.P[line.P.Count-1];var path=Centre(new List<Vector2>{new Vector2(endP.x,endP.z),new Vector2(foot.x,foot.z)},t,.5f,2,endP.y);
    drives.Add(new Drive23{p=p,line=line,path=path,foot=foot,padEnd=padEnd,old=old});
    // the old band back to the forest floor, then level and paint the new
    PaintVerge(t,old,s=>1.55f,0,1.4f);
    Func<float,float> half=s=>PadHalf(s,padEnd);Level(t,line,half,3.5f);Level(t,path,s=>.6f,1.5f);
    PaintVerge(t,line,half,1,1.2f);PaintVerge(t,path,s=>.6f,1,.6f);}
   // the two pier cabins sit down into a low mound of their own ground
   foreach(var p in county.Properties.Where(x=>x.Index==0||x.Index==2)){var shell=(p.Building?p.Building:p.transform).GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name=="Cabin1");if(!shell)continue;var b=shell.bounds;
    var foot=new Line();foot.P.Add(new Vector3(b.min.x,b.min.y+.015f,b.center.z));foot.P.Add(new Vector3(b.max.x,b.min.y+.015f,b.center.z));foot.S.Add(0);foot.S.Add(b.size.x);
    Level(t,foot,s=>b.extents.z-Verge+.2f,2.5f);log.AppendLine($"DRIVES23 p{p.Index} cabin set into a mound up to {b.min.y-.02f:F2} m");}
   // re-fit to the levelled ground, then build
   foreach(var dr in drives){foreach(var l in new[]{dr.line,dr.path})for(int i=0;i<l.P.Count;i++){var q=l.P[i];q.y=TerrainY(t,q)+.035f;l.P[i]=q;}
    var p=dr.p;var mf=county.GetComponentsInChildren<MeshFilter>(true).First(m=>m.name.StartsWith($"V19 drive {p.Index} "));
    var mesh=DriveMesh(dr.line,s=>PadHalf(s,dr.padEnd),2.7f,mf.transform,$"V23 drive {p.Index}");mf.sharedMesh=mesh;var mc=mf.GetComponent<MeshCollider>();if(mc)mc.sharedMesh=mesh;EditorUtility.SetDirty(mf);
    var oldPath=mf.transform.parent.Find($"V23 footpath {p.Index}");if(oldPath)Object.DestroyImmediate(oldPath.gameObject);
    var pg=new GameObject($"V23 footpath {p.Index}",typeof(MeshFilter),typeof(MeshRenderer));pg.transform.SetParent(mf.transform.parent,false);var pm=DriveMesh(dr.path,s=>.62f,.62f,pg.transform,$"V23 footpath {p.Index}");
    pg.GetComponent<MeshFilter>().sharedMesh=pm;var pr=pg.GetComponent<MeshRenderer>();pr.sharedMaterial=gravel?gravel:mf.GetComponent<MeshRenderer>().sharedMaterial;pr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;pg.isStatic=true;pg.AddComponent<MeshCollider>().sharedMesh=pm;pg.AddComponent<ServiceGameV2.ServiceSurface>().Kind="gravel";
    // the walk: along the drive from 3 m to the pad, then the footpath; the car may go as far as the pad's end
    var walk=new List<Vector3>();for(int i=0;i<18;i++)walk.Add(At(dr.line,Mathf.Lerp(3,dr.padEnd,i/17f),out _)+Vector3.up*.02f);for(int i=1;i<=7;i++)walk.Add(At(dr.path,dr.path.Length*i/7f,out _)+Vector3.up*.02f);
    p.ApproachRoute=walk.ToArray();p.DriveLength=dr.padEnd-1.2f;
    var gate=At(dr.line,Mathf.Min(4.5f,dr.line.Length*.3f),out _);var ahead=At(dr.line,Mathf.Min(10,dr.line.Length*.5f),out _);p.Gate.position=gate+Vector3.up*.02f;var fw=ahead-gate;fw.y=0;p.Gate.rotation=Quaternion.LookRotation(fw);EditorUtility.SetDirty(p);
    log.AppendLine($"DRIVES23 p{p.Index} {p.Address}: drive {dr.padEnd:F0} m to a {PadHalf(dr.padEnd,dr.padEnd)*2:F1} m pad with a rounded end, footpath {dr.path.Length:F1} m to the steps (old drive {dr.old.Length:F0} m)");}
   ClearCorridors(t,drives.Select(dr=>(dr.line,(Func<float,float>)(s=>PadHalf(s,dr.padEnd)))).Concat(drives.Select(dr=>(dr.path,(Func<float,float>)(s=>.6f)))).ToList());
   t.Flush();}
  // a drive ribbon whose end is a half circle (no blunt square end); u across, v along
  static Mesh DriveMesh(Line l,Func<float,float> half,float capRadius,Transform space,string name){
   var cl=l.P.ToList();var cs=l.S.ToList();var last=cl[cl.Count-1];var f=last-cl[cl.Count-2];f.y=0;f.Normalize();
   int caps=8;for(int k=1;k<=caps;k++){float x=capRadius*k/(float)caps;cl.Add(last+f*x);cs.Add(l.Length+x);}
   var verts=new List<Vector3>();var uvs=new List<Vector2>();var tris=new List<int>();
   for(int i=0;i<cl.Count;i++){var fw=(i<cl.Count-1?cl[i+1]-cl[i]:cl[i]-cl[i-1]);fw.y=0;fw.Normalize();var r=new Vector3(fw.z,0,-fw.x);
    float h=cs[i]<=l.Length?half(cs[i]):half(l.Length)*Mathf.Sqrt(Mathf.Max(0,1-Mathf.Pow((cs[i]-l.Length)/capRadius,2)));h=Mathf.Max(h,.02f);
    foreach(var (lat,u) in new[]{(-h*1.18f,0f),(h*1.18f,1f)}){var w=cl[i]+r*lat;w.y=cl[Mathf.Min(i,l.P.Count-1)].y;verts.Add(space.InverseTransformPoint(w));uvs.Add(new Vector2(u,cs[i]/3.2f));}}
   for(int i=0;i<cl.Count-1;i++){int a=i*2;tris.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
   var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(verts);mesh.SetUVs(0,uvs);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();
   var nm=mesh.normals;if(nm.Length>0&&nm.Average(x=>x.y)<0){var tr=mesh.triangles;for(int i=0;i<tr.Length;i+=3){var x=tr[i+1];tr[i+1]=tr[i+2];tr[i+2]=x;}mesh.triangles=tr;mesh.RecalculateNormals();}
   mesh.RecalculateTangents();mesh.RecalculateBounds();var path=$"{V23}/Road/{name}.asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);return mesh;}

  // ---------------------------------------------------------------- grass
  static void Grass23(Terrain t){
   // materials as the verge rows wore them (URP conversions), keyed by the prefab's own material names
   var conv=new Dictionary<string,Material>();var rows=new[]{county.transform.Find("V21 verge grass"),county.LateRoad?county.LateRoad.transform.Find("V21 verge grass (Route 9)"):null};
   foreach(var row in rows.Where(x=>x))foreach(var r in row.GetComponentsInChildren<Renderer>(true)){var src=PrefabUtility.GetCorrespondingObjectFromSource(r);if(!src)continue;var a=src.sharedMaterials;var b=r.sharedMaterials;for(int i=0;i<a.Length&&i<b.Length;i++)if(a[i]&&b[i]&&!conv.ContainsKey(a[i].name))conv[a[i].name]=b[i];}
   int removed=0;foreach(var row in rows.Where(x=>x)){removed+=row.childCount;Object.DestroyImmediate(row.gameObject);}
   var prefabs=new[]{"Grass_Tall_A","Grass_Tall_C","Grass_Tall_B","Grass_Small_C"}.Select(nm=>AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Flooded_Grounds/Prefabs/Nature/Grass/{nm}.prefab")).Where(x=>x).ToArray();
   if(prefabs.Length==0){log.AppendLine("GRASS23 no grass prefabs");return;}
   var holder=county.transform.Find("V23 open grass");if(holder)Object.DestroyImmediate(holder.gameObject);holder=new GameObject("V23 open grass").transform;holder.SetParent(county.transform,false);
   var roads=county.transform.Find("V19 county roads");var lateT=county.LateRoad?county.LateRoad.transform:null;
   bool RoadUnder(Vector3 q){foreach(var h in Physics.RaycastAll(new Vector3(q.x,q.y+20,q.z),Vector3.down,40,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)){var tr=h.collider.transform;if(roads&&tr.IsChildOf(roads))return true;if(tr.name.StartsWith("V19 County Route 9")||tr.name.StartsWith("V19 drive")||tr.name.StartsWith("V23 footpath")||tr.name.StartsWith("V19 depot"))return true;}return false;}
   var lines=new List<Line>{Centre(MainRoad,t,.8f,9),Centre(LateRoadPath,t,.8f,9)};foreach(var p in county.Properties)if(p.ApproachRoute!=null&&p.ApproachRoute.Length>1)lines.Add(FromPoints(p.ApproachRoute.ToList()));
   var houses=county.Properties.Select(p=>{var b=RBounds(p.Building?p.Building:p.transform);b.Expand(new Vector3(3.2f,0,3.2f));return b;}).ToList();
   var keep=new List<Vector3>();keep.AddRange(county.Properties.Select(p=>p.Gate.position));keep.AddRange(county.Properties.Where(p=>p.Mailbox).Select(p=>p.Mailbox.position));
   keep.AddRange(county.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Utility pole")||x.name.StartsWith("Street sign")||x.name.StartsWith("County sign")||x.name.StartsWith("Depot sign")).Select(x=>x.position));
   var td=t.terrainData;var o=t.transform.position;var rng=new System.Random(2309);int placed=0;const float step=2.5f;
   for(float x=o.x+1;x<o.x+td.size.x-1;x+=step)for(float z=o.z+1;z<o.z+td.size.z-1;z+=step){
    var q=new Vector3(x+((float)rng.NextDouble()-.5f)*step*.9f,0,z+((float)rng.NextDouble()-.5f)*step*.9f);
    float near=lines.Min(l=>PolyDist(l,q,out _));bool byHouse=county.Properties.Any(p=>Vector2.Distance(new Vector2(p.Door.position.x,p.Door.position.z),new Vector2(q.x,q.z))<38f);
    if(near>42f&&!byHouse)continue;                         // the visible band: along the roads and drives, round the houses
    if(near<1.6f)continue;                                   // never on a road or a drive (or hard against one)
    if(houses.Any(b=>q.x>b.min.x&&q.x<b.max.x&&q.z>b.min.z&&q.z<b.max.z))continue;
    if(q.x>-14&&q.x<12&&q.z>-16&&q.z<13)continue;           // the depot yard
    if(keep.Any(k=>Vector2.Distance(new Vector2(k.x,k.z),new Vector2(q.x,q.z))<2f))continue;
    q.y=TerrainY(t,q);var nrm=td.GetInterpolatedNormal((q.x-o.x)/td.size.x,(q.z-o.z)/td.size.z);if(nrm.y<.85f)continue;
    if(RoadUnder(q))continue;
    if(Physics.CheckSphere(q+Vector3.up*.6f,.35f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))continue; // a trunk, a rock, a post
    var pf=prefabs[rng.Next(prefabs.Length)];var g=(GameObject)PrefabUtility.InstantiatePrefab(pf,holder);g.transform.SetPositionAndRotation(q,Quaternion.Euler(0,(float)rng.NextDouble()*360,0));g.transform.localScale*=.8f+(float)rng.NextDouble()*.45f;
    foreach(var r in g.GetComponentsInChildren<Renderer>()){var ms=r.sharedMaterials;for(int m=0;m<ms.Length;m++)if(ms[m]&&conv.TryGetValue(ms[m].name,out var cm))ms[m]=cm;r.sharedMaterials=ms;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
    foreach(var c in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
    placed++;}
   foreach(var m in conv.Values.Distinct())if(m&&!m.enableInstancing){m.enableInstancing=true;EditorUtility.SetDirty(m);}
   log.AppendLine($"GRASS23 removed {removed} verge clumps; {placed} tall-grass clumps spread evenly ({step} m grid) over the open ground by the roads and houses ({conv.Count} URP materials carried over)");}
  // every bush, fern and tuft settled onto the ground under it (the lowest point under its base, a little into it)
  static void SnapFoliage23(Terrain t){int moved=0;var seen=new HashSet<Transform>();
   foreach(var r in county.GetComponentsInChildren<Renderer>(true)){var u=FoliageRoot(r.transform);if(!u||!seen.Add(u))continue;var b=RBounds(u);float rad=Mathf.Min(Mathf.Max(b.extents.x,b.extents.z)*.5f,.6f);
    float low=float.MaxValue;foreach(var off in new[]{Vector3.zero,Vector3.right*rad,Vector3.left*rad,Vector3.forward*rad,Vector3.back*rad}){var q=new Vector3(b.center.x,0,b.center.z)+off;float g=TerrainY(t,q);
     // a built surface right at the base (a porch, a drive) counts; anything higher (a branch, a rock) does not
     foreach(var h in Physics.RaycastAll(new Vector3(q.x,b.min.y+.3f,q.z),Vector3.down,1.5f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))if(!(h.collider is TerrainCollider)&&!h.collider.transform.IsChildOf(u)&&h.point.y>g)g=h.point.y;
     low=Mathf.Min(low,g);}
    float gap=b.min.y-low;if(gap>.05f||gap<-.35f){u.position+=Vector3.up*(-gap-.03f);moved++;}}
   log.AppendLine($"FOLIAGE23 {moved} plants settled onto the ground");}

  // ---------------------------------------------------------------- look
  static void World23Shots(){var dir=Path.Combine(Work,"Audit","world23");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   var t=county.GetComponentInChildren<Terrain>();var n0=NeutralLight();var cam=AuditCam();var lamp=new GameObject("audit lamp").AddComponent<Light>();lamp.type=LightType.Point;lamp.range=14;lamp.intensity=3.5f;lamp.shadows=LightShadows.None;lamp.transform.SetParent(cam.transform,false);lamp.enabled=false;
   try{foreach(var p in county.Properties){var r=p.ApproachRoute;var hb=RBounds(p.Building?p.Building:p.transform);
     var all=new Bounds(hb.center,hb.size);if(r!=null)foreach(var v in r)all.Encapsulate(v);
     cam.orthographic=true;cam.nearClipPlane=1;cam.farClipPlane=300;cam.orthographicSize=Mathf.Max(all.extents.x,all.extents.z)+8;cam.transform.position=all.center+Vector3.up*120;cam.transform.rotation=Quaternion.Euler(90,0,0);Shoot(cam,Path.Combine(dir,$"p{p.Index}-top.jpg"),1200,1200);
     cam.orthographic=false;cam.nearClipPlane=.05f;cam.farClipPlane=400;cam.fieldOfView=62;
     if(r!=null&&r.Length>3){for(int k=0;k<r.Length;k+=4){int j=Mathf.Min(k+2,r.Length-1);var f=r[j]-r[Mathf.Max(0,k-1)];f.y=0;if(f.sqrMagnitude<.01f)continue;var eye=r[k];eye.y=TerrainY(t,eye)+1.6f;cam.transform.position=eye;cam.transform.rotation=Quaternion.LookRotation(f.normalized+Vector3.down*.2f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-walk-{k:00}.jpg"),960,540);}
      var m0=r[0];var into=r[2]-m0;into.y=0;into.Normalize();var e2=m0-into*10;e2.y=TerrainY(t,e2)+1.3f;cam.transform.position=e2;cam.transform.rotation=Quaternion.LookRotation(into+Vector3.down*.1f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-from-road.jpg"),960,540);}
     for(int k=0;k<4;k++){var d=Quaternion.Euler(0,k*90+30,0)*Vector3.forward;float reach=Mathf.Max(hb.extents.x,hb.extents.z)+8;var eye=hb.center+d*reach;eye.y=TerrainY(t,eye)+1.4f;cam.transform.position=eye;var look=hb.center;look.y=eye.y-1.1f;cam.transform.LookAt(look);Shoot(cam,Path.Combine(dir,$"p{p.Index}-side-{k}.jpg"),960,540);}
     lamp.enabled=true;var h=p.GetComponentInChildren<ServiceGameV2.ServiceHearth>();if(h){var hp=h.transform.position;cam.fieldOfView=55;cam.transform.position=hp+h.transform.forward*1.6f+Vector3.up*.9f;cam.transform.LookAt(hp+Vector3.up*.25f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-hearth.jpg"),960,540);
      cam.transform.position=hp+h.transform.forward*3.5f+Vector3.up*1.6f;cam.transform.LookAt(hp+Vector3.up*.4f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-hearth-room.jpg"),960,540);}
     foreach(var bk in p.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("V23 shelf books")).Take(2)){cam.transform.position=bk.position+bk.forward*1.6f+Vector3.up*.2f;cam.transform.LookAt(bk.position);Shoot(cam,Path.Combine(dir,$"p{p.Index}-books-{bk.GetSiblingIndex()}.jpg"),640,400);}
     var mat=p.transform.Find("V23 doormat");if(mat){cam.transform.position=mat.position+mat.forward*1.8f+Vector3.up*1.5f;cam.transform.LookAt(mat.position);Shoot(cam,Path.Combine(dir,$"p{p.Index}-doormat.jpg"),640,400);}
     lamp.enabled=false;}
   }finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}}
 }
}
