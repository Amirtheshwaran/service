using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2.Editor {
 public static class V5RouteInspect {
  public static void Run(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var scene=UnityEngine.Object.FindFirstObjectByType<CountyScene>();var nav=NavMesh.AddNavMeshData(scene.Navigation);var log=new StringBuilder();
   foreach(var p in scene.Properties.Where(p=>p.HasEncounter)){
    log.AppendLine("PROPERTY "+p.Index+" "+p.Template+" door="+p.Building.InverseTransformPoint(p.Door.position));
    foreach(var pair in new[]{("desk",p.TableApproach), ("spawn",p.EntitySpawn),("gate",p.Gate)}){
     var path=new NavMeshPath();bool sampled=NavMesh.SamplePosition(pair.Item2.position,out var hit,.8f,NavMesh.AllAreas);bool pass=sampled&&Mathf.Abs(hit.position.y-pair.Item2.position.y)<.5f&&NavMesh.CalculatePath(hit.position,p.Gate.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
     float length=0;for(int i=1;i<path.corners.Length;i++)length+=Vector3.Distance(path.corners[i-1],path.corners[i]);log.AppendLine(pair.Item1+" "+pass+" local="+p.Building.InverseTransformPoint(pair.Item2.position)+" sampled="+sampled+" length="+length);
     if(!pass){var candidates=new System.Collections.Generic.List<(float,Vector3)>();var local=p.Building.InverseTransformPoint(pair.Item2.position);
      for(float x=local.x-10;x<=local.x+10;x+=1)for(float z=local.z-10;z<=local.z+10;z+=1){var q=p.Building.TransformPoint(new Vector3(x,local.y,z));if(!p.InteriorBounds.Contains(q))continue;if(NavMesh.SamplePosition(q,out var candidate,.35f,NavMesh.AllAreas)&&Mathf.Abs(q.y-candidate.position.y)<.3f&&NavMesh.CalculatePath(candidate.position,p.Gate.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete)candidates.Add((Vector3.Distance(q,pair.Item2.position),p.Building.InverseTransformPoint(candidate.position)));}
      foreach(var candidate in candidates.OrderBy(c=>c.Item1).Take(8))log.AppendLine(" candidate "+candidate.Item2);
     }
    }
   }
   var lodge=scene.Properties[3];for(float z=12;z>3;z-=.5f){var q=lodge.Building.TransformPoint(new Vector3(3,2.5f,z));if(Physics.Raycast(q,Vector3.down,out var floor,3))log.AppendLine("LODGE floor z="+z+" hit="+lodge.Building.InverseTransformPoint(floor.point)+" name="+floor.collider.name);}
   var cam=scene.View;cam.transform.SetParent(null);cam.cullingMask=~((1<<8)|(1<<9)|(1<<10));cam.transform.position=lodge.Door.position+lodge.Door.forward*6+Vector3.up*1.6f;cam.transform.LookAt(lodge.Door.position+Vector3.up);typeof(ServiceBuild).GetMethod("Render",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{cam,"v5-lodge-entrance"});
   File.WriteAllText(Path.GetFullPath(Application.dataPath+"/../../v5-routes.txt"),log.ToString());nav.Remove();
  }
 }
}
