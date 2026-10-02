using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V20 navigation, part 1 (world): a numbered mailbox at every drive mouth (Blue mailbox on a wooden stand,
 // Rylae Shylna, CC-BY) with a reflective 911 address plate and a hand-lettered name strip, plus the County Route 9
 // centre line for the runtime guide. Positions come from the road centre lines and each property's Gate, so this
 // runs after any road work.
 public static partial class ServiceV19Rebuild {
  const string MailDir="Assets/ServiceArt/V20/Mailbox";
  static readonly string[] HouseNumber={"214","77","1","236","91","108"};
  public static float MailYaw=90; // the model's door is on its local X axis; turn it to face across the road
  public static void Guide(){Open();GuideStage();Save("guide");GuideShots();}
  static List<Vector3> GuideLine(Vector2[] path,float step){return CatmullRom(path,step).Select(p=>new Vector3(p.x,0,p.y)).ToList();}
  static void GuideNearest(List<Vector3> line,Vector3 at,out Vector3 point,out Vector3 tangent,out float arc){point=line[0];tangent=Vector3.forward;arc=0;float best=float.MaxValue,run=0;var q=at;q.y=0;
   for(int i=1;i<line.Count;i++){var a=line[i-1];var b=line[i];var ab=b-a;float len=ab.magnitude;if(len<1e-4f)continue;float t=Mathf.Clamp01(Vector3.Dot(q-a,ab)/(len*len));var c=a+ab*t;float dd=(q-c).sqrMagnitude;if(dd<best){best=dd;point=c;tangent=ab/len;arc=run+t*len;}run+=len;}}
  static float GuideLength(List<Vector3> line){float l=0;for(int i=1;i<line.Count;i++)l+=Vector3.Distance(line[i-1],line[i]);return l;}
  static Material MailMat(){
   var path=MailDir+"/Mailbox.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.SetTexture("_BaseMap",PropTex(MailDir+"/mailbox_basecolor.png",false,true,1024));m.SetColor("_BaseColor",Color.white);
   var n=PropTex(MailDir+"/mailbox_normal.png",true,false,1024);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
   var ms=PropTex(MailDir+"/mailbox_metallic_smoothness.png",false,false,1024);if(ms){m.SetTexture("_MetallicGlossMap",ms);m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",.85f);m.SetFloat("_SmoothnessTextureChannel",0);}
   var oc=PropTex(MailDir+"/mailbox_occlusion.png",false,false,1024);if(oc){m.SetTexture("_OcclusionMap",oc);m.EnableKeyword("_OCCLUSIONMAP");}
   EditorUtility.SetDirty(m);return m;}
  static Material PlateMat(string tex,float smooth,float glow){var path=$"{MailDir}/{Path.GetFileNameWithoutExtension(tex)}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   var t=PropTex($"{MailDir}/{tex}",false,true,512);m.SetTexture("_BaseMap",t);m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);
   // retro-reflective sheeting reads at night: a faint emission of the plate's own colours
   m.EnableKeyword("_EMISSION");m.SetTexture("_EmissionMap",t);m.SetColor("_EmissionColor",new Color(glow,glow,glow));m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;EditorUtility.SetDirty(m);return m;}
  static void GuideStage(){
   AssetDatabase.Refresh();Physics.SyncTransforms();
   var imp=(ModelImporter)AssetImporter.GetAtPath(MailDir+"/MailBox_J_low.fbx");if(imp){imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importAnimation=false;imp.importCameras=imp.importLights=false;imp.SaveAndReimport();}
   var fbx=AssetDatabase.LoadAssetAtPath<GameObject>(MailDir+"/MailBox_J_low.fbx");if(!fbx)throw new System.Exception("mailbox FBX missing");
   var mat=MailMat();
   var main=GuideLine(MainRoad,2f);var late=GuideLine(LateRoadPath,2f);float mainLen=GuideLength(main);
   var lateT=county.LateRoad?county.LateRoad.transform:null;
   if(lateT){var old=lateT.Find("V20 late route");if(old)Object.DestroyImmediate(old.gameObject);var root=new GameObject("V20 late route").transform;root.SetParent(lateT,false);
    var pts=new List<Transform>();float acc=0;for(int i=0;i<late.Count;i++){if(i>0)acc+=Vector3.Distance(late[i-1],late[i]);if(i>0&&i<late.Count-1&&acc<12)continue;acc=0;var t=new GameObject("Route "+pts.Count.ToString("00")).transform;t.SetParent(root,false);var w=late[i];w.y=GroundAt(w);t.position=w;pts.Add(t);}
    county.LateRoute=pts.ToArray();log.AppendLine($"GUIDE late route {pts.Count} points");}
   foreach(var p in county.Properties){
    var holder=p.Index==2&&lateT?lateT:p.transform;
    foreach(var t in new[]{p.transform,lateT}){if(!t)continue;foreach(var o in t.Cast<Transform>().Where(x=>x.name.StartsWith("V20 mailbox — "+HouseNumber[p.Index]+" ")).ToArray())Object.DestroyImmediate(o.gameObject);}
    var line=p.Index==2?late:main;GuideNearest(line,p.Gate.position,out var c,out var tan,out var arc);
    var right=new Vector3(tan.z,0,-tan.x);var g=p.Gate.position;g.y=0;int side=Vector3.Dot(g-c,right)>=0?1:-1;var sideV=right*side;
    // just past the drive mouth for northbound traffic, outside the pull-off flare (half-width 2.9 m): a car cutting
    // the corner into the drive comes from the near side, so it never meets the box
    var at=c+sideV*5.4f+tan*4.6f;at.y=GroundAt(at);
    var box=new GameObject($"V20 mailbox — {HouseNumber[p.Index]} (Rylae Shylna, CC-BY)").transform;box.SetParent(holder,true);box.position=at;box.rotation=Quaternion.LookRotation(-sideV,Vector3.up);
    var model=(GameObject)PrefabUtility.InstantiatePrefab(fbx,box);model.name="Mailbox model";model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.Euler(0,MailYaw,0);model.transform.localScale=Vector3.one;
    foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
    // a rural mailbox on its post: ~1.2 m to the top of the box, standing on the ground
    var b=BoundsOf(model);float k=1.2f/Mathf.Max(b.size.y,1e-3f);model.transform.localScale=Vector3.one*k;b=BoundsOf(model);model.transform.position+=new Vector3(at.x-b.center.x,at.y-b.min.y,at.z-b.center.z);
    b=BoundsOf(model);var col=box.gameObject.AddComponent<BoxCollider>();col.center=box.InverseTransformPoint(new Vector3(b.center.x,b.min.y+.55f,b.center.z));col.size=new Vector3(.16f,1.1f,.16f);
    // 911 plate on the post, square to the road so it reads from either direction (printed on both faces)
    var plateMat=PlateMat($"plate_{p.Index}.png",.6f,.04f);
    var plate=new GameObject("Address plate "+HouseNumber[p.Index]).transform;plate.SetParent(box,false);plate.position=new Vector3(b.center.x,b.min.y+.74f,b.center.z);plate.rotation=Quaternion.LookRotation(-tan,Vector3.up);
    Piece(PrimitiveType.Quad,"Plate front",plate,new Vector3(0,0,-.012f),new Vector3(.34f,.17f,1),Quaternion.Euler(0,180,0),plateMat,false);
    Piece(PrimitiveType.Quad,"Plate back",plate,new Vector3(0,0,.012f),new Vector3(.34f,.17f,1),Quaternion.identity,plateMat,false);
    // hand-lettered name strip on the box's road side
    var nameMat=PlateMat($"name_{p.Index}.png",.2f,.012f);
    var strip=Piece(PrimitiveType.Quad,"Name strip",box,Vector3.zero,new Vector3(.30f,.075f,1),Quaternion.identity,nameMat,false).transform;
    float half=Mathf.Abs(Vector3.Dot(b.extents,new Vector3(Mathf.Abs(sideV.x),0,Mathf.Abs(sideV.z))));strip.position=new Vector3(b.center.x,b.max.y-.16f,b.center.z)-sideV*(half+.004f);strip.rotation=Quaternion.LookRotation(sideV,Vector3.up);
    p.Mailbox=box;p.RoadSide=side;p.RoadDistance=(p.Index==2?mainLen:0)+arc;EditorUtility.SetDirty(p);
    log.AppendLine($"GUIDE p{p.Index} {HouseNumber[p.Index]}: mailbox {at} {(side>0?"right":"left")} side, {p.RoadDistance:F0} m along the road, model {b.size}");
   }
   EditorUtility.SetDirty(county);
  }
  static void GuideShots(){
   var dir=Path.Combine(Work,"Audit","guide");Directory.CreateDirectory(dir);var muted=MuteFeatures();
   var cam=new GameObject("guide cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~(1<<8);cam.fieldOfView=60;
   try{foreach(var p in county.Properties){if(!p.Mailbox)continue;var line=GuideLine(p.Index==2?LateRoadPath:MainRoad,2f);GuideNearest(line,p.Mailbox.position,out var c,out var tan,out var arc);var right=new Vector3(tan.z,0,-tan.x);
     foreach(var d in new[]{24f,10f}){var eye=c-tan*d+right*1.6f;eye.y=GroundAt(eye)+1.25f;cam.transform.position=eye;cam.transform.LookAt(p.Mailbox.position+Vector3.up*.9f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-{d:0}m.png"),960,540);}
     cam.transform.position=p.Mailbox.position-tan*2.4f+Vector3.up*1.3f;cam.transform.LookAt(p.Mailbox.position+Vector3.up*.8f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-close.png"),960,540);
     cam.transform.position=p.Mailbox.position-p.Mailbox.forward*-2.6f+Vector3.up*1.3f;cam.transform.LookAt(p.Mailbox.position+Vector3.up*.9f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-road.png"),960,540);}}
   finally{Object.DestroyImmediate(cam.gameObject);Restore(muted);}
  }
 }
}
