using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V23: everything in the county drawn on the car-exterior layer (8) or the other special layers (9, 10), outside the
 // car, the walker and the view - the player camera hides layer 8 while driving, and raycasts skip 8/9/10.
 public static partial class ServiceV19Rebuild {
  public static void Layers23(){Open();var sb=new StringBuilder();
   foreach(int layer in new[]{8,9,10}){
    var rs=county.GetComponentsInChildren<Renderer>(true).Where(r=>r.gameObject.layer==layer&&!r.transform.IsChildOf(county.Car)&&!(county.Walker&&r.transform.IsChildOf(county.Walker.transform))&&!(county.View&&r.transform.IsChildOf(county.View.transform))).ToList();
    sb.AppendLine($"== layer {layer} ({LayerMask.LayerToName(layer)}): {rs.Count} renderers outside the car/walker/view");
    foreach(var g in rs.GroupBy(r=>{var t=r.transform;var top=t;while(top.parent&&top.parent!=county.transform)top=top.parent;return top.name;}).OrderByDescending(g=>g.Count()))
     sb.AppendLine($"   {g.Count(),5} under '{g.Key}', e.g. {string.Join(", ",g.Take(6).Select(r=>r.name))}");
    var cs=county.GetComponentsInChildren<Collider>(true).Count(c=>c.gameObject.layer==layer&&!c.transform.IsChildOf(county.Car));sb.AppendLine($"   colliders on layer {layer} outside the car: {cs}");}
   sb.AppendLine("collision matrix (layer 8 vs 0..10): "+string.Join(" ",Enumerable.Range(0,11).Select(i=>i+":"+(Physics.GetIgnoreLayerCollision(8,i)?"ignore":"hit"))));
   sb.AppendLine("walker layer "+(county.Walker?county.Walker.gameObject.layer:-1)+", car body layer "+(county.CarBody?county.CarBody.gameObject.layer:-1));
   File.WriteAllText(Path.Combine(Work,"Audit","layers23.txt"),sb.ToString());}
 }
}
