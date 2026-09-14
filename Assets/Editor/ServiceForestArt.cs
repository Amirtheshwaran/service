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
   var terrain=world.GetComponentInChildren<Terrain>();var data=terrain.terrainData;
   foreach(var layer in data.terrainLayers){string baseName=layer.diffuseTexture.name.Replace("_AS","_N");layer.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(FG+"Content/Textures/"+baseName+".tif");layer.normalScale=.85f;layer.tileSize=new Vector2(4,4);EditorUtility.SetDirty(layer);}
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
