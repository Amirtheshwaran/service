using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 static class V27Ext{public static float Dot(this Vector3 e,Vector3 n)=>Mathf.Abs(e.x*n.x)+Mathf.Abs(e.y*n.y)+Mathf.Abs(e.z*n.z);}
 // V27 redo - fixes from the second look at the reviewers' list.
 public static partial class ServiceV19Rebuild {
  // "The third house door opens the wrong way": 77 Latigo's (Vale's) leaf was hung on the same side as its handle, so it
  // swung open from the handle edge. The hinge goes to the other jamb (the leaf stays where it is when shut) and the
  // swing is chosen again against the real colliders: into the house, clear of the wall and the hall bench.
  public static void ValeHinge27(){Open();Physics.SyncTransforms();var p=county.Properties.First(x=>x.Index==1);var hp=p.DoorPanel;
   var rs=hp.GetComponentsInChildren<MeshRenderer>(true).Where(r=>{var n=r.name.ToLowerInvariant();return !n.Contains("paper")&&!n.Contains("tape")&&!n.Contains("note");}).ToList();
   float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
   foreach(var r in rs){var mf=r.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;var b=mf.sharedMesh.bounds;
    for(int i=0;i<8;i++){var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));var l=hp.InverseTransformPoint(r.transform.TransformPoint(corner));
     minX=Mathf.Min(minX,l.x);maxX=Mathf.Max(maxX,l.x);minZ=Mathf.Min(minZ,l.z);maxZ=Mathf.Max(maxZ,l.z);}}
   bool alongX=maxX-minX>=maxZ-minZ;float far=alongX?(Mathf.Abs(maxX)>Mathf.Abs(minX)?maxX:minX):(Mathf.Abs(maxZ)>Mathf.Abs(minZ)?maxZ:minZ);
   float near=alongX?(Mathf.Abs(maxX)>Mathf.Abs(minX)?minX:maxX):(Mathf.Abs(maxZ)>Mathf.Abs(minZ)?minZ:maxZ);
   log.AppendLine($"VALE HINGE leaf in hinge space x {minX:F3}..{maxX:F3} z {minZ:F3}..{maxZ:F3}; width axis {(alongX?"x":"z")}, hinge edge {near:F3}, latch edge {far:F3}; swing was {p.DoorSwing}");
   if(Mathf.Abs(near)>.12f){log.AppendLine("VALE HINGE the pivot is not at a leaf edge - left alone");Save("ValeHinge27");return;}
   // the new pivot: the latch edge (as far past it as the old pivot sat past the hinge edge)
   var local=alongX?new Vector3(far+near,0,0):new Vector3(0,0,far+near);var newPos=hp.TransformPoint(local);newPos.y=hp.position.y;
   var kids=new List<(Transform t,Vector3 pos,Quaternion rot)>();foreach(Transform k in hp)kids.Add((k,k.position,k.rotation));
   var oldPos=hp.position;hp.position=newPos;foreach(var (t,pos,rot) in kids){t.position=pos;t.rotation=rot;}
   Physics.SyncTransforms();log.AppendLine($"VALE HINGE pivot {oldPos:F3} -> {newPos:F3} ({Vector3.Distance(oldPos,newPos):F2} m)");
   // swing: both ways at 70-115 degrees; into the house, fewest contacts, then nearest 95
   var (leaf,half)=LeafBox(p);var rest=hp.localRotation;var o=OutwardOf(p);var cands=new List<(float sw,int hits,float inward)>();
   if(leaf){foreach(float sign in new[]{1f,-1f})for(float a=70;a<=115;a+=5){float sw=sign*a;hp.localRotation=rest*Quaternion.Euler(0,sw,0);Physics.SyncTransforms();int hits=LeafHits(p,leaf,half,out var centre);float inward=-Vector3.Dot(new Vector3(centre.x-p.Door.position.x,0,centre.z-p.Door.position.z),o);cands.Add((sw,hits,inward));}}
   hp.localRotation=rest;Physics.SyncTransforms();var inside=cands.Where(c=>c.inward>.15f).ToList();
   var best=inside.OrderBy(c=>c.hits).ThenBy(c=>Mathf.Abs(Mathf.Abs(c.sw)-95)).FirstOrDefault();
   log.AppendLine("VALE HINGE swings "+string.Join(" ",cands.Select(c=>$"{c.sw:+0;-0}:{c.hits}/{c.inward:F2}"))+$" -> {(inside.Count>0?best.sw.ToString():"none inward!")}");
   if(inside.Count>0)p.DoorSwing=best.sw;EditorUtility.SetDirty(p);EditorUtility.SetDirty(hp);
   ServiceBuild.RebakeOpenDoors(county);Save("ValeHinge27");}
  // every piece hung on a wall (frames, mirrors, sconces, the wall clock...): the wall behind it must be there across its
  // whole back, flat (no brace or post across it, no opening), and it must sit flush - its back on the wall's most
  // proud point, 2 mm off. A piece that fails moves to the nearest place on the same wall that passes (and touches no
  // other prop). WallFix27 does it; WallCheck27 only reports.
  public static void WallCheck27(){WallFix(false);}
  public static void WallFix27(){WallFix(true);}
  static void WallFix(bool fix){Open();Physics.SyncTransforms();var sb=new StringBuilder();int n=0,bad=0,moved=0;
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var vowner=new Dictionary<Collider,Transform>();var vis=Visuals(p,new List<Transform>(),vowner);try{
    foreach(var t in roots){var kind=KindOf(IdOf(t),t.name,lay);if(kind!="wall")continue;if(!OOB(t,out var c,out _,out _,out var rs))continue;n++;var own=new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));
     var bb=rs.Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});var thin=bb.size.x<bb.size.z?Vector3.right:Vector3.forward;
     // the wall: the building face nearest its middle across the thin axis
     Vector3 nrm=Vector3.zero;float best=99;foreach(var sg in new[]{1f,-1f}){foreach(var h in Physics.RaycastAll(c+thin*sg*.6f,-thin*sg,1.2f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(Vector3.Dot(h.normal,thin*sg)>.7f){float dd=Mathf.Abs(Vector3.Dot(h.point-c,thin));if(dd<best){best=dd;nrm=h.normal;}}break;}}
     if(nrm==Vector3.zero){sb.AppendLine($"p{p.Index} {IdOf(t)}: no wall found behind it");bad++;continue;}nrm=Vector3.Dot(nrm,thin)>0?thin:-thin;
     var along=Vector3.Cross(Vector3.up,nrm).normalized;float hw=Vector3.Dot(bb.extents,new Vector3(Mathf.Abs(along.x),0,Mathf.Abs(along.z)))*.9f,hh=bb.extents.y*.9f;float backOff=Vector3.Dot(c-bb.center,nrm)-Vector3.Dot(bb.extents,new Vector3(Mathf.Abs(nrm.x),0,Mathf.Abs(nrm.z)));
     var others=roots.Where(o=>o!=t).SelectMany(o=>o.GetComponentsInChildren<Renderer>(false)).Where(r=>r.enabled).Select(r=>r.bounds).ToList();
     // a place on the wall: (along, up) from where it is. Returns the distance from 0.5 m out to the wall's proudest point, or NaN
     float Test(float a,float b,out float spread){spread=0;var q=bb.center+along*a+Vector3.up*b;float mn=99,mx=-99;
      for(int i=-2;i<=2;i++)for(int j=-2;j<=2;j++){var o=q+nrm*.5f+along*(hw*i/2f)+Vector3.up*(hh*j/2f);float hit=float.NaN;
       foreach(var h in Physics.RaycastAll(o,-nrm,.9f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){hit=h.distance;break;}
       if(float.IsNaN(hit))return float.NaN;mn=Mathf.Min(mn,hit);mx=Mathf.Max(mx,hit);}
      spread=mx-mn;var box=new Bounds(q+nrm*(.5f-mn+bb.extents.Dot(nrm)),bb.size);foreach(var ob in others)if(ob.Intersects(box))return float.NaN;return mn;}
     float here=Test(0,0,out var sp0);float depthNow=float.IsNaN(here)?99:(.5f-here)-(Vector3.Dot(bb.center-c,nrm)+0)-Vector3.Dot(c-bb.center,nrm);
     float backNow=Vector3.Dot(bb.center-(bb.center),nrm)-Vector3.Dot(bb.extents,new Vector3(Mathf.Abs(nrm.x),0,Mathf.Abs(nrm.z)));
     float gap=float.IsNaN(here)?99:Vector3.Dot((bb.center-bb.extents.Dot(nrm)*nrm)-(bb.center+nrm*(.5f-here)),nrm);
     // fittings on a small base (lamps, sconces, clocks, telephones): only the middle of the back has to be on the wall
     var nm=t.name.ToLowerInvariant();bool fitting=nm.Contains("lamp")||nm.Contains("sconce")||nm.Contains("clock")||nm.Contains("telephone");
     if(fitting){float mid=float.NaN;foreach(var h in Physics.RaycastAll(bb.center+nrm*.5f,-nrm,.9f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){mid=h.distance;break;}here=mid;sp0=0;gap=float.IsNaN(mid)?99:Vector3.Dot((bb.center-bb.extents.Dot(nrm)*nrm)-(bb.center+nrm*(.5f-mid)),nrm);}
     bool ok=!float.IsNaN(here)&&sp0<=.05f&&gap>=-.012f&&gap<=(fitting?.025f:.015f);
     sb.AppendLine($"p{p.Index} {IdOf(t)} ({t.name.Substring(Mathf.Min(t.name.Length,IdOf(t).Length)).Trim(' ','—')}): {(float.IsNaN(here)?"wall missing or a prop in the way":$"wall spread {sp0:F3}, back {gap*100:F1} cm off the wall")} {(ok?"ok":"FAILS")}");
     if(ok)continue;bad++;if(!fix)continue;
     (float s,float a,float b,float mn) pick=(99,0,0,0);int nanN=0,sprN=0;for(float a=-1.6f;a<=1.6f;a+=.025f)for(float b=-.5f;b<=.8f;b+=.025f){float s=new Vector2(a,b).magnitude;if(s>=pick.s)continue;float mn=Test(a,b,out var spr);if(float.IsNaN(mn)){nanN++;continue;}if(spr>.05f){sprN++;continue;}pick=(s,a,b,mn);}
     sb.AppendLine($"   search: {nanN} places with no wall or a prop in the way, {sprN} not flat");
     if(pick.s>=99){sb.AppendLine("   nowhere clean within 0.9 m - left for a hand fix");continue;}
     var q2=bb.center+along*pick.a+Vector3.up*pick.b;var wallPt=q2+nrm*(.5f-pick.mn);var want=wallPt+nrm*(bb.extents.Dot(nrm)+.002f);var delta=want-bb.center;t.position+=delta;Physics.SyncTransforms();moved++;
     sb.AppendLine($"   moved {delta:F3} ({pick.a:+0.000;-0.000} along, {pick.b:+0.000;-0.000} up, flush)");}}finally{Unvisual(vis);}}
   sb.Insert(0,$"Wall{(fix?"Fix":"Check")}27: {n} wall pieces, {bad} failing, {moved} moved"+System.Environment.NewLine);File.WriteAllText(Path.Combine(Work,"Audit","placement27",fix?"wallfix27.txt":"wallcheck27.txt"),sb.ToString());if(fix){log.Append(sb.ToString());Save("WallFix27");}}
  // what is drawn, as something to hit: a temporary mesh collider for every visible mesh of a house (layer 31) and of
  // each of its props (layer 30). Colliders are not always where the drawing is (a wall's box 6 cm proud of its paper);
  // these are. Always taken away again before anything is saved.
  const int VisHouse=31,VisProp=30;const int VisMask=(1<<VisHouse)|(1<<VisProp);
  static List<GameObject> Visuals(ServiceProperty p,List<Transform> props,Dictionary<Collider,Transform> owner){var made=new List<GameObject>();
   void Add(MeshRenderer r,int layer,Transform who){if(!r.enabled||!r.gameObject.activeInHierarchy)return;var mf=r.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)return;var g=new GameObject("V27 visual probe");g.layer=layer;g.transform.SetParent(r.transform,false);var mc=g.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;owner[mc]=who;made.Add(g);}
   // skinned (rigged) meshes: baked as they are posed, at their true size (their own bounds are loose - a rigged stove's
   // box stood 10 cm over its burners)
   void AddSkinned(SkinnedMeshRenderer r,int layer,Transform who){if(!r.enabled||!r.gameObject.activeInHierarchy||!r.sharedMesh)return;var m=new Mesh();r.BakeMesh(m);var g=new GameObject("V27 visual probe");g.layer=layer;g.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);g.transform.SetParent(r.transform,true);var mc=g.AddComponent<MeshCollider>();mc.sharedMesh=m;owner[mc]=who;made.Add(g);}
   foreach(var r in p.Building.GetComponentsInChildren<MeshRenderer>(false))Add(r,VisHouse,p.Building);foreach(var r in p.Building.GetComponentsInChildren<SkinnedMeshRenderer>(false))AddSkinned(r,VisHouse,p.Building);
   foreach(var t in props){foreach(var r in t.GetComponentsInChildren<MeshRenderer>(false))Add(r,VisProp,t);foreach(var r in t.GetComponentsInChildren<SkinnedMeshRenderer>(false))AddSkinned(r,VisProp,t);}Physics.SyncTransforms();return made;}
  // a prop's drawn vertices in the world, skinned ones as posed
  static IEnumerable<Vector3> DrawnVerts(Transform t,int cap=3000){foreach(var mf in t.GetComponentsInChildren<MeshFilter>(false)){var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mr.enabled||!mf.sharedMesh)continue;var vs=mf.sharedMesh.vertices;int step=Mathf.Max(1,vs.Length/cap);for(int i=0;i<vs.Length;i+=step)yield return mf.transform.TransformPoint(vs[i]);}
   foreach(var sr in t.GetComponentsInChildren<SkinnedMeshRenderer>(false)){if(!sr.enabled||!sr.sharedMesh)continue;var m=new Mesh();sr.BakeMesh(m);var vs=m.vertices;var M=Matrix4x4.TRS(sr.transform.position,sr.transform.rotation,Vector3.one);int step=Mathf.Max(1,vs.Length/cap);for(int i=0;i<vs.Length;i+=step)yield return M.MultiplyPoint3x4(vs[i]);}}
  static void Unvisual(List<GameObject> made){foreach(var g in made)if(g)Object.DestroyImmediate(g);made.Clear();Physics.SyncTransforms();}
  // V27 redo, the strictest check: every vertex of every prop against the drawn house and the drawn props around it
  public static void Touch27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();int props=0,verts=0,badS=0,badP=0;var found=new List<(ServiceProperty p,Transform t,string id,Vector3 at,Vector3 n,float d,string what)>();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var byId=new Dictionary<string,Transform>();foreach(var t in roots)byId[IdOf(t)]=t;
    var owner=new Dictionary<Collider,Transform>();var made=Visuals(p,roots,owner);
    try{foreach(var t in roots){var n=t.name.ToLowerInvariant();if(n.Contains("fire")||n.Contains("blood")||n.Contains("apparition")||FlatWords.Any(w=>n.Contains(w)))continue;if(!OOB(t,out var c,out _,out _,out var rs))continue;props++;
      var kind=KindOf(IdOf(t),t.name,lay);float bottom=rs.Min(x=>x.bounds.min.y);bool standing=kind=="floor"||kind=="top";
      Transform support=lay.TryGetValue(IdOf(t),out var li)&&!string.IsNullOrEmpty(li.on)&&byId.TryGetValue(li.on,out var sp)?sp:null;
      float worstS=0,worstP=0;string whatS="",whatP="";int nS=0,nP=0;Vector3 atS=c,nS3=Vector3.forward,atP=c,nP3=Vector3.forward;
      foreach(var mf in t.GetComponentsInChildren<MeshFilter>(false)){var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mr.enabled||!mf.sharedMesh)continue;var vs=mf.sharedMesh.vertices;int step=Mathf.Max(1,vs.Length/2500);
       for(int i=0;i<vs.Length;i+=step){var v=mf.transform.TransformPoint(vs[i]);if(standing&&v.y<bottom+.025f)continue;verts++;var o=new Vector3(c.x,v.y,c.z);var dv=v-o;float len=dv.magnitude;if(len<.02f)continue;
        foreach(var h in Physics.RaycastAll(o,dv/len,len,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(h.collider,out var who)||who==t)continue;
         float depth=Vector3.Dot(v-h.point,-h.normal);if(depth<=.01f)break;
         if(who==p.Building){nS++;if(depth>worstS){worstS=depth;whatS=h.collider.transform.parent?h.collider.transform.parent.name:"";atS=h.point;nS3=h.normal;}}
         else{if(who==support||(support==null&&byId.TryGetValue(IdOf(who),out _)&&lay.TryGetValue(IdOf(who),out var wl)&&wl.on==IdOf(t)))break;nP++;if(depth>worstP){worstP=depth;whatP=IdOf(who);atP=h.point;nP3=h.normal;}}
         break;}}}
      if(nS>0&&kind!="ceiling"){badS++;sb.AppendLine($"p{p.Index} [{kind}] {IdOf(t)}: {nS} vertices through the house, deepest {worstS:F3} m ({whatS})");found.Add((p,t,IdOf(t),atS,nS3,worstS,whatS));}
      if(nP>0){badP++;sb.AppendLine($"p{p.Index} [{kind}] {IdOf(t)}: {nP} vertices into {whatP}, deepest {worstP:F3} m");found.Add((p,t,IdOf(t)+"-x-"+whatP,atP,nP3,worstP,whatP));}}}
    finally{Unvisual(made);}}
   sb.Insert(0,$"Touch27: {props} props, {verts} vertices against the drawn house and props; {badS} through the house (ceiling mounts not counted), {badP} into another prop"+System.Environment.NewLine);
   var dir=Path.Combine(Work,"Audit","placement27","touch");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);File.WriteAllText(Path.Combine(Work,"Audit","placement27","touch27.txt"),sb.ToString());
   if(UnityEngine.SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=60;cam.aspect=4f/3f;cam.nearClipPlane=.02f;
   try{foreach(var (p,t,id,at,nrm,d,what) in found){var nn=nrm;var side=Vector3.Cross(Vector3.up,nn);if(side.sqrMagnitude<.01f)side=Vector3.right;side.Normalize();var pos=at+nn*.9f+side*.55f+Vector3.up*.3f;
     if(Physics.Raycast(at+nn*.05f,(pos-at).normalized,out var hh,(pos-at).magnitude,~0,QueryTriggerInteraction.Ignore))pos=hh.point-(pos-at).normalized*.08f;cam.transform.position=pos;cam.transform.LookAt(at);Shoot(cam,Path.Combine(dir,$"p{p.Index}-{id}.jpg"),480,360);}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}}
  // the drawn surface behind a wall piece, across its width and height: where the wall is, how far back
  public static void WallProfile27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var (pi,id) in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1]))){
    var p=county.Properties.First(x=>x.Index==pi);var t=PropRoots(p).First(x=>IdOf(x)==id);OOB(t,out var c,out _,out _,out var rs);var bb=rs.Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});var thin=bb.size.x<bb.size.z?Vector3.right:Vector3.forward;
    var vo=new Dictionary<Collider,Transform>();var vis=Visuals(p,new List<Transform>(),vo);
    try{foreach(var sg in new[]{1f,-1f}){var nrm=-thin*sg;var along=Vector3.Cross(Vector3.up,nrm).normalized;sb.AppendLine($"p{pi} {id} centre {bb.center:F3} looking along {-nrm} (from the {(sg>0?"+":"-")} side); piece spans {Vector3.Dot(bb.min,thin):F3}..{Vector3.Dot(bb.max,thin):F3}");
      foreach(float up in new[]{-.3f,0f,.3f}){var line=new StringBuilder();for(float a=-.8f;a<=.8f;a+=.04f){var o=bb.center+nrm*.6f+along*a+Vector3.up*up;string cell="  --  ";foreach(var h in Physics.RaycastAll(o,-nrm,1.6f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){cell=$"{Vector3.Dot(h.point,thin):F2}";break;}line.Append(cell.PadLeft(7));}
       sb.AppendLine($"  up {up:+0.0;-0.0}: "+line);}
      sb.AppendLine("  along: "+string.Join("",Enumerable.Range(0,41).Select(k=>(-.8f+k*.04f).ToString("+0.00;-0.00").PadLeft(7))));}}
    finally{Unvisual(vis);}}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","wallprofile27.txt"),sb.ToString());}
  // Vale's vestibule mirror hung half off the pilaster it is on, 11 cm in front of it: centred on the pilaster's face,
  // a little smaller so it fits with 2 cm each side, and flush
  public static void VestMirror27(){Open();Physics.SyncTransforms();var p=county.Properties.First(x=>x.Index==1);var t=PropRoots(p).First(x=>IdOf(x)=="vest-mirror");
   OOB(t,out _,out _,out _,out var rs);var bb=rs.Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});var vo=new Dictionary<Collider,Transform>();var vis=Visuals(p,new List<Transform>(),vo);
   try{var nrm=-Vector3.forward;var along=Vector3.Cross(Vector3.up,nrm).normalized;float face=float.NaN;var ranges=new List<(float a,float b)>();
    {foreach(var h in Physics.RaycastAll(bb.center+nrm*.6f+along*.16f,-nrm,1.6f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){face=Vector3.Dot(h.point,-nrm);break;}}
    foreach(float up in new[]{-.3f,0f,.3f}){float a0=float.NaN,a1=float.NaN;for(float a=-.3f;a<=.7f;a+=.005f){var o=bb.center+nrm*.6f+along*a+Vector3.up*up;bool on=false;foreach(var h in Physics.RaycastAll(o,-nrm,1.6f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){on=Mathf.Abs(Vector3.Dot(h.point,-nrm)-face)<.008f;break;}
      if(on&&float.IsNaN(a0))a0=a;if(on)a1=a;else if(!float.IsNaN(a0)&&!float.IsNaN(a1)&&a>a1+.02f)break;}ranges.Add((a0,a1));}
    float lo=ranges.Max(r=>r.a),hi=ranges.Min(r=>r.b);float width=hi-lo,mid=(lo+hi)*.5f;float wNow=Vector3.Dot(bb.size,new Vector3(Mathf.Abs(along.x),0,Mathf.Abs(along.z)));float k=Mathf.Min(1f,(width-.04f)/wNow);
    log.AppendLine($"VEST MIRROR pilaster face at {face:F3} from {lo:+0.000;-0.000} to {hi:+0.000;-0.000} along ({width:F3} m wide); mirror {wNow:F3} wide -> scale {k:F3}, centre at {mid:+0.000;-0.000}");
    if(k>.6f&&!float.IsNaN(face)){var c0=bb.center;t.localScale*=k;Physics.SyncTransforms();OOB(t,out _,out _,out _,out rs);bb=rs.Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
     var wantCentre=c0+along*mid;var back=Vector3.Dot(bb.center,-nrm)+Vector3.Dot(bb.extents,new Vector3(0,0,1));var delta=(wantCentre-bb.center);delta-=(-nrm)*Vector3.Dot(delta,-nrm);delta+=(-nrm)*(face-.002f-back);delta.y=c0.y-bb.center.y;t.position+=delta;
     log.AppendLine($"VEST MIRROR moved {delta:F3}, scaled {k:F3}");}}
   finally{Unvisual(vis);}Save("VestMirror27");}
  // hanging lights: the crown meets the ceiling as drawn (Vale's landing lantern hung 15 cm under it). Raised by the gap
  // over the middle of its top; if that would put any of it through the slope, it stays and is reported.
  public static void Ceil27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();int moved=0;
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var vis=Visuals(p,new List<Transform>(),new Dictionary<Collider,Transform>());
    try{foreach(var t in roots){if(KindOf(IdOf(t),t.name,lay)!="ceiling")continue;if(!OOB(t,out var c,out _,out _,out var rs))continue;float top=rs.Max(x=>x.bounds.max.y);
      float gap=float.NaN;foreach(var h in Physics.RaycastAll(new Vector3(c.x,top-.05f,c.z),Vector3.up,4f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){gap=h.point.y-top;break;}
      sb.AppendLine($"p{p.Index} {IdOf(t)} ({t.name.Substring(Mathf.Min(t.name.Length,IdOf(t).Length)).Trim(' ','—')}): crown {(float.IsNaN(gap)?"no ceiling over it":(gap*100).ToString("F1")+" cm under the ceiling")}");
      if(float.IsNaN(gap)||gap<=.012f)continue;var lights=t.GetComponentsInChildren<Light>(true).Length;var near=p.GetComponentsInChildren<Light>(true).Where(l=>!l.transform.IsChildOf(t)&&Vector3.Distance(l.transform.position,c)<.9f).ToList();
      var move=Vector3.up*(gap-.004f);t.position+=move;foreach(var l in near)l.transform.position+=move;Physics.SyncTransforms();
      // nothing of it through the drawn house now
      float worst=0;foreach(var mf in t.GetComponentsInChildren<MeshFilter>(false)){if(!mf.sharedMesh)continue;var vs=mf.sharedMesh.vertices;int step=Mathf.Max(1,vs.Length/2500);for(int i=0;i<vs.Length;i+=step){var v=mf.transform.TransformPoint(vs[i]);var o=new Vector3(c.x,v.y,c.z);var dv=v-o;float len=dv.magnitude;if(len<.02f)continue;if(Physics.Raycast(o,dv/len,out var h,len,1<<VisHouse,QueryTriggerInteraction.Collide)){float dd=Vector3.Dot(v-h.point,-h.normal);if(dd>worst)worst=dd;}}}
      if(worst>.01f){t.position-=move;Physics.SyncTransforms();
       // somewhere close by where it can hang from the ceiling with nothing through it
       float Worst(){float w=0;foreach(var mf in t.GetComponentsInChildren<MeshFilter>(false)){if(!mf.sharedMesh)continue;var vs=mf.sharedMesh.vertices;int step=Mathf.Max(1,vs.Length/1500);OOB(t,out var cc,out _,out _,out _);for(int i=0;i<vs.Length;i+=step){var v=mf.transform.TransformPoint(vs[i]);var o=new Vector3(cc.x,v.y,cc.z);var dv=v-o;float len=dv.magnitude;if(len<.02f)continue;if(Physics.Raycast(o,dv/len,out var h,len,1<<VisHouse,QueryTriggerInteraction.Collide)){float dd=Vector3.Dot(v-h.point,-h.normal);if(dd>w)w=dd;}}}return w;}
       var start=t.position;(float s,Vector3 pos) pick=(99,start);
       for(float dx=-.6f;dx<=.6f;dx+=.05f)for(float dz=-.6f;dz<=.6f;dz+=.05f){float sc=new Vector2(dx,dz).magnitude;if(sc>=pick.s)continue;t.position=start+new Vector3(dx,0,dz);Physics.SyncTransforms();OOB(t,out var c2,out _,out _,out var rs2);float top2=rs2.Max(x=>x.bounds.max.y);float g2=float.NaN;
        foreach(var h in Physics.RaycastAll(new Vector3(c2.x,top2-.05f,c2.z),Vector3.up,4f,1<<VisHouse,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){g2=h.point.y-top2;break;}if(float.IsNaN(g2))continue;t.position+=Vector3.up*(g2-.004f);Physics.SyncTransforms();if(Worst()<=.01f)pick=(sc,t.position);}
       t.position=pick.pos;Physics.SyncTransforms();var mv=pick.pos-start;foreach(var l in near)l.transform.position+=mv;
       if(pick.s>=99){sb.AppendLine($"   raising it would put {worst:F3} m through the ceiling and nowhere within 0.6 m is clean - left");continue;}
       moved++;sb.AppendLine($"   moved {mv:F3} to hang from the ceiling clear of the slope ({near.Count} light(s) with it)");continue;}
      moved++;sb.AppendLine($"   raised {move.y:F3} m ({near.Count} light(s) with it; {lights} inside it)");}}
    finally{Unvisual(vis);}}
   sb.Insert(0,$"Ceil27: {moved} raised"+System.Environment.NewLine);log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","placement27","ceil27.txt"),sb.ToString());Save("Ceil27");}
  // what is under a prop: every collider and drawn mesh straight down from its middle (SERVICE_LOOK list)
  public static void Under27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var (pi,id) in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1]))){
    var p=county.Properties.First(x=>x.Index==pi);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var t=roots.First(x=>IdOf(x)==id);OOB(t,out var c,out _,out _,out var rs);float bottom=rs.Min(x=>x.bounds.min.y);
    var lay=LayoutIds(pi);sb.AppendLine($"p{pi} {id}: bottom {bottom:F3}, on '{(lay.TryGetValue(id,out var li)?li.on:"")}'");
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var h in Physics.RaycastAll(new Vector3(c.x,bottom+.3f,c.z),Vector3.down,2.5f,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance))sb.AppendLine($"   {bottom-h.point.y:+0.000;-0.000} below: {(owner.TryGetValue(h.collider,out var ow)?"DRAWN "+(ow==p.Building?h.collider.transform.parent.name:IdOf(ow)):"collider "+h.collider.name)}");
     foreach(var r in roots.Where(x=>x!=t).SelectMany(x=>x.GetComponentsInChildren<MeshRenderer>(false)).Where(r=>r.bounds.Intersects(new Bounds(new Vector3(c.x,bottom-.05f,c.z),new Vector3(.3f,.2f,.3f))))){var mf=r.GetComponent<MeshFilter>();sb.AppendLine($"   near: {PathOf(r.transform).Replace("SERVICE — Hollis County/","")} enabled {r.enabled} mesh {(mf&&mf.sharedMesh?mf.sharedMesh.name+" readable "+mf.sharedMesh.isReadable+" verts "+mf.sharedMesh.vertexCount:"none")} probe {r.GetComponentsInChildren<MeshCollider>().Any(m=>m.gameObject.layer==VisProp)}");}}
    finally{Unvisual(vis);}}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","under27.txt"),sb.ToString());}
  // what each standing prop actually rests on: its lowest vertices (leg tips, a base, the side it lies on), each tested
  // straight down against the drawn house and drawn props. It stands if at least three of them (one for something
  // lying on a shelf edge-on is not enough either) are within 1.2 cm of a drawn surface; else it floats by the smallest
  // gap. Renderers of every kind count as drawn (a stove drawn by a skinned or LOD renderer is still a stove).
  public static void Stand27(){Stand(false);}
  public static void Stand27Fix(){Stand(true);}
  static void Stand(bool fixIt){Open();Physics.SyncTransforms();var sb=new StringBuilder();int n=0,bad=0,fixedN=0;var dir=Path.Combine(Work,"Audit","placement27","stand");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var shots=new List<(ServiceProperty p,Transform t,Vector3 c)>();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    // anything drawn without a plain mesh renderer gets a probe too

    var byId=new Dictionary<string,Transform>();foreach(var t in roots)byId[IdOf(t)]=t;var byDeps=new Dictionary<Transform,List<Transform>>();foreach(var t in roots)if(lay.TryGetValue(IdOf(t),out var li)&&!string.IsNullOrEmpty(li.on)&&byId.TryGetValue(li.on,out var sup)){if(!byDeps.ContainsKey(sup))byDeps[sup]=new List<Transform>();byDeps[sup].Add(t);}
    try{foreach(var t in roots){var kind=KindOf(IdOf(t),t.name,lay);if(kind!="floor"&&kind!="top")continue;if(t.name.StartsWith("V27 drive lamp"))continue;var nm=t.name.ToLowerInvariant();if(nm.Contains("fire")||nm.Contains("blood")||nm.Contains("apparition"))continue;if(!OOB(t,out var c,out _,out _,out var rs))continue;n++;
      var allV=DrawnVerts(t,20000).ToList();if(allV.Count==0)continue;float bottom=allV.Min(v=>v.y);var low=allV.Where(v=>v.y<bottom+.012f).ToList();
      if(low.Count==0)continue;var pick=low.Count>80?Enumerable.Range(0,80).Select(i=>low[i*low.Count/80]).ToList():low;int held=0;float minGap=99;
      foreach(var v in pick){foreach(var h in Physics.RaycastAll(v+Vector3.up*.15f,Vector3.down,.75f,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){bool drawn=h.collider is TerrainCollider||(owner.TryGetValue(h.collider,out var ow)&&ow!=t);if(!drawn)continue;float g=h.distance-.15f;minGap=Mathf.Min(minGap,g);if(g<=.012f&&g>=-.1f)held++;break;}}
      bool stands=held>=Mathf.Max(Mathf.Min(3,pick.Count),Mathf.CeilToInt(pick.Count*.3f));if(stands)continue;bad++;
      if(fixIt&&minGap>.012f&&minGap<.6f){
       // straight down by the least room under any part of it, so nothing passes through anything on the way
       float room=99;foreach(var mf in t.GetComponentsInChildren<MeshFilter>(false)){if(!mf.sharedMesh)continue;var vs=mf.sharedMesh.vertices;int step=Mathf.Max(1,vs.Length/3000);for(int i=0;i<vs.Length;i+=step){var v=mf.transform.TransformPoint(vs[i]);foreach(var h in Physics.RaycastAll(v+Vector3.up*.005f,Vector3.down,1f,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){bool drawn=h.collider is TerrainCollider||(owner.TryGetValue(h.collider,out var ow)&&ow!=t&&!(byDeps.TryGetValue(t,out var dl)&&dl.Contains(ow)));if(!drawn)continue;room=Mathf.Min(room,h.distance-.005f);break;}}}
       if(room>.012f&&room<.6f){var drop=Vector3.down*(room-.002f);void Down(Transform x){x.position+=drop;if(byDeps.TryGetValue(x,out var ds))foreach(var y in ds)Down(y);}Down(t);Physics.SyncTransforms();fixedN++;sb.AppendLine($"   set down {room-.002f:F3} m (with {(byDeps.TryGetValue(t,out var dd2)?dd2.Count:0)} thing(s) on it)");}
       else sb.AppendLine($"   room under it {room:F3} m - left");}
      sb.AppendLine($"p{p.Index} [{kind}] {IdOf(t)} ({t.name.Substring(Mathf.Min(t.name.Length,IdOf(t).Length)).Trim(' ','—')}): {held} of {pick.Count} lowest points on something drawn; nearest gap {(minGap>=99?"none within 0.6 m":(minGap*100).ToString("F1")+" cm")}");shots.Add((p,t,c));}}
    finally{Unvisual(vis);}}
   sb.Insert(0,$"Stand27{(fixIt?"Fix":"")}: {n} standing props, {bad} not resting on anything drawn, {fixedN} set down"+System.Environment.NewLine);File.WriteAllText(Path.Combine(Work,"Audit","placement27",fixIt?"stand27fix.txt":"stand27.txt"),sb.ToString());if(fixIt){log.Append(sb.ToString());Save("Stand27Fix");return;}
   if(UnityEngine.SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=50;cam.aspect=4f/3f;cam.nearClipPlane=.02f;
   try{foreach(var (p,t,c) in shots){OOB(t,out var cc,out var e,out _,out _);float r=Mathf.Max(.35f,e.magnitude*2.2f);for(int k=0;k<2;k++){var d=Quaternion.Euler(0,k*90+30,0)*Vector3.forward;var pos=cc+d*r+Vector3.up*.05f;if(Physics.Raycast(cc,d,out var hh,r,~0,QueryTriggerInteraction.Ignore))pos=cc+d*Mathf.Max(.2f,hh.distance-.05f)+Vector3.up*.05f;cam.transform.position=pos;cam.transform.LookAt(cc-Vector3.up*e.y*.6f);Shoot(cam,Path.Combine(dir,$"p{p.Index}-{IdOf(t)}-{k}.jpg"),480,360);}}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}}
  // a map of what is drawn under a prop's footprint (and a margin round it): height of the first drawn surface below
  // its bottom, per 3 cm cell; '.' nothing within 1 m. Its own footprint is marked with capitals.
  public static void Surface27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var (pi,id) in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1]))){
    var p=county.Properties.First(x=>x.Index==pi);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var t=roots.First(x=>IdOf(x)==id);OOB(t,out var c,out var e,out _,out var rs);var bb=rs.Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{sb.AppendLine($"p{pi} {id}: bottom {bb.min.y:F3}, footprint x {bb.min.x:F2}..{bb.max.x:F2} z {bb.min.z:F2}..{bb.max.z:F2}; cells: drop below its bottom in cm (first drawn surface), [..] = under it");
     for(float z=bb.max.z+.15f;z>=bb.min.z-.15f;z-=.03f){var line=new StringBuilder();for(float x=bb.min.x-.15f;x<=bb.max.x+.15f;x+=.03f){string cell="  .";foreach(var h in Physics.RaycastAll(new Vector3(x,bb.max.y+.3f,z),Vector3.down,bb.size.y+1.3f,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(h.collider,out var ow)||ow==t)continue;cell=((int)Mathf.Round((bb.min.y-h.point.y)*100)).ToString().PadLeft(3);break;}
       bool inside=x>=bb.min.x&&x<=bb.max.x&&z>=bb.min.z&&z<=bb.max.z;line.Append(inside?"["+cell.Trim().PadLeft(3)+"]":" "+cell+" ");}sb.AppendLine(line.ToString());}}
    finally{Unvisual(vis);}}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","surface27.txt"),sb.ToString());}
  public static void Dump27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var (pi,id) in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1]))){
    var p=county.Properties.First(x=>x.Index==pi);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var t=roots.FirstOrDefault(x=>IdOf(x)==id);if(!t){sb.AppendLine($"p{pi} {id}: not found");continue;}
    sb.AppendLine($"p{pi} {id}: {PathOf(t)} active {t.gameObject.activeInHierarchy} pos {t.position:F3} scale {t.lossyScale:F3}");
    foreach(var r in t.GetComponentsInChildren<Renderer>(true)){var mf=r.GetComponent<MeshFilter>();sb.AppendLine($"   {r.GetType().Name} {r.name} enabled {r.enabled} active {r.gameObject.activeInHierarchy} bounds {r.bounds.min:F3}..{r.bounds.max:F3} mesh {(mf&&mf.sharedMesh?mf.sharedMesh.name+" v"+mf.sharedMesh.vertexCount+" readable "+mf.sharedMesh.isReadable+" meshbounds "+mf.sharedMesh.bounds.size.ToString("F3"):"-")}");}
    foreach(var c in t.GetComponentsInChildren<Collider>(true))sb.AppendLine($"   collider {c.GetType().Name} {c.name} bounds {c.bounds.min:F3}..{c.bounds.max:F3}");
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);try{foreach(var kv in owner.Where(k=>k.Value==t))sb.AppendLine($"   probe {kv.Key.name} under {kv.Key.transform.parent.name} bounds {kv.Key.bounds.min:F3}..{kv.Key.bounds.max:F3} mesh {((MeshCollider)kv.Key).sharedMesh?.name}");}finally{Unvisual(vis);}}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","dump27.txt"),sb.ToString());}
  // things that should rest on another prop but are not on anything drawn (a pot over a rigged stove's loose box, a torch
  // over an open shelf top, a clock over a nightstand's back rail): onto the support's highest flat drawn surface that
  // takes the thing's own contact points (its base ring, its feet, the side it lies on) and has headroom for it, the
  // place nearest where it was. Checked after: nothing of it through the house or another prop. SERVICE_LOOK limits it.
  public static void Rest27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();int moved=0;var only=(System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).ToList();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var byId=new Dictionary<string,Transform>();foreach(var t in roots)byId[IdOf(t)]=t;
    var deps=new Dictionary<Transform,List<Transform>>();foreach(var t in roots)if(lay.TryGetValue(IdOf(t),out var li)&&!string.IsNullOrEmpty(li.on)&&byId.TryGetValue(li.on,out var sup)){if(!deps.ContainsKey(sup))deps[sup]=new List<Transform>();deps[sup].Add(t);}
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var t in roots){if(only.Count>0&&!only.Contains($"{p.Index}:{IdOf(t)}"))continue;var kind=KindOf(IdOf(t),t.name,lay);if(kind!="top")continue;if(!lay.TryGetValue(IdOf(t),out var tl)||string.IsNullOrEmpty(tl.on)||!byId.TryGetValue(tl.on,out var support))continue;
      var verts=DrawnVerts(t,6000).ToList();if(verts.Count==0)continue;float bottom=verts.Min(v=>v.y),top=verts.Max(v=>v.y);var cxz=new Vector3(verts.Average(v=>v.x),0,verts.Average(v=>v.z));
      var contacts=verts.Where(v=>v.y<bottom+.012f).Select(v=>new Vector3(v.x-cxz.x,0,v.z-cxz.z)).ToList();if(contacts.Count>40)contacts=Enumerable.Range(0,40).Select(i=>contacts[i*contacts.Count/40]).ToList();
      // does it rest now?
      float Drawn(Vector3 at,float from,out Transform who){who=null;foreach(var h in Physics.RaycastAll(new Vector3(at.x,from,at.z),Vector3.down,3f,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(h.collider,out var ow)||ow==t||(deps.TryGetValue(t,out var dl)&&dl.Contains(ow)))continue;who=ow;return h.point.y;}return float.NaN;}
      int held=0;foreach(var o in contacts){float y=Drawn(cxz+o,bottom+.15f,out _);if(!float.IsNaN(y)&&bottom-y<=.012f&&bottom-y>=-.03f)held++;}
      if(held>=Mathf.Max(Mathf.Min(3,contacts.Count),Mathf.CeilToInt(contacts.Count*.3f)))continue;
      OOB(support,out var sc,out var se,out _,out var srs);var sb2=srs.Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});
      (float score,Vector3 at,float y) best=(float.MaxValue,Vector3.zero,0);float bestLevel=float.MinValue;var cands=new List<(Vector3 at,float y)>();
      for(float x=sb2.min.x;x<=sb2.max.x;x+=.02f)for(float z=sb2.min.z;z<=sb2.max.z;z+=.02f){var at=new Vector3(x,0,z);float hi=float.MinValue,lo=float.MaxValue;bool ok=true;
       foreach(var o in contacts){float y=Drawn(at+o,sb2.max.y+.05f,out var who);if(float.IsNaN(y)||who!=support){ok=false;break;}hi=Mathf.Max(hi,y);lo=Mathf.Min(lo,y);if(hi-lo>.01f){ok=false;break;}}
       if(!ok)continue;
       // headroom: nothing drawn over the place for its height (another shelf board, the stove's backsplash)
       float need=top-bottom+.01f;foreach(var o in contacts.Take(12)){var from=new Vector3(at.x+o.x,hi+.004f,at.z+o.z);foreach(var h in Physics.RaycastAll(from,Vector3.up,need,VisMask,QueryTriggerInteraction.Collide)){if(owner.TryGetValue(h.collider,out var ow)&&ow!=t){ok=false;break;}}if(!ok)break;}
       if(ok)cands.Add((at,hi));}
      if(cands.Count==0){sb.AppendLine($"p{p.Index} {IdOf(t)} on {IdOf(support)}: not resting ({held}/{contacts.Count}) and no clean place on it - left");continue;}
      float topLevel=cands.Max(k=>k.y);var pickC=cands.Where(k=>k.y>topLevel-.03f).OrderBy(k=>(k.at-cxz).sqrMagnitude).First();
      var delta=new Vector3(pickC.at.x-cxz.x,pickC.y+.002f-bottom,pickC.at.z-cxz.z);void Mv(Transform x){x.position+=delta;if(deps.TryGetValue(x,out var ds))foreach(var y in ds)Mv(y);}Mv(t);Physics.SyncTransforms();
      // and nothing of it through anything now
      float worst=0;var nv=DrawnVerts(t,3000).ToList();var mid=new Vector3(nv.Average(v=>v.x),0,nv.Average(v=>v.z));float nb=nv.Min(v=>v.y);foreach(var v in nv){if(v.y<nb+.02f)continue;var o2=new Vector3(mid.x,v.y,mid.z);var dv=v-o2;float len=dv.magnitude;if(len<.02f)continue;foreach(var h in Physics.RaycastAll(o2,dv/len,len,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(h.collider,out var ow)||ow==t)continue;worst=Mathf.Max(worst,Vector3.Dot(v-h.point,-h.normal));break;}}
      if(worst>.01f){delta=-delta;Mv(t);Physics.SyncTransforms();sb.AppendLine($"p{p.Index} {IdOf(t)} on {IdOf(support)}: the clean place put {worst:F3} m of it through something - left");continue;}
      moved++;sb.AppendLine($"p{p.Index} {IdOf(t)} on {IdOf(support)}: was resting on {held}/{contacts.Count} points; set {delta:F3} onto the surface at {pickC.y:F3} ({cands.Count} clean places)");}}
    finally{Unvisual(vis);}}
   // the wreck by the last house stands on its tyres: down by the gap under its lowest points (2 cm)
   {var p2=county.Properties.First(x=>x.Index==2);var car=PropRoots(p2).FirstOrDefault(x=>x.name.StartsWith("Prop_Car_A"));if(car&&(only.Count==0||only.Contains("2:Prop_Car_A"))){var vs=DrawnVerts(car,20000).ToList();float b=vs.Min(v=>v.y);float gap=99;foreach(var v in vs.Where(v=>v.y<b+.012f)){foreach(var h in Physics.RaycastAll(v+Vector3.up*.15f,Vector3.down,.5f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(!(h.collider is TerrainCollider))continue;gap=Mathf.Min(gap,h.distance-.15f);break;}}
     if(gap>.008f&&gap<.1f){car.position+=Vector3.down*(gap-.002f);moved++;sb.AppendLine($"p2 Prop_Car_A: down {gap-.002f:F3} m onto its tyres");}else sb.AppendLine($"p2 Prop_Car_A: gap {gap:F3} - left");}}
   sb.Insert(0,$"Rest27: {moved} set onto what holds them"+System.Environment.NewLine);log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","placement27","rest27.txt"),sb.ToString());Save("Rest27");}
  public static void Bake27(){Open();var sb=new StringBuilder();var p=county.Properties.First(x=>x.Index==0);var t=PropRoots(p).First(x=>IdOf(x)=="kitchen-stove");
   foreach(var sr in t.GetComponentsInChildren<SkinnedMeshRenderer>(true)){var a=new Mesh();sr.BakeMesh(a);var b=new Mesh();sr.BakeMesh(b,true);var sm=sr.sharedMesh;
    sb.AppendLine($"{sr.name}: shared {(sm?sm.vertexCount+" readable "+sm.isReadable+" bounds "+sm.bounds.size.ToString("F4"):"none")} bones {sr.bones.Length} root {(sr.rootBone?sr.rootBone.name:"-")} scale {sr.transform.lossyScale:F3}; bake {a.vertexCount} v bounds {a.bounds.center:F3}/{a.bounds.size:F4}; bakeScaled {b.vertexCount} v bounds {b.bounds.center:F3}/{b.bounds.size:F4}; tris {b.triangles.Length/3}");}
   var vs=DrawnVerts(t,20000).ToList();sb.AppendLine($"drawn verts {vs.Count} y {(vs.Count>0?vs.Min(v=>v.y).ToString("F3")+".."+vs.Max(v=>v.y).ToString("F3"):"-")}");
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","bake27.txt"),sb.ToString());}
  // level with the bottom of a prop, from three sides: a gap under it, or a foot through what it stands on, shows
  public static void Base27(){Open();Physics.SyncTransforms();var dir=Path.Combine(Work,"Audit","placement27","base");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=40;cam.aspect=4f/3f;cam.nearClipPlane=.01f;
   try{foreach(var (pi,id) in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1]))){
     var p=county.Properties.First(x=>x.Index==pi);var t=PropRoots(p).First(x=>IdOf(x)==id);var vs=DrawnVerts(t,6000).ToList();float bottom=vs.Min(v=>v.y);var c=new Vector3(vs.Average(v=>v.x),bottom,vs.Average(v=>v.z));float r=Mathf.Max(vs.Max(v=>new Vector2(v.x-c.x,v.z-c.z).magnitude)*2.4f,.35f);
     for(int k=0;k<3;k++){var d=Quaternion.Euler(0,k*120+15,0)*Vector3.forward;var pos=c+d*r+Vector3.up*.03f;cam.transform.position=pos;cam.transform.LookAt(c+Vector3.up*.03f);Shoot(cam,Path.Combine(dir,$"p{pi}-{id}-{k}.jpg"),400,300);}}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}}
  // two props drawn through each other: the first moves away from the second along the second's surface, by how far it
  // goes in (+6 mm), sideways only; then checked against the house and everything else. SERVICE_LOOK="2:kitchen-hutch>kitchen-stove"
  public static void Apart27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var pair in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains('>'))){var pi=int.Parse(pair.Split(':')[0]);var a=pair.Split(':')[1].Split('>')[0];var b=pair.Split('>')[1];
    var p=county.Properties.First(x=>x.Index==pi);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var ta=roots.First(x=>IdOf(x)==a);var tb=roots.First(x=>IdOf(x)==b);var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{Vector3 Need(out float worst,Transform into){worst=0;var push=Vector3.zero;var vs=DrawnVerts(ta,4000).ToList();var mid=new Vector3(vs.Average(v=>v.x),0,vs.Average(v=>v.z));float bot=vs.Min(v=>v.y);
      foreach(var v in vs){if(v.y<bot+.025f)continue;var o=new Vector3(mid.x,v.y,mid.z);var dv=v-o;float len=dv.magnitude;if(len<.02f)continue;foreach(var h in Physics.RaycastAll(o,dv/len,len,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(h.collider,out var ow)||ow==ta)continue;if(into!=null&&ow!=into)break;float d=Vector3.Dot(v-h.point,-h.normal);if(d>.01f){worst=Mathf.Max(worst,d);var n=h.normal;n.y=0;if(n.sqrMagnitude>.25f){n.Normalize();float have=Vector3.Dot(push,n);if(d+.006f>have)push+=n*(d+.006f-have);}}break;}}return push;}
     var need=Need(out var w0,tb);sb.AppendLine($"p{pi} {a} into {b}: {w0:F3} m; push {need:F3}");
     if(need.magnitude>0&&need.magnitude<.15f){ta.position+=need;Physics.SyncTransforms();Need(out var w1,null);if(w1>.012f){ta.position-=need;Physics.SyncTransforms();sb.AppendLine($"   moving it put {w1:F3} m into something else - left");}else sb.AppendLine("   moved, clear of everything");}}
    finally{Unvisual(vis);}}
   log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","placement27","apart27.txt"),sb.ToString());Save("Apart27");}
  // the drive lamps against what is round them: mailboxes, posts, trees, thickets, rocks, the road
  public static void LampsCheck27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var lamp in county.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("V27 drive lamp"))){var rs=lamp.GetComponentsInChildren<Renderer>().ToList();if(rs.Count==0)continue;var b=rs.Select(r=>r.bounds).Aggregate((x,y)=>{x.Encapsulate(y);return x;});
    var near=county.GetComponentsInChildren<Renderer>().Where(r=>!r.transform.IsChildOf(lamp)&&r.enabled&&r.gameObject.activeInHierarchy&&!(r is ParticleSystemRenderer)&&r.bounds.Intersects(new Bounds(b.center,b.size+new Vector3(.4f,0,.4f)))&&!(r.name.ToLowerInvariant().Contains("grass"))).Select(r=>PathOf(r.transform).Replace("SERVICE — Hollis County/","")+" "+Vector2.Distance(new Vector2(r.bounds.center.x,r.bounds.center.z),new Vector2(b.center.x,b.center.z)).ToString("F2")+" m").Distinct().Take(12).ToList();
    var post=new Vector3(b.center.x,b.min.y,b.center.z);bool onRoad=false;foreach(var h in Physics.RaycastAll(post+Vector3.up*3f,Vector3.down,4f,~0,QueryTriggerInteraction.Ignore))if(h.collider.name.ToLowerInvariant().Contains("road")||h.collider.name.ToLowerInvariant().Contains("asphalt"))onRoad=true;
    sb.AppendLine($"{PathOf(lamp).Replace("SERVICE — Hollis County/","")} at {b.center:F1} footprint {b.size.x:F2}x{b.size.z:F2}, on road {onRoad}; within 0.2 m: {(near.Count==0?"nothing":string.Join("; ",near))}");}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","lamps27.txt"),sb.ToString());}
  // where the post is put down in each house: on a drawn table top, not over its edge or in the air
  public static void Delivery27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.DeliveryPoint||!p.Building)continue;var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{var d=p.DeliveryPoint.position;var line=$"p{p.Index} delivery point {d:F3}:";foreach(var off in new[]{Vector3.zero,new Vector3(.12f,0,0),new Vector3(-.12f,0,0),new Vector3(0,0,.12f),new Vector3(0,0,-.12f)}){string what="nothing";foreach(var h in Physics.RaycastAll(d+off+Vector3.up*.4f,Vector3.down,2f,VisMask,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance)){if(!owner.TryGetValue(h.collider,out var ow))continue;what=$"{(ow==p.Building?"house":IdOf(ow))} at {h.point.y-d.y:+0.000;-0.000}";break;}line+=$" [{off.x:+0.00;-0.00},{off.z:+0.00;-0.00}] {what};";}
     sb.AppendLine(line);}finally{Unvisual(vis);}}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","delivery27.txt"),sb.ToString());}
  // the same model twice within reach of itself (a doubled clock, two identical chairs in one spot), every house
  public static void Dups27(){Open();var sb=new StringBuilder();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();
    string Model(Transform t){var n=t.name;int k=n.IndexOf(" — ");return k>0?n.Substring(k+3):n;}
    foreach(var g in roots.GroupBy(Model)){var list=g.ToList();for(int i=0;i<list.Count;i++)for(int j=i+1;j<list.Count;j++){float dd=Vector3.Distance(list[i].position,list[j].position);
      bool big=OOB(list[i],out _,out var e,out _,out _)&&e.magnitude>.45f;if(dd<(big?1.8f:.3f))sb.AppendLine($"p{p.Index} {g.Key}: {IdOf(list[i])} and {IdOf(list[j])} {dd:F2} m apart (at {list[i].position:F2} / {list[j].position:F2})");}}
    foreach(var t in roots.Where(x=>x.name.Contains("grandfather")))sb.AppendLine($"   clock p{p.Index} {IdOf(t)} at {t.position:F2} active {t.gameObject.activeInHierarchy}");}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","dups27.txt"),sb.ToString());}
  // every drawn thing whose name matches SERVICE_FIND, anywhere in the county
  public static void Find27(){Open();var sb=new StringBuilder();var key=(System.Environment.GetEnvironmentVariable("SERVICE_FIND")??"").ToLowerInvariant();
   foreach(var r in county.GetComponentsInChildren<Renderer>(true).Where(r=>PathOf(r.transform).ToLowerInvariant().Contains(key)))sb.AppendLine($"{PathOf(r.transform).Replace("SERVICE — Hollis County/","")} active {r.gameObject.activeInHierarchy} enabled {r.enabled} at {r.bounds.center:F2} size {r.bounds.size:F2}");
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","find27.txt"),sb.ToString());}
  // environment reflections: what glossy surfaces reflect at night
  public static void Refl27(){Open();var sb=new StringBuilder();
   sb.AppendLine($"reflection mode {RenderSettings.defaultReflectionMode} res {RenderSettings.defaultReflectionResolution} intensity {RenderSettings.reflectionIntensity} bounces {RenderSettings.reflectionBounces} custom {(RenderSettings.customReflectionTexture?RenderSettings.customReflectionTexture.name:"none")} skybox {(RenderSettings.skybox?RenderSettings.skybox.name+" / "+RenderSettings.skybox.shader.name:"none")} ambient {RenderSettings.ambientMode} {RenderSettings.ambientLight} sky {RenderSettings.ambientSkyColor} intensity {RenderSettings.ambientIntensity}");
   var lsa=UnityEditor.Lightmapping.lightingDataAsset;sb.AppendLine($"lighting data {(lsa?AssetDatabase.GetAssetPath(lsa):"none")}");
   foreach(var rp in county.GetComponentsInChildren<ReflectionProbe>(true))sb.AppendLine($"probe {PathOf(rp.transform)} mode {rp.mode} baked {(rp.bakedTexture?rp.bakedTexture.name:"-")} intensity {rp.intensity} size {rp.size}");
   foreach(var r in county.GetComponentsInChildren<Renderer>(true).Where(r=>r.name.Contains("mirror")||r.transform.parent&&r.transform.parent.name.Contains("mirror")))foreach(var m in r.sharedMaterials.Where(m=>m)){sb.AppendLine($"{PathOf(r.transform).Replace("SERVICE — Hollis County/","")} mat {m.name} shader {m.shader.name} smooth {(m.HasProperty("_Smoothness")?m.GetFloat("_Smoothness"):-1)} metal {(m.HasProperty("_Metallic")?m.GetFloat("_Metallic"):-1)} col {(m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():"-")} envrefl {(m.HasProperty("_EnvironmentReflections")?m.GetFloat("_EnvironmentReflections"):-1)} spec {(m.HasProperty("_SpecularHighlights")?m.GetFloat("_SpecularHighlights"):-1)} path {AssetDatabase.GetAssetPath(m)}");}
   // the glossiest materials in the houses (what else could mirror the day sky)
   var mats=new Dictionary<Material,int>();foreach(var p in county.Properties)foreach(var r in p.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials.Where(m=>m&&m.HasProperty("_Smoothness"))){float s=m.GetFloat("_Smoothness");bool mapped=m.HasProperty("_MetallicGlossMap")&&m.GetTexture("_MetallicGlossMap");if(s>.6f||mapped){mats[m]=mats.TryGetValue(m,out var k)?k+1:1;}}
   foreach(var kv in mats.OrderByDescending(k=>k.Key.GetFloat("_Smoothness")).Take(40))sb.AppendLine($"glossy {kv.Key.name} x{kv.Value} smooth {kv.Key.GetFloat("_Smoothness"):F2} metal {(kv.Key.HasProperty("_Metallic")?kv.Key.GetFloat("_Metallic"):-1):F2} map {(kv.Key.HasProperty("_MetallicGlossMap")&&kv.Key.GetTexture("_MetallicGlossMap")?"yes":"no")} envrefl {(kv.Key.HasProperty("_EnvironmentReflections")?kv.Key.GetFloat("_EnvironmentReflections"):-1)} shader {kv.Key.shader.name}");
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","refl27.txt"),sb.ToString());}
  // the wall behind a piece as drawn (its mesh) against its collider: SERVICE_LOOK list
  public static void WallMesh27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();
   foreach(var (pi,id) in (System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1]))){
    var p=county.Properties.First(x=>x.Index==pi);var t=PropRoots(p).FirstOrDefault(x=>IdOf(x)==id);if(!t||!OOB(t,out var c,out _,out _,out var rs))continue;var bb=rs.Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});var thin=bb.size.x<bb.size.z?Vector3.right:Vector3.forward;
    sb.AppendLine($"p{pi} {id}: bounds {bb.min:F3}..{bb.max:F3} thin {thin}");
    foreach(var sg in new[]{1f,-1f})foreach(var h in Physics.RaycastAll(c+thin*sg*.6f,-thin*sg,1.2f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).Take(4))sb.AppendLine($"   ray {sg:+0;-0}: {h.collider.name} ({h.collider.GetType().Name}) at {Vector3.Dot(h.point,thin):F3} normal {h.normal:F2}");
    // mesh faces of the building near it: vertex planes along the thin axis within its footprint
    foreach(var mf in p.Building.GetComponentsInChildren<MeshFilter>()){if(!mf.sharedMesh)continue;var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mr.bounds.Intersects(new Bounds(bb.center,bb.size+Vector3.one*.3f)))continue;var vs=mf.sharedMesh.vertices;var planes=new SortedSet<float>();
     foreach(var v0 in vs){var v=mf.transform.TransformPoint(v0);if(Mathf.Abs(v.y-bb.center.y)>1.2f)continue;var off=v-bb.center;off-=thin*Vector3.Dot(off,thin);if(off.magnitude>1.2f)continue;planes.Add(Mathf.Round(Vector3.Dot(v,thin)*1000f)/1000f);}
     if(planes.Count>0)sb.AppendLine($"   mesh {mf.name}: planes along {thin} near it: {string.Join(" ",planes.Where(x=>Mathf.Abs(x-Vector3.Dot(bb.center,thin))<.4f).Take(14).Select(x=>x.ToString("F3")))}");}}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","wallmesh27.txt"),sb.ToString());}
  // pieces hung on a wall: from in front, and grazing along the wall from both sides (a frame sunk into the wall or
  // standing off it shows at once). SERVICE_LOOK="5:landing-picture,...".
  public static void WallLook27(){Open();Physics.SyncTransforms();var dir=Path.Combine(Work,"Audit","placement27","wall");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);var sb=new StringBuilder();
   var list=(System.Environment.GetEnvironmentVariable("SERVICE_LOOK")??"").Split(',').Where(x=>x.Contains(':')).Select(x=>x.Split(':')).Select(x=>(int.Parse(x[0]),x[1])).ToList();
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=55;cam.aspect=4f/3f;cam.nearClipPlane=.02f;
   try{foreach(var (pi,id) in list){var p=county.Properties.First(x=>x.Index==pi);var t=PropRoots(p).FirstOrDefault(x=>IdOf(x)==id);if(!t||!OOB(t,out var c,out var e,out _,out var rs))continue;var own=new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));
     var bb=rs.Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});var thin=bb.size.x<bb.size.z?Vector3.right:Vector3.forward;Vector3 n=Vector3.zero;float best=99;
     foreach(var sg in new[]{1f,-1f}){foreach(var h in Physics.RaycastAll(c+thin*sg*.6f,-thin*sg,1.2f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(own.Contains(h.collider)||h.collider.transform.IsChildOf(t))continue;if(Vector3.Dot(h.normal,thin*sg)>.7f){float dd=Mathf.Abs(Vector3.Dot(h.point-c,thin));if(dd<best){best=dd;n=h.normal;}}break;}}
     if(n==Vector3.zero)n=thin;var along=Vector3.Cross(Vector3.up,n).normalized;sb.AppendLine($"p{pi} {id} at {c:F2} size {bb.size:F3} wall normal {n:F2}, wall face {best:F3} m from its middle");
     foreach(var (tag,pos) in new[]{("front",c+n*1.3f),("grazeL",c+n*.22f+along*.9f),("grazeR",c+n*.22f-along*.9f)}){cam.transform.position=pos;cam.transform.LookAt(c);Shoot(cam,Path.Combine(dir,$"p{pi}-{id}-{tag}.jpg"),480,360);}}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}File.WriteAllText(Path.Combine(dir,"wall.txt"),sb.ToString());}
  // vertices of a prop through the house's structure (the Poke27 test for one prop): how many, how deep
  static (int n,float d) PokeOne(ServiceProperty p,Transform t){if(!OOB(t,out var c,out _,out _,out _))return (0,0);var own=new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));int n=0;float worst=0;
   foreach(var mf in t.GetComponentsInChildren<MeshFilter>(false)){if(!mf.sharedMesh)continue;var vs=mf.sharedMesh.vertices;int step=Mathf.Max(1,vs.Length/3000);for(int i=0;i<vs.Length;i+=step){var v=mf.transform.TransformPoint(vs[i]);var o=new Vector3(c.x,v.y,c.z);var dv=v-o;float len=dv.magnitude;if(len<.02f)continue;
    foreach(var h in Physics.RaycastAll(o,dv/len,len,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(own.Contains(h.collider)||h.collider.transform.IsChildOf(t)||!h.collider.transform.IsChildOf(p.Building))continue;float depth=Vector3.Dot(v-h.point,-h.normal);if(depth>.004f){n++;worst=Mathf.Max(worst,depth);}break;}}
    // and straight up from the top of it: nothing above but the ceiling it hangs from
   }return (n,worst);}
  // Harrow's dining lantern: over the table, its crown at the ceiling, nothing through the slope or the end wall. Tries
  // places near the table's middle (towards the ridge, away from the end wall) and takes the nearest clean one.
  public static void Lantern27(){Open();Physics.SyncTransforms();var p=county.Properties.First(x=>x.Index==3);var roots=PropRoots(p).ToList();var lan=roots.First(t=>IdOf(t)=="dining-lantern");var table=roots.First(t=>IdOf(t)=="dining-table");
   OOB(table,out var tc,out _,out _,out var trs);float tableTop=trs.Max(r=>r.bounds.max.y);var start=lan.position;var lights=p.GetComponentsInChildren<Light>(true).Where(l=>!l.transform.IsChildOf(lan)&&Vector3.Distance(l.transform.position,start)<1.2f).ToList();var lightStart=lights.Select(l=>l.transform.position).ToList();
   var own=new HashSet<Collider>(lan.GetComponentsInChildren<Collider>(true));(float score,Vector3 pos,int n,float d,float gap) best=(float.MaxValue,start,0,0,0);
   for(float dx=-.9f;dx<=.9f;dx+=.05f)for(float dz=-.6f;dz<=.6f;dz+=.05f){
    var probe=new Vector3(tc.x+dx,tableTop+.4f,tc.z+dz);float ceil=float.NaN;foreach(var h in Physics.RaycastAll(probe,Vector3.up,6f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(own.Contains(h.collider)||!h.collider.transform.IsChildOf(p.Building))continue;ceil=h.point.y;break;}if(float.IsNaN(ceil))continue;
    OOB(lan,out var lc,out _,out _,out var lrs);float top=lrs.Max(r=>r.bounds.max.y);lan.position+=new Vector3(tc.x+dx-lc.x,ceil-.004f-top,tc.z+dz-lc.z);Physics.SyncTransforms();
    var (n,d)=PokeOne(p,lan);float off=new Vector2(dx,dz).magnitude;float score=n*10f+d*100f+off;if(score<best.score){OOB(lan,out lc,out _,out _,out lrs);best=(score,lan.position,n,d,lrs.Min(r=>r.bounds.min.y)-tableTop);}}
   lan.position=best.pos;Physics.SyncTransforms();var moved=best.pos-start;for(int i=0;i<lights.Count;i++)lights[i].transform.position=lightStart[i]+moved;
   log.AppendLine($"LANTERN at {best.pos:F2} (moved {moved:F2}): {best.n} vertices through ({best.d:F3} m), {best.gap:F2} m over the table, crown at the ceiling; {lights.Count} light(s) moved with it");Save("Lantern27");}
  // V27 redo, by hand: Harrow's dining lantern hangs over the dining table with its crown at the ceiling (it had been
  // lowered out of the slope and was left hanging in the air); Morrow's hall mirror comes out of the door casing beside it
  // and sits flat on the wall (the console under it has no clip of its own and stays).
  public static void Hand27(){Open();Physics.SyncTransforms();
   {var p=county.Properties.First(x=>x.Index==3);var roots=PropRoots(p).ToList();var lan=roots.FirstOrDefault(t=>IdOf(t)=="dining-lantern");var table=roots.FirstOrDefault(t=>IdOf(t)=="dining-table");
    if(lan&&table&&OOB(table,out var tc,out var te,out _,out var trs)&&OOB(lan,out var lc,out var le,out _,out var lrs)){float tableTop=trs.Max(r=>r.bounds.max.y);float lanTop=lrs.Max(r=>r.bounds.max.y),lanBottom=lrs.Min(r=>r.bounds.min.y);
     var own=new HashSet<Collider>(lan.GetComponentsInChildren<Collider>(true));var from=new Vector3(tc.x,tableTop+.4f,tc.z);float ceil=float.NaN;string what="";
     foreach(var h in Physics.RaycastAll(from,Vector3.up,6f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(own.Contains(h.collider)||!h.collider.transform.IsChildOf(p.Building))continue;ceil=h.point.y;what=h.collider.name;break;}
     log.AppendLine($"HAND lantern was at {lc:F2} top {lanTop:F2} bottom {lanBottom:F2}; table {IdOf(table)} centre {tc:F2} top {tableTop:F2}; ceiling over the table {ceil:F2} ({what})");
     if(!float.IsNaN(ceil)){float drop=ceil-.005f-lanTop;var move=new Vector3(tc.x-lc.x,drop,tc.z-lc.z);lan.position+=move;Physics.SyncTransforms();OOB(lan,out lc,out le,out _,out lrs);
      log.AppendLine($"HAND lantern moved {move:F2}: now centre {lc:F2}, top {lrs.Max(r=>r.bounds.max.y):F2}, bottom {lrs.Min(r=>r.bounds.min.y):F2} ({lrs.Min(r=>r.bounds.min.y)-tableTop:F2} m over the table)");
      foreach(var l in p.GetComponentsInChildren<Light>(true))if(Vector3.Distance(l.transform.position,lc-move)<1.2f&&!l.transform.IsChildOf(lan)){l.transform.position+=move;log.AppendLine($"HAND   its light {l.name} moved with it");}}}}
   {var p=county.Properties.First(x=>x.Index==5);var m=PropRoots(p).FirstOrDefault(t=>IdOf(t)=="hall-mirror");
    if(m&&OOB(m,out var c,out var e,out var r,out var rs)){var own=new HashSet<Collider>(m.GetComponentsInChildren<Collider>(true));
     // the wall it hangs on: straight back from in front of it
     var thin=rs.Select(x=>x.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});var nrm=thin.size.x<thin.size.z?Vector3.right:Vector3.forward;
     Vector3 wallN=Vector3.zero;float wallAt=0;foreach(var sgn in new[]{1f,-1f}){var o=c+nrm*sgn*.6f;foreach(var h in Physics.RaycastAll(o,-nrm*sgn,1.2f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(own.Contains(h.collider)||h.collider.transform.IsChildOf(m))continue;if(!h.collider.transform.IsChildOf(p.Building))break;if(Vector3.Dot(h.normal,nrm*sgn)>.7f){float at=Vector3.Dot(h.point,nrm);if(wallN==Vector3.zero||Mathf.Abs(at-Vector3.Dot(c,nrm))<Mathf.Abs(wallAt-Vector3.Dot(c,nrm))){wallN=h.normal;wallAt=at;}}break;}}
     var along=Vector3.Cross(Vector3.up,wallN).normalized;
     // what stands proud of the wall to either side at the mirror's height: the casing
     float half=Vector3.Dot(thin.extents,new Vector3(Mathf.Abs(along.x),0,Mathf.Abs(along.z)));float shift=0;var probeFrom=new Vector3(c.x,c.y,c.z)+wallN*(Vector3.Dot(wallN,nrm)*(wallAt-Vector3.Dot(c,nrm))-.03f);
     foreach(var sgn in new[]{1f,-1f}){foreach(float hy in new[]{-.25f,0f,.25f}){var o=probeFrom+Vector3.up*hy;foreach(var h in Physics.RaycastAll(o,along*sgn,half+.4f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(own.Contains(h.collider)||h.collider.transform.IsChildOf(m))continue;float need=half+.03f-h.distance;if(need>0&&Mathf.Abs(need)>Mathf.Abs(shift))shift=-sgn*need;log.AppendLine($"HAND mirror side {sgn:+0;-0} at {hy:+0.00;-0.00}: {h.collider.name} {h.distance:F3} m from its middle (half width {half:F3})");break;}}}
     float back=float.MaxValue;foreach(var x in rs){var b=x.bounds;foreach(var k in new[]{b.min,b.max})back=Mathf.Min(back,Vector3.Dot(k-c,wallN));}
     float outBy=wallAt*1f-Vector3.Dot(c+wallN*back,nrm);var move=along*shift+wallN*(Vector3.Dot(wallN,nrm)*(wallAt-Vector3.Dot(c+wallN*back,nrm))+.004f);
     log.AppendLine($"HAND mirror at {c:F2} size {thin.size:F3}; wall normal {wallN:F2} surface at {wallAt:F3} along {nrm}; back {back:F3}; slide {shift:F3} along {along:F2}; move {move:F3}");
     if(move.magnitude<.4f){m.position+=move;Physics.SyncTransforms();}
     }}
   Save("Hand27");}
  // V27 redo: the strict check. Every vertex of every prop, seen from inside the prop (its middle, at that vertex's
  // height): if the line out to the vertex crosses a surface of the house first - a wall, a sill, a sloped ceiling, a
  // stair - that vertex is through it, by exactly how far. No bounding boxes. Also: a prop whose middle is itself inside
  // the house's structure (a mirror sunk into a wall). Poke27 reports; Poke27Fix moves standing furniture and wall-hung
  // pieces out by what they need.
  public static void Poke27(){Poke(false);}
  public static void Poke27Fix(){Poke(true);}
  static void Poke(bool fix){Open();Physics.SyncTransforms();var sb=new StringBuilder();int props=0,verts=0,bad=0,moved=0,unread=0;var found=new List<(ServiceProperty p,Transform t,string id,Vector3 at,Vector3 n,float d)>();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);
    var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();var byId=new Dictionary<string,Transform>();foreach(var t in roots)byId[IdOf(t)]=t;
    var deps=new Dictionary<Transform,List<Transform>>();foreach(var t in roots)if(lay.TryGetValue(IdOf(t),out var li)&&!string.IsNullOrEmpty(li.on)&&byId.TryGetValue(li.on,out var sup)){if(!deps.ContainsKey(sup))deps[sup]=new List<Transform>();deps[sup].Add(t);}
    void Move(Transform t,Vector3 delta){t.position+=delta;if(deps.TryGetValue(t,out var ds))foreach(var c2 in ds)Move(c2,delta);}
    foreach(var t in roots){var n=t.name.ToLowerInvariant();if(n.Contains("fire")||n.Contains("blood")||n.Contains("apparition")||FlatWords.Any(w=>n.Contains(w)))continue;
     if(!OOB(t,out var c,out var e,out var r,out var rs))continue;props++;var kind=KindOf(IdOf(t),t.name,lay);
     var own=new HashSet<Collider>(t.GetComponentsInChildren<Collider>(true));var perNormal=new Dictionary<Vector3Int,(Vector3 n,float d,string what)>();float worst=0;string worstWhat="";int pokes=0;Vector3 worstAt=c,worstN=Vector3.forward;
     float bottom=float.MaxValue;foreach(var x in rs)bottom=Mathf.Min(bottom,x.bounds.min.y);bool standing=kind=="floor"||kind=="top";
     foreach(var mf in t.GetComponentsInChildren<MeshFilter>(false)){var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mr.enabled||!mf.sharedMesh)continue;Vector3[] vs;try{vs=mf.sharedMesh.vertices;}catch{unread++;continue;}if(vs==null||vs.Length==0){unread++;continue;}
      int step=Mathf.Max(1,vs.Length/3000);for(int i=0;i<vs.Length;i+=step){var v=mf.transform.TransformPoint(vs[i]);if(standing&&v.y<bottom+.025f)continue;verts++;var o=new Vector3(c.x,v.y,c.z);var dv=v-o;float len=dv.magnitude;if(len<.02f)continue;
       foreach(var h in Physics.RaycastAll(o,dv/len,len,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)){if(own.Contains(h.collider)||h.collider.transform.IsChildOf(t))continue;if(!h.collider.transform.IsChildOf(p.Building))continue;
        float depth=Vector3.Dot(v-h.point,-h.normal);if(depth>.008f){pokes++;var key=Vector3Int.RoundToInt(h.normal*4);var cur=perNormal.TryGetValue(key,out var pv)?pv:(h.normal,0f,h.collider.name);if(depth>cur.Item2)perNormal[key]=(h.normal,depth,h.collider.name);if(depth>worst){worst=depth;worstWhat=h.collider.name;worstAt=h.point;worstN=h.normal;}}break;}}}
     bool sunk=Physics.OverlapSphere(c,.01f,~0,QueryTriggerInteraction.Ignore).Any(x=>!own.Contains(x)&&x.transform.IsChildOf(p.Building)&&!(x is MeshCollider));
     if(pokes==0&&!sunk)continue;bad++;
     sb.AppendLine($"p{p.Index} [{kind}] {IdOf(t)} ({t.name.Substring(Mathf.Min(t.name.Length,IdOf(t).Length)).Trim(' ','—')}) {pokes} vertices through, deepest {worst:F3} m into {worstWhat}{(sunk?" - ITS MIDDLE IS INSIDE THE STRUCTURE":"")} at {c:F2}: "+string.Join("; ",perNormal.Values.Select(x=>$"{x.d:F3} m against normal {x.n:F2} ({x.what})")));
     if(!fix){found.Add((p,t,IdOf(t),worstAt,worstN,worst));continue;}
     // standing furniture and wall pieces: out along each wall's normal by what goes through it (sideways only for floor pieces)
     var push=Vector3.zero;var dirs=new List<Vector3>();foreach(var x in perNormal.Values){var nn=x.n;nn.y=0;if(nn.sqrMagnitude<.08f)continue;nn.Normalize();dirs.Add(nn);float have=Vector3.Dot(push,nn);if(x.d+.006f>have)push+=nn*(x.d+.006f-have);}
     if(dirs.Any(a=>dirs.Any(b=>Vector3.Dot(a,b)<-.5f))){sb.AppendLine("   through faces on both sides - left for a hand fix");continue;}
     if(kind=="ceiling"||IdOf(t)=="hall-mirror"){sb.AppendLine("   (placed by hand: Hand27)");continue;}
     if(push.magnitude>.22f){sb.AppendLine($"   needs {push.magnitude:F2} m - left for a hand fix");continue;}
     if(push.sqrMagnitude>1e-6f){Move(t,push);moved++;Physics.SyncTransforms();sb.AppendLine($"   moved {push:F3}");}}}
   sb.Insert(0,$"Poke27{(fix?"Fix":"")}: {props} props, {verts} vertices tested, {bad} through the structure, {moved} moved, {unread} meshes unreadable"+System.Environment.NewLine);
   File.WriteAllText(Path.Combine(Work,"Audit","placement27",fix?"poke27fix.txt":"poke27.txt"),sb.ToString());if(fix){log.Append(sb.ToString());Save("Poke27Fix");return;}
   // a close look at each one: from the room, a little off to the side of the deepest place, so a clip or a gap shows
   if(UnityEngine.SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
   var dir=Path.Combine(Work,"Audit","placement27","poke");Directory.CreateDirectory(dir);foreach(var f in Directory.GetFiles(dir))File.Delete(f);
   var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=60;cam.aspect=4f/3f;cam.nearClipPlane=.02f;
   try{foreach(var (p,t,id,at,nrm,d) in found){var n=nrm;var side=Vector3.Cross(Vector3.up,n);if(side.sqrMagnitude<.01f)side=Vector3.right;side.Normalize();
     foreach(var (tag,off) in new[]{("a",n*1.1f+side*.7f+Vector3.up*.35f),("b",n*1.1f-side*.7f+Vector3.up*.35f)}){var pos=at+off;
      // keep the camera in the room: back it off if something is in the way
      if(Physics.Raycast(at+n*.05f,(pos-at).normalized,out var hh,(pos-at).magnitude,~0,QueryTriggerInteraction.Ignore))pos=hh.point-(pos-at).normalized*.08f;
      cam.transform.position=pos;cam.transform.LookAt(at);Shoot(cam,Path.Combine(dir,$"p{p.Index}-{id}-{tag}.jpg"),480,360);}}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}}
  // railings (porches, stairs, landings): which ones a person could walk through - no solid collider over them
  public static void Rails27(){Open();Physics.SyncTransforms();var sb=new StringBuilder();int bad=0,all=0;
   string[] words={"rail","balust","banister","baluster","handrail","fence","spindle","newel"};
   foreach(var p in county.Properties.OrderBy(x=>x.Index))foreach(var r in p.GetComponentsInChildren<MeshRenderer>(true)){if(!r.gameObject.activeInHierarchy)continue;var n=r.name.ToLowerInvariant();if(!words.Any(w=>n.Contains(w)))continue;all++;
    var b=r.bounds;var own=r.GetComponentsInParent<Collider>(true).Concat(r.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled&&!c.isTrigger).ToList();
    var hits=Physics.OverlapBox(b.center,Vector3.Max(b.extents*.9f,Vector3.one*.02f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Where(c=>!(c is TerrainCollider)&&c.bounds.max.y>b.min.y+.3f&&!(c is CharacterController)).ToList();
    bool covered=own.Count>0||hits.Any(c=>c.transform==r.transform||c.transform.IsChildOf(r.transform.parent?r.transform.parent:r.transform));
    // something solid filling the railing's own volume (its own mesh collider or a box over it)
    if(!covered){bad++;sb.AppendLine($"p{p.Index} NO COLLIDER {PathOf(r.transform).Replace("SERVICE — Hollis County/","")} at {b.center:F2} size {b.size:F2}; overlapping: {string.Join(", ",hits.Take(4).Select(c=>c.name))}");}}
   sb.Insert(0,$"Rails27: {all} railing pieces, {bad} without their own collider"+System.Environment.NewLine);File.WriteAllText(Path.Combine(Work,"Audit","placement27","rails27.txt"),sb.ToString());}
  // every paper on or near a front door, and whether the game can read it
  public static void Notes27(){Open();var sb=new StringBuilder();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){sb.AppendLine($"p{p.Index} {p.name}: NoticePoint {(p.NoticePoint?PathOf(p.NoticePoint).Replace("SERVICE — Hollis County/","")+" active "+p.NoticePoint.gameObject.activeSelf+" at "+p.NoticePoint.position.ToString("F2"):"none")} text [{(p.NoticeText??"").Replace('\n','/')}]");
    foreach(var r in p.GetComponentsInChildren<Renderer>(true)){var n=r.name.ToLowerInvariant();if(!(n.Contains("note")||n.Contains("paper")||n.Contains("tape")||n.Contains("letter")||n.Contains("sign")))continue;float dd=Vector3.Distance(r.bounds.center,p.Door.position);if(dd>4f)continue;
     sb.AppendLine($"   {PathOf(r.transform).Replace("SERVICE — Hollis County/","")} active {r.gameObject.activeInHierarchy} {dd:F1} m from the door, size {r.bounds.size:F2}");}}
   File.WriteAllText(Path.Combine(Work,"Audit","placement27","notes27.txt"),sb.ToString());}
  // what the opened leaf touches, and pictures from in, out and above at shut, half and open
  public static void ValeLook27(){Open();Physics.SyncTransforms();var p=county.Properties.First(x=>x.Index==1);p.MeasureDoorway();var hp=p.DoorPanel;var rest=hp.localRotation;var (leaf,half)=LeafBox(p);
   hp.localRotation=rest*Quaternion.Euler(0,p.DoorSwing,0);Physics.SyncTransforms();var centre=leaf.transform.TransformPoint(((BoxCollider)leaf).center);
   foreach(var h in Physics.OverlapBox(centre,half*.92f,leaf.transform.rotation,~0,QueryTriggerInteraction.Ignore))if(!h.transform.IsChildOf(hp))log.AppendLine($"VALE LOOK open leaf touches {PathOf(h.transform)} bounds {h.bounds.center:F2} {h.bounds.size:F2}");
   log.AppendLine($"VALE LOOK leaf box {half*2:F2} centre {centre:F2}; door {p.Door.position:F2}; opening {p.OpeningCentre:F2} inward {p.Inward:F2} width {p.DoorWidth:F2}");hp.localRotation=rest;
   var dir=Path.Combine(Work,"Audit","placement27","valedoor");Directory.CreateDirectory(dir);var muted=MuteFeatures();var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=70;cam.aspect=16f/9f;
   try{foreach(var (tag,deg) in new[]{("shut",0f),("half",.5f),("open",1f)}){hp.localRotation=rest*Quaternion.Euler(0,p.DoorSwing*deg,0);var oc=p.OpeningCentre;var inw=p.Inward;inw.y=0;inw.Normalize();
     cam.orthographic=false;cam.transform.position=oc-inw*3.2f+Vector3.up*1.6f;cam.transform.LookAt(oc+Vector3.up*1.1f);Shoot(cam,Path.Combine(dir,$"{tag}-out.jpg"),960,540);
     cam.transform.position=oc+inw*3.2f+Vector3.up*1.6f;cam.transform.LookAt(oc+Vector3.up*1.1f);Shoot(cam,Path.Combine(dir,$"{tag}-in.jpg"),960,540);
     cam.transform.position=oc+inw*1.6f-Vector3.Cross(Vector3.up,inw)*1.4f+Vector3.up*1.5f;cam.transform.LookAt(oc+inw*.3f+Vector3.up*.9f);Shoot(cam,Path.Combine(dir,$"{tag}-side.jpg"),960,540);
     cam.transform.position=oc+inw*1.6f+Vector3.Cross(Vector3.up,inw)*1.4f+Vector3.up*1.5f;cam.transform.LookAt(oc+inw*.3f+Vector3.up*.9f);Shoot(cam,Path.Combine(dir,$"{tag}-side2.jpg"),960,540);}
    hp.localRotation=rest;}
   finally{hp.localRotation=rest;EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);Restore(muted);}
   File.AppendAllText(Path.Combine(Work,"v19-rebuild.txt"),"==== ValeLook27"+System.Environment.NewLine+log);log.Clear();}
 }
}
