using UnityEngine;
namespace ServiceGameV2 {
 public enum EncounterKind { Pursuit, LookAway, None }
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
  public string Address, Brief, Instructions, RevealLine, DeathLine, Template;
  public EncounterKind Encounter;
  public int CreatureVariant;
  public Bounds InteriorBounds;
  public Vector3[] ApproachRoute;
  public bool HasEncounter => Encounter != EncounterKind.None;
 }
}
