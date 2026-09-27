using UnityEngine;
namespace ServiceGameV2 {
 public static class ServiceBuildingSafety {
  public static bool AnyBuildingContains(this ServiceProperty[] properties,Vector3 point,float clearance){foreach(var p in properties){var b=p.InteriorBounds;b.Expand(new Vector3(clearance*2,0,clearance*2));point.y=b.center.y;if(b.Contains(point))return true;}return false;}
 }
}
