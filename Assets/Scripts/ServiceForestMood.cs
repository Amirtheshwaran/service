using UnityEngine;
using UnityEngine.Rendering;

namespace ServiceGameV2 {
 public static class ServiceForestMood {
  public static void Apply(CountyScene scene,int night){
   // V17: Fears to Fathom night - near-black blue fog so the pines fall into silhouette beyond the lights.
   var mist=new Color(.075f,.083f,.1f);
   RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
   RenderSettings.fogDensity=night==0?.028f:night==1?.031f:.035f;RenderSettings.fogColor=mist;
   RenderSettings.ambientMode=AmbientMode.Custom;
   var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.125f,.13f,.15f));RenderSettings.ambientProbe=probe;
   if(scene.Moon){scene.Moon.color=new Color(.64f,.69f,.86f);scene.Moon.intensity=night==0?.42f:.34f;scene.Moon.transform.rotation=Quaternion.Euler(18-night*3,-28,0);}
   if(scene.View){scene.View.clearFlags=CameraClearFlags.SolidColor;scene.View.backgroundColor=mist;scene.View.farClipPlane=175;}
  }
 }
}
