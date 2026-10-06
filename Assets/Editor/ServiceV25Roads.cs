using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V25: the drives and roads from where you stand ("the roads especially are messed up"). RoadProbe25: the terrain's
 // layers and paint resolution, every drive and road material, and pictures from eye height along each drive and road.
 public static partial class ServiceV19Rebuild {
  // V25 furniture audit: the interior views without the camcorder grain (Interior23, filter muted for the pictures only)
  public static void Interior25(){var muted=MuteFeatures();try{Interior23();}finally{Restore(muted);AssetDatabase.SaveAssets();}}
  public static void RoadProbe25(){Open();var sb=new StringBuilder();bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   var dir=Path.Combine(Work,"Audit","v25roads");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var t=county.GetComponentInChildren<Terrain>();var td=t.terrainData;
   sb.AppendLine($"terrain {t.name} at {t.transform.position} size {td.size} heightmap {td.heightmapResolution} alphamap {td.alphamapResolution} ({td.size.x/td.alphamapResolution:F2} m per texel) detail {td.detailResolution}");
   for(int i=0;i<td.terrainLayers.Length;i++){var l=td.terrainLayers[i];sb.AppendLine($"  layer {i} {(l?l.name:"-")} tile {(l?l.tileSize.ToString():"-")} diffuse {(l&&l.diffuseTexture?l.diffuseTexture.name:"-")} path {(l?AssetDatabase.GetAssetPath(l):"-")}");}
   // how much of each layer under the drives, the road and the open ground
   var am=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
   string Mix(Vector3 w){int x=Mathf.Clamp(Mathf.RoundToInt((w.x-t.transform.position.x)/td.size.x*(td.alphamapWidth-1)),0,td.alphamapWidth-1),z=Mathf.Clamp(Mathf.RoundToInt((w.z-t.transform.position.z)/td.size.z*(td.alphamapHeight-1)),0,td.alphamapHeight-1);
    var parts=new List<string>();for(int k=0;k<td.alphamapLayers;k++)if(am[z,x,k]>.05f)parts.Add($"{k}:{am[z,x,k]:F2}");return string.Join(" ",parts);}
   foreach(var mr in county.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("V19 drive")||r.name.StartsWith("V23 footpath")||r.name.StartsWith("V19 County")||r.name.Contains("road")||r.name.Contains("Road")).Take(40))
    sb.AppendLine($"  mesh {PathOf(mr.transform)} on {mr.enabled} mats {string.Join(",",mr.sharedMaterials.Where(m=>m).Select(m=>m.name+"("+m.shader.name+")"))} bounds {mr.bounds.size}");
   var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=62;cam.aspect=16f/9f;
   try{
    foreach(var p in county.Properties){var R=p.ApproachRoute;if(R==null||R.Length<3)continue;
     sb.AppendLine($"p{p.Index} {p.Address}: mouth mix [{Mix(R[0])}]  mid [{Mix(R[R.Length/2])}]  off the drive 6 m [{Mix(R[R.Length/2]+Vector3.Cross(Vector3.up,(R[R.Length/2+1]-R[R.Length/2]).normalized)*6)}]");
     var road=county.Route!=null&&county.Route.Length>0?county.Route.Where(x=>x).Select(x=>x.position).OrderBy(x=>Vector3.Distance(x,R[0])).First():R[0];
     var shots=new List<(string,Vector3,Vector3)>{("a-mouth",R[0]+(R[0]-R[2]).normalized*5+Vector3.up*1.6f,R[R.Length-1]),("b-mid",R[R.Length/3]+Vector3.up*1.6f,R[R.Length-1]),("c-house",R[R.Length-2]+Vector3.up*1.6f,R[0]),("d-high",R[R.Length/2]+Vector3.up*9f+(R[0]-R[R.Length-1]).normalized*6,R[R.Length/2])};
     foreach(var (tag,pos,look) in shots){cam.transform.position=pos;cam.transform.LookAt(look+Vector3.up*(tag=="d-high"?0:.4f));Shoot(cam,Path.Combine(dir,$"p{p.Index}-{tag}.jpg"),960,540);}}
    // the county road and Route 9 from the driver's seat, every ~40 m
    var line=county.Route.Where(x=>x).Select(x=>x.position).ToList();var late=county.LateRoute!=null?county.LateRoute.Where(x=>x).Select(x=>x.position).ToList():new List<Vector3>();
    int k=0;foreach(var (name,L) in new[]{("road",line),("r9",late)}){for(int i=0;i+1<L.Count;i+=Mathf.Max(1,L.Count/10)){var a=L[i];var b=L[Mathf.Min(L.Count-1,i+1)];var fw=(b-a);fw.y=0;fw.Normalize();var side=Vector3.Cross(Vector3.up,fw);
      cam.transform.position=a+side*1.3f+Vector3.up*1.25f;cam.transform.rotation=Quaternion.LookRotation(fw+Vector3.down*.06f,Vector3.up);Shoot(cam,Path.Combine(dir,$"{name}-{k:00}.jpg"),960,540);
      sb.AppendLine($"{name}-{k:00} at {a:F1}: road mix [{Mix(a)}] verge 5 m [{Mix(a+side*5)}]");k++;}}
   } finally {EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
   File.WriteAllText(Path.Combine(dir,"roads25.txt"),sb.ToString());}
 }
}
