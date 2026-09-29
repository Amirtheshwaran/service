using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {public static class ServiceV16ResidentFit {
 public static void Build(){var s=ServiceV16Audit.Open();var cast=s.GetComponentInChildren<ServiceResidents>();var log=new List<string>();
  foreach(var go in new[]{cast.Correll,cast.Bell,cast.DepotWorker.gameObject}){var anim=go.GetComponent<Animation>();var original=anim.clip;string name=go==cast.DepotWorker.gameObject?"Nathan walking in place":"Sophia idle";string path="Assets/ServiceArt/V16 "+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=Object.Instantiate(original);clip.name=name;clip.legacy=true;float skip=1/original.frameRate;
    foreach(var binding in AnimationUtility.GetCurveBindings(original)){var curve=AnimationUtility.GetEditorCurve(original,binding);bool rootTravel=name.StartsWith("Nathan")&&binding.path.EndsWith("_root")&&(binding.propertyName=="m_LocalPosition.x"||binding.propertyName=="m_LocalPosition.z");if(binding.propertyName.StartsWith("m_LocalPosition")&&Mathf.Abs(curve.Evaluate(original.length)-curve.Evaluate(skip))>1)log.Add("TRANSLATION "+binding.path+" "+binding.propertyName+" delta="+(curve.Evaluate(original.length)-curve.Evaluate(skip))+" locked="+rootTravel);
     var keys=curve.keys.Where(k=>k.time>=skip).Select(k=>{k.time-=skip;if(rootTravel){k.value=curve.Evaluate(skip);k.inTangent=k.outTangent=0;}return k;}).ToList();if(keys.Count==0||keys[0].time>.0001f)keys.Insert(0,new Keyframe(0,curve.Evaluate(skip)));AnimationUtility.SetEditorCurve(clip,binding,new AnimationCurve(keys.ToArray()));}
    clip.wrapMode=WrapMode.Loop;AssetDatabase.CreateAsset(clip,path);
   }foreach(AnimationState state in anim.Cast<AnimationState>().ToArray())anim.RemoveClip(state.name);anim.AddClip(clip,clip.name);anim.clip=clip;anim.playAutomatically=true;anim.wrapMode=WrapMode.Loop;clip.SampleAnimation(go,.4f);
   foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterial.SetFloat("_Smoothness",.1f);EditorUtility.SetDirty(r.sharedMaterial);}var collider=go.GetComponent<CapsuleCollider>();if(!collider)collider=go.AddComponent<CapsuleCollider>();collider.center=Vector3.up*.87f;collider.height=1.74f;collider.radius=.25f;
  }
  cast.WalkFrom=new Vector3(-6.2f,.025f,-11);cast.WalkTo=new Vector3(-6.2f,.025f,-1);cast.DepotWorker.position=cast.WalkFrom;cast.DepotWorker.rotation=Quaternion.identity;
  var colliders=cast.GetComponentsInChildren<Collider>();foreach(var c in colliders)c.enabled=false;ServiceBuild.RebakeV13(s);foreach(var c in colliders)c.enabled=true;
  EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(ServiceV16Audit.Work,"v16-resident-fit.txt"),log);ServiceQuickBuild.Build();
 }
}}
