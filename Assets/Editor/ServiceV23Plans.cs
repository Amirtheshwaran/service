using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V23: cutaway floor plans of every house (orthographic, from 2.1 m above each floor, looking down, so the roof and
 // upper walls are cut away) with the pixel->world mapping written beside them; plus a raycast dump of each bookcase.
 public static partial class ServiceV19Rebuild {
  public static void Plans23(){Open();var dir=Path.Combine(Work,"Audit","plans23");Directory.CreateDirectory(dir);var sb=new StringBuilder();
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   var n0=NeutralLight();var cam=AuditCam();
   try{foreach(var p in county.Properties){var ib=p.InteriorBounds;float floor=p.Door.position.y;
     foreach(float fl in new[]{floor,floor+3.8f}){if(fl>ib.max.y-1)continue;
      cam.orthographic=true;float size=Mathf.Max(ib.extents.x,ib.extents.z)+1;cam.orthographicSize=size;cam.transform.position=new Vector3(ib.center.x,fl+2.1f,ib.center.z);cam.transform.rotation=Quaternion.Euler(90,0,0);cam.nearClipPlane=.01f;cam.farClipPlane=3f;
      var file=$"p{p.Index}-plan-{fl:F1}.jpg";Shoot(cam,Path.Combine(dir,file),1000,1000);
      sb.AppendLine($"{file}: 1000x1000 px, centre {ib.center.x:F2},{ib.center.z:F2}, {2*size/1000f:F4} m per px; x right = +x, up = +z; door {p.Door.position}");}}
    // bookcases: every hit straight down the middle
    foreach(var p in county.Properties)foreach(var sh in InteriorItems(p).Where(x=>x.name.ToLowerInvariant().Contains("bookshelf")||x.name.ToLowerInvariant().Contains("bookcase"))){var b=RBounds(sh);
     sb.AppendLine($"bookcase p{p.Index} {sh.name} bounds {b.center} {b.size}; colliders: {string.Join("; ",sh.GetComponentsInChildren<Collider>().Select(c=>c.GetType().Name+" "+c.bounds.size))}");
     var mf=sh.GetComponentInChildren<MeshFilter>();if(mf&&mf.sharedMesh)sb.AppendLine($"   mesh {mf.sharedMesh.name} readable {mf.sharedMesh.isReadable} verts {mf.sharedMesh.vertexCount} scale {mf.transform.lossyScale}");
     var probe=new GameObject("V23 board probe");probe.transform.SetParent(mf.transform,false);var mc=probe.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;Physics.SyncTransforms();
     foreach(var off in new[]{0f,.15f,-.15f})foreach(var h in Physics.RaycastAll(new Vector3(b.center.x,b.max.y+.3f,b.center.z)+new Vector3(off,0,off),Vector3.down,b.size.y+.6f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
      sb.AppendLine($"   off {off}: {h.collider.name} y {h.point.y-b.min.y:F2} normal {h.normal}");
     Object.DestroyImmediate(probe);}
   }finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
   File.WriteAllText(Path.Combine(dir,"plans.txt"),sb.ToString());}
 }
}
