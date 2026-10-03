using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V21 stages. Each is idempotent and appends what it changed to work/v19-rebuild.txt.
 public static partial class ServiceV19Rebuild {
  const string V17Root="Assets/ServiceArt/V17";
  // Residents: the answer choreography (ServiceResidents) walks them from the hall into the doorway and back, so their
  // controller gains Walk (Locomotion--Walk_N) and WalkBack (the same clip played backwards) beside Idle.
  public static void Residents21(){Open();Residents21Stage();Save("residents21");}
  static void Residents21Stage(){
   var ctrl=AssetDatabase.LoadAssetAtPath<AnimatorController>($"{V17Root}/Prefabs/V17 Resident.controller");
   var walk=AssetDatabase.LoadAllAssetsAtPath($"{V17Root}/Anim/Locomotion--Walk_N.anim.fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview"));
   if(!ctrl||!walk){log.AppendLine($"RESIDENTS controller {(bool)ctrl} walk {(bool)walk}");return;}
   var sm=ctrl.layers[0].stateMachine;
   AnimatorState Ensure(string name,float speed){var s=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==name);if(!s)s=sm.AddState(name);s.motion=walk;s.speed=speed;return s;}
   Ensure("Walk",1);Ensure("WalkBack",-1);EditorUtility.SetDirty(ctrl);
   var res=Object.FindAnyObjectByType<ServiceResidents>();
   foreach(var r in new[]{res.Correll,res.Bell}){if(!r)continue;foreach(var a in r.GetComponentsInChildren<Animator>(true)){a.applyRootMotion=false;log.AppendLine($"RESIDENT {r.name}: animator {a.name} human {a.isHuman} avatar {(a.avatar?a.avatar.name:"-")} controller {(a.runtimeAnimatorController?a.runtimeAnimatorController.name:"-")}");}}
   log.AppendLine($"RESIDENTS controller states: {string.Join(",",sm.states.Select(x=>x.state.name+"("+x.state.speed+")"))} walk clip {walk.name} {walk.length:F2}s human {walk.humanMotion}");
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Where the skinned arms mesh actually is for each clip: left-hand vertices vs the hand.L bone.
  public static void ArmsProbe21(){
   var fbx="Assets/ServiceArt/V19/Arms/service_arms.fbx";var model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);var g=(GameObject)PrefabUtility.InstantiatePrefab(model);
   var sb=new StringBuilder();var anim=g.GetComponent<Animation>();var clips=AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToDictionary(c=>c.name);
   var smrs=g.GetComponentsInChildren<SkinnedMeshRenderer>(true);sb.AppendLine("skinned renderers: "+string.Join("; ",smrs.Select(s=>$"{s.name} verts {s.sharedMesh.vertexCount} bones {s.bones.Length} root {(s.rootBone?s.rootBone.name:"-")}")));
   Transform B(string n)=>g.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==n);
   foreach(var (clip,frame) in new[]{("Hold",1),("Knock",14),("Give",18),("Push",12)}){
    if(!clips.TryGetValue(clip,out var c)){sb.AppendLine("no clip "+clip);continue;}c.SampleAnimation(g,(frame-1)/30f);
    var hl=B("hand.L");var hr=B("hand.R");sb.AppendLine($"{clip}@{frame}: hand.L {g.transform.InverseTransformPoint(hl.position)} hand.R {g.transform.InverseTransformPoint(hr.position)} camera {g.transform.InverseTransformPoint(B("camera").position)}");
    foreach(var s in smrs){var m=new Mesh();s.BakeMesh(m,true);var vs=m.vertices;float best=99;Vector3 bestV=Vector3.zero;int near=0;
     foreach(var v in vs){var w=s.transform.TransformPoint(v);float dd=Vector3.Distance(w,hl.position);if(dd<best){best=dd;bestV=w;}if(dd<.12f)near++;}
     var bw=s.bounds;sb.AppendLine($"   {s.name}: nearest vertex to hand.L {best:F3} m, verts within 12 cm {near}; renderer bounds {bw.center} size {bw.size}");}
   }
   // which bones carry weight for vertices on the left side
   foreach(var s in smrs){var bw=s.sharedMesh.boneWeights;var counts=new System.Collections.Generic.Dictionary<string,int>();foreach(var w in bw){if(w.weight0<.5f)continue;var n=s.bones[w.boneIndex0].name;counts[n]=counts.TryGetValue(n,out var k)?k+1:1;}
    sb.AppendLine($"{s.name} dominant bones: "+string.Join(", ",counts.OrderByDescending(x=>x.Value).Take(40).Select(x=>x.Key+" "+x.Value)));}
   Object.DestroyImmediate(g);File.WriteAllText(Path.Combine(Work,"Audit","arms-probe21.txt"),sb.ToString());
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Render the gesture previews twice: normal culling and double-sided, to tell inverted faces from placement.
  public static void HandsCull21(){Open();var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/ServiceArt/V19/Arms/V19 arms jacket.mat");
   var dir=Path.Combine(Work,"Audit","hands");
   float was=mat.HasProperty("_Cull")?mat.GetFloat("_Cull"):2;
   try{
    HandsPreview();foreach(var f in Directory.GetFiles(dir,"v21-*.png"))File.Copy(f,f.Replace("v21-","cullback-"),true);
    mat.SetFloat("_Cull",0);mat.doubleSidedGI=true;HandsPreview();foreach(var f in Directory.GetFiles(dir,"v21-*.png"))File.Copy(f,f.Replace("v21-","culloff-"),true);
   }finally{mat.SetFloat("_Cull",was);}
   // count faces on each side whose winding normal points into the arm (toward the bone axis)
   var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ServiceArt/V19/Arms/service_arms.fbx"));var smr=g.GetComponentInChildren<SkinnedMeshRenderer>();var m=new Mesh();smr.BakeMesh(m,true);
   var v=m.vertices;var t=m.triangles;int lin=0,lout=0,rin=0,rout=0;var c=Vector3.zero;
   Transform B(string n)=>g.GetComponentsInChildren<Transform>(true).First(x=>x.name==n);
   for(int i=0;i<t.Length;i+=3){var a=smr.transform.TransformPoint(v[t[i]]);var b=smr.transform.TransformPoint(v[t[i+1]]);var cc=smr.transform.TransformPoint(v[t[i+2]]);var n=Vector3.Cross(b-a,cc-a);var mid=(a+b+cc)/3;
    bool left=mid.x<g.transform.position.x;var fa=B(left?"forearm.L":"forearm.R");var axis=(B(left?"hand.L":"hand.R").position-fa.position);var rel=mid-fa.position;var radial=rel-axis.normalized*Vector3.Dot(rel,axis.normalized);
    bool outward=Vector3.Dot(n,radial)>0;if(left){if(outward)lout++;else lin++;}else{if(outward)rout++;else rin++;}}
   Object.DestroyImmediate(g);File.WriteAllText(Path.Combine(Work,"Audit","arms-winding21.txt"),$"left faces outward {lout} inward {lin}; right faces outward {rout} inward {rin}");
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void HandsScene21(){Open();var sb=new StringBuilder();var vm=county.View.transform.Find("V19 viewmodel");
   var anim=vm.GetComponentInChildren<Animation>();var smr=vm.GetComponentInChildren<SkinnedMeshRenderer>(true);
   sb.AppendLine($"anim on {anim.name}; smr {smr.name} enabled {smr.enabled} active {smr.gameObject.activeInHierarchy} updateWhenOffscreen {smr.updateWhenOffscreen} quality {smr.quality} layer {smr.gameObject.layer} viewMask {county.View.cullingMask}");
   var hlByName=vm.GetComponentsInChildren<Transform>(true).First(t=>t.name=="hand.L");var hlInBones=smr.bones.FirstOrDefault(b=>b&&b.name=="hand.L");
   sb.AppendLine($"hand.L same transform as smr bone: {hlByName==hlInBones}; smr bones null: {smr.bones.Count(b=>!b)}; root bone {(smr.rootBone?smr.rootBone.name:"-")}");
   foreach(var (clip,frame) in new[]{("Hold",1),("Knock",14),("Give",18)}){var c=anim.GetClip(clip);if(!c){sb.AppendLine("no clip "+clip);continue;}c.SampleAnimation(anim.gameObject,(frame-1)/30f);
    var m=new Mesh();smr.BakeMesh(m,true);float best=99;int near=0;foreach(var v in m.vertices){var w=smr.transform.TransformPoint(v);float dd=Vector3.Distance(w,hlByName.position);if(dd<best)best=dd;if(dd<.04f)near++;}
    var eye=county.View.transform;sb.AppendLine($"{clip}@{frame}: hand.L in eye space {eye.InverseTransformPoint(hlByName.position)}; nearest vert {best:F3}; verts within 4cm {near}; smr bounds {smr.bounds.center}/{smr.bounds.size}; lossy {smr.transform.lossyScale}");
    // where are the left-side vertices relative to the eye?
    var vs=m.vertices.Select(v=>eye.InverseTransformPoint(smr.transform.TransformPoint(v))).ToArray();var left=vs.Where(p=>p.x<0).ToArray();
    if(left.Length>0)sb.AppendLine($"   left-of-eye verts {left.Length}: x {left.Min(p=>p.x):F3}..{left.Max(p=>p.x):F3} y {left.Min(p=>p.y):F3}..{left.Max(p=>p.y):F3} z {left.Min(p=>p.z):F3}..{left.Max(p=>p.z):F3}");}
   File.WriteAllText(Path.Combine(Work,"Audit","hands-scene21.txt"),sb.ToString());}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void HandsLook21(){Open();HandsPreview();}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void MirrorProbe21(){Open();var sb=new StringBuilder();var seat=county.DriverSeat;
   foreach(var t in county.Car.GetComponentsInChildren<Transform>(true).Where(t=>t.name.ToLowerInvariant().Contains("mirror"))){
    var rs=t.GetComponentsInChildren<Renderer>(true);var b=rs.Length>0?rs[0].bounds:new Bounds(t.position,Vector3.zero);foreach(var r in rs)b.Encapsulate(r.bounds);
    sb.AppendLine($"{PathOf(t)} active {t.gameObject.activeInHierarchy} layer {t.gameObject.layer} seat-local pos {seat.InverseTransformPoint(t.position)} bounds centre(seat) {seat.InverseTransformPoint(b.center)} size {b.size} renderers {rs.Length}");}
   sb.AppendLine("cockpit "+(county.Cockpit?PathOf(county.Cockpit.transform):"-"));File.WriteAllText(Path.Combine(Work,"Audit","mirror-probe21.txt"),sb.ToString());}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Audio import: long music and loops stream (no main-thread decode on first play - the chaos test caught 0.8 s hitches
  // when the pursuit score and the radio first played); short effects are preloaded so they never stall either.
  public static void Audio21(){AssetDatabase.Refresh();var sb=new StringBuilder();
   foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Audio/V21","Assets/Resources/Audio/Radio","Assets/Resources/Audio/V9","Assets/Resources/Audio/V17"})){
    var path=AssetDatabase.GUIDToAssetPath(guid);var imp=AssetImporter.GetAtPath(path) as AudioImporter;if(!imp)continue;var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    bool longOne=clip&&clip.length>12;var st=imp.defaultSampleSettings;st.loadType=longOne?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;st.preloadAudioData=!longOne;imp.defaultSampleSettings=st;imp.loadInBackground=false;
    imp.SaveAndReimport();sb.AppendLine($"{path}: {(clip?clip.length:0):F1}s -> {st.loadType}{(st.preloadAudioData?" preloaded":"")}");}
   File.WriteAllText(Path.Combine(Work,"Audit","audio21.txt"),sb.ToString());}
 }
}
