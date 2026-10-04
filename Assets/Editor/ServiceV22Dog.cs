using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V22: Rex charges. The V17 dog controller only had its two idles; this adds the asset's own run loop (A_Run, RetroStyle
  // Games German Shepherd pack) as a "Run" state, measures the clip's stride so ServiceLife can match leg speed to ground
  // speed, and squares the model to its walking transform (the shepherd faced the root's side, so turning dog.forward
  // toward the player showed his flank). Audit/dog22.txt and close-up renders in Audit/dog22/.
  public static void Dog22(){Open();var sb=new StringBuilder();Dog22Stage(sb);Save("dog22");DogLook22(sb);File.WriteAllText(Path.Combine(Work,"Audit","dog22.txt"),sb.ToString());}
  public static void DogLook22Only(){Open();var sb=new StringBuilder();DogLook22(sb);File.WriteAllText(Path.Combine(Work,"Audit","dog22-look.txt"),sb.ToString());}
  static Transform DogRoot()=>All().FirstOrDefault(t=>t.name=="Correll yard dog");
  static void Dog22Stage(StringBuilder sb){
   const string ctrlPath="Assets/ServiceArt/V17/Prefabs/V17 Dog.controller";
   ServiceV17Apply.Loop("Assets/ServiceArt/V17/Dog/A_Idle_Playing.fbx",true); // held while Rex stands barking: must loop (was a 4 s one-shot)
   var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);if(!ctrl){sb.AppendLine("no dog controller");return;}
   var run=AssetDatabase.LoadAllAssetsAtPath("Assets/ServiceArt/V17/Dog/A_Run.fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview"));
   if(!run){sb.AppendLine("no run clip");return;}
   var sm=ctrl.layers[0].stateMachine;var st=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Run")??sm.AddState("Run",new Vector3(300,120,0));
   st.motion=run;st.writeDefaultValues=true;EditorUtility.SetDirty(ctrl);AssetDatabase.SaveAssets();
   sb.AppendLine($"controller states: {string.Join(", ",sm.states.Select(s=>s.state.name+"("+(s.state.motion?s.state.motion.name:"-")+")"))}; run clip '{run.name}' {run.length:F3} s loop {run.isLooping}");
   var root=DogRoot();if(!root){sb.AppendLine("no 'Correll yard dog'");return;}
   var anim=root.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.runtimeAnimatorController);if(!anim){sb.AppendLine("no dog animator");return;}
   var bones=anim.GetComponentsInChildren<Transform>(true);
   Transform Bone(params string[] keys)=>bones.FirstOrDefault(b=>keys.Any(k=>b.name.ToLowerInvariant().Contains(k)));
   sb.AppendLine("bones: "+string.Join(", ",bones.Select(b=>b.name).Take(80)));
   // facing: from the hips to the head, on the ground plane
   var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/ServiceArt/V17/Dog/A_Breathing.fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview"));if(idle)idle.SampleAnimation(anim.gameObject,0);
   // the RetroStyle rig: DEF-spine is the hips, DEF-jaw the head
   var head=bones.FirstOrDefault(b=>b.name=="DEF-jaw")??Bone("head");var hips=bones.FirstOrDefault(b=>b.name=="DEF-spine")??Bone("pelvis","hips");
   if(head&&hips){var face=head.position-hips.position;face.y=0;float yaw=Vector3.SignedAngle(root.forward,face,Vector3.up);
    sb.AppendLine($"facing: root.forward {root.forward}, hips->head {face.normalized} ({face.magnitude:F2} m), off by {yaw:F1} deg");
    if(Mathf.Abs(yaw)>20){var model=anim.transform;var fix=Quaternion.AngleAxis(-yaw,Vector3.up);var pivot=root.position;model.SetPositionAndRotation(pivot+fix*(model.position-pivot),fix*model.rotation);EditorUtility.SetDirty(model);
     face=head.position-hips.position;face.y=0;sb.AppendLine($"squared the model to the root: now off by {Vector3.SignedAngle(root.forward,face,Vector3.up):F1} deg");}}
   else sb.AppendLine("facing: head/hips bones not found");
   // stride: how far a front paw travels against the body over one cycle (stance length), and the cycle time
   var paw=bones.FirstOrDefault(b=>b.name=="DEF-front_toe.L")??bones.FirstOrDefault(b=>b.name=="DEF-front_foot.L");
   if(paw){float lo=float.MaxValue,hi=float.MinValue;Vector3 drift0=Vector3.zero,drift1=Vector3.zero;int n=40;
    for(int i=0;i<=n;i++){float t=run.length*i/n;run.SampleAnimation(anim.gameObject,t);var local=anim.transform.InverseTransformPoint(paw.position);var fwd=anim.transform.InverseTransformDirection(root.forward);float along=Vector3.Dot(local,fwd);lo=Mathf.Min(lo,along);hi=Mathf.Max(hi,along);
     if(i==0&&hips)drift0=anim.transform.InverseTransformPoint(hips.position);if(i==n&&hips)drift1=anim.transform.InverseTransformPoint(hips.position);}
    float stride=(hi-lo)*anim.transform.lossyScale.x;float cycle=Mathf.Max(.05f,run.length);
    sb.AppendLine($"run: paw '{paw.name}' swings {stride:F2} m along the body per {cycle:F2} s cycle; hips drift over a cycle {(drift1-drift0).magnitude*anim.transform.lossyScale.x:F3} m");
    // a gallop covers about twice the paw's reach per cycle; runRef is the ground speed the clip shows at speed 1
    sb.AppendLine($"RUNREF {stride*2f/cycle:F2}");}
   else sb.AppendLine("run: no paw bone found");
   if(idle)idle.SampleAnimation(anim.gameObject,0);
  }
  static void DogLook22(StringBuilder sb){
   var root=DogRoot();if(!root)return;var anim=root.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.runtimeAnimatorController);if(!anim)return;
   var run=AssetDatabase.LoadAllAssetsAtPath("Assets/ServiceArt/V17/Dog/A_Run.fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview"));if(!run)return;
   var dir=Path.Combine(Work,"Audit","dog22");Directory.CreateDirectory(dir);var muted=MuteFeatures();
   var cam=new GameObject("dog cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.fieldOfView=40;
   var key=new GameObject("dog key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.3f;key.transform.rotation=Quaternion.Euler(45,30,0);
   var smrs=anim.GetComponentsInChildren<SkinnedMeshRenderer>(true);
   try{
    int k=0;foreach(var t in new[]{0f,run.length*.25f,run.length*.5f,run.length*.75f}){run.SampleAnimation(anim.gameObject,t);
     var baked=smrs.Select(s=>{var m=new Mesh();s.BakeMesh(m,true);var o=new GameObject("baked");o.transform.SetParent(s.transform,false);o.AddComponent<MeshFilter>().sharedMesh=m;o.AddComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;o.layer=s.gameObject.layer;s.enabled=false;return (s,o,m);}).ToList();
     try{var c=root.position+Vector3.up*.45f;
      cam.transform.position=c+root.right*3.2f+Vector3.up*.4f;cam.transform.LookAt(c);Shoot(cam,Path.Combine(dir,$"side-{k}.png"),640,400);
      if(k==0){cam.transform.position=c+root.forward*3.2f+Vector3.up*.6f;cam.transform.LookAt(c);Shoot(cam,Path.Combine(dir,"front.png"),640,400);}}
     finally{foreach(var (s,o,m) in baked){s.enabled=true;Object.DestroyImmediate(o);Object.DestroyImmediate(m);}}k++;}
    sb.AppendLine("renders in Audit/dog22 (side-0..3 = run cycle quarters from the dog's right; front = from root.forward)");
   }finally{Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(key.gameObject);Restore(muted);}
  }
 }
}
