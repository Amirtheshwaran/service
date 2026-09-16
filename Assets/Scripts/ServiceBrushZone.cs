using System.Collections.Generic;
using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceBrushZone:MonoBehaviour {
  public Vector3 Center;public float Radius=1.2f;
  public static readonly List<ServiceBrushZone> Active=new List<ServiceBrushZone>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){Active.Clear();}
  void OnEnable(){if(!Active.Contains(this))Active.Add(this);}
  void OnDisable(){Active.Remove(this);}
  public static bool Contact(Vector3 position,out Vector3 origin){
   foreach(var zone in Active){var delta=position-zone.Center;if(Mathf.Abs(delta.y)<2&&new Vector2(delta.x,delta.z).sqrMagnitude<zone.Radius*zone.Radius){origin=zone.Center;return true;}}
   origin=position;return false;
  }
 }
}
