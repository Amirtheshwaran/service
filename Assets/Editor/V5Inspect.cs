using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ServiceGameV2.Editor {
public static class V5Inspect {
 public static void Run(){
  var src=EditorSceneManager.OpenScene("Assets/Flooded_Grounds/Scenes/PreAsembeld_Buildings.unity",OpenSceneMode.Additive);
  var report=new StringBuilder();
  foreach(var n in new[]{"Pref_Cabin1_A","Pref_Cabin2_A","Pref_BrickHouse_A","Pref_Villa2_A"}){
   var o=src.GetRootGameObjects().First(g=>g.name==n);report.AppendLine(n);
   foreach(var t in o.GetComponentsInChildren<Transform>())if(t.name.ToLower().Contains("door")||t.name.ToLower().Contains("stairs")){
    var rr=t.GetComponentsInChildren<Renderer>();var b=rr.Length>0?rr[0].bounds:new Bounds(t.position,Vector3.zero);foreach(var r in rr)b.Encapsulate(r.bounds);
    report.AppendLine(t.name+" pos="+o.transform.InverseTransformPoint(t.position)+" center="+o.transform.InverseTransformPoint(b.center)+" size="+b.size+" yaw="+t.eulerAngles.y);
   }
  }
  File.WriteAllText(Path.GetFullPath(Application.dataPath+"/../../v5-architecture.txt"),report.ToString());
  EditorSceneManager.CloseScene(src,true);
 }
}}
