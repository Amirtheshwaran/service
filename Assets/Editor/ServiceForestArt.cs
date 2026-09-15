using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 public static partial class ServiceBuild {
  const string Conifers="Assets/Forst/Conifers [BOTD]/Render Pipeline Support/URP/Prefabs/";
  static GameObject Sourced(string path,Vector3 at,float yaw,float height,Transform parent,bool convert=false){
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new Exception("Missing sourced art: "+path);
   var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(o,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   o.transform.SetParent(parent);o.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
   if(height>0){ResizePlant(o,height);var b=BoundsOf(o);o.transform.position+=at-V(b.center.x,b.min.y,b.center.z);}
   foreach(var billboard in o.GetComponentsInChildren<BillboardRenderer>(true))if(billboard.billboard)billboard.sharedMaterial=billboard.billboard.material;
   if(convert)Convert(o);return o;
  }
  static void ForestArt(){
   var old=world.Find("Forest canopy and understory");if(old)Object.DestroyImmediate(old.gameObject);
   var forest=Empty("Conifer forest — canopy, clearings and ground cover",Vector3.zero,world);
   var rng=new System.Random(21476);float R(float lo,float hi)=>(float)(lo+rng.NextDouble()*(hi-lo));
   string[] types={"Tall","Medium","Small","Bare"};
   // Uneven groves with gaps; sightlines and playable routes are excluded before placement.
   for(float z=15;z<434;z+=6)for(float x=-135;x<154;x+=6){
    var at=V(x+R(-2.8f,2.8f),0,z+R(-2.8f,2.8f));
    float grove=Mathf.PerlinNoise((at.x+190)*.018f,at.z*.021f);
    if(!ClearForPlay(at,2)||R(0,1)>(grove>.45f?.82f:.25f))continue;
    at.y=LandHeight(at.x,at.z);string type=types[rng.Next(types.Length)];
    var tree=Sourced(Conifers+"PF Conifer "+type+" BOTD URP.prefab",at,R(0,360),R(12,23),forest);
    foreach(var c in tree.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
    var trunk=tree.AddComponent<CapsuleCollider>();trunk.radius=.3f/tree.transform.lossyScale.x;trunk.height=8/tree.transform.lossyScale.y;trunk.center=tree.transform.InverseTransformPoint(at+Vector3.up*4);
    // Grass is clustered around groves, leaving the path edges legible.
    for(int n=0;n<5;n++){var q=at+V(R(-3,3),0,R(-3,3));q.y=0;if(!ClearForPlay(q,.25f))continue;q.y=LandHeight(q.x,q.z);
     var g=Prop(n%3==0?"Grass_Tall_C":n%3==1?"Grass_Small_C":"Grass_Tall_A",q,R(0,360),R(.22f,.48f),forest);
     ResizePlant(g,R(.22f,.65f));foreach(var c in g.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
     foreach(var r in g.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
    }
   }
   for(int i=0;i<100;i++){
    var at=V(R(-126,145),0,R(26,420));if(!ClearForPlay(at,5))continue;at.y=LandHeight(at.x,at.z)-.12f;
    Sourced("Assets/Rocks and Boulders 2/Rocks/Prefabs/"+new[]{"Rock1B","Rock1C","Rock4A"}[i%3]+".prefab",at,R(0,360),R(.55f,1.6f),forest,true);
   }
   var wind=Sourced("Assets/Forst/CTI Runtime Components/CTI Runtime Components URP 14plus/Prefabs/CTI Windzone URP.prefab",Vector3.zero,35,0,forest);
   var zone=wind.GetComponent<WindZone>();zone.windMain=.28f;zone.windTurbulence=.2f;zone.windPulseMagnitude=.18f;
   ForestUnderstory();
   var terrain=world.GetComponentInChildren<Terrain>();var data=terrain.terrainData;
   RoughGround(data);
   int res=data.alphamapResolution;var alpha=new float[res,res,2];
   for(int z=0;z<res;z++)for(int x=0;x<res;x++){
    var at=terrain.transform.position+V(x*data.size.x/(res-1),0,z*data.size.z/(res-1));
    float noise=Mathf.PerlinNoise((at.x+300)*.09f,at.z*.09f);float dirt=Mathf.Lerp(.18f,.82f,noise);
    dirt=Mathf.Max(dirt,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,7,RoadDistance(at))));
    alpha[z,x,0]=1-dirt;alpha[z,x,1]=dirt;
   }data.SetAlphamaps(0,0,alpha);
   // Preserve authored CTI shaders and their wind/LOD support.
   foreach(var r in forest.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m)m.enableInstancing=true;
  }
  static void RoughGround(TerrainData data){
   var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Forest surface mask.asset");
   if(!mask){mask=new Texture2D(1,1,TextureFormat.RGBA32,false,true);mask.SetPixel(0,0,new Color(0,1,.5f,.035f));mask.Apply();AssetDatabase.CreateAsset(mask,Art+"Forest surface mask.asset");}
   foreach(var layer in data.terrainLayers){string baseName=layer.diffuseTexture.name.Replace("_AS","_N");layer.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(FG+"Content/Textures/"+baseName+".tif");layer.normalScale=.35f;layer.maskMapTexture=mask;layer.maskMapRemapMin=Vector4.zero;layer.maskMapRemapMax=Vector4.one;layer.tileSize=new Vector2(4,4);EditorUtility.SetDirty(layer);}
  }
  static void ForestUnderstory(){
   var old=world.Find("Forest understory thickets");if(old)Object.DestroyImmediate(old.gameObject);
   var holder=Empty("Forest understory thickets",Vector3.zero,world);var rng=new System.Random(7714);
   float R(float lo,float hi)=>(float)(lo+rng.NextDouble()*(hi-lo));int plants=0,clusters=0;
   for(float z=20;z<428;z+=9)for(float x=-130;x<148;x+=9){
    var at=V(x+R(-3,3),0,z+R(-3,3));float road=RoadDistance(at);
    if(!ClearForPlay(at,.5f)||R(0,1)>(road<35?.94f:.72f))continue;
    var cluster=Empty("Mixed woodland thicket",at,holder);
    for(int n=0;n<10;n++){
     var q=at+V(R(-4.5f,4.5f),0,R(-4.5f,4.5f));bool shrub=n<3,sapling=n==9&&clusters%3==0;
     if(n==9&&!sapling)continue;if(!ClearForPlay(q,shrub||sapling?1.2f:.25f))continue;q.y=LandHeight(q.x,q.z);
     GameObject plant;
     if(sapling)plant=Sourced(Conifers+"PF Conifer Small BOTD URP.prefab",q,R(0,360),R(2.8f,4.8f),cluster);
     else {plant=Prop(shrub?"TreeCreator_Bush_A":n%2==0?"Grass_Tall_C":"Grass_Tall_A",q,R(0,360),1,cluster);ResizePlant(plant,shrub?R(1.1f,2.3f):R(.45f,.95f));if(!shrub)plant.transform.localScale=Vector3.Scale(plant.transform.localScale,V(2.2f,1,2.2f));}
     foreach(var c in plant.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
     foreach(var lod in plant.GetComponentsInChildren<LODGroup>()){
      var levels=lod.GetLODs();if(levels.Length>0)foreach(var renderer in levels.Skip(1).SelectMany(l=>l.renderers).Distinct().Where(r=>r&&!levels[0].renderers.Contains(r)))Object.DestroyImmediate(renderer);
      Object.DestroyImmediate(lod);
     }
     foreach(var r in plant.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;foreach(var m in r.sharedMaterials)if(m)m.enableInstancing=true;}
     plants++;
    }
    var renderers=cluster.GetComponentsInChildren<Renderer>().Where(r=>!(r is BillboardRenderer)).ToArray();
    // Cull complete patches beyond the nearby forest; retain shared, instanced materials.
    var group=cluster.gameObject.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.075f,renderers)});group.RecalculateBounds();clusters++;
   }
   foreach(var property in scene.Properties){
    var fringe=Empty("Low woodland along "+property.name,Vector3.zero,holder);Vector3 last=Vector3.one*9999;var path=property.ApproachRoute;
    for(int i=1;i<path.Length-1;i++){
     var at=path[i];if(Vector3.Distance(at,last)<2.7f||Vector3.Distance(at,property.Door.position)<10)continue;last=at;
     var side=Vector3.Cross(Vector3.up,(path[i+1]-path[i-1]).normalized);
     foreach(int sign in new[]{-1,1})for(int n=0;n<4;n++){
      var q=at+side*sign*(3.5f+n*1.4f+R(-.4f,.4f));if(RoadDistance(q)<5)continue;
      var bounds=property.InteriorBounds;bounds.Expand(3);if(q.x>bounds.min.x&&q.x<bounds.max.x&&q.z>bounds.min.z&&q.z<bounds.max.z)continue;
      q.y=LandHeight(q.x,q.z);bool bush=n%2==0;var plant=Prop(bush?"TreeCreator_Bush_A":"Grass_Tall_C",q,R(0,360),1,fringe);ResizePlant(plant,bush?R(.45f,.85f):R(.5f,.8f));plant.transform.localScale=Vector3.Scale(plant.transform.localScale,V(bush?1.4f:2.2f,1,bush?1.4f:2.2f));
      foreach(var c in plant.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);foreach(var r in plant.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;plants++;
     }
    }
    var lod=fringe.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.06f,fringe.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
   }
   Debug.Log("SERVICE_FOREST: "+plants+" undergrowth plants in "+clusters+" patches plus six path fringes");
  }
  public static void FinishForestBuild(){
   UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");
   scene=Object.FindFirstObjectByType<CountyScene>();world=scene.transform;route=Curve(scene.Route.Select(p=>p.position).ToArray());ForestUnderstory();
   RoughGround(Object.FindFirstObjectByType<Terrain>().terrainData);
   UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene.gameObject.scene);
   if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene.gameObject.scene))throw new Exception("Forest scene could not be saved");
   AssetDatabase.SaveAssets();ServiceQuickBuild.Build();
  }
  static void DemonVariant(){
   var root=scene.Entity;var model=Sourced("Assets/Demon Horror Creature with Weapon/Prefabs/Demon_default.prefab",root.transform.position,0,2.1f,root.transform,true);
   var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Demon Horror Creature with Weapon/Meshes/Demon.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
   AnimationClip Choose(string term){var clip=clips.FirstOrDefault(c=>c.name.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0);if(!clip)throw new Exception("Demon needs "+term+" clip. Available: "+string.Join(",",clips.Select(c=>c.name)));var copy=Object.Instantiate(clip);var settings=AnimationUtility.GetAnimationClipSettings(copy);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(copy,settings);AssetDatabase.CreateAsset(copy,Art+"Demon "+term+".anim");return copy;}
   var ctrl=UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(Art+"Demon presence.controller");ctrl.AddParameter("Moving",AnimatorControllerParameterType.Bool);var sm=ctrl.layers[0].stateMachine;
   var idle=sm.AddState("Watching");idle.motion=Choose("idle");var walk=sm.AddState("Pursuing");walk.motion=Choose("walk");sm.defaultState=idle;
   var a=idle.AddTransition(walk);a.hasExitTime=false;a.duration=.2f;a.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If,0,"Moving");var b=walk.AddTransition(idle);b.hasExitTime=false;b.duration=.2f;b.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot,0,"Moving");
   foreach(var animator in model.GetComponentsInChildren<Animator>(true)){animator.runtimeAnimatorController=ctrl;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
   foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=9;foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
   model.SetActive(false);scene.EntityVariants=scene.EntityVariants.Concat(new[]{model}).ToArray();scene.Properties[3].CreatureVariant=3;scene.Properties[5].CreatureVariant=3;
  }
 }
}
