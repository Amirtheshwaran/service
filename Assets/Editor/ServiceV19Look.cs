using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace ServiceGameV2.Editor {
 // V19 camera look: low internal resolution with point upscaling and the Service/Camcorder full-screen pass
 // (grain, ordered dither, lifted blue-grey shadows, vignette, fringing), plus bloom on practical lights.
 public static partial class ServiceV19Rebuild {
  public static void Look(){
   var shader=Shader.Find("Service/Camcorder");if(!shader)throw new System.Exception("Service/Camcorder shader missing");
   var matPath=$"{V19}/V19 Camcorder.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,matPath);}mat.shader=shader;EditorUtility.SetDirty(mat);
   foreach(var path in new[]{"Assets/Settings/PC_Renderer.asset","Assets/Settings/Mobile_Renderer.asset"}){
    var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);if(!data)continue;
    var existing=data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f=>f.name=="V19 Camcorder");
    if(!existing){
     existing=ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();existing.name="V19 Camcorder";AssetDatabase.AddObjectToAsset(existing,data);
     AssetDatabase.TryGetGUIDAndLocalFileIdentifier(existing,out string _,out long id);
     var so=new SerializedObject(data);var list=so.FindProperty("m_RendererFeatures");var map=so.FindProperty("m_RendererFeatureMap");
     list.arraySize++;list.GetArrayElementAtIndex(list.arraySize-1).objectReferenceValue=existing;map.arraySize++;map.GetArrayElementAtIndex(map.arraySize-1).longValue=id;so.ApplyModifiedPropertiesWithoutUndo();
    }
    existing.injectionPoint=FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;existing.passMaterial=mat;existing.passIndex=0;existing.fetchColorBuffer=true;existing.requirements=ScriptableRenderPassInput.Color;existing.SetActive(true);
    EditorUtility.SetDirty(existing);EditorUtility.SetDirty(data);log.AppendLine("LOOK camcorder pass on "+path);
   }
   foreach(var path in new[]{"Assets/Settings/PC_RPAsset.asset","Assets/Settings/Mobile_RPAsset.asset"}){var a=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);if(!a)continue;a.upscalingFilter=UpscalingFilterSelection.Point;a.renderScale=.42f;EditorUtility.SetDirty(a);log.AppendLine("LOOK point upscaling on "+path);}
   var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/ServiceArt/County grade.asset");
   if(profile){
    if(profile.TryGet<MotionBlur>(out var mb))mb.active=false;
    if(profile.TryGet<FilmGrain>(out var fg))fg.active=false;
    if(profile.TryGet<ChromaticAberration>(out var ca))ca.active=false;
    if(profile.TryGet<Vignette>(out var vg))vg.active=false;
    if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>(true);
    bloom.active=true;bloom.threshold.Override(.82f);bloom.intensity.Override(1.1f);bloom.scatter.Override(.78f);bloom.tint.Override(new Color(1f,.9f,.78f));
    if(profile.TryGet<ColorAdjustments>(out var cadj)){cadj.postExposure.Override(.1f);cadj.contrast.Override(6);cadj.saturation.Override(-12);}
    if(profile.TryGet<Tonemapping>(out var tm))tm.mode.Override(TonemappingMode.Neutral);
    EditorUtility.SetDirty(profile);log.AppendLine("LOOK volume: bloom on practicals, grain/blur/vignette handed to the camcorder pass");
   }
   AssetDatabase.SaveAssets();System.IO.File.AppendAllText(System.IO.Path.Combine(Work,"v19-rebuild.txt"),"==== look\n"+log);log.Clear();
  }
 }
}
