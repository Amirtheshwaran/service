using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V20 arms: the torch hand re-posed overhand (back of the hand to the eye) and a second copy of the rig seated at the
 // driver's eye holding the steering wheel, with one pose per wheel angle (Sources/V19/arms_pose.py, Drive_0..8).
 public static partial class ServiceV19Rebuild {
  public static void Arms20(){Open();HandsStage();DrivingArmsStage();Save("arms20");DrivingPreview();}
  public static void DrivePreview(){Open();DrivingPreview();}
  public static void DriveOcclusion(){Open();var seat=county.DriverSeat;var dir=Path.Combine(Work,"Audit","drive20");var muted=MuteFeatures();
   var arms=seat.GetComponentsInChildren<ServiceDrivingHands>(true).First();arms.GetComponent<Animation>().GetClip(arms.Clips[4]).SampleAnimation(arms.gameObject,0);
   var cam=new GameObject("occ cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~(1<<8);
   var sb=new System.Text.StringBuilder();sb.AppendLine("View parent "+(county.View.transform.parent?county.View.transform.parent.name:"-")+" children: "+string.Join(", ",county.View.transform.Cast<Transform>().Select(t=>t.name)));
   foreach(var r in county.Car.GetComponentsInChildren<SkinnedMeshRenderer>(true))sb.AppendLine("SKINNED "+AnimationUtility.CalculateTransformPath(r.transform,county.Car)+" enabled "+r.enabled+" active "+r.gameObject.activeInHierarchy);
   var armR=arms.GetComponentsInChildren<Renderer>(true);
   try{cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(11,0,0));
    foreach(var r in armR)r.enabled=false;Shoot(cam,Path.Combine(dir,"occ-noarms.png"),960,540);foreach(var r in armR)r.enabled=true;
    // rays from the eye to the right knuckles: what is hit first?
    Physics.queriesHitBackfaces=true;Physics.SyncTransforms();var tmp=new System.Collections.Generic.List<MeshCollider>();foreach(var mf in county.Car.GetComponentsInChildren<MeshFilter>(true)){if(!mf.sharedMesh||!mf.gameObject.activeInHierarchy)continue;var mc=mf.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;tmp.Add(mc);}
    Physics.SyncTransforms();var handR=arms.GetComponentsInChildren<Transform>(true).First(t=>t.name=="f_middle.01.R");var handL=arms.GetComponentsInChildren<Transform>(true).First(t=>t.name=="f_middle.01.L");
    foreach(var h in new[]{handR,handL}){var dirv=h.position-seat.position;foreach(var hit in Physics.RaycastAll(seat.position,dirv.normalized,dirv.magnitude+.02f).OrderBy(x=>x.distance))sb.AppendLine($"RAY to {h.name} ({dirv.magnitude:F3} m): hits {AnimationUtility.CalculateTransformPath(hit.collider.transform,county.Car)} at {hit.distance:F3} layer {hit.collider.gameObject.layer}");}
    foreach(var c in tmp)Object.DestroyImmediate(c);
    var torchVm=county.View.transform.Find("V19 viewmodel");var tv=torchVm?torchVm.GetComponentsInChildren<Renderer>().ToArray():new Renderer[0];foreach(var r in tv)r.enabled=false;
    var wheelR=county.SteeringWheel.GetComponentsInChildren<Renderer>(true);foreach(var r in wheelR)r.enabled=false;Shoot(cam,Path.Combine(dir,"occ-nowheel.png"),960,540);foreach(var r in wheelR)r.enabled=true;
    Shoot(cam,Path.Combine(dir,"occ-full.png"),960,540);foreach(var r in tv)r.enabled=true;
   }finally{Physics.queriesHitBackfaces=false;Object.DestroyImmediate(cam.gameObject);Restore(muted);}
   File.WriteAllText(Path.Combine(dir,"occlusion.txt"),sb.ToString());}
  // The real rim tube of the Tyble wheel, in the steering pivot's frame: ring radius, tube radius and depth per sector.
  public static void RimProbe(){Open();var w=county.SteeringWheel;var seat=county.DriverSeat;var sb=new System.Text.StringBuilder();var pts=new System.Collections.Generic.List<Vector3>();
   foreach(var mf in w.GetComponentsInChildren<MeshFilter>())if(mf.sharedMesh)foreach(var v in mf.sharedMesh.vertices)pts.Add(w.InverseTransformPoint(mf.transform.TransformPoint(v)));
   var ring=pts.Where(p=>{var r=new Vector2(p.x,p.y).magnitude;return r>.16f&&r<.25f;}).ToList();
   sb.AppendLine($"ring verts {ring.Count} of {pts.Count}");
   for(int sec=0;sec<12;sec++){float a0=sec*30-15,a1=sec*30+15;var inS=ring.Where(p=>{float a=Mathf.Atan2(p.x,p.y)*Mathf.Rad2Deg;if(a<-15)a+=360;return a>=a0&&a<a1;}).ToList();if(inS.Count==0){sb.AppendLine($"sector {sec*30}: none");continue;}
    var rs=inS.Select(p=>new Vector2(p.x,p.y).magnitude).ToList();var zs=inS.Select(p=>p.z).ToList();
    sb.AppendLine($"sector {sec*30,3} deg: n {inS.Count} r {rs.Min():F3}..{rs.Max():F3} mid {(rs.Min()+rs.Max())/2:F3} z {zs.Min():F3}..{zs.Max():F3} midz {(zs.Min()+zs.Max())/2:F3}");}
   sb.AppendLine($"pivot seat-local {seat.InverseTransformPoint(w.position)} axis {seat.InverseTransformDirection(w.forward)} up {seat.InverseTransformDirection(w.up)}");
   File.WriteAllText(Path.Combine(Work,"Audit","rim-probe.txt"),sb.ToString());}
  public static void ArmsAlone(){Open();var seat=county.DriverSeat;var dir=Path.Combine(Work,"Audit","drive20");var muted=MuteFeatures();
   var arms=seat.GetComponentsInChildren<ServiceDrivingHands>(true).First();var anim=arms.GetComponent<Animation>();anim.GetClip(arms.Clips[4]).SampleAnimation(arms.gameObject,0);
   var off=county.Car.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&!r.transform.IsChildOf(arms.transform)).ToArray();foreach(var r in off)r.enabled=false;
   var cam=new GameObject("alone cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~0;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.2f,.22f,.25f);
   try{cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(11,0,0));Shoot(cam,Path.Combine(dir,"alone-eye.png"),960,540);
    cam.transform.position=seat.TransformPoint(new Vector3(0,.2f,1.4f));cam.transform.LookAt(seat.TransformPoint(new Vector3(0,-.3f,.3f)));Shoot(cam,Path.Combine(dir,"alone-front.png"),960,540);
    var smr=arms.GetComponentInChildren<SkinnedMeshRenderer>();var sb=new System.Text.StringBuilder();sb.AppendLine("bones "+smr.bones.Length+" root "+smr.rootBone+" mesh "+smr.sharedMesh.name+" verts "+smr.sharedMesh.vertexCount+" submeshes "+smr.sharedMesh.subMeshCount+" mats "+smr.sharedMaterials.Length);
    var baked=new Mesh();smr.BakeMesh(baked,true);var vs=baked.vertices;int right=0,left=0;foreach(var v in vs){var w=seat.InverseTransformPoint(smr.transform.TransformPoint(v));if(w.x>.05f)right++;else if(w.x<-.05f)left++;}sb.AppendLine($"baked verts right {right} left {left}");File.WriteAllText(Path.Combine(dir,"alone.txt"),sb.ToString());
   }finally{foreach(var r in off)r.enabled=true;Object.DestroyImmediate(cam.gameObject);Restore(muted);}}
  static void DrivingArmsStage(){
   const string fbx="Assets/ServiceArt/V19/Arms/service_arms.fbx";
   var seat=county.DriverSeat;var old=seat.Find("V20 driving arms");if(old)Object.DestroyImmediate(old.gameObject);
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/ServiceArt/V19/Arms/V19 arms jacket.mat");
   var root=new GameObject("V20 driving arms").transform;root.SetParent(seat,false);
   var arms=(GameObject)PrefabUtility.InstantiatePrefab(model,root);arms.name="Arms — driving (PSX First Person Arms, Drillimpact, CC0)";
   foreach(var r in arms.GetComponentsInChildren<Renderer>()){r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;if(r is SkinnedMeshRenderer s)s.updateWhenOffscreen=true;}
   // Layer 10 is the cabin layer: the dashboard fill light only lights it, and the in-car camera renders it.
   foreach(var t in arms.GetComponentsInChildren<Transform>(true))t.gameObject.layer=10;root.gameObject.layer=10;
   var anim=arms.GetComponent<Animation>();if(!anim)anim=arms.AddComponent<Animation>();
   var clips=AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>c.name.StartsWith("Drive_")).OrderBy(c=>int.Parse(c.name.Substring(6))).ToArray();
   if(clips.Length<2)throw new System.Exception("arms FBX has no Drive_ clips");
   foreach(var c in clips)if(anim.GetClip(c.name)==null)anim.AddClip(c,c.name);
   anim.clip=null;anim.playAutomatically=false;
   Transform Bone(string n)=>arms.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
   arms.transform.localPosition=Vector3.zero;arms.transform.localRotation=Quaternion.identity;arms.transform.localScale=Vector3.one;
   var mid=clips[clips.Length/2];mid.SampleAnimation(arms,0);
   Vector3 L(string n)=>arms.transform.InverseTransformPoint(Bone(n).position);
   var eye=L("camera");var rootB=L("root");var shR=L("shoulder.R");var shL=L("shoulder.L");
   var up=(eye-rootB).normalized;var right=shR-shL;right-=up*Vector3.Dot(right,up);right.Normalize();var fwd=Vector3.Cross(right,up);
   var rig=Quaternion.LookRotation(fwd,up);arms.transform.localRotation=Quaternion.Inverse(rig);arms.transform.localPosition=-(arms.transform.localRotation*eye);
   mid.SampleAnimation(arms,0);
   var dh=arms.AddComponent<ServiceDrivingHands>();dh.Rig=anim;dh.Clips=clips.Select(c=>c.name).ToArray();
   var wl=county.SteeringWheel;log.AppendLine($"ARMS20 driving arms under {seat.name}: {clips.Length} wheel poses; right hand {seat.InverseTransformPoint(Bone("hand.R").position)} left {seat.InverseTransformPoint(Bone("hand.L").position)} wheel {seat.InverseTransformPoint(wl.position)}");
   EditorUtility.SetDirty(county);
  }
  // Driver's-eye renders at three wheel angles with the exterior body culled exactly as in the game.
  public static void DrivingPreview(){
   var dir=Path.Combine(Work,"Audit","drive20");Directory.CreateDirectory(dir);var seat=county.DriverSeat;var muted=MuteFeatures();
   var arms=seat.GetComponentsInChildren<ServiceDrivingHands>(true).FirstOrDefault();if(!arms){Restore(muted);return;}
   var anim=arms.GetComponent<Animation>();var wheel=county.SteeringWheel;var rest=wheel.localRotation;
   var cam=new GameObject("drive preview cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~(1<<8);
   var torchVm=county.View.transform.Find("V19 viewmodel");var hidden=torchVm?torchVm.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray():new Renderer[0];foreach(var r in hidden)r.enabled=false;
   try{
    if(county.Cockpit)county.Cockpit.SetActive(true);
    foreach(var k in new[]{0,2,4,6,8}){var clip=anim.GetClip(arms.Clips[k]);clip.SampleAnimation(arms.gameObject,0);float st=-1+k*.25f;wheel.localRotation=rest*Quaternion.Euler(0,0,-st*110);
     cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(11,0,0));Shoot(cam,Path.Combine(dir,$"unity-drive{k}.png"),960,540);
     cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(24,0,0));Shoot(cam,Path.Combine(dir,$"unity-drive{k}-down.png"),960,540);
     var wc=wheel.position;cam.transform.position=seat.TransformPoint(new Vector3(.75f,.05f,.3f));cam.transform.LookAt(wc);Shoot(cam,Path.Combine(dir,$"unity-drive{k}-side.png"),960,540);
     cam.transform.position=seat.TransformPoint(new Vector3(0f,.55f,.1f));cam.transform.LookAt(wc);Shoot(cam,Path.Combine(dir,$"unity-drive{k}-top.png"),960,540);
     if(k==4){var sb=new System.Text.StringBuilder();foreach(var t in arms.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("hand.")||t.name.StartsWith("forearm")||t.name.StartsWith("upper_arm")||t.name.StartsWith("shoulder"))sb.AppendLine($"{t.name} seat-local {seat.InverseTransformPoint(t.position)}");
      foreach(var r in arms.GetComponentsInChildren<Renderer>(true))sb.AppendLine($"RENDERER {r.name} enabled {r.enabled} bounds {r.bounds.center} {r.bounds.size} layer {r.gameObject.layer}");File.WriteAllText(Path.Combine(dir,"bones.txt"),sb.ToString());}}
   }finally{foreach(var r in hidden)r.enabled=true;wheel.localRotation=rest;Object.DestroyImmediate(cam.gameObject);Restore(muted);}
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void DogProbe(){Open();var sb=new System.Text.StringBuilder();var p0=county.Properties[0];var p4=county.Properties[4];
   foreach(var t in county.GetComponentsInChildren<Transform>(true))if(t.name.ToLower().Contains("dog")||t.name.ToLower().Contains("shepherd"))
    sb.AppendLine($"{AnimationUtility.CalculateTransformPath(t,county.transform)} active {t.gameObject.activeInHierarchy} pos {t.position} renderers {t.GetComponentsInChildren<Renderer>(true).Length} animator {(t.GetComponent<Animator>()?t.GetComponent<Animator>().runtimeAnimatorController?.name:"-")} anim {(t.GetComponent<Animation>()?"yes":"-")}");
   foreach(var p in new[]{p0,p4}){sb.AppendLine($"P{p.Index} door {p.Door.position} fwd {p.Door.forward} right {p.Door.right} panel {(p.DoorPanel?p.DoorPanel.position.ToString():"-")} swing {p.DoorSwing} interior {p.InteriorBounds.center}");}
   var res=county.GetComponentInChildren<ServiceResidents>(true);if(res){foreach(var r in new[]{res.Correll,res.Bell})if(r){sb.AppendLine($"RESIDENT {r.name} pos {r.transform.position} rot {r.transform.eulerAngles} path {AnimationUtility.CalculateTransformPath(r.transform,county.transform)}");foreach(var c in r.GetComponentsInChildren<Collider>(true))sb.AppendLine("  collider "+c.GetType().Name+" "+c.name);}}
   File.WriteAllText(Path.Combine(Work,"Audit","dog-probe.txt"),sb.ToString());}
 }
}
