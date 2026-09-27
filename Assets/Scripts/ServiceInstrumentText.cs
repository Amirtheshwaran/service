using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceInstrumentText:MonoBehaviour {
  public Material SurfaceMaterial;
  Material runtime;
  Font face;
  void Start(){
   var labels=GetComponentsInChildren<TextMesh>();
   if(!SurfaceMaterial||labels.Length==0)return;
   face=labels[0].font;runtime=new Material(SurfaceMaterial);
   foreach(var label in labels)label.GetComponent<Renderer>().sharedMaterial=runtime;
   Font.textureRebuilt+=Rebuilt;Rebuilt(face);
  }
  void Rebuilt(Font font){if(runtime&&font==face)runtime.mainTexture=font.material.mainTexture;}
  void OnDestroy(){Font.textureRebuilt-=Rebuilt;if(runtime)Destroy(runtime);}
 }
}
