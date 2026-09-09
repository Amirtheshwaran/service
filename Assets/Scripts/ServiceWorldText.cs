using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceWorldText:MonoBehaviour {
  void OnEnable(){Font.textureRebuilt+=Refresh;Refresh(null);}
  void OnDisable(){Font.textureRebuilt-=Refresh;}
  public static void Refresh(Font font){foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(text.font&&(font==null||font==text.font)){var mat=text.GetComponent<Renderer>().sharedMaterial;if(mat&&mat.shader.name=="Service/World Type")mat.mainTexture=text.font.material.mainTexture;}}
 }
}
