using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ServiceGameV2.Editor {
public static class ServiceDoorInspect {
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=Object.FindAnyObjectByType<CountyScene>();var lines=new List<string>();Physics.SyncTransforms();
 foreach(int i in new[]{0,3,4}){var p=s.Properties[i];lines.Add("PROPERTY "+i+" anchor local="+p.Building.InverseTransformPoint(p.Door.position));
 for(float x=-3;x<=3;x+=.25f){var origin=p.Door.position+p.Door.right*x+p.Door.forward*2+Vector3.up*1.1f;var hits=Physics.RaycastAll(origin,-p.Door.forward,12).Where(h=>h.transform.IsChildOf(p.Building)).OrderBy(h=>h.distance).ToArray();lines.Add("x="+x+" distance="+(hits.Length>0?hits[0].distance:-1));}
 }
 foreach(var n in new[]{"Cabin1_Door_A","BrickHouse_Door_A"}){var path=AssetDatabase.FindAssets(n+" t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(x=>Path.GetFileNameWithoutExtension(x)==n);var o=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));o.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);lines.Add(n+" mesh "+o.GetComponent<MeshFilter>().sharedMesh.bounds);Object.DestroyImmediate(o);}
 File.WriteAllLines(Path.GetFullPath(Application.dataPath+"/../../door-inspection.txt"),lines);
 }
}}
