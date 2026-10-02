using UnityEngine;
namespace ServiceGameV2 {
 // V20: the county has an edge. The chaos test walked south past the depot, off the terrain, and fell forever.
 // Invisible walls stand just inside the terrain's border (they stop the car too), and if anything ever drops below
 // the ground the walker is put back where it last stood on it.
 public sealed class ServiceWorldEdge:MonoBehaviour {
  ServiceDirector d;Vector3 lastSafe;float nextSafe;
  public int Rescues {get;private set;}
  public void Initialize(ServiceDirector director){
   d=director;var t=Terrain.activeTerrain;if(!t)return;var o=t.transform.position;var size=t.terrainData.size;
   var root=new GameObject("V20 world edge").transform;root.SetParent(transform,false);
   const float inset=2.5f,thick=2f,height=80f;float cx=o.x+size.x*.5f,cz=o.z+size.z*.5f,y=o.y+height*.5f-10;
   void Wall(string n,Vector3 c,Vector3 s){var g=new GameObject(n);g.transform.SetParent(root,false);g.transform.position=c;var b=g.AddComponent<BoxCollider>();b.size=s;g.isStatic=true;}
   Wall("South edge",new Vector3(cx,y,o.z+inset-thick*.5f),new Vector3(size.x+10,height,thick));
   Wall("North edge",new Vector3(cx,y,o.z+size.z-inset+thick*.5f),new Vector3(size.x+10,height,thick));
   Wall("West edge",new Vector3(o.x+inset-thick*.5f,y,cz),new Vector3(thick,height,size.z+10));
   Wall("East edge",new Vector3(o.x+size.x-inset+thick*.5f,y,cz),new Vector3(thick,height,size.z+10));
   if(d.Scene.Walker)lastSafe=d.Scene.Walker.transform.position;
  }
  void Update(){
   if(!d||d.Phase!=ServicePhase.Playing||d.Player.InCar||!d.Scene.Walker)return;var t=Terrain.activeTerrain;if(!t)return;
   var w=d.Scene.Walker.transform.position;float g=t.SampleHeight(w)+t.transform.position.y;
   if(w.y<g-4f){Rescues++;d.Player.RescueWalker(lastSafe);return;}
   if(Time.time>nextSafe&&d.Scene.Walker.isGrounded&&w.y>g-.6f){lastSafe=w;nextSafe=Time.time+.5f;}
  }
 }
}
