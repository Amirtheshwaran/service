using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
namespace ServiceGameV2.Editor {
public static class V3Regression {
 public static void Check(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=UnityEngine.Object.FindFirstObjectByType<CountyScene>();var shell=s.CarExterior[0].transform;var house=s.Properties[0].GetComponentsInChildren<Transform>().First(t=>t.name=="Pref_Cabin1_A");var p=s.Properties[0];
 var errors=new System.Collections.Generic.List<string>();
 // The imported sedan's nose is local +X, measured in the four-side inspection.
 if(Vector3.Dot(shell.right,s.Car.forward)<.95f)errors.Add("Sedan nose points against driving direction");
 if(Mathf.Abs(house.InverseTransformPoint(p.Door.position).z-2.4f)>.3f)errors.Add("Cabin service door is detached from wall plane z=2.31");
 if(!UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="Pref_Villa1_A"))errors.Add("Large villa encounter is missing");
 File.WriteAllText(Path.GetFullPath(Application.dataPath+"/../../v3-regression.txt"),errors.Count==0?"PASS":string.Join("\n",errors));if(errors.Count>0)throw new Exception(string.Join("; ",errors));
 }
 public static void Navigation(){
  EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=UnityEngine.Object.FindFirstObjectByType<CountyScene>();var p=s.Properties[1];var nav=UnityEngine.AI.NavMesh.AddNavMeshData(s.Navigation);
  var report=new System.Collections.Generic.List<string>();
  foreach(var t in new[]{p.Gate,p.Door,p.TableApproach,p.EntitySpawn}){bool found=UnityEngine.AI.NavMesh.SamplePosition(t.position,out var hit,2,UnityEngine.AI.NavMesh.AllAreas);report.Add(t.name+": "+found+" position "+hit.position+" delta "+hit.distance);}
  foreach(var pair in new[]{new[]{p.Gate,p.TableApproach},new[]{p.EntitySpawn,p.TableApproach},new[]{p.EntitySpawn,p.Gate}}){var path=new UnityEngine.AI.NavMeshPath();bool found=UnityEngine.AI.NavMesh.CalculatePath(pair[0].position,pair[1].position,UnityEngine.AI.NavMesh.AllAreas,path);report.Add(pair[0].name+" -> "+pair[1].name+": "+found+" "+path.status+" "+string.Join(";",path.corners.Select(v=>v.ToString())));}
  File.WriteAllLines(Path.GetFullPath(Application.dataPath+"/../../navigation-check.txt"),report);nav.Remove();
 }
 public static void Walk(){
  EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=UnityEngine.Object.FindFirstObjectByType<CountyScene>();var c=s.Walker;var log=new System.Collections.Generic.List<string>();
  foreach(var p in s.Properties.Take(2)){
   c.enabled=false;c.transform.position=p.Gate.position+Vector3.up*.1f;c.enabled=true;Physics.SyncTransforms();
   var target=p.Index==1?p.TableApproach.position:p.Door.position+p.Door.forward*.9f;int frames=0;
   for(;frames<800;frames++){var to=target-c.transform.position;to.y=0;if(to.magnitude<.3f)break;c.Move((to.normalized*3.25f+Vector3.down*2)/60);Physics.SyncTransforms();}
   log.Add(p.Index+" from gate to "+target+" reached "+c.transform.position+" frames "+frames);
  }
  File.WriteAllLines(Path.GetFullPath(Application.dataPath+"/../../walking-check.txt"),log);
 }
}}

