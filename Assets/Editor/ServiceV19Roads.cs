using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 // V19 ground and roads. One county road laid along hand-placed control points (EasyRoads3D textures; the free
 // edition refuses scripted road creation), gravel drives with a pull-off at every house, a gravel depot yard,
 // the closed survey road beyond the barricade, terrain levelled under every surface, verges that fray into
 // the forest floor, and the woods cleared back from each corridor.
 public static partial class ServiceV19Rebuild {
  const string V19="Assets/ServiceArt/V19";
  // Main road: depot yard to the survey barricade. Chosen to pass each house's pull-off at 9-14 m.
  static readonly Vector2[] MainRoad={new Vector2(0,4),new Vector2(0,30),new Vector2(0,62),new Vector2(2,90),new Vector2(8,115),new Vector2(18,140),new Vector2(28,163),new Vector2(35,187),new Vector2(37,212),new Vector2(36,234),new Vector2(31,256),new Vector2(23,276),new Vector2(12,297),new Vector2(2,318),new Vector2(-3,340),new Vector2(-5,362),new Vector2(-5,380),new Vector2(-4.5f,392)};
  // County Route 9 beyond the survey line (night three only): older, narrower, unmarked.
  static readonly Vector2[] LateRoadPath={new Vector2(-4.5f,392),new Vector2(-4.5f,410),new Vector2(-5.5f,428),new Vector2(-7.5f,446),new Vector2(-9,461),new Vector2(-10,474),new Vector2(-10,484)};
  const float MainHalf=3.1f,LateHalf=2.6f,DriveHalf=1.55f,PullHalf=2.9f,Verge=1.35f;

  public static void Ground(){Open();GroundStage();Save("ground");}
  public static void Roads(){Open();RoadsStage();Save("roads");}

  // ---------- ground ----------
  static void GroundStage(){
   AssetDatabase.Refresh();
   var terrain=county.GetComponentInChildren<Terrain>();var td=terrain.terrainData;var layers=td.terrainLayers.ToArray();
   (string tex,float tile)[] plan={("forest_leaves_04",2.8f),("forest_ground_04",3.2f),("mud_forest",3.6f)};
   for(int i=0;i<plan.Length&&i<layers.Length;i++){layers[i]=GroundLayer(plan[i].tex,plan[i].tile);log.AppendLine($"GROUND layer {i} -> {plan[i].tex} tile {plan[i].tile}");}
   td.terrainLayers=layers;EditorUtility.SetDirty(td);
  }
  static TerrainLayer GroundLayer(string name,float tile){
   var path=$"{V19}/Ground/V19 {name}.terrainlayer";var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);}
   l.diffuseTexture=PropTex($"{V19}/Ground/{name}_diff.png",false,true);l.normalMapTexture=PropTex($"{V19}/Ground/{name}_normal.png",true,false);l.maskMapTexture=PropTex($"{V19}/Ground/{name}_mask.png",false,false);
   l.tileSize=new Vector2(tile,tile);l.normalScale=1;l.metallic=0;l.smoothness=1;EditorUtility.SetDirty(l);return l;
  }

  // ---------- geometry helpers ----------
  sealed class Line{public List<Vector3> P=new List<Vector3>();public List<float> S=new List<float>();public float Length=>S.Count>0?S[S.Count-1]:0;}
  static List<Vector2> CatmullRom(IList<Vector2> c,float step){
   var pts=new List<Vector2>();var ext=new List<Vector2>{c[0]*2-c[1]};ext.AddRange(c);ext.Add(c[c.Count-1]*2-c[c.Count-2]);
   for(int i=1;i<ext.Count-2;i++){
    Vector2 p0=ext[i-1],p1=ext[i],p2=ext[i+1],p3=ext[i+2];
    float t01=Mathf.Pow(Vector2.Distance(p0,p1),.5f),t12=Mathf.Pow(Vector2.Distance(p1,p2),.5f),t23=Mathf.Pow(Vector2.Distance(p2,p3),.5f);
    Vector2 m1=(p2-p1+t12*((p1-p0)/Mathf.Max(t01,1e-4f)-(p2-p0)/Mathf.Max(t01+t12,1e-4f)));
    Vector2 m2=(p2-p1+t12*((p3-p2)/Mathf.Max(t23,1e-4f)-(p3-p1)/Mathf.Max(t12+t23,1e-4f)));
    int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(p1,p2)/step));
    for(int k=0;k<n;k++){float t=k/(float)n,t2=t*t,t3=t2*t;pts.Add((2*t3-3*t2+1)*p1+(t3-2*t2+t)*m1+(-2*t3+3*t2)*p2+(t3-t2)*m2);}
   }
   pts.Add(c[c.Count-1]);return pts;
  }
  static float TerrainY(Terrain t,Vector3 p)=>t.SampleHeight(p)+t.transform.position.y;
  static Line Centre(IList<Vector2> ctrl,Terrain t,float step,float window,float? startY=null){
   var xz=CatmullRom(ctrl,step);var raw=xz.Select(v=>TerrainY(t,new Vector3(v.x,0,v.y))).ToList();
   var line=new Line();float s=0;for(int i=0;i<xz.Count;i++){if(i>0)s+=Vector2.Distance(xz[i-1],xz[i]);line.S.Add(s);}
   for(int i=0;i<xz.Count;i++){float sum=0,wsum=0;for(int j=0;j<xz.Count;j++){float d=Mathf.Abs(line.S[j]-line.S[i]);if(d>window)continue;float w=1-d/window;sum+=raw[j]*w;wsum+=w;}float y=sum/wsum;
    if(startY.HasValue){float k=Mathf.Clamp01(line.S[i]/6f);y=Mathf.Lerp(startY.Value,y,k);}
    line.P.Add(new Vector3(xz[i].x,y,xz[i].y));}
   return line;
  }
  static void Nearest(Line l,Vector3 p,out float dist,out float s,out float y){
   dist=float.MaxValue;s=0;y=0;for(int i=1;i<l.P.Count;i++){var a=l.P[i-1];var b=l.P[i];var ab=new Vector2(b.x-a.x,b.z-a.z);var ap=new Vector2(p.x-a.x,p.z-a.z);float t=Mathf.Clamp01(Vector2.Dot(ap,ab)/Mathf.Max(ab.sqrMagnitude,1e-6f));float d=(ap-ab*t).magnitude;if(d<dist){dist=d;s=Mathf.Lerp(l.S[i-1],l.S[i],t);y=Mathf.Lerp(a.y,b.y,t);}}
  }
  static Vector3 At(Line l,float s,out Vector3 right){int i=1;while(i<l.S.Count-1&&l.S[i]<s)i++;float t=Mathf.InverseLerp(l.S[i-1],l.S[i],s);var p=Vector3.Lerp(l.P[i-1],l.P[i],t);var f=l.P[i]-l.P[i-1];f.y=0;f.Normalize();right=new Vector3(f.z,0,-f.x);return p;}
  // Level the terrain under a surface: flat (just below the surface) inside the verge, eased back to the natural ground outside it.
  static void Level(Terrain t,Line l,Func<float,float> half,float falloff){
   var td=t.terrainData;int res=td.heightmapResolution;var size=td.size;var o=t.transform.position;
   float minX=l.P.Min(p=>p.x)-12-falloff,maxX=l.P.Max(p=>p.x)+12+falloff,minZ=l.P.Min(p=>p.z)-12-falloff,maxZ=l.P.Max(p=>p.z)+12+falloff;
   int x0=Mathf.Clamp(Mathf.FloorToInt((minX-o.x)/size.x*(res-1)),0,res-1),x1=Mathf.Clamp(Mathf.CeilToInt((maxX-o.x)/size.x*(res-1)),0,res-1);
   int z0=Mathf.Clamp(Mathf.FloorToInt((minZ-o.z)/size.z*(res-1)),0,res-1),z1=Mathf.Clamp(Mathf.CeilToInt((maxZ-o.z)/size.z*(res-1)),0,res-1);
   var h=td.GetHeights(x0,z0,x1-x0+1,z1-z0+1);int changed=0;
   for(int zi=0;zi<=z1-z0;zi++)for(int xi=0;xi<=x1-x0;xi++){
    var w=new Vector3(o.x+(x0+xi)/(float)(res-1)*size.x,0,o.z+(z0+zi)/(float)(res-1)*size.z);
    Nearest(l,w,out var d,out var s,out var y);float flat=half(s)+Verge;if(d>flat+falloff)continue;
    float target=(y-.035f-o.y)/size.y;float k=d<=flat?0:Mathf.SmoothStep(0,1,(d-flat)/falloff);h[zi,xi]=Mathf.Lerp(target,h[zi,xi],k);changed++;}
   td.SetHeights(x0,z0,h);log.AppendLine($"LEVEL {changed} terrain samples");
  }
  // Paint the dirt layer along a verge so asphalt and gravel blend into the forest floor.
  static void PaintVerge(Terrain t,Line l,Func<float,float> half,int layer,float width){
   var td=t.terrainData;int res=td.alphamapResolution;var size=td.size;var o=t.transform.position;var map=td.GetAlphamaps(0,0,res,res);int layers=map.GetLength(2);
   float minX=l.P.Min(p=>p.x)-10,maxX=l.P.Max(p=>p.x)+10,minZ=l.P.Min(p=>p.z)-10,maxZ=l.P.Max(p=>p.z)+10;
   for(int zi=Mathf.Max(0,(int)((minZ-o.z)/size.z*res));zi<Mathf.Min(res,(int)((maxZ-o.z)/size.z*res)+1);zi++)for(int xi=Mathf.Max(0,(int)((minX-o.x)/size.x*res));xi<Mathf.Min(res,(int)((maxX-o.x)/size.x*res)+1);xi++){
    var w=new Vector3(o.x+(xi+.5f)/res*size.x,0,o.z+(zi+.5f)/res*size.z);Nearest(l,w,out var d,out var s,out var _);float inner=half(s),outer=inner+Verge+width;if(d>outer)continue;
    float k=d<=inner+Verge?1:1-Mathf.SmoothStep(0,1,(d-inner-Verge)/width);k*=.85f;
    for(int c=0;c<layers;c++)map[zi,xi,c]*=1-k;map[zi,xi,layer]+=k;}
   td.SetAlphamaps(0,0,map);
  }
  // Ribbon mesh: strips across the road; u either spans the strip (texture already laid out across the road) or is world-tiled.
  sealed class Strip{public float A,B;public bool Relative;public float U0,U1;public float VTile;public bool WorldU;public float UTile;public float Lift;public bool Terrain;}
  static GameObject Ribbon(string name,Line l,Func<float,float> half,Strip[] strips,Material mat,Transform parent,Terrain t,bool collider,string surface){
   var verts=new List<Vector3>();var uvs=new List<Vector2>();var tris=new List<int>();
   foreach(var st0 in strips){int start=verts.Count;var st=st0.A<=st0.B?st0:new Strip{A=st0.B,B=st0.A,U0=st0.U1,U1=st0.U0,Relative=st0.Relative,VTile=st0.VTile,WorldU=st0.WorldU,UTile=st0.UTile,Lift=st0.Lift,Terrain=st0.Terrain};
    for(int i=0;i<l.P.Count;i++){var p=l.P[i];var f=(i<l.P.Count-1?l.P[i+1]-p:p-l.P[i-1]);f.y=0;f.Normalize();var r=new Vector3(f.z,0,-f.x);float h=half(l.S[i]);
     foreach(var (off,u) in new[]{(st.A,st.U0),(st.B,st.U1)}){float lat=st.Relative?off*h:(off<0?off-h:off+h)*1;var v=p+r*lat;float y=st.Terrain&&Mathf.Abs(off)>1.05f?TerrainY(t,v)+.012f:p.y+st.Lift;v.y=y;verts.Add(v);uvs.Add(new Vector2(st.WorldU?lat/st.UTile:u,l.S[i]/st.VTile));}}
    for(int i=0;i<l.P.Count-1;i++){int a=start+i*2;tris.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}}
   var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(verts);mesh.SetUVs(0,uvs);mesh.SetTriangles(tris,0);
   // Wind the faces upward.
   mesh.RecalculateNormals();var n=mesh.normals;if(n.Length>0&&n.Average(v=>v.y)<0){var tr=mesh.triangles;for(int i=0;i<tr.Length;i+=3){var x=tr[i+1];tr[i+1]=tr[i+2];tr[i+2]=x;}mesh.triangles=tr;mesh.RecalculateNormals();}
   mesh.RecalculateTangents();mesh.RecalculateBounds();
   var meshPath=$"{V19}/Road/{name}.asset";AssetDatabase.DeleteAsset(meshPath);AssetDatabase.CreateAsset(mesh,meshPath);
   var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=g.GetComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;g.isStatic=true;
   if(collider){g.AddComponent<MeshCollider>().sharedMesh=mesh;g.AddComponent<ServiceSurface>().Kind=surface;}
   return g;
  }
  static Material RoadMat(string name,string albedo,string normal,float smooth,bool clip,Vector2 scale){
   var path=$"{V19}/Road/{name}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   var a=PropTex(albedo,false,true,2048);m.SetTexture("_BaseMap",a);m.SetTextureScale("_BaseMap",scale);m.SetColor("_BaseColor",Color.white);
   var n=PropTex(normal,true,false,2048);if(n){m.SetTexture("_BumpMap",n);m.SetTextureScale("_BumpMap",scale);m.EnableKeyword("_NORMALMAP");}
   m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);
   if(clip){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.42f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
   m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
  }

  // ---------- roads ----------
  static void RoadsStage(){
   AssetDatabase.Refresh();Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,V19,"Road"));
   var t=county.GetComponentInChildren<Terrain>();var world=county.transform;
   // Old surfaces, driveway lamp cubes and the survey-point route go; everything below is rebuilt.
   foreach(var n in new[]{"Millbrook and Latigo","V19 county roads"}){var old=world.Find(n);if(old)Kill(old,"replaced by the V19 county road");}
   foreach(var tr in All().Where(x=>x&&(x.name.StartsWith("Garden path ")||x.name=="Vale gravel approach"||x.name=="Garden lamp post"||x.name=="Garden amber pool"||x.name=="North survey closure")).ToList())Kill(tr,"replaced by the V19 drive / closure");
   foreach(var p in county.Properties)foreach(var tr in p.GetComponentsInChildren<Transform>(true).Where(x=>x&&x.parent==p.transform&&x.name=="Prop_Lamp_E"&&x.position.y<2.2f&&(x.position-p.Door.position).magnitude>8).ToList())Kill(tr,"lamp head that sat on a cube post");
   var ER="Assets/EasyRoads3D/textures/roads";
   var asphalt=RoadMat("V19 county asphalt",$"{V19}/Road/V19 county asphalt.png",$"{ER}/road_N.png",.55f,false,Vector2.one);
   var closed=RoadMat("V19 closed road asphalt",$"{V19}/Ground/asphalt_02_diff.png",$"{V19}/Ground/asphalt_02_normal.png",.45f,false,Vector2.one);closed.SetColor("_BaseColor",new Color(.62f,.62f,.6f));
   var gravel=RoadMat("V19 gravel drive",$"{ER}/dirtRoad_A.tga",$"{ER}/dirtRoad_N.tga",.3f,true,Vector2.one);
   var yard=RoadMat("V19 depot yard gravel",$"{V19}/Ground/stony_dirt_path_diff.png",$"{V19}/Ground/stony_dirt_path_normal.png",.25f,false,Vector2.one);
   var holder=new GameObject("V19 county roads").transform;holder.SetParent(world,false);

   // Depot yard, then the county road over it.
   var yardLine=Centre(new[]{new Vector2(-1,-12),new Vector2(-1,9)},t,1,30);Func<float,float> yardHalf=s=>9.5f;
   Level(t,yardLine,yardHalf,6);
   var main=Centre(MainRoad,t,.8f,9,yardLine.P.Last().y);Func<float,float> mainHalf=s=>MainHalf;
   Level(t,main,mainHalf,6);
   var late=Centre(LateRoadPath,t,.8f,7,main.P.Last().y);Func<float,float> lateHalf=s=>LateHalf;
   Level(t,late,lateHalf,5);

   // Drives: from the road edge through the pull-off, then along the old approach to the foot of the steps.
   var drives=new List<(ServiceProperty p,Line l)>();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){
    var road=p.Index==2?late:main;Nearest(road,p.Gate.position,out var gd,out var gs,out var gy);var rp=At(road,gs,out var right);var side=Mathf.Sign(Vector3.Dot(p.Gate.position-rp,right));
    var start=rp+right*side*((p.Index==2?LateHalf:MainHalf)+.15f);
    var route=(p.ApproachRoute??new Vector3[0]).Select(v=>new Vector2(v.x,v.z)).ToList();
    var ctrl=new List<Vector2>{new Vector2(start.x,start.z),new Vector2(start.x,start.z)+new Vector2(right.x,right.z)*side*5f};
    var end=route.Count>0?route[route.Count-1]:new Vector2(p.Door.position.x,p.Door.position.z);
    var back=route.Count>1?(route[route.Count-2]-end).normalized:Vector2.zero;var stop=end+back*1.6f;
    for(int i=3;i<route.Count;i+=3){var q=route[i];if(Vector2.Distance(q,stop)<4||Vector2.Distance(q,ctrl.Last())<4)continue;if(Vector2.Distance(q,new Vector2(start.x,start.z))<9)continue;ctrl.Add(q);}
    ctrl.Add(stop);
    var line=Centre(ctrl,t,.6f,3,start.y);drives.Add((p,line));
    float len=line.Length;Func<float,float> dh=s=>Mathf.Lerp(PullHalf,DriveHalf,Mathf.SmoothStep(0,1,(s-6)/5f));
    Level(t,line,dh,3.5f);
    log.AppendLine($"DRIVE {p.Index} {p.Address}: {len:F0} m, pull-off at {start}");
   }
   // Re-sample heights after levelling so every mesh sits on the final ground.
   void Refit(Line l,float lift){for(int i=0;i<l.P.Count;i++){var v=l.P[i];v.y=TerrainY(t,v)+lift;l.P[i]=v;}}
   Refit(yardLine,.035f);Refit(main,.035f);Refit(late,.035f);foreach(var d in drives)Refit(d.l,.035f);

   var road2Lane=new[]{new Strip{A=-1,B=1,Relative=true,U0=0,U1=1,VTile=7f}};
   var verge=new[]{new Strip{A=-1.02f,B=-1.02f-Verge/MainHalf,Relative=true,U0=.24f,U1=0,VTile=3f,Terrain=true,Lift=-.004f},new Strip{A=1.02f,B=1.02f+Verge/MainHalf,Relative=true,U0=.24f,U1=0,VTile=3f,Terrain=true,Lift=-.004f}};
   Ribbon("V19 depot yard",yardLine,yardHalf,new[]{new Strip{A=-1,B=1,Relative=true,WorldU=true,UTile=4.5f,VTile=4.5f,Lift=-.01f}},yard,holder,t,true,"gravel");
   Ribbon("V19 depot yard edge",yardLine,yardHalf,new[]{new Strip{A=-1.0f,B=-1.14f,Relative=true,U0=.24f,U1=0,VTile=3,Terrain=true,Lift=-.012f},new Strip{A=1.0f,B=1.14f,Relative=true,U0=.24f,U1=0,VTile=3,Terrain=true,Lift=-.012f}},gravel,holder,t,false,"");
   Ribbon("V19 Millbrook Road and Latigo Trail",main,mainHalf,road2Lane,asphalt,holder,t,true,"stone");
   Ribbon("V19 county road verge",main,mainHalf,verge,gravel,holder,t,false,"");
   var lateHolder=county.LateRoad?county.LateRoad.transform:holder;
   var lateRoad=Ribbon("V19 County Route 9 (closed 1971)",late,lateHalf,new[]{new Strip{A=-1,B=1,Relative=true,WorldU=true,UTile=5f,VTile=5f}},closed,lateHolder,t,true,"stone");
   Ribbon("V19 County Route 9 verge",late,lateHalf,new[]{new Strip{A=-1.02f,B=-1.02f-Verge/LateHalf,Relative=true,U0=.24f,U1=0,VTile=3,Terrain=true,Lift=-.004f},new Strip{A=1.02f,B=1.02f+Verge/LateHalf,Relative=true,U0=.24f,U1=0,VTile=3,Terrain=true,Lift=-.004f}},gravel,lateHolder,t,false,"");
   foreach(var (p,l) in drives){Func<float,float> dh=s=>Mathf.Lerp(PullHalf,DriveHalf,Mathf.SmoothStep(0,1,(s-6)/5f));var parent=p.Index==2?lateHolder:holder;
    Ribbon($"V19 drive {p.Index} — {p.Address}",l,dh,new[]{new Strip{A=-1.18f,B=1.18f,Relative=true,U0=0,U1=1,VTile=3.2f,Lift=.012f}},gravel,parent,t,true,"gravel");}

   // Verges painted to the dirt layer.
   PaintVerge(t,main,mainHalf,1,2.2f);PaintVerge(t,late,lateHalf,1,2.6f);PaintVerge(t,yardLine,yardHalf,1,2);foreach(var (p,l) in drives)PaintVerge(t,l,s=>DriveHalf,1,1.2f);

   // Clear the woods back from every surface.
   var lines=new List<(Line l,Func<float,float> half)>{(main,mainHalf),(late,lateHalf),(yardLine,yardHalf)};foreach(var (p,l) in drives)lines.Add((l,s=>PullHalf));
   ClearCorridors(t,lines);

   // Pull-off (parking) at the start of each drive, and the walking route to the steps.
   foreach(var (p,l) in drives){var gate=At(l,Mathf.Min(4.5f,l.Length*.3f),out var _);var ahead=At(l,Mathf.Min(10,l.Length*.5f),out var __);p.Gate.position=gate+Vector3.up*.02f;var f=ahead-gate;f.y=0;p.Gate.rotation=Quaternion.LookRotation(f);
    var route=new Vector3[25];for(int i=0;i<25;i++)route[i]=At(l,Mathf.Lerp(3,l.Length,i/24f),out var ___)+Vector3.up*.02f;p.ApproachRoute=route;EditorUtility.SetDirty(p);}

   // Survey barricade at the end of the county road (nights one and two), the survey line, and the route map.
   Barricade(main,holder);
   if(county.LateThreshold){var end=main.P.Last();var dir=(end-main.P[main.P.Count-8]);dir.y=0;county.LateThreshold.SetPositionAndRotation(new Vector3(end.x,end.y,end.z)-dir.normalized*1.5f,Quaternion.LookRotation(dir));}
   var routeRoot=world.Find("V19 route");if(routeRoot)Object.DestroyImmediate(routeRoot.gameObject);routeRoot=new GameObject("V19 route").transform;routeRoot.SetParent(world,false);
   var route2=new List<Transform>();for(float s=0;s<=main.Length;s+=14){var pt=At(main,s,out var _);var g=new GameObject($"Route {route2.Count:00}").transform;g.SetParent(routeRoot,false);g.position=pt;route2.Add(g);}
   foreach(var old in county.Route.Where(r=>r&&r.name.StartsWith("Survey point")))Object.DestroyImmediate(old.gameObject);
   county.Route=route2.ToArray();EditorUtility.SetDirty(county);
   // Signs follow the road: street-name blades stand on the verge beside the lane.
   Relocate("MILLBROOK RD",main,24,-1);Relocate("LATIGO TRAIL",main,196,1);
   // Navigation over the new ground.
   Physics.SyncTransforms();ServiceBuild.RebakeV13(county);log.AppendLine("NAV rebaked");
  }
  static void Relocate(string text,Line main,float s,int side){
   var tm=county.GetComponentsInChildren<TextMesh>(true).FirstOrDefault(x=>x.text==text);if(!tm)return;var sign=tm.transform.parent;var p=At(main,s,out var right);var pos=p+right*side*(MainHalf+Verge+1.1f);
   // Text reads from its -forward side, so its forward points the way northbound traffic travels.
   var travel=At(main,s+5,out var _)-p;travel.y=0;var now=tm.transform.forward;now.y=0;
   sign.rotation=Quaternion.FromToRotation(now.normalized,travel.normalized)*sign.rotation;sign.position=new Vector3(pos.x,TerrainY(county.GetComponentInChildren<Terrain>(),pos)+1.9f,pos.z);
   log.AppendLine($"SIGN {text} moved to {sign.position}");
  }
  static void Barricade(Line main,Transform holder){
   var closure=new GameObject("North survey closure").transform;closure.SetParent(county.transform,false);
   var group=new GameObject("Barricade").transform;group.SetParent(closure,false);
   var end=At(main,main.Length-2.2f,out var right);var fwd=Vector3.Cross(right,Vector3.up);
   var striped=AssetDatabase.LoadAssetAtPath<GameObject>($"{PropRoot}/concrete_road_barrier/concrete_road_barrier.prefab");var plain=AssetDatabase.LoadAssetAtPath<GameObject>($"{PropRoot}/concrete_road_barrier_02/concrete_road_barrier_02.prefab");
   float[] offs={-2.45f,-.8f,.85f,2.5f};for(int i=0;i<offs.Length;i++){var pf=i%3==0?plain:striped;if(!pf)continue;var g=(GameObject)PrefabUtility.InstantiatePrefab(pf,group);var pos=end+right*offs[i]+fwd*(i%2==0?.12f:-.1f);pos.y=TerrainY(county.GetComponentInChildren<Terrain>(),pos)-.02f;g.transform.SetPositionAndRotation(pos,Quaternion.LookRotation(fwd)*Quaternion.Euler(0,(i-1.5f)*2.5f,0));}
   var ctl=closure.gameObject.AddComponent<ServiceReturnRoad>();ctl.Points=new Vector3[0];ctl.NorthClosure=group.gameObject;
   log.AppendLine("BARRICADE at "+end);
  }
  static float PolyDist(Line l,Vector3 p,out float s){Nearest(l,p,out var d,out s,out var _);return d;}
  static void ClearCorridors(Terrain t,List<(Line l,Func<float,float> half)> lines){
   var boxes=lines.Select(x=>new Rect(x.l.P.Min(v=>v.x)-30,x.l.P.Min(v=>v.z)-30,x.l.P.Max(v=>v.x)-x.l.P.Min(v=>v.x)+60,x.l.P.Max(v=>v.z)-x.l.P.Min(v=>v.z)+60)).ToArray();
   bool Near(Vector3 p,float margin){for(int i=0;i<lines.Count;i++){if(!boxes[i].Contains(new Vector2(p.x,p.z)))continue;float d=PolyDist(lines[i].l,p,out var s);if(d<lines[i].half(s)+margin)return true;}return false;}
   var containers=new[]{"Winter woodland — bare trunks and mist","Forest understory thickets","Scanned woodland details","V17 forest floor — Poly Haven scans (CC0)","Storm verge planting"};
   int trees=0,small=0;
   foreach(var cname in containers){var c=county.transform.Find(cname);if(!c)continue;
    foreach(Transform x in c.Cast<Transform>().ToList()){var n=x.name;bool big=n.StartsWith("PF Conifer")||n.StartsWith("TreeCreator_Tall")||n.StartsWith("V16 retained")||n.StartsWith("V17 Fallen")||n.StartsWith("dead_tree_trunk")||n.StartsWith("TreeCreator_Small")||n.StartsWith("Mixed woodland thicket")||n.StartsWith("Low woodland")||n.StartsWith("Rock");
     if(n.StartsWith("CTI Windzone"))continue;
     if(Near(x.position,big?Verge+2.4f:Verge+.9f)){Object.DestroyImmediate(x.gameObject);if(big)trees++;else small++;}}}
   foreach(var p in county.Properties)foreach(var x in p.GetComponentsInChildren<Transform>(true).Where(x=>x&&x.parent==p.transform&&(x.name.StartsWith("DecoBush"))).ToList())if(Near(x.position,Verge+1.2f)){Object.DestroyImmediate(x.gameObject);small++;}
   var td=t.terrainData;var inst=td.treeInstances.ToList();int before=inst.Count;
   inst=inst.Where(ti=>!Near(Vector3.Scale(ti.position,td.size)+t.transform.position,Verge+2.6f)).ToList();td.treeInstances=inst.ToArray();
   log.AppendLine($"CLEARED {trees} large and {small} small objects, {before-inst.Count} terrain trees from the road corridors");
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void GroundRoadsCatalog(){Open();GroundStage();Save("ground");RoadsStage();Save("roads");CatalogShelf();}
 }
}
