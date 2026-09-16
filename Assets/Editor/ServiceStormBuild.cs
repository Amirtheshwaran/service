using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 public static partial class ServiceBuild {
  public static void BuildStormPass(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");scene=Object.FindAnyObjectByType<CountyScene>();world=scene.transform;route=Curve(scene.Route.Select(p=>p.position).ToArray());
   foreach(string name in new[]{"Local rainfall","Manor title viewpoint","Storm verge planting"}){var old=world.Find(name);if(old)Object.DestroyImmediate(old.gameObject);}
   var rain=Empty("Local rainfall",Vector3.up*10,world).gameObject.AddComponent<ParticleSystem>();rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=rain.main;main.loop=true;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.95f;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.018f,.03f);main.startColor=new Color(.68f,.74f,.85f,.40f);main.maxParticles=2600;
   var emission=rain.emission;emission.rateOverTime=1800;
   var shape=rain.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(22,.5f,22);
   var velocity=rain.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=-1.2f;velocity.y=-19;velocity.z=.5f;
   var collision=rain.collision;collision.enabled=true;collision.type=ParticleSystemCollisionType.World;collision.mode=ParticleSystemCollisionMode.Collision3D;collision.quality=ParticleSystemCollisionQuality.Medium;collision.collidesWith=~(1<<9);collision.lifetimeLoss=1;collision.enableDynamicColliders=true;
   var renderer=rain.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.velocityScale=.025f;renderer.lengthScale=9;renderer.cameraVelocityScale=0;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"Materials/Rain streaks.mat");if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(material,Art+"Materials/Rain streaks.mat");}
   material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(FG+"Content/Textures/ATM_ExhaustParticle1_AS.tif"));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;renderer.sharedMaterial=material;EditorUtility.SetDirty(material);scene.Rain=rain;
   var manor=scene.Properties[1];var view=Empty("Manor title viewpoint",manor.Door.position+manor.Door.forward*25-manor.Door.right*8+Vector3.up*4.2f,world);
   var target=manor.InteriorBounds.center+Vector3.up*2;view.LookAt(target);view.LookAt(target-view.right*7);
   var plants=Empty("Storm verge planting",Vector3.zero,world);var rng=new System.Random(9012);int count=0;
   string[] kinds={"TreeCreator_Bush_A","TreeCreator_Small_A","TreeCreator_Small_B","Grass_Med_B","Grass_Small_D"};
   foreach(var property in scene.Properties){Vector3 last=Vector3.one*9999;var path=property.ApproachRoute;
    for(int i=2;i<path.Length-2;i++){
     var at=path[i];if(Vector3.Distance(at,last)<3.5f||Vector3.Distance(at,property.Door.position)<9)continue;last=at;var side=Vector3.Cross(Vector3.up,(path[i+1]-path[i-1]).normalized);
     foreach(int sign in new[]{-1,1}){
      var q=at+side*sign*(2.15f+(float)rng.NextDouble()*.7f);if(RoadDistance(q)<5)continue;q.y=LandHeight(q.x,q.z);
      int kind=count%kinds.Length;var plant=Prop(kinds[kind],q,(float)rng.NextDouble()*360,1,plants);ResizePlant(plant,kind==0?.8f+(float)rng.NextDouble()*.5f:kind<3?1.25f+(float)rng.NextDouble()*.7f:.4f+(float)rng.NextDouble()*.4f);
      foreach(var c in plant.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);foreach(var r in plant.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
      var contact=plant.AddComponent<ServiceBrushZone>();contact.Center=q;contact.Radius=1.15f;count++;
     }
    }
   }
   // Tag existing shrubs too, with no physics collider or navigation cost.
   var thickets=world.Find("Forest understory thickets");foreach(var t in thickets.GetComponentsInChildren<Transform>())if(t.name.StartsWith("TreeCreator_Bush_A")&&!t.GetComponent<ServiceBrushZone>()){var zone=t.gameObject.AddComponent<ServiceBrushZone>();var b=BoundsOf(t.gameObject);zone.Center=new Vector3(b.center.x,b.min.y,b.center.z);zone.Radius=Mathf.Clamp(Mathf.Max(b.extents.x,b.extents.z),.65f,1.8f);}
   // Restore any references invalidated by the former index-based material filenames.
   foreach(var kind in new[]{"TreeCreator_Bush_A","TreeCreator_Small_A","TreeCreator_Small_B"}){
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(FG+"Prefabs/Nature/Trees/"+kind+".prefab").GetComponentsInChildren<Renderer>();
    foreach(var plant in world.GetComponentsInChildren<Transform>().Where(t=>t.name==kind)){
     var renderers=plant.GetComponentsInChildren<Renderer>();if(renderers.Length!=source.Length)throw new Exception("Foliage renderer mismatch: "+kind);
     for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=source[i].sharedMaterials.Select(ConvertMat).ToArray();
    }
   }
   var missing=world.GetComponentsInChildren<Renderer>(true).Where(r=>r.sharedMaterials.Any(m=>!m)).Select(r=>r.name).ToArray();
   if(missing.Length>0)throw new Exception("Missing scene materials: "+string.Join(",",missing.Take(12)));
   foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Audio/V9"})){
    var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;settings.loadType=path.Contains("traversal")||path.Contains("chase")||path.Contains("rain")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
   }
   var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");rp.supportsCameraDepthTexture=true;EditorUtility.SetDirty(rp);
   EditorSceneManager.MarkSceneDirty(scene.gameObject.scene);if(!EditorSceneManager.SaveScene(scene.gameObject.scene))throw new Exception("Storm scene save failed");AssetDatabase.SaveAssets();
   Debug.Log("SERVICE_STORM: rain, manor title, "+count+" varied verge plants");ServiceStormContract.Run();ServiceQuickBuild.Build();
  }
 }
}
