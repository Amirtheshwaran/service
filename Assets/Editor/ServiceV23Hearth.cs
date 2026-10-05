using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V23 probe: chimney / fireplace / hearth / stove pieces in every house, with bounds, to place the fires.
 public static partial class ServiceV19Rebuild {
  public static void HearthProbe23(){Open();var sb=new StringBuilder();bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   foreach(var p in county.Properties){sb.AppendLine($"== p{p.Index} {p.Address} door {p.Door.position} interior {p.InteriorBounds.center} {p.InteriorBounds.size}");
    foreach(var r in p.GetComponentsInChildren<Renderer>(true).Where(r=>{var n=r.name.ToLowerInvariant();return n.Contains("chimn")||n.Contains("fire")||n.Contains("hearth")||n.Contains("stove")||n.Contains("mantel")||n.Contains("brick");}))
     sb.AppendLine($"   {r.name} ({PathOf(r.transform.parent)}) bounds {r.bounds.center} size {r.bounds.size} mats {string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name))}");}
   sb.AppendLine("== shells");
   foreach(var mr in county.GetComponentsInChildren<MeshRenderer>(true).Where(x=>x.name=="Cabin1"||x.name=="Cabin2_Mid1"||x.name=="Cabin2_Mid2"||x.name=="Cabin2_End1"||x.name=="Cabin2_End2")){var mf=mr.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;var m=mf.sharedMesh;
    sb.AppendLine($"   {PathOf(mr.transform)} readable {m.isReadable} submeshes {m.subMeshCount}");
    for(int i=0;i<m.subMeshCount;i++){var sm=m.GetSubMesh(i);var c=mr.transform.TransformPoint(sm.bounds.center);var size=Vector3.Scale(sm.bounds.size,mr.transform.lossyScale);sb.AppendLine($"      sub {i} mat {(i<mr.sharedMaterials.Length&&mr.sharedMaterials[i]?mr.sharedMaterials[i].name:"-")} centre {c} size(local-scaled) {size} rot {mr.transform.rotation.eulerAngles}");}}
   if(county.LateRoad)county.LateRoad.SetActive(lateWas);File.WriteAllText(Path.Combine(Work,"Audit","hearth23.txt"),sb.ToString());}
 }
}
