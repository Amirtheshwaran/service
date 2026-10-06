using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V25 "the roads especially are messed up" / "road 214 need work": every drive was a near-black gravel ribbon laid on a
 // pale forest floor (the EasyRoads dirt-road texture is dark grey; the ground averages 0.36 luminance) - it read as a
 // strip of black plastic with a dark triangle where it met the road. Now the drives, footpaths and road shoulders are
 // painted into the terrain itself with the Poly Haven gravel_road texture (same tone as the ground, CC0, already in the
 // project) with soft edges and a damp margin, at twice the terrain's paint resolution; the ribbons stay as invisible
 // walking surfaces (their colliders and gravel footsteps). Drives25 saves; Drives25Try renders without saving.
 public static partial class ServiceV19Rebuild {
  const string V25="Assets/ServiceArt/V25";
  public static void Drives25(){Open();Drives25Stage(true);Save("drives25");}
  public static void Drives25Try(){Open();Drives25Stage(false);}
  static bool Footprint25(Renderer r){var n=r.name;return n.StartsWith("V19 drive ")||n.StartsWith("V23 footpath ")||n=="V19 county road verge"||n=="V19 County Route 9 verge";}
  static void Drives25Stage(bool save){
   Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,V25,"Ground"));AssetDatabase.Refresh();
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);
   var t=county.GetComponentInChildren<Terrain>();var td=t.terrainData;
   try{
    // 1. the gravel layer (Poly Haven gravel_road, already imported in V19)
    var gl=AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{V25}/Ground/V25 gravel_road.terrainlayer");
    if(!gl){gl=new TerrainLayer();AssetDatabase.CreateAsset(gl,$"{V25}/Ground/V25 gravel_road.terrainlayer");}
    gl.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ServiceArt/V19/Ground/gravel_road_diff.png");gl.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ServiceArt/V19/Ground/gravel_road_normal.png");
    gl.tileSize=new Vector2(2.4f,2.4f);gl.normalScale=.8f;gl.smoothness=0;gl.metallic=0;gl.diffuseRemapMax=new Vector4(.8f,.78f,.76f,1);/* a shade greyer and darker than the forest floor, so the way up is readable at night */EditorUtility.SetDirty(gl);
    var layers=td.terrainLayers.ToList();int g=layers.FindIndex(l=>l==gl);int mud=layers.FindIndex(l=>l&&l.name.Contains("mud_forest"));
    // 2. twice the paint resolution (0.70 x 1.27 m texels made a 3 m drive a smear), upsampled from the old map
    int oldRes=td.alphamapResolution;var old=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);int L0=old.GetLength(2);
    if(g<0){layers.Add(gl);g=layers.Count-1;}
    int res=Mathf.Max(oldRes,1024);int L=layers.Count;var map=new float[res,res,L];
    for(int z=0;z<res;z++)for(int x=0;x<res;x++){float u=x*(oldRes-1f)/(res-1f),v=z*(oldRes-1f)/(res-1f);int x0=Mathf.Min((int)u,oldRes-2),z0=Mathf.Min((int)v,oldRes-2);float fx=u-x0,fz=v-z0;
     for(int k=0;k<L0&&k<L;k++)map[z,x,k]=Mathf.Lerp(Mathf.Lerp(old[z0,x0,k],old[z0,x0+1,k],fx),Mathf.Lerp(old[z0+1,x0,k],old[z0+1,x0+1,k],fx),fz);}
    // 3. the footprints of the drive, footpath and shoulder ribbons, rasterised at the new resolution
    var cover=new float[res,res];int tris=0;var tp=t.transform.position;var sz=td.size;
    var ribbons=county.GetComponentsInChildren<MeshRenderer>(true).Where(Footprint25).ToList();
    foreach(var r in ribbons){var mf=r.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;var m=mf.sharedMesh;var vs=m.vertices.Select(v=>r.transform.TransformPoint(v)).ToArray();var tr=m.triangles;
     // shoulders paint at full strength only on their inner part; drives and paths over their whole (alpha-edged) width
     for(int i=0;i<tr.Length;i+=3){var a=vs[tr[i]];var b=vs[tr[i+1]];var c=vs[tr[i+2]];tris++;
      float ax=(a.x-tp.x)/sz.x*(res-1),az=(a.z-tp.z)/sz.z*(res-1),bx=(b.x-tp.x)/sz.x*(res-1),bz=(b.z-tp.z)/sz.z*(res-1),cx=(c.x-tp.x)/sz.x*(res-1),cz=(c.z-tp.z)/sz.z*(res-1);
      int xa=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(ax,Mathf.Min(bx,cx)))),xb=Mathf.Min(res-1,Mathf.CeilToInt(Mathf.Max(ax,Mathf.Max(bx,cx)))),za=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(az,Mathf.Min(bz,cz)))),zb=Mathf.Min(res-1,Mathf.CeilToInt(Mathf.Max(az,Mathf.Max(bz,cz))));
      float area=(bx-ax)*(cz-az)-(cx-ax)*(bz-az);if(Mathf.Abs(area)<1e-6f)continue;
      for(int z=za;z<=zb;z++)for(int x=xa;x<=xb;x++){float w0=((bx-x)*(cz-z)-(cx-x)*(bz-z))/area,w1=((cx-x)*(az-z)-(ax-x)*(cz-z))/area,w2=1-w0-w1;if(w0>=-.02f&&w1>=-.02f&&w2>=-.02f)cover[z,x]=1;}}}
    // two wheel ruts down each drive (a rural two-track), from the ribbon's own cross-sections
    var ruts=new float[res,res];int rutSegs=0;
    foreach(var r in ribbons.Where(x=>x.name.StartsWith("V19 drive "))){var m=r.GetComponent<MeshFilter>().sharedMesh;var vs=m.vertices.Select(v=>r.transform.TransformPoint(v)).ToArray();
     for(int side=-1;side<=1;side+=2){Vector3? prev=null;for(int i=0;i+1<vs.Length;i+=2){var lft=vs[i];var rgt=vs[i+1];var c=(lft+rgt)*.5f;var across=(rgt-lft);float half=across.magnitude*.5f/1.18f;if(half<1.1f){prev=null;continue;}
       var pt=c+across.normalized*side*.72f;if(prev.HasValue){Line25(ruts,res,tp,sz,prev.Value,pt,.16f);rutSegs++;}prev=pt;}}}
    ruts=Blur25(ruts,res,1);
    // 4. soft edges: a narrow blur for the gravel, a wide one for a damp margin around it
    var near=Blur25(cover,res,1);var wide=Blur25(cover,res,5);int painted=0;
    for(int z=0;z<res;z++)for(int x=0;x<res;x++){float gw=Mathf.Clamp01(near[z,x]*1.2f-.1f)*.97f;float mw=mud>=0?Mathf.Clamp01(wide[z,x]-near[z,x])*.7f+ruts[z,x]*.42f*gw:0;if(gw<=.001f&&mw<=.001f)continue;painted++;
     float rest=0;for(int k=0;k<L;k++)if(k!=g&&k!=mud)rest+=map[z,x,k];float keepMud=mud>=0?map[z,x,mud]:0;
     float newG=Mathf.Max(map[z,x,g],gw),newM=Mathf.Max(keepMud,mw)*(1-newG);float left=Mathf.Max(0,1-newG-newM);
     for(int k=0;k<L;k++)if(k!=g&&k!=mud)map[z,x,k]=rest>1e-5f?map[z,x,k]/rest*left:(k==0?left:0);map[z,x,g]=newG;if(mud>=0)map[z,x,mud]=newM;}
    td.terrainLayers=layers.ToArray();td.alphamapResolution=res;td.SetAlphamaps(0,0,map);EditorUtility.SetDirty(td);
    // 5. the ribbons go invisible: they stay the walking surface (colliders, gravel footsteps)
    foreach(var r in ribbons){r.enabled=false;EditorUtility.SetDirty(r);}
    log.AppendLine($"DRIVES25 {rutSegs} rut segments; terrain paint {oldRes} -> {res} ({sz.x/res:F2} x {sz.z/res:F2} m texels), gravel layer {g}, mud {mud}; {ribbons.Count} ribbons ({tris} triangles) painted into {painted} texels and hidden");
    if(!save)Drives25Shots();
   } finally {if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
  }
  static void Line25(float[,] m,int res,Vector3 tp,Vector3 sz,Vector3 a,Vector3 b,float halfW){
   float len=Vector3.Distance(new Vector3(a.x,0,a.z),new Vector3(b.x,0,b.z));int steps=Mathf.Max(1,Mathf.CeilToInt(len/.12f));
   for(int s=0;s<=steps;s++){var p=Vector3.Lerp(a,b,s/(float)steps);float px=(p.x-tp.x)/sz.x*(res-1),pz=(p.z-tp.z)/sz.z*(res-1);float rx=halfW/sz.x*(res-1),rz=halfW/sz.z*(res-1);
    for(int z=Mathf.Max(0,Mathf.FloorToInt(pz-rz));z<=Mathf.Min(res-1,Mathf.CeilToInt(pz+rz));z++)for(int x=Mathf.Max(0,Mathf.FloorToInt(px-rx));x<=Mathf.Min(res-1,Mathf.CeilToInt(px+rx));x++)m[z,x]=1;}}
  static float[,] Blur25(float[,] src,int res,int r){var a=new float[res,res];var b=new float[res,res];
   for(int z=0;z<res;z++){float acc=0;int n=0;for(int x=-r;x<res;x++){if(x+r<res){acc+=src[z,x+r];n++;}if(x-r-1>=0){acc-=src[z,x-r-1];n--;}if(x>=0)a[z,x]=acc/Mathf.Max(n,1);}}
   for(int x=0;x<res;x++){float acc=0;int n=0;for(int z=-r;z<res;z++){if(z+r<res){acc+=a[z+r,x];n++;}if(z-r-1>=0){acc-=a[z-r-1,x];n--;}if(z>=0)b[z,x]=acc/Mathf.Max(n,1);}}
   return b;}
  // V25 "some of the bushes are floating": the forest thickets (a bush and two tall grasses each, 2700 of them) were
  // settled in V23 as whole clusters - the cluster's combined footprint against one terrain height - so on a hillside the
  // individual plants hung up to 1.3 m in the air (census: 730). Each plant now sits on the ground under its own footprint.
  public static void Foliage25(){Open();Foliage25Stage();Save("foliage25");}
  static void Foliage25Stage(){var t=county.GetComponentInChildren<Terrain>();int moved=0,seen=0;float worst=0;var done=new HashSet<Transform>();
   // the plants exactly as the census counts them (FoliageRoot: each bush, fern or grass clump), measured the same way
   foreach(var r in county.GetComponentsInChildren<Renderer>(false)){if(!r.enabled)continue;var u=FoliageRoot(r.transform);if(!u||!done.Add(u))continue;
    if(u.name.Contains("thicket"))continue; // a whole cluster: its plants are units of their own
    seen++;var b=RBounds(u);float rad=Mathf.Min(Mathf.Max(b.extents.x,b.extents.z)*.6f,.7f);var c=new Vector3(b.center.x,0,b.center.z);
    float centreG=TerrainY(t,c),low=centreG;foreach(var o in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back})low=Mathf.Min(low,TerrainY(t,c+o*rad));
    float gc=b.min.y-centreG,gm=b.min.y-low;
    if(gc>.1f||gm>.22f){float drop=gm+.04f;worst=Mathf.Max(worst,drop);u.position+=Vector3.down*drop;moved++;EditorUtility.SetDirty(u);}}
   log.AppendLine($"FOLIAGE25 {seen} plants, {moved} lowered onto the ground under their own footprint (largest drop {worst:F2} m)");}
  // the V23 open grass that stands on (or straddles the edge of) a drive, a footpath or a road shoulder - with the drives now
  // painted into the ground, a clump there reads as grass growing on the drive
  public static void Grass25(){Open();Grass25Stage();Save("grass25");}
  static void Grass25Stage(){bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   var roads=county.transform.Find("V19 county roads");var late=county.LateRoad?county.LateRoad.transform:null;var holder=county.transform.Find("V23 open grass");int gone=0,seen=0;
   if(holder)foreach(var u in holder.Cast<Transform>().ToList()){seen++;var b=RBounds(u);float top=b.max.y+3;float rad=Mathf.Min(Mathf.Max(b.extents.x,b.extents.z)*.6f,.7f);bool onRoad=false;
     foreach(var o in new[]{Vector3.zero,Vector3.right,Vector3.left,Vector3.forward,Vector3.back}){var q=new Vector3(b.center.x,b.min.y,b.center.z)+o*rad;if(GroundHit(q,top,u,out var h)&&Kind(h.collider,roads,late)=="road")onRoad=true;}
     if(onRoad){Object.DestroyImmediate(u.gameObject);gone++;}}
   if(county.LateRoad)county.LateRoad.SetActive(lateWas);
   log.AppendLine($"GRASS25 {gone} of {seen} open-grass clumps were on a drive, footpath or shoulder - removed");}
  // the same views as RoadProbe25, from eye height, after the paint
  public static void Drives25Look(){Open();Drives25Shots();}
  static void Drives25Shots(){var dir=Path.Combine(Work,"Audit","v25roads");Directory.CreateDirectory(dir);var n0=NeutralLight();var cam=AuditCam();cam.fieldOfView=62;cam.aspect=16f/9f;
   try{foreach(var p in county.Properties){var R=p.ApproachRoute;if(R==null||R.Length<3)continue;
     foreach(var (tag,pos,look) in new[]{("a-mouth",R[0]+(R[0]-R[2]).normalized*5+Vector3.up*1.6f,R[R.Length-1]),("b-mid",R[R.Length/3]+Vector3.up*1.6f,R[R.Length-1]),("c-house",R[R.Length-2]+Vector3.up*1.6f,R[0]),("d-high",R[R.Length/2]+Vector3.up*9f+(R[0]-R[R.Length-1]).normalized*6,R[R.Length/2])}){
      cam.transform.position=pos;cam.transform.LookAt(look+Vector3.up*(tag=="d-high"?0:.4f));Shoot(cam,Path.Combine(dir,$"new-p{p.Index}-{tag}.jpg"),960,540);}}
    var line=county.Route.Where(x=>x).Select(x=>x.position).ToList();int k=0;for(int i=0;i+1<line.Count;i+=Mathf.Max(1,line.Count/6)){var a=line[i];var b=line[i+1];var fw=b-a;fw.y=0;fw.Normalize();var side=Vector3.Cross(Vector3.up,fw);
     cam.transform.position=a+side*1.3f+Vector3.up*1.25f;cam.transform.rotation=Quaternion.LookRotation(fw+Vector3.down*.06f,Vector3.up);Shoot(cam,Path.Combine(dir,$"new-road-{k++:00}.jpg"),960,540);}
   } finally {EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);}}
 }
}
