using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V28b: where Bell's stairs are against his door - heights on a grid around the stair foot, and navmesh paths from the
  // came-back standpoint (3 m inside his door) and from the stair top to the doorway
  public static void BellProbe28(){Open();Physics.SyncTransforms();var nav=NavMesh.AddNavMeshData(county.Navigation);var sb=new StringBuilder();
   try{var p=county.Properties.First(x=>x.Index==4);var door=p.Door.position;var oc=p.OpeningCentre;var inw=p.Inward;
    sb.AppendLine($"door {door:F2} opening {oc:F2} inward {inw:F2} spawn {p.EntitySpawn.position:F2} table {p.TableApproach.position:F2}");
    var stand=oc+inw*3f;sb.AppendLine($"came-back stand {stand:F2}");
    var foot=new Vector3(-77.19f,0.79f,297.58f);
    sb.AppendLine("height grid (x across, z down), top hit y below 6 m:");
    for(float z=foot.z+4f;z>=foot.z-3f;z-=.5f){var line=new StringBuilder($"z {z,7:F2}:");for(float x=foot.x-2f;x<=foot.x+6f;x+=.5f){line.Append(Physics.Raycast(new Vector3(x,6f,z),Vector3.down,out var h,6f,~0,QueryTriggerInteraction.Ignore)?$" {h.point.y,4:F1}":"    -");}sb.AppendLine(line.ToString());}
    sb.Append("x columns:");for(float x=foot.x-2f;x<=foot.x+6f;x+=.5f)sb.Append($" {x,4:F1}");sb.AppendLine();
    float Len(Vector3 a,Vector3 b){var path=new NavMeshPath();if(!NavMesh.SamplePosition(a,out var ha,1.6f,NavMesh.AllAreas)||!NavMesh.SamplePosition(b,out var hb,1.6f,NavMesh.AllAreas))return -1;if(!NavMesh.CalculatePath(ha.position,hb.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return -2;float l=0;for(int i=1;i<path.corners.Length;i++)l+=Vector3.Distance(path.corners[i-1],path.corners[i]);return l;}
    // the highest walkable point near the stairs: sample navmesh upward from the foot
    for(float y=1f;y<=5f;y+=.5f)for(float dz=-2f;dz<=4f;dz+=1f)for(float dx=-2f;dx<=3f;dx+=1f){var q=new Vector3(foot.x+dx,y,foot.z+dz);if(NavMesh.SamplePosition(q,out var h,.3f,NavMesh.AllAreas)&&h.position.y>2f)sb.AppendLine($"upper navmesh at {h.position:F2}: path to stand {Len(h.position,stand):F1} m, to door {Len(h.position,oc):F1} m");}
    sb.AppendLine($"stand to door {Len(stand,oc+(-inw)*1.8f):F1} m; foot to stand {Len(foot,stand):F1} m; foot to door {Len(foot,oc):F1} m; spawn to stand {Len(p.EntitySpawn.position,stand):F1} m");
    foreach(var t in p.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("living-box")))sb.AppendLine($"{t.name} {t.position:F2}");}
   finally{nav.Remove();}
   Directory.CreateDirectory(Path.Combine(Work,"Audit","v28"));File.WriteAllText(Path.Combine(Work,"Audit","v28","bellprobe28.txt"),sb.ToString());log.Append(sb.ToString());}
 }
}
