using UnityEngine;
using UnityEngine.AI;
using System;
using System.IO;
using System.Collections.Generic;
namespace ServiceGameV2.Editor {
 public static class V4RouteCheck {
 public static void Run(){ServiceBuild.SceneOnly=true;ServiceBuild.Build();ServiceBuild.SceneOnly=false;Check();}
 public static void Check(){var s=UnityEngine.Object.FindFirstObjectByType<CountyScene>();var p=s.Properties[1];var nav=NavMesh.AddNavMeshData(s.Navigation);var c=s.Walker;var log=new List<string>();
 try{
 foreach(bool down in new[]{false,true}){var start=down?p.TableApproach.position:p.Gate.position;var target=down?p.Gate.position:p.TableApproach.position;var path=new NavMeshPath();bool found=NavMesh.CalculatePath(start,target,NavMesh.AllAreas,path);log.Add((down?"DOWN":"UP")+" "+found+" "+path.status);if(!found||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Stairs navigation disconnected");
 c.enabled=false;c.transform.position=start+Vector3.up*.08f;c.enabled=true;Physics.SyncTransforms();
 foreach(var point in path.corners){int i=0;for(;i<600;i++){var delta=point-c.transform.position;delta.y=0;if(delta.magnitude<.12f)break;c.Move((delta.normalized*(down?5.7f:3.25f)+Vector3.down*4)/60);Physics.SyncTransforms();}log.Add("corner "+p.Building.InverseTransformPoint(point)+" reached "+p.Building.InverseTransformPoint(c.transform.position)+" frames "+i);if(i==600)throw new Exception("Physical stair route blocked");}
 if(Mathf.Abs(c.transform.position.y-target.y)>.4f)throw new Exception("Wrong floor after physical traversal");
 }log.Add("PASS physical stair ascent and descent");
 }catch(Exception e){log.Add(e.ToString());throw;}finally{File.WriteAllLines(Path.GetFullPath(Application.dataPath+"/../../v4-route-check.txt"),log);nav.Remove();}
 }
 }
}
