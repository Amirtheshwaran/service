using System.Collections.Generic;
using UnityEngine;
namespace ServiceGameV2 {
 // V21: walking through grass, ferns and low brush makes it rustle against your legs (NOX "tall grass movement" foley,
 // CC0). Every grass clump, fern, bush and thicket in the county goes into a 2 m grid once; each footstep asks whether the
 // walker's legs are inside one, and how tall it is.
 public sealed class ServiceFoliage:MonoBehaviour {
  struct Clump{public Vector2 c;public float r,h;public Renderer rd;}
  readonly Dictionary<long,List<Clump>> grid=new Dictionary<long,List<Clump>>();const float Cell=2f;
  public int Clumps {get;private set;}
  public int Rustles {get;set;}
  static long Key(int x,int z)=>((long)x<<32)^(uint)z;
  static bool Foliage(string n)=>n.StartsWith("Grass_")||n.StartsWith("TreeCreator_Bush")||n.StartsWith("fern")||n.StartsWith("DecoBush")||n.Contains("thicket")||n.StartsWith("Bush")||n.StartsWith("Shrub");
  ServiceDirector d;readonly List<Renderer> flattened=new List<Renderer>();readonly HashSet<Renderer> under=new HashSet<Renderer>();float nextFlatten;
  public int Flattened=>flattened.Count;
  public void Initialize(ServiceDirector director){d=director;
   foreach(var r in d.Scene.GetComponentsInChildren<Renderer>(true)){
    var n=r.name;var p=r.transform.parent;if(!Foliage(n)&&!(p&&Foliage(p.name)))continue;
    var b=r.bounds;if(b.size.y<.12f||b.size.y>4.5f)continue;
    var cl=new Clump{c=new Vector2(b.center.x,b.center.z),r=Mathf.Clamp(Mathf.Max(b.extents.x,b.extents.z)*.85f,.2f,3.5f),h=b.size.y,rd=r};
    int x0=Mathf.FloorToInt((cl.c.x-cl.r)/Cell),x1=Mathf.FloorToInt((cl.c.x+cl.r)/Cell),z0=Mathf.FloorToInt((cl.c.y-cl.r)/Cell),z1=Mathf.FloorToInt((cl.c.y+cl.r)/Cell);
    for(int x=x0;x<=x1;x++)for(int z=z0;z<=z1;z++){var k=Key(x,z);if(!grid.TryGetValue(k,out var list))grid[k]=list=new List<Clump>();list.Add(cl);}
    Clumps++;}
  }
  // V21: grass and brush the car is standing in would poke up through the cabin floor into the driver's view (the car
  // has no collision with foliage). While you sit in the car, whatever lies under its body is laid flat (not drawn),
  // and comes back once the car has moved off it.
  void LateUpdate(){
   if(!d||!d.Scene||!d.Scene.Car||Time.unscaledTime<nextFlatten)return;nextFlatten=Time.unscaledTime+.1f;
   under.Clear();
   if(d.Player&&d.Player.InCar&&(d.Phase==ServicePhase.Playing||d.Phase==ServicePhase.Paused)){
    var car=d.Scene.Car;var at=car.position;const float halfX=1.0f,halfZ=2.45f,reach=3.2f;
    int x0=Mathf.FloorToInt((at.x-reach)/Cell),x1=Mathf.FloorToInt((at.x+reach)/Cell),z0=Mathf.FloorToInt((at.z-reach)/Cell),z1=Mathf.FloorToInt((at.z+reach)/Cell);
    for(int x=x0;x<=x1;x++)for(int z=z0;z<=z1;z++){if(!grid.TryGetValue(Key(x,z),out var list))continue;
     foreach(var c in list){if(!c.rd)continue;var local=car.InverseTransformPoint(new Vector3(c.c.x,at.y,c.c.y));
      float dx=Mathf.Max(0,Mathf.Abs(local.x)-halfX),dz=Mathf.Max(0,Mathf.Abs(local.z)-halfZ);if(dx*dx+dz*dz<c.r*c.r*.36f)under.Add(c.rd);}}
   }
   for(int i=flattened.Count-1;i>=0;i--){var r=flattened[i];if(!under.Contains(r)){if(r)r.enabled=true;flattened.RemoveAt(i);}}
   foreach(var r in under)if(r.enabled&&!flattened.Contains(r)){r.enabled=false;flattened.Add(r);}
  }
  // How deep the walker is in foliage at this point: 0 = clear, up to 1 = waist-high brush.
  public float Depth(Vector3 at){
   if(!grid.TryGetValue(Key(Mathf.FloorToInt(at.x/Cell),Mathf.FloorToInt(at.z/Cell)),out var list))return 0;
   var q=new Vector2(at.x,at.z);float best=0;
   foreach(var c in list){float dd=(q-c.c).magnitude;if(dd>c.r+.22f)continue;best=Mathf.Max(best,Mathf.Clamp01(c.h/1.1f)*(1-Mathf.Clamp01((dd-c.r*.6f)/(c.r*.4f+.22f))*.5f));}
   return best;
  }
 }
}
