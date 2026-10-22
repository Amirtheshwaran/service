using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V28: how much of each standing thing's base is on something DRAWN. Its foot points (vertices within 1.5 cm of its
  // lowest point, clustered into feet/edges) are each tested straight down against the drawn house and drawn props:
  // "on" within 1.2 cm, "over a gap" when the drawn surface is further down (the collision floor holds it, the eye sees
  // it hover). Every standing prop is listed with the share of its feet that touch, and the worst gap under a foot.
  public static void Support28(){Open();Physics.SyncTransforms();var sb=new StringBuilder();var rows=new List<(float score,string line,ServiceProperty p,Transform t)>();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();
    var byId=new Dictionary<string,Transform>();foreach(var t in roots)byId[IdOf(t)]=t;var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var t in roots){var kind=KindOf(IdOf(t),t.name,lay);if(kind!="floor"&&kind!="top")continue;var nm=t.name.ToLowerInvariant();if(nm.Contains("fire")||nm.Contains("blood")||nm.Contains("apparition")||nm.StartsWith("v27 drive lamp")||nm.Contains("shelf books"))continue;
      var vs=DrawnVerts(t,30000).ToList();if(vs.Count==0)continue;float bottom=vs.Min(v=>v.y);var feet=vs.Where(v=>v.y<bottom+.015f).ToList();
      // cluster foot points on a 4 cm grid so one big leg doesn't outvote three small ones
      var cells=new Dictionary<Vector2Int,Vector3>();foreach(var v in feet){var k=new Vector2Int(Mathf.FloorToInt(v.x/.04f),Mathf.FloorToInt(v.z/.04f));if(!cells.ContainsKey(k)||v.y<cells[k].y)cells[k]=v;}
      int on=0,gapN=0;float worst=0;Vector3 worstAt=Vector3.zero;
      foreach(var v in cells.Values){float gap=float.NaN;foreach(var h in Physics.RaycastAll(v+Vector3.up*.12f,Vector3.down,1.2f,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){bool drawn=h.collider is TerrainCollider||(owner.TryGetValue(h.collider,out var ow)&&ow!=t);if(!drawn)continue;gap=h.distance-.12f;break;}
       if(float.IsNaN(gap)){gapN++;if(1.1f>worst){worst=1.1f;worstAt=v;}continue;}if(gap<=.012f)on++;else{gapN++;if(gap>worst){worst=gap;worstAt=v;}}}
      int n=cells.Count;float share=n>0?on/(float)n:1;string support=lay.TryGetValue(IdOf(t),out var li)&&!string.IsNullOrEmpty(li.on)?li.on:"floor";
      var line=$"p{p.Index} [{kind}] {IdOf(t)} ({t.name.Substring(Mathf.Min(t.name.Length,IdOf(t).Length)).Trim(' ','—')}) on {support}: {on}/{n} feet touch ({share*100:F0}%), worst gap {(worst>=1.1f?"none below":(worst*100).ToString("F1")+" cm")} at {worstAt:F2}";
      rows.Add((share-Mathf.Min(worst,.3f),line,p,t));}}
    finally{Unvisual(vis);}}
   foreach(var r in rows.OrderBy(r=>r.score))sb.AppendLine(r.line);
   sb.Insert(0,$"Support28: {rows.Count} standing props; {rows.Count(r=>r.line.Contains("(0%)"))} with no foot on anything drawn"+System.Environment.NewLine);
   File.WriteAllText(Path.Combine(Work,"Audit","v28","support28.txt"),sb.ToString());
   // level views at the base of the 36 worst, for looking at
   if(UnityEngine.SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;var dir=Path.Combine(Work,"Audit","v28","support");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=42;cam.aspect=4f/3f;cam.nearClipPlane=.01f;int k2=0;
   try{foreach(var r in rows.OrderBy(r=>r.score).Take(36)){var vs=DrawnVerts(r.t,6000).ToList();float bottom=vs.Min(v=>v.y);var c=new Vector3(vs.Average(v=>v.x),bottom,vs.Average(v=>v.z));float rad=Mathf.Max(vs.Max(v=>new Vector2(v.x-c.x,v.z-c.z).magnitude)*2.2f,.4f);
     for(int a=0;a<2;a++){var dv=Quaternion.Euler(0,a*90+25,0)*Vector3.forward;var pos=c+dv*rad+Vector3.up*.04f;if(Physics.Raycast(c+Vector3.up*.05f,dv,out var hh,rad,~0,QueryTriggerInteraction.Ignore))pos=c+dv*Mathf.Max(.25f,hh.distance-.05f)+Vector3.up*.04f;cam.transform.position=pos;cam.transform.LookAt(c+Vector3.up*.05f);Shoot(cam,Path.Combine(dir,$"{k2:00}-p{r.p.Index}-{IdOf(r.t)}-{a}.jpg"),400,300);}k2++;}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}}
 }
}
