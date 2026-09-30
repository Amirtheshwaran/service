using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2.Editor {
 // V18: furnish the delivery rooms with Poly Haven furniture (CC0), Fears to Fathom style.
 // A piece is only placed when it stands on the room floor with its back to a wall found by raycast,
 // overlaps no existing collider, stays inside the property, and keeps clear of the walking path, doors, table and player spot.
 public static class ServiceV18Interiors {
  const string F="Assets/ServiceArt/V18/Furniture",ScenePath="Assets/Scenes/HollisCounty.unity";
  static readonly Dictionary<int,string[]> Plan=new Dictionary<int,string[]>{
   {1,new[]{"Shelf_01","ArmChair_01","cardboard_box_01","WoodenChair_01","cardboard_box_01","ClassicNightstand_01"}},
   {3,new[]{"Shelf_01","WoodenChair_01","cardboard_box_01","ClassicNightstand_01","cardboard_box_01"}},
   {5,new[]{"ArmChair_01","ClassicNightstand_01","Shelf_01","WoodenChair_01","cardboard_box_01"}},
   {2,new[]{"Sofa_01","Rockingchair_01","ClassicNightstand_01","WoodenChair_01"}}};
  public static bool FrontIsPlusZ=true;
  public static void RunAndBuild(){Run();ServiceV17Apply.Build();}
  static GameObject Prefab(string name){
   var path=$"{F}/{name}/{name}.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(existing)return existing;
   var model=AssetDatabase.LoadAssetAtPath<GameObject>($"{F}/{name}/{name}.fbx");
   var matPath=$"{F}/{name}/{name}.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
   if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
   mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>($"{F}/{name}/{name}_diff_1k.jpg"));mat.SetFloat("_Smoothness",.18f);mat.enableInstancing=true;EditorUtility.SetDirty(mat);
   var root=new GameObject(name);var inst=Object.Instantiate(model);inst.name="Model";inst.transform.SetParent(root.transform,false);
   var rs=inst.GetComponentsInChildren<MeshRenderer>();foreach(var r in rs)r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
   var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);inst.transform.position+=new Vector3(-b.center.x,-b.min.y,-b.center.z);
   foreach(var mf in inst.GetComponentsInChildren<MeshFilter>()){var mc=mf.gameObject.AddComponent<MeshCollider>();mc.convex=true;}
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return prefab;
  }
  static Bounds Size(GameObject prefab){var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);Object.DestroyImmediate(g);return b;}
  static float SegDist(Vector3 p,Vector3 a,Vector3 b){p.y=a.y=b.y=0;var ab=b-a;float t=ab.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude);return Vector3.Distance(p,a+ab*t);}
  public static void Run(){
   var log=new StringBuilder();
   var scene=EditorSceneManager.OpenScene(ScenePath);var county=Object.FindAnyObjectByType<CountyScene>();
   var nav=NavMesh.AddNavMeshData(county.Navigation);
   var old=county.transform.Find("V18 interiors — Poly Haven furniture (CC0)");if(old)Object.DestroyImmediate(old.gameObject);
   // Each piece belongs to its property, so it hides with buildings that only exist on some nights.
   foreach(var t in county.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("V18 furniture — ")).ToList())Object.DestroyImmediate(t.gameObject);
   Physics.SyncTransforms();
   foreach(var kv in Plan){
    var p=county.Properties.First(x=>x.Index==kv.Key);
    if(!Physics.Raycast(p.TableApproach.position+Vector3.up*.6f,Vector3.down,out var floorHit,3,~0,QueryTriggerInteraction.Ignore)){log.AppendLine($"ROOM {p.Index} no floor");continue;}
    float floorY=floorHit.point.y;
    var path=new List<Vector3>();
    if(NavMesh.SamplePosition(p.Door.position,out var a,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(p.TableApproach.position,out var b,2,NavMesh.AllAreas)){var np=new NavMeshPath();if(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,np))path.AddRange(np.corners);}
    if(path.Count<2)path.AddRange(new[]{p.Door.position,p.TableApproach.position});
    var doors=p.Building?p.Building.GetComponentsInChildren<Transform>(true).Where(t=>t.name.ToLower().Contains("door")).Select(t=>t.position).ToList():new List<Vector3>();
    if(p.DoorPanel)doors.Add(p.DoorPanel.position);
    var walls=new List<(Vector3 point,Vector3 normal)>();
    foreach(var origin in new[]{p.TableApproach.position,p.DeliveryPoint.position}){
     var o=new Vector3(origin.x,floorY+1.05f,origin.z);
     for(int i=0;i<32;i++){var dir=Quaternion.Euler(0,i*11.25f,0)*Vector3.forward;if(Physics.Raycast(o,dir,out var h,7,~0,QueryTriggerInteraction.Ignore)&&h.distance>.9f&&Mathf.Abs(h.normal.y)<.15f)walls.Add((h.point,new Vector3(h.normal.x,0,h.normal.z).normalized));}
    }
    int placed=0;var rejects=new Dictionary<string,int>();void Reject(string why){rejects[why]=rejects.TryGetValue(why,out var n)?n+1:1;}
    foreach(var item in kv.Value){
     var prefab=Prefab(item);var size=Size(prefab).size;float w=size.x,h=size.y,dp=size.z,reach=Mathf.Max(w,dp)*.5f;bool ok=false;
     foreach(var (point,normal) in walls){
      var tangent=Vector3.Cross(Vector3.up,normal);
      foreach(float slide in new[]{0f,.7f,-.7f,1.4f,-1.4f}){
       var pos=point+normal*(dp*.5f+.05f)+tangent*slide;pos.y=floorY;
       var rot=Quaternion.LookRotation(FrontIsPlusZ?normal:-normal,Vector3.up);
       if(p.InteriorBounds.size.sqrMagnitude>1&&!p.InteriorBounds.Contains(pos+Vector3.up*.5f)){Reject("outside");continue;}
       bool floor=true;foreach(var c in new[]{new Vector3(-w,0,-dp),new Vector3(w,0,-dp),new Vector3(-w,0,dp),new Vector3(w,0,dp),Vector3.zero}){var q=pos+rot*(c*.45f)+Vector3.up*.45f;if(!Physics.Raycast(q,Vector3.down,out var fh,1.2f,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(fh.point.y-floorY)>.12f){floor=false;break;}}
       if(!floor){Reject("floor");continue;}
       if(Physics.OverlapBox(pos+Vector3.up*(h*.5f+.07f),new Vector3(w*.5f-.03f,h*.5f-.06f,dp*.5f-.03f),rot,~0,QueryTriggerInteraction.Ignore).Length>0){Reject("overlap");continue;}
       bool clearPath=true;for(int i=1;i<path.Count;i++)if(Mathf.Abs(path[i].y-floorY)<1.5f&&SegDist(pos,path[i-1],path[i])<reach+.8f){clearPath=false;break;}
       if(!clearPath){Reject("path");continue;}
       if(Vector3.Distance(Flat(pos),Flat(p.TableApproach.position))<reach+1.1f||Vector3.Distance(Flat(pos),Flat(p.DeliveryPoint.position))<reach+.7f){Reject("table");continue;}
       if(doors.Any(dr=>Mathf.Abs(dr.y-floorY)<2.6f&&Vector3.Distance(Flat(pos),Flat(dr))<reach+1.3f)){Reject("door");continue;}
       var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,p.transform);go.name=$"V18 furniture — {item}";go.transform.SetPositionAndRotation(pos,rot);go.isStatic=true;Physics.SyncTransforms();
       placed++;ok=true;log.AppendLine($"  PLACED {item} at {pos} facing {normal}");break;
      }
      if(ok)break;
     }
     if(!ok)log.AppendLine($"  SKIPPED {item}");
    }
    log.AppendLine($"ROOM {p.Index} {p.Address}: walls={walls.Count} placed={placed} rejects={string.Join(",",rejects.Select(r=>r.Key+"="+r.Value))}");
   }
   nav.Remove();
   // House signs used the default text shader, which is not depth-tested, so their lettering showed through walls
   // (mirrored inside the late cabin). Give every sign the project's depth-tested world-type material.
   var worldType=AssetDatabase.FindAssets("t:Material").Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(m=>m&&m.shader&&m.shader.name=="Service/World Type");
   int signs=0;if(worldType)foreach(var tm in county.GetComponentsInChildren<TextMesh>(true)){if(!tm.GetComponentInParent<ServiceProperty>(true)||tm.GetComponentInParent<ServiceInstrumentText>(true))continue;var it=tm.gameObject.AddComponent<ServiceInstrumentText>();it.SurfaceMaterial=worldType;signs++;}
   log.AppendLine($"SIGNS depth-tested {signs} (material {(worldType?worldType.name:"missing")})");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"v18-interiors.txt"),log.ToString());
  }
  static bool SignSized(Transform t){var rs=t.GetComponentsInChildren<Renderer>(true);if(rs.Length==0||rs.Length>8)return false;var bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);return bb.size.x<3&&bb.size.y<3&&bb.size.z<3;}
  static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
 }
}
