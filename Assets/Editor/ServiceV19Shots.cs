using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace ServiceGameV2.Editor {
 // Editor-side photographs (batch mode with graphics): catalog turnarounds of props, and interior views plus
 // top-down cutaways of every delivery house, so layouts can be checked without building the player.
 public static partial class ServiceV19Rebuild {
  static List<(ScriptableRendererFeature f,bool was)> MuteFeatures(){
   var list=new List<(ScriptableRendererFeature,bool)>();
   foreach(var guid in AssetDatabase.FindAssets("t:UniversalRendererData")){var d=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));if(!d)continue;foreach(var f in d.rendererFeatures)if(f&&f.name.Contains("Camcorder")){list.Add((f,f.isActive));f.SetActive(false);}}
   return list;
  }
  static void Restore(List<(ScriptableRendererFeature f,bool was)> l){foreach(var (f,was) in l)f.SetActive(was);}
  static void Shoot(Camera cam,string path,int w,int h){
   var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);rt.antiAliasing=2;cam.targetTexture=rt;cam.Render();
   var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(w,h,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();
   File.WriteAllBytes(path,path.EndsWith(".jpg")?tex.EncodeToJPG(88):tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=prev;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
  }
  // Four-side turnaround of each named prop (Audit/catalog2-list.txt, one name per line; empty = every V19 prop folder).
  public static void CatalogRender(){
   var muted=MuteFeatures();
   try{
    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
    RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.64f,.68f);RenderSettings.ambientEquatorColor=new Color(.45f,.45f,.45f);RenderSettings.ambientGroundColor=new Color(.25f,.24f,.22f);
    var sun=new GameObject("Key").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.transform.rotation=Quaternion.Euler(42,-35,0);sun.shadows=LightShadows.Soft;
    var fill=new GameObject("Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.transform.rotation=Quaternion.Euler(30,150,0);
    var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.localScale=Vector3.one*4;var fm=new Material(Shader.Find("Universal Render Pipeline/Lit"));fm.SetColor("_BaseColor",new Color(.32f,.32f,.33f));floor.GetComponent<Renderer>().sharedMaterial=fm;
    var cam=new GameObject("Cam").AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.55f,.57f,.6f);cam.fieldOfView=35;cam.nearClipPlane=.02f;cam.farClipPlane=100;
    cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
    var listFile=Path.Combine(Work,"Audit","catalog2-list.txt");var names=File.Exists(listFile)?File.ReadAllLines(listFile).Select(l=>l.Trim()).Where(l=>l.Length>0).ToArray():Directory.GetDirectories(PropRoot).Select(Path.GetFileName).ToArray();
    var outDir=Path.Combine(Work,"Audit","catalog2");Directory.CreateDirectory(outDir);var sizes=new List<string>();
    foreach(var name in names){
     var path=name.Contains("/")?name:File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName,$"{PropRoot}/{name}/{name}.prefab"))?$"{PropRoot}/{name}/{name}.prefab":$"{PropRoot}/Flooded/{name}.prefab";
     var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab){sizes.Add(name+"\tMISSING "+path);continue;}
     var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
     var b=BoundsOf(g);floor.transform.position=new Vector3(0,b.min.y-.001f,0);float r=Mathf.Max(b.extents.magnitude,.12f);float dist=r/Mathf.Sin(cam.fieldOfView*.5f*Mathf.Deg2Rad)*1.05f;
     int k=0;foreach(var dir in new[]{Vector3.forward,Vector3.right,Vector3.back,Vector3.left}){
      var from=b.center+(dir*Mathf.Cos(18*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(18*Mathf.Deg2Rad))*dist;cam.transform.position=from;cam.transform.LookAt(b.center);
      var tag=new[]{"pZ","pX","nZ","nX"}[k++];Shoot(cam,Path.Combine(outDir,$"{Path.GetFileNameWithoutExtension(path)}_{tag}.png"),360,360);}
     sizes.Add($"{Path.GetFileNameWithoutExtension(path)}\t{path}\t{b.size.x:F2}\t{b.size.y:F2}\t{b.size.z:F2}\t{b.center.x:F2}\t{b.min.y:F2}\t{b.center.z:F2}");
     Object.DestroyImmediate(g);
    }
    File.WriteAllLines(Path.Combine(outDir,"sizes.tsv"),sizes);
   }finally{Restore(muted);AssetDatabase.SaveAssets();}
  }
  // Interior photographs of every delivery house: door view, the walk to the table, the table, room turnarounds at the table
  // and at every room centre listed in Audit/layouts/rooms-p<N>.json (optional), plus a top-down cutaway of each storey.
  public static void RoomShots(){
   Open();var muted=MuteFeatures();var outDir=Path.Combine(Work,"Audit","roomshots");Directory.CreateDirectory(outDir);foreach(var f in Directory.GetFiles(outDir))File.Delete(f);
   Physics.SyncTransforms();var nav=NavMesh.AddNavMeshData(county.Navigation);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   var lights=new List<Light>();
   try{
    RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.36f,.36f,.38f);
    foreach(var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)){if(l.type==LightType.Directional)l.intensity=Mathf.Max(l.intensity,.15f);}
    var cam=new GameObject("V19 room camera").AddComponent<Camera>();cam.fieldOfView=70;cam.nearClipPlane=.05f;cam.farClipPlane=120;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.05f,.06f,.08f);
    var cd=cam.gameObject.AddComponent<UniversalAdditionalCameraData>();cd.renderPostProcessing=false;
    var work=new GameObject("Work light").AddComponent<Light>();work.type=LightType.Point;work.range=14;work.intensity=2.4f;work.color=new Color(1,.96f,.9f);work.shadows=LightShadows.Soft;work.transform.SetParent(cam.transform,false);work.transform.localPosition=new Vector3(0,.35f,-.2f);
    var index=new List<string>();
    foreach(var p in county.Properties.OrderBy(x=>x.Index)){
     if(p.DoorPanel){var hinge=p.DoorPanel;hinge.localRotation=hinge.localRotation;} // doors are shot as authored; open views step through the doorway
     var outward=OutwardOf(p);var views=new List<(string name,Vector3 pos,Vector3 fwd)>();
     var inside=p.Door.position-outward*.7f;inside.y=p.Door.position.y+1.55f;views.Add(("door-in",inside+outward*.2f,-outward));
     var path=new NavMeshPath();if(NavMesh.SamplePosition(p.Door.position-outward*.9f,out var a,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(p.TableApproach.position,out var t,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,t.position,NavMesh.AllAreas,path)){
      var c=path.corners;float acc=0,next=2.2f;for(int i=1;i<c.Length;i++){float len=Vector3.Distance(c[i-1],c[i]);for(float s=0;s<len;s+=.25f){acc+=.25f;if(acc>=next){next+=2.6f;var q=Vector3.Lerp(c[i-1],c[i],s/len);var ahead=c[i]-c[i-1];ahead.y=0;if(ahead.sqrMagnitude<.01f)continue;views.Add(($"walk{views.Count:00}",q+Vector3.up*1.6f,ahead.normalized));}}}}
     var stand=p.TableApproach.position+Vector3.up*1.6f;var toTable=p.DeliveryPoint.position-p.TableApproach.position;toTable.y=0;views.Add(("table",stand,toTable.sqrMagnitude>.01f?toTable.normalized:Vector3.forward));
     for(int d=0;d<360;d+=90)views.Add(($"atTable-{d:000}",stand,Quaternion.Euler(0,d,0)*Vector3.forward));
     var roomsFile=Path.Combine(Work,"Audit","layouts",$"rooms-p{p.Index}.json");
     if(File.Exists(roomsFile)){foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(roomsFile),"\"name\"\\s*:\\s*\"([^\"]+)\"[^}]*?\"x\"\\s*:\\s*(-?[0-9.]+)[^}]*?\"z\"\\s*:\\s*(-?[0-9.]+)[^}]*?\"floor\"\\s*:\\s*(-?[0-9.]+)")){var pos=new Vector3(float.Parse(m.Groups[2].Value),float.Parse(m.Groups[4].Value)+1.6f,float.Parse(m.Groups[3].Value));for(int d=0;d<360;d+=90)views.Add(($"{m.Groups[1].Value}-{d:000}",pos,Quaternion.Euler(0,d,0)*Vector3.forward));}}
     cam.orthographic=false;cam.fieldOfView=70;work.enabled=true;
     foreach(var v in views){cam.transform.position=v.pos;cam.transform.rotation=Quaternion.LookRotation(Quaternion.AngleAxis(12,Vector3.Cross(Vector3.up,v.fwd).normalized)*v.fwd);var file=$"p{p.Index}-{v.name}.jpg";Shoot(cam,Path.Combine(outDir,file),800,500);index.Add($"{file}\tcamera ({v.pos.x:F1},{v.pos.y:F1},{v.pos.z:F1}) facing {Mathf.Repeat(Mathf.Atan2(v.fwd.x,v.fwd.z)*Mathf.Rad2Deg,360):F0} deg (0=N/+Z, 90=E/+X)");}
     // Top-down cutaways: an orthographic camera just under each ceiling looking straight down.
     var b=p.InteriorBounds;var floors=new List<float>();
     void AddFloor(Vector3 probe){if(Physics.Raycast(probe+Vector3.up*.8f,Vector3.down,out var h,3,~0,QueryTriggerInteraction.Ignore)&&!floors.Any(f=>Mathf.Abs(f-h.point.y)<1.2f))floors.Add(h.point.y);}
     AddFloor(p.Door.position-outward*1.2f);AddFloor(p.TableApproach.position);
     cam.orthographic=true;work.enabled=false;var top=new GameObject("Top light").AddComponent<Light>();top.type=LightType.Directional;top.intensity=1.1f;top.transform.rotation=Quaternion.Euler(70,30,0);top.shadows=LightShadows.None;
     float halfX=b.extents.x+.8f,halfZ=b.extents.z+.8f;int W=900,H=Mathf.Clamp(Mathf.RoundToInt(W*halfZ/halfX),300,1800);
     foreach(var f in floors){cam.transform.position=new Vector3(b.center.x,f+2.15f,b.center.z);cam.transform.rotation=Quaternion.Euler(90,0,0);cam.nearClipPlane=.01f;cam.farClipPlane=2.6f;cam.orthographicSize=halfZ;var file=$"p{p.Index}-top-floor{f:F1}.jpg";Shoot(cam,Path.Combine(outDir,file),W,H);index.Add($"{file}	top-down cutaway at {f+2.15f:F2} m, north (+Z) UP, east (+X) RIGHT; x {b.center.x-halfX:F2}..{b.center.x+halfX:F2}, z {b.center.z-halfZ:F2}..{b.center.z+halfZ:F2}");}
     Object.DestroyImmediate(top.gameObject);cam.farClipPlane=120;cam.nearClipPlane=.05f;
    }
    File.WriteAllLines(Path.Combine(outDir,"index.tsv"),index);
    Object.DestroyImmediate(cam.gameObject);
   }finally{nav.Remove();if(county.LateRoad)county.LateRoad.SetActive(lateWas);Restore(muted);}
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void PropsAndCatalog(){Props();WrapFlooded();CatalogRender();}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void WrapCatalogShots(){WrapFlooded();CatalogRender();RoomShots();}
 }
}
