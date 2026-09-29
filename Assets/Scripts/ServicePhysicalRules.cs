using UnityEngine;
namespace ServiceGameV2 {
 public static class ServiceBuildingSafety {
  public static bool AnyBuildingContains(this ServiceProperty[] properties,Vector3 point,float clearance){foreach(var p in properties){var b=p.InteriorBounds;b.Expand(new Vector3(clearance*2,0,clearance*2));point.y=b.center.y;if(b.Contains(point))return true;}return false;}
  // Keeps the car out of house footprints, but never traps a car that is already inside one: moving outward is always allowed.
  public static bool BlocksVehicle(this ServiceProperty[] properties,Vector3 from,Vector3 to,float clearance){foreach(var p in properties){var b=p.InteriorBounds;b.Expand(new Vector3(clearance*2,0,clearance*2));from.y=to.y=b.center.y;if(!b.Contains(to))continue;if(!b.Contains(from))return true;var c=b.center;if((to-c).sqrMagnitude<(from-c).sqrMagnitude)return true;}return false;}
 }
}
