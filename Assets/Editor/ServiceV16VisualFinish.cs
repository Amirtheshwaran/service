using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.Rendering;using UnityEngine.Rendering.Universal;
namespace ServiceGameV2.Editor {public static class ServiceV16VisualFinish {
 public static void VerifyBuild(){ServiceV16Audit.Open();ServiceQuickBuild.Build();}
 public static void Build(){var s=ServiceV16Audit.Open();var terrain=s.GetComponentInChildren<Terrain>();
  foreach(var entry in new[]{(0,new Vector3(-62.5f,0,95.2f)),(4,new Vector3(-68.5f,0,299.8f)),(5,new Vector3(81.2f,2.53f,356.8f))}){var p=s.Properties.Single(p=>p.Index==entry.Item1);var sign=p.NoticePoint.parent;sign.position=entry.Item2;var min=sign.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);float floor=entry.Item1==5?2.53f:terrain.SampleHeight(sign.position)+terrain.transform.position.y;sign.position+=Vector3.up*(floor+.02f-min);}
  var morrow=s.Properties.Single(p=>p.Index==5);if(morrow.PorchLight)morrow.PorchLight.transform.position=morrow.Door.position+Vector3.up*3.25f+morrow.Door.forward*.4f;if(morrow.AddressLabel)morrow.AddressLabel.transform.position=morrow.Door.position+Vector3.up*2.1f+morrow.Door.forward*.18f+morrow.Door.right*1.5f;
  var volume=s.GetComponentInChildren<Volume>();var profile=volume.sharedProfile;profile.components.RemoveAll(c=>!c);
  if(!profile.TryGet<FilmGrain>(out var grain))grain=profile.Add<FilmGrain>(true);grain.type.Override(FilmGrainLookup.Medium1);grain.intensity.Override(.22f);grain.response.Override(.72f);
  if(!profile.TryGet<ChromaticAberration>(out var ca))ca=profile.Add<ChromaticAberration>(true);ca.intensity.Override(.045f);
  foreach(var component in profile.components){if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(component);}EditorUtility.SetDirty(profile);
  EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();ServiceQuickBuild.Build();
 }
}}
