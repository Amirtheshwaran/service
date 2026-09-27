using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 public static class ServiceV15Repair {
  static string Work=>Directory.GetParent(Application.dataPath).Parent.FullName;
  static CountyScene Open(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");return Object.FindAnyObjectByType<CountyScene>();}
  static string PathOf(Transform t)=>AnimationUtility.CalculateTransformPath(t,t.root);
  public static void Inspect(){
   var s=Open();var lines=new List<string>();
   foreach(Transform t in s.transform)lines.Add("ROOT "+t.name+" "+t.position+" children="+t.childCount);
   foreach(var p in s.Properties){lines.Add("PROPERTY "+p.Index+" "+p.name+" bounds="+p.InteriorBounds+" gate="+p.Gate.position+" door="+p.Door.position+" table="+p.TableApproach.position);foreach(var f in p.GetComponentsInChildren<MeshFilter>(true))if(f.name.ToLower().Contains("door")||f.name.ToLower().Contains("stair"))lines.Add("PART "+p.Index+" "+PathOf(f.transform)+" "+f.GetComponent<Renderer>()?.bounds);}
   foreach(var f in s.Car.GetComponentsInChildren<MeshFilter>(true))lines.Add("CAR "+PathOf(f.transform)+" "+f.GetComponent<Renderer>()?.bounds+" "+AssetDatabase.GetAssetPath(f.sharedMesh));
   foreach(var needle in new[]{s.SpeedNeedle,s.RevNeedle}){lines.Add("NEEDLE "+PathOf(needle)+" pos="+needle.position+" rotation="+needle.rotation);foreach(var f in needle.GetComponentsInChildren<MeshFilter>())foreach(var v in f.sharedMesh.vertices.Distinct())lines.Add("VERTEX "+f.name+" "+s.Cockpit.transform.InverseTransformPoint(f.transform.TransformPoint(v)).ToString("F5"));}
   foreach(var t in s.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("TreeCreator_")||t.name.StartsWith("PF Conifer")).Take(20)){var rs=t.GetComponentsInChildren<Renderer>();lines.Add("TREE "+PathOf(t)+" at="+t.position+" scale="+t.lossyScale+" min="+(rs.Length>0?rs.Min(r=>r.bounds.min.y):0)+" colliders="+t.GetComponentsInChildren<Collider>().Length+" layer="+t.gameObject.layer);}
   File.WriteAllLines(Path.Combine(Work,"v15-inspection.txt"),lines);
  }
  public static void AssetInspect(){
   Inspect();var s=Open();var lines=new List<string>();
   foreach(var name in new[]{"road-straight","road-bend","road-curve","road-intersection","construction-barrier"}){var o=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External/KenneyRoads/"+name+".fbx"));foreach(var r in o.GetComponentsInChildren<Renderer>())lines.Add("ROAD "+name+" "+r.bounds);Object.DestroyImmediate(o);}
   var terrain=s.GetComponentInChildren<Terrain>();foreach(float z in new[]{0,40,80,120,160,200,240,280,320,360,390})lines.Add("HEIGHT "+z+" "+terrain.SampleHeight(new Vector3(-32,0,z))+" terrain="+terrain.transform.position.y);
   foreach(var clip in AssetDatabase.LoadAllAssetsAtPath("Assets/External/QuaterniusDog/Dog.fbx").OfType<AnimationClip>())lines.Add("DOG CLIP "+clip.name+" "+clip.length);
   var dog=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External/QuaterniusDog/Dog.fbx"));foreach(var r in dog.GetComponentsInChildren<Renderer>())lines.Add("DOG "+r.bounds+" materials="+string.Join(",",r.sharedMaterials.Select(m=>m.name)));Object.DestroyImmediate(dog);
   File.WriteAllLines(Path.Combine(Work,"v15-assets-inspection.txt"),lines);
  }
  static Bounds BoundsOf(Transform t){var rs=t.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
  static bool Plant(Transform t){var n=t.name;return n.StartsWith("TreeCreator_")||n.StartsWith("PF Conifer")||n.StartsWith("Grass_")||n.StartsWith("DecoBush")||n=="fern_02"||n=="dead_tree_trunk_02"||n=="tree_stump_01"||n=="dry_branches_medium_01"||n=="rock_moss_set_01"||n.StartsWith("Rock1")||n.StartsWith("Rock4");}
  public static void ApplyCore(){
   var s=Open();var changes=new List<string>();var terrain=s.GetComponentInChildren<Terrain>();
   // Correct existing author-placed instances; never seed or generate scenery.
   foreach(var plant in s.GetComponentsInChildren<Transform>(true).Where(Plant).Where(t=>!Plant(t.parent)).ToArray()){
    if(plant.GetComponentsInChildren<Renderer>(true).Length==0)continue;var b=BoundsOf(plant);
    if(s.Properties.Any(p=>{var floor=p.InteriorBounds;floor.Expand(new Vector3(1,0,1));return floor.Intersects(b);})) {changes.Add("Interior plant removed "+PathOf(plant));Object.DestroyImmediate(plant.gameObject);continue;}
    bool tree=plant.name.StartsWith("TreeCreator_")||plant.name.StartsWith("PF Conifer");
    if(!tree)continue;
    float ground=terrain.SampleHeight(plant.position)+terrain.transform.position.y;
    // Bury the existing root flare slightly; align every trunk collider to the visible tree.
    float delta=ground-.12f-b.min.y;if(Mathf.Abs(delta)>.04f){plant.position+=Vector3.up*delta;changes.Add("Root seated "+PathOf(plant)+" delta="+delta);}
    var c=plant.GetComponent<CapsuleCollider>();if(!c)c=plant.gameObject.AddComponent<CapsuleCollider>();c.enabled=true;c.isTrigger=false;c.direction=1;c.radius=.42f/Mathf.Max(.01f,Mathf.Abs(plant.lossyScale.x));c.height=6/Mathf.Abs(plant.lossyScale.y);c.center=plant.InverseTransformPoint(new Vector3(plant.position.x,ground+3,plant.position.z));
   }
   foreach(var p in s.Properties)foreach(var r in p.GetComponentsInChildren<MeshRenderer>())if(r.name.ToLowerInvariant().Contains("stair")){var zone=r.GetComponent<ServiceStairZone>();if(!zone)zone=r.gameObject.AddComponent<ServiceStairZone>();zone.Area=r.bounds;var area=zone.Area;area.Expand(new Vector3(.5f,1.4f,.5f));zone.Area=area;changes.Add("Stair jump zone "+PathOf(r.transform));}
   Calibrate(s,s.SpeedNeedle,true);Calibrate(s,s.RevNeedle,false);
   // Source shell polygons need both sides rendered from inside the cabin.
   foreach(var m in s.Cockpit.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct())if(m&&m.HasProperty("_Cull")&&m.shader.name.Contains("Lit")){m.SetFloat("_Cull",0);m.doubleSidedGI=true;EditorUtility.SetDirty(m);}
   // The taxi source carries an opaque rear glazing insert. Keep its authored frame and seats.
   var rear=s.Cockpit.transform.Find("Plane.018");if(rear){rear.gameObject.SetActive(false);changes.Add("Opaque rear glazing insert disabled");}
   var volume=s.GetComponentInChildren<Volume>();if(volume){var profile=volume.sharedProfile;T Get<T>()where T:VolumeComponent{if(!profile.TryGet<T>(out var c))c=profile.Add<T>(true);return c;}
    var grade=Get<ColorAdjustments>();grade.postExposure.Override(-.1f);grade.contrast.Override(17);grade.saturation.Override(-22);grade.colorFilter.Override(new Color(.91f,.95f,1));
    var grain=Get<FilmGrain>();grain.type.Override(FilmGrainLookup.Medium1);grain.intensity.Override(.24f);grain.response.Override(.72f);
    var ca=Get<ChromaticAberration>();ca.intensity.Override(.065f);var v=Get<Vignette>();v.intensity.Override(.24f);v.smoothness.Override(.55f);EditorUtility.SetDirty(profile);
   }
   foreach(var lod in s.GetComponentsInChildren<LODGroup>(true)){var levels=lod.GetLODs();for(int i=0;i<levels.Length;i++)levels[i].renderers=levels[i].renderers.Where(r=>r).ToArray();lod.SetLODs(levels);}
   EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(Work,"v15-core-scene-changes.txt"),changes);AssetInspect();
  }
  static void Calibrate(CountyScene s,Transform pivot,bool mph){
   var mesh=pivot.GetComponentInChildren<MeshFilter>();var vertices=mesh.sharedMesh.vertices.Distinct().ToArray();
   // First 32 vertices form the authored circular needle hub; the old pivot was below it.
   var hub=vertices.Take(32).Aggregate(Vector3.zero,(a,b)=>a+mesh.transform.TransformPoint(b))/32;
   var tip=vertices.Select(v=>mesh.transform.TransformPoint(v)).OrderByDescending(v=>v.y).First();var up=(tip-hub).normalized;
   var plane=pivot.parent;mesh.transform.SetParent(s.Cockpit.transform,true);plane.position=hub;plane.rotation=Quaternion.LookRotation(Vector3.Cross(plane.right,up),up);pivot.SetParent(plane,false);pivot.localPosition=Vector3.zero;pivot.localRotation=Quaternion.identity;mesh.transform.SetParent(pivot,true);
   var gauge=pivot.GetComponent<ServiceGauge>();if(!gauge)gauge=pivot.gameObject.AddComponent<ServiceGauge>();gauge.Rest=Quaternion.identity;
   foreach(var label in s.Cockpit.GetComponentsInChildren<TextMesh>())if(float.TryParse(label.text,out var number)&&(label.transform.localPosition.x<-.49f)!=mph){float a=Mathf.Lerp(130,-130,number/(mph?60:6))*Mathf.Deg2Rad;label.transform.position=hub+plane.TransformDirection(new Vector3(-Mathf.Sin(a),Mathf.Cos(a),0))*(mph?.040f:.022f)-plane.forward*.002f;label.transform.rotation=plane.rotation;}
  }

  static GameObject RoadAsset(CountyScene s,string name,Vector3 pos,Vector3 scale,float yaw,Transform parent,Material material){var o=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External/KenneyRoads/"+name+".fbx"),parent);o.name=name;o.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));o.transform.localScale=scale;foreach(var f in o.GetComponentsInChildren<MeshFilter>()){var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;f.gameObject.AddComponent<ServiceSurface>().Kind="stone";if(material)f.GetComponent<Renderer>().sharedMaterials=f.GetComponent<Renderer>().sharedMaterials.Select(m=>material).ToArray();}return o;}
  public static void Finish(){
   var s=Open();var root=s.transform;var log=new List<string>();
   var prior=root.Find("Depot return lane");if(prior)Object.DestroyImmediate(prior.gameObject);var loop=new GameObject("Depot return lane").transform;loop.SetParent(root,false);
   var asphalt=root.Find("Millbrook and Latigo").GetComponent<Renderer>().sharedMaterial;
   // Fixed, authored road layout. Only instantiate source road meshes, never construct a ribbon.
   var points=new[]{new Vector3(-4,0,385),new Vector3(-32,0,385),new Vector3(-32,0,-18),new Vector3(0,0,-18),new Vector3(0,0,0)};
   for(int i=1;i<points.Length;i++){var a=points[i-1];var b=points[i];var delta=b-a;RoadAsset(s,"road-straight",(a+b)*.5f+Vector3.up*.035f,new Vector3(7,1,delta.magnitude),Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,loop,asphalt);}
   foreach(var point in points.Take(points.Length-1))RoadAsset(s,"road-square",point+Vector3.up*.037f,new Vector3(9,1,9),0,loop,asphalt);
   var route=loop.gameObject.AddComponent<ServiceReturnRoad>();route.Points=points;
   var terrain=s.GetComponentInChildren<Terrain>();var data=terrain.terrainData;int res=data.heightmapResolution;var heights=data.GetHeights(0,0,res,res);
   float Distance(Vector3 point,Vector3 a,Vector3 b){var ab=b-a;float u=Mathf.Clamp01(Vector3.Dot(point-a,ab)/ab.sqrMagnitude);return Vector3.Distance(point,a+ab*u);}
   for(int z=0;z<res;z++)for(int x=0;x<res;x++){var point=terrain.transform.position+new Vector3(x*data.size.x/(res-1),0,z*data.size.z/(res-1));point.y=0;float distance=Enumerable.Range(1,points.Length-1).Min(i=>Distance(point,points[i-1],points[i]));if(distance>8)continue;float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.8f,8,distance));heights[z,x]=Mathf.Lerp(heights[z,x],-terrain.transform.position.y/data.size.y,blend);}data.SetHeights(0,0,heights);EditorUtility.SetDirty(data);
   foreach(var plant in s.GetComponentsInChildren<Transform>(true).Where(Plant).Where(t=>!Plant(t.parent)).ToArray()){if(!plant.GetComponentsInChildren<Renderer>(true).Any())continue;var b=BoundsOf(plant);var p=plant.position;p.y=0;float distance=Enumerable.Range(1,points.Length-1).Min(i=>Distance(p,points[i-1],points[i]));if(distance<7+Mathf.Min(1.5f,b.extents.x)){Object.DestroyImmediate(plant.gameObject);log.Add("Return road cleared "+p);}}
   // Close the unstaffed Bell entrances with the building's own door mesh.
   var bell=s.Properties.Single(p=>p.Index==4);
   foreach(var target in new[]{new Vector3(-72.01f,2.06f,291.15f),new Vector3(-79.99f,2.06f,288.26f)}){
    string name="Locked secondary entrance "+target.z;var old=bell.transform.Find(name);if(old)Object.DestroyImmediate(old.gameObject);
    var o=Object.Instantiate(bell.DoorPanel.gameObject,bell.transform);o.name=name;if(target.x< -76)o.transform.Rotate(0,180,0,Space.World);var bounds=BoundsOf(o.transform);o.transform.position+=target-bounds.center;
    foreach(var f in o.GetComponentsInChildren<MeshFilter>())if(!f.GetComponent<Collider>()){var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;}
   }
   // Authored roadwork barriers create a visible stagger, leaving a 1.8m escape lane.
   var hurdle=root.Find("Return path roadworks");if(hurdle)Object.DestroyImmediate(hurdle.gameObject);hurdle=new GameObject("Return path roadworks").transform;hurdle.SetParent(root,false);
   foreach(int index in new[]{1,4,5}){var p=s.Properties.Single(p=>p.Index==index);var dir=(p.Gate.position-p.Door.position).normalized;dir.y=0;var side=Vector3.Cross(Vector3.up,dir);foreach(int step in new[]{0,1}){var point=p.Gate.position-dir*(9+step*7)+side*(step==0?1.65f:-1.65f);point.y=.02f;var o=RoadAsset(s,"construction-barrier",point,new Vector3(7,7,7),Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg, hurdle,null);foreach(var r in o.GetComponentsInChildren<Renderer>()){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=new Color(.34f,.23f,.085f);string path="Assets/External/KenneyRoads/Weathered barrier.mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);if(!saved){AssetDatabase.CreateAsset(m,path);saved=m;}else Object.DestroyImmediate(m);r.sharedMaterial=saved;}}}
   const string dogPath="Assets/External/QuaterniusDog/Dog.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(dogPath);importer.animationType=ModelImporterAnimationType.Legacy;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
   var dog=root.Find("Correll yard dog");foreach(Transform child in dog.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
   var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(dogPath),dog);model.name="Quaternius domestic dog";model.transform.localRotation=Quaternion.Euler(0,90,0);model.transform.localPosition=Vector3.zero;var db=BoundsOf(model.transform);model.transform.localScale*=.68f/db.size.y;db=BoundsOf(model.transform);model.transform.position+=new Vector3(dog.position.x-db.center.x,dog.position.y-db.min.y,dog.position.z-db.center.z);
   foreach(var r in model.GetComponentsInChildren<Renderer>()){var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(.16f,.115f,.08f);var path="Assets/External/QuaterniusDog/Chocolate coat.mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);if(!saved){AssetDatabase.CreateAsset(mat,path);saved=mat;}else Object.DestroyImmediate(mat);r.sharedMaterial=saved;}
   var animation=model.GetComponent<Animation>();if(animation){animation.playAutomatically=true;foreach(AnimationState state in animation)state.wrapMode=WrapMode.Loop;}
   foreach(var lod in s.GetComponentsInChildren<LODGroup>(true)){var levels=lod.GetLODs();for(int i=0;i<levels.Length;i++)levels[i].renderers=levels[i].renderers.Where(r=>r).ToArray();lod.SetLODs(levels);}
   ServiceBuild.RebakeV13(s);EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(Work,"v15-finish-scene.txt"),log);Preview();ServiceQuickBuild.Build();
  }
  public static void Preview(){
   var s=Open();var cam=s.View;cam.transform.SetParent(s.DriverSeat,false);cam.transform.localPosition=Vector3.zero;cam.nearClipPlane=.025f;cam.fieldOfView=68;cam.cullingMask&=~(1<<8);s.Cockpit.SetActive(true);var dir=Path.Combine(Work,"PreviewV15");Directory.CreateDirectory(dir);
   foreach(var pose in new[]{new Vector3(24,0,0),new Vector3(12,85,0),new Vector3(12,-85,0),new Vector3(5,175,0)}){cam.transform.localRotation=Quaternion.Euler(pose);Capture(cam,Path.Combine(dir,"cabin-"+pose.y+".png"));}
   foreach(var ratio in new[]{0f,.25f,.5f,.75f,1f}){ServiceGauge.Set(s.SpeedNeedle,ratio);cam.transform.localRotation=Quaternion.Euler(24,0,0);Capture(cam,Path.Combine(dir,"needle-"+ratio+".png"));}
  }
  static void Capture(Camera cam,string path){var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=prev;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}

  public static void Review(){
   var s=Open();s.DriverSeat.localPosition=new Vector3(s.DriverSeat.localPosition.x,1.41f,s.DriverSeat.localPosition.z);var log=new List<string>();
   foreach(var box in s.GetComponentsInChildren<BoxCollider>(true)){var scale=box.transform.lossyScale;if(scale.x>=0&&scale.y>=0&&scale.z>=0)continue;var f=box.GetComponent<MeshFilter>();if(!f)continue;var mesh=box.GetComponent<MeshCollider>();if(!mesh)mesh=box.gameObject.AddComponent<MeshCollider>();mesh.sharedMesh=f.sharedMesh;mesh.enabled=box.enabled;log.Add("Mirrored collision corrected "+PathOf(box.transform));Object.DestroyImmediate(box);}
   foreach(var bush in s.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("TreeCreator_Bush"))){var c=bush.GetComponent<CapsuleCollider>();if(c)Object.DestroyImmediate(c);}
   var rear=s.Cockpit.transform.Find("Plane.018");if(rear)rear.gameObject.SetActive(true); // source trunk lid, not a glazing cover
   var dog=s.transform.Find("Correll yard dog");foreach(var child in dog.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
   var fit=new GameObject("Dog visual fit").transform;fit.SetParent(dog,false);
   var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External/QuaterniusDog/Dog.fbx"),fit);source.name="Authored domestic dog";
   var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/External/QuaterniusDog/Dog.fbx").OfType<AnimationClip>().First(c=>c.name.Contains("Idle")&&!c.name.StartsWith("__"));clip.SampleAnimation(source,0);
   Bounds Posed(){bool first=true;var b=new Bounds();foreach(var r in source.GetComponentsInChildren<SkinnedMeshRenderer>()){var m=new Mesh();r.BakeMesh(m);foreach(var vertex in m.vertices){var v=r.transform.TransformPoint(vertex);if(first){b=new Bounds(v,Vector3.zero);first=false;}else b.Encapsulate(v);}Object.DestroyImmediate(m);}return b;}
   var before=Posed();fit.localRotation=Quaternion.Euler(0,90,0);fit.localScale=Vector3.one;fit.localPosition=Vector3.zero;
   // The FBX now carries meter units, with animation roots kept at unit scale.
   foreach(var tr in source.GetComponentsInChildren<Transform>())log.Add("DOG TRANSFORM "+tr.name+" pos="+tr.localPosition+" scale="+tr.localScale);
   foreach(var binding in AnimationUtility.GetCurveBindings(clip).Where(b=>b.path==""))log.Add("ROOT CURVE "+binding.propertyName+"="+AnimationUtility.GetEditorCurve(clip,binding).Evaluate(0));
   foreach(var r in source.GetComponentsInChildren<SkinnedMeshRenderer>()){r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/External/QuaterniusDog/Chocolate coat.mat");r.updateWhenOffscreen=true;}
   var anim=source.GetComponent<Animation>();if(anim){anim.clip=clip;anim.playAutomatically=true;foreach(AnimationState state in anim)state.wrapMode=WrapMode.Loop;}
   log.Add("DOG rest="+before+" fitted="+Posed()+" fit="+fit.position+" root="+dog.position);
   var road=s.GetComponentInChildren<ServiceReturnRoad>();var old=road.transform.Find("North survey closure");if(old)Object.DestroyImmediate(old.gameObject);var closure=new GameObject("North survey closure").transform;closure.SetParent(road.transform,false);road.NorthClosure=closure.gameObject;
   var barrierMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/External/KenneyRoads/Weathered barrier.mat");foreach(float x in new[]{-6.2f,-4f,-1.8f})RoadAsset(s,"construction-barrier",new Vector3(x,.02f,392),new Vector3(10,10,10),90,closure,barrierMat);
   foreach(var oldSign in road.transform.Cast<Transform>().Where(t=>t.name=="road-sign-empty"||t.name=="sign-highway"||t.name=="Depot direction lettering").ToArray())Object.DestroyImmediate(oldSign.gameObject);
   var sign=RoadAsset(s,"sign-highway",new Vector3(2f,.02f,382),new Vector3(3,3,3),0,road.transform,barrierMat);var signBounds=BoundsOf(sign.transform);log.Add("SIGN "+signBounds);
   var words=new GameObject("Depot direction lettering");words.transform.SetParent(road.transform,false);words.transform.position=new Vector3(signBounds.center.x,signBounds.max.y-.25f,signBounds.min.z-.015f);var text=words.AddComponent<TextMesh>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Barlow-Regular.ttf");text.fontSize=64;text.characterSize=.025f;text.text="DEPOT\nRETURN LANE  <";text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.8f,.76f,.61f);text.GetComponent<Renderer>().sharedMaterial=text.font.material;
   ServiceBuild.RebakeV13(s);EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(Work,"v15-review.txt"),log);ServiceQuickBuild.Build();
  }

 }
}
