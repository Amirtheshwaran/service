using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V18 Fears to Fathom rework: Bell becomes the man who follows you (human pursuer + tree-line figure),
 // residents look at you while talking, and the depot worker returns to the recorded-walk model.
 public static class ServiceV18Apply {
  const string R="Assets/ServiceArt/V17",ScenePath="Assets/Scenes/HollisCounty.unity";
  public static void RunAndBuild(){Run();ServiceV17Apply.Build();}
  public static void Run(){
   var log=new StringBuilder();
   ServiceV17Apply.Loop(R+"/Anim/Locomotion--Run_N.anim.fbx",true);
   var mi=(ModelImporter)AssetImporter.GetAtPath(R+"/Anim/Locomotion--Run_N.anim.fbx");
   if(mi.animationType!=ModelImporterAnimationType.Human||mi.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel){mi.animationType=ModelImporterAnimationType.Human;mi.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;mi.SaveAndReimport();ServiceV17Apply.Loop(R+"/Anim/Locomotion--Run_N.anim.fbx",true);}
   var scene=EditorSceneManager.OpenScene(ScenePath);
   var county=Object.FindAnyObjectByType<CountyScene>();var res=Object.FindAnyObjectByType<ServiceResidents>();
   // Residents: look-at IK so they follow you with their eyes.
   var resident=AssetDatabase.LoadAssetAtPath<AnimatorController>(R+"/Prefabs/V17 Resident.controller");
   var layers=resident.layers;layers[0].iKPass=true;resident.layers=layers;EditorUtility.SetDirty(resident);
   foreach(var go in new[]{res.Correll,res.Bell}){var an=go.GetComponentInChildren<Animator>(true);if(an&&!an.GetComponent<ServiceLookAt>())an.gameObject.AddComponent<ServiceLookAt>();}
   // Human pursuer controller: Idle <-> Run on the "Moving" flag used by ServiceHorror, plus an "Attack" state.
   var path=R+"/Prefabs/V18 Pursuer.controller";AssetDatabase.DeleteAsset(path);
   var pc=AnimatorController.CreateAnimatorControllerAtPath(path);pc.AddParameter("Moving",AnimatorControllerParameterType.Bool);var sm=pc.layers[0].stateMachine;
   var idle=sm.AddState("Idle");idle.motion=ServiceV17Apply.Clip(R+"/Anim/Stand--Idle.anim.fbx");sm.defaultState=idle;
   var run=sm.AddState("Run");run.motion=ServiceV17Apply.Clip(R+"/Anim/Locomotion--Run_N.anim.fbx");
   var attack=sm.AddState("Attack");attack.motion=idle.motion;
   var go1=idle.AddTransition(run);go1.hasExitTime=false;go1.duration=.12f;go1.AddCondition(AnimatorConditionMode.If,0,"Moving");
   var go2=run.AddTransition(idle);go2.hasExitTime=false;go2.duration=.18f;go2.AddCondition(AnimatorConditionMode.IfNot,0,"Moving");
   // The man as an extra pursuit variant under the shared entity (same NavMesh agent, capture and retry flow).
   var entity=county.Entity.transform;
   var variants=(county.EntityVariants??new GameObject[0]).Where(v=>v).ToList();
   foreach(var old in variants.Where(v=>v.name.StartsWith("The man")).ToList()){variants.Remove(old);Object.DestroyImmediate(old);}
   var man=ServiceV17Apply.Human(R+"/Characters/JustMan/JustMan.fbx","The man — Daniel Bell (human pursuer)",entity,entity.position,entity.position+entity.forward,pc,m=>ServiceV17Apply.DressJustMan(m,false));
   man.transform.localPosition=Vector3.zero;man.transform.localRotation=Quaternion.identity;var ls=entity.lossyScale;man.transform.localScale=new Vector3(1/ls.x,1/ls.y,1/ls.z);man.SetActive(false);
   variants.Add(man);county.EntityVariants=variants.ToArray();
   log.AppendLine($"PURSUER variant index {variants.Count-1} of {variants.Count}: {string.Join(", ",variants.Select(v=>v.name))}");
   // The tree-line figure (same man, standing still).
   if(county.StalkerFigure)Object.DestroyImmediate(county.StalkerFigure);
   var figure=ServiceV17Apply.Human(R+"/Characters/JustMan/JustMan.fbx","The man — tree line figure",county.transform,Vector3.zero,Vector3.forward,resident,m=>ServiceV17Apply.DressJustMan(m,false));
   figure.SetActive(false);county.StalkerFigure=figure;
   // Depot worker back to Nathan's recorded walk: the leather-jacket model now belongs to Bell alone.
   var nathan=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Nathan — authored resident");
   if(nathan&&res.DepotWorker&&res.DepotWorker!=nathan){var v17=res.DepotWorker;nathan.gameObject.SetActive(true);res.DepotWorker=nathan;res.WorkerAnimation=nathan.GetComponentInChildren<Animation>(true);res.WorkerAnimator=null;Object.DestroyImmediate(v17.gameObject);log.AppendLine("WORKER restored Nathan");}
   EditorUtility.SetDirty(county);EditorUtility.SetDirty(res);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"v18-apply.txt"),log.ToString());
  }
 }
}
