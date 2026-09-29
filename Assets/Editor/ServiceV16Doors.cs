using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace ServiceGameV2.Editor {public static class ServiceV16Doors {
 public static void Run(){var s=ServiceV16Audit.Open();foreach(var p in s.Properties)p.DoorSwing=p.Index==1||p.Index==5?-100:100;
  var hands=s.View.GetComponentInChildren<ServiceHands>();hands.transform.localPosition=new Vector3(0,-1.80f,.08f);
  var h=s.Properties.Single(p=>p.Index==3);var old=new Vector3(83.09f,1.2f,149.98f);foreach(var l in h.GetComponentsInChildren<Light>().Where(l=>l!=h.PorchLight&&l!=h.WindowLight&&Vector3.Distance(l.transform.position,old)<3))l.transform.position+=new Vector3(5.41f,0,6.42f);
  var lamp=h.GetComponentsInChildren<Light>().FirstOrDefault(l=>Vector3.Distance(l.transform.position,h.DeliveryPoint.position)<2);if(!lamp){var go=new GameObject("Delivery desk lamp light");go.transform.SetParent(h.transform);go.transform.position=h.DeliveryPoint.position+Vector3.up*.55f;lamp=go.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=5;lamp.intensity=1.2f;lamp.color=new Color(1,.68f,.39f);}
  EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();ServiceQuickBuild.Build();
 }
}}
