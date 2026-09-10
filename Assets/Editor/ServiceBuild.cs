using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace ServiceGameV2.Editor
{
 public static partial class ServiceBuild
 {
  public static bool SceneOnly;
  const string FG="Assets/Flooded_Grounds/";
  const string Art="Assets/ServiceArt/";
  static string Work=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
  static readonly Dictionary<Material,Material> converted=new Dictionary<Material,Material>();
  static readonly Dictionary<string,GameObject> templates=new Dictionary<string,GameObject>();
  static Material dark,metal,paper,road;
  static List<Vector3> route;
  static CountyScene scene;
  static Transform world;
  static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);

  public static void Build()
  {
   Directory.CreateDirectory(Art+"Materials");Directory.CreateDirectory(Art+"Meshes");Directory.CreateDirectory(Art+"Templates");
   AssetDatabase.Refresh(); converted.Clear();templates.Clear();
   CaptureBuildings();
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var root=new GameObject("SERVICE — Hollis County");scene=root.AddComponent<CountyScene>();root.AddComponent<ServiceDirector>();world=root.transform;
   dark=Mat("Cabin charcoal",new Color(.028f,.033f,.031f));
   metal=Mat("Dull painted metal",new Color(.19f,.21f,.19f));
   paper=Mat("County stationery",new Color(.76f,.75f,.66f));
   road=Mat("County asphalt",new Color(.72f,.73f,.70f),"GR_Asphalt1_AS");road.SetFloat("_Smoothness",.15f);
   var points=new[]{V(0,0,-27),V(0,0,20),V(0,0,100),V(17,0,145),V(36,0,190),V(36,0,247),V(13,0,297),V(-4,0,349),V(-4,0,420),V(-4,0,506)};
   route=Curve(points);
   TerrainLand();
   Ribbon("Millbrook and Latigo",route,5.8f,road,world);
   var asphalt=world.Find("Millbrook and Latigo");asphalt.gameObject.AddComponent<MeshCollider>().sharedMesh=asphalt.GetComponent<MeshFilter>().sharedMesh;asphalt.gameObject.AddComponent<ServiceSurface>().Kind="stone";
   scene.Route=points.Select((p,i)=>Empty("Survey point "+i,p,world)).ToArray();
   scene.Depot=Empty("Depot parking bay",V(0,.03f,0),world);
   scene.LateThreshold=Empty("End of county survey",V(-4,0,385),world);
   scene.LateRoad=new GameObject("Beyond the survey — October 09");scene.LateRoad.transform.SetParent(world);
   CreateFirstShift();
   scene.Properties[2].transform.SetParent(scene.LateRoad.transform,true);
   var depot=Building("Pref_IndBuilding2_A",V(-17,0,-3),90,.6f,world);
   LightAt("Depot sodium lamp",V(-8,4,-3),new Color(1,.68f,.34f),3.2f,19,world);
   Sign("HOLLIS COUNTY\nCIVIL PROCESS\nRETURN COPIES HERE",V(-8,1.8f,-5),90,.12f,world);
   Sign("MILLBROOK RD",V(5,1.9f,36),0,.10f,world);
   Sign("LATIGO TRAIL",V(41,1.9f,189),0,.1f,world);
   Sign("END COUNTY\nMAINTENANCE",V(1,1.65f,381),0,.095f,world);
   DressDenseCounty();
   Vehicle(); Lighting();
   scene.Walker=new GameObject("Field officer — on foot").AddComponent<CharacterController>();scene.Walker.transform.SetParent(world);scene.Walker.height=1.8f;scene.Walker.radius=.3f;scene.Walker.center=V(0,.9f,0);scene.Walker.stepOffset=.32f;
   scene.View=new GameObject("First person view").AddComponent<Camera>();scene.View.tag="MainCamera";scene.View.gameObject.AddComponent<AudioListener>();scene.View.transform.SetParent(scene.DriverSeat,false);scene.View.nearClipPlane=.035f;scene.View.farClipPlane=230;scene.View.fieldOfView=68;scene.View.backgroundColor=new Color(.065f,.078f,.085f);scene.View.clearFlags=CameraClearFlags.Skybox;
   var cameraData=scene.View.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
   scene.Flashlight=LightAt("Hand torch",Vector3.zero,new Color(.93f,.91f,.80f),4.2f,24,scene.View.transform);scene.Flashlight.transform.localPosition=V(.12f,-.14f,.15f);scene.Flashlight.type=LightType.Spot;scene.Flashlight.spotAngle=54;scene.Flashlight.innerSpotAngle=25;scene.Flashlight.shadows=LightShadows.Soft;scene.Flashlight.enabled=false;
   Creature(); CreatureVariants();
   root.AddComponent<ServiceWorldText>();
   foreach(var text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
    text.font.RequestCharactersInTexture(text.text+"0123456789. mi",text.fontSize,text.fontStyle);
    string path=Art+"Materials/World type "+text.font.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Service/World Type"));AssetDatabase.CreateAsset(mat,path);}text.GetComponent<Renderer>().sharedMaterial=mat;
   }
   ServiceWorldText.Refresh(null);
   ConfigureProject();
   BakeNavigation();
   EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),"Assets/Scenes/HollisCounty.unity");
   AssetDatabase.SaveAssets();
   ValidateScene();
   RenderPreviews();
   if(SceneOnly || Environment.GetCommandLineArgs().Contains("-serviceSceneOnly"))return;
   var output=Path.Combine(Work,"Build/Service.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/HollisCounty.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   File.WriteAllText(Path.Combine(Work,"build-summary.txt"),report.summary.result+" / errors "+report.summary.totalErrors+" / "+report.summary.totalSize+" bytes");
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
  }
  static void CaptureBuildings()
  {
   var src=EditorSceneManager.OpenScene(FG+"Scenes/PreAsembeld_Buildings.unity");
   foreach(string name in new[]{"Pref_Cabin1_A","Pref_Cabin2_A","Pref_IndBuilding2_A","Pref_Villa1_A","Pref_Villa2_A","Pref_BrickHouse_A","Pref_Barn1_A"})
   {
    var o=src.GetRootGameObjects().First(x=>x.name==name);o.transform.position=Vector3.zero;
    foreach(var t in o.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
    var path=Art+"Templates/"+name+".prefab";PrefabUtility.SaveAsPrefabAsset(o,path);templates[name]=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   }
  }
  static Bounds BoundsOf(GameObject o)
  {var rr=o.GetComponentsInChildren<Renderer>(true);var b=rr.Length>0?rr[0].bounds:new Bounds(o.transform.position,Vector3.zero);foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
  static Transform Empty(string name,Vector3 pos,Transform parent){var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.position=pos;return o.transform;}
  static GameObject Building(string name,Vector3 pos,float yaw,float scale,Transform parent)
  {
   var o=(GameObject)PrefabUtility.InstantiatePrefab(templates[name]);PrefabUtility.UnpackPrefabInstance(o,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   o.transform.SetParent(parent,false);o.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));o.transform.localScale=Vector3.one*scale;
   var b=BoundsOf(o);o.transform.position=pos-V(b.center.x,b.min.y,b.center.z);Convert(o);return o;
  }
  static GameObject Prop(string partial,Vector3 pos,float yaw,float scale,Transform parent,bool ground=true)
  {
   if(!propPaths.TryGetValue(partial,out string path)){path=AssetDatabase.FindAssets(partial+" t:Prefab",new[]{FG+"Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>Path.GetFileNameWithoutExtension(p)==partial);propPaths[partial]=path;}
   if(path==null)throw new Exception("Missing licensed prop "+partial);
   var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
   PrefabUtility.UnpackPrefabInstance(o,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   o.transform.SetParent(parent,false);o.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,yaw,0));o.transform.localScale=Vector3.one*scale;
   var b=BoundsOf(o);o.transform.position=pos-(ground?V(b.center.x,b.min.y,b.center.z):b.center);Convert(o);return o;
  }
  static void Convert(GameObject o)
  {
   foreach(var t in o.GetComponentsInChildren<Transform>(true)){GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);t.gameObject.layer=0;}
   foreach(var r in o.GetComponentsInChildren<Renderer>(true)){r.sharedMaterials=r.sharedMaterials.Select(ConvertMat).ToArray();foreach(var m in r.sharedMaterials)if(m)m.enableInstancing=true;}
  }
  static Material ConvertMat(Material old)
  {
   if(old==null)return dark;if(old.shader!=null&&old.shader.name.StartsWith("Universal Render Pipeline"))return old;
   if(converted.TryGetValue(old,out var result))return result;
   var n=new Material(Shader.Find("Universal Render Pipeline/Lit"));n.name=old.name+" URP";
   Texture tex=old.HasProperty("_MainTex")?old.GetTexture("_MainTex"):null;
   n.SetTexture("_BaseMap",tex);n.SetColor("_BaseColor",old.HasProperty("_Color")?old.GetColor("_Color"):Color.white);
   foreach(var key in new[]{"_BumpMap","_MetallicGlossMap","_OcclusionMap"})if(old.HasProperty(key))n.SetTexture(key,old.GetTexture(key));
   n.SetFloat("_Smoothness",.22f);if(n.GetTexture("_BumpMap"))n.EnableKeyword("_NORMALMAP");if(n.GetTexture("_MetallicGlossMap"))n.EnableKeyword("_METALLICSPECGLOSSMAP");
   bool leaf=old.name.ToLower().Contains("leaf")||old.name.ToLower().Contains("leaves")||old.name.ToLower().Contains("grass")||old.name.ToLower().Contains("branch");
   if(leaf){n.SetFloat("_AlphaClip",1);n.SetFloat("_Cutoff",.42f);n.SetFloat("_Cull",0);n.EnableKeyword("_ALPHATEST_ON");n.renderQueue=2450;}
   if(old.name.ToLower().Contains("glass")){n.SetFloat("_Surface",1);n.SetFloat("_SrcBlend",5);n.SetFloat("_DstBlend",10);n.SetFloat("_ZWrite",0);n.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");n.SetColor("_BaseColor",new Color(.37f,.42f,.40f,.22f));n.SetFloat("_Smoothness",.8f);n.renderQueue=3000;}
   string path=Art+"Materials/"+old.name.Replace('/','_')+"_"+converted.Count+".mat";AssetDatabase.CreateAsset(n,path);converted[old]=n;return n;
  }
  static Material Mat(string name,Color c,string texture=null)
  {
   var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",.15f);
   if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(FG+"Content/Textures/"+texture+".tif"));
   AssetDatabase.CreateAsset(m,Art+"Materials/"+name+".mat");return m;
  }
  static GameObject Box(string name,Vector3 pos,Vector3 size,Material m,Transform parent,bool collider=true)
  {
   var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;if(!collider)Object.DestroyImmediate(o.GetComponent<Collider>());return o;
  }
  static Light LightAt(string name,Vector3 pos,Color color,float intensity,float range,Transform parent)
  {var l=Empty(name,pos,parent).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.renderMode=LightRenderMode.ForcePixel;return l;}
  static TextMesh Label(string text,Vector3 pos,float yaw,float size,Color color,Transform parent)
  {
   var t=Empty(text,pos,parent).gameObject.AddComponent<TextMesh>();t.text=text;t.characterSize=size*.15625f;t.fontSize=64;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;t.transform.rotation=Quaternion.Euler(0,yaw,0);
   t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/CourierPrime-Regular.ttf");t.GetComponent<Renderer>().sharedMaterial=t.font.material;return t;
  }
  static void Sign(string text,Vector3 pos,float yaw,float size,Transform parent)
  {
   var mount=Empty("County sign",pos,parent);mount.rotation=Quaternion.Euler(0,yaw,0);
   Box("Galvanized post",V(0,-.9f,.09f),V(.06f,1.9f,.06f),metal,mount);
   Box("Painted signboard",Vector3.zero,V(text.Contains("\n")?1.45f:1.8f,text.Contains("\n")?.72f:.32f,.045f),dark,mount);
   Label(text,pos-mount.forward*.03f,yaw,size,new Color(.76f,.79f,.71f),mount);
  }
  static ServiceProperty Property(int index,string template,Vector3 position,float yaw)
  {
   var holder=Empty(index==0?"214 Millbrook — Correll":index==1?"77 Latigo — Vale":"Unsurveyed parcel",position,world);
   var p=holder.gameObject.AddComponent<ServiceProperty>();p.Index=index;
   if(index==1)return Villa(p,holder,position,yaw);
   var house=Building(template,position,yaw,.72f,holder);Vector3 outward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
   // Collider probes place the front wall at local z=2.31; the stair anchor is z=4.
   Vector3 at=house.transform.TransformPoint(V(3.5f,1.08f,2.39f));
   var actual=Prop("Cabin1_Door_A",at,yaw,.72f,holder);p.DoorPanel=actual.transform;
   p.Door=Empty("Serve at threshold",at,holder);p.Door.rotation=Quaternion.LookRotation(outward);
   Vector3 gate=at+outward*14;gate.y=.02f;p.Gate=Empty("Driveway entrance",gate,holder);p.Gate.rotation=Quaternion.LookRotation(-outward);
   Vector3 roadpoint=V(index==1?36:index==0?0:-4,0,gate.z);
   var driveEnd=at+outward*3.1f;driveEnd.y=.025f;
   Ribbon("Gravel driveway",new List<Vector3>{roadpoint,gate,driveEnd},3.4f,Mat("Driveway "+index,new Color(.68f,.66f,.59f),"GR_Dirt1_AS"),holder);
   p.PorchLight=LightAt("Porch tungsten bulb",at+outward*.4f+Vector3.up*2.4f,new Color(1,.62f,.30f),2.7f,12,holder);p.PorchLight.shadows=LightShadows.Soft;
   Prop("Prop_Lamp_E",at+outward*.13f+Vector3.up*2.4f,yaw,.55f,holder,false);
   p.WindowLight=LightAt("Occupied back room",at-outward*2+Vector3.up*1.5f,new Color(1,.70f,.41f),1.7f,8,holder);
   p.SoundPoint=Empty("Behind the closed door",at-outward*2,holder);
   p.AddressLabel=Label(index==1?"77":"214",at+outward*.12f+Vector3.up*1.85f,yaw+180,.095f,new Color(.79f,.78f,.70f),holder);
   p.PostedPaper=Box("Posted county copy",Vector3.zero,V(.21f,.29f,.006f),paper,holder,false);p.PostedPaper.transform.position=at+outward*.13f+Vector3.up*1.1f;p.PostedPaper.transform.rotation=Quaternion.Euler(0,yaw,2);p.PostedPaper.SetActive(false);
   var curtains=new List<Renderer>();
   if(index!=1)
   {
    var cotton=Mat("Curtain cotton "+index,new Color(.22f,.18f,.13f));
    foreach(float x in new[]{0f,-3.5f})
    {
     Vector3 center=house.transform.TransformPoint(V(x,2.65f,2.28f));
     var curtain=Box("Drawn front curtain",Vector3.zero,V(.98f,1.9f,.025f),cotton,holder,false);curtain.transform.SetPositionAndRotation(center,Quaternion.Euler(0,yaw,0));curtains.Add(curtain.GetComponent<Renderer>());
     Prop("Cabin1_Deco_WindowGlass_A",center+outward*.045f,yaw-90,.72f,holder,false);
    }
   }
   p.Curtains=curtains.ToArray();
   for(int side=-1;side<=1;side+=2)
   {
    Vector3 tangent=Vector3.Cross(Vector3.up,outward);
    for(int f=0;f<3;f++)Prop("Struct_Fence1_Mid_C",gate+tangent*side*(4.2f+f*4.4f),yaw,.7f,holder);
   }
   // One seat on the porch, parallel to the wall, looking into the yard.
   if(index==0)Prop("Prop_ParkBench_A",house.transform.TransformPoint(V(-1.0f,1.08f,3.35f)),yaw,.62f,holder);
   Prop("Prop_Rug_A",house.transform.TransformPoint(V(3.5f,1.09f,3.15f)),yaw,.32f,holder);
   return p;
  }
  static ServiceProperty Villa(ServiceProperty p,Transform holder,Vector3 position,float yaw)
  {
   var house=Building("Pref_Villa1_A",position,yaw,.55f,holder);p.Building=house.transform;
   Vector3 outward=house.transform.forward;
   Vector3 at=house.transform.TransformPoint(V(14,1.03f,16.25f));
   p.Door=Empty("Villa entrance — open passage",at,holder);p.Door.rotation=Quaternion.LookRotation(outward);
   Vector3 gate=at+outward*17;gate.y=.03f;p.Gate=Empty("Villa parking approach",gate,holder);p.Gate.rotation=Quaternion.LookRotation(-outward);
   var driveEnd=at+outward*2;driveEnd.y=.025f;
   Ribbon("Vale gravel approach",new List<Vector3>{V(36,0,gate.z),gate,driveEnd},3.8f,Mat("Vale driveway",new Color(.61f,.6f,.55f),"GR_Dirt1_AS"),holder);
   p.PorchLight=LightAt("Vale entrance bulb",at+outward*.6f+Vector3.up*2.8f,new Color(1,.65f,.35f),3.5f,12,holder);p.PorchLight.shadows=LightShadows.Soft;
   Prop("Prop_Lamp_E",at+outward*.13f+Vector3.up*2.8f,yaw,.6f,holder,false);
   p.AddressLabel=Label("77",at+house.transform.right*1.2f+Vector3.up*1.7f,yaw+180,.12f,new Color(.82f,.8f,.69f),holder);
   Sign("VALE HOUSE\nDELIVER TO STUDY\nSECOND FLOOR",at+outward*2+house.transform.right*2,yaw+180,.095f,holder);
   Vector3 tableAt=house.transform.TransformPoint(V(21,8.04f,12.8f));
   var table=Prop("Prop_LargeTable_A",tableAt,yaw,.55f,holder);
   var oldCenter=BoundsOf(table).center;var tableScale=table.transform.localScale;tableScale.x*=.75f;table.transform.localScale=tableScale;table.transform.position+=oldCenter-BoundsOf(table).center;
   var bounds=BoundsOf(table);p.DeliveryPoint=Empty("Delivery table",V(bounds.center.x,bounds.max.y+.03f,bounds.center.z),holder);
   p.TableApproach=Empty("Upstairs study approach",house.transform.TransformPoint(V(21,8.04f,10.4f)),holder);
   p.WindowLight=LightAt("Hall lamp",p.DeliveryPoint.position+Vector3.up*1.7f,new Color(1,.70f,.4f),2.5f,10,holder);p.WindowLight.shadows=LightShadows.Soft;
   Prop("Prop_Lamp_A",p.DeliveryPoint.position+house.transform.right*.25f,0,.3f,holder);
   Prop("Prop_Chair_B",tableAt-house.transform.right*1.65f,yaw+95,.65f,holder);
   p.SoundPoint=Empty("Footsteps upstairs",house.transform.TransformPoint(V(22,8.04f,12)),holder);
   p.Curtains=new Renderer[0];
   p.PostedPaper=Box("Notice left on hall table",Vector3.zero,V(.24f,.006f,.32f),paper,holder,false);p.PostedPaper.transform.position=p.DeliveryPoint.position;p.PostedPaper.transform.rotation=Quaternion.Euler(0,yaw+8,0);p.PostedPaper.SetActive(false);
   p.EntitySpawn=Empty("Entity emerging from upstairs hall",house.transform.TransformPoint(V(30,8.08f,12)),holder);
   p.EntitySpawn.rotation=Quaternion.LookRotation((p.TableApproach.position-p.EntitySpawn.position).normalized);
   DressVilla(p,house.transform,yaw,holder);
   return p;
  }
  static void DressVilla(ServiceProperty p,Transform house,float yaw,Transform holder)
  {
   Vector3 At(float x,float y,float z)=>house.TransformPoint(V(x,y,z));
   GameObject Furnish(string name,float x,float y,float z,float turn,float scale)=>Prop(name,At(x,y,z),yaw+turn,scale,holder);
   // Foyer: a waiting area and an empty hall table; all furniture sits outside the circulation lane.
   Furnish("Prop_Sofa_A",7,1.04f,14.8f,180,.65f);
   Furnish("Prop_SmallTable_B",7,1.04f,12,0,.65f);
   Furnish("Prop_Rug_C",7,1.06f,12,0,.9f);
   Furnish("Prop_Cabinet_A",19,1.04f,8.8f,0,.58f);
   Furnish("Prop_Clock_A",25,1.04f,14.8f,180,.58f);
   Furnish("Prop_Rug_A",14,1.05f,12,90,.9f);
   // Landing and study: furniture backs against solid wall sections, not doorways.
   Furnish("Prop_Cabinet_B",18,8.04f,8.8f,0,.58f);
   Furnish("Prop_Sofa_A",7,8.04f,14.8f,180,.65f);
   Furnish("Prop_Rug_D",22,8.06f,11.8f,0,.85f);
   var sideTable=Furnish("Prop_SmallTable_A",11,8.04f,-2.8f,0,.7f);var top=BoundsOf(sideTable);Prop("Prop_Vase_B",V(top.center.x,top.max.y,top.center.z),yaw,.65f,holder);
   foreach(float level in new[]{1f,8f})
    Prop("Prop_Painting_C",At(26,level+3.2f,8.45f),yaw,.7f,holder,false);
   var lamps=new List<Light>();
   foreach(var v in new[]{V(14,5,12),V(11,5,6),V(2,8,4),V(10,12,2),V(14,12,9),V(26,12,12)}){
    var lamp=Prop("Prop_Lamp_C",At(v.x,v.y,v.z),yaw,.35f,holder,false);
    var ceiling=At(v.x,v.y<=8?7.9f:14.9f,v.z);var center=BoundsOf(lamp).max;center.x=At(v.x,v.y,v.z).x;center.z=At(v.x,v.y,v.z).z;
    var cord=Box("Pendant cord",Vector3.zero,V(.012f,Mathf.Max(.1f,ceiling.y-center.y),.012f),dark,holder,false);cord.transform.position=(center+ceiling)*.5f;
    var light=LightAt("Vale pendant",At(v.x,v.y-.3f,v.z),new Color(1,.67f,.37f),1.35f,6,holder);lamps.Add(light);
   }
   p.EncounterLights=lamps.ToArray();
   // Readable diegetic directions at the two decision points.
   void Plaque(string text,Vector3 position,float facing,float width,float height,float size){
    var board=Box("Room directions",Vector3.zero,V(width,height,.025f),dark,holder,false);
    board.transform.SetPositionAndRotation(position,Quaternion.Euler(0,facing,0));
    Label(text,position-board.transform.forward*.016f,facing,size,new Color(.85f,.81f,.69f),holder);
   }
   Plaque("STUDY UPSTAIRS\nRIGHT STAIRCASE",At(12,4,8.45f),yaw+180,.75f,.29f,.067f);
   Plaque("STUDY",At(12,11.2f,7.55f),yaw,.48f,.19f,.085f);
   var reverb=Empty("Villa acoustics",At(20,5,6),holder).gameObject.AddComponent<AudioReverbZone>();reverb.reverbPreset=AudioReverbPreset.StoneCorridor;reverb.minDistance=5;reverb.maxDistance=13;
  }
  static List<Vector3> Curve(Vector3[] p)
  {
   var result=new List<Vector3>();
   for(int i=0;i<p.Length-1;i++)for(int j=0;j<12;j++){
    float t=j/12f;Vector3 a=p[Mathf.Max(0,i-1)],b=p[i],c=p[i+1],d=p[Mathf.Min(p.Length-1,i+2)];
    result.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
   }result.Add(p[p.Length-1]);return result;
  }
  static void Ribbon(string name,List<Vector3> points,float width,Material mat,Transform parent)
  {
   var vv=new List<Vector3>();var uv=new List<Vector2>();var tt=new List<int>();float length=0;
   for(int i=0;i<points.Count;i++){
    if(i>0)length+=Vector3.Distance(points[i],points[i-1]);var dir=(points[Mathf.Min(i+1,points.Count-1)]-points[Mathf.Max(i-1,0)]).normalized;var side=Vector3.Cross(Vector3.up,dir)*width*.5f;
    vv.Add(points[i]-side+Vector3.up*.027f);vv.Add(points[i]+side+Vector3.up*.027f);uv.Add(new Vector2(0,length/5));uv.Add(new Vector2(width/5,length/5));
    if(i>0){int k=i*2;tt.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}
   }
   var mesh=new Mesh{name=name};mesh.SetVertices(vv);mesh.SetUVs(0,uv);mesh.SetTriangles(tt,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Art+"Meshes/"+name+"_"+Guid.NewGuid().ToString("N").Substring(0,6)+".asset");
   var o=new GameObject(name);o.transform.SetParent(parent,true);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=mat;
  }
  static float RoadDistance(Vector3 p){float best=float.MaxValue;foreach(var q in route){float d=(p-q).sqrMagnitude;if(d<best)best=d;}return Mathf.Sqrt(best);}
  static float LandHeight(float x,float z)
  {
   float d=RoadDistance(V(x,0,z));
   foreach(var h in PropertyCenters)d=Mathf.Min(d,Mathf.Max(0,Vector3.Distance(V(x,0,z),h)-43));
   return Mathf.SmoothStep(0,1,Mathf.Clamp01((d-15)/40))*(5+3*Mathf.Sin(z*.019f+x*.023f));
  }
  static void TerrainLand()
  {
   var data=new TerrainData{heightmapResolution=257,size=V(360,35,650),alphamapResolution=256,baseMapResolution=512};
   var hh=new float[257,257];for(int z=0;z<257;z++)for(int x=0;x<257;x++)hh[z,x]=(2+LandHeight(-170+x*360/256f,-85+z*650/256f))/35;
   data.SetHeights(0,0,hh);
   var layers=new List<TerrainLayer>();foreach(var n in new[]{"GR_Moss1_AS","GR_Dirt1_AS"}){
    var layer=new TerrainLayer{diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(FG+"Content/Textures/"+n+".tif"),tileSize=new Vector2(7,7),smoothness=.1f};AssetDatabase.CreateAsset(layer,Art+n+".terrainlayer");layers.Add(layer);}
   data.terrainLayers=layers.ToArray();var aa=new float[256,256,2];for(int z=0;z<256;z++)for(int x=0;x<256;x++){float d=RoadDistance(V(-170+x*360/255f,0,-85+z*650/255f));float dirt=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,10,d));aa[z,x,0]=1-dirt;aa[z,x,1]=dirt;}data.SetAlphamaps(0,0,aa);
   AssetDatabase.CreateAsset(data,Art+"Hollis terrain.asset");var o=Terrain.CreateTerrainGameObject(data);o.name="Hollis County terrain";o.transform.SetParent(world);o.transform.position=V(-170,-2,-85);var terrain=o.GetComponent<Terrain>();terrain.heightmapPixelError=8;terrain.drawInstanced=true;terrain.materialTemplate=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));AssetDatabase.CreateAsset(terrain.materialTemplate,Art+"Materials/Terrain URP.mat");
  }
  static void DressRoad()
  {
   var vegetation=Empty("Authored roadside vegetation",Vector3.zero,world);
   // Fixed placements saved in the scene. Paired verges deliberately leave driveway sightlines clear.
   for(int i=0;i<route.Count;i+=2)foreach(int side in new[]{-1,1})
   {
    var p=route[i];var dir=(route[Mathf.Min(i+1,route.Count-1)]-route[Mathf.Max(0,i-1)]).normalized;var right=Vector3.Cross(Vector3.up,dir);
    for(int row=0;row<2;row++){
     Vector3 at=p+right*side*(12+row*14+3*Mathf.Sin(i*1.7f+side));
     if(new[]{V(-24,0,99),V(67,0,235),V(-28,0,469),V(-17,0,-3)}.Any(h=>Vector3.Distance(h,at)<29))continue;
     at+=dir*(3*Mathf.Sin(i*2.7f+row*9+side));
     at.y=LandHeight(at.x,at.z);var tree=Prop(new[]{"TreeCreator_Tall_C","TreeCreator_Crinkly_A","TreeCreator_Tall_B","TreeCreator_Small_A"}[(i/2+row+(side==1?2:0))%4],at,i*137+row*47,.63f+(i%5)*.045f,vegetation);
     if(tree.GetComponentsInChildren<Renderer>().Length==0)Debug.LogWarning("Tree missing renderer "+tree.name);
    }

    for(int g=0;g<2;g++){var at=p+right*side*(3.4f+g*.75f);if(scene.Properties.Any(h=>Vector3.Distance(h.Gate.position,at)<7))continue;at.y=LandHeight(at.x,at.z);Prop(g==0?"Grass_Small_C":"Grass_Tall_A",at,i*47+g*91,.65f,vegetation);}
   }
  }
  static void Vehicle()
  {
   scene.Car=Empty("County sedan",V(0,.08f,0),world);scene.Car.gameObject.layer=8;
   scene.CarBody=scene.Car.gameObject.AddComponent<CharacterController>();scene.CarBody.height=1.45f;scene.CarBody.radius=.82f;scene.CarBody.center=V(0,.86f,0);scene.CarBody.stepOffset=.18f;
   var shell=Prop("Prop_Car_A",Vector3.zero,270,.75f,scene.Car);shell.transform.localPosition+=V(0,.03f,0);scene.CarExterior=shell.GetComponentsInChildren<Renderer>();
   foreach(var t in shell.GetComponentsInChildren<Transform>())t.gameObject.layer=8;foreach(var c in shell.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
   scene.DriverSeat=Empty("Driver eye position",V(-.35f,1.19f,.12f),scene.Car);scene.DriverSeat.localPosition=V(-.35f,1.19f,.12f);
   scene.ExitLeft=Empty("Left door clearance",Vector3.zero,scene.Car);scene.ExitLeft.localPosition=V(-1.7f,.1f,0);
   scene.ExitRight=Empty("Right door clearance",Vector3.zero,scene.Car);scene.ExitRight.localPosition=V(1.7f,.1f,0);
   ServiceCockpit.Create(scene);
   foreach(float side in new[]{-.62f,.62f}){var h=LightAt("Low beam",Vector3.zero,new Color(1,.91f,.73f),7,57,scene.Car);h.transform.localPosition=V(side,.65f,2.22f);h.transform.localRotation=Quaternion.Euler(7,0,0);h.type=LightType.Spot;h.spotAngle=65;h.innerSpotAngle=34;h.shadows=LightShadows.Soft;}
  }
  static void Lighting()
  {
   var sky=new Material(Shader.Find("Skybox/Cubemap"));sky.SetTexture("_Tex",AssetDatabase.LoadAssetAtPath<Cubemap>(FG+"Content/Textures/BGR_Sky1.tif"));sky.SetFloat("_Exposure",.45f);sky.SetColor("_Tint",new Color(.42f,.47f,.53f));AssetDatabase.CreateAsset(sky,Art+"Materials/Hollis sky.mat");RenderSettings.skybox=sky;
   RenderSettings.ambientMode=AmbientMode.Custom;var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.095f,.115f,.14f));RenderSettings.ambientProbe=sh;RenderSettings.ambientIntensity=1;
   scene.Moon=LightAt("Last light over Hollis County",V(0,80,0),new Color(.67f,.77f,.88f),.36f,1000,world);scene.Moon.type=LightType.Directional;scene.Moon.shadows=LightShadows.Soft;scene.Moon.transform.rotation=Quaternion.Euler(23,-28,0);RenderSettings.sun=scene.Moon;
   var volume=Empty("County color grade",Vector3.zero,world).gameObject.AddComponent<Volume>();volume.isGlobal=true;var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Art+"County grade.asset");volume.sharedProfile=profile;
   var tone=profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);var grade=profile.Add<ColorAdjustments>();grade.postExposure.Override(.4f);grade.saturation.Override(-13);grade.contrast.Override(9);var vignette=profile.Add<Vignette>();vignette.intensity.Override(.2f);vignette.smoothness.Override(.45f);
   foreach(var effect in profile.components)AssetDatabase.AddObjectToAsset(effect,profile);
  }
  static void ConfigureProject()
  {
   var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");GraphicsSettings.defaultRenderPipeline=rp;QualitySettings.renderPipeline=rp;rp.shadowDistance=65;rp.msaaSampleCount=2;
   PlayerSettings.companyName="Hollis County";PlayerSettings.productName="SERVICE";PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.resizableWindow=true;
   PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
   var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null)input.intValue=1;settings.ApplyModifiedPropertiesWithoutUndo();
   QualitySettings.vSyncCount=1;QualitySettings.lodBias=1.4f;QualitySettings.maximumLODLevel=0;EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/HollisCounty.unity",true)};
  }
  static void BakeNavigation()
  {
   Physics.SyncTransforms();var sources=new List<UnityEngine.AI.NavMeshBuildSource>();
   var bounds=new Bounds(V(12,10,222),V(300,42,415));
   UnityEngine.AI.NavMeshBuilder.CollectSources(bounds,~((1<<8)|(1<<9)),UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders,0,new List<UnityEngine.AI.NavMeshBuildMarkup>(),sources);
   var settings=UnityEngine.AI.NavMesh.GetSettingsByID(0);settings.agentRadius=.28f;settings.agentHeight=1.8f;settings.agentClimb=.32f;settings.agentSlope=46;settings.overrideVoxelSize=true;settings.voxelSize=.065f;
   scene.Navigation=UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
   if(!scene.Navigation)throw new Exception("Villa navigation failed");AssetDatabase.CreateAsset(scene.Navigation,Art+"Vale navigation.asset");
  }
  static void Creature()
  {
   const string source="Assets/Creep Horror Creature/";
   scene.Entity=Empty("The presence in Vale House",scene.Properties[1].EntitySpawn.position,world).gameObject;
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source+"Prefabs/Creep1.prefab"));
   PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   model.transform.SetParent(scene.Entity.transform,false);model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
   var b=BoundsOf(model);model.transform.localScale*=2.1f/b.size.y;b=BoundsOf(model);model.transform.position+=V(scene.Entity.transform.position.x-b.center.x,scene.Entity.transform.position.y-b.min.y,scene.Entity.transform.position.z-b.center.z);
   Convert(model);
   var clips=AssetDatabase.LoadAllAssetsAtPath(source+"Meshes/Creep_mesh.fbx").OfType<AnimationClip>().ToArray();
   AnimationClip Clip(string name,bool loop){var c=Object.Instantiate(clips.First(x=>x.name.Contains(name)));c.name=name;var settings=AnimationUtility.GetAnimationClipSettings(c);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(c,settings);AssetDatabase.CreateAsset(c,Art+name+".anim");return c;}
   var controller=new UnityEditor.Animations.AnimatorController();AssetDatabase.CreateAsset(controller,Art+"Vale creature.controller");controller.AddLayer("Base Layer");controller.AddParameter("Moving",AnimatorControllerParameterType.Bool);
   var machine=controller.layers[0].stateMachine;var idle=machine.AddState("Watching");idle.motion=Clip("Idle1_Action",true);var walk=machine.AddState("Pursuing");walk.motion=Clip("Walk1_Action",true);machine.defaultState=idle;
   var go=idle.AddTransition(walk);go.hasExitTime=false;go.duration=.2f;go.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If,0,"Moving");
   var stop=walk.AddTransition(idle);stop.hasExitTime=false;stop.duration=.2f;stop.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot,0,"Moving");
   foreach(var a in model.GetComponentsInChildren<Animator>(true)){a.runtimeAnimatorController=controller;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
   foreach(var t in scene.Entity.GetComponentsInChildren<Transform>(true))t.gameObject.layer=9;
   foreach(var c in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
   var agent=scene.Entity.AddComponent<UnityEngine.AI.NavMeshAgent>();agent.radius=.28f;agent.height=1.8f;agent.speed=2.2f;agent.acceleration=6;agent.angularSpeed=180;agent.stoppingDistance=.65f;
   scene.Entity.SetActive(false);
  }
  static void ValidateScene()
  {
   var missing=new List<string>();foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var m in r.sharedMaterials)if(m==null||m.shader==null)missing.Add(r.name);
   foreach(var a in new[]{"night","wind","engine","dog","footstep","knock","paper","door","start","starter","tension","reveal","growl"})if(!Resources.Load<AudioClip>("Audio/"+a))missing.Add("Audio "+a);
   if(!scene.Entity||!scene.Entity.GetComponent<UnityEngine.AI.NavMeshAgent>()||!scene.Entity.GetComponentInChildren<Animator>(true)?.runtimeAnimatorController)missing.Add("Animated creature");
   if(!scene.Navigation||!scene.Properties[1].DeliveryPoint)missing.Add("Villa encounter references");
   var nav=UnityEngine.AI.NavMesh.AddNavMeshData(scene.Navigation);var villa=scene.Properties[1];
   foreach(var from in new[]{villa.EntitySpawn.position,villa.TableApproach.position}){var path=new UnityEngine.AI.NavMeshPath();if(!UnityEngine.AI.NavMesh.CalculatePath(from,villa.Gate.position,UnityEngine.AI.NavMesh.AllAreas,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)missing.Add("Disconnected upstairs route from "+from);}
   nav.Remove();
   var expandedNav=UnityEngine.AI.NavMesh.AddNavMeshData(scene.Navigation);
   foreach(var property in scene.Properties.Where(p=>p.HasEncounter))foreach(var start in new[]{property.TableApproach,property.EntitySpawn}){var path=new UnityEngine.AI.NavMeshPath();if(!UnityEngine.AI.NavMesh.SamplePosition(start.position,out var sampled,.8f,UnityEngine.AI.NavMesh.AllAreas)||Mathf.Abs(sampled.position.y-start.position.y)>.5f||!UnityEngine.AI.NavMesh.CalculatePath(sampled.position,property.Gate.position,UnityEngine.AI.NavMesh.AllAreas,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)missing.Add("Property "+property.Index+" disconnected "+start.name);}
   expandedNav.Remove();
   if(missing.Count>0)throw new Exception("Missing content: "+string.Join(", ",missing.Take(15)));
   File.WriteAllText(Path.Combine(Work,"scene-validation.txt"),"All materials, shaders, thirteen sourced audio clips, animated creature, navigation and delivery references present.");
  }
  static void RenderPreviews()
  {
   Directory.CreateDirectory(Path.Combine(Work,"Previews"));
   var cam=scene.View;cam.transform.SetParent(null);
   foreach(int i in new[]{0,3,1,4,5}){var p=scene.Properties[i];cam.transform.position=p.Door.position+p.Door.forward*15+Vector3.up*1.8f;cam.transform.LookAt(p.Door.position+Vector3.up*2);Render(cam,"property-"+i);}
   var villa=scene.Properties[1];cam.transform.position=villa.TableApproach.position+Vector3.up*1.65f;cam.transform.LookAt(villa.DeliveryPoint.position+Vector3.up*.35f);Render(cam,"villa-interior");
   cam.transform.position=scene.Car.position+scene.Car.forward*5+scene.Car.right*3+Vector3.up*1.7f;cam.transform.LookAt(scene.Car.position+Vector3.up*.9f);Render(cam,"car-front");
   var cabin=scene.Properties[0];cam.transform.position=cabin.Door.position+cabin.Door.forward*4+Vector3.up*1.4f;cam.transform.LookAt(cabin.Door.position+Vector3.up);Render(cam,"cabin-porch");
   cam.transform.SetParent(scene.DriverSeat,false);cam.transform.localPosition=Vector3.zero;cam.transform.localRotation=Quaternion.identity;cam.cullingMask=~(1<<8);Render(cam,"driving");cam.cullingMask=~0;
  }
  static void Render(Camera cam,string name)
  {
   var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Work,"Previews/"+name+".png"),tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
  }
 }
}





