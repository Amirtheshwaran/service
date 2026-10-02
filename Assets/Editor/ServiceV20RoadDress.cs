using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V20 road dressing, on top of the V19 road (geometry, gates and drives untouched):
 //  - worn asphalt: the paint faded unevenly and darker wheel paths (Sources/V20/road_worn.py),
 //  - shallow drainage ditches along both verges of the county road, broken at the depot, the barricade and every
 //    drive mouth (where a drive would cross on a culvert); anything standing in the ditch band is set back down,
 //  - a line of timber utility poles with their own sagging wires down the west verge (Electrical Powerline Pole,
 //    tiedtke, CC-BY), one span (27 m) apart, each turned to face the next.
 public static partial class ServiceV19Rebuild {
  const string PoleDir="Assets/ServiceArt/V20/Powerline";
  public static void RoadDress(){Open();RoadDressStage();Save("road-dress");Physics.SyncTransforms();ServiceBuild.RebakeOpenDoors(county);Save("road-dress-nav");RoadLook();}
  static void RoadDressStage(){
   AssetDatabase.Refresh();var t=county.GetComponentInChildren<Terrain>();var td=t.terrainData;var o=t.transform.position;var size=td.size;
   // 1. worn asphalt
   var asphalt=AssetDatabase.LoadAssetAtPath<Material>($"{V19}/Road/V19 county asphalt.mat");var worn=PropTex("Assets/ServiceArt/V20/Road/county_asphalt_worn.png",false,true,2048);
   if(asphalt&&worn){asphalt.SetTexture("_BaseMap",worn);asphalt.SetFloat("_Smoothness",.42f);EditorUtility.SetDirty(asphalt);log.AppendLine("DRESS asphalt -> worn");}
   var main=Centre(MainRoad,t,.8f,9);
   var gates=county.Properties.Where(p=>p.Index!=2).Select(p=>{Nearest(main,p.Gate.position,out var gd,out var gs,out var gy);return gs;}).ToList();
   bool NearDrive(float s,float margin)=>gates.Any(g=>Mathf.Abs(s-g)<margin);
   // 2. ditches
   int res=td.heightmapResolution;float spacing=size.x/(res-1);log.AppendLine($"DRESS terrain {size} heightmap {res} ({spacing:F2} m per sample)");
   float a0=MainHalf+Verge+.35f,a1=a0+3.6f,depth=.42f;
   // remember what stands near the road so it can be set back down on the new ground
   var containers=new[]{"Winter woodland — bare trunks and mist","Forest understory thickets","Scanned woodland details","V17 forest floor — Poly Haven scans (CC0)","Storm verge planting"};
   var movers=new List<(Transform tr,float lift)>();
   foreach(var cname in containers){var c=county.transform.Find(cname);if(!c)continue;foreach(Transform x in c){Nearest(main,x.position,out var d,out var s,out var y);if(d>a0-.2f&&d<a1+.3f)movers.Add((x,x.position.y-TerrainY(t,x.position)));}}
   foreach(var p in county.Properties)foreach(var x in p.GetComponentsInChildren<Transform>(true).Where(x=>x&&x.parent==p.transform&&x.name.StartsWith("DecoBush"))){Nearest(main,x.position,out var d,out var s,out var y);if(d>a0-.2f&&d<a1+.3f)movers.Add((x,x.position.y-TerrainY(t,x.position)));}
   // street blades and other roadside signs in the band ride down with the ground too
   foreach(var tm in county.GetComponentsInChildren<TextMesh>(true)){var sign=tm.transform.parent?tm.transform.parent:tm.transform;Nearest(main,sign.position,out var d,out var s2,out var y2);if(d>a0-.2f&&d<a1+.3f&&!movers.Any(m=>m.tr==sign))movers.Add((sign,sign.position.y-TerrainY(t,sign.position)));}
   if(spacing<=1.3f){
    float minX=main.P.Min(p=>p.x)-a1-2,maxX=main.P.Max(p=>p.x)+a1+2,minZ=main.P.Min(p=>p.z)-a1-2,maxZ=main.P.Max(p=>p.z)+a1+2;
    int x0=Mathf.Clamp(Mathf.FloorToInt((minX-o.x)/size.x*(res-1)),0,res-1),x1=Mathf.Clamp(Mathf.CeilToInt((maxX-o.x)/size.x*(res-1)),0,res-1);
    int z0=Mathf.Clamp(Mathf.FloorToInt((minZ-o.z)/size.z*(res-1)),0,res-1),z1=Mathf.Clamp(Mathf.CeilToInt((maxZ-o.z)/size.z*(res-1)),0,res-1);
    var h=td.GetHeights(x0,z0,x1-x0+1,z1-z0+1);int changed=0;
    for(int zi=0;zi<=z1-z0;zi++)for(int xi=0;xi<=x1-x0;xi++){
     var w=new Vector3(o.x+(x0+xi)/(float)(res-1)*size.x,0,o.z+(z0+zi)/(float)(res-1)*size.z);Nearest(main,w,out var d,out var s,out var y);
     if(d<a0||d>a1||s<16||s>main.Length-10)continue;
     // taper the ditch out over 6 m either side of a drive mouth instead of stopping dead
     float g=1;foreach(var gs in gates){float dd=Mathf.Abs(s-gs);if(dd<12)g=Mathf.Min(g,Mathf.SmoothStep(0,1,(dd-6)/6f));}
     if(g<=0)continue;float u=(d-a0)/(a1-a0);float dep=depth*Mathf.Pow(Mathf.Sin(u*Mathf.PI),.8f)*g;
     h[zi,xi]=h[zi,xi]-dep/size.y;changed++;}
    td.SetHeights(x0,z0,h);log.AppendLine($"DRESS ditches: {changed} heightmap samples lowered up to {depth} m");
    // terrain trees in the band follow the ground
    var inst=td.treeInstances;int moved=0;for(int i=0;i<inst.Length;i++){var wp=Vector3.Scale(inst[i].position,size)+o;Nearest(main,wp,out var d,out var s,out var y);if(d<a0-.5f||d>a1+.5f)continue;var p2=inst[i].position;p2.y=(t.SampleHeight(wp))/size.y;inst[i].position=p2;moved++;}td.treeInstances=inst;
    foreach(var (tr,lift) in movers){var p=tr.position;p.y=TerrainY(t,p)+lift;tr.position=p;}
    log.AppendLine($"DRESS re-grounded {movers.Count} objects and {moved} terrain trees in the ditch band");
   }else log.AppendLine("DRESS ditches skipped: heightmap too coarse for a 3.6 m ditch");
   // 3. utility poles down the west verge
   var old=county.transform.Find("V20 utility poles");if(old)Object.DestroyImmediate(old.gameObject);
   var imp=(ModelImporter)AssetImporter.GetAtPath(PoleDir+"/Powerline_V003.fbx");if(imp){imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importAnimation=false;imp.importCameras=imp.importLights=false;imp.SaveAndReimport();}
   var fbx=AssetDatabase.LoadAssetAtPath<GameObject>(PoleDir+"/Powerline_V003.fbx");if(!fbx){log.AppendLine("DRESS no pole FBX");return;}
   // The FBX is authored in centimetres: measure the imported pole and correct the import scale to an 11 m pole.
   {var probe=(GameObject)PrefabUtility.InstantiatePrefab(fbx);var pb=BoundsOf(probe);Object.DestroyImmediate(probe);var e=new[]{pb.size.x,pb.size.y,pb.size.z}.OrderByDescending(v=>v).ToArray();float height=e[1];
    if(imp&&Mathf.Abs(height-11f)>.5f){imp.globalScale*=11f/height;imp.SaveAndReimport();fbx=AssetDatabase.LoadAssetAtPath<GameObject>(PoleDir+"/Powerline_V003.fbx");log.AppendLine($"DRESS pole import scale -> {imp.globalScale:F3} (was {height:F3} m tall)");}}
   var mpath=PoleDir+"/Powerline.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(mpath);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,mpath);}
   mat.SetTexture("_BaseMap",PropTex(PoleDir+"/powerline_basecolor.png",false,true,1024));var n=PropTex(PoleDir+"/powerline_normal.png",true,false,1024);if(n){mat.SetTexture("_BumpMap",n);mat.EnableKeyword("_NORMALMAP");}mat.SetFloat("_Smoothness",.18f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);
   var holder=new GameObject("V20 utility poles").transform;holder.SetParent(county.transform,false);
   const float span=27.2f;var spots=new List<Vector3>();float sPos=10;
   while(sPos<main.Length-6){float s=sPos;int guard=0;while(NearDrive(s,6)&&guard++<10)s+=2.5f;var c=At(main,s,out var right);var at=c-right*(a1+1.2f);at.y=GroundAt(at);spots.Add(at);sPos=s+span;}
   for(int i=0;i<spots.Count;i++){
    var at=spots[i];var next=i<spots.Count-1?spots[i+1]:at+(at-spots[i-1]);var dir=next-at;dir.y=0;float dist=dir.magnitude;
    var pole=new GameObject($"Utility pole {i:00} (tiedtke, CC-BY)");pole.transform.SetParent(holder,false);var model=(GameObject)PrefabUtility.InstantiatePrefab(fbx,pole.transform);model.name="Pole model";
    foreach(var r in pole.GetComponentsInChildren<Renderer>()){r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
    // The model's axes: its longest extent is the wire span, the next is the pole's height; the base sits at the
    // model origin, so each axis points from the origin toward the bulk of the geometry. Stand it up and aim the span.
    pole.transform.position=Vector3.zero;pole.transform.rotation=Quaternion.identity;pole.transform.localScale=Vector3.one;var b=BoundsOf(pole);
    Vector3[] axes={Vector3.right,Vector3.up,Vector3.forward};float[] ext={b.size.x,b.size.y,b.size.z};var order=new[]{0,1,2}.OrderByDescending(k2=>ext[k2]).ToArray();
    var w=axes[order[0]]*Mathf.Sign(Vector3.Dot(b.center,axes[order[0]]));var hgt=axes[order[1]]*Mathf.Sign(Vector3.Dot(b.center,axes[order[1]]));
    var rot=Quaternion.LookRotation(dir.normalized,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(w,hgt));
    float k=Mathf.Clamp(dist/span,.85f,1.2f);if(i<spots.Count-1){var ax=new Vector3(Mathf.Abs(w.x),Mathf.Abs(w.y),Mathf.Abs(w.z));pole.transform.localScale=Vector3.one+ax*(k-1);}
    pole.transform.rotation=rot;pole.transform.position=at;
    var colB=pole.AddComponent<BoxCollider>();var hb=new Vector3(Mathf.Abs(hgt.x),Mathf.Abs(hgt.y),Mathf.Abs(hgt.z));colB.center=hgt*(ext[order[1]]*.5f);colB.size=Vector3.Scale(Vector3.one-hb,new Vector3(.38f,.38f,.38f)/(1f))+hb*ext[order[1]];
    b=BoundsOf(pole);pole.transform.position+=Vector3.up*(at.y-b.min.y);pole.isStatic=true;
   }
   log.AppendLine($"DRESS {spots.Count} utility poles, span {span} m, down the west verge");
  }
 }
}
