using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ServiceGameV2.Editor {
 public static class ServiceV15FinalCheck {
  public static void FitAndBuild(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=Object.FindAnyObjectByType<CountyScene>();
   var road=s.GetComponentInChildren<ServiceReturnRoad>();var sign=road.transform.Find("sign-highway");sign.rotation=Quaternion.Euler(0,90,0);
   var rs=sign.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
   var words=road.transform.Find("Depot direction lettering");words.position=new Vector3(b.center.x,b.max.y-.25f,b.min.z-.015f);
   var palette=AssetDatabase.LoadAssetAtPath<Material>("Assets/External/KenneyRoads/Source palette.mat");if(!palette){palette=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(palette,"Assets/External/KenneyRoads/Source palette.mat");}
   palette.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/External/KenneyRoads/colormap.png"));palette.color=new Color(.62f,.62f,.62f);palette.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(palette);
   foreach(var tr in s.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="construction-barrier"||t.name=="sign-highway"))foreach(var r in tr.GetComponentsInChildren<Renderer>())r.sharedMaterial=palette;
   var meshes=AssetDatabase.LoadAllAssetsAtPath("Assets/External/FloodedGroundsOpenBed/CountyTruck.fbx").OfType<Mesh>().ToDictionary(m=>m.name);
   foreach(var f in s.Car.Find("Prop_Car_A").GetComponentsInChildren<MeshFilter>(true)){
    var old=f.sharedMesh;var next=meshes[old.name];Debug.Log("V15_BED "+old.name+" old="+old.bounds+" new="+next.bounds);
    if(Vector3.Distance(old.bounds.size,next.bounds.size)>.01f)throw new System.Exception("Imported truck units differ; preserve source placement");
    f.sharedMesh=next;var c=f.GetComponent<MeshCollider>();if(c)c.sharedMesh=next;
    foreach(var m in f.GetComponent<Renderer>().sharedMaterials)if(m.HasProperty("_Cull")){m.SetFloat("_Cull",0);EditorUtility.SetDirty(m);}
   }
   EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();ServiceQuickBuild.Build();
  }
  public static void Inspect(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=Object.FindAnyObjectByType<CountyScene>();
   var dog=s.transform.Find("Correll yard dog");var t=s.GetComponentInChildren<Terrain>();
   var lines=new System.Collections.Generic.List<string>{"dog="+dog.position+" ground="+(t.SampleHeight(dog.position)+t.transform.position.y)};
   foreach(var r in dog.GetComponentsInChildren<Renderer>(true))lines.Add(r.name+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" layer="+r.gameObject.layer+" shader="+r.sharedMaterial.shader.name+" bounds="+r.bounds);
   File.WriteAllLines(Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"v15-final-inspect.txt"),lines);
  }
 }
}
