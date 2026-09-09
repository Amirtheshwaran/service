using UnityEngine;
using UnityEditor.SceneManagement;
using System.Reflection;
namespace ServiceGameV2.Editor {
 public static class V4Presentation {
 public static void Run(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=Object.FindFirstObjectByType<CountyScene>();var h=s.Properties[1].Building;var cam=s.View;cam.transform.SetParent(null);cam.cullingMask=~((1<<8)|(1<<9)|(1<<10));
 foreach(var t in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None))t.font.RequestCharactersInTexture(t.text,t.fontSize,t.fontStyle);ServiceWorldText.Refresh(null);
 var positions=new[]{new Vector3(11,1.1f,5.8f),new Vector3(14,1.1f,14),new Vector3(10,8.1f,4)};
 var targets=new[]{new Vector3(2,5.7f,5),new Vector3(18,4,8.5f),new Vector3(14,10,8.5f)};
 var names=new[]{"v4-stairs","v4-foyer","v4-landing"};
 for(int i=0;i<positions.Length;i++){cam.transform.position=h.TransformPoint(positions[i])+Vector3.up*1.65f;cam.transform.LookAt(h.TransformPoint(targets[i]));typeof(ServiceBuild).GetMethod("Render",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{cam,names[i]});}
 }
 }
}
