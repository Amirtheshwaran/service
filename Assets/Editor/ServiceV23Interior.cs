using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V23 interior look (batch mode with graphics; nothing is saved): every house from standing spots found by casting
 // down onto its floors (no navmesh in batch), four ways from eye height, lit from the camera; plus a structural dump
 // of each property (interior bounds, building, door, and every furnishing with its position and support).
 public static partial class ServiceV19Rebuild {
  public static void Interior23(){
   Open();var dir=Path.Combine(Work,"Audit","interior23");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   var sb=new StringBuilder();
   foreach(var p in county.Properties){var host=p.Building?p.Building:p.transform;var hb=RBounds(host);
    sb.AppendLine($"== property {p.Index} {p.Address} at {p.transform.position} (parent {PathOf(p.transform.parent)})");
    sb.AppendLine($"   interior bounds {p.InteriorBounds.center} size {p.InteriorBounds.size}; building {PathOf(host)} bounds {hb.center} size {hb.size}; door {p.Door.position}; table {(p.TableApproach?p.TableApproach.position.ToString():"-")}");
    foreach(var ic in p.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("V19 interior")))
     foreach(Transform it in ic){var b=RBounds(it);bool active=it.gameObject.activeInHierarchy;
      float floor=float.NaN;if(GroundHit(new Vector3(b.center.x,b.min.y,b.center.z),b.min.y+.03f,it,out var h))floor=h.point.y;
      sb.AppendLine($"   {(active?"":"[inactive] ")}{it.name} at {it.position} bounds min.y {b.min.y:F2} max.y {b.max.y:F2} size {b.size}  under it {(float.IsNaN(floor)?"nothing":(b.min.y-floor).ToString("F2")+" m above "+h.collider.name)}  inside {p.InteriorBounds.Contains(b.center)}");}
   }
   File.WriteAllText(Path.Combine(dir,"structure.txt"),sb.ToString());
   var n0=NeutralLight();var cam=AuditCam();var lamp=new GameObject("audit lamp").AddComponent<Light>();lamp.type=LightType.Point;lamp.range=16;lamp.intensity=4f;lamp.shadows=LightShadows.None;lamp.transform.SetParent(cam.transform,false);
   try{
    cam.fieldOfView=80;
    foreach(var p in county.Properties){var host=p.Building?p.Building:p.transform;var ib=p.InteriorBounds;var hb=RBounds(host);var area=new Bounds(hb.center,hb.size);area.Encapsulate(ib);
     var pts=new List<Vector3>();
     for(float x=area.min.x+.8f;x<area.max.x;x+=1.6f)for(float z=area.min.z+.8f;z<area.max.z;z+=1.6f)
      foreach(var hit in Physics.RaycastAll(new Vector3(x,area.max.y+1,z),Vector3.down,area.size.y+3,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)){
       if(hit.collider is TerrainCollider||hit.normal.y<.85f)continue;var q=hit.point;
       if(Physics.Raycast(q+Vector3.up*.1f,Vector3.up,1.75f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore))continue; // no headroom (under a table, on a shelf)
       if(!hb.Contains(q+Vector3.up*.5f)&&!ib.Contains(q+Vector3.up*.5f))continue;
       if(pts.Any(o=>Vector3.Distance(o,q)<2.4f))continue;pts.Add(q);}
     File.AppendAllText(Path.Combine(dir,"structure.txt"),$"p{p.Index}: {pts.Count} standing spots\n");
     int i=0;foreach(var q in pts.OrderBy(o=>o.y).ThenBy(o=>o.x).ThenBy(o=>o.z).Take(40)){
      for(int k=0;k<4;k++){cam.transform.position=q+Vector3.up*1.6f;cam.transform.rotation=Quaternion.Euler(14,k*90+45,0);Shoot(cam,Path.Combine(dir,$"p{p.Index}-in-{i:00}-{k}.jpg"),640,360);}i++;}
    }
   }finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
  }
 }
}
