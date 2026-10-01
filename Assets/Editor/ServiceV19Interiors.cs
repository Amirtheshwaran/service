using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2.Editor {
 // V19 interiors from hand-written layouts (Audit/layouts/p<N>.json): each item names a prefab, a spot, the storey,
 // and the compass direction its front faces. Placement is literal; validation reports anything that clips, floats,
 // overhangs or narrows the walk from the front door to the delivery table (Audit/layouts/report.json).
 public static partial class ServiceV19Rebuild {
  [System.Serializable] public class LayoutItem{public string id,prefab,on;public float x,z,floor,yaw,height,scale;public bool wall,againstWall,light,delivery,ceiling;}
  [System.Serializable] public class Layout{public int property;public LayoutItem[] items;}
  [System.Serializable] class CatalogEntry{public string name,source,path,front;public float[] size;public float scale;}
  [System.Serializable] class CatalogList{public CatalogEntry[] items;}
  static Dictionary<string,(string path,string front,float scale)> CatalogMap(){
   var list=JsonUtility.FromJson<CatalogList>("{\"items\":"+File.ReadAllText(Path.Combine(Work,"Audit","prop-catalog-oriented.json"))+"}");
   return list.items.GroupBy(e=>e.name).ToDictionary(g=>g.Key,g=>(g.First().path,g.First().front,g.First().scale>0.01f?g.First().scale:1f));
  }
  static float FrontYaw(string f)=>f=="+X"?90:f=="-Z"?180:f=="-X"?270:0;
  static readonly string[] OldInteriorProps={"Prop_SmallTable","Prop_Lamp_A","Prop_Lamp_C","Pendant cord","Prop_Rug","Prop_Sofa","Prop_Cabinet","Prop_Clock","Prop_Painting","Prop_Vase","Prop_LargeTable","Hall lamp","Prop_Chair","Prop_Bed"};
  public static void Interiors(){Open();InteriorsStage();ServiceBuild.RebakeOpenDoors(county);Save("interiors");}
  public static void InteriorsAndShots(){Interiors();RoomShots();}
  static void InteriorsStage(){
   var dir=Path.Combine(Work,"Audit","layouts");var cat=CatalogMap();var report=new StringBuilder("[\n");bool firstReport=true;
   Physics.SyncTransforms();var nav=NavMesh.AddNavMeshData(county.Navigation);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   try{
   foreach(var file in Directory.GetFiles(dir,"p*.json").OrderBy(f=>f)){
    var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(file));var p=county.Properties.First(x=>x.Index==layout.property);
    var old=p.transform.Find("V19 interior");if(old)Object.DestroyImmediate(old.gameObject);
    foreach(var t in p.GetComponentsInChildren<Transform>(true).Where(t=>t&&t.parent==p.transform&&OldInteriorProps.Any(n=>t.name.StartsWith(n))&&p.InteriorBounds.Contains(t.position)).ToList())Kill(t,"old interior dressing replaced by the V19 layout");
    if(p.Building)foreach(var t in p.Building.GetComponentsInChildren<Transform>(true).Where(t=>t&&t.name.StartsWith("Villa1_Deco_IntDivider")).ToList())if(Vector3.Distance(Flat(t.position),Flat(p.Door.position))<4.5f)Kill(t,"divider standing in the entrance");
    var root=new GameObject("V19 interior").transform;root.SetParent(p.transform,false);
    var placed=new Dictionary<string,GameObject>();var issues=new List<string>();
    foreach(var it in layout.items){
     if(!cat.TryGetValue(it.prefab,out var entry)){issues.Add($"{it.id}: unknown prefab {it.prefab}");continue;}
     var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(entry.path);if(!prefab){issues.Add($"{it.id}: missing asset {entry.path}");continue;}
     var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);g.name=$"{it.id} — {it.prefab}";
     var rot=Quaternion.Euler(0,it.yaw-(entry.front=="none"?0:FrontYaw(entry.front)),0);g.transform.rotation=rot;float sc=(it.scale>0.01f?it.scale:1f)*entry.scale;if(Mathf.Abs(sc-1)>.001f)g.transform.localScale=Vector3.one*sc;
     var faceDir=Quaternion.Euler(0,it.yaw,0)*Vector3.forward;
     float y=it.floor;
     if(!string.IsNullOrEmpty(it.on)&&placed.TryGetValue(it.on,out var parent)){var pb=BoundsOf(parent);y=pb.max.y;}
     else if(it.ceiling){g.transform.position=new Vector3(it.x,it.floor,it.z);var hb=BoundsOf(g);if(Physics.Raycast(new Vector3(it.x,it.floor+1.2f,it.z),Vector3.up,out var ch,6f,~0,QueryTriggerInteraction.Ignore))y=ch.point.y-(hb.max.y-it.floor)-.005f;else{issues.Add($"{it.id}: no ceiling found above");y=it.floor+2.4f-(hb.max.y-it.floor);}if(it.height>0.01f&&ch.collider!=null){float drop=(ch.point.y-it.floor)-it.height-(hb.max.y-hb.min.y);if(drop<0)issues.Add($"{it.id}: hangs {-drop:F2} m lower than the requested clearance");}}
     else if(it.wall){y=it.floor+it.height;}
     else if(Physics.Raycast(new Vector3(it.x,it.floor+1.0f,it.z),Vector3.down,out var fh,1.6f,~0,QueryTriggerInteraction.Ignore))y=fh.point.y;
     g.transform.position=new Vector3(it.x,y,it.z);
     if(it.wall){var b0=BoundsOf(g);var center=b0.center;if(Physics.Raycast(center,-faceDir,out var wh,1.2f,~0,QueryTriggerInteraction.Ignore)){float back=Vector3.Dot(b0.extents,new Vector3(Mathf.Abs(faceDir.x),0,Mathf.Abs(faceDir.z)));g.transform.position+=(-faceDir)*(wh.distance-back-.01f);}else issues.Add($"{it.id}: no wall found behind a wall-mounted item");g.transform.position+=Vector3.up*(it.floor+it.height-BoundsOf(g).center.y);}
     if(it.againstWall&&!it.wall){var b0=BoundsOf(g);var probe=new Vector3(b0.center.x,it.floor+.9f,b0.center.z);if(Physics.Raycast(probe,-faceDir,out var wh,2.0f,~0,QueryTriggerInteraction.Ignore)&&!wh.transform.IsChildOf(root)){float back=Vector3.Dot(b0.extents,new Vector3(Mathf.Abs(faceDir.x),0,Mathf.Abs(faceDir.z)));g.transform.position+=(-faceDir)*(wh.distance-back-.03f);}else issues.Add($"{it.id}: againstWall but no wall within 2 m behind it");}
     if(it.light){var top=BoundsOf(g);var l=new GameObject("Lamp light").AddComponent<Light>();l.transform.SetParent(g.transform,false);l.transform.position=it.ceiling?new Vector3(top.center.x,top.min.y+.06f,top.center.z):it.wall?top.center+(Quaternion.Euler(0,it.yaw,0)*Vector3.forward)*(.12f):new Vector3(top.center.x,top.max.y-.08f,top.center.z);l.type=LightType.Point;l.color=new Color(1f,.72f,.42f);l.range=it.delivery?5.5f:4.2f;l.intensity=it.delivery?2.2f:1.4f;l.shadows=it.delivery?LightShadows.Soft:LightShadows.None;}
     g.isStatic=true;placed[it.id]=g;
    }
    Physics.SyncTransforms();
    // Delivery: papers go on the item flagged delivery, the player stands in front of it.
    var desk=layout.items.FirstOrDefault(i=>i.delivery&&string.IsNullOrEmpty(i.on)&&!i.light)??layout.items.FirstOrDefault(i=>i.delivery);
    if(desk!=null&&placed.TryGetValue(desk.id,out var dg)){var db=BoundsOf(dg);var face=Quaternion.Euler(0,desk.yaw,0)*Vector3.forward;
     p.DeliveryPoint.position=new Vector3(db.center.x,db.max.y+.005f,db.center.z)+face*Mathf.Min(.12f,db.extents.z*.3f);
     var stand=new Vector3(db.center.x,desk.floor,db.center.z)+face*(Vector3.Dot(db.extents,new Vector3(Mathf.Abs(face.x),0,Mathf.Abs(face.z)))+.62f);
     if(NavMesh.SamplePosition(stand,out var sh,1,NavMesh.AllAreas))stand=sh.position;p.TableApproach.position=stand;p.TableApproach.rotation=Quaternion.LookRotation(-face);
     if(p.PostedPaper){p.PostedPaper.transform.position=p.DeliveryPoint.position+Vector3.up*.004f;p.PostedPaper.transform.rotation=Quaternion.LookRotation(face)*Quaternion.Euler(0,0,0);}
     foreach(var l in p.GetComponentsInChildren<Light>(true).Where(l=>l.transform.parent==p.transform&&(l.name=="Occupied room"||(l.name=="House practical light"&&l.transform.position.y>p.Door.position.y+1.2f))))l.transform.position=new Vector3(db.center.x,db.max.y+1.6f,db.center.z);
     if(p.EntitySpawn&&Vector3.Distance(p.EntitySpawn.position,stand)<2.5f)issues.Add("entity spawn is within 2.5 m of the delivery spot");
     EditorUtility.SetDirty(p);}
    else issues.Add("no delivery item flagged");
    // Validation.
    var corridor=new List<Vector3>();if(NavMesh.SamplePosition(p.Door.position-OutwardOf(p)*.9f,out var a,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(p.TableApproach.position,out var tt,2,NavMesh.AllAreas)){var np=new NavMeshPath();if(NavMesh.CalculatePath(a.position,tt.position,NavMesh.AllAreas,np))corridor.AddRange(np.corners);}
    foreach(var kv in placed){var g=kv.Value;var it=layout.items.First(i=>i.id==kv.Key);var b=BoundsOf(g);
     bool flat=b.size.y<.1f&&!it.wall; // rugs lie under furniture by design
     var shrink=new Vector3(Mathf.Max(.005f,b.extents.x-.04f),Mathf.Max(.005f,b.extents.y-.04f),Mathf.Max(.005f,b.extents.z-.04f));
     if(!flat)foreach(var c in Physics.OverlapBox(b.center,shrink,Quaternion.identity,~0,QueryTriggerInteraction.Ignore)){if(c.transform.IsChildOf(g.transform)||c is TerrainCollider||c is CharacterController)continue;var other=c.transform;while(other.parent&&other.parent!=root&&other.parent!=p.transform)other=other.parent;
      if(!string.IsNullOrEmpty(it.on)&&placed.TryGetValue(it.on,out var par)&&c.transform.IsChildOf(par.transform))continue;
      if(other.parent==root){var oi=layout.items.FirstOrDefault(i=>other.name.StartsWith(i.id+" "));if(oi!=null&&oi.on==it.id)continue;}
      if(c.bounds.max.y<=b.min.y+.06f)continue;
      issues.Add($"{it.id} ({it.prefab}) intersects {(other.parent==root?"item "+other.name:"structure "+c.name)}");}
     if(!it.wall&&!it.ceiling&&string.IsNullOrEmpty(it.on)){int miss=0;foreach(var cx in new[]{-1,1})foreach(var cz in new[]{-1,1}){var q=new Vector3(b.center.x+cx*b.extents.x*.85f,b.min.y+.3f,b.center.z+cz*b.extents.z*.85f);if(!Physics.Raycast(q,Vector3.down,out var h2,.6f,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(h2.point.y-b.min.y)>.08f)miss++;}if(miss>0)issues.Add($"{it.id} ({it.prefab}) has {miss} corner(s) without floor beneath (overhang or on stairs)");}
     if(!it.wall&&!it.ceiling&&b.size.y>.25f)for(int i=1;i<corridor.Count;i++){float len=Vector3.Distance(corridor[i-1],corridor[i]);for(float s=0;s<len;s+=.25f){var q=Vector3.Lerp(corridor[i-1],corridor[i],s/len);if(Mathf.Abs(q.y-b.min.y)>1f)continue;var d=Vector2.Distance(new Vector2(q.x,q.z),new Vector2(Mathf.Clamp(q.x,b.min.x,b.max.x),Mathf.Clamp(q.z,b.min.z,b.max.z)));if(d<.4f){issues.Add($"{it.id} ({it.prefab}) narrows the walk to the table near ({q.x:F1},{q.z:F1}) — {d:F2} m clearance");s=len;i=corridor.Count;}}}
    }
    var itemsOut=string.Join(",",placed.Select(kv=>{var b=BoundsOf(kv.Value);return $"{{\"id\":\"{kv.Key}\",\"min\":[{b.min.x:F2},{b.min.y:F2},{b.min.z:F2}],\"max\":[{b.max.x:F2},{b.max.y:F2},{b.max.z:F2}]}}";}));
    report.Append(firstReport?"":",\n");firstReport=false;report.Append($"{{\"property\":{p.Index},\"placed\":{placed.Count},\"delivery\":[{p.DeliveryPoint.position.x:F2},{p.DeliveryPoint.position.y:F2},{p.DeliveryPoint.position.z:F2}],\"stand\":[{p.TableApproach.position.x:F2},{p.TableApproach.position.z:F2}],\"issues\":[{string.Join(",",issues.Select(s=>"\""+s.Replace("\"","'")+"\""))}],\"bounds\":[{itemsOut}]}}");
    log.AppendLine($"INTERIOR {p.Index}: {placed.Count} items, {issues.Count} issues");
   }
   }finally{nav.Remove();if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
   File.WriteAllText(Path.Combine(dir,"report.json"),report+"\n]");
  }
  static Vector3 OutwardOf(ServiceProperty p){var o=p.Door.position-p.InteriorBounds.center;o.y=0;return o.sqrMagnitude<.01f?p.Door.forward:o.normalized;}
  static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
  static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();var lod=g.GetComponent<LODGroup>();if(lod){var first=lod.GetLODs()[0].renderers.Where(r=>r).ToArray();if(first.Length>0)rs=first;}if(rs.Length==0)return new Bounds(g.transform.position,Vector3.zero);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 }
}
