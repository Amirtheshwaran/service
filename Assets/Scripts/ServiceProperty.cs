using UnityEngine;
namespace ServiceGameV2 {
 public enum EncounterKind { Pursuit, LookAway, None }
 public sealed class ServiceProperty : MonoBehaviour {
  public int Index;
  public Transform Door, Gate, DeliveryPoint, TableApproach, EntitySpawn;
  public Light PorchLight, WindowLight;
  public Transform DoorPanel;
  public float DoorSwing=100;
  public Transform KnockPoint, NoticePoint;
  [TextArea] public string NoticeText;
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
  // V23: how far along ApproachRoute (in the corridor's measure, which starts at 3 m) the car may go: the end of the
  // parking pad by the house; the gravel footpath beyond it is for walking. 0 = not set (the old 9 m pull-off rule).
  public float DriveLength;
  // V20 navigation: the numbered mailbox at the drive mouth, which side of the road it is on (+1 = right travelling north)
  // and its distance along the road from the depot (County Route 9 continues the count past the barricade).
  public Transform Mailbox;public int RoadSide;public float RoadDistance;
  // V20: the doorway measured from the shut leaf (DoorPanel pivot = hinge). Residents answer from AnswerPoint, on the
  // latch side just inside, clear of the opened leaf; Inward points into the house.
  bool doorwayKnown;Vector3 hinge,along,inward;float doorWidth;
  public void MeasureDoorway(){
   if(doorwayKnown||!DoorPanel)return;var rs=DoorPanel.GetComponentsInChildren<Renderer>();if(rs.Length==0)return;
   var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);hinge=DoorPanel.position;var c=b.center;c.y=hinge.y;
   along=c-hinge;along.y=0;doorWidth=along.magnitude*2;if(doorWidth<.3f)return;along/=doorWidth*.5f;
   inward=Vector3.Cross(Vector3.up,along);var toIn=InteriorBounds.center-Door.position;toIn.y=0;if(Vector3.Dot(inward,toIn)<0)inward=-inward;doorwayKnown=true;}
  public float DoorWidth{get{MeasureDoorway();return doorwayKnown?doorWidth:1f;}}
  public Vector3 Inward{get{MeasureDoorway();return doorwayKnown?inward:-Door.forward;}}
  public Vector3 OpeningCentre{get{MeasureDoorway();var c=doorwayKnown?hinge+along*doorWidth*.5f:Door.position;c.y=Door.position.y;return c;}}
  public Vector3 AnswerPoint(float depth=.42f,float latch=.7f){MeasureDoorway();if(!doorwayKnown)return Door.position-Door.forward*.6f;var p=hinge+along*(doorWidth*latch)+inward*depth;p.y=Door.position.y;return p;}
  public bool HasEncounter => Encounter != EncounterKind.None;
 }
}
