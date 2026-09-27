using System;using System.IO;using System.Linq;using UnityEditor;using UnityEditor.Animations;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.Rendering;
namespace ServiceGameV2.Editor { public static class ServiceV12Build {
 static string Root="Assets/External/RacoonCar/";
 static Material Mat(string name,Color color,float smooth=.25f){var path=Root+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;}
 static Transform Empty(string name,Transform parent,Vector3 pos){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;return t;}
 static Transform Box(string name,Transform parent,Vector3 pos,Vector3 size,Material mat){var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(parent,false);t.localPosition=pos;t.localScale=size;UnityEngine.Object.DestroyImmediate(t.GetComponent<Collider>());t.GetComponent<Renderer>().sharedMaterial=mat;return t;}
 public static void Build(){
 EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");var s=UnityEngine.Object.FindAnyObjectByType<CountyScene>();
 var old=s.Car.Find("County vehicle interior");if(!old)throw new Exception("Original cabin missing");
 var previous=s.Car.Find("Racoon county cabin");if(previous){ServiceV12Contract.Run();ServiceQuickBuild.Build();return;}
 var imported=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"CountyCar.fbx");var cabin=UnityEngine.Object.Instantiate(imported,s.Car);cabin.name="Racoon county cabin";cabin.transform.localPosition=Vector3.zero;cabin.transform.localRotation=Quaternion.identity;
 var vinyl=Mat("Black vinyl",new Color(.08f,.087f,.09f),.2f);var scan=AssetDatabase.FindAssets("leather_white t:Material").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).FirstOrDefault();if(scan){vinyl.CopyPropertiesFromMaterial(scan);vinyl.SetColor("_BaseColor",new Color(.095f,.105f,.11f));}
 var paint=Mat("County graphite paint",new Color(.105f,.12f,.125f),.55f);var rubber=Mat("Rubber",new Color(.018f,.021f,.022f),.12f);var metal=Mat("Brushed fittings",new Color(.24f,.25f,.25f),.5f);metal.SetFloat("_Metallic",.6f);var dark=Mat("Recesses",new Color(.008f,.009f,.01f),.2f);var fabric=Mat("Seat cloth",new Color(.14f,.15f,.15f),.08f);
 var glass=Mat("Window glass",new Color(.14f,.19f,.2f,.035f),.93f);glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.renderQueue=3000;
 foreach(var r in cabin.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(m=>{string n=m?m.name.ToLower():"";return n.Contains("windows")?glass:n.Contains("interior.light")?fabric:n.Contains("interior")?vinyl:n.Contains("tire")?rubber:n.Contains("metal")?metal:n.Contains("white")||n.Contains("navmap")?dark:paint;}).ToArray();r.shadowCastingMode=ShadowCastingMode.Off;}
 // Match the exterior to the actual cabin. Keep original collision and exit anchors.
 foreach(var r in s.Car.GetComponentsInChildren<Renderer>(true))if(r.gameObject.layer==8)r.enabled=false;
 var exterior=UnityEngine.Object.Instantiate(cabin,s.Car);exterior.name="Racoon county exterior";foreach(var t in exterior.GetComponentsInChildren<Transform>())t.gameObject.layer=8;
 // Keep instrument mechanics while removing the old block cabin entirely.
 foreach(var t in new[]{s.SpeedNeedle.parent,s.RevNeedle.parent,s.Odometer.transform})t.SetParent(cabin.transform,false);
 s.SpeedNeedle.parent.localPosition=new Vector3(-.396f,1.218f,.487f);s.SpeedNeedle.parent.localRotation=Quaternion.identity;s.SpeedNeedle.parent.localScale=Vector3.one*.98f;
 s.RevNeedle.parent.localPosition=new Vector3(-.18f,1.205f,.55f);s.RevNeedle.parent.localRotation=Quaternion.identity;s.RevNeedle.parent.localScale=Vector3.one*.8f;
 s.Odometer.transform.localPosition=new Vector3(-.396f,1.176f,.473f);s.Odometer.characterSize=.0013f;
 var wheel=cabin.transform.Find("Wheel");if(!wheel)throw new Exception("Authored steering wheel missing");var pivot=Empty("Steering wheel pivot",cabin.transform,wheel.localPosition+wheel.localRotation*Vector3.Scale(wheel.GetComponent<MeshFilter>().sharedMesh.bounds.center,wheel.localScale));wheel.SetParent(pivot,true);s.SteeringWheel=pivot;
 foreach(string name in new[]{"Radio face","Radio preset","Radio tuning knob","RADIO OFF","88.5   FM"})foreach(var t in old.GetComponentsInChildren<Transform>(true).Where(t=>t.name==name).ToArray()){t.SetParent(cabin.transform,false);t.localPosition+=new Vector3(-.1f,.19f,.18f);}
 var radio=cabin.GetComponentsInChildren<TextMesh>().FirstOrDefault(t=>t.text=="RADIO OFF"||t.text.Contains("88.5"));if(radio){radio.text="RADIO OFF";radio.characterSize=.0016f;radio.transform.localPosition=new Vector3(.016f,1.036f,.46f);}
 var mirrorHousing=Box("Rearview mirror housing",cabin.transform,new Vector3(0,1.66f,.56f),new Vector3(.245f,.079f,.025f),rubber);
 Box("Mirror stem",cabin.transform,new Vector3(0,1.745f,.57f),new Vector3(.014f,.12f,.017f),rubber);
 var mirror=Empty("Rearview glass",cabin.transform,new Vector3(0,1.66f,.544f));var mesh=new Mesh{name="Mirror face with planar UVs"};mesh.vertices=new[]{new Vector3(-.112f,-.03f,0),new Vector3(.112f,-.03f,0),new Vector3(.112f,.03f,0),new Vector3(-.112f,.03f,0)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();var mp=Root+"Mirror.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(mp);if(existing){EditorUtility.CopySerialized(mesh,existing);mesh=existing;}else AssetDatabase.CreateAsset(mesh,mp);mirror.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;mirror.gameObject.AddComponent<MeshRenderer>().sharedMaterial=metal;
 for(int i=0;i<2;i++){var w=Empty("Rain wiper "+i,cabin.transform,new Vector3(-.57f+i*.74f,1.22f,.79f));w.localRotation=Quaternion.Euler(-27,0,0);Box("Arm",w,new Vector3(.17f,.045f,0),new Vector3(.35f,.009f,.013f),rubber);Box("Blade",w,new Vector3(.24f,.062f,0),new Vector3(.39f,.016f,.018f),rubber);}
 var light=Empty("Instrument spill",cabin.transform,new Vector3(-.32f,1.47f,-.1f)).gameObject.AddComponent<Light>();light.color=new Color(.72f,.74f,.68f);light.intensity=.65f;light.range=1.65f;light.cullingMask=1<<10;
 foreach(var t in cabin.GetComponentsInChildren<Transform>())t.gameObject.layer=10;
 old.gameObject.SetActive(false);s.Cockpit=cabin;s.DriverSeat.localPosition=new Vector3(-.395f,1.52f,-.22f);
 foreach(var name in new[]{"Vale creature","Demon presence"}){
 var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/ServiceArt/"+name+".controller");var machine=controller.layers[0].stateMachine;var state=machine.states.Select(x=>x.state).FirstOrDefault(x=>x.name=="Attack")??machine.AddState("Attack");string source=name.StartsWith("Vale")?"Assets/Creep Horror Creature/Meshes/Creep_mesh.fbx":"Assets/Demon Horror Creature with Weapon/Meshes/Demon.fbx";state.motion=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().First(c=>c.name.Contains(name.StartsWith("Vale")?"Punch_Action":"Punch1"));state.speed=2.2f;EditorUtility.SetDirty(controller);
 }
 s.Properties[1].RevealLine="A foot drags across the landing.";s.Properties[1].DeathLine="The study door is still open.";
 s.Properties[3].RevealLine="Something is breathing behind you.";s.Properties[3].DeathLine="You moved before it left.";
 s.Properties[4].RevealLine="The porch door slams behind you.";s.Properties[4].DeathLine="The porch light went out before anyone saw you.";
 s.Properties[5].RevealLine="A second set of footsteps crosses the gallery.";s.Properties[5].DeathLine="Something followed you down.";
 foreach(var id in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Audio/V12"})){var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(id));var settings=importer.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;settings.loadType=AudioClipLoadType.CompressedInMemory;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);ServiceV12Contract.Run();ServiceQuickBuild.Build();
 }
}}
