using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceGauge:MonoBehaviour {
  public Quaternion Rest=Quaternion.identity;public float StartAngle=130,EndAngle=-130;
  public static void Set(Transform pivot,float value){if(!pivot)return;var gauge=pivot.GetComponent<ServiceGauge>();pivot.localRotation=(gauge?gauge.Rest:Quaternion.identity)*Quaternion.AngleAxis(Mathf.Lerp(gauge?gauge.StartAngle:130,gauge?gauge.EndAngle:-130,value),Vector3.forward);}
 }
}
