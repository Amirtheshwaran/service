using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V28: where exactly a wall is, and where its openings are. SERVICE_RAYS="name:p:floor:ax:az:dx:dz:steps:step"
  // casts, from a+k*step*(along), rays in direction d (along = d turned 90 deg left) at 0.4, 1.2 and 2.0 m over the
  // floor, against what is drawn of the house only; Audit/v28/sense/rays-<name>.txt: one line per step with the hit
  // distance at each height (- for none within 3 m).
  public static void Probe28(){Open();Physics.SyncTransforms();var dir=Path.Combine(Work,"Audit","v28","sense");Directory.CreateDirectory(dir);
   foreach(var spec in (System.Environment.GetEnvironmentVariable("SERVICE_RAYS")??"").Split(';').Where(x=>x.Count(c=>c==':')>=8)){var a=spec.Split(':');var p=county.Properties.First(x=>x.Index==int.Parse(a[1]));float fl=float.Parse(a[2]);
    var start=new Vector3(float.Parse(a[3]),0,float.Parse(a[4]));var d=new Vector3(float.Parse(a[5]),0,float.Parse(a[6])).normalized;var along=new Vector3(-d.z,0,d.x);int steps=int.Parse(a[7]);float step=float.Parse(a[8]);
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,new List<Transform>(),owner);var sb=new StringBuilder();
    try{for(int k=0;k<=steps;k++){var o=start+along*(k*step);sb.Append($"{o.x:F2},{o.z:F2}");
      foreach(float h in new[]{.4f,1.2f,2.0f}){var q=new Vector3(o.x,fl+h,o.z);sb.Append(Physics.Raycast(q,d,out var hit,3f,1<<VisHouse,QueryTriggerInteraction.Collide)?$"\t{hit.distance:F3}":"\t-");}
      sb.AppendLine();}}
    finally{Unvisual(vis);}
    File.WriteAllText(Path.Combine(dir,$"rays-{a[0]}.txt"),sb.ToString());}}
 }
}
