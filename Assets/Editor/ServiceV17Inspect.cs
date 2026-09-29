using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // Compact scene summary for the V17 pass. Read-only.
 public static class ServiceV17Inspect {
  static string P(Vector3 v)=>$"({v.x:F1},{v.y:F1},{v.z:F1})";
  static string Path(Transform t)=>t?(t.parent?Path(t.parent)+"/"+t.name:t.name):"-";
  public static void Run(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");
   var s=new StringBuilder();
   foreach(var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None)){
    var d=t.terrainData;
    s.AppendLine($"TERRAIN {t.name} pos{P(t.transform.position)} size{P(d.size)} trees={d.treeInstanceCount} layers=[{string.Join(",",d.terrainLayers.Select(l=>l?l.name:"null"))}] details={d.detailPrototypes.Length} heightRes={d.heightmapResolution}");
    s.AppendLine("  treeProtos: "+string.Join(", ",d.treePrototypes.Select(p=>p.prefab?p.prefab.name:"null")));
   }
   s.AppendLine("ROOTS:");
   foreach(var r in EditorSceneManager.GetActiveScene().GetRootGameObjects())s.AppendLine($"  {r.name} children={r.transform.childCount} total={r.GetComponentsInChildren<Transform>(true).Length}");
   var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
   foreach(var key in new[]{"conifer","pine","tree","fir","spruce"}){
    var hits=all.Where(t=>t.name.ToLower().Contains(key)&&t.GetComponent<MeshRenderer>()==null&&t.GetComponent<LODGroup>()!=null||t.name.ToLower().Contains(key)&&t.parent&&!t.parent.name.ToLower().Contains(key)).ToArray();
    s.AppendLine($"NAME~{key}: {hits.Length} top-level; parents: "+string.Join(", ",hits.GroupBy(h=>h.parent?h.parent.name:"<root>").OrderByDescending(g=>g.Count()).Take(6).Select(g=>g.Key+"x"+g.Count())));
   }
   var scene=Object.FindAnyObjectByType<CountyScene>();
   foreach(var p in scene.Properties){
    s.AppendLine($"PROP {p.Index} {p.Address} enc={p.Encounter} building={Path(p.Building)} bpos={P(p.Building?p.Building.position:Vector3.zero)}");
    s.AppendLine($"   door={Path(p.Door)} panel={Path(p.DoorPanel)} knock={(p.KnockPoint?P(p.KnockPoint.position):"-")} notice={(p.NoticePoint?P(p.NoticePoint.position):"-")} delivery={(p.DeliveryPoint?P(p.DeliveryPoint.position):"-")} gate={(p.Gate?P(p.Gate.position):"-")}");
    s.AppendLine($"   instr='{p.Instructions}' notice='{p.NoticeText?.Replace("\n"," | ")}'");
   }
   var res=Object.FindAnyObjectByType<ServiceResidents>();
   if(res)s.AppendLine($"RESIDENTS correll={Path(res.Correll?res.Correll.transform:null)} {P(res.Correll?res.Correll.transform.position:Vector3.zero)} bell={Path(res.Bell?res.Bell.transform:null)} {P(res.Bell?res.Bell.transform.position:Vector3.zero)} worker={Path(res.DepotWorker)} from{P(res.WalkFrom)} to{P(res.WalkTo)}");
   foreach(var t in all.Where(t=>t.name.ToLower().Contains("dog")||t.name.ToLower().Contains("shepherd")||t.name.ToLower().Contains("notice")||t.name.ToLower().Contains("hands")).Where(t=>t.parent==null||!(t.parent.name.ToLower().Contains("dog")||t.parent.name.ToLower().Contains("shepherd")||t.parent.name.ToLower().Contains("notice")||t.parent.name.ToLower().Contains("hands"))).Take(30))
    s.AppendLine($"OBJ {Path(t)} {P(t.position)} active={t.gameObject.activeInHierarchy}");
   s.AppendLine($"SKY {(RenderSettings.skybox?RenderSettings.skybox.name+" / "+RenderSettings.skybox.shader.name:"none")} fog={RenderSettings.fog} {RenderSettings.fogMode} dens={RenderSettings.fogDensity} start={RenderSettings.fogStartDistance} end={RenderSettings.fogEndDistance} col={RenderSettings.fogColor} ambient={RenderSettings.ambientMode} {RenderSettings.ambientLight}");
   foreach(var v in Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None))s.AppendLine($"VOLUME {Path(v.transform)} global={v.isGlobal} profile={(v.sharedProfile?v.sharedProfile.name+": "+string.Join(",",v.sharedProfile.components.Select(c=>c.GetType().Name)):"none")}");
   File.WriteAllText(System.IO.Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"v17-inspect.txt"),s.ToString());
  }
 }
}
