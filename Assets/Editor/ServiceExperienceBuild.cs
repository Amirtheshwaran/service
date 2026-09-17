using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 public static partial class ServiceBuild {
  public static void InspectExperience(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");scene=Object.FindAnyObjectByType<CountyScene>();world=scene.transform;
   var lines=new List<string>();
   foreach(var p in scene.Properties){lines.Add("PROPERTY "+p.Index+" door "+p.Door.position+" outward "+p.Door.forward);foreach(var r in p.Building.GetComponentsInChildren<Renderer>().Where(r=>r.name.ToLower().Contains("door")).OrderBy(r=>Vector3.Distance(r.bounds.center,p.Door.position)).Take(4))lines.Add("DOOR "+r.name+" "+r.bounds+" distance="+Vector3.Distance(r.bounds.center,p.Door.position));}
   foreach(var r in world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.ToLower().Contains("road")||r.sharedMaterials.Any(m=>m&&(m.name.ToLower().Contains("pav")||m.name.ToLower().Contains("asphalt")))))lines.Add("PAVED "+r.name+" "+r.bounds);
   string path="Assets/External/AnimatedCanine/Canine.fbx";foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())lines.Add("CANINE CLIP "+a.name+" "+a.length);
   File.WriteAllLines(Path.Combine(Work,"experience-inventory.txt"),lines);ServiceExperienceContract.Run();
  }
  static bool LoosePlant(Transform t){string n=t.name;return n.StartsWith("TreeCreator_")||n.StartsWith("Grass_")||n.StartsWith("PF Conifer")||n=="fern_02"||n=="dead_tree_trunk_02"||n=="tree_stump_01"||n=="dry_branches_medium_01"||n=="rock_moss_set_01"||n.StartsWith("Rock1")||n.StartsWith("Rock4");}
  static List<(Vector3 a,Vector3 b,float half)> PavedStrips(){
   var strips=new List<(Vector3,Vector3,float)>();foreach(var f in world.GetComponentsInChildren<MeshFilter>()){
    string n=f.name.ToLowerInvariant();if(n!="millbrook and latigo"&&!n.Contains("gravel driveway")&&!n.Contains("vale gravel approach")&&!n.StartsWith("garden path "))continue;
    var v=f.sharedMesh.vertices;for(int i=2;i+1<v.Length;i+=2){Vector3 a=f.transform.TransformPoint((v[i-2]+v[i-1])*.5f),b=f.transform.TransformPoint((v[i]+v[i+1])*.5f);float half=Vector3.Distance(f.transform.TransformPoint(v[i]),f.transform.TransformPoint(v[i+1]))*.5f;strips.Add((a,b,half));}
   }return strips;
  }
  static bool InRoad(Transform t,List<(Vector3 a,Vector3 b,float half)> strips){
   var bounds=BoundsOf(t.gameObject);if(bounds.size==Vector3.zero)return false;var at=bounds.center;float radius=Mathf.Max(bounds.extents.x,bounds.extents.z);var trunk=t.GetComponent<CapsuleCollider>();if(trunk){at=t.TransformPoint(trunk.center);radius=.5f;}
   at.y=0;foreach(var s in strips){var a=s.a;a.y=0;var b=s.b;b.y=0;var delta=b-a;float p=Mathf.Clamp01(Vector3.Dot(at-a,delta)/Mathf.Max(delta.sqrMagnitude,.001f));if(Vector3.Distance(at,a+delta*p)<s.half+radius+.12f)return true;}
   return false;
  }
  public static void BuildExperience(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");scene=Object.FindAnyObjectByType<CountyScene>();world=scene.transform;route=Curve(scene.Route.Select(p=>p.position).ToArray());var strips=PavedStrips();int removed=0;
   var candidates=world.GetComponentsInChildren<Transform>().Where(LoosePlant).Where(t=>!t.parent||!LoosePlant(t.parent)).ToArray();
   Physics.SyncTransforms();foreach(var t in candidates){if(!t)continue;bool road=InRoad(t,strips);var bounds=BoundsOf(t.gameObject);
    if(!road&&bounds.size.y<5){var feet=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);foreach(var hit in Physics.RaycastAll(feet+Vector3.up*.4f,Vector3.down,.9f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))if(hit.normal.y>.75f&&scene.Properties.Any(p=>hit.transform.IsChildOf(p.Building))){road=true;break;}}
    if(road){Object.DestroyImmediate(t.gameObject);removed++;}
   }
   foreach(var group in world.GetComponentsInChildren<LODGroup>()){var lods=group.GetLODs();for(int i=0;i<lods.Length;i++)lods[i].renderers=lods[i].renderers.Where(r=>r).ToArray();group.SetLODs(lods);if(lods.Any(l=>l.renderers.Length>0))group.RecalculateBounds();}
   int residual=world.GetComponentsInChildren<Transform>().Where(LoosePlant).Where(t=>!t.parent||!LoosePlant(t.parent)).Count(t=>InRoad(t,strips));if(residual>0)throw new Exception("Vegetation still intersects roads: "+residual);
   // Refinish the existing cabin; no invented vehicle or replacement generated mesh.
   var leather=ScanMaterial("leather_white");leather.SetColor("_BaseColor",new Color(.19f,.205f,.21f));leather.SetTextureScale("_BaseMap",Vector2.one*4);EditorUtility.SetDirty(leather);
   var cockpit=scene.Cockpit.transform;
   foreach(var r in cockpit.GetComponentsInChildren<Renderer>()){if(r is MeshRenderer&&r.GetComponent<TextMesh>())continue;if(r.sharedMaterial&&r.sharedMaterial.name=="Cabin vinyl")r.sharedMaterial=leather;}
   var dashboard=cockpit.Find("Shaped dashboard").GetComponent<MeshFilter>().sharedMesh;var vertices=dashboard.vertices;var normals=dashboard.normals;var uv=new Vector2[vertices.Length];for(int i=0;i<uv.Length;i++)uv[i]=Mathf.Abs(normals[i].y)>.5f?new Vector2(vertices[i].x,vertices[i].z):new Vector2(vertices[i].x,vertices[i].y);dashboard.uv=uv;EditorUtility.SetDirty(dashboard);
   scene.SteeringWheel.localPosition=new Vector3(-.34f,.73f,.08f);scene.DriverSeat.localPosition=new Vector3(-.34f,1.34f,-.35f);
   var column=cockpit.Find("Steering column");if(column)column.localPosition=new Vector3(-.34f,.69f,.34f);
   foreach(var text in cockpit.GetComponentsInChildren<TextMesh>()){if(text.text.Contains("COUNTY FLEET"))text.gameObject.SetActive(false);if(text.text.Contains("88.5")||text.text.Contains("RADIO OFF")){text.text="RADIO OFF";text.characterSize=.0025f;}text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);}
   var spill=cockpit.Find("Instrument spill").GetComponent<Light>();spill.intensity=1.15f;spill.color=new Color(.64f,.71f,.74f);
   string path="Assets/External/AnimatedCanine/Canine.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=ModelImporterAnimationType.Legacy;importer.importAnimation=true;var clips=importer.defaultClipAnimations;foreach(var c in clips){c.loopTime=true;c.wrapMode=WrapMode.Loop;}importer.clipAnimations=clips;importer.SaveAndReimport();
   var old=world.Find("Correll yard dog");if(old)Object.DestroyImmediate(old.gameObject);var p0=scene.Properties[0];p0.Door.position=p0.Building.TransformPoint(new Vector3(0,1.05f,2.4f));var at=p0.Door.position+p0.Door.forward*6+p0.Door.right*3;at.y=world.GetComponentInChildren<Terrain>().SampleHeight(at)+world.GetComponentInChildren<Terrain>().transform.position.y;
   var yardEnd=at+p0.Door.right*2.5f;foreach(var t in world.GetComponentsInChildren<Transform>().Where(LoosePlant).Where(t=>!t.parent||!LoosePlant(t.parent)).ToArray()){var b=BoundsOf(t.gameObject);var c=b.center;c.y=at.y;var closest=at+Vector3.ClampMagnitude(Vector3.Project(c-at,yardEnd-at),2.5f);if(Vector3.Distance(c,closest)<1.4f)Object.DestroyImmediate(t.gameObject);}
   var dogRoot=Empty("Correll yard dog",at,world);var dog=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));PrefabUtility.UnpackPrefabInstance(dog,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);dog.transform.SetParent(dogRoot,false);ResizePlant(dog,.8f);var db=BoundsOf(dog);dog.transform.position+=at-new Vector3(db.center.x,db.min.y,db.center.z);
   var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/External/AnimatedCanine/Coat.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,"Assets/External/AnimatedCanine/Coat.mat");}mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/External/AnimatedCanine/wolf_Tex.png"));mat.SetFloat("_Smoothness",.13f);EditorUtility.SetDirty(mat);foreach(var r in dog.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
   var anim=dog.GetComponent<Animation>();if(!anim)anim=dog.AddComponent<Animation>();foreach(var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview"))){clip.wrapMode=WrapMode.Loop;anim.AddClip(clip,clip.name);}anim.playAutomatically=false;
   foreach(var id in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Audio/Radio"})){var audio=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(id));var settings=audio.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;audio.defaultSampleSettings=settings;audio.SaveAndReimport();}
   // Fit the pack's authored leaves to the actual openings, not the stair markers.
   foreach(int i in new[]{0,3,4}){var p=scene.Properties[i];string name=i==0?"Cabin1_Door_A":i==3?"Cabin2_Door_A":"BrickHouse_Door_A";var previous=p.transform.Find("Working front door");if(previous)Object.DestroyImmediate(previous.gameObject);
    if(i==0)p.Door.position=p.Building.TransformPoint(new Vector3(0,1.05f,2.4f));
    string leafPath=AssetDatabase.FindAssets(name+" t:Prefab").Select(AssetDatabase.GUIDToAssetPath).First(x=>Path.GetFileNameWithoutExtension(x)==name);var leaf=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(leafPath));leaf.name=name;Convert(leaf);var bounds=leaf.GetComponent<MeshFilter>().sharedMesh.bounds;float scale=Mathf.Abs(p.Building.lossyScale.x);
    var hinge=Empty("Working front door",p.Door.position+p.Door.right*(-bounds.center.x*scale),p.transform);hinge.rotation=p.Door.rotation;leaf.transform.SetParent(hinge,false);leaf.transform.localPosition=Vector3.zero;leaf.transform.localRotation=Quaternion.identity;leaf.transform.localScale=Vector3.one*scale;p.DoorPanel=hinge;
   }
   var volume=world.GetComponentInChildren<UnityEngine.Rendering.Volume>();if(volume){var profile=volume.sharedProfile;if(!profile.TryGet<UnityEngine.Rendering.Universal.MotionBlur>(out var blur)){blur=profile.Add<UnityEngine.Rendering.Universal.MotionBlur>(true);AssetDatabase.AddObjectToAsset(blur,profile);}blur.mode.Override(UnityEngine.Rendering.Universal.MotionBlurMode.CameraOnly);blur.intensity.Override(.16f);blur.clamp.Override(.035f);EditorUtility.SetDirty(profile);EditorUtility.SetDirty(blur);}
   // Keep friendly delivery instructions readable and consistent across every UI surface.
   p0.Instructions="Knock at the front door. Leave a notice if nobody answers.";scene.Properties[4].Instructions="Knock first. The dining table is inside.";
   var doors=scene.Properties.Where(p=>p.DoorPanel&&p.DoorPanel.name=="Working front door").SelectMany(p=>p.DoorPanel.GetComponentsInChildren<Collider>()).ToArray();foreach(var c in doors)c.enabled=false;BakeNavigation();foreach(var c in doors)c.enabled=true;
   EditorSceneManager.MarkSceneDirty(scene.gameObject.scene);if(!EditorSceneManager.SaveScene(scene.gameObject.scene))throw new Exception("Experience scene save failed");AssetDatabase.SaveAssets();ServiceExperienceContract.Run();
   File.WriteAllText(Path.Combine(Work,"experience-scene.txt"),"PASS: "+removed+" misplaced plants/debris removed; zero remaining road-footprint overlaps; canine legacy animation; refinished cabin; navigation baked");ServiceQuickBuild.Build();
  }
 }
}
