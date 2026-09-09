using UnityEngine;
using UnityEngine.AI;
using UnityEditor.SceneManagement;
using System.Linq;
using System.IO;
using System.Collections.Generic;
namespace ServiceGameV2.Editor {
 public static class V4Inspect {
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=Object.FindFirstObjectByType<CountyScene>();var p=s.Properties[1];var h=p.GetComponentsInChildren<Transform>().First(t=>t.name=="Pref_Villa1_A");var nav=NavMesh.AddNavMeshData(s.Navigation);var log=new List<string>();
 foreach(var t in h.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("IntStairs")))log.Add("STAIRS "+t.name+" "+h.InverseTransformPoint(t.position)+" rotation "+t.localEulerAngles);
 foreach(float x in new[]{6f,14f,22f,30f,38f})foreach(float z in new[]{-2f,4f,12f}){var target=h.TransformPoint(new Vector3(x,8.08f,z));var path=new NavMeshPath();if(NavMesh.SamplePosition(target,out var hit,.3f,NavMesh.AllAreas)){bool ok=NavMesh.CalculatePath(p.Gate.position,hit.position,NavMesh.AllAreas,path);log.Add(x+","+z+" "+ok+" "+path.status+" "+string.Join("; ",path.corners.Select(c=>h.InverseTransformPoint(c).ToString("F2"))));}else log.Add(x+","+z+" NO FLOOR");}
 File.WriteAllLines(Path.GetFullPath(Application.dataPath+"/../../v4-layout.txt"),log);nav.Remove();
 var cam=s.View;cam.transform.SetParent(null);cam.cullingMask=~((1<<8)|(1<<9)|(1<<10));cam.orthographic=true;cam.orthographicSize=14;
 foreach(float y in new[]{7.5f,14.5f}){cam.transform.position=h.TransformPoint(new Vector3(20,y,4));cam.transform.rotation=h.rotation*Quaternion.Euler(90,0,0);var rt=new RenderTexture(1200,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1200,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1200,800),0,0);tex.Apply();File.WriteAllBytes(Path.GetFullPath(Application.dataPath+"/../../floor-"+y+".png"),tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
 }
 }
}
