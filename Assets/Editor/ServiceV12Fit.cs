using System.IO;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace ServiceGameV2.Editor {public static class ServiceV12Fit {
 public static void Build(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=Object.FindAnyObjectByType<CountyScene>();var cabin=s.Cockpit.transform;var wheel=cabin.GetComponentsInChildren<Transform>().First(t=>t.name=="Wheel");var pivot=s.SteeringWheel;wheel.SetParent(cabin,true);pivot.localPosition=cabin.InverseTransformPoint(wheel.TransformPoint(wheel.GetComponent<MeshFilter>().sharedMesh.bounds.center));wheel.SetParent(pivot,true);
 if(pivot.localPosition.x>0)throw new System.Exception("Steering wheel must be on driver side");
 var vinyl=AssetDatabase.LoadAssetAtPath<Material>("Assets/External/RacoonCar/Black vinyl.mat");vinyl.SetColor("_BaseColor",new Color(.30f,.32f,.32f));vinyl.SetFloat("_Smoothness",.25f);EditorUtility.SetDirty(vinyl);
 var paint=AssetDatabase.LoadAssetAtPath<Material>("Assets/External/RacoonCar/County graphite paint.mat");paint.SetColor("_BaseColor",new Color(.20f,.22f,.23f));EditorUtility.SetDirty(paint);
 cabin.Find("Instrument spill").GetComponent<Light>().intensity=1.8f;
 s.Odometer.transform.localPosition=new Vector3(-.396f,1.119f,.474f);s.Odometer.characterSize=.0016f;
 s.RevNeedle.parent.localPosition=new Vector3(-.18f,1.205f,.487f);
 foreach(var text in cabin.GetComponentsInChildren<TextMesh>())text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
 EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();ServiceV12Contract.Run();ServiceQuickBuild.Build();
 }
}}
