using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V25 playtest: "need it more insidious - like a person standing, then your character blinks" / "closing eyes and
 // opening to see the monster appear first then disappear after blinking". The figure is Renderpeople's Sophia (free
 // animated sample, credited since V16), held almost still in her idle; ServiceBlink shows her between two blinks.
 // Apparition25 puts her in the scene, hidden, as "V25 apparition"; ApparitionLook25 renders her where she will stand.
 public static partial class ServiceV19Rebuild {
  public static void Apparition25(){Open();Apparition25Stage();Save("apparition25");}
  static void Apparition25Stage(){
   var old=county.transform.Find("V25 apparition");if(old)Object.DestroyImmediate(old.gameObject);
   var fbx=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External/Renderpeople/Sophia/rp_sophia_animated_003_idling.fbx");if(!fbx){log.AppendLine("APPARITION25 no Sophia model");return;}
   var go=(GameObject)PrefabUtility.InstantiatePrefab(fbx,county.transform);go.name="V25 apparition";
   var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/ServiceArt/V16 Sophia.mat");if(mat)foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var ms=r.sharedMaterials;for(int i=0;i<ms.Length;i++)ms[i]=mat;r.sharedMaterials=ms;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/ServiceArt/V16 Sophia idle.anim");
   Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Assets/ServiceArt/V25"));
   var cpath="Assets/ServiceArt/V25/V25 apparition.controller";AssetDatabase.DeleteAsset(cpath);
   var an=go.GetComponentInChildren<Animator>(true);if(!an)an=go.AddComponent<Animator>();
   if(clip){var ctl=AnimatorController.CreateAnimatorControllerAtPathWithClip(cpath,clip);an.runtimeAnimatorController=ctl;}
   an.applyRootMotion=false;an.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var b=new Bounds(go.transform.position,Vector3.zero);foreach(var r in go.GetComponentsInChildren<Renderer>(true))b.Encapsulate(r.bounds);
   log.AppendLine($"APPARITION25 Sophia placed hidden: height {b.size.y:F2} m, {go.GetComponentsInChildren<Renderer>(true).Length} renderers, material {(mat?mat.name:"none")}, idle clip {(clip?clip.name+" "+clip.length.ToString("F1")+" s":"none")}, avatar {(an.avatar?an.avatar.name:"none")}");
   go.SetActive(false);EditorUtility.SetDirty(go);}
  // V25 furniture review (four reviewers over 127 interior sheets): one real fault - Harrow's painting over the hearth hung
  // so low it sat on the top of the fire opening (now a lit firebox). It goes up clear of the stone surround.
  public static void Props25(){Open();Props25Stage();Save("props25");}
  static void Props25Stage(){var p=county.Properties.First(x=>x.Index==3);
   var pic=p.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name.StartsWith("fire-painting"));var h=p.GetComponentInChildren<ServiceHearth>(true);
   if(!pic||!h||!h.FireBox){log.AppendLine("PROPS25 Harrow fire-painting or firebox missing");return;}
   var rs=pic.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   var fb=h.FireBox.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.StartsWith("Firebox")).Select(r=>r.bounds).FirstOrDefault();
   float want=fb.max.y+.42f;float lift=Mathf.Max(0,want-b.min.y);pic.position+=Vector3.up*lift;EditorUtility.SetDirty(pic);
   log.AppendLine($"PROPS25 Harrow fire-painting raised {lift:F2} m: bottom {b.min.y:F2} -> {b.min.y+lift:F2} (firebox top {fb.max.y:F2})");}
  // V25 the Correll branch: blood in Walter's hall (seen past him in the doorway on night two) and a drag trail further
  // in. No blood decal is licensed in the project, so the shapes are Kenney Particle Pack smoke blotches (CC0) cut out at
  // their alpha and coloured a dark wet red. Hidden; ServiceDirector shows it on the branch nights.
  public static void Blood25(){Open();Blood25Stage();Save("blood25");}
  static void Blood25Stage(){var p=county.Properties.First(x=>x.Index==0);var old=p.transform.Find("V25 blood");if(old)Object.DestroyImmediate(old.gameObject);
   AssetDatabase.Refresh();
   foreach(var n in new[]{"smoke_03","smoke_04","smoke_06","smoke_07","smoke_08"}){var ti=AssetImporter.GetAtPath($"Assets/ServiceArt/V25/Blood/Kenney/{n}.png") as TextureImporter;if(!ti)continue;ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=true;ti.wrapMode=TextureWrapMode.Clamp;ti.maxTextureSize=512;ti.SaveAndReimport();}
   Material Mat(string sprite){var path=$"Assets/ServiceArt/V25/Blood/V25 blood {sprite}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
    m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/ServiceArt/V25/Blood/Kenney/{sprite}.png"));m.SetColor("_BaseColor",new Color(.24f,.018f,.012f,1));m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.32f);m.EnableKeyword("_ALPHATEST_ON");
    m.SetFloat("_Smoothness",.5f);m.SetFloat("_Metallic",0);m.SetFloat("_Cull",0);m.renderQueue=2450;EditorUtility.SetDirty(m);return m;}
   var holder=new GameObject("V25 blood").transform;holder.SetParent(p.transform,false);
   var inward=p.Inward;inward.y=0;inward.Normalize();var side=Vector3.Cross(Vector3.up,inward);var door=p.Door.position;int placed=0;
   // a pool behind where he stands in the doorway, then a drag trail on into the house, drifting to one side
   // on the bare porch boards at your feet and across the threshold (the hall runner is dark red - blood vanished on it),
   // then a short smear in past where he stands
   var spots=new[]{(-.75f,.15f,.95f,.85f,"smoke_08"),(-1.35f,-.25f,.42f,.5f,"smoke_03"),(.05f,.1f,.6f,.9f,"smoke_07"),(.85f,.22f,.5f,.95f,"smoke_04"),(1.6f,.3f,.36f,.7f,"smoke_06")};
   foreach(var (inn,lat,w,len,spr) in spots){var at=door+inward*inn+side*lat;
    if(!Physics.Raycast(at+Vector3.up*1.2f,Vector3.down,out var hit,2.5f,~0,QueryTriggerInteraction.Ignore)){log.AppendLine($"BLOOD25 nothing under {at}");continue;}
    var q=GameObject.CreatePrimitive(PrimitiveType.Quad);q.name="Blood "+(++placed);Object.DestroyImmediate(q.GetComponent<Collider>());q.transform.SetParent(holder,true);
    q.transform.position=hit.point+hit.normal*.016f;/* above the hall rug (lifted 7 mm in V23) */q.transform.rotation=Quaternion.LookRotation(-hit.normal,inward)*Quaternion.Euler(0,0,Random.Range(-14f,14f));q.transform.localScale=new Vector3(w,len,1);
    var r=q.GetComponent<MeshRenderer>();r.sharedMaterial=Mat(spr);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=true;}
   holder.gameObject.SetActive(false);log.AppendLine($"BLOOD25 {placed} blood shapes in Walter's hall (hidden; shown on the Correll branch nights)");}
  // the blood, lit by a torch from the doorstep (for review)
  public static void BloodLook25(){Open();var p=county.Properties.First(x=>x.Index==0);var b=p.transform.Find("V25 blood");if(!b)return;b.gameObject.SetActive(true);
   var dir=Path.Combine(Work,"Audit","v25scares");Directory.CreateDirectory(dir);var cam=AuditCam();cam.fieldOfView=62;cam.aspect=16f/9f;
   var lamp=new GameObject("torch").AddComponent<Light>();lamp.type=LightType.Spot;lamp.spotAngle=60;lamp.range=12;lamp.intensity=6;lamp.transform.SetParent(cam.transform,false);
   var res=Object.FindAnyObjectByType<ServiceResidents>(FindObjectsInactive.Include);bool walterWas=res&&res.Correll&&res.Correll.activeSelf;if(res&&res.Correll)res.Correll.SetActive(false);var n0=NeutralLight();
   try{var inward=p.Inward;inward.y=0;inward.Normalize();var floor=p.Door.position+inward*2.2f;
    cam.transform.position=p.Door.position+inward*.2f+Vector3.up*2.3f;cam.transform.LookAt(floor+Vector3.down*.9f);Shoot(cam,Path.Combine(dir,"blood-hall.jpg"),960,540);
    EndNeutral(n0);n0=NeutralLight();RenderSettings.ambientIntensity=.12f;cam.transform.position=p.Door.position-inward*1.7f+Vector3.up*1.6f;cam.transform.LookAt(p.Door.position+inward*.3f+Vector3.down*1.5f);Shoot(cam,Path.Combine(dir,"blood-doorstep.jpg"),960,540);}
   finally{EndNeutral(n0);
    // straight down over the doorway, neutral light, and what each shape is
    var n1=NeutralLight();cam.transform.position=p.Door.position+Vector3.up*3.2f;cam.transform.rotation=Quaternion.LookRotation(Vector3.down,p.Inward);cam.fieldOfView=75;Shoot(cam,Path.Combine(dir,"blood-top.jpg"),960,540);EndNeutral(n1);
    var sb=new System.Text.StringBuilder();foreach(var r in b.GetComponentsInChildren<MeshRenderer>(true)){var tx=r.sharedMaterial.GetTexture("_BaseMap") as Texture2D;var ti=tx?AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tx)) as TextureImporter:null;
     sb.AppendLine($"{r.name} at {r.transform.position:F3} fwd {r.transform.forward:F2} scale {r.transform.lossyScale:F2} mat {r.sharedMaterial.name} tex {(tx?tx.name+" "+tx.format:"-")} alphaSrc {(ti?ti.alphaSource.ToString():"-")} hasAlpha {(ti?ti.DoesSourceTextureHaveAlpha().ToString():"-")} clip {r.sharedMaterial.GetFloat("_AlphaClip")} cutoff {r.sharedMaterial.GetFloat("_Cutoff")} kw {string.Join(",",r.sharedMaterial.shaderKeywords)}");}
    File.WriteAllText(Path.Combine(dir,"blood.txt"),sb.ToString()+"door "+p.Door.position+" inward "+p.Inward);
    Object.DestroyImmediate(cam.gameObject);b.gameObject.SetActive(false);if(res&&res.Correll)res.Correll.SetActive(walterWas);}}
  // V25 "cook a running animation that looks fair": Walter ran after you on his walk cycle played 2.6x. The residents'
  // controller gets a Run state on the Starter Assets Run_N (Unity, humanoid, already in the project with the walk), so
  // Walter runs on his own rig; ServiceWalter blends walk -> run with his speed. RunLook25 renders the stride.
  const string ResidentCtl="Assets/ServiceArt/V17/Prefabs/V17 Resident.controller";
  static AnimationClip RunClip()=>AssetDatabase.LoadAllAssetsAtPath("Assets/ServiceArt/V17/Anim/Locomotion--Run_N.anim.fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview"));
  public static void RunAnim25(){Open();var ctl=AssetDatabase.LoadAssetAtPath<AnimatorController>(ResidentCtl);var run=RunClip();if(!ctl||!run){log.AppendLine("RUN25 controller or clip missing");Save("run25");return;}
   var sm=ctl.layers[0].stateMachine;var st=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name=="Run");if(!st)st=sm.AddState("Run",new Vector3(300,260,0));st.motion=run;st.speed=1;st.writeDefaultValues=true;
   EditorUtility.SetDirty(ctl);AssetDatabase.SaveAssets();
   log.AppendLine($"RUN25 Run state on {ResidentCtl}: {run.name} {run.length:F2} s, human {run.isHumanMotion}, loop {run.isLooping}, average speed {run.averageSpeed.magnitude:F2} m/s (apparent {run.apparentSpeed:F2})");Save("run25");}
  public static void RunLook25(){Open();var fbx=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ServiceArt/V17/Characters/ElderlyMan/ElderlyMan.fbx");var ctl=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ResidentCtl);
   var dir=Path.Combine(Work,"Audit","v25scares");Directory.CreateDirectory(dir);var go=(GameObject)PrefabUtility.InstantiatePrefab(fbx);go.transform.position=new Vector3(-40,0,60);
   var res=Object.FindAnyObjectByType<ServiceResidents>(FindObjectsInactive.Include);if(res&&res.Correll){foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var src=res.Correll.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x=>x.name==r.name);if(src)r.sharedMaterials=src.sharedMaterials;}}
   var an=go.GetComponentInChildren<Animator>();an.runtimeAnimatorController=ctl;an.applyRootMotion=false;var t=county.GetComponentInChildren<Terrain>();var p0=go.transform.position;p0.y=t.SampleHeight(p0)+t.transform.position.y;go.transform.position=p0;
   var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=40;cam.aspect=1f;
   try{int k=0;foreach(var ph in new[]{0f,.17f,.33f,.5f,.67f,.83f}){an.Play("Run",0,ph);an.Update(0);an.Update(.001f);
     cam.transform.position=p0+go.transform.right*3.2f+Vector3.up*1.1f;cam.transform.LookAt(p0+Vector3.up*.95f);Shoot(cam,Path.Combine(dir,$"run-side-{k}.jpg"),360,360);
     cam.transform.position=p0+go.transform.forward*3.6f+Vector3.up*1.3f;cam.transform.LookAt(p0+Vector3.up*.95f);Shoot(cam,Path.Combine(dir,$"run-front-{k}.jpg"),360,360);k++;}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(go);}}
  // a look at her where she stands at Morrow's landing (the desk end), lit and in the dark
  public static void ApparitionLook25(){Open();var go=county.transform.Find("V25 apparition");if(!go)return;var p=county.Properties.First(x=>x.Index==5);
   var dir=Path.Combine(Work,"Audit","v25scares");Directory.CreateDirectory(dir);go.gameObject.SetActive(true);
   var at=p.TableApproach.position;var toStairs=p.Door.position-at;toStairs.y=0;go.position=at;go.rotation=Quaternion.LookRotation(toStairs.normalized);
   var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=60;cam.aspect=16f/9f;
   try{foreach(var (tag,d) in new[]{("near",2.2f),("far",6f)}){cam.transform.position=at+toStairs.normalized*d+Vector3.up*1.6f;cam.transform.LookAt(at+Vector3.up*1.2f);Shoot(cam,Path.Combine(dir,$"apparition-{tag}.jpg"),960,540);}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);go.gameObject.SetActive(false);}}
 }
}
