using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {public static class ServiceV16Revision {
 public static void InspectHands(){var s=ServiceV16Audit.Open();ServiceForestMood.Apply(s,0);s.Cockpit.SetActive(false);s.Entity.SetActive(false);var cam=s.View;cam.transform.SetParent(null);cam.transform.position=new Vector3(-63,2.5f,92);cam.transform.LookAt(new Vector3(-68,2,92));cam.nearClipPlane=.035f;cam.cullingMask=~(1<<8);s.Flashlight.enabled=true;var hands=cam.GetComponentInChildren<ServiceHands>();var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/External/DrillimpactArms/arms_rig.fbx").OfType<AnimationClip>().Where(c=>c.name.EndsWith("|relax")||c.name.EndsWith("|jab.R")).ToArray();string dir=Path.Combine(ServiceV16Audit.Work,"PreviewV16HandFit");Directory.CreateDirectory(dir);foreach(int yaw in new[]{0,180})foreach(float z in new[]{.4f,.7f}){hands.transform.localRotation=Quaternion.Euler(0,yaw,0);hands.transform.localPosition=new Vector3(0,-1.74315f,z);foreach(var clip in clips){clip.SampleAnimation(hands.Rig.gameObject,clip.length*.5f);ServiceV16Audit.Capture(cam,Path.Combine(dir,yaw+"-"+z+"-"+clip.name.Split('|').Last()+".png"));}}}
 public static void Run(){
  var s=ServiceV16Audit.Open();var log=new List<string>();var terrain=s.GetComponentInChildren<Terrain>();
  var hands=s.View.GetComponentInChildren<ServiceHands>();hands.transform.localRotation=Quaternion.Euler(0,180,0);
  foreach(var p in s.Properties){if(!p.NoticePoint)continue;var sign=p.NoticePoint.parent;sign.position=p.Door.position+p.Door.forward*2.3f+p.Door.right*2.6f;
   var bounds=sign.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});float y=terrain.SampleHeight(sign.position)+terrain.transform.position.y;sign.position+=Vector3.up*(y+.025f-bounds.min.y);
  }
  foreach(var t in s.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Closure")||t.name.Contains("survey")||t.name.Contains("road")||t.name.Contains("Road"))){log.Add("ROAD "+ServiceV16Audit.PathOf(t)+" pos "+t.position+" scale "+t.lossyScale);foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true))log.Add("  "+r.name+" "+r.bounds+" "+(r.GetComponent<MeshFilter>()?AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh):"text"));}
  foreach(var r in hands.GetComponentsInChildren<SkinnedMeshRenderer>()){var m=new Mesh();r.BakeMesh(m);foreach(var v in new[]{m.bounds.min,m.bounds.max,m.bounds.center})log.Add("HANDS local "+s.View.transform.InverseTransformPoint(r.transform.TransformPoint(v)));Object.DestroyImmediate(m);}
  EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(ServiceV16Audit.Work,"v16-revision-audit.txt"),log);
  ServiceV16Audit.Preview(s,"Signs");
  var cam=s.View;cam.transform.position=new Vector3(-63,2.5f,92);cam.transform.LookAt(new Vector3(-68,2,92));
  string dir=Path.Combine(ServiceV16Audit.Work,"PreviewV16Hands");Directory.CreateDirectory(dir);var anim=hands.Rig;
  foreach(var clip in AssetDatabase.LoadAllAssetsAtPath("Assets/External/DrillimpactArms/arms_rig.fbx").OfType<AnimationClip>().Where(c=>c.name.EndsWith("|relax")||c.name.EndsWith("|jab.R")||c.name.EndsWith("|grab.R"))){clip.SampleAnimation(anim.gameObject,clip.length*.5f);ServiceV16Audit.Capture(cam,Path.Combine(dir,clip.name.Split('|').Last()+".png"));}
  // Building a loaded scene avoids Unity's direct scene-load/build crash on this project.
  ServiceV16Audit.Open();ServiceQuickBuild.Build();
 }
}}

