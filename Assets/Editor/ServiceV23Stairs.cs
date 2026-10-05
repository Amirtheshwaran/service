using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V23 probe: every stair in the county with its colliders, and what a walker running at it from the side meets.
 public static partial class ServiceV19Rebuild {
  public static void Stairs23(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var r in county.GetComponentsInChildren<MeshRenderer>(true).Where(x=>x.name.ToLowerInvariant().Contains("stair"))){
    var b=r.bounds;sb.AppendLine($"{PathOf(r.transform)}  bounds {b.center} size {b.size} zone {(r.GetComponent<ServiceGameV2.ServiceStairZone>()!=null)}");
    foreach(var c in r.GetComponentsInChildren<Collider>(true))sb.AppendLine($"    collider {c.GetType().Name} enabled {c.enabled} convex {(c is MeshCollider mc?mc.convex.ToString():"-")} bounds {c.bounds.center} size {c.bounds.size}");
    foreach(var c in Physics.OverlapBox(b.center,b.extents+Vector3.one*.3f,Quaternion.identity,~0,QueryTriggerInteraction.Collide).Where(c=>!c.transform.IsChildOf(r.transform)&&c.bounds.size.magnitude<6))sb.AppendLine($"    nearby collider {PathOf(c.transform)} {c.GetType().Name} trigger {c.isTrigger} size {c.bounds.size}");
    // from each side at knee height, what stands in the way within 1 m of the stair edge
    foreach(var dir in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back}){var from=b.center-dir*(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(dir.x),0,Mathf.Abs(dir.z)))+1.2f);from.y=b.min.y+.45f;
     if(Physics.Raycast(from,dir,out var h,2.5f,~0,QueryTriggerInteraction.Ignore))sb.AppendLine($"    from {dir}: hits {h.collider.GetType().Name} '{h.collider.name}' at {h.distance:F2} m, face normal {h.normal}");}}
   File.WriteAllText(Path.Combine(Work,"Audit","stairs23.txt"),sb.ToString());}
 }
}
