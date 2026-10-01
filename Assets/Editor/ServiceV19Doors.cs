using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace ServiceGameV2.Editor {
 // Entrances: what stands in and around every front doorway (Audit/door-probe.txt), photographs of each entrance
 // from the drive, the porch and through the opened door (Audit/doorshots), and the rain camera fade.
 public static partial class ServiceV19Rebuild {
  public static void DoorProbe(){
   Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){
    var at=p.Door.position;sb.AppendLine($"== p{p.Index} {p.Address} door {at} rotY {p.Door.eulerAngles.y:F0} outward {OutwardOf(p)} panel {(p.DoorPanel?p.DoorPanel.name+" "+p.DoorPanel.position+" rotY "+p.DoorPanel.eulerAngles.y.ToString("F0"):"none")} swing {p.DoorSwing}");
    foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)){if(r is ParticleSystemRenderer)continue;var b=r.bounds;var c=b.ClosestPoint(at);if(Vector3.Distance(c,at)>1.6f||b.size.magnitude>9)continue;
     var mf=r.GetComponent<MeshFilter>();sb.AppendLine($"  {AnimationUtility.CalculateTransformPath(r.transform,county.transform)} | mesh {(mf&&mf.sharedMesh?mf.sharedMesh.name:"-")} | min {b.min} max {b.max} | collider {(r.GetComponent<Collider>()?"yes":"no")}");}
   }
   File.WriteAllText(Path.Combine(Work,"Audit","door-probe.txt"),sb.ToString());
  }
  public static void DoorShots(){
   Open();var muted=MuteFeatures();var outDir=Path.Combine(Work,"Audit","doorshots");Directory.CreateDirectory(outDir);foreach(var f in Directory.GetFiles(outDir))File.Delete(f);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   try{
    RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.34f,.34f,.36f);
    var cam=new GameObject("V19 door camera").AddComponent<Camera>();cam.fieldOfView=62;cam.nearClipPlane=.05f;cam.farClipPlane=200;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.05f,.06f,.08f);
    cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
    var work=new GameObject("Work light").AddComponent<Light>();work.type=LightType.Spot;work.spotAngle=80;work.range=40;work.intensity=6f;work.color=new Color(1,.97f,.92f);work.shadows=LightShadows.Soft;work.transform.SetParent(cam.transform,false);
    var index=new List<string>();
    void Shot(string file,Vector3 eye,Vector3 look){cam.transform.position=eye;cam.transform.LookAt(look);Shoot(cam,Path.Combine(outDir,file),800,500);index.Add($"{file}\tcamera {eye} looking at {look}");}
    foreach(var p in county.Properties.OrderBy(x=>x.Index)){
     var o=OutwardOf(p);var door=p.Door.position;var head=door+Vector3.up*1.35f;
     var route=p.ApproachRoute;if(route!=null&&route.Length>0){var last=route[route.Length-1];Shot($"p{p.Index}-drive.jpg",last+Vector3.up*1.65f,head);}
     Shot($"p{p.Index}-porch.jpg",door+o*3.2f+Vector3.up*1.65f,head);
     Shot($"p{p.Index}-porch-left.jpg",door+o*2.6f+Vector3.Cross(Vector3.up,o)*1.6f+Vector3.up*1.65f,head);
     if(p.NoticePoint)Shot($"p{p.Index}-note.jpg",p.NoticePoint.position+o*.75f+Vector3.up*.12f,p.NoticePoint.position);
     if(p.DoorPanel){var rest=p.DoorPanel.localRotation;p.DoorPanel.localRotation=rest*Quaternion.Euler(0,p.DoorSwing,0);Physics.SyncTransforms();
      Shot($"p{p.Index}-open.jpg",door+o*1.9f+Vector3.up*1.6f,door-o*3f+Vector3.up*1.3f);
      Shot($"p{p.Index}-open-back.jpg",door-o*1.6f+Vector3.up*1.6f,door+o*3f+Vector3.up*1.3f);
      p.DoorPanel.localRotation=rest;}
    }
    File.WriteAllLines(Path.Combine(outDir,"index.tsv"),index);Object.DestroyImmediate(cam.gameObject);
   }finally{if(county.LateRoad)county.LateRoad.SetActive(lateWas);Restore(muted);}
  }
  // Rain streaks that pass right in front of the lens turn into huge bars: fade them out near the camera.
  public static void RainFade(){
   var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/ServiceArt/Materials/Rain streaks.mat");if(!m){log.AppendLine("RAIN material missing");return;}
   float near=.45f,far=2.4f;m.SetFloat("_CameraFadingEnabled",1);m.SetFloat("_CameraNearFadeDistance",near);m.SetFloat("_CameraFarFadeDistance",far);m.SetVector("_CameraFadeParams",new Vector4(near,1f/(far-near),0,0));m.EnableKeyword("_FADING_ON");EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();log.AppendLine("RAIN camera fade on");
  }
  public static void BatchA(){Hands();SignsAndNotes();RainFade();DoorProbe();DoorShots();}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Morrow House: the villa wall has two narrow door openings split by a pillar, but V16 stretched ONE leaf across
  // both, so opening it left the pillar standing in the middle of the doorway. Give every opening its own leaf at the
  // kit's height; the working leaf is the one the walk uses, the others stay shut.
  public static void MorrowEntrance(){Open();MorrowStage();NotesStage();ServiceBuild.RebakeOpenDoors(county);Save("morrow-entrance");}
  static void MorrowStage(){
   var p=county.Properties.First(x=>x.Index==5);var locked=p.transform.Find("Locked matching side entrance");
   var hinges=new List<Transform>{p.DoorPanel};if(locked)hinges.Add(locked);var shutOld=p.transform.Find("Shut door leaves");if(shutOld)hinges.Add(shutOld);
   var cols=hinges.Where(h=>h).SelectMany(h=>h.GetComponentsInChildren<Collider>(true)).ToList();foreach(var c in cols)c.enabled=false;Physics.SyncTransforms();
   float x=p.Door.position.x,fy=p.Door.position.y;var free=new List<(float a,float b)>();float start=float.NaN;
   for(float z=356.56f;z<=366.4f;z+=.01f){bool blocked=Physics.CheckBox(new Vector3(x,fy+1.5f,z),new Vector3(.14f,.55f,.004f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)||Physics.CheckBox(new Vector3(x,fy+.3f,z),new Vector3(.14f,.2f,.004f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);if(!blocked&&float.IsNaN(start))start=z;if(blocked&&!float.IsNaN(start)){free.Add((start,z));start=float.NaN;}}
   foreach(var c in cols)if(c)c.enabled=true;
   var openings=free.Where(f=>f.b-f.a>.5f&&f.b-f.a<1.7f).ToList();log.AppendLine("MORROW openings "+string.Join(", ",openings.Select(o=>$"{o.a:F2}-{o.b:F2}")));
   if(openings.Count==0){log.AppendLine("MORROW no openings found; unchanged");return;}
   // The walk from the drive uses the opening nearest the old entrance point on the path side (north half, z>359).
   var work=openings.OrderBy(o=>Mathf.Abs((o.a+o.b)*.5f-359.6f)).First();
   foreach(var h in hinges)if(h)foreach(var t in h.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Villa2_Door_B")).ToList())Kill(t,"one leaf stretched across two openings and the pillar between them");
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Buildings/Villa2/Villa2_Door_B.prefab");
   Transform Leaf(Transform parent,(float a,float b) o,float height){
    var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(g,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);g.name="Villa2_Door_B leaf";g.transform.SetParent(p.transform,false);
    g.transform.localScale=Vector3.one;g.transform.rotation=Quaternion.Euler(0,90,0);var bb=BoundsOf(g);if(bb.size.x>bb.size.z){g.transform.Rotate(0,90,0);bb=BoundsOf(g);}
    float w=o.b-o.a-.02f;g.transform.localScale=new Vector3(w/bb.size.z,height/bb.size.y,1);bb=BoundsOf(g);g.transform.position+=new Vector3(x,fy+height*.5f,(o.a+o.b)*.5f)-bb.center;
    foreach(var r in g.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)UpgradeStandard(m);
    if(!g.GetComponent<Collider>()){var box=g.AddComponent<BoxCollider>();}
    g.transform.SetParent(parent,true);return g.transform;
   }
   float hgt=3.10f;
   p.DoorPanel.position=new Vector3(x,fy,work.b);Leaf(p.DoorPanel,work,hgt);
   var shut=p.transform.Find("Shut door leaves");if(shut)Object.DestroyImmediate(shut.gameObject);shut=new GameObject("Shut door leaves").transform;shut.SetParent(p.transform,false);
   foreach(var o in openings.Where(o=>o!=work))Leaf(shut,o,hgt);
   if(locked)Kill(locked,"replaced by per-opening shut leaves");
   var o2=OutwardOf(p);p.Door.position=new Vector3(x,fy,(work.a+work.b)*.5f);
   if(p.KnockPoint){p.KnockPoint.position=new Vector3(x,fy+1.55f,(work.a+work.b)*.5f)+o2*.1f;}
   EditorUtility.SetDirty(p);log.AppendLine($"MORROW working leaf {work.a:F2}-{work.b:F2}, {openings.Count-1} shut leaves, door point {p.Door.position}");
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void BatchB(){MorrowEntrance();WorldFonts();DoorShots();}
  public static void BatchC(){MorrowEntrance();DoorShots();}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void BuildPlayer(){ServiceV17Apply.Build();}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // The two log cabins (Correll, p0, and the parcel, p2) are the same building at the same rotation. Correll's
  // working door sits in the cabin's real front opening; the parcel's was a Cabin2 leaf standing on the porch in
  // front of plain wall. Copy Correll's door across through the building-relative transform, and close each
  // cabin's second porch doorway (an empty hole) with a matching shut leaf.
  public static void CabinDoors(){Open();CabinDoorsStage();NotesStage();ServiceBuild.RebakeOpenDoors(county);Save("cabin-doors");}
  static List<(float a,float b)> FacadeOpenings(ServiceProperty p,float along0,float along1,IEnumerable<Collider> ignore){
   var cols=ignore.Where(c=>c&&c.enabled).ToList();foreach(var c in cols)c.enabled=false;Physics.SyncTransforms();
   float x=p.Door.position.x,fy=p.Door.position.y;var free=new List<(float a,float b)>();float start=float.NaN;
   for(float z=along0;z<=along1;z+=.01f){bool blocked=Physics.CheckBox(new Vector3(x,fy+1.3f,z),new Vector3(.16f,.5f,.004f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)||Physics.CheckBox(new Vector3(x,fy+.35f,z),new Vector3(.16f,.2f,.004f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);if(!blocked&&float.IsNaN(start))start=z;if(blocked&&!float.IsNaN(start)){free.Add((start,z));start=float.NaN;}}
   foreach(var c in cols)c.enabled=true;return free.Where(f=>f.b-f.a>.6f&&f.b-f.a<1.6f).ToList();
  }
  static void CabinDoorsStage(){
   var p0=county.Properties.First(x=>x.Index==0);var p2=county.Properties.First(x=>x.Index==2);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   try{
    var zb=p0.Building.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
    var ignore0=p0.DoorPanel.GetComponentsInChildren<Collider>(true).Concat(p0.transform.Find("Shut door leaves")?p0.transform.Find("Shut door leaves").GetComponentsInChildren<Collider>(true):new Collider[0]);
    var open0=FacadeOpenings(p0,zb.min.z+.2f,zb.max.z-.2f,ignore0);log.AppendLine("CABIN p0 openings "+string.Join(", ",open0.Select(o=>$"{o.a:F2}-{o.b:F2}")));
    var leaf=p0.DoorPanel.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!r.name.Contains("Paper")&&!r.name.Contains("Tape")).OrderByDescending(r=>r.bounds.size.y).First();
    float doorMid=p0.Door.position.z;var second=open0.Where(o=>doorMid<o.a-.05f||doorMid>o.b+.05f).ToList();
    // Correll: shut leaf in every other porch opening.
    Transform Shut(ServiceProperty p,Transform leafSrc,(float a,float b) o,float xw,float fy){
     var holder=p.transform.Find("Shut door leaves");if(!holder){holder=new GameObject("Shut door leaves").transform;holder.SetParent(p.transform,false);}
     var g=Object.Instantiate(leafSrc.gameObject,holder);g.name="Shut door leaf";foreach(var t in g.GetComponentsInChildren<Transform>(true).Where(t=>t!=g.transform&&(t.name=="Door note"||t.name=="Door interaction target")).ToList())Object.DestroyImmediate(t.gameObject);
     g.transform.rotation=leafSrc.rotation;g.transform.localScale=leafSrc.lossyScale;var bb=BoundsOf(g);float want=o.b-o.a-.02f;if(bb.size.z>.05f){var s=g.transform.localScale;g.transform.localScale=new Vector3(s.x*(Mathf.Abs(leafSrc.forward.z)<.5f?want/bb.size.z:1),s.y,s.z*(Mathf.Abs(leafSrc.forward.z)>=.5f?want/bb.size.z:1));bb=BoundsOf(g);}
     g.transform.position+=new Vector3(xw-bb.center.x,fy-bb.min.y,(o.a+o.b)*.5f-bb.center.z);g.isStatic=true;return g.transform;
    }
    var old0=p0.transform.Find("Shut door leaves");if(old0)Object.DestroyImmediate(old0.gameObject);
    var lb=leaf.bounds;foreach(var o in second){Shut(p0,leaf.transform,o,lb.center.x,lb.min.y);log.AppendLine($"CABIN p0 shut leaf in {o.a:F2}-{o.b:F2}");}
    // Parcel: Correll's door through the building-relative transform.
    Matrix4x4 M=p2.Building.localToWorldMatrix*p0.Building.worldToLocalMatrix;Quaternion R=p2.Building.rotation*Quaternion.Inverse(p0.Building.rotation);
    var oldPanel=p2.DoorPanel;var clone=Object.Instantiate(p0.DoorPanel.gameObject,p2.transform);clone.name="Working front door";
    foreach(var t in clone.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Door note").ToList())Object.DestroyImmediate(t.gameObject);
    clone.transform.position=M.MultiplyPoint3x4(p0.DoorPanel.position);clone.transform.rotation=R*p0.DoorPanel.rotation;clone.transform.localScale=p0.DoorPanel.localScale;
    if(oldPanel)Kill(oldPanel,"a Cabin2 leaf standing on the porch in front of plain wall");
    p2.DoorPanel=clone.transform;p2.DoorSwing=p0.DoorSwing;p2.KnockPoint=clone.transform.Find("Door interaction target");
    p2.Door.position=M.MultiplyPoint3x4(p0.Door.position);p2.Door.rotation=R*p0.Door.rotation;
    var old2=p2.transform.Find("Shut door leaves");if(old2)Object.DestroyImmediate(old2.gameObject);
    var leaf2=clone.GetComponentsInChildren<MeshRenderer>(true).OrderByDescending(r=>r.bounds.size.y).First();var lb2=leaf2.bounds;
    foreach(var o in second){var a=M.MultiplyPoint3x4(new Vector3(lb.center.x,lb.min.y,o.a));var b=M.MultiplyPoint3x4(new Vector3(lb.center.x,lb.min.y,o.b));Shut(p2,leaf2.transform,(Mathf.Min(a.z,b.z),Mathf.Max(a.z,b.z)),a.x,a.y);log.AppendLine($"CABIN p2 shut leaf at {a}-{b}");}
    EditorUtility.SetDirty(p0);EditorUtility.SetDirty(p2);log.AppendLine($"CABIN p2 door now {p2.Door.position} panel {p2.DoorPanel.position} swing {p2.DoorSwing}");
   }finally{if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
  }
  public static void BatchD(){CabinDoors();DoorShots();}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Every working door must swing INTO the house and come to rest without passing through jambs or walls. Try both
  // directions and a range of angles against the real colliders, keep the best, and photograph the result.
  public static void DoorFix(){Open();DoorFixStage();Save("door-fix");DoorShots();DoorTopShots();}
  static (Collider c,Vector3 half) LeafBox(ServiceProperty p){
   var c=p.DoorPanel.GetComponentsInChildren<BoxCollider>(true).Where(b=>!b.transform.name.Contains("note")).OrderByDescending(b=>b.size.magnitude).FirstOrDefault();
   if(!c)return (null,Vector3.zero);var s=c.transform.lossyScale;return (c,new Vector3(Mathf.Abs(c.size.x*s.x),Mathf.Abs(c.size.y*s.y),Mathf.Abs(c.size.z*s.z))*.5f);
  }
  static int LeafHits(ServiceProperty p,Collider leaf,Vector3 half,out Vector3 centre){
   centre=leaf.transform.TransformPoint(((BoxCollider)leaf).center);var hits=Physics.OverlapBox(centre,half*.92f,leaf.transform.rotation,~0,QueryTriggerInteraction.Ignore);
   float bottom=centre.y-half.y;var shut=p.transform.Find("Shut door leaves");
   return hits.Count(h=>!(h is TerrainCollider)&&!(h is CharacterController)&&!h.transform.IsChildOf(p.DoorPanel)&&h.bounds.max.y>bottom+.12f&&h.bounds.min.y<bottom+1.6f);
  }
  static void DoorFixStage(){
   Physics.SyncTransforms();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){
    if(!p.DoorPanel)continue;var (leaf,half)=LeafBox(p);if(!leaf){log.AppendLine($"DOOR p{p.Index}: no leaf box collider");continue;}
    var rest=p.DoorPanel.localRotation;var o=OutwardOf(p);var cands=new List<(float sw,int hits,float inward)>();
    foreach(float sign in new[]{1f,-1f})for(float a=70;a<=115;a+=5){float sw=sign*a;p.DoorPanel.localRotation=rest*Quaternion.Euler(0,sw,0);Physics.SyncTransforms();int hits=LeafHits(p,leaf,half,out var centre);float inward=-Vector3.Dot(new Vector3(centre.x-p.Door.position.x,0,centre.z-p.Door.position.z),o);cands.Add((sw,hits,inward));}
    p.DoorPanel.localRotation=rest;Physics.SyncTransforms();
    var inside=cands.Where(c=>c.inward>.15f).ToList();
    // Fewest contacts; among those, the widest opening up to 95 degrees (past that the leaf tends to bury in the wall).
    var best=inside.OrderBy(c=>c.hits).ThenBy(c=>Mathf.Abs(Mathf.Abs(c.sw)-95)).FirstOrDefault();
    log.AppendLine($"DOOR p{p.Index}: was {p.DoorSwing}; "+string.Join(" ",cands.Select(c=>$"{c.sw:+0;-0}:{c.hits}/{c.inward:F2}"))+$" -> {(inside.Count>0?best.sw.ToString():"none inward!")}");
    if(inside.Count>0){p.DoorSwing=best.sw;EditorUtility.SetDirty(p);}
   }
  }
  // Top-down views of every doorway with the door open, to see where the leaf comes to rest.
  public static void DoorTopShots(){
   Open();var muted=MuteFeatures();var outDir=Path.Combine(Work,"Audit","doorshots");Directory.CreateDirectory(outDir);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   try{
    RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.5f,.5f,.52f);
    var cam=new GameObject("V19 door top camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=2.2f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;cam.nearClipPlane=.01f;cam.farClipPlane=2.2f;
    cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;var top=new GameObject("top light").AddComponent<Light>();top.type=LightType.Directional;top.intensity=1.2f;top.transform.rotation=Quaternion.Euler(80,20,0);
    foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.DoorPanel)continue;var rest=p.DoorPanel.localRotation;
     foreach(var (tag,ang) in new[]{("closed",0f),("open",p.DoorSwing)}){p.DoorPanel.localRotation=rest*Quaternion.Euler(0,ang,0);cam.transform.position=p.Door.position+Vector3.up*2.0f;cam.transform.rotation=Quaternion.Euler(90,0,0);Shoot(cam,Path.Combine(outDir,$"p{p.Index}-top-{tag}.jpg"),600,600);}
     p.DoorPanel.localRotation=rest;}
    Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(top.gameObject);
   }finally{if(county.LateRoad)county.LateRoad.SetActive(lateWas);Restore(muted);}
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // What is above the driver's head: renderers over the seat, and photos from the seat looking ahead and up.
  public static void CarProbe(){
   Open();Physics.SyncTransforms();var seat=county.DriverSeat;var sb=new System.Text.StringBuilder();sb.AppendLine("seat "+seat.position);
   foreach(var r in county.Car.GetComponentsInChildren<Renderer>(true)){var b=r.bounds;if(b.max.y<seat.position.y+.2f)continue;var mf=r.GetComponent<MeshFilter>();sb.AppendLine($"{AnimationUtility.CalculateTransformPath(r.transform,county.Car)} | {(mf&&mf.sharedMesh?mf.sharedMesh.name:"-")} | min {b.min} max {b.max} | mats {string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name+"("+m.shader.name+" cull "+(m.HasProperty("_Cull")?m.GetFloat("_Cull").ToString():"?")+")"))} | active {r.gameObject.activeInHierarchy}");}
   foreach(var hit in Physics.RaycastAll(seat.position,Vector3.up,3f,~0,QueryTriggerInteraction.Ignore))sb.AppendLine("ray up hits "+hit.collider.name+" at "+hit.distance);
   File.WriteAllText(Path.Combine(Work,"Audit","car-probe.txt"),sb.ToString());
   var muted=MuteFeatures();try{var cam=new GameObject("seat cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~0;
    cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(5,0,0));if(county.Cockpit)county.Cockpit.SetActive(true);
    Shoot(cam,Path.Combine(Work,"Audit","car-seat-ahead.png"),960,540);cam.transform.rotation=seat.rotation*Quaternion.Euler(-60,0,0);Shoot(cam,Path.Combine(Work,"Audit","car-seat-up.png"),960,540);Object.DestroyImmediate(cam.gameObject);}finally{Restore(muted);}
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void CockpitProbe(){
   Open();var seat=county.DriverSeat;var sb=new System.Text.StringBuilder();var ck=county.Cockpit?county.Cockpit.transform:null;sb.AppendLine("cockpit "+(ck?ck.name+" layer "+ck.gameObject.layer:"none")+" seat "+seat.position+" seat fwd "+seat.forward+" car fwd "+county.Car.forward);
   if(ck)foreach(var r in ck.GetComponentsInChildren<Renderer>(true)){var b=r.bounds;var lo=seat.InverseTransformPoint(b.min);var hi=seat.InverseTransformPoint(b.max);sb.AppendLine($"{AnimationUtility.CalculateTransformPath(r.transform,ck)} layer {r.gameObject.layer} | seat-local {lo} .. {hi} | size {b.size}");}
   File.WriteAllText(Path.Combine(Work,"Audit","cockpit-probe.txt"),sb.ToString());
  }
 }
}
