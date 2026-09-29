using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 public static class ServiceV16Audit {
  public static string Work=>Directory.GetParent(Application.dataPath).Parent.FullName;
  public static CountyScene Open(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");return Object.FindAnyObjectByType<CountyScene>();}
  public static string PathOf(Transform t)=>AnimationUtility.CalculateTransformPath(t,t.root);
  public static void Preview(){var s=Open();Preview(s,"Before");}
  public static void Openings(){var s=Open();Physics.SyncTransforms();var log=new List<string>();foreach(int index in new[]{1,2,5}){var p=s.Properties.Single(p=>p.Index==index);for(float z=p.Door.position.z-18;z<p.Door.position.z+9;z+=.2f){bool clear=true;foreach(float h in new[]{.18f,.8f,1.6f}){var from=new Vector3(p.Door.position.x-1.8f,p.Door.position.y+h,z);if(Physics.Raycast(from,Vector3.right,3f,~0,QueryTriggerInteraction.Ignore))clear=false;}if(clear)log.Add("OPEN "+index+" z="+z.ToString("F2"));}}File.WriteAllLines(Path.Combine(Work,"v16-openings.txt"),log);}
  public static void Preview(CountyScene s,string suffix){
   ServiceForestMood.Apply(s,0);s.Cockpit.SetActive(false);s.Entity.SetActive(false);var cam=s.View;cam.transform.SetParent(null);cam.nearClipPlane=.04f;cam.fieldOfView=65;cam.cullingMask=~(1<<8);if(s.Flashlight)s.Flashlight.enabled=true;
   var poses=new[]{new Vector3(-57,2,87),new Vector3(73,2.3f,150),new Vector3(88,2.3f,230.65f),new Vector3(76,4.2f,368.92f),new Vector3(-64,2,297),new Vector3(-47,1.3f,14.5f),new Vector3(-4,2,374),new Vector3(4,2,-10),new Vector3(96.5f,6,230)};
   var targets=new[]{new Vector3(-65,1.5f,91),new Vector3(79.24f,1.8f,149.98f),new Vector3(94.2f,1.8f,230.65f),new Vector3(83,3.8f,368.92f),new Vector3(-73,2,295),new Vector3(-49.13f,.35f,17.61f),new Vector3(-15,0,385),new Vector3(-8,1.8f,-5),new Vector3(96,5.6f,235)};
   string dir=Path.Combine(Work,"PreviewV16"+suffix);Directory.CreateDirectory(dir);
   for(int i=0;i<poses.Length;i++){cam.transform.position=poses[i];cam.transform.LookAt(targets[i]);Capture(cam,Path.Combine(dir,i+".png"));}
  }
  public static void Capture(Camera cam,string path){var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(rt);Object.DestroyImmediate(texture);}
  public static void Run(){
   var s=Open();var lines=new List<string>();var terrain=s.GetComponentInChildren<Terrain>();
   lines.Add("TERRAIN "+AssetDatabase.GetAssetPath(terrain.terrainData));
   foreach(Transform t in s.transform)lines.Add("ROOT "+t.name+" "+t.position);
   foreach(var p in s.Properties){
    lines.Add("PROPERTY "+p.Index+" door="+p.Door.position+" yaw="+p.Door.eulerAngles+" panel="+(p.DoorPanel?PathOf(p.DoorPanel):"NONE")+" building="+p.Building.position+" table="+p.DeliveryPoint.position+" instructions="+p.Instructions);
    foreach(var f in p.GetComponentsInChildren<MeshFilter>(true))lines.Add("PART "+p.Index+" "+PathOf(f.transform)+" bounds="+f.GetComponent<Renderer>()?.bounds+" source="+AssetDatabase.GetAssetPath(f.sharedMesh));
   }
   foreach(var group in s.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("TreeCreator_")||t.name.StartsWith("PF Conifer")).Where(t=>!t.name.Contains("Bush")).GroupBy(t=>t.name)){
    foreach(var t in group.Take(2)){
     lines.Add("TREE "+t.name+" count="+group.Count()+" position="+t.position+" scale="+t.lossyScale+" ground="+(terrain.SampleHeight(t.position)+terrain.transform.position.y));
     foreach(var f in t.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>())){float min=f.sharedMesh.vertices.Min(v=>f.transform.TransformPoint(v).y);lines.Add("MESH "+PathOf(f.transform)+" mesh="+f.sharedMesh.name+" geoMin="+min+" rendererMin="+f.GetComponent<Renderer>().bounds.min.y+" source="+AssetDatabase.GetAssetPath(f.sharedMesh));}
    }
   }
   foreach(var t in s.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("sign")||t.name.Contains("Sign")||t.name.Contains("fence")||t.name.Contains("Fence")))lines.Add("SIGN/FENCE "+PathOf(t)+" pos="+t.position+" rot="+t.eulerAngles);
   File.WriteAllLines(Path.Combine(Work,"v16-audit.txt"),lines);
  }
 }
}
