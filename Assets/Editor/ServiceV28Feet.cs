using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V28: a close look at where listed props meet what they stand on - four low views round each, level with its base,
  // from the room side (a view whose line from the prop is cut by a wall is skipped)
  // (SERVICE_LOOK="1:landing-chair,5:study-desk"). Shots: Audit/v28/feet/<prop>-<n>.jpg
  public static void Feet28(){Open();Physics.SyncTransforms();var dir=Path.Combine(Work,"Audit","v28","feet");Directory.CreateDirectory(dir);
   var list=(System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1])).ToList();
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.aspect=4f/3f;cam.nearClipPlane=.01f;cam.fieldOfView=50;
   try{foreach(var (pi,id) in list){var p=county.Properties.First(x=>x.Index==pi);var t=PropRoots(p).FirstOrDefault(x=>IdOf(x)==id);if(!t)continue;var vs=DrawnVerts(t,6000).ToList();if(vs.Count==0)continue;
     float bottom=vs.Min(v=>v.y),top=vs.Max(v=>v.y);var c=new Vector3(vs.Average(v=>v.x),bottom,vs.Average(v=>v.z));float rad=Mathf.Max(.3f,vs.Max(v=>new Vector2(v.x-c.x,v.z-c.z).magnitude));
     var own=t.GetComponentsInChildren<Collider>().ToList();int n=0;
     for(int a=0;a<24&&n<4;a++){var dv=Quaternion.Euler(0,15+a*45+(a>=8?22.5f:0),0)*Vector3.forward;var pos=c+dv*(rad+.75f)+Vector3.up*.1f;var mid=c+Vector3.up*Mathf.Min(.5f,(top-bottom)*.5f);
      if(Physics.Linecast(mid+dv*rad*.9f,pos,out var hit,~0,QueryTriggerInteraction.Ignore)&&!own.Contains(hit.collider))continue;
      cam.transform.position=pos;cam.transform.LookAt(c+Vector3.up*.05f);Shoot(cam,Path.Combine(dir,$"p{pi}-{id}-{n}.jpg"),640,480);n++;}}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}}
  // the same from where you can stand: the walkable spots round it, low and at eye height (Audit/v28/feet/room-*.jpg)
  public static void FeetRoom28(){Open();Physics.SyncTransforms();var dir=Path.Combine(Work,"Audit","v28","feet");Directory.CreateDirectory(dir);
   var list=(System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1])).ToList();
   var nav=UnityEngine.AI.NavMesh.AddNavMeshData(county.Navigation);var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.aspect=4f/3f;cam.nearClipPlane=.01f;cam.fieldOfView=55;
   try{foreach(var (pi,id) in list){var p=county.Properties.First(x=>x.Index==pi);var t=PropRoots(p).FirstOrDefault(x=>IdOf(x)==id);if(!t)continue;var vs=DrawnVerts(t,6000).ToList();if(vs.Count==0)continue;
     float bottom=vs.Min(v=>v.y),top=vs.Max(v=>v.y);var c=new Vector3(vs.Average(v=>v.x),bottom,vs.Average(v=>v.z));float rad=Mathf.Max(.3f,vs.Max(v=>new Vector2(v.x-c.x,v.z-c.z).magnitude));int n=0;
     for(int a=0;a<24&&n<3;a+=1){var dv=Quaternion.Euler(0,a*15,0)*Vector3.forward;var q=c+dv*(rad+1.1f);if(!UnityEngine.AI.NavMesh.SamplePosition(q,out var h,.35f,UnityEngine.AI.NavMesh.AllAreas)||Mathf.Abs(h.position.y-bottom)>.5f)continue;
      var pos=h.position+Vector3.up*.18f;cam.transform.position=pos;cam.transform.LookAt(c+Vector3.up*.08f);Shoot(cam,Path.Combine(dir,$"room-p{pi}-{id}-{n}low.jpg"),640,480);
      cam.transform.position=h.position+Vector3.up*1.6f;cam.transform.LookAt(c+Vector3.up*(top-bottom)*.4f);Shoot(cam,Path.Combine(dir,$"room-p{pi}-{id}-{n}eye.jpg"),640,480);n++;a+=5;}
     // and from above on the side of the house's middle, looking down past it at the floor round its base
     {var inw=p.InteriorBounds.center-c;inw.y=0;inw=inw.sqrMagnitude<1e-4f?Vector3.forward:inw.normalized;cam.transform.position=c+inw*(rad+.55f)+Vector3.up*(top-bottom+.5f);cam.transform.LookAt(c);Shoot(cam,Path.Combine(dir,$"room-p{pi}-{id}-above.jpg"),640,480);
      cam.transform.position=c+inw*(rad+.9f)+Vector3.up*.35f;cam.transform.LookAt(c+Vector3.up*.05f);Shoot(cam,Path.Combine(dir,$"room-p{pi}-{id}-front.jpg"),640,480);}}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);nav.Remove();}}
  // V28: Bell's stacked box (living-box-3) had a third of its base on the box under it - set it squarely on that box
  public static void Box28Fix(){Open();Physics.SyncTransforms();var p=county.Properties.First(x=>x.Index==4);var roots=PropRoots(p).ToList();
   var top=roots.First(x=>IdOf(x)=="living-box-3");var under=roots.First(x=>IdOf(x)=="living-box-1");
   var u=DrawnVerts(under,6000).ToList();var b=DrawnVerts(top,6000).ToList();float uTop=u.Max(v=>v.y),bBottom=b.Min(v=>v.y);
   var uc=new Vector3((u.Min(v=>v.x)+u.Max(v=>v.x))*.5f,0,(u.Min(v=>v.z)+u.Max(v=>v.z))*.5f);var bc=new Vector3((b.Min(v=>v.x)+b.Max(v=>v.x))*.5f,0,(b.Min(v=>v.z)+b.Max(v=>v.z))*.5f);
   var move=new Vector3(uc.x-bc.x,uTop-bBottom,uc.z-bc.z);var flat=new Vector3(move.x,0,move.z);if(flat.magnitude>.04f)flat-=flat.normalized*.04f;move=new Vector3(flat.x,move.y,flat.z);
   Undo.RecordObject(top,"box");top.position+=move;log.AppendLine($"Box28Fix: living-box-3 moved {new Vector2(move.x,move.z).magnitude*100:F1} cm across, {move.y*100:F1} cm up, onto living-box-1");Save("Box28Fix");}
 }
}
