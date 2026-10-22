using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V28 ("avoid floating furniture and weird asset placements"): every standing thing seated on what is DRAWN under it.
  // Its feet (the lowest 1.5 cm of its mesh, on a 4 cm grid) must all be on the drawn top it belongs on - the table, the
  // nightstand, the stove, or for floor pieces the drawn floorboards (the ruined houses' floors have gaps and holes over a
  // flat walking floor) - and 1.5 cm in from any edge. A piece that hangs over an edge or over a hole moves the shortest
  // way to a spot where all its feet are on wood and level, then is checked: nothing of it through the house or another
  // prop; things standing on it move with it. Seat28 reports, Seat28Fix moves. SERVICE_LOOK limits it to some props.
  public static void Seat28(){Seat(false);}
  public static void Seat28Fix(){Seat(true);}
  static void Seat(bool fix){Open();Physics.SyncTransforms();var sb=new StringBuilder();int checkedN=0,bad=0,moved=0,left=0;
   var only=(System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).ToList();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();
    var byId=new Dictionary<string,Transform>();foreach(var t in roots)byId[IdOf(t)]=t;
    var deps=new Dictionary<Transform,List<Transform>>();foreach(var t in roots)if(lay.TryGetValue(IdOf(t),out var li)&&!string.IsNullOrEmpty(li.on)&&byId.TryGetValue(li.on,out var sup)){if(!deps.ContainsKey(sup))deps[sup]=new List<Transform>();deps[sup].Add(t);}
    HashSet<Transform> Tree(Transform t){var s=new HashSet<Transform>{t};if(deps.TryGetValue(t,out var ds))foreach(var c in ds)s.UnionWith(Tree(c));return s;}
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var t in roots){if(only.Count>0&&!only.Contains($"{p.Index}:{IdOf(t)}"))continue;var kind=KindOf(IdOf(t),t.name,lay);if(kind!="floor"&&kind!="top")continue;
      var nm=t.name.ToLowerInvariant();if(nm.Contains("fire")||nm.Contains("blood")||nm.Contains("apparition")||nm.StartsWith("v27 drive lamp")||nm.Contains("shelf books")||nm.Contains("hearth")||nm.Contains("rug")||nm.Contains("doormat")||nm.Contains("prop_car")||nm.Contains("lamp_e")||nm.Contains("mailbox"))continue;
      Transform support=null;if(kind=="top"){if(!lay.TryGetValue(IdOf(t),out var tl)||string.IsNullOrEmpty(tl.on)||!byId.TryGetValue(tl.on,out support))continue;}
      var tree=Tree(t);var sup2=support;bool IsSupport(Transform who)=>kind=="top"?who==sup2:(who==p.Building||(who&&FlatWords.Any(w=>who.name.ToLowerInvariant().Contains(w))));
      // the support surface under a point: the first drawn thing below that is not this piece (or what stands on it)
      float Under(Vector3 at,float from,out Transform who){who=null;foreach(var h in Physics.RaycastAll(new Vector3(at.x,from,at.z),Vector3.down,from-at.y+1.2f,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(h.collider,out var ow)||tree.Contains(ow))continue;who=ow;return h.point.y;}return float.NaN;}
      var vs=DrawnVerts(t,30000).ToList();if(vs.Count==0)continue;checkedN++;float bottom=vs.Min(v=>v.y),top=vs.Max(v=>v.y);
      var cells=new Dictionary<Vector2Int,Vector3>();foreach(var v in vs.Where(v=>v.y<bottom+.015f)){var k=new Vector2Int(Mathf.FloorToInt(v.x/.04f),Mathf.FloorToInt(v.z/.04f));if(!cells.ContainsKey(k)||v.y<cells[k].y)cells[k]=v;}
      var feet=cells.Values.ToList();var cen=new Vector3(feet.Average(v=>v.x),0,feet.Average(v=>v.z));
      // each foot, and the same foot 1.5 cm further out from the middle (the edge margin)
      var probes=new List<Vector3>();foreach(var f in feet){var o=new Vector3(f.x-cen.x,0,f.z-cen.z);probes.Add(o);if(o.sqrMagnitude>1e-6f)probes.Add(o+o.normalized*.015f);}
      if(probes.Count>120)probes=Enumerable.Range(0,120).Select(i=>probes[i*probes.Count/120]).ToList();
      bool Fits(Vector3 at,float rise,out float level){level=float.MinValue;float lo=float.MaxValue,hi=float.MinValue;
       foreach(var o in probes){var q=at+o;float y=Under(new Vector3(q.x,bottom,q.z),bottom+rise,out var who);if(float.IsNaN(y)||!IsSupport(who))return false;lo=Mathf.Min(lo,y);hi=Mathf.Max(hi,y);}
       level=hi;return hi-lo<=.012f;}
      // where it stands now
      int nowOn=0;float nowWorst=0;foreach(var f in feet){float y=Under(f,bottom+.12f,out var who);float g=float.IsNaN(y)?1f:bottom-y;if(!float.IsNaN(y)&&IsSupport(who)&&g<=.012f&&g>=-.03f)nowOn++;else nowWorst=Mathf.Max(nowWorst,float.IsNaN(y)?1f:Mathf.Abs(g));}
      if(Fits(cen,.12f,out var lvNow)&&Mathf.Abs(bottom-lvNow)<=.012f)continue;
      // old boards are uneven: a floor piece is a problem only if a foot is over a real gap or edge (more than 2.5 cm), or
      // under half its feet touch
      float share=feet.Count>0?nowOn/(float)feet.Count:1;if(kind=="floor"&&nowWorst<=.025f&&share>=.5f)continue;
      bad++;var line=$"p{p.Index} [{kind}] {IdOf(t)} ({t.name.Substring(Mathf.Min(t.name.Length,IdOf(t).Length)).Trim(' ','—')}) on {(kind=="top"?IdOf(support):"the floor")}: {nowOn}/{feet.Count} feet on it, worst {(nowWorst>=1?"nothing under":(nowWorst*100).ToString("F1")+" cm")}";
      if(!fix){sb.AppendLine(line);continue;}
      // candidates around where it is, nearest first (tops: anywhere on the support; floor pieces: within 45 cm)
      // a floor piece that only hovers (every foot has floor a little under it, level): set it down where it is
      if(kind=="floor"){float lo2=float.MaxValue,hi2=float.MinValue;bool all=true;foreach(var o in probes){var q=cen+o;float y=Under(new Vector3(q.x,bottom,q.z),bottom+.03f,out var who);if(float.IsNaN(y)||!IsSupport(who)||bottom-y>.04f){all=false;break;}lo2=Mathf.Min(lo2,y);hi2=Mathf.Max(hi2,y);}
       if(all&&hi2-lo2<=.012f){var dn=new Vector3(0,hi2+.0015f-bottom,0);foreach(var x in tree)x.position+=dn;Physics.SyncTransforms();moved++;sb.AppendLine(line+$"  -> set down {(-dn.y*100):F1} cm where it stands");continue;}}
      float reach=kind=="top"?1.2f:.5f;var cands=new List<Vector3>();for(float dx=-reach;dx<=reach+1e-4f;dx+=.01f)for(float dz=-reach;dz<=reach+1e-4f;dz+=.01f){if(dx*dx+dz*dz>reach*reach)continue;cands.Add(new Vector3(dx,0,dz));}
      cands=cands.OrderBy(c=>c.sqrMagnitude).ToList();bool done=false;int tried=0;
      foreach(var c in cands){var at=cen+c;if(!Fits(at,kind=="top"?(top-bottom)+.3f:.12f,out var lv))continue;
       // headroom: nothing drawn over the place for its height
       bool room=true;foreach(var o in probes.Take(16)){var q=at+o;foreach(var h in Physics.RaycastAll(new Vector3(q.x,lv+.01f,q.z),Vector3.up,top-bottom,VisMask,QueryTriggerInteraction.Collide)){if(owner.TryGetValue(h.collider,out var ow)&&!tree.Contains(ow)){room=false;break;}}if(!room)break;}
       if(!room)continue;if(++tried>40)break;
       var delta=new Vector3(c.x,lv+.0015f-bottom,c.z);foreach(var x in tree)x.position+=delta;Physics.SyncTransforms();
       // through anything now? (every vertex from the piece's middle, against the drawn house and the other props)
       float worst=0;var nv=DrawnVerts(t,4000).ToList();var mid=new Vector3(nv.Average(v=>v.x),0,nv.Average(v=>v.z));float nb=nv.Min(v=>v.y);
       foreach(var v in nv){if(v.y<nb+.02f)continue;var o2=new Vector3(mid.x,v.y,mid.z);var dv=v-o2;float len=dv.magnitude;if(len<.02f)continue;foreach(var h in Physics.RaycastAll(o2,dv/len,len,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(h.collider,out var ow)||tree.Contains(ow))continue;worst=Mathf.Max(worst,Vector3.Dot(v-h.point,-h.normal));break;}}
       if(worst>.01f){foreach(var x in tree)x.position-=delta;Physics.SyncTransforms();continue;}
       moved++;done=true;sb.AppendLine(line+$"  -> moved {new Vector2(c.x,c.z).magnitude*100:F1} cm sideways, {(delta.y*100):+0.0;-0.0} cm up, all feet on it ({tree.Count-1} thing(s) on it moved too)");break;}
      if(!done){left++;sb.AppendLine(line+"  -> NO clean spot nearby - left for a hand fix");}}}
    finally{Unvisual(vis);}}
   sb.Insert(0,$"Seat28{(fix?"Fix":"")}: {checkedN} standing props, {bad} not seated, {moved} moved, {left} left"+System.Environment.NewLine);File.WriteAllText(Path.Combine(Work,"Audit","v28",fix?"seat28fix.txt":"seat28.txt"),sb.ToString());if(fix){log.Append(sb.ToString());Save("Seat28Fix");}}
 }
}
