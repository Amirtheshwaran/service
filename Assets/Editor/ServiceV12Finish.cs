using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace ServiceGameV2.Editor {public static class ServiceV12Finish {
 public static void Build(){
 EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=Object.FindAnyObjectByType<CountyScene>();var cabin=s.Cockpit.transform;
 var normalPath="Assets/External/CarConceptWheel/SteeringNormal.png";var importer=(TextureImporter)AssetImporter.GetAtPath(normalPath);importer.textureType=TextureImporterType.NormalMap;importer.maxTextureSize=2048;importer.SaveAndReimport();
 var steeringMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/External/CarConceptWheel/Steering.mat");if(!steeringMat){steeringMat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(steeringMat,"Assets/External/CarConceptWheel/Steering.mat");}steeringMat.SetColor("_BaseColor",new Color(.10f,.115f,.12f));steeringMat.SetFloat("_Smoothness",.32f);steeringMat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));steeringMat.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(steeringMat);
 foreach(var t in cabin.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Wheel"))t.gameObject.SetActive(false);
 var pivot=s.SteeringWheel;pivot.localPosition=new Vector3(-.396f,1.095f,.24f);pivot.localRotation=Quaternion.identity;
 var prior=pivot.Find("Authored leather steering");if(prior)Object.DestroyImmediate(prior.gameObject);
 var wheel=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External/CarConceptWheel/Steering.fbx"),pivot);wheel.name="Authored leather steering";wheel.transform.localPosition=Vector3.zero;wheel.transform.localRotation=Quaternion.identity;
 foreach(var r in wheel.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(_=>steeringMat).ToArray();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}foreach(var t in wheel.GetComponentsInChildren<Transform>())t.gameObject.layer=10;
 var column=cabin.Find("Connected steering column");if(!column){column=GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;Object.DestroyImmediate(column.GetComponent<Collider>());column.name="Connected steering column";column.SetParent(cabin,false);}column.localPosition=new Vector3(-.396f,1.09f,.38f);column.localScale=new Vector3(.05f,.14f,.05f);column.localRotation=Quaternion.Euler(90,0,0);column.gameObject.layer=10;column.GetComponent<Renderer>().sharedMaterial=steeringMat;
 var vinyl=AssetDatabase.LoadAssetAtPath<Material>("Assets/External/RacoonCar/Black vinyl.mat");vinyl.SetColor("_BaseColor",new Color(.17f,.19f,.20f));EditorUtility.SetDirty(vinyl);
 var exterior=s.Car.Find("Racoon county exterior");foreach(var t in exterior.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Wheel"))t.gameObject.SetActive(false);var outer=Object.Instantiate(wheel,exterior);outer.transform.localPosition=pivot.localPosition;outer.transform.localRotation=Quaternion.identity;foreach(var t in outer.GetComponentsInChildren<Transform>())t.gameObject.layer=8;
 s.Properties[4].RevealLine="The front door strikes the wall.";
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);ServiceV12Contract.Run();ServiceQuickBuild.Build();
 }
}}
