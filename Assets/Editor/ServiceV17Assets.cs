using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V17: import licensed downloads and report their structure.
 public static class ServiceV17Assets {
  public const string Root="Assets/ServiceArt/V17";
  static readonly string[] Humans={"Characters/JustMan/JustMan.fbx","Characters/ElderlyMan/ElderlyMan.fbx","Characters/OldFatMan/OldFatMan.fbx"};
  public static void ImportAndReport(){
   var dog=Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"Sources/V17/raw/RSG_DogsPack_GermanShepherd-Unity/RSG_DogsPack_GermanShepherd.unitypackage");
   if(!AssetDatabase.FindAssets("GermanShepherd t:Model").Any()&&File.Exists(dog))AssetDatabase.ImportPackage(dog,false);
   AssetDatabase.Refresh();
   foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root})){
    var path=AssetDatabase.GUIDToAssetPath(guid);var ti=(TextureImporter)AssetImporter.GetAtPath(path);var n=Path.GetFileNameWithoutExtension(path).ToLower();
    bool normal=n.Contains("normal")||n.EndsWith("_nm")||n.EndsWith("-n")||n.Contains("nor_gl")||n.Contains("normalgl");
    bool changed=false;
    if(normal&&ti.textureType!=TextureImporterType.NormalMap){ti.textureType=TextureImporterType.NormalMap;changed=true;}
    if(ti.maxTextureSize>2048){ti.maxTextureSize=2048;changed=true;}
    if(changed)ti.SaveAndReimport();
   }
   foreach(var h in Humans.Concat(new[]{"Anim/Stand--Idle.anim.fbx","Anim/Locomotion--Walk_N.anim.fbx"})){var mi=(ModelImporter)AssetImporter.GetAtPath(Root+"/"+h);if(mi&&(mi.animationType!=ModelImporterAnimationType.Human||mi.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel)){mi.animationType=ModelImporterAnimationType.Human;mi.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;mi.SaveAndReimport();}}
   Report();
  }
  public static void Report(){
   var s=new StringBuilder();
   var models=AssetDatabase.FindAssets("t:Model",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.Contains("/Characters/")||p.Contains("/Dog/")||p.Contains("/Anim/"));
   foreach(var path in models){
    var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!go)continue;
    foreach(var r in go.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials.Where(m=>m))s.AppendLine($"  R {r.name}: {m.name} [{m.shader.name}] base={(m.HasProperty("_BaseMap")&&m.GetTexture("_BaseMap")?m.GetTexture("_BaseMap").name:"-")} nrm={(m.HasProperty("_BumpMap")&&m.GetTexture("_BumpMap")?m.GetTexture("_BumpMap").name:"-")}");
    var rs=go.GetComponentsInChildren<Renderer>(true);var b=new Bounds();bool first=true;foreach(var r in rs){if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}
    s.AppendLine($"MODEL {path} renderers={rs.Length} size=({b.size.x:F2},{b.size.y:F2},{b.size.z:F2}) center=({b.center.x:F2},{b.center.y:F2},{b.center.z:F2})");
    s.AppendLine("  children: "+string.Join(", ",Enumerable.Range(0,go.transform.childCount).Select(i=>go.transform.GetChild(i).name).Take(40)));
    s.AppendLine("  materials: "+string.Join(", ",rs.SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct()));
    var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview"));
    if(clips.Any())s.AppendLine("  clips: "+string.Join(", ",clips.Select(c=>$"{c.name}({c.length:F1}s)")));
    var av=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();if(av)s.AppendLine($"  avatar human={av.isHuman} valid={av.isValid}");
   }
   foreach(var p in AssetDatabase.FindAssets("t:Prefab GermanShepherd").Select(AssetDatabase.GUIDToAssetPath))s.AppendLine("DOGPREFAB "+p);
   foreach(var p in AssetDatabase.FindAssets("t:AnimatorController",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.ToLower().Contains("shepherd")||p.ToLower().Contains("dog")))s.AppendLine("DOGCTRL "+p);
   File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"v17-assets.txt"),s.ToString());
  }
 }
}
