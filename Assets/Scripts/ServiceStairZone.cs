using System.Collections.Generic;
using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceStairZone:MonoBehaviour {
  public Bounds Area;
  // Queried every walking frame; a registry avoids a whole-scene search (and its garbage) per frame.
  static readonly List<ServiceStairZone> zones=new List<ServiceStairZone>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){zones.Clear();}
  void OnEnable(){if(!zones.Contains(this))zones.Add(this);}
  void OnDisable(){zones.Remove(this);}
  public static bool Contains(Vector3 point){foreach(var zone in zones)if(zone.Area.Contains(point+Vector3.up*.15f))return true;return false;}
 }
}
