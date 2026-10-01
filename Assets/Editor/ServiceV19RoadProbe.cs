using System.IO;
using System.Linq;
using System.Text;
using EasyRoads3Dv3;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static class ServiceV19RoadProbe {
  public static void Probe(){
   var sb=new StringBuilder();
   try{
    EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");
    var terrain=Object.FindAnyObjectByType<Terrain>();
    var net=new ERRoadNetwork();sb.AppendLine("network ok version="+net.Version());
    var type=new ERRoadType();type.roadWidth=6;type.roadMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/EasyRoads3D/Resources/Materials/roads/road material.mat");type.hasMeshCollider=true;
    var pts=new[]{new Vector3(0,0,-6),new Vector3(0,0,30),new Vector3(4,0,70)}.Select(p=>new Vector3(p.x,terrain.SampleHeight(p)+terrain.transform.position.y,p.z)).ToArray();
    var road=net.CreateRoad("probe road",type,pts);sb.AppendLine("road ok markers="+road.GetMarkerCount()+" width="+road.GetWidth());
    int e=0;var mid=road.GetPosition(20,ref e);sb.AppendLine("pos at 20m="+mid);
    foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.sharedMesh&&m.transform.root.name.Contains("Road Network")))sb.AppendLine("mesh "+mf.name+" verts="+mf.sharedMesh.vertexCount+" bounds="+mf.sharedMesh.bounds);
    net.BuildRoadNetwork(false,false,false,false,terrain);sb.AppendLine("build ok");
    foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(m=>m.sharedMesh&&m.transform.root.name.Contains("Road Network")))sb.AppendLine("built mesh "+m(mf)+" verts="+mf.sharedMesh.vertexCount+" bounds="+mf.GetComponent<Renderer>().bounds);
    foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g=>g.name.Contains("Road")))sb.AppendLine("root "+root.name+" children="+root.transform.childCount+" : "+string.Join(", ",root.GetComponentsInChildren<Transform>().Take(12).Select(t=>t.name)));
   }catch(System.Exception ex){sb.AppendLine("EXCEPTION "+ex);}
   File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"Audit","road-probe.txt"),sb.ToString());
  }
  static string m(MeshFilter f){var s=f.name;var t=f.transform;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
 }
}
