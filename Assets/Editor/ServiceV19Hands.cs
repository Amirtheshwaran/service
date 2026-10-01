using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V19 first-person hands: PSX First Person Arms (Drillimpact, CC0) posed in Blender (Sources/V19/arms_pose.py)
 // with a corduroy jacket sleeve, holding the Poly Haven small plastic torch (CC0) that carries the flashlight.
 public static partial class ServiceV19Rebuild {
  public static bool TorchLensAtPlusZ=true; // catalog front +Z: the lens (glass + reflector) is at +Z
  // Where the fist sits on screen (normalised device coordinates, x right, y up) and how far in front of the eye
  // (camera-space metres; the viewmodel is scaled 0.3 toward the eye, so ~0.135 reads as a real arm's length).
  static readonly Vector2 HandScreen=new Vector2(.5f,-.56f);const float HandDepth=.12f;
  public static void HandsPreview(){
   var dir=Path.Combine(Work,"Audit","hands");Directory.CreateDirectory(dir);var cam=county.View;var muted=MuteFeatures();var tmp=new System.Collections.Generic.List<GameObject>();
   var vm=cam.transform.Find("V19 viewmodel");if(!vm){Restore(muted);return;}var lp=vm.localPosition;var lr=vm.localRotation;var ls=vm.localScale;
   var pc=new GameObject("Hands preview camera").AddComponent<Camera>();tmp.Add(pc.gameObject);pc.CopyFrom(cam);pc.transform.SetPositionAndRotation(new Vector3(0,600,0),Quaternion.identity);pc.clearFlags=CameraClearFlags.SolidColor;pc.backgroundColor=new Color(.04f,.05f,.06f);
   pc.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
   try{
    vm.SetParent(pc.transform,false);vm.localPosition=lp;vm.localRotation=lr;vm.localScale=ls;
    var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",new Color(.5f,.5f,.48f));
    var wall=GameObject.CreatePrimitive(PrimitiveType.Quad);tmp.Add(wall);wall.transform.position=pc.transform.position+Vector3.forward*3.2f;wall.transform.rotation=Quaternion.identity;wall.transform.localScale=new Vector3(14,9,1);wall.GetComponent<Renderer>().sharedMaterial=m;
    var floor=GameObject.CreatePrimitive(PrimitiveType.Quad);tmp.Add(floor);floor.transform.position=pc.transform.position+new Vector3(0,-1.6f,2);floor.transform.rotation=Quaternion.Euler(90,0,0);floor.transform.localScale=new Vector3(14,9,1);floor.GetComponent<Renderer>().sharedMaterial=m;
    var fill=new GameObject("preview fill").AddComponent<Light>();tmp.Add(fill.gameObject);fill.type=LightType.Directional;fill.intensity=.25f;fill.transform.rotation=Quaternion.Euler(35,-20,0);
    if(county.Flashlight)county.Flashlight.enabled=true;
    var anim=vm.GetComponentInChildren<Animation>();var hold0=anim?anim.GetClip("Hold"):null;if(hold0)hold0.SampleAnimation(anim.gameObject,0);
    Shoot(pc,Path.Combine(dir,"view-hold.png"),1280,720);
    {var torchT=vm.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.StartsWith("Torch"));if(torchT){var tcen=BoundsOf(torchT.gameObject).center;
     var cc=new GameObject("Hands close camera").AddComponent<Camera>();tmp.Add(cc.gameObject);cc.CopyFrom(pc);cc.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cc.nearClipPlane=.002f;
     cc.transform.position=pc.transform.position;cc.transform.LookAt(tcen);cc.fieldOfView=20;Shoot(cc,Path.Combine(dir,"zoom-hold.png"),960,720);
     foreach(var (tag,off) in new[]{("side-in",-pc.transform.right*.09f+pc.transform.up*.02f),("side-out",pc.transform.right*.09f+pc.transform.up*.03f),("top",pc.transform.up*.09f+pc.transform.forward*.01f),("front",pc.transform.forward*.11f+pc.transform.up*.02f)}){cc.transform.position=tcen+off;cc.transform.LookAt(tcen);cc.fieldOfView=45;Shoot(cc,Path.Combine(dir,$"{tag}-hold.png"),720,540);}}}var knock=anim?anim.GetClip("Knock"):null;
    if(knock){knock.SampleAnimation(anim.gameObject,17f/30f);Shoot(pc,Path.Combine(dir,"view-knock.png"),1280,720);var reach=anim.GetClip("Reach");if(reach){reach.SampleAnimation(anim.gameObject,reach.length*.55f);Shoot(pc,Path.Combine(dir,"view-reach.png"),1280,720);}var hold=anim.GetClip("Hold");if(hold)hold.SampleAnimation(anim.gameObject,0);}
   }finally{vm.SetParent(cam.transform,false);vm.localPosition=lp;vm.localRotation=lr;vm.localScale=ls;foreach(var g in tmp)Object.DestroyImmediate(g);Restore(muted);}
  }

  public static void Hands(){Open();HandsStage();Save("hands");}
  public static void HandsLook(){Open();HandsPreview();}
  static void HandsStage(){
   const string dir="Assets/ServiceArt/V19/Arms";var fbx=dir+"/service_arms.fbx";AssetDatabase.Refresh();
   var mi=(ModelImporter)AssetImporter.GetAtPath(fbx);
   mi.animationType=ModelImporterAnimationType.Legacy;mi.materialImportMode=ModelImporterMaterialImportMode.None;mi.importCameras=mi.importLights=false;mi.importAnimation=true;mi.SaveAndReimport();
   var clips=mi.defaultClipAnimations;foreach(var c in clips){var n=c.name.Contains("|")?c.name.Substring(c.name.LastIndexOf('|')+1):c.name;c.name=n;c.wrapMode=n=="Hold"?WrapMode.Loop:WrapMode.Once;c.loopTime=n=="Hold";}
   mi.clipAnimations=clips;mi.SaveAndReimport();log.AppendLine("HANDS clips "+string.Join(",",clips.Select(c=>c.name)));
   var matPath=dir+"/V19 arms jacket.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
   mat.SetTexture("_BaseMap",PropTex(dir+"/arms_jacket.png",false,true,512));mat.SetFloat("_Smoothness",.18f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);

   var view=county.View.transform;var old=view.Find("V19 viewmodel");
   // The flashlight rides on the torch inside the viewmodel: lift it out before the old viewmodel is rebuilt.
   if(county.Flashlight&&old&&county.Flashlight.transform.IsChildOf(old))county.Flashlight.transform.SetParent(view,false);
   if(old)Object.DestroyImmediate(old.gameObject);
   if(!county.Flashlight){var torchLight=new GameObject("Hand torch").AddComponent<Light>();torchLight.transform.SetParent(view,false);torchLight.type=LightType.Spot;torchLight.color=new Color(.93f,.91f,.8f);torchLight.intensity=4.2f;torchLight.renderMode=LightRenderMode.ForcePixel;county.Flashlight=torchLight;log.AppendLine("HANDS flashlight recreated (it had been destroyed with an earlier viewmodel)");}
   foreach(var h in view.GetComponentsInChildren<ServiceHands>(true))Object.DestroyImmediate(h.gameObject);
   var vm=new GameObject("V19 viewmodel").transform;vm.SetParent(view,false);vm.localScale=Vector3.one*.3f;// scaled toward the eye so the hands never pass through walls
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);var arms=(GameObject)PrefabUtility.InstantiatePrefab(model,vm);arms.name="Arms — PSX First Person Arms (Drillimpact, CC0)";
   foreach(var r in arms.GetComponentsInChildren<Renderer>()){r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;if(r is SkinnedMeshRenderer s){s.updateWhenOffscreen=true;}}
   var anim=arms.GetComponent<Animation>();if(!anim)anim=arms.AddComponent<Animation>();
   foreach(var c in AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")))if(anim.GetClip(c.name)==null)anim.AddClip(c,c.name);
   var hold=anim.GetClip("Hold");anim.clip=hold;anim.playAutomatically=true;
   Transform Bone(string n)=>arms.GetComponentsInChildren<Transform>(true).First(t=>t.name==n);
   arms.transform.localPosition=Vector3.zero;arms.transform.localRotation=Quaternion.identity;arms.transform.localScale=Vector3.one;
   hold.SampleAnimation(arms,0);
   Vector3 L(string n)=>arms.transform.InverseTransformPoint(Bone(n).position);
   var eye=L("camera");var root=L("root");var shR=L("shoulder.R");var shL=L("shoulder.L");
   var up=(eye-root).normalized;var right=shR-shL;right-=up*Vector3.Dot(right,up);right.Normalize();var fwd=Vector3.Cross(right,up);
   var rig=Quaternion.LookRotation(fwd,up);arms.transform.localRotation=Quaternion.Inverse(rig);arms.transform.localPosition=-(arms.transform.localRotation*eye);
   hold.SampleAnimation(arms,0);
   var handR=Bone("hand.R");var local=vm.InverseTransformPoint(handR.position);log.AppendLine($"HANDS right hand in eye space {local} (target ~(0.27,-0.28,0.40))");
   // Torch seated THROUGH the closed right fist. The arms FBX carries grip markers parented to hand.R
   // (Sources/V19/arms_pose.py): the fist's centre line, a point 10 cm toward the lens, and a point toward the back
   // of the hand. The fingers were posed to wrap a 22 mm-radius body, so the torch is sized to that.
   var torchPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ServiceArt/V19/Props/small_plastic_torch/small_plastic_torch.prefab");
   var torch=(GameObject)PrefabUtility.InstantiatePrefab(torchPrefab,vm);torch.name="Torch — small plastic torch (Poly Haven, CC0)";
   Object.DestroyImmediate(torch.GetComponent<Collider>());foreach(var r in torch.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
   Transform Mk(string n)=>arms.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==n);
   var mg=Mk("torch_grip");var ml=Mk("torch_lens");var mt=Mk("torch_top");
   if(!mg||!ml||!mt)throw new System.Exception("arms FBX has no torch_grip / torch_lens / torch_top markers");
   Vector3 lensDir=(ml.position-mg.position).normalized,topDir=mt.position-mg.position;topDir-=lensDir*Vector3.Dot(topDir,lensDir);topDir.Normalize();
   float armsScale=(ml.position-mg.position).magnitude/.1f;
   torch.transform.rotation=Quaternion.identity;var tb=BoundsOf(torch);float thick=Mathf.Min(tb.size.x,tb.size.y);
   torch.transform.localScale*=(.044f*armsScale)/Mathf.Max(thick,1e-4f);
   torch.transform.rotation=Quaternion.LookRotation(lensDir*(TorchLensAtPlusZ?1:-1),topDir);
   var tc=BoundsOf(torch).center;torch.transform.position+=(mg.position+lensDir*(.04f*armsScale))-tc;
   var tlen=BoundsOf(torch);float halfLen=Vector3.Dot(tlen.extents,new Vector3(Mathf.Abs(lensDir.x),Mathf.Abs(lensDir.y),Mathf.Abs(lensDir.z)));
   torch.transform.SetParent(handR,true);
   var lamp=county.Flashlight;if(lamp){lamp.transform.SetParent(torch.transform,true);lamp.transform.position=tlen.center+lensDir*(halfLen+.002f);lamp.transform.rotation=Quaternion.LookRotation(lensDir,topDir);
    lamp.type=LightType.Spot;lamp.spotAngle=46;lamp.innerSpotAngle=16;lamp.range=24;lamp.shadows=LightShadows.Soft;lamp.shadowNearPlane=.1f;log.AppendLine($"HANDS flashlight on torch lens, intensity {lamp.intensity}");}
   county.View.nearClipPlane=.015f;
   // Frame it like Fears to Fathom: the torch hand low in the right third of the screen with the lens aimed just
   // left of and above centre, so the beam lands where the player looks. Rotate the whole viewmodel about the eye
   // until the lens axis points there, then slide it so the fist sits at the target spot.
   Physics.SyncTransforms();
   var lensAxis=view.InverseTransformDirection(lensDir).normalized;
   var want=new Vector3(-.05f,.035f,1).normalized;
   vm.localRotation=Quaternion.FromToRotation(lensAxis,want)*vm.localRotation;
   float vfov=county.View.fieldOfView,aspect=16f/9f;float hx=Mathf.Tan(vfov*.5f*Mathf.Deg2Rad)*aspect,hy=Mathf.Tan(vfov*.5f*Mathf.Deg2Rad);
   float depth=HandDepth;var fistNow=view.InverseTransformPoint(mg.position);
   var fistWant=new Vector3(HandScreen.x*hx*depth,HandScreen.y*hy*depth,depth);vm.localPosition+=fistWant-fistNow;
   log.AppendLine($"HANDS framed: vfov {vfov:F0}, lens axis {lensAxis} -> {want}, fist {fistNow} -> {fistWant}");
   var hands=arms.AddComponent<ServiceHands>();hands.Rig=anim;hands.Sway=vm;hands.Torch=torch.transform;
   EditorUtility.SetDirty(county);
   HandsPreview();
  }
 }
}
