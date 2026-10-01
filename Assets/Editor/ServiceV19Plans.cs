using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2.Editor {
 // Collision floor plans of every delivery house: what is wall, window, doorway, stair, existing furniture and open floor,
 // on each storey the player uses, with the walking path from the front door to the delivery table.
 public static partial class ServiceV19Rebuild {
  public static void Plans(){Open();PlansStage();}
  static void PlansStage(){
   var outDir=Path.Combine(Work,"Audit","plans");Directory.CreateDirectory(outDir);
   Physics.SyncTransforms();var nav=NavMesh.AddNavMeshData(county.Navigation);
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   try{
    foreach(var p in county.Properties.OrderBy(x=>x.Index)){
     var b=p.InteriorBounds;if(b.size.sqrMagnitude<1)continue;
     var outward=p.Door.position-b.center;outward.y=0;outward.Normalize();
     var floors=new List<float>();
     void AddFloor(Vector3 probe){if(Physics.Raycast(probe+Vector3.up*.8f,Vector3.down,out var h,3,~0,QueryTriggerInteraction.Ignore)&&!floors.Any(f=>Mathf.Abs(f-h.point.y)<1.2f))floors.Add(h.point.y);}
     AddFloor(p.Door.position-outward*1.2f);AddFloor(p.TableApproach.position);
     var path=new NavMeshPath();Vector3[] corners=new Vector3[0];
     if(NavMesh.SamplePosition(p.Door.position-outward*.9f,out var a,2,NavMesh.AllAreas)&&NavMesh.SamplePosition(p.TableApproach.position,out var t,2,NavMesh.AllAreas)&&NavMesh.CalculatePath(a.position,t.position,NavMesh.AllAreas,path))corners=path.corners;
     foreach(var f in floors.OrderBy(v=>v))Plan(p,f,b,corners,outDir);
    }
   }finally{nav.Remove();if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
  }
  static bool Blocked(Vector3 c,float half,out Collider hit){var cols=Physics.OverlapBox(c,new Vector3(half,.04f,half),Quaternion.identity,~0,QueryTriggerInteraction.Ignore);hit=cols.FirstOrDefault(x=>!(x is TerrainCollider)&&!(x is CharacterController));return hit!=null;}
  static void Plan(ServiceProperty p,float floor,Bounds b,Vector3[] path,string outDir){
   const float cell=.25f;float x0=b.min.x-1,x1=b.max.x+1,z0=b.min.z-1,z1=b.max.z+1;int nx=Mathf.CeilToInt((x1-x0)/cell),nz=Mathf.CeilToInt((z1-z0)/cell);
   var grid=new char[nz,nx];var owner=new Dictionary<string,int>();
   for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){
    float x=x0+(ix+.5f)*cell,z=z0+(iz+.5f)*cell;char ch=' ';
    bool hasFloor=Physics.Raycast(new Vector3(x,floor+.6f,z),Vector3.down,out var fh,1.2f,~0,QueryTriggerInteraction.Ignore)&&!(fh.collider is TerrainCollider);
    float fy=hasFloor?fh.point.y:floor;
    bool low=Blocked(new Vector3(x,floor+.35f,z),cell*.45f,out var lowHit),mid=Blocked(new Vector3(x,floor+1.45f,z),cell*.45f,out var midHit),high=Blocked(new Vector3(x,floor+2.15f,z),cell*.45f,out var _);
    bool structural(Collider c)=>c&&p.Building&&c.transform.IsChildOf(p.Building);
    bool door(Collider c)=>c&&p.DoorPanel&&c.transform.IsChildOf(p.DoorPanel);
    if(door(lowHit)||door(midHit))ch='+';
    else if(low&&mid&&(structural(lowHit)||structural(midHit)))ch='#';
    else if(low&&!mid&&structural(lowHit)&&high)ch='w';
    else if((low&&!structural(lowHit))||(mid&&!structural(midHit))){var c=lowHit&&!structural(lowHit)?lowHit:midHit;var root=c.transform;while(root.parent&&root.parent!=p.transform&&!(root.parent.GetComponent<ServiceProperty>()))root=root.parent;var key=root.name;if(!owner.ContainsKey(key))owner[key]=owner.Count;ch=(char)('a'+owner[key]%26);}
    else if(hasFloor&&Mathf.Abs(fy-floor)<.12f)ch='.';
    else if(hasFloor&&fy>floor+.12f&&fy<floor+2.2f)ch='s';
    else if(hasFloor&&fy<floor-.12f)ch='v';
    grid[iz,ix]=ch;
   }
   void Mark(Vector3 w,char c){int ix=Mathf.FloorToInt((w.x-x0)/cell),iz=Mathf.FloorToInt((w.z-z0)/cell);if(ix>=0&&ix<nx&&iz>=0&&iz<nz)grid[iz,ix]=c;}
   for(int i=1;i<path.Length;i++){if(Mathf.Abs(path[i].y-floor)>1.3f&&Mathf.Abs(path[i-1].y-floor)>1.3f)continue;float len=Vector3.Distance(path[i-1],path[i]);for(float s=0;s<len;s+=cell*.5f){var q=Vector3.Lerp(path[i-1],path[i],s/len);if(Mathf.Abs(q.y-floor)<1.3f&&grid[Mathf.Clamp(Mathf.FloorToInt((q.z-z0)/cell),0,nz-1),Mathf.Clamp(Mathf.FloorToInt((q.x-x0)/cell),0,nx-1)]=='.')Mark(q,'~');}}
   if(Mathf.Abs(p.Door.position.y-floor)<1.3f)Mark(p.Door.position,'D');if(Mathf.Abs(p.DeliveryPoint.position.y-floor)<1.6f)Mark(p.DeliveryPoint.position,'T');if(Mathf.Abs(p.TableApproach.position.y-floor)<1.3f)Mark(p.TableApproach.position,'P');
   var sb=new StringBuilder();
   sb.AppendLine($"PROPERTY {p.Index} {p.Address} ({ServiceScript.For(p.Index)?.Docket}) building={p.Building?.name} floor y={floor:F2}");
   sb.AppendLine($"Grid cell = {cell} m. Column i -> x = {x0:F2} + (i+0.5)*{cell}; row j (TOP row is the NORTH/+Z end) -> z = {z1:F2} - (j+0.5)*{cell}.");
   sb.AppendLine("Legend: # wall  w window (wall below 1.45 m, open at eye height)  + front door leaf  . open floor at this storey  s higher floor/stairs  v lower floor  ~ walking path door->table  D front door  P where the player stands at the table  T delivery table spot  letters = existing furniture/props (key below)  blank = outside/no floor");
   sb.AppendLine($"Front door at ({p.Door.position.x:F2},{p.Door.position.z:F2}); delivery point ({p.DeliveryPoint.position.x:F2},{p.DeliveryPoint.position.z:F2}); player at table ({p.TableApproach.position.x:F2},{p.TableApproach.position.z:F2}).");
   sb.AppendLine("Props key: "+string.Join("; ",owner.OrderBy(k=>k.Value).Select(k=>$"{(char)('a'+k.Value%26)}={k.Key}")));
   sb.Append("      ");for(int ix=0;ix<nx;ix++)sb.Append(ix%8==0?'|':' ');sb.AppendLine();
   for(int iz=nz-1;iz>=0;iz--){float z=z0+(iz+.5f)*cell;sb.Append(z.ToString("F1").PadLeft(6));for(int ix=0;ix<nx;ix++)sb.Append(grid[iz,ix]);sb.AppendLine();}
   sb.Append("  x=  ");for(int ix=0;ix<nx;ix+=8){var lab=(x0+(ix+.5f)*cell).ToString("F1");sb.Append(lab.PadRight(8).Substring(0,8));}sb.AppendLine();
   File.WriteAllText(Path.Combine(outDir,$"p{p.Index}-floor{floor:F1}.txt"),sb.ToString());
   // PNG version (10 px per cell).
   int sc=10;var tex=new Texture2D(nx*sc,nz*sc);var cols=new Dictionary<char,Color>{{' ',new Color(.08f,.1f,.08f)},{'.',new Color(.55f,.5f,.42f)},{'#',Color.black},{'w',new Color(.3f,.55f,.95f)},{'+',new Color(.1f,.8f,.2f)},{'s',new Color(.75f,.72f,.6f)},{'v',new Color(.35f,.32f,.28f)},{'~',new Color(.2f,.45f,1f)},{'D',Color.green},{'T',Color.red},{'P',new Color(1,.5f,0)}};
   for(int iz=0;iz<nz;iz++)for(int ix=0;ix<nx;ix++){var ch=grid[iz,ix];var c=cols.TryGetValue(ch,out var cc)?cc:Color.HSVToRGB(((ch-'a')*.137f)%1,.7f,.95f);for(int dy=0;dy<sc;dy++)for(int dx=0;dx<sc;dx++){bool line=((ix*sc+dx)%(sc*4)==0)||((iz*sc+dy)%(sc*4)==0);tex.SetPixel(ix*sc+dx,iz*sc+dy,line?Color.Lerp(c,Color.white,.25f):c);}}
   tex.Apply();File.WriteAllBytes(Path.Combine(outDir,$"p{p.Index}-floor{floor:F1}.png"),tex.EncodeToPNG());Object.DestroyImmediate(tex);
  }
 }
}
