using UnityEngine;
namespace ServiceGameV2 {
 public sealed class CountyScene : MonoBehaviour {
  public Transform Car, DriverSeat, ExitLeft, ExitRight, Depot;
  public Camera View;
  public CharacterController CarBody, Walker;
  public Light Flashlight, Moon;
  public ServiceProperty[] Properties;
  public GameObject LateRoad;
  public Transform LateThreshold;
  public Transform[] Route;
  public TextMesh Odometer;
  public Renderer[] CarExterior;
  public ParticleSystem Rain;
  public UnityEngine.AI.NavMeshData Navigation;
  public GameObject Entity;
  public GameObject[] EntityVariants;
  public GameObject Cockpit;
  public Transform SteeringWheel,SpeedNeedle,RevNeedle;
 }
}
