using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V19 audit: dump the whole county scene so every object can be accounted for.
 public static class ServiceV19Inventory {
  static string P(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s.Replace("\n"," ");}
  static string V(Vector3 v)=>$"({v.x:F2},{v.y:F2},{v.z:F2})";
  public static void Run(){
   var scene=EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");
   var outDir=Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"Audit");Directory.CreateDirectory(outDir);
   var roots=scene.GetRootGameObjects();
   // 1. Tree outline (depth 4) with child/renderer counts.
   var tree=new StringBuilder();
   void Walk(Transform t,int depth){int rc=t.GetComponentsInChildren<Renderer>(true).Length;var src=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
    var comps=string.Join(",",t.GetComponents<Component>().Where(c=>c&&!(c is Transform)).Select(c=>c.GetType().Name).Take(6));
    tree.AppendLine($"{new string(' ',depth*2)}{t.name.Replace("\n"," ")} [{(t.gameObject.activeSelf?"on":"OFF")}] pos{V(t.position)} kids={t.childCount} rend={rc} {comps} {(string.IsNullOrEmpty(src)?"":"prefab="+src)}");
    if(depth<4&&t.childCount<=60)foreach(Transform c in t)Walk(c,depth+1);else if(depth<4)tree.AppendLine($"{new string(' ',(depth+1)*2)}... {t.childCount} children (names: {string.Join(" | ",t.Cast<Transform>().Select(c=>c.name).GroupBy(n=>System.Text.RegularExpressions.Regex.Replace(n,@"[\s_\-]*\(?\d+\)?$","")).Select(g=>g.Key+" x"+g.Count()).Take(25))})");}
   foreach(var r in roots)Walk(r.transform,0);
   File.WriteAllText(Path.Combine(outDir,"scene-tree.txt"),tree.ToString());
   // 2. Every renderer: path, bounds, mesh, materials, prefab source.
   var rows=new StringBuilder();rows.AppendLine("path\tactiveInHierarchy\tcenter\tsize\tmesh\tmaterials\tprefab");
   foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))){
    var mf=r.GetComponent<MeshFilter>();var mesh=mf&&mf.sharedMesh?mf.sharedMesh.name:(r is SkinnedMeshRenderer s&&s.sharedMesh?s.sharedMesh.name:r.GetType().Name);
    var src=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(r.gameObject);
    rows.AppendLine($"{P(r.transform)}\t{r.gameObject.activeInHierarchy}\t{V(r.bounds.center)}\t{V(r.bounds.size)}\t{mesh}\t{string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name))}\t{src}");
   }
   File.WriteAllText(Path.Combine(outDir,"renderers.tsv"),rows.ToString());
   // 3. Properties and scene references.
   var props=new StringBuilder();var county=Object.FindAnyObjectByType<CountyScene>();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){
    props.AppendLine($"== {p.Index} {p.Address} ({p.name}) encounter={p.Encounter} variant={p.CreatureVariant} template={p.Template}");
    foreach(var f in typeof(ServiceProperty).GetFields()){var v=f.GetValue(p);string s=v is Transform tr&&tr?P(tr)+" @"+V(tr.position)+" rotY="+tr.eulerAngles.y.ToString("F0"):v is Component c&&c?P(c.transform):v is System.Array a?$"[{a.Length}] "+string.Join("; ",a.Cast<object>().Select(o=>o is Component cc&&cc?cc.name:o?.ToString())):v is string str?str.Replace("\n","\\n"):v?.ToString();props.AppendLine($"  {f.Name} = {s}");}
   }
   props.AppendLine("== CountyScene");foreach(var f in typeof(CountyScene).GetFields()){var v=f.GetValue(county);props.AppendLine($"  {f.Name} = {(v is Component c&&c?P(c.transform)+" @"+V(c.transform.position):v is GameObject g&&g?P(g.transform)+" @"+V(g.transform.position)+(g.activeSelf?"":" OFF"):v is System.Array a?"["+a.Length+"] "+string.Join("; ",a.Cast<object>().Select(o=>o is Component cc&&cc?cc.name+V(cc.transform.position):o is GameObject gg&&gg?gg.name:o?.ToString())):v?.ToString())}");}
   File.WriteAllText(Path.Combine(outDir,"properties.txt"),props.ToString());
   // 4. Text meshes (all signage) and lights.
   var misc=new StringBuilder();
   foreach(var tm in roots.SelectMany(g=>g.GetComponentsInChildren<TextMesh>(true)))misc.AppendLine($"TEXT {P(tm.transform)} active={tm.gameObject.activeInHierarchy} pos{V(tm.transform.position)} fwd{V(tm.transform.forward)} size={tm.characterSize} font={(tm.font?tm.font.name:"-")} mat={tm.GetComponent<Renderer>().sharedMaterial?.name}/{tm.GetComponent<Renderer>().sharedMaterial?.shader?.name} text=[{tm.text.Replace("\n","\\n")}]");
   foreach(var l in roots.SelectMany(g=>g.GetComponentsInChildren<Light>(true)))misc.AppendLine($"LIGHT {P(l.transform)} {l.type} on={l.enabled&&l.gameObject.activeInHierarchy} pos{V(l.transform.position)} int={l.intensity:F1} range={l.range:F1} color={l.color}");
   foreach(var t in roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)))misc.AppendLine($"TERRAIN {P(t.transform)} pos{V(t.transform.position)} size{V(t.terrainData.size)} layers={string.Join(",",t.terrainData.terrainLayers.Select(l=>l?l.name+"/"+l.tileSize:"null"))} trees={t.terrainData.treeInstanceCount} protos={t.terrainData.treePrototypes.Length} details={t.terrainData.detailPrototypes.Length}");
   File.WriteAllText(Path.Combine(outDir,"text-lights-terrain.txt"),misc.ToString());
   // 5. Render pipeline and post-processing state.
   var rp=new StringBuilder();var urp=UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;rp.AppendLine("pipeline="+(urp?AssetDatabase.GetAssetPath(urp):"none"));
   foreach(var lvl in Enumerable.Range(0,QualitySettings.names.Length)){var a=QualitySettings.GetRenderPipelineAssetAt(lvl);rp.AppendLine($"quality {lvl} {QualitySettings.names[lvl]} -> {(a?AssetDatabase.GetAssetPath(a):"default")}");}
   foreach(var v in roots.SelectMany(g=>g.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true))){rp.AppendLine($"VOLUME {P(v.transform)} global={v.isGlobal} weight={v.weight} priority={v.priority} profile={(v.sharedProfile?AssetDatabase.GetAssetPath(v.sharedProfile):"none")}");if(v.sharedProfile)foreach(var c in v.sharedProfile.components)rp.AppendLine($"   {c.GetType().Name} active={c.active} "+string.Join(" ",c.parameters.Select((pp,i)=>pp.overrideState?c.GetType().GetFields().Where(f=>typeof(UnityEngine.Rendering.VolumeParameter).IsAssignableFrom(f.FieldType)).ElementAtOrDefault(i)?.Name+"="+pp.GetType().GetProperty("value")?.GetValue(pp):null).Where(s=>s!=null)));}
   File.WriteAllText(Path.Combine(outDir,"render.txt"),rp.ToString());
  }
 }
}
