using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace ServiceGameV2.Editor {
 public static partial class ServiceBuild {
  public static void RefreshReferenceLighting(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");scene=Object.FindAnyObjectByType<CountyScene>();ServiceForestMood.Apply(scene,0);
   EditorSceneManager.MarkSceneDirty(scene.gameObject.scene);if(!EditorSceneManager.SaveScene(scene.gameObject.scene))throw new Exception("Could not save lighting");AssetDatabase.SaveAssets();ServiceQuickBuild.Build();
  }
  static readonly Dictionary<Material,Material> winterMaterials=new Dictionary<Material,Material>();
  static Material WinterMaterial(Material source){
   if(winterMaterials.TryGetValue(source,out var result))return result;
   AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);
   string path=Art+"Materials/Winter "+guid+"_"+id+".mat";
   result=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!result){result=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(result,path);}
   result.SetTexture("_BaseMap",source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null);
   result.SetColor("_BaseColor",new Color(.43f,.44f,.48f));result.SetFloat("_Smoothness",.06f);result.enableInstancing=true;
   if(source.name.ToLowerInvariant().Contains("leaf")){result.SetFloat("_AlphaClip",1);result.SetFloat("_Cutoff",.42f);result.SetFloat("_Cull",0);result.EnableKeyword("_ALPHATEST_ON");result.renderQueue=2450;}
   EditorUtility.SetDirty(result);winterMaterials[source]=result;return result;
  }
  public static void BuildReferenceLook(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");scene=Object.FindAnyObjectByType<CountyScene>();world=scene.transform;
   route=Curve(scene.Route.Select(p=>p.position).ToArray());
   var forest=world.Find("Conifer forest — canopy, clearings and ground cover");if(!forest)forest=world.Find("Winter woodland — bare trunks and mist");
   if(!forest)throw new Exception("Forest canopy is missing");
   var trees=forest.GetComponentsInChildren<CapsuleCollider>().Where(c=>c.transform.parent==forest).ToArray();
   var rng=new System.Random(80317);float R(float lo,float hi)=>(float)(lo+rng.NextDouble()*(hi-lo));
   int count=0;
   foreach(var trunk in trees){
    var at=trunk.transform.TransformPoint(trunk.center)-Vector3.up*4;at.y=LandHeight(at.x,at.z);
    Object.DestroyImmediate(trunk.gameObject);
    // Authored dead hardwoods dominate; bare conifers break up the silhouette.
    bool hardwood=count%5!=4;string path=hardwood?FG+"Prefabs/Nature/Trees/TreeCreator_Tall_C_Dead.prefab":Conifers+"PF Conifer Bare BOTD URP.prefab";
    var tree=Sourced(path,at,R(0,360),R(19,29),forest);
    if(hardwood){tree.transform.localScale=Vector3.Scale(tree.transform.localScale,V(R(1.15f,1.75f),1,R(1.15f,1.75f)));foreach(var renderer in tree.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(WinterMaterial).ToArray();}
    foreach(var c in tree.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
    var collider=tree.AddComponent<CapsuleCollider>();collider.radius=.48f/Mathf.Abs(tree.transform.lossyScale.x);collider.height=8/tree.transform.lossyScale.y;collider.center=tree.transform.InverseTransformPoint(at+Vector3.up*4);
    count++;
   }
   forest.name="Winter woodland — bare trunks and mist";
   ServiceForestMood.Apply(scene,0);
   var volume=world.GetComponentInChildren<Volume>();var profile=volume.sharedProfile;
   if(profile.TryGet<ColorAdjustments>(out var grade)){grade.postExposure.Override(-.05f);grade.contrast.Override(14);grade.saturation.Override(-38);EditorUtility.SetDirty(grade);}
   if(profile.TryGet<Vignette>(out var vignette)){vignette.intensity.Override(.19f);vignette.smoothness.Override(.65f);EditorUtility.SetDirty(vignette);}
   var terrain=world.GetComponentInChildren<Terrain>();foreach(var layer in terrain.terrainData.terrainLayers){layer.tileSize=new Vector2(2.8f,2.8f);layer.normalScale=.55f;EditorUtility.SetDirty(layer);}
   // Narrow the oversized bare corridor visually without adding collision to the walking route.
   foreach(var p in scene.Properties){var path=world.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.name=="Garden path "+p.Index);if(path&&path.sharedMaterial){path.sharedMaterial.SetColor("_BaseColor",new Color(.40f,.40f,.43f));path.sharedMaterial.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(path.sharedMaterial);}}
   EditorUtility.SetDirty(profile);BakeNavigation();EditorSceneManager.MarkSceneDirty(scene.gameObject.scene);
   if(!EditorSceneManager.SaveScene(scene.gameObject.scene))throw new Exception("Could not save reference forest");AssetDatabase.SaveAssets();
   Debug.Log("SERVICE_REFERENCE_LOOK: "+count+" trees; fog "+RenderSettings.fogDensity+"; image 3 palette");
   ServiceQuickBuild.Build();
  }
 }
}
