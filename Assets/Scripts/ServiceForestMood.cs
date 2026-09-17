using UnityEngine;
using UnityEngine.Rendering;

namespace ServiceGameV2 {
 public static class ServiceForestMood {
  public static void Apply(CountyScene scene,int night){
   var mist=new Color(.62f,.60f,.65f);
   RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
   RenderSettings.fogDensity=night==0?.025f:night==1?.028f:.032f;RenderSettings.fogColor=mist;
   RenderSettings.ambientMode=AmbientMode.Custom;
   var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(.13f,.135f,.15f));RenderSettings.ambientProbe=probe;
   if(scene.Moon){scene.Moon.color=new Color(.73f,.75f,.88f);scene.Moon.intensity=night==0?.38f:.30f;scene.Moon.transform.rotation=Quaternion.Euler(18-night*3,-28,0);}
   if(scene.View){scene.View.clearFlags=CameraClearFlags.SolidColor;scene.View.backgroundColor=mist;scene.View.farClipPlane=175;}
  }
 }
}
