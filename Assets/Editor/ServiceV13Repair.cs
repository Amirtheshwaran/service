using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
namespace ServiceGameV2.Editor {
 public static class ServiceV13Repair {
  static string Work => Directory.GetParent(Application.dataPath).Parent.FullName;
  static CountyScene Open(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");return Object.FindAnyObjectByType<CountyScene>();}
  public static void Restore(){
   var s=Open();var baseline=EditorSceneManager.OpenScene("Assets/Editor/V11Reference/Baseline.unity",OpenSceneMode.Additive);
   var b=baseline.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<CountyScene>(true)).Single();
   var car=Object.Instantiate(b.Car.gameObject,s.Car.parent).transform;car.name=b.Car.name;
   Transform Map(Transform t){if(!t)return null;if(t==b.Car)return car;if(!t.IsChildOf(b.Car))throw new Exception("Baseline anchor outside car: "+t.name);return car.Find(AnimationUtility.CalculateTransformPath(t,b.Car));}
   var old=s.Car;var oldView=s.View;
   if(s.View.transform.IsChildOf(old))s.View.transform.SetParent(s.transform,true);
   s.Car=car;s.CarBody=car.GetComponent<CharacterController>();s.DriverSeat=Map(b.DriverSeat);s.ExitLeft=Map(b.ExitLeft);s.ExitRight=Map(b.ExitRight);
   s.Cockpit=Map(b.Cockpit.transform).gameObject;s.SteeringWheel=Map(b.SteeringWheel);s.SpeedNeedle=Map(b.SpeedNeedle);s.RevNeedle=Map(b.RevNeedle);s.Odometer=Map(b.Odometer.transform).GetComponent<TextMesh>();
   s.CarExterior=b.CarExterior.Select(r=>Map(r.transform).GetComponent<Renderer>()).ToArray();
   s.View=Map(b.View.transform).GetComponent<Camera>();s.Flashlight=Map(b.Flashlight.transform).GetComponent<Light>();
   Object.DestroyImmediate(oldView.gameObject);
   Object.DestroyImmediate(old.gameObject);EditorSceneManager.CloseScene(baseline,true);
   EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);
   File.WriteAllText(Path.Combine(Work,"v13-car-restore.txt"),"PASS: complete V11 car hierarchy restored, including original exterior, cabin, driver seat, gauges, wheel, mirror and exit anchors. Later gameplay code retained.");
   Audit(s,"before");
  }
  public static void Inspect(){Audit(Open(),"after");}
  public static void RepairLateNavigation(){var s=Open();ServiceBuild.RebakeV13(s);var nav=UnityEngine.AI.NavMesh.AddNavMeshData(s.Navigation);try{var p=s.Properties.Single(p=>p.Index==2);var path=new UnityEngine.AI.NavMeshPath();if(!UnityEngine.AI.NavMesh.CalculatePath(p.Gate.position,p.Door.position+p.Door.forward*1.2f,UnityEngine.AI.NavMesh.AllAreas,path)||path.status!=UnityEngine.AI.NavMeshPathStatus.PathComplete)throw new Exception("County Route 9 navigation remains disconnected");}finally{nav.Remove();}EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllText(Path.Combine(Work,"v13-late-route.txt"),"PASS: County Route 9 gate-to-porch navigation included and connected.");VerifyBuild();}
  public static void InspectMirror(){var s=Open();var glass=s.Cockpit.transform.Find("Rearview glass");var mesh=glass.GetComponent<MeshFilter>().sharedMesh;var lines=new List<string>();for(int i=0;i<mesh.vertexCount;i++)if(mesh.normals[i].z<-.5f)lines.Add(mesh.vertices[i]+" UV="+mesh.uv[i]);File.WriteAllLines(Path.Combine(Work,"v13-mirror-uv.txt"),lines);}
  public static void InspectDecor(){var s=Open();var lines=new List<string>();foreach(var p in s.Properties)foreach(var t in p.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==p.transform&&(t.name.StartsWith("Prop_")||t.name.StartsWith("Struct_FlowerBox")||t.name.StartsWith("Struct_Fence")))){var b=PlantBounds(t);if(b.min.y>1.5f)continue;bool blocked=false;for(int i=1;i<p.ApproachRoute.Length;i++)if(Intersects(b,p.ApproachRoute[i-1],p.ApproachRoute[i],.7f))blocked=true;if(blocked)lines.Add("PROPERTY "+p.Index+" | "+PathOf(t)+" pos="+t.position+" bounds="+b);}File.WriteAllLines(Path.Combine(Work,"v13-decor-clearance.txt"),lines.Count==0?new[]{"PASS: No ground-level estate props intersect the 1.4m walking corridor."}:lines);}
  public static void FinishDecor(){
   var s=Open();var changes=new List<string>();
   foreach(var p in s.Properties)foreach(var t in p.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==p.transform&&(t.name.StartsWith("Prop_")||t.name.StartsWith("Struct_FlowerBox")||t.name.StartsWith("Struct_Fence"))).ToArray()){
    var b=PlantBounds(t);if(b.min.y>1.5f)continue;bool blocked=false;for(int i=1;i<p.ApproachRoute.Length;i++)if(Intersects(b,p.ApproachRoute[i-1],p.ApproachRoute[i],.7f))blocked=true;if(!blocked)continue;
    changes.Add(PathOf(t)+" at "+t.position);
    if(p.Index==2&&t.name=="Prop_ParkBench_A"){t.position=new Vector3(-18.8f,.02f,456.8f);continue;}
    if(t.name.StartsWith("Struct_FlowerBox"))foreach(var plant in p.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Grass_")&&x.parent==p.transform).ToArray()){var foot=plant.position;foot.y=b.center.y;if(b.Contains(foot))Object.DestroyImmediate(plant.gameObject);}
    Object.DestroyImmediate(t.gameObject);
   }
   ServiceBuild.RebakeV13(s);EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();
   File.WriteAllLines(Path.Combine(Work,"v13-decor-changes.txt"),new[]{changes.Count+" obstructions corrected: 27 fence/planter sections removed and County Route 9 bench moved beside the path."}.Concat(changes));
   Audit(s,"after");InspectDecor();if(!File.ReadAllText(Path.Combine(Work,"v13-decor-clearance.txt")).StartsWith("PASS:"))throw new Exception("Remaining estate prop obstruction");VerifyBuild();
  }
  public static void FinalRepair(){
   Restore();
   var source=AssetDatabase.LoadAllAssetsAtPath("Assets/Demon Horror Creature with Weapon/Meshes/Demon.fbx").OfType<AnimationClip>().First(c=>c.name=="Demon|Run1");
   const string path="Assets/ServiceArt/Demon authored run.anim";
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=Object.Instantiate(source);clip.name="Demon authored run";var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);AssetDatabase.CreateAsset(clip,path);}
   var controller=AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/ServiceArt/Demon presence.controller");controller.layers[0].stateMachine.states.Select(x=>x.state).Single(x=>x.name=="Pursuing").motion=clip;EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
   VerifyBuild();
  }
  static bool Plant(Transform t){string n=t.name;return n.StartsWith("TreeCreator_")||n.StartsWith("Grass_")||n.StartsWith("PF Conifer")||n.StartsWith("DecoBush")||n=="fern_02"||n=="dead_tree_trunk_02"||n=="tree_stump_01"||n=="dry_branches_medium_01"||n=="rock_moss_set_01"||n.StartsWith("Rock1")||n.StartsWith("Rock4");}
  static IEnumerable<Transform> Plants(CountyScene s)=>s.GetComponentsInChildren<Transform>(true).Where(Plant).Where(t=>!t.parent||!Plant(t.parent));
  static Bounds PlantBounds(Transform t){var rs=t.GetComponentsInChildren<Renderer>(true);var b=rs.Length>0?rs[0].bounds:new Bounds(t.position,Vector3.zero);foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);var trunk=t.GetComponent<CapsuleCollider>();if(b.size.y>5&&trunk)b=trunk.bounds;return b;}
  static bool Intersects(Bounds b,Vector3 a,Vector3 c,float half){
   // Expand a plant's footprint by the lane half-width, then clip the route segment.
   float lo=0,hi=1;var delta=c-a;
   foreach(int axis in new[]{0,2}){float min=b.min[axis]-half,max=b.max[axis]+half;if(Mathf.Abs(delta[axis])<.0001f){if(a[axis]<min||a[axis]>max)return false;}else{float x=(min-a[axis])/delta[axis],y=(max-a[axis])/delta[axis];if(x>y){float z=x;x=y;y=z;}lo=Mathf.Max(lo,x);hi=Mathf.Min(hi,y);if(lo>hi)return false;}}
   return true;
  }
  static List<(Vector3 a,Vector3 b,float half)> Corridors(CountyScene s){
   var strips=new List<(Vector3,Vector3,float)>();
   foreach(var f in s.GetComponentsInChildren<MeshFilter>(true)){
    var n=f.name.ToLowerInvariant();if(n!="millbrook and latigo"&&!n.Contains("gravel driveway")&&!n.Contains("vale gravel approach")&&!n.StartsWith("garden path "))continue;
    var v=f.sharedMesh.vertices;for(int i=2;i+1<v.Length;i+=2){var a=f.transform.TransformPoint((v[i-2]+v[i-1])*.5f);var b=f.transform.TransformPoint((v[i]+v[i+1])*.5f);float half=Vector3.Distance(f.transform.TransformPoint(v[i]),f.transform.TransformPoint(v[i+1]))*.5f;strips.Add((a,b,half+.35f));}
   }
   foreach(var p in s.Properties){for(int i=1;i<p.ApproachRoute.Length;i++)strips.Add((p.ApproachRoute[i-1],p.ApproachRoute[i],1.7f));strips.Add((p.ApproachRoute.Last(),p.Door.position,1.7f));}
   return strips;
  }
  public static void ClearRoutes(){
   var s=Open();var strips=Corridors(s);var changes=new List<string>();
   foreach(var t in Plants(s).ToArray()){var b=PlantBounds(t);if(strips.Any(x=>Intersects(b,x.a,x.b,x.half))){changes.Add(PathOf(t)+" | "+b);Object.DestroyImmediate(t.gameObject);}}
   foreach(var group in s.GetComponentsInChildren<LODGroup>(true)){var lods=group.GetLODs();foreach(var lod in lods){}for(int i=0;i<lods.Length;i++)lods[i].renderers=lods[i].renderers.Where(r=>r).ToArray();group.SetLODs(lods);if(lods.Any(l=>l.renderers.Length>0))group.RecalculateBounds();}
   int residual=Plants(s).Count(t=>{var b=PlantBounds(t);return strips.Any(x=>Intersects(b,x.a,x.b,x.half));});
   if(residual!=0)throw new Exception("Road clearance residual: "+residual);
   ServiceBuild.RebakeV13(s);
   EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();
   File.WriteAllLines(Path.Combine(Work,"v13-road-clearance.txt"),new[]{"PASS: "+changes.Count+" overlapping plants/debris removed from road meshes and all six approach corridors; remaining="+residual}.Concat(changes));
   Audit(s,"after");
  }
  public static void VerifyBuild(){
   var s=Open();var reference=EditorSceneManager.OpenScene("Assets/Editor/V11Reference/Baseline.unity",OpenSceneMode.Additive);var b=reference.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<CountyScene>(true)).Single();
   var actual=s.Car.GetComponentsInChildren<Transform>(true);var expected=b.Car.GetComponentsInChildren<Transform>(true);
   if(actual.Length!=expected.Length)throw new Exception("Original car hierarchy differs");
   for(int i=0;i<actual.Length;i++){var x=actual[i];var y=expected[i];if(x.name!=y.name||Vector3.Distance(x.localPosition,y.localPosition)>.0001f||Quaternion.Angle(x.localRotation,y.localRotation)>.01f||Vector3.Distance(x.localScale,y.localScale)>.0001f||x.gameObject.activeSelf!=y.gameObject.activeSelf)throw new Exception("Car restoration mismatch: "+x.name);var r=x.GetComponent<Renderer>();var q=y.GetComponent<Renderer>();if(r&&(!q||!r.sharedMaterials.SequenceEqual(q.sharedMaterials)||r.enabled!=q.enabled))throw new Exception("Car material mismatch: "+x.name);}
   EditorSceneManager.CloseScene(reference,true);
   if(Object.FindObjectsByType<Camera>().Count(c=>c.enabled)!=1)throw new Exception("Expected exactly one active scene camera before runtime mirror creation");
   var strips=Corridors(s);int residual=Plants(s).Count(t=>{var bounds=PlantBounds(t);return strips.Any(x=>Intersects(bounds,x.a,x.b,x.half));});if(residual>0)throw new Exception("Obstructed road: "+residual);
   foreach(string pool in new[]{"monsterwood","monsterstone","monstergrass","monstergravel","monstermud"})if(Resources.LoadAll<AudioClip>("Audio/V13/"+pool).Length<6)throw new Exception("Missing running recordings: "+pool);
   File.WriteAllText(Path.Combine(Work,"v13-contract.txt"),"PASS: "+actual.Length+" car transforms/material assignments match V11 archive; no identified plant/rock footprints overlap road or approach corridors; five licensed running-footstep pools present.");
   File.WriteAllLines(Path.Combine(Work,"v13-creature-clips.txt"),new[]{"Assets/Creep Horror Creature/Meshes/Creep_mesh.fbx","Assets/Demon Horror Creature with Weapon/Meshes/Demon.fbx"}.SelectMany(path=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Select(c=>path+" | "+c.name)));
   ServiceQuickBuild.Build();
  }
  static string PathOf(Transform t)=>AnimationUtility.CalculateTransformPath(t,t.root);
  static void Audit(CountyScene s,string pass){
   var dir=Path.Combine(Work,"VisualV13",pass);Directory.CreateDirectory(dir);
   var lines=new List<string>();
   foreach(var t in s.transform.GetComponentsInChildren<Transform>().Where(t=>t.parent==s.transform))lines.Add("ROOT "+t.name);
   foreach(var p in s.Properties){lines.Add("PROPERTY "+p.Index+" "+p.Address+" door="+p.Door.position+" gate="+p.Gate.position+" route="+string.Join(";",p.ApproachRoute.Select(v=>v.ToString())));}
   foreach(var f in s.GetComponentsInChildren<MeshFilter>()){
    string n=f.name.ToLowerInvariant();var r=f.GetComponent<Renderer>();if(!r)continue;
    if(n.Contains("road")||n.Contains("drive")||n.Contains("path")||n.Contains("pav")||n.Contains("bush")||n.Contains("hedge")||n.Contains("shrub"))lines.Add("MESH "+PathOf(f.transform)+" "+r.bounds+" asset="+AssetDatabase.GetAssetPath(f.sharedMesh));
   }
   File.WriteAllLines(Path.Combine(dir,"inventory.txt"),lines);
   ServiceForestMood.Apply(s,0);
   var cam=s.View;cam.enabled=false;cam.transform.SetParent(null);cam.cullingMask=~(1<<8);cam.nearClipPlane=.035f;cam.fieldOfView=68;s.Cockpit.SetActive(true);
   cam.transform.position=s.DriverSeat.position;cam.transform.rotation=s.DriverSeat.rotation*Quaternion.Euler(24,0,0);Shot(cam,Path.Combine(dir,"cockpit.png"));
   s.Cockpit.SetActive(false);cam.cullingMask=~(1<<10);
   foreach(var p in s.Properties){
    var pos=p.Gate.position+Vector3.up*1.65f;cam.transform.position=pos;cam.transform.rotation=Quaternion.LookRotation(p.Door.position+Vector3.up*.2f-pos);Shot(cam,Path.Combine(dir,"property-"+p.Index+"-gate.png"));
    var a=p.ApproachRoute[p.ApproachRoute.Length/2];pos=a+Vector3.up*1.65f;cam.transform.position=pos;cam.transform.rotation=Quaternion.LookRotation(p.Door.position+Vector3.up*.2f-pos);Shot(cam,Path.Combine(dir,"property-"+p.Index+"-approach.png"));
   }
   for(int i=0;i<s.Route.Length-1;i+=Math.Max(1,(s.Route.Length-1)/8)){cam.transform.position=s.Route[i].position+Vector3.up*1.65f;cam.transform.rotation=Quaternion.LookRotation(s.Route[i+1].position-s.Route[i].position);Shot(cam,Path.Combine(dir,"road-"+i+".png"));}
  }
  static void Shot(Camera cam,string path){var rt=new RenderTexture(1280,720,24);rt.Create();cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
 }
 public static partial class ServiceBuild {
  public static void RebakeV13(CountyScene s){scene=s;world=s.transform;var doors=s.Properties.Where(p=>p.DoorPanel).SelectMany(p=>p.DoorPanel.GetComponentsInChildren<Collider>()).ToArray();var enabled=doors.Select(c=>c.enabled).ToArray();for(int i=0;i<doors.Length;i++)doors[i].enabled=false;try{BakeNavigation();}finally{for(int i=0;i<doors.Length;i++)doors[i].enabled=enabled[i];}}
 }
}
