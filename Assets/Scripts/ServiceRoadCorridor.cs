using System.Collections.Generic;
using UnityEngine;
namespace ServiceGameV2 {
 // V22: where the county car may go. Playtest: "the car can come onto the lawn", "the car can go anywhere". The car keeps
 // to the county road (and County Route 9 on the night it is open), the depot yard, and the first few metres of each
 // drive - the pull-off by the mailbox where you park and walk up. Leaving that corridor acts like a soft kerb in
 // ServicePlayer.Drive: the car scrapes along it and slows, it never crashes, and moving back in is always allowed.
 // Widths are the editor road stages' own (ServiceV19Roads MainHalf/LateHalf, ServiceV21Roads ApronHalf).
 public sealed class ServiceRoadCorridor {
  public const float MainHalf=3.1f,LateHalf=2.6f,RoadMargin=.9f,DriveMargin=.25f,DriveReach=9f,DriveStandOff=2.5f;
  static float ApronHalf(float s)=>(1.55f+1.1f*Mathf.Exp(-Mathf.Max(s,0)/1.7f))*1.18f;
  struct Piece{public Vector2[] p;public float[] s;public int kind;public int property;}
  readonly CountyScene scene;readonly List<Piece> pieces=new List<Piece>();
  public int LastKind {get;private set;}=-1; public int LastProperty {get;private set;}=-1; public bool LastAtDriveEnd {get;private set;}
  public ServiceRoadCorridor(CountyScene s){
   scene=s;
   var road=new List<Vector3>();if(s.Route!=null)foreach(var t in s.Route)if(t)road.Add(t.position);
   if(s.LateRoute!=null&&s.LateRoute.Length>0&&s.LateRoute[0])road.Add(s.LateRoute[0].position); // the last stretch to the road end
   if(road.Count>1)pieces.Add(Make(road,0,-1,null));
   var late=new List<Vector3>();if(s.LateRoute!=null)foreach(var t in s.LateRoute)if(t)late.Add(t.position);
   if(late.Count>1)pieces.Add(Make(late,1,-1,null));
   if(s.Properties!=null)foreach(var p in s.Properties){if(!p||p.ApproachRoute==null||p.ApproachRoute.Length<2)continue;
    var along=p.Index==2&&late.Count>1?late:road;var a0=p.ApproachRoute[0];var mouth=Closest(along,a0);
    var pts=new List<Vector3>{mouth};var sv=new List<float>{3-Flat(a0-mouth).magnitude};float acc=3;
    pts.Add(a0);sv.Add(acc);
    // V23: the whole drive is driveable now, up to the parking end by the house ("roads stop and start abruptly" - the
    // car used to be held 9 m up a drive that plainly went on); it stops short of the steps.
    float total=3;for(int i=1;i<p.ApproachRoute.Length;i++)total+=Flat(p.ApproachRoute[i]-p.ApproachRoute[i-1]).magnitude;float reach=p.DriveLength>0?p.DriveLength:Mathf.Max(DriveReach,total-DriveStandOff);
    for(int i=1;i<p.ApproachRoute.Length;i++){float seg=Flat(p.ApproachRoute[i]-p.ApproachRoute[i-1]).magnitude;
     if(acc+seg>=reach){float k=(reach-acc)/Mathf.Max(seg,1e-4f);pts.Add(Vector3.Lerp(p.ApproachRoute[i-1],p.ApproachRoute[i],k));sv.Add(reach);break;}
     acc+=seg;pts.Add(p.ApproachRoute[i]);sv.Add(acc);}
    pieces.Add(Make(pts,2,p.Index,sv));}
  }
  static Vector2 Flat(Vector3 v)=>new Vector2(v.x,v.z);
  static Piece Make(List<Vector3> pts,int kind,int property,List<float> s){
   var piece=new Piece{p=new Vector2[pts.Count],s=new float[pts.Count],kind=kind,property=property};float acc=0;
   for(int i=0;i<pts.Count;i++){piece.p[i]=Flat(pts[i]);if(s!=null)piece.s[i]=s[i];else{if(i>0)acc+=Vector2.Distance(piece.p[i],piece.p[i-1]);piece.s[i]=acc;}}
   return piece;}
  static Vector3 Closest(List<Vector3> line,Vector3 q){var best=line[0];float bd=float.MaxValue;
   for(int i=1;i<line.Count;i++){var a=line[i-1];var b=line[i];var ab=b-a;ab.y=0;float t=Mathf.Clamp01(Vector3.Dot(q-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-6f));var c=a+(b-a)*t;var dd=Flat(q-c).sqrMagnitude;if(dd<bd){bd=dd;best=c;}}
   return best;}
  bool Active(Piece c)=>c.kind==1||(c.kind==2&&c.property==2)?scene.LateRoad&&scene.LateRoad.activeInHierarchy:true;
  float Allowed(Piece c,float s)=>c.kind==0?MainHalf+RoadMargin:c.kind==1?LateHalf+RoadMargin:ApronHalf(s)+DriveMargin;
  // Distance beyond the drivable corridor at a point (zero or less: inside), and which way is out.
  public float Excess(Vector3 at,out Vector3 outward){
   var q=Flat(at);float best=float.MaxValue;Vector2 dir=Vector2.zero;
   // the depot yard (gravel x -10.5..8.5, z -12..9 about the depot marker, plus half a metre)
   if(scene.Depot){var l=scene.Depot.InverseTransformPoint(at);float ex=Mathf.Max(-11-l.x,l.x-9,0),ez=Mathf.Max(-12.5f-l.z,l.z-9.5f,0);float e=new Vector2(ex,ez).magnitude;
    if(ex==0&&ez==0)e=-Mathf.Min(l.x+11,9-l.x,l.z+12.5f,9.5f-l.z);
    if(e<best){best=e;var o=new Vector3(l.x<-11?-1:l.x>9?1:0,0,l.z<-12.5f?-1:l.z>9.5f?1:0);var w=scene.Depot.TransformDirection(o);dir=new Vector2(w.x,w.z);LastKind=3;LastProperty=-1;LastAtDriveEnd=false;}}
   foreach(var c in pieces){if(!Active(c))continue;
    for(int i=1;i<c.p.Length;i++){var a=c.p[i-1];var b=c.p[i];var ab=b-a;float len2=ab.sqrMagnitude;if(len2<1e-6f)continue;float t=Mathf.Clamp01(Vector2.Dot(q-a,ab)/len2);var cp=a+ab*t;var off=q-cp;float dist=off.magnitude;
     float sAt=Mathf.Lerp(c.s[i-1],c.s[i],t);float e=dist-Allowed(c,sAt);if(e<best){best=e;dir=dist>1e-4f?off/dist:Vector2.zero;LastKind=c.kind;LastProperty=c.property;LastAtDriveEnd=c.kind==2&&sAt>=c.s[c.s.Length-1]-1f;}}}
   outward=new Vector3(dir.x,0,dir.y);if(outward.sqrMagnitude>1e-6f)outward.Normalize();
   return best;
  }
  public float Excess(Vector3 at)=>Excess(at,out _);
 }
}
