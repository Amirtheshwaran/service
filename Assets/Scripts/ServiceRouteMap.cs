using System.Collections.Generic;
using UnityEngine;
namespace ServiceGameV2 {
 public static class ServiceRouteMap {
  public static Vector3[] Road(CountyScene scene){
   foreach(var filter in scene.GetComponentsInChildren<MeshFilter>())if(filter.name=="Millbrook and Latigo"){
    var vertices=filter.sharedMesh.vertices;var result=new Vector3[vertices.Length/2];for(int i=0;i<result.Length;i++)result[i]=filter.transform.TransformPoint((vertices[i*2]+vertices[i*2+1])*.5f);return result;
   }
   var fallback=new Vector3[scene.Route.Length];for(int i=0;i<fallback.Length;i++)fallback[i]=scene.Route[i].position;return fallback;
  }
  public static Rect Bounds(IEnumerable<Vector3> points){
   float x=float.MaxValue,z=x,xx=float.MinValue,zz=xx;
   foreach(var p in points){x=Mathf.Min(x,p.x);z=Mathf.Min(z,p.z);xx=Mathf.Max(xx,p.x);zz=Mathf.Max(zz,p.z);}
   return new Rect(x-20,z-20,Mathf.Max(40,xx-x+40),Mathf.Max(40,zz-z+40));
  }
  public static Vector2 Project(Vector3 p,Rect world,Rect panel){
   float scale=Mathf.Min(panel.width/world.width,panel.height/world.height);
   return panel.center+new Vector2(p.x-world.center.x,world.center.y-p.z)*scale;
  }
 }
}
