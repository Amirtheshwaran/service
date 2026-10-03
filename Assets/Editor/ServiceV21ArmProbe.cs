using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Collapsed-forearm hunt: sample each gesture finely, alone and cross-faded from Hold, bake the skinned mesh and
  // measure how far left-arm vertices stray from the shoulder-elbow-wrist chain (a skinning collapse throws vertices
  // far off the chain). Writes Audit/armprobe21.txt.
  public static void ArmProbe21(){
   var fbx="Assets/ServiceArt/V19/Arms/service_arms.fbx";var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
   var anim=g.GetComponent<Animation>();if(!anim)anim=g.AddComponent<Animation>();
   foreach(var c in AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")))if(anim.GetClip(c.name)==null)anim.AddClip(c,c.name);
   var smr=g.GetComponentInChildren<SkinnedMeshRenderer>();Transform B(string n)=>g.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
   var sh=B("upper_arm.L");var el=B("forearm.L");var wr=B("hand.L");var sb=new StringBuilder();
   var mi=(ModelImporter)AssetImporter.GetAtPath(fbx);sb.AppendLine($"importer: resampleCurves {mi.resampleCurves} compression {mi.animationCompression} rotErr {mi.animationRotationError} posErr {mi.animationPositionError}");
   float Seg(Vector3 p,Vector3 a,Vector3 b){var ab=b-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-8f));return Vector3.Distance(p,a+ab*t);}
   float Worst(){var m=new Mesh();smr.BakeMesh(m,true);float w=0;foreach(var v in m.vertices){var p=smr.transform.TransformPoint(v);if(Vector3.Distance(p,wr.position)<.12f)continue; // hand/fingers excluded
     if(p.x>g.transform.position.x+.05f)continue;float d=Mathf.Min(Seg(p,sh.position,el.position),Seg(p,el.position,wr.position));if(d<.25f&&d>w)w=d;}Object.DestroyImmediate(m);return w;}
   foreach(var name in new[]{"Knock","Give","Place","Push","Reach","Torch"}){
    var clip=anim.GetClip(name);if(!clip){sb.AppendLine("no "+name);continue;}
    float worst=0,at=0;for(float t=0;t<=clip.length;t+=1/240f){clip.SampleAnimation(g,t);float w=Worst();if(w>worst){worst=w;at=t;}}
    // cross-fade from Hold at the start, as the game plays it (0.15 s)
    float worstX=0,atX=0;var hold=anim["Hold"];var st=anim[name];
    for(float t=0;t<.3f;t+=1/240f){float k=Mathf.Clamp01(t/.15f);anim.Stop();hold.enabled=true;hold.weight=1-k;hold.time=0;st.enabled=true;st.weight=k;st.time=t;anim.Sample();float w=Worst();if(w>worstX){worstX=w;atX=t;}}
    hold.enabled=st.enabled=false;
    sb.AppendLine($"{name}: alone worst sleeve offset {worst*100:F1} cm at {at:F3}s; cross-fade from Hold worst {worstX*100:F1} cm at {atX:F3}s (clip {clip.length:F2}s)");}

   Object.DestroyImmediate(g);File.WriteAllText(Path.Combine(Work,"Audit","armprobe21.txt"),sb.ToString());
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // The in-game viewmodel through the first 0.6 s of Push (and Give, Knock), cross-faded from Hold as ServiceHands
  // plays it, baked and photographed at 1/40 s steps (Audit/pushframes21).
  public static void PushFrames21(){Open();
   var dir=Path.Combine(Work,"Audit","pushframes21");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var cam=county.View;var muted=MuteFeatures();var vm=cam.transform.Find("V19 viewmodel");var lp=vm.localPosition;var lr=vm.localRotation;var ls=vm.localScale;
   var pc=new GameObject("pf cam").AddComponent<Camera>();pc.CopyFrom(cam);pc.transform.SetPositionAndRotation(new Vector3(0,600,0),Quaternion.identity);pc.clearFlags=CameraClearFlags.SolidColor;pc.backgroundColor=new Color(.3f,.32f,.35f);
   pc.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;var key=new GameObject("pf key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.1f;key.transform.rotation=Quaternion.Euler(30,-20,0);
   try{vm.SetParent(pc.transform,false);vm.localPosition=lp;vm.localRotation=lr;vm.localScale=ls;
    var anim=vm.GetComponentInChildren<Animation>();var smr=vm.GetComponentInChildren<SkinnedMeshRenderer>();
    foreach(var name in new[]{"Push","Give","Knock","Place","Torch"}){var hold=anim["Hold"];var st=anim[name];if(st==null)continue;
     var hsv=vm.GetComponentInChildren<ServiceHands>();int n=Mathf.CeilToInt(st.length*20);
     for(int i=0;i<=n;i++){float t=i/20f;float k=Mathf.Clamp01(t/.15f);float nt=t/st.length;
      if(hsv){foreach(var r in hsv.Envelope?hsv.Envelope.GetComponentsInChildren<Renderer>(true):new Renderer[0])r.enabled=name=="Place"&&nt>.1f&&t<hsv.PlaceLead;foreach(var r in hsv.EnvelopeGive?hsv.EnvelopeGive.GetComponentsInChildren<Renderer>(true):new Renderer[0])r.enabled=name=="Give"&&nt>.24f&&nt<.62f;}anim.Stop();foreach(AnimationState s in anim){s.enabled=false;s.weight=0;}
      hold.enabled=true;hold.weight=1-k;hold.time=0;st.enabled=true;st.weight=k;st.time=t;anim.Sample();
      var baked=new Mesh();smr.BakeMesh(baked,true);var bo=new GameObject("baked");bo.transform.SetParent(smr.transform,false);bo.AddComponent<MeshFilter>().sharedMesh=baked;bo.AddComponent<MeshRenderer>().sharedMaterials=smr.sharedMaterials;bo.layer=smr.gameObject.layer;smr.enabled=false;
      try{Shoot(pc,Path.Combine(dir,$"{name}-{i:00}.png"),480,270);}finally{smr.enabled=true;Object.DestroyImmediate(bo);Object.DestroyImmediate(baked);}}}
   }finally{vm.SetParent(cam.transform,false);vm.localPosition=lp;vm.localRotation=lr;vm.localScale=ls;Object.DestroyImmediate(pc.gameObject);Object.DestroyImmediate(key.gameObject);Restore(muted);}
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void PaperProbe21(){Open();var sb=new StringBuilder();
   foreach(var p in county.Properties){var pp=p.PostedPaper;if(!pp){sb.AppendLine($"p{p.Index} no PostedPaper");continue;}
    sb.AppendLine($"p{p.Index}: PostedPaper '{PathOf(pp.transform)}' at {pp.transform.position} active {pp.activeSelf}; DeliveryPoint {(p.DeliveryPoint?p.DeliveryPoint.position.ToString():"-")} dist {(p.DeliveryPoint?Vector3.Distance(pp.transform.position,p.DeliveryPoint.position):-1):F2} m; TableApproach {(p.TableApproach?p.TableApproach.position.ToString():"-")}");}
   // every table: Settle the papers, then photograph them from the player's eye at the table approach
   var muted=MuteFeatures();var cam=new GameObject("pp cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~(1<<8);
   var key=new GameObject("pp key").AddComponent<Light>();key.type=LightType.Point;key.range=4;key.intensity=1.5f;
   try{foreach(var v in county.Properties){var paper=v.PostedPaper;if(!paper||!v.TableApproach)continue;var start=paper.transform.position;paper.SetActive(true);
     var own=paper.GetComponentsInChildren<Renderer>();var pb=own[0].bounds;foreach(var r in own)pb.Encapsulate(r.bounds);
     var eye0=v.TableApproach.position+Vector3.up*1.6f;ServicePaperPlacement.Settle(paper,eye0);sb.AppendLine($"p{v.Index} paper size {pb.size} Settle shift {ServicePaperPlacement.LastShift} table {ServicePaperPlacement.LastTable} clear {ServicePaperPlacement.LastClear} hidden {ServicePaperPlacement.LastHidden} (blocker {ServicePaperPlacement.LastBlocker.min}..{ServicePaperPlacement.LastBlocker.max}, paper {pb.center}, eye {eye0}) mats {string.Join(",",own.Select(r=>r.sharedMaterial?r.sharedMaterial.name+"/"+r.sharedMaterial.shader.name:"-"))}");
     pb=own[0].bounds;foreach(var r in own)pb.Encapsulate(r.bounds);
     var eye=v.TableApproach.position+Vector3.up*1.55f;eye+=(new Vector3(eye.x,0,eye.z)-new Vector3(pb.center.x,0,pb.center.z)).normalized*.15f;
     key.transform.position=eye+Vector3.up*.3f;cam.fieldOfView=60;cam.transform.position=eye;cam.transform.LookAt(pb.center);Shoot(cam,Path.Combine(Work,"Audit",$"paperprobe-eye{v.Index}.png"),640,360);
     cam.transform.position=pb.center+Vector3.up*1.1f;cam.transform.rotation=Quaternion.LookRotation(Vector3.down,(new Vector3(pb.center.x,0,pb.center.z)-new Vector3(eye.x,0,eye.z)).normalized);Shoot(cam,Path.Combine(Work,"Audit",$"paperprobe-top{v.Index}.png"),640,360);
     paper.transform.position=start;paper.SetActive(false);}}
   finally{Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(key.gameObject);Restore(muted);}
   File.WriteAllText(Path.Combine(Work,"Audit","paperprobe21.txt"),sb.ToString());}
 }
}
