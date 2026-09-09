using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceProperty : MonoBehaviour {
  public int Index;
  public Transform Door, Gate, DeliveryPoint, TableApproach, EntitySpawn;
  public Light PorchLight, WindowLight;
  public Transform DoorPanel;
  public Renderer[] Curtains;
  public GameObject PostedPaper;
  public Transform SoundPoint;
  public TextMesh AddressLabel;
  public Transform Building;
  public Light[] EncounterLights;
 }
}
