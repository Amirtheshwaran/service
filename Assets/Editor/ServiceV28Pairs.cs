using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V28: the same three cameras on a list of props in two versions of the scene (before and after a fix). The first run
  // (SERVICE_CAMS=save) places the cameras from where the props are and writes them down; the second (SERVICE_CAMS=load)
  // uses the written cameras. Shots: SERVICE_TAG-<prop>-<n>.jpg in Audit/v28/pairs.
  //  0: from the room at eye height, 2.2 m back;  1: from the room, low, level with its base;  2: from above, looking down.
  public static void Pairs28(){Open();Physics.SyncTransforms();var dir=Path.Combine(Work,"Audit","v28","pairs");Directory.CreateDirectory(dir);var tag=System.Environment.GetEnvironmentVariable("SERVICE_TAG")??"x";
   var camsFile=Path.Combine(dir,"cams.txt");bool load=(System.Environment.GetEnvironmentVariable("SERVICE_CAMS")??"")=="load";var cams=new Dictionary<string,(Vector3 pos,Vector3 at)>();
   if(load)foreach(var l in File.ReadAllLines(camsFile)){var a=l.Split('|');cams[a[0]]=(V3(a[1]),V3(a[2]));}
   var list=(System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1])).ToList();
   var nav=UnityEngine.AI.NavMesh.AddNavMeshData(county.Navigation);var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.aspect=4f/3f;cam.nearClipPlane=.02f;var outCams=new StringBuilder();
   try{foreach(var (pi,id) in list){var p=county.Properties.First(x=>x.Index==pi);var t=PropRoots(p).FirstOrDefault(x=>IdOf(x)==id);if(!t)continue;var vs=DrawnVerts(t,6000).ToList();if(vs.Count==0)continue;
     float bottom=vs.Min(v=>v.y),top=vs.Max(v=>v.y);var c=new Vector3(vs.Average(v=>v.x),(bottom+top)*.5f,vs.Average(v=>v.z));float rad=Mathf.Max(.35f,vs.Max(v=>new Vector2(v.x-c.x,v.z-c.z).magnitude));
     for(int k=0;k<3;k++){var key=$"{pi}:{id}:{k}";Vector3 pos,at;
      if(load&&cams.TryGetValue(key,out var cc)){pos=cc.pos;at=cc.at;}
      else{// a room-side direction: the navmesh point nearest the prop, else straight out from the nearest wall
       Vector3 from=c+Vector3.forward*2f;float best=99;for(int a=0;a<24;a++){var dv=Quaternion.Euler(0,a*15,0)*Vector3.forward;var q=c+dv*(rad+1.6f);if(UnityEngine.AI.NavMesh.SamplePosition(q,out var h,.6f,UnityEngine.AI.NavMesh.AllAreas)&&Mathf.Abs(h.position.y-bottom)<1.2f&&!Physics.Linecast(c+Vector3.up*.1f,h.position+Vector3.up*1.2f,~0,QueryTriggerInteraction.Ignore)){float dd=Vector3.Distance(h.position,q);if(dd<best){best=dd;from=h.position;}}}
       var dir2=from-c;dir2.y=0;if(dir2.sqrMagnitude<1e-4f)dir2=Vector3.forward;dir2.Normalize();
       if(k==0){pos=c+dir2*(rad+1.9f);pos.y=bottom+1.55f;if(bottom>c.y-.1f&&top-bottom<.6f)pos.y=Mathf.Max(pos.y,top+.9f);at=c;}
       else if(k==1){pos=c+Quaternion.Euler(0,30,0)*dir2*(rad+.9f);pos.y=bottom+.06f;at=new Vector3(c.x,bottom+.06f,c.z);}
       else{pos=c+dir2*(rad*.6f)+Vector3.up*(top-c.y+1.3f);at=new Vector3(c.x,bottom,c.z);}
       outCams.AppendLine($"{key}|{pos.x},{pos.y},{pos.z}|{at.x},{at.y},{at.z}");}
      cam.fieldOfView=k==2?55:48;cam.transform.position=pos;cam.transform.LookAt(at);Shoot(cam,Path.Combine(dir,$"{tag}-p{pi}-{id}-{k}.jpg"),400,300);}}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);nav.Remove();}
   if(!load)File.WriteAllText(camsFile,outCams.ToString());}
  // put listed props back where props28.txt (the scene as it was at the start of V28) has them: SERVICE_LOOK="1:landing-nook-chair-n"
  public static void Restore28(){Open();var rows=File.ReadAllLines(Path.Combine(Work,"ServiceProject","Assets","Resources","V28","props28.txt")).Select(l=>l.Split((char)9)).ToList();
   foreach(var (pi,id) in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1]))){
    var p=county.Properties.First(x=>x.Index==pi);var t=PropRoots(p).FirstOrDefault(x=>IdOf(x)==id);if(!t)continue;var r=rows.FirstOrDefault(a=>a[0]==PathOf(t));if(r==null){log.AppendLine($"RESTORE {id}: not in the list");continue;}
    var was=t.position;t.position=new Vector3(float.Parse(r[1],System.Globalization.CultureInfo.InvariantCulture),float.Parse(r[2],System.Globalization.CultureInfo.InvariantCulture),float.Parse(r[3],System.Globalization.CultureInfo.InvariantCulture));log.AppendLine($"RESTORE p{pi} {id}: {was:F3} -> {t.position:F3}");}
   Save("Restore28");}
  static Vector3 V3(string s){var a=s.Split(',').Select(x=>float.Parse(x,System.Globalization.CultureInfo.InvariantCulture)).ToArray();return new Vector3(a[0],a[1],a[2]);}
 }
}
