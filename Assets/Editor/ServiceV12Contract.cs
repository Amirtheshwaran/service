using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.Animations;using UnityEngine;
namespace ServiceGameV2.Editor {public static class ServiceV12Contract {
 public static void Run(){
 if(typeof(ServicePlayer).GetProperty("SteeringInput")==null)throw new Exception("V12: smoothed driving contract missing");
 if(typeof(ServiceHorror).GetProperty("ReturnAmbushes")==null)throw new Exception("V12: return-path encounter missing");
 if(!AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External/RacoonCar/CountyCar.fbx"))throw new Exception("V12: authored cabin missing");
 foreach(var name in new[]{"Vale creature","Demon presence"}){var c=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/ServiceArt/"+name+".controller");if(!c.layers[0].stateMachine.states.Any(s=>s.state.name=="Attack"&&s.state.motion))throw new Exception("V12: attack animation missing: "+name);}
 foreach(var key in new[]{"doorslam","metalrattle","impact","woodstress"})if(Resources.LoadAll<AudioClip>("Audio/V12/"+key).Length==0)throw new Exception("V12: recorded foley missing "+key);
 File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"v12-contract.txt"),"PASS: authored cabin, driving and return encounter contracts, both authored attack animations, four new recorded foley pools");
 }
}}
