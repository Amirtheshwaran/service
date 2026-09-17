using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ServiceGameV2.Editor {
public static class ServiceFinalFit {
 public static void Build(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var scene=Object.FindAnyObjectByType<CountyScene>();Physics.SyncTransforms();var report=new List<string>();
  foreach(var p in scene.Properties.Where(p=>p.DoorPanel&&p.DoorPanel.name=="Working front door")){
   bool Wall(float x){var origin=p.Door.position+p.Door.right*x+p.Door.forward+Vector3.up*1.1f;return Physics.RaycastAll(origin,-p.Door.forward,1.5f).Any(h=>h.transform.IsChildOf(p.Building));}
   if(Wall(0))throw new System.Exception("Door is not inside an opening: "+p.Index);
   float left=.025f,right=.025f;while(left<2&&!Wall(-left))left+=.025f;while(right<2&&!Wall(right))right+=.025f;
   float width=left+right-.035f;if(width<.7f||width>2.2f)throw new System.Exception("Unexpected door opening width: "+p.Index+" "+width);
   var leaf=p.DoorPanel.GetChild(0);var b=leaf.GetComponent<MeshFilter>().sharedMesh.bounds;var scale=leaf.localScale;scale.x=width/b.size.x;leaf.localScale=scale;p.DoorPanel.position=p.Door.position+p.Door.right*((right-left)*.5f-b.center.x*scale.x);
   report.Add("Door "+p.Index+": opening width "+width.ToString("F3")+"m; leaf fitted to authored opening");
  }
  EditorSceneManager.MarkSceneDirty(scene.gameObject.scene);EditorSceneManager.SaveScene(scene.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllLines(Path.GetFullPath(Application.dataPath+"/../../door-fit.txt"),report);ServiceQuickBuild.Build();
 }
}}
