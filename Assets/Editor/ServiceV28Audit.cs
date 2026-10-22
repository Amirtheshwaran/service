using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V28 audit stages ("furniture still misplaced - avoid floating furniture and weird asset placements")
 public static partial class ServiceV19Rebuild {
  // every prop of every house, as saved in the scene: the game compares itself against this at run time (-propDiff)
  public static void PropDump28(){Open();var sb=new StringBuilder();int n=0;
   foreach(var p in county.Properties.OrderBy(x=>x.Index))foreach(var t in PropRoots(p)){foreach(var x in t.GetComponentsInChildren<Transform>(true)){if(x!=t&&!x.GetComponent<Renderer>()&&!x.GetComponent<Rigidbody>())continue;
     sb.AppendLine($"{PathOf(x)}\t{x.position.x:F4}\t{x.position.y:F4}\t{x.position.z:F4}\t{x.eulerAngles.x:F2}\t{x.eulerAngles.y:F2}\t{x.eulerAngles.z:F2}\t{(x.gameObject.activeInHierarchy?1:0)}");n++;}}
   File.WriteAllText(Path.Combine(Work,"ServiceProject","Assets","Resources","V28","props28.txt"),sb.ToString());log.AppendLine($"PropDump28: {n} transforms");
   // rigidbodies anywhere in the houses (anything that can fall or be pushed)
   var rb=new StringBuilder();foreach(var p in county.Properties)foreach(var r in p.GetComponentsInChildren<Rigidbody>(true))rb.AppendLine($"{PathOf(r.transform)} kinematic {r.isKinematic} gravity {r.useGravity} active {r.gameObject.activeInHierarchy}");
   File.WriteAllText(Path.Combine(Work,"Audit","v28","rigidbodies.txt"),rb.ToString());AssetDatabase.Refresh();}
 }
}
