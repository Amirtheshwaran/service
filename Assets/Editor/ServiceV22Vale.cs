using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V22: night-one Vale glimpse. Adds "Emerging" (the creature's own Walk1_Action, slowed) and "Retreating" (the same
  // walk played backwards) to the Vale creature controller, with no transitions, so ServiceHorror's per-frame Moving=false
  // cannot pull them back to Watching. Then probes where ServiceOmens.ValePoints puts the glimpse as seen from the
  // upstairs approach and the study desk, and renders it (Audit/vale22/, Audit/vale22.txt).
  public static void Vale22(){Open();var sb=new StringBuilder();Vale22States(sb);Vale22Probe(sb);File.WriteAllText(Path.Combine(Work,"Audit","vale22.txt"),sb.ToString());}
  static void Vale22States(StringBuilder sb){
   const string path="Assets/ServiceArt/Vale creature.controller";var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
   var walk=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ServiceArt/Walk1_Action.anim");if(!ctrl||!walk){sb.AppendLine("controller or walk clip missing");return;}
   var sm=ctrl.layers[0].stateMachine;
   AnimatorState Ensure(string n,float speed,Vector3 pos){var st=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==n)??sm.AddState(n,pos);st.motion=walk;st.speed=speed;st.writeDefaultValues=true;return st;}
   Ensure("Emerging",.55f,new Vector3(500,40,0));Ensure("Retreating",-.5f,new Vector3(500,120,0));
   EditorUtility.SetDirty(ctrl);AssetDatabase.SaveAssets();
   sb.AppendLine("states: "+string.Join(", ",sm.states.Select(s=>$"{s.state.name}({(s.state.motion?s.state.motion.name:"-")} x{s.state.speed}, {s.state.transitions.Length} transitions)")));
   sb.AppendLine("any-state transitions: "+sm.anyStateTransitions.Length);
  }
  static void Vale22Probe(StringBuilder sb){
   var p=county.Properties[1];var data=county.Navigation;NavMeshDataInstance inst=default;if(data)inst=NavMesh.AddNavMeshData(data);
   var dir=Path.Combine(Work,"Audit","vale22");Directory.CreateDirectory(dir);var muted=MuteFeatures();
   var cam=new GameObject("vale cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~(1<<8);
   var torch=new GameObject("probe torch").AddComponent<Light>();torch.type=LightType.Spot;torch.spotAngle=46;torch.range=24;torch.intensity=4.2f;torch.color=new Color(.93f,.91f,.8f);
   var ent=county.Entity;bool wasActive=ent&&ent.activeSelf;var pos0=ent?ent.transform.position:Vector3.zero;var rot0=ent?ent.transform.rotation:Quaternion.identity;
   var agent=ent?ent.GetComponent<NavMeshAgent>():null;bool agentWas=agent&&agent.enabled;
   try{
    float floor=4.41f;var eyes=new[]{("stair-head",new Vector3(101.5f,floor+1.65f,227.6f)),("hall-entry",new Vector3(98.2f,floor+1.65f,230.8f)),("desk",new Vector3(p.TableApproach.position.x,p.TableApproach.position.y+1.65f,p.TableApproach.position.z))};
    foreach(var (name,eye) in eyes){
     if(!ServiceOmens.ValePoints(p,eye,out var E,out var H)){sb.AppendLine($"{name}: no glimpse points from {eye}");continue;}
     bool clear=ServiceInteraction.Clear(eye,E+Vector3.up*1.4f,null,null);float dist=Vector3.Distance(eye,E);
     sb.AppendLine($"{name}: eye {eye} -> E {E} (H {H}, spawn {p.EntitySpawn.position}) distance {dist:F1} m, line of sight {clear}");
     if(ent){if(agent)agent.enabled=false;var face=eye-E;face.y=0;ent.transform.SetPositionAndRotation(E,Quaternion.LookRotation(face));ent.SetActive(true);}
     cam.transform.position=eye;cam.transform.LookAt(E+Vector3.up*1.2f);torch.transform.SetPositionAndRotation(eye+cam.transform.right*.25f+Vector3.down*.3f,cam.transform.rotation);
     Shoot(cam,Path.Combine(dir,$"{name}.png"),640,360);}
   }finally{if(ent){ent.transform.SetPositionAndRotation(pos0,rot0);ent.SetActive(wasActive);if(agent)agent.enabled=agentWas;}Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(torch.gameObject);Restore(muted);if(data)NavMesh.RemoveNavMeshData(inst);}
  }
 }
}
