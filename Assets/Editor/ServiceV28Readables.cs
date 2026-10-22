using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V28 story ("we need lore on the monster and the girl and the connection"): papers you can pick up and read, laid on
  // furniture in the houses (the door-note paper, laid flat), and one photograph on Harrow's wall. Each is a
  // "V28 readable — <key>" object; ServiceScenes27 shows each on its night, and its words come from the script.
  //  harrow-photo    236 Millbrook, night one: the frame nearest his table (no new object - a marker on the frame)
  //  vale-letter     77 Latigo, night two: on the console in her front hall
  //  morrow-bulletin 108 Latigo, night two: on the console in her hall
  //  vale-clipping   77 Latigo, night three: on the study desk, beside where the papers go
  // pictures of each readable where it lies: from standing height nearby, and close
  public static void ReadablesLook28(){Open();Physics.SyncTransforms();var dir=Path.Combine(Work,"Audit","v28","readables");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var nav=UnityEngine.AI.NavMesh.AddNavMeshData(county.Navigation);var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.aspect=4f/3f;cam.nearClipPlane=.02f;
   try{foreach(var t in county.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("V28 readable")).ToList()){bool was=t.gameObject.activeSelf;t.gameObject.SetActive(true);var at=t.position;
     Vector3 stand=at+Vector3.forward;float best=99;for(int a=0;a<24;a++){var dv=Quaternion.Euler(0,a*15,0)*Vector3.forward;var q=at+dv*1.1f;if(UnityEngine.AI.NavMesh.SamplePosition(q,out var h,.8f,UnityEngine.AI.NavMesh.AllAreas)&&Mathf.Abs(h.position.y-(at.y-.9f))<1.6f){float dd=Vector3.Distance(h.position,q);if(dd<best){best=dd;stand=h.position;}}}
     var key=t.name.Substring(t.name.IndexOf("— ")+2);
     cam.fieldOfView=60;cam.transform.position=stand+Vector3.up*1.6f;cam.transform.LookAt(at);Shoot(cam,Path.Combine(dir,$"{key}-0.jpg"),480,360);
     var back=(stand-at);back.y=0;back.Normalize();cam.fieldOfView=40;cam.transform.position=at+back*.55f+Vector3.up*.35f;cam.transform.LookAt(at);Shoot(cam,Path.Combine(dir,$"{key}-1.jpg"),480,360);t.gameObject.SetActive(was);}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);nav.Remove();}}
  public static void Readables28(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var old in county.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("V28 readable")).ToList())Object.DestroyImmediate(old.gameObject);
   var src=county.Properties.Select(p=>p.DoorPanel?p.DoorPanel.Find("Door note/Paper"):null).FirstOrDefault(t=>t&&t.GetComponent<MeshRenderer>());
   if(!src){sb.AppendLine("no door-note paper to copy");}
   // a paper lying on a prop's top, at the clear spot nearest its middle (or near a given point), turned a little
   void Lay(int pi,string key,string onId,Vector3? near,float yaw,float scale,Vector3? avoid=null){var p=county.Properties.First(x=>x.Index==pi);var sup=PropRoots(p).FirstOrDefault(t=>IdOf(t)==onId);if(!sup||!src){sb.AppendLine($"{key}: no {onId}");return;}
    var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{var vs=DrawnVerts(sup,6000).ToList();float top=vs.Max(v=>v.y);var c=new Vector3(vs.Average(v=>v.x),top,vs.Average(v=>v.z));var aim=near??c;
     var bb=sup.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
     var paper=Object.Instantiate(src.gameObject,p.transform);paper.name="V28 readable — "+key;foreach(var col in paper.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
     // flat: the quad's own normal up
     var mf=paper.GetComponent<MeshFilter>();paper.transform.rotation=Quaternion.FromToRotation(src.forward,Vector3.up)*Quaternion.Euler(0,0,0);paper.transform.rotation=Quaternion.LookRotation(Vector3.down,Quaternion.Euler(0,yaw,0)*Vector3.forward);
     paper.transform.localScale=src.lossyScale*scale;Physics.SyncTransforms();var pb=paper.GetComponent<Renderer>().bounds;float half=Mathf.Max(pb.extents.x,pb.extents.z)+.02f;
     (float s,Vector3 at,float y) best=(99,c,top);
     for(float dx=-1f;dx<=1f;dx+=.02f)for(float dz=-1f;dz<=1f;dz+=.02f){var q=new Vector3(aim.x+dx,0,aim.z+dz);if(q.x-half<bb.min.x||q.x+half>bb.max.x||q.z-half<bb.min.z||q.z+half>bb.max.z)continue;float sc=new Vector2(dx,dz).magnitude;if(sc>=best.s)continue;if(avoid.HasValue&&new Vector2(q.x-avoid.Value.x,q.z-avoid.Value.z).magnitude<.34f)continue;
      float hi=float.MinValue,lo=float.MaxValue;bool ok=true;foreach(var o in new[]{Vector3.zero,new Vector3(half,0,0),new Vector3(-half,0,0),new Vector3(0,0,half),new Vector3(0,0,-half)}){var r0=new Vector3(q.x+o.x,top+.4f,q.z+o.z);
       if(!Physics.Raycast(r0,Vector3.down,out var h,.6f,VisMask,QueryTriggerInteraction.Collide)||!owner.TryGetValue(h.collider,out var ow)||ow!=sup){ok=false;break;}hi=Mathf.Max(hi,h.point.y);lo=Mathf.Min(lo,h.point.y);}
      if(ok&&hi-lo<.004f)best=(sc,q,hi);}
     paper.transform.position=new Vector3(best.at.x,best.y+.0015f,best.at.z);paper.SetActive(false);
     sb.AppendLine($"{key}: on p{pi} {onId} at {paper.transform.position:F3} ({best.s*100:F0} cm from where it was aimed){(best.s>=99?" - NO FLAT SPOT":"")}");}
    finally{Unvisual(vis);}}
   Lay(1,"vale-letter","vest-console",null,-12f,1.05f);
   Lay(5,"morrow-bulletin","hall-console",null,8f,1.1f);
   {var p1=county.Properties.First(x=>x.Index==1);var dp=p1.DeliveryPoint?p1.DeliveryPoint.position:(Vector3?)null;Lay(1,"vale-clipping","study-desk",dp.HasValue?dp.Value+Vector3.right*.32f:(Vector3?)null,20f,1.25f,dp);}
   {var p=county.Properties.First(x=>x.Index==3);var at=p.DeliveryPoint?p.DeliveryPoint.position:p.Door.position;
    var frame=PropRoots(p).Where(t=>t.name.ToLowerInvariant().Contains("photo")).OrderBy(t=>Vector3.Distance(t.position,at)).FirstOrDefault();
    if(frame){var m=new GameObject("V28 readable — harrow-photo");m.transform.SetParent(frame,false);var r=frame.GetComponentsInChildren<Renderer>().Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});m.transform.position=r.center;sb.AppendLine($"harrow-photo: on {IdOf(frame)} at {r.center:F2}, {Vector3.Distance(r.center,at):F1} m from his table");}}
   log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","v28","readables28.txt"),sb.ToString());Save("Readables28");}
 }
}
