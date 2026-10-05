using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V23 probe: every renderer in the county whose material is missing or whose shader is not a working URP shader.
 public static partial class ServiceV19Rebuild {
  public static void Mats23(){Open();var sb=new StringBuilder();bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);int bad=0;
   foreach(var r in county.GetComponentsInChildren<Renderer>(true)){if(r is ParticleSystemRenderer)continue;var ms=r.sharedMaterials;
    for(int i=0;i<ms.Length;i++){var m=ms[i];string why=null;if(!m)why="missing material";else if(!m.shader)why="no shader";else if(!m.shader.isSupported)why="unsupported shader "+m.shader.name;else if(m.shader.name.StartsWith("Hidden/InternalErrorShader"))why="error shader";
     else if(!(m.shader.name.StartsWith("Universal Render Pipeline")||m.shader.name.StartsWith("Shader Graphs")||m.shader.name.StartsWith("Service")||m.shader.name.StartsWith("CTI")||m.shader.name.StartsWith("Sprites")||m.shader.name.StartsWith("TextMeshPro")||m.shader.name.StartsWith("GUI")||m.shader.name.StartsWith("Nature")||m.shader.name.StartsWith("Hidden/TerrainEngine")))why="non-URP shader "+m.shader.name;
     if(why!=null){bad++;sb.AppendLine($"{why} | slot {i} of {ms.Length} | {PathOf(r.transform)} | active {r.gameObject.activeInHierarchy} enabled {r.enabled}");}}}
   if(county.LateRoad)county.LateRoad.SetActive(lateWas);sb.Insert(0,$"{bad} bad material slots\n");File.WriteAllText(Path.Combine(Work,"Audit","mats23.txt"),sb.ToString());}
 }
}
