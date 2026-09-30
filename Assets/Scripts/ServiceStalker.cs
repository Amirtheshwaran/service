using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2 {
 // Night one foreshadowing: the man at the tree line by Vale's gate, and a note left on the car after Morrow.
 public sealed class ServiceStalker:MonoBehaviour {
  ServiceDirector d;GameObject figure;bool shown,done,noteDone;float seen;
  public bool FigureVisible=>figure&&figure.activeSelf;
  public bool NoteFound=>noteDone;
  public void Initialize(ServiceDirector director,GameObject stalkerFigure){d=director;figure=stalkerFigure;ResetForShift();}
  public void ResetForShift(){shown=done=noteDone=false;seen=0;if(figure)figure.SetActive(false);}
  void Update(){
   if(!d||d.Phase!=ServicePhase.Playing||d.NightIndex!=0)return;
   if(!done&&figure){
    var vale=d.Property(1);
    if(!shown&&d.Player.InCar&&d.ResultAt(3)!=ServiceResult.Pending&&Vector3.Distance(d.Scene.Car.position,vale.Gate.position)<55){Place(vale);shown=true;figure.SetActive(true);}
    if(shown){
     var eye=d.Scene.View.transform;var to=figure.transform.position+Vector3.up*1.6f-eye.position;
     bool looking=to.magnitude<60&&Vector3.Dot(eye.forward,to.normalized)>.9f;if(looking)seen+=Time.deltaTime;
     if(seen>1.6f||to.magnitude<14){figure.SetActive(false);done=true;d.Say(seen>1.6f?ServiceScript.TreeLineSeen:ServiceScript.TreeLineMissed);}
    }
   }
   if(!noteDone&&d.Player.InCar&&d.Docket.Exists(e=>e.Property==5)&&d.ResultAt(5)!=ServiceResult.Pending){noteDone=true;d.Audio.Paper();d.Say(ServiceScript.WindshieldNote);}
  }
  public void SmokeShowFigure(){if(!figure)return;Place(d.Property(1));figure.SetActive(true);shown=true;}
  void Place(ServiceProperty vale){
   var gate=vale.Gate.position;var axis=vale.Door.position-gate;axis.y=0;axis.Normalize();var side=Vector3.Cross(Vector3.up,axis);
   var spot=gate+axis*11;
   foreach(float s in new[]{1f,-1f})if(NavMesh.SamplePosition(gate+axis*10+side*9*s,out var hit,4,NavMesh.AllAreas)){spot=hit.position;break;}
   figure.transform.position=spot;var look=gate-spot;look.y=0;if(look.sqrMagnitude>.01f)figure.transform.rotation=Quaternion.LookRotation(look);
  }
 }
}
