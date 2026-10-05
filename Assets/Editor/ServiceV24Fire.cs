using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V24: the hearth fires get flames (Kenney Particle Pack, CC0). The cabins' fireplaces are iron fire-covers set in the
 // brick (part of each cabin's shell mesh), so the fire burns in a dark firebox laid over the cover.
 // FireProbe24: square-on pictures of each chimney (wide, then the cover) to find the opening.
 public static partial class ServiceV19Rebuild {
  public static void FireProbe24(){Open();var sb=new StringBuilder();bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   var dir=Path.Combine(Work,"Audit","fire24");Directory.CreateDirectory(dir);
   var n0=NeutralLight();var cam=AuditCam();var lamp=new GameObject("audit lamp").AddComponent<Light>();lamp.type=LightType.Point;lamp.range=9;lamp.intensity=2.5f;lamp.shadows=LightShadows.None;lamp.transform.SetParent(cam.transform,false);
   try{
    foreach(var h in county.GetComponentsInChildren<ServiceHearth>(true)){var root=h.transform;var p=county.Properties.First(x=>x.Index==h.Property);
     // wide: from 2.6 m out, 90 degrees, three headings (straight, 40 left, 40 right) to see the whole chimney breast
     foreach(var yaw in new[]{0f,-40f,40f}){cam.fieldOfView=90;cam.aspect=1.6f;var back=Quaternion.AngleAxis(yaw,Vector3.up)*(-root.forward);
      cam.transform.SetPositionAndRotation(root.position+Vector3.up*1.2f+root.forward*2.6f,Quaternion.LookRotation(back,Vector3.up));Shoot(cam,Path.Combine(dir,$"p{p.Index}-wide{yaw:+0;-0;0}.jpg"),800,500);}
     sb.AppendLine($"p{p.Index} root {root.position.x:F3} {root.position.y:F3} {root.position.z:F3} fwd {root.forward.x:F4} {root.forward.z:F4} right {root.right.x:F4} {root.right.z:F4}");
     // the face normal along the root's width at door height
     for(float lx=-1.2f;lx<=1.21f;lx+=.2f){var o=root.position+root.right*lx+Vector3.up*.9f+root.forward*1.2f;
      if(Physics.Raycast(o,-root.forward,out var hit,3f,~0,QueryTriggerInteraction.Ignore))sb.AppendLine($"  lx {lx:+0.0;-0.0} hit {hit.point.x:F3} {hit.point.y:F3} {hit.point.z:F3} n {hit.normal.x:F3} {hit.normal.y:F3} {hit.normal.z:F3} d {hit.distance:F2} {hit.collider.name}");}}
   } finally {EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
   File.WriteAllText(Path.Combine(dir,"fire24-probe.txt"),sb.ToString());}
  // The iron cover of each fireplace: Correll's (p0) measured on its chimney; Route 9 (p2) is the same Cabin1 model, so the
  // same spot through its own transform; Harrow's (p3) on its Cabin2 end.
  static Transform Shell(ServiceProperty p,string name)=>(p.Building?p.Building:p.transform).GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name==name).Select(r=>r.transform).FirstOrDefault();
  static bool Cover24(ServiceProperty p,out Vector3 centre,out Vector3 normal){centre=Vector3.zero;normal=Vector3.forward;
   var p0=county.Properties.First(x=>x.Index==0);var c0=Shell(p0,"Cabin1");if(!c0)return false;var wc=new Vector3(-69.994f,1.953f,88.694f);var wn=Vector3.right;
   if(p.Index==0){centre=wc;normal=wn;return true;}
   if(p.Index==2){var c2=Shell(p,"Cabin1");if(!c2)return false;centre=c2.TransformPoint(c0.InverseTransformPoint(wc));normal=c2.TransformDirection(c0.InverseTransformDirection(wn));return true;}
   if(p.Index==3){centre=new Vector3(85.21f,1.60f,145.607f);normal=Vector3.forward;return true;}
   return false;}
  // ---------------------------------------------------------------- the fire (stage Fire24)
  const string V24Fire="Assets/ServiceArt/V24/Fire";
  public static void Fire24(){Open();Fire24Stage();Save("fire24");}
  static Material FireMat(string name,string sprite,Color tint,float intensity){var path=$"{V24Fire}/{name}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Service/FireAdditive"));AssetDatabase.CreateAsset(m,path);}
   m.shader=Shader.Find("Service/FireAdditive");m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>($"{V24Fire}/Kenney/{sprite}.png"));m.SetColor("_TintColor",tint);m.SetFloat("_Intensity",intensity);m.renderQueue=3000;EditorUtility.SetDirty(m);return m;}
  // the recess of the iron cover around its centre: step out along the plane until the surface jumps (more than 2 cm) off it
  static float Edge24(Vector3 c,Vector3 n,Vector3 dir,float depth0,float max){float last=0;for(float k=.02f;k<=max;k+=.01f){var o=c+dir*k+n*.6f;
    if(!Physics.Raycast(o,-n,out var hit,1.2f,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs((.6f-hit.distance)-depth0)>.02f)return last;last=k;}return last;}
  static ParticleSystem Flames24(Transform parent,string name,Material mat,float w,float h,float size,float rate,float speed,float life,float alpha,Vector2 aspect,int seed){
   var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,-h*.5f+.04f,.025f);
   var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)seed;
   var main=ps.main;main.loop=true;main.playOnAwake=true;main.duration=4;main.prewarm=true;main.startLifetime=new ParticleSystem.MinMaxCurve(life*.6f,life);main.startSpeed=0;
   main.startSize3D=true;main.startSizeX=new ParticleSystem.MinMaxCurve(size*aspect.x*.75f,size*aspect.x);main.startSizeY=new ParticleSystem.MinMaxCurve(size*aspect.y*.55f,size*aspect.y*1.1f);main.startSizeZ=1;
   main.startRotation=new ParticleSystem.MinMaxCurve(-.12f,.12f);main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.7f,.38f,alpha),new Color(1,.5f,.2f,alpha));
   main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.maxParticles=60;
   var em=ps.emission;em.rateOverTime=rate;
   var sh=ps.shape;sh.enabled=true;sh.shapeType=ParticleSystemShapeType.Box;sh.scale=new Vector3(w*.78f,.03f,.004f);
   var vel=ps.velocityOverLifetime;vel.enabled=true;vel.space=ParticleSystemSimulationSpace.Local;vel.x=new ParticleSystem.MinMaxCurve(-.03f,.03f);vel.y=new ParticleSystem.MinMaxCurve(speed*.7f,speed);vel.z=new ParticleSystem.MinMaxCurve(0f,0f);
   var col=ps.colorOverLifetime;col.enabled=true;var g=new Gradient();
   g.SetKeys(new[]{new GradientColorKey(new Color(1,.85f,.55f),0),new GradientColorKey(new Color(1,.5f,.16f),.45f),new GradientColorKey(new Color(.55f,.1f,.03f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(.75f,.6f),new GradientAlphaKey(0,1)});col.color=g;
   var sz=ps.sizeOverLifetime;sz.enabled=true;sz.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.55f),new Keyframe(.3f,1),new Keyframe(1,.3f)));
   var nz=ps.noise;nz.enabled=true;nz.strength=new ParticleSystem.MinMaxCurve(.04f);nz.frequency=1.6f;nz.scrollSpeed=.8f;nz.damping=true;
   var r=go.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Billboard;r.alignment=ParticleSystemRenderSpace.Local;r.sharedMaterial=mat;r.sortMode=ParticleSystemSortMode.None;
   r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;r.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
   return ps;}
  static void Fire24Stage(){
   AssetDatabase.Refresh();
   foreach(var png in Directory.GetFiles(Path.Combine(Directory.GetParent(Application.dataPath).FullName,V24Fire,"Kenney"),"*.png")){var ap=$"{V24Fire}/Kenney/{Path.GetFileName(png)}";var ti=AssetImporter.GetAtPath(ap) as TextureImporter;if(!ti)continue;
    ti.textureType=TextureImporterType.Default;ti.sRGBTexture=true;ti.alphaSource=TextureImporterAlphaSource.None;ti.wrapMode=TextureWrapMode.Clamp;ti.mipmapEnabled=true;ti.maxTextureSize=512;ti.SaveAndReimport();}
   var soot=AssetDatabase.LoadAssetAtPath<Material>($"{V24Fire}/V24 firebox soot.mat");if(!soot){soot=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(soot,$"{V24Fire}/V24 firebox soot.mat");}
   soot.SetColor("_BaseColor",new Color(.014f,.010f,.008f,1));soot.SetFloat("_Cull",0);EditorUtility.SetDirty(soot);
   var tongueA=FireMat("V24 fire tongue a","flame_05",new Color(1f,.62f,.26f),1.7f);var tongueB=FireMat("V24 fire tongue b","flame_06",new Color(1f,.56f,.22f),1.5f);
   var body=FireMat("V24 fire body","flame_02",new Color(1f,.44f,.13f),1.1f);var embers=FireMat("V24 embers","fire_01",new Color(1f,.30f,.07f),.8f);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   try{
    foreach(var h in county.GetComponentsInChildren<ServiceHearth>(true)){var p=county.Properties.First(x=>x.Index==h.Property);
     var old=h.transform.Find("V24 fire");if(old)Object.DestroyImmediate(old.gameObject);
     if(!Cover24(p,out var c,out var n)){log.AppendLine($"FIRE24 p{p.Index}: no cover known");continue;}
     if(!Physics.Raycast(c+n*.6f,-n,out var ch,1.2f,~0,QueryTriggerInteraction.Ignore)){log.AppendLine($"FIRE24 p{p.Index}: nothing at the cover centre");continue;}
     float depth0=.6f-ch.distance;var plane=c+n*depth0;var right=Vector3.Cross(Vector3.up,n).normalized; // right as seen from the room
     float L=Edge24(plane,n,-right,0,1.2f),R=Edge24(plane,n,right,0,1.2f),T=Edge24(plane,n,Vector3.up,0,.9f),B=Edge24(plane,n,Vector3.down,0,.9f);
     // inside the cover's edges (the stone or iron surround stands proud of it and hides the seam); the cover's top corners are rounded
     float x0=-L+.035f,x1=R-.035f,y0=-B+.025f,y1=T-.05f;float w=x1-x0,hh=y1-y0;var centre=plane+right*((x0+x1)*.5f)+Vector3.up*((y0+y1)*.5f);
     if(w<.4f||hh<.35f){log.AppendLine($"FIRE24 p{p.Index}: opening too small ({w:F2} x {hh:F2}) - edges L{L:F2} R{R:F2} T{T:F2} B{B:F2}");continue;}
     var root=new GameObject("V24 fire").transform;root.SetParent(h.transform,true);root.SetPositionAndRotation(centre+n*.004f,Quaternion.LookRotation(n,Vector3.up));
     var back=GameObject.CreatePrimitive(PrimitiveType.Quad);back.name="Firebox (soot)";Object.DestroyImmediate(back.GetComponent<Collider>());back.transform.SetParent(root,false);back.transform.localRotation=Quaternion.Euler(0,180,0);back.transform.localScale=new Vector3(w,hh,1);
     var br=back.GetComponent<MeshRenderer>();br.sharedMaterial=soot;br.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;br.receiveShadows=false;
     var bed=GameObject.CreatePrimitive(PrimitiveType.Quad);bed.name="Embers";Object.DestroyImmediate(bed.GetComponent<Collider>());bed.transform.SetParent(root,false);bed.transform.localRotation=Quaternion.Euler(0,180,0);
     bed.transform.localScale=new Vector3(w*.96f,hh*.5f,1);bed.transform.localPosition=new Vector3(0,-hh*.5f+hh*.2f,.008f);var er=bed.GetComponent<MeshRenderer>();er.sharedMaterial=embers;er.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;er.receiveShadows=false;
     // the Kenney tongues fill only the middle of their sprite: a sprite about two thirds of the opening's height is a ~30 cm flame
     float size=Mathf.Clamp(hh*.68f,.3f,.55f);
     var f1=Flames24(root,"Flames",tongueA,w*.8f,hh,size,34,hh*.4f,.8f,1f,new Vector2(.85f,1.15f),11+p.Index);
     var f2=Flames24(root,"Flames 2",tongueB,w*.7f,hh,size*.85f,24,hh*.3f,.7f,.9f,new Vector2(.8f,1.1f),37+p.Index);
     var f3=Flames24(root,"Fire body",body,w*.85f,hh,size*1.25f,14,hh*.22f,1f,.75f,new Vector2(1f,.9f),71+p.Index);f3.transform.localPosition+=new Vector3(0,0,-.006f);
     if(h.Glow){h.Glow.transform.position=centre+Vector3.up*(-hh*.5f+hh*.3f)+n*.45f;}
     h.FireBox=root.gameObject;h.FireParticles=new[]{f1,f2,f3};h.Embers=er;EditorUtility.SetDirty(h);
     log.AppendLine($"FIRE24 p{p.Index}: cover {L+R:F2} x {T+B:F2} m (L{L:F2} R{R:F2} T{T:F2} B{B:F2}) at {plane:F3}, fire opening {w:F2} x {hh:F2} m at {centre:F3} facing {n:F2}");}
   } finally {if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
  }
  // each fire from the room: lit by the audit lamp, then in the dark with only its own light; particles run 1.6 s first
  public static void FireLook24(){Open();var dir=Path.Combine(Work,"Audit","fire24");Directory.CreateDirectory(dir);bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   var cam=AuditCam();var lamp=new GameObject("audit lamp").AddComponent<Light>();lamp.type=LightType.Point;lamp.range=9;lamp.intensity=1.2f;lamp.shadows=LightShadows.None;lamp.transform.SetParent(cam.transform,false);
   try{
    foreach(var h in county.GetComponentsInChildren<ServiceHearth>(true)){var p=county.Properties.First(x=>x.Index==h.Property);if(!h.FireBox)continue;h.FireBox.SetActive(true);
     foreach(var ps in h.FireParticles)if(ps)ps.Simulate(1.6f,true,true);
     var f=h.FireBox.transform;var n=f.forward;
     foreach(var (tag,dist,up,side) in new[]{("near",1.5f,.25f,0f),("room",3.2f,.75f,.35f),("side",2.0f,.4f,.9f)}){
      var right=Vector3.Cross(Vector3.up,n).normalized;cam.fieldOfView=55;cam.aspect=16f/9f;cam.transform.position=f.position+n*dist+Vector3.up*(1.55f-(f.position.y-p.InteriorBounds.min.y))*0+Vector3.up*up+right*side*dist;cam.transform.LookAt(f.position);
      lamp.enabled=true;Shoot(cam,Path.Combine(dir,$"p{p.Index}-fire-{tag}-lit.jpg"),960,540);
      var n0=RenderSettings.ambientIntensity;var amb=RenderSettings.ambientLight;RenderSettings.ambientIntensity=.15f;lamp.enabled=false;
      Shoot(cam,Path.Combine(dir,$"p{p.Index}-fire-{tag}-dark.jpg"),960,540);RenderSettings.ambientIntensity=n0;}}
   } finally {Object.DestroyImmediate(cam.gameObject);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}}
  public static void FireDoor24(){Open();var sb=new StringBuilder();bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   var dir=Path.Combine(Work,"Audit","fire24");Directory.CreateDirectory(dir);
   var n0=NeutralLight();var cam=AuditCam();var lamp=new GameObject("audit lamp").AddComponent<Light>();lamp.type=LightType.Point;lamp.range=9;lamp.intensity=2.5f;lamp.shadows=LightShadows.None;lamp.transform.SetParent(cam.transform,false);
   try{
    foreach(var h in county.GetComponentsInChildren<ServiceHearth>(true)){var p=county.Properties.First(x=>x.Index==h.Property);if(!Cover24(p,out var c,out var n)){sb.AppendLine($"p{p.Index} no cover");continue;}
     var right=Vector3.Cross(Vector3.up,-n).normalized; // camera right when looking along -n
     cam.fieldOfView=70;cam.aspect=1;cam.nearClipPlane=.05f;cam.transform.SetPositionAndRotation(c+n*1.3f,Quaternion.LookRotation(-n,Vector3.up));Shoot(cam,Path.Combine(dir,$"p{p.Index}-door.png"),640,640);
     sb.AppendLine($"p{p.Index} centre {c.x:F3} {c.y:F3} {c.z:F3} normal {n.x:F3} {n.y:F3} {n.z:F3} camRight {right.x:F3} {right.z:F3}  picture 640 px over {2*1.3f*Mathf.Tan(35*Mathf.Deg2Rad):F3} m at the plane");
     // depth relative to the centre's plane, 4 cm grid (+ = towards the room), columns left to right as in the picture
     for(float ly=.6f;ly>=-.6f;ly-=.04f){var row=new StringBuilder();for(float lx=-.7f;lx<=.7f;lx+=.04f){var o=c+right*lx+Vector3.up*ly+n*.8f;
       row.Append(Physics.Raycast(o,-n,out var hit,1.6f,~0,QueryTriggerInteraction.Ignore)?Mathf.Clamp(Mathf.RoundToInt((.8f-hit.distance)*100),-99,99).ToString().PadLeft(4):"   .");}
      sb.AppendLine($"  {ly:+0.00;-0.00} {row}");}}
   } finally {EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
   File.WriteAllText(Path.Combine(dir,"fire24-doors.txt"),sb.ToString());}
 }
}
