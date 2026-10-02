using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Overview of the county road: a top-down orthographic plan and low views along the road at intervals.
  public static void RoadLook(){Open();var dir=Path.Combine(Work,"Audit","roads20");Directory.CreateDirectory(dir);var muted=MuteFeatures();
   var cam=new GameObject("road cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~0;
   var fog=RenderSettings.fog;RenderSettings.fog=false;
   try{
    cam.orthographic=true;cam.orthographicSize=130;cam.transform.SetPositionAndRotation(new Vector3(10,300,240),Quaternion.Euler(90,0,0));cam.farClipPlane=600;cam.nearClipPlane=1;Shoot(cam,Path.Combine(dir,"plan-south.png"),1024,1024);
    cam.orthographicSize=70;cam.transform.position=new Vector3(15,300,180);Shoot(cam,Path.Combine(dir,"plan-mid.png"),1024,1024);
    cam.orthographic=false;cam.fieldOfView=62;cam.nearClipPlane=.1f;RenderSettings.fog=fog;
    var line=CatmullRom(MainRoad,2f).Select(p=>new Vector3(p.x,0,p.y)).ToList();
    foreach(int k in new[]{10,45,85,120,160,185}){if(k>=line.Count-3)continue;var a=line[k];var b=line[k+3];var t=(b-a).normalized;var right=new Vector3(t.z,0,-t.x);var eye=a+right*1.6f;eye.y=GroundAt(eye)+1.3f;cam.transform.position=eye;cam.transform.rotation=Quaternion.LookRotation(t+Vector3.down*.06f);Shoot(cam,Path.Combine(dir,$"drive-{k:000}.png"),960,540);}
   }finally{RenderSettings.fog=fog;Object.DestroyImmediate(cam.gameObject);Restore(muted);}
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void PoleProbe(){Open();var h=county.transform.Find("V20 utility poles");var sb=new System.Text.StringBuilder();if(!h){System.IO.File.WriteAllText(System.IO.Path.Combine(Work,"Audit","pole-probe.txt"),"no poles");return;}
   foreach(Transform p in h){var rs=p.GetComponentsInChildren<Renderer>();var b=rs.Length>0?rs[0].bounds:new Bounds(p.position,Vector3.zero);foreach(var r in rs)b.Encapsulate(r.bounds);sb.AppendLine($"{p.name} pos {p.position} rot {p.eulerAngles} scale {p.localScale} bounds {b.center} size {b.size} renderers {rs.Length} enabled {rs.Count(r=>r.enabled)} mats {string.Join(",",rs.SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).Distinct())}");}
   var first=h.GetChild(0);var muted=MuteFeatures();var cam=new GameObject("pole cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~0;cam.fieldOfView=60;
   try{cam.transform.position=first.position+new Vector3(14,3,-14);cam.transform.LookAt(first.position+Vector3.up*6);Shoot(cam,System.IO.Path.Combine(Work,"Audit","roads20","pole-first.png"),960,540);}finally{Object.DestroyImmediate(cam.gameObject);Restore(muted);}
   System.IO.File.WriteAllText(System.IO.Path.Combine(Work,"Audit","pole-probe.txt"),sb.ToString());}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void PoleImport(){var imp=(ModelImporter)AssetImporter.GetAtPath("Assets/ServiceArt/V20/Powerline/Powerline_V003.fbx");var fbx=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ServiceArt/V20/Powerline/Powerline_V003.fbx");
   var sb=new System.Text.StringBuilder();sb.AppendLine($"importer globalScale {imp.globalScale} useFileScale {imp.useFileScale} fileScale {imp.fileScale}");
   var g=(GameObject)PrefabUtility.InstantiatePrefab(fbx);foreach(var r in g.GetComponentsInChildren<Renderer>())sb.AppendLine($"renderer {r.name} bounds {r.bounds.size} localScale chain {r.transform.lossyScale} mesh {(r.GetComponent<MeshFilter>()?r.GetComponent<MeshFilter>().sharedMesh.bounds.size.ToString():"-")}");
   sb.AppendLine("root rot "+g.transform.eulerAngles+" child "+string.Join(";",g.GetComponentsInChildren<Transform>().Select(t=>t.name+" "+t.localScale+" "+t.localEulerAngles)));Object.DestroyImmediate(g);
   System.IO.File.WriteAllText(System.IO.Path.Combine(Work,"Audit","pole-import.txt"),sb.ToString());}
 }
}
