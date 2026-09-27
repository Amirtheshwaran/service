using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceStairZone:MonoBehaviour {
  public Bounds Area;
  public static bool Contains(Vector3 point){foreach(var zone in FindObjectsByType<ServiceStairZone>(FindObjectsSortMode.None))if(zone.Area.Contains(point+Vector3.up*.15f))return true;return false;}
 }
}
