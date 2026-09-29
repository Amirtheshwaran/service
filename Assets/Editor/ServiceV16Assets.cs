using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {public static class ServiceV16Assets {
 static List<string> log=new List<string>();
 public static void Import(){
  var s=ServiceV16Audit.Open();
  foreach(string path in new[]{"Assets/External/DrillimpactArms/arms_rig.fbx","Assets/External/Renderpeople/Sophia/rp_sophia_animated_003_idling.fbx","Assets/External/Renderpeople/Nathan/rp_nathan_animated_003_walking.fbx"}){
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Legacy;importer.isReadable=true;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
   var o=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));log.Add("ASSET "+path);foreach(var r in o.GetComponentsInChildren<Renderer>())log.Add("RENDER "+r.name+" "+r.bounds+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NONE")));
   foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")))log.Add("CLIP "+clip.name+" seconds="+clip.length);
   foreach(var t in o.GetComponentsInChildren<Transform>().Take(6))log.Add("TRANSFORM "+t.name+" "+t.localPosition+" "+t.localScale+" "+t.localEulerAngles);Object.DestroyImmediate(o);
  }
  // Reuse the original atlas with a fresh URP material; don't carry incompatible Standard keywords.
  var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="Authored door atlas URP";mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/Content/Textures/BLD_Doors1_A.tif"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flooded_Grounds/Content/Textures/BLD_Doors1_N.tif"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Smoothness",.23f);mat.SetFloat("_Cull",0);AssetDatabase.CreateAsset(mat,"Assets/ServiceArt/V16 authored doors.mat");
  foreach(var p in s.Properties)if(p.Index==1||p.Index==2||p.Index==5){foreach(var r in p.DoorPanel.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;}
  foreach(var r in s.GetComponentsInChildren<Renderer>().Where(r=>r.transform.parent&&r.transform.parent.name=="Locked matching side entrance"))r.sharedMaterial=mat;
  var vale=s.Properties.Single(p=>p.Index==1);var leaf=vale.DoorPanel.GetComponentInChildren<MeshFilter>().transform;leaf.localScale=Vector3.Scale(leaf.localScale,new Vector3(.89f,.815f,1));var b=leaf.GetComponent<Renderer>().bounds;leaf.position+=vale.Door.position+Vector3.up*1.08f-b.center;vale.KnockPoint.position=leaf.GetComponent<Renderer>().bounds.center+vale.Door.forward*.08f;
  EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(ServiceV16Audit.Work,"v16-character-import.txt"),log);ServiceV16Audit.Preview(s,"Revision2");
 }
}}
