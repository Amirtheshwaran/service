using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace ServiceGameV2.Editor {
 // V21 audits (batch mode with graphics). Nothing here saves the scene.
 //  RoadAudit: every object standing within a few metres of a road edge, listed with its clearance and ground contact,
 //   photographed close up in flat neutral light; plus driver-eye views every 15 m both ways along the county road,
 //   Route 9 and each drive mouth (Audit/roads21).
 //  DoorstepAudit: sweeps every front door leaf through its opening arc and measures it against a resident standing at
 //   ServiceProperty.AnswerPoint (Audit/doorstep21.txt and top-down frames).
 public static partial class ServiceV19Rebuild {
  sealed class Neutral{public AmbientMode mode;public Color sky,eq,gr;public bool fog;public Light key,fill;public List<(Light l,bool on)> lights=new List<(Light,bool)>();}
  static Neutral NeutralLight(){
   var n=new Neutral{mode=RenderSettings.ambientMode,sky=RenderSettings.ambientSkyColor,eq=RenderSettings.ambientEquatorColor,gr=RenderSettings.ambientGroundColor,fog=RenderSettings.fog};
   foreach(var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){n.lights.Add((l,l.enabled));l.enabled=false;}
   RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.6f,.62f,.66f);RenderSettings.ambientEquatorColor=new Color(.45f,.45f,.45f);RenderSettings.ambientGroundColor=new Color(.25f,.24f,.22f);
   n.key=new GameObject("audit key").AddComponent<Light>();n.key.type=LightType.Directional;n.key.intensity=1.25f;n.key.shadows=LightShadows.Soft;n.key.transform.rotation=Quaternion.Euler(48,-35,0);
   n.fill=new GameObject("audit fill").AddComponent<Light>();n.fill.type=LightType.Directional;n.fill.intensity=.4f;n.fill.transform.rotation=Quaternion.Euler(30,150,0);
   return n;
  }
  static void EndNeutral(Neutral n){RenderSettings.ambientMode=n.mode;RenderSettings.ambientSkyColor=n.sky;RenderSettings.ambientEquatorColor=n.eq;RenderSettings.ambientGroundColor=n.gr;RenderSettings.fog=n.fog;foreach(var (l,on) in n.lights)if(l)l.enabled=on;Object.DestroyImmediate(n.key.gameObject);Object.DestroyImmediate(n.fill.gameObject);}
  static Camera AuditCam(){var cam=new GameObject("audit cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~0;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.55f,.57f,.6f);cam.fieldOfView=55;cam.nearClipPlane=.05f;cam.farClipPlane=400;return cam;}
  static float Dist2(IList<Vector3> line,Vector3 p,out Vector3 nearest){float best=float.MaxValue;nearest=p;for(int i=1;i<line.Count;i++){var a=line[i-1];var b=line[i];var ab=new Vector2(b.x-a.x,b.z-a.z);var ap=new Vector2(p.x-a.x,p.z-a.z);float t=Mathf.Clamp01(Vector2.Dot(ap,ab)/Mathf.Max(ab.sqrMagnitude,1e-6f));float d=(ap-ab*t).magnitude;if(d<best){best=d;nearest=Vector3.Lerp(a,b,t);}}return best;}
  static Transform UnitOf(Transform t){
   // the meaningful object a renderer belongs to: the highest ancestor below a container/property/county level
   var root=county.transform;var u=t;while(u.parent&&u.parent!=root&&!u.parent.GetComponent<ServiceProperty>()&&u.parent.parent!=root)u=u.parent;
   if(u.parent&&u.parent.parent==root&&u.parent.childCount<3)u=u.parent;return u;}
  public static void RoadAudit(){
   Open();var dir=Path.Combine(Work,"Audit","roads21");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var t=county.GetComponentInChildren<Terrain>();var main=Centre(MainRoad,t,.8f,9).P;var late=Centre(LateRoadPath,t,.8f,9).P;
   var drives=county.Properties.Where(p=>p.ApproachRoute!=null&&p.ApproachRoute.Length>1).Select(p=>(p.Index,line:(IList<Vector3>)p.ApproachRoute)).ToList();
   var roads=county.transform.Find("V19 county roads");
   var report=new StringBuilder();var units=new Dictionary<Transform,(float clear,string road,Vector3 near)>();
   foreach(var r in county.GetComponentsInChildren<Renderer>(false)){
    if(!r.enabled||r is ParticleSystemRenderer)continue;var tr=r.transform;
    if(roads&&tr.IsChildOf(roads))continue;if(tr.IsChildOf(county.Car))continue;if(county.Walker&&tr.IsChildOf(county.Walker.transform))continue;if(county.View&&tr.IsChildOf(county.View.transform))continue;
    if(county.Properties.Any(p=>p.Building&&tr.IsChildOf(p.Building)))continue;
    var b=r.bounds;if(b.size.magnitude<.15f)continue;
    float rad=Mathf.Max(b.extents.x,b.extents.z);
    float dm=Dist2(main,b.center,out var nm)-rad-MainHalf;float dl=Dist2(late,b.center,out var nl)-rad-LateHalf;
    float best=dm;string which="main";var near=nm;if(dl<best){best=dl;which="route9";near=nl;}
    foreach(var (idx,line) in drives){float dd=Dist2(line,b.center,out var nd)-rad-DriveHalf;if(dd<best){best=dd;which="drive"+idx;near=nd;}}
    if(best>3.5f)continue;var u=UnitOf(tr);
    if(!units.TryGetValue(u,out var cur)||best<cur.clear)units[u]=(best,which,near);
   }
   var list=units.OrderBy(k=>k.Value.clear).ToList();
   report.AppendLine($"ROAD AUDIT: {list.Count} objects within 3.5 m of a road edge (negative clearance = overlapping the road surface)");
   int n=0;var n0=NeutralLight();var cam=AuditCam();
   try{
    foreach(var (u,v) in list.Select(k=>(k.Key,k.Value))){
     var rs=u.GetComponentsInChildren<Renderer>().Where(x=>x.enabled&&!(x is ParticleSystemRenderer)).ToArray();if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var x in rs)b.Encapsulate(x.bounds);
     float ground=TerrainY(t,b.center);string contact=b.min.y>ground+.08f?$"FLOATS {b.min.y-ground:F2} m":b.max.y<ground+.05f?"BURIED":(b.min.y<ground-.6f?$"sunk {ground-b.min.y:F2} m":"on ground");
     report.AppendLine($"{n:000} clear {v.clear,6:F2} m  {v.road,-7} {contact,-18} size {b.size}  at {b.center}  {PathOf(u)}");
     if(n<90){var side=b.center-v.near;side.y=0;if(side.sqrMagnitude<.01f)side=Vector3.right;side.Normalize();
      float reach=Mathf.Clamp(b.size.magnitude*1.4f,4,14);var eye=v.near-side*1.2f+Vector3.up*1.6f;eye.y=Mathf.Max(eye.y,TerrainY(t,eye)+1.6f);
      var look=b.center-eye;if(look.magnitude<reach){eye=b.center-look.normalized*reach;eye.y=Mathf.Max(eye.y,TerrainY(t,eye)+1.5f);}
      cam.transform.position=eye;cam.transform.LookAt(b.center);Shoot(cam,Path.Combine(dir,$"obj-{n:000}.jpg"),480,300);}
     n++;
    }
    // driver-eye views along the county road both ways, Route 9, and every drive mouth
    void Views(IList<Vector3> line,string tag,float every){float s=0;int k=0;for(int i=1;i<line.Count;i++){s+=Vector3.Distance(line[i-1],line[i]);if(s<every)continue;s=0;
      int j=Mathf.Min(i+4,line.Count-1);var f=line[j]-line[i];f.y=0;if(f.sqrMagnitude<.01f)continue;f.Normalize();var right=new Vector3(f.z,0,-f.x);
      foreach(var (dirn,name) in new[]{(1,"n"),(-1,"s")}){var fw=f*dirn;var r2=right*dirn;var eye=line[i]+r2*1.5f;eye.y=TerrainY(t,eye)+1.25f;var p0=line[i];eye.y=Mathf.Max(eye.y,p0.y+1.25f);
       cam.transform.position=eye;cam.transform.rotation=Quaternion.LookRotation(fw+Vector3.down*.05f);Shoot(cam,Path.Combine(dir,$"view-{tag}-{k:00}{name}.jpg"),640,360);}
      k++;}}
    cam.fieldOfView=62;Views(main,"main",15);Views(late,"r9",15);
    foreach(var p in county.Properties){if(p.ApproachRoute==null||p.ApproachRoute.Length<2)continue;var r=p.ApproachRoute;
     // the drive mouth from the road (gate end = last point) and from the drive looking back at the road
     var mouth=r[0];var into=r[2]-mouth;into.y=0;into.Normalize();
     var eye=mouth-into*12+Vector3.up*1.3f;eye.y=TerrainY(t,eye)+1.3f;cam.transform.position=eye;cam.transform.rotation=Quaternion.LookRotation(into+Vector3.down*.08f);Shoot(cam,Path.Combine(dir,$"mouth-{p.Index}-in.jpg"),640,360);
     eye=mouth+into*8+Vector3.up*1.3f;eye.y=TerrainY(t,eye)+1.3f;cam.transform.position=eye;cam.transform.rotation=Quaternion.LookRotation(-into+Vector3.down*.08f);Shoot(cam,Path.Combine(dir,$"mouth-{p.Index}-out.jpg"),640,360);
     cam.orthographic=true;cam.orthographicSize=16;cam.transform.position=mouth+Vector3.up*60;cam.transform.rotation=Quaternion.Euler(90,0,0);cam.nearClipPlane=1;Shoot(cam,Path.Combine(dir,$"mouth-{p.Index}-top.jpg"),640,640);cam.orthographic=false;cam.nearClipPlane=.05f;}
    // signs anywhere in the county, wherever they stand
    report.AppendLine();report.AppendLine("SIGNS AND BOARDS (anywhere):");int sgn=0;
    foreach(var tm in county.GetComponentsInChildren<TextMesh>(false)){var s=tm.transform.parent?tm.transform.parent:tm.transform;var dm=Dist2(main,s.position,out _);var dl=Dist2(late,s.position,out _);float dd=drives.Count>0?drives.Min(x=>Dist2(x.line,s.position,out _)):999;
     report.AppendLine($"sign {sgn:00} '{tm.text.Replace("\n"," / ")}' at {s.position} main {dm:F1} m route9 {dl:F1} m drive {dd:F1} m  {PathOf(s)}");
     cam.transform.position=s.position-s.forward*5+Vector3.up*.6f;cam.transform.LookAt(s.position);Shoot(cam,Path.Combine(dir,$"sign-{sgn:00}.jpg"),480,300);sgn++;}
   }finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);}
   File.WriteAllText(Path.Combine(Work,"Audit","road-audit21.txt"),report.ToString());
  }
  public static void DoorstepAudit(){
   Open();var dir=Path.Combine(Work,"Audit","doorstep21");Directory.CreateDirectory(dir);var sb=new StringBuilder();
   var res=Object.FindAnyObjectByType<ServiceResidents>();
   foreach(var p in county.Properties){
    if(!p.DoorPanel){sb.AppendLine($"p{p.Index} no door panel");continue;}p.MeasureDoorway();
    var rs=p.DoorPanel.GetComponentsInChildren<Renderer>();var rest=p.DoorPanel.localRotation;
    // leaf as a box in hinge space
    var inv=p.DoorPanel.worldToLocalMatrix;var lb=new Bounds();bool first=true;foreach(var r in rs){var b=r.bounds;for(int c=0;c<8;c++){var corner=new Vector3((c&1)==0?b.min.x:b.max.x,(c&2)==0?b.min.y:b.max.y,(c&4)==0?b.min.z:b.max.z);var lc=inv.MultiplyPoint3x4(corner);if(first){lb=new Bounds(lc,Vector3.zero);first=false;}else lb.Encapsulate(lc);}}
    float open=(p.Index==0||p.Index==4)?Mathf.Sign(p.DoorSwing)*ServiceLife.FriendlyOpen:p.DoorSwing;
    var ans=p.AnswerPoint();sb.AppendLine($"p{p.Index} width {p.DoorWidth:F2} swing {p.DoorSwing} opens to {open} inward {p.Inward} answer {ans} hinge {p.DoorPanel.position} leafLocal {lb}");
    float worst=999;string hits="";
    for(float a=0;Mathf.Abs(a)<=Mathf.Abs(open)+.01f;a+=Mathf.Sign(open)*5){
     p.DoorPanel.localRotation=rest*Quaternion.Euler(0,a,0);
     // distance from the resident's axis (radius .26) to the leaf box, sampled up the body
     float dmin=999;for(float h=.2f;h<1.7f;h+=.25f){var q=ans+Vector3.up*h;var lq=p.DoorPanel.worldToLocalMatrix.MultiplyPoint3x4(q);var cl=lb.ClosestPoint(lq);var w=p.DoorPanel.localToWorldMatrix.MultiplyPoint3x4(cl);var d=w-q;d.y=0;dmin=Mathf.Min(dmin,d.magnitude);}
     float clear=dmin-.26f;worst=Mathf.Min(worst,clear);if(clear<0)hits+=$"{a:F0}° ";
    }
    p.DoorPanel.localRotation=rest;
    sb.AppendLine($"   resident clearance worst {worst:F2} m; leaf passes through the resident at: {(hits.Length>0?hits:"never")}");
   }
   File.WriteAllText(Path.Combine(Work,"Audit","doorstep21.txt"),sb.ToString());
  }
 }
}
