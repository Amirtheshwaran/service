using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static class ServiceStormContract {
  public static void Run(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var scene=UnityEngine.Object.FindAnyObjectByType<CountyScene>();var errors=new List<string>();
   if(!scene.Rain||scene.Rain.GetComponent<ParticleSystemRenderer>().sharedMaterial==null)errors.Add("Rain must have an authored scene emitter and material");
   if(!scene.transform.Find("Manor title viewpoint"))errors.Add("Title needs a separate manor viewpoint");
   foreach(var key in new[]{"rain","wetmud","wetstone","thunder","traversal","chase"})if(Resources.LoadAll<AudioClip>("Audio/V9/"+key).Length==0)errors.Add("Missing licensed audio pool: "+key);
   var type=typeof(ServicePlayer).GetMethod("ResolveLookBack");
   if(type==null)errors.Add("Running look-back controls are missing");
   else {float Call(bool run,bool car,bool blocked,bool q,bool e)=>(float)type.Invoke(null,new object[]{run,car,blocked,q,e});if(Call(true,false,false,true,false)!=-155||Call(true,false,false,false,true)!=155||Call(false,false,false,false,true)!=0||Call(true,true,false,true,false)!=0||Call(true,false,true,true,false)!=0||Call(true,false,false,true,true)!=0)errors.Add("Look-back must preserve interaction, car and input-blocked states");}
   var result=errors.Count==0?"PASS: storm scene and control contract":"FAIL: "+string.Join("; ",errors);File.WriteAllText(Path.GetFullPath("../storm-contract.txt"),result);Debug.Log(result);if(errors.Count>0)throw new Exception(result);
  }
 }
}
