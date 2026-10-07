using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V26 (playtest round 3): Bell's stairs and books ("book should fall when player go near stairs"), Morrow's mat, the
 // watcher's room at Harrow. Probe26: what stands where, with pictures from the hall.
 public static partial class ServiceV19Rebuild {
  // V26: Bell's stair treads for the falling book - a disabled MeshCollider of the stair's own drawn mesh (the asset,
  // not the static-batched combined mesh the player draws with), switched on only while the book comes down
  public static void StairTreads26(){Open();var p=county.Properties.First(x=>x.Index==4);var mr=p.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name.Contains("IntStairs"));
   var old=mr.transform.Find("V26 stair treads");if(old)Object.DestroyImmediate(old.gameObject);
   var mesh=mr.GetComponent<MeshFilter>().sharedMesh;var g=new GameObject("V26 stair treads");g.transform.SetParent(mr.transform,false);GameObjectUtility.SetStaticEditorFlags(g,0);g.layer=mr.gameObject.layer;
   var mc=g.AddComponent<MeshCollider>();mc.sharedMesh=mesh;mc.convex=false;mc.enabled=false;
   log.AppendLine($"V26 stair treads under {PathOf(mr.transform)}: mesh {mesh.name} ({AssetDatabase.GetAssetPath(mesh)}) v{mesh.vertexCount} readable {mesh.isReadable}, collider off");Save("StairTreads26");}
  public static void Probe26(){Open();Physics.SyncTransforms();var sb=new StringBuilder();var dir=Path.Combine(Work,"Audit","v26probe");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   foreach(var p in county.Properties.Where(x=>x.Index==4||x.Index==5||x.Index==3)){
    sb.AppendLine($"p{p.Index} {p.Address} encounter {p.Encounter} variant {p.CreatureVariant} door {p.Door.position:F2} inward {p.Inward:F2} table {p.TableApproach.position:F2} spawn {p.EntitySpawn.position:F2} bounds {p.InteriorBounds.center:F1}/{p.InteriorBounds.size:F1}");
    foreach(var r in p.GetComponentsInChildren<Transform>(true).Where(t=>{var n=t.name.ToLowerInvariant();return n.Contains("stair")||n.Contains("step")||n.Contains("landing")||n.Contains("book")||n.Contains("shelf")||n.Contains("doormat")||n.Contains("banister")||n.Contains("rail");})){
     var mr=r.GetComponent<Renderer>();sb.AppendLine($"   {PathOf(r)} at {r.position:F2} fw {r.forward:F2} right {r.right:F2} active {r.gameObject.activeInHierarchy} {(mr?"bounds "+mr.bounds.center.ToString("F2")+" size "+mr.bounds.size.ToString("F2"):"")} kids {r.childCount}");}
    // stair zones / colliders anywhere inside the bounds
    foreach(var z in county.GetComponentsInChildren<ServiceGameV2.ServiceStairZone>(true).Where(z=>p.InteriorBounds.Contains(z.transform.position)))sb.AppendLine($"   stair zone {PathOf(z.transform)} at {z.transform.position:F2}");
    foreach(var mr in county.GetComponentsInChildren<MeshRenderer>(true).Where(m=>!m.transform.IsChildOf(p.transform)&&p.InteriorBounds.Contains(m.bounds.center)&&(m.name.ToLowerInvariant().Contains("stair")||m.name.ToLowerInvariant().Contains("book"))))sb.AppendLine($"   (outside p) {PathOf(mr.transform)} bounds {mr.bounds.center:F2} size {mr.bounds.size:F2}");
   }
   // Bell's flight: its collider, and what a ray straight down finds along it and across it
   {var p4=county.Properties.First(x=>x.Index==4);var mr=p4.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name.Contains("IntStairs"));
    foreach(var c in mr.GetComponentsInChildren<Collider>(true))sb.AppendLine($"FLIGHT collider {PathOf(c.transform)} {c.GetType().Name} enabled {c.enabled} {(c is MeshCollider m?"mesh "+(m.sharedMesh?m.sharedMesh.name+" v"+m.sharedMesh.vertexCount+" readable "+m.sharedMesh.isReadable:"none"):"")} bounds {c.bounds.center:F2}/{c.bounds.size:F2} layer {c.gameObject.layer}");
    var mf=mr.GetComponent<MeshFilter>();sb.AppendLine($"FLIGHT render mesh {(mf&&mf.sharedMesh?mf.sharedMesh.name+" v"+mf.sharedMesh.vertexCount+" readable "+mf.sharedMesh.isReadable:"-")} fw {mr.transform.forward:F2}");
    var col=mr.GetComponentInChildren<Collider>();var bb=col.bounds;var f2=mr.transform.forward;f2.y=0;f2.Normalize();var side=Vector3.Cross(Vector3.up,f2);
    foreach(float s2 in new[]{-1.2f,-.8f,-.4f,0f,.4f,.8f,1.2f}){var line=new StringBuilder($"FLIGHT across {s2:F1}:");for(float k=-1.6f;k<=1.6f;k+=.2f){var o=bb.center+side*s2+f2*k;o.y=bb.max.y+.5f;if(col.Raycast(new Ray(o,Vector3.down),out var h,bb.size.y+1f))line.Append($" {h.point.y:F2}/{h.normal.y:F2}");else line.Append(" -");}sb.AppendLine(line.ToString());}}
   // anything drawn with a missing or broken shader (magenta in the hall at Bell's)
   foreach(var r in county.GetComponentsInChildren<Renderer>(true)){var bad=r.sharedMaterials.Where(m=>!m||!m.shader||m.shader.name.Contains("InternalError")||!m.shader.isSupported).Select(m=>m?m.name+"("+(m.shader?m.shader.name:"no shader")+")":"null").ToList();
    if(bad.Count>0)sb.AppendLine($"BAD {PathOf(r.transform)} active {r.gameObject.activeInHierarchy} enabled {r.enabled} at {r.bounds.center:F2} size {r.bounds.size:F2}: {string.Join(", ",bad)}");}
   var cam0=new Vector3(-73.2f,2.36f,296.9f);foreach(var r in county.GetComponentsInChildren<Renderer>(true).Where(r=>r.gameObject.activeInHierarchy&&r.enabled&&r.bounds.center.z>297.5f&&r.bounds.center.z<303f&&r.bounds.center.x<-73.5f&&r.bounds.center.x>-79f&&r.bounds.center.y<2.2f))
    sb.AppendLine($"NEAR {PathOf(r.transform)} at {r.bounds.center:F2} size {r.bounds.size:F2} mats {string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name+"("+m.shader.name+")"))}");
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=70;cam.aspect=16f/9f;
   try{foreach(var p in county.Properties.Where(x=>x.Index==4)){
     var stairs=p.GetComponentsInChildren<MeshRenderer>(true).Where(m=>m.name.ToLowerInvariant().Contains("stair")).ToList();
     var inside=p.Door.position+p.Inward*1.2f+Vector3.up*1.6f;
     cam.transform.position=inside;cam.transform.rotation=Quaternion.LookRotation(p.Inward);Shoot(cam,Path.Combine(dir,"p4-hall.jpg"),960,540);
     int k=0;foreach(var s in stairs.Take(3)){var b=s.bounds;var from=b.center+(inside-b.center).normalized*3.2f;from.y=b.min.y+1.6f;cam.transform.position=from;cam.transform.LookAt(b.center);Shoot(cam,Path.Combine(dir,$"p4-stairs-{k++}.jpg"),960,540);}
    }}finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}
   File.WriteAllText(Path.Combine(dir,"probe26.txt"),sb.ToString());}
 }
}
