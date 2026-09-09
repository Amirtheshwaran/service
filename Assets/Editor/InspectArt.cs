using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
namespace ServiceGameV2.Editor {
public static class InspectArt {
public static void Props(){
 Directory.CreateDirectory(Dir);EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");
 var s=Object.FindFirstObjectByType<CountyScene>();var cam=s.View;cam.transform.SetParent(null);cam.cullingMask=~0;
 var bench=s.Properties[0].GetComponentsInChildren<Transform>().First(t=>t.name=="Prop_ParkBench_A");
 var origin=bench.rotation;
 for(int i=0;i<4;i++){bench.rotation=origin*Quaternion.Euler(0,90*i,0);var b=Bounds(bench.gameObject);cam.transform.position=b.center+s.Properties[0].Door.forward*3+Vector3.up*.6f;cam.transform.LookAt(b.center);Shot(cam,"bench-turn-"+i);}
 foreach(var n in new[]{"Prop_Lamp_B","Prop_Lamp_C","Prop_Lamp_D","Prop_Lamp_E"}){
 var path=AssetDatabase.FindAssets(n+" t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(x=>Path.GetFileNameWithoutExtension(x)==n);var o=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));o.transform.SetPositionAndRotation(new Vector3(0,30,0),Quaternion.identity);typeof(ServiceBuild).GetMethod("Convert",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{o});var b=Bounds(o);cam.transform.position=b.center+new Vector3(2,1,3);cam.transform.LookAt(b.center);Shot(cam,n);Object.DestroyImmediate(o);}
}
static string Dir=>Path.GetFullPath(Application.dataPath+"/../../ArtInspection");
static Bounds Bounds(GameObject o){var rr=o.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
public static void Run(){
 Directory.CreateDirectory(Dir);
 var src=EditorSceneManager.OpenScene("Assets/Flooded_Grounds/Scenes/PreAsembeld_Buildings.unity");
 var list=new List<GameObject>();
 foreach(var n in new[]{"Pref_Cabin1_A","Pref_Cabin2_A","Pref_Villa1_A","Pref_Villa2_A","Pref_IndBuilding1_A"}){
 var o=Object.Instantiate(src.GetRootGameObjects().First(x=>x.name==n));o.name=n;o.transform.position=Vector3.zero;list.Add(o);}
 foreach(var n in new[]{"Prop_Car_A","Prop_ParkBench_A","Cabin1_Door_A","Cabin2_Door_A"}){var path=AssetDatabase.FindAssets(n+" t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(x=>Path.GetFileNameWithoutExtension(x)==n);var o=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));o.name=n;o.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);list.Add(o);}
 foreach(var o in list)Object.DontDestroyOnLoad(o);
 var inspect=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
 // Move inspection copies into the new scene without relying on play-mode persistence.
 foreach(var o in list)if(o)UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(o,inspect);
}
public static void Saved(){
 Directory.CreateDirectory(Dir);
 var sc=EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");
 var cam=Object.FindFirstObjectByType<Camera>();cam.transform.SetParent(null);cam.cullingMask=~0;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.22f,.24f,.27f);
 foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.type==LightType.Directional)l.intensity=1.5f;
 var targets=new[]{"Prop_Car_A","Prop_ParkBench_A","Pref_Cabin1_A","Pref_Cabin2_A"};
 foreach(var n in targets){var o=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name==n).gameObject;var b=Bounds(o);for(int i=0;i<4;i++){var dir=Quaternion.Euler(0,i*90,0)*Vector3.forward;cam.transform.position=b.center+dir*b.size.magnitude*.9f+Vector3.up*b.size.magnitude*.17f;cam.transform.LookAt(b.center);Shot(cam,n+"-"+i);}}
 var details=new List<string>();foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.Contains("Door")||t.name.Contains("Cabin")))details.Add(t.name+" world "+t.position+" rot "+t.eulerAngles);
 File.WriteAllLines(Dir+"/saved.txt",details);
}
public static void Buildings(){
 Directory.CreateDirectory(Dir);var src=EditorSceneManager.OpenScene("Assets/Flooded_Grounds/Scenes/PreAsembeld_Buildings.unity");
 RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;RenderSettings.fog=false;
 var light=new GameObject("Inspection sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-30,0);
 var cam=new GameObject("Inspection camera").AddComponent<Camera>();cam.nearClipPlane=.03f;cam.farClipPlane=500;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.gray;
 var info=new List<string>();
 foreach(var root in src.GetRootGameObjects())if(root!=cam.gameObject&&root!=light.gameObject)root.SetActive(false);
 foreach(var n in new[]{"Pref_Cabin1_A","Pref_Cabin2_A","Pref_Villa1_A","Pref_Villa2_A","Pref_IndBuilding1_A"}){
 var o=src.GetRootGameObjects().First(x=>x.name==n);o.SetActive(true);typeof(ServiceBuild).GetMethod("Convert",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{o});
 var b=Bounds(o);foreach(Transform t in o.transform)info.Add(n+" / "+t.name+" local "+t.localPosition+" rot "+t.localEulerAngles+" "+Bounds(t.gameObject));
 for(int i=0;i<4;i++){var dir=Quaternion.Euler(0,i*90,0)*Vector3.forward;cam.transform.position=b.center+dir*b.size.magnitude*.85f+Vector3.up*b.size.magnitude*.12f;cam.transform.LookAt(b.center);Shot(cam,n+"-"+i);}
 Physics.SyncTransforms();
 if(n.Contains("Cabin")){foreach(float x in new[]{1.5f,2.5f,3.5f,4.5f,5.5f})foreach(float y in new[]{1.2f,2f,3f}){var origin=o.transform.TransformPoint(new Vector3(x,y,15));foreach(var hit in Physics.RaycastAll(origin,-o.transform.forward,30))info.Add("PROBE "+n+" x "+x+" y "+y+" hit "+o.transform.InverseTransformPoint(hit.point)+" "+hit.collider.name);}}
 o.SetActive(false);}
 File.WriteAllLines(Dir+"/buildings.txt",info);
}
static void Shot(Camera c,string name){var rt=new RenderTexture(1100,800,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;var t=new Texture2D(1100,800,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1100,800),0,0);t.Apply();File.WriteAllBytes(Dir+"/"+name+".png",t.EncodeToPNG());c.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(t);Object.DestroyImmediate(rt);}
}}
