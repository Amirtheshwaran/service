using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace ServiceGameV2 {
 public sealed class ServiceV5Smoke:MonoBehaviour {
  ServiceDirector d;string dir;
  public void Run(ServiceDirector director){d=director;dir=Environment.GetEnvironmentVariable("SERVICE_CAPTURE_DIR");if(string.IsNullOrEmpty(dir))dir=Path.Combine(Application.persistentDataPath,"SmokeV5");Directory.CreateDirectory(dir);StartCoroutine(Guard());}
  IEnumerator Guard(){var stack=new Stack<IEnumerator>();stack.Push(Check());bool failed=false;while(stack.Count>0){object next=null;bool more=false;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){Debug.LogException(e);failed=true;break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;}d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeThrottle=0;for(int i=0;i<6;i++)PlayerPrefs.DeleteKey("SERVICE.test.result"+i);PlayerPrefs.DeleteKey("SERVICE.test.night");PlayerPrefs.Save();var result=(failed?"FAIL":"PASS")+": SERVICE_V5_SMOKE (five distinct properties, physical access and escape, five encounters, watcher rules, retry checkpoint, surfaces, needle, pause, route save)";File.WriteAllText(Path.Combine(dir,"smoke-result.txt"),result);Debug.Log(result);Application.Quit(failed?1:0);}
  void Require(bool pass,string message){if(!pass)throw new Exception("SERVICE_V5: "+message);}
  IEnumerator Shot(string name){yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));yield return new WaitForSeconds(.2f);}
  IEnumerator Navigate(Vector3 target,bool sprint){
   Require(NavMesh.SamplePosition(d.Scene.Walker.transform.position,out var from,1.2f,NavMesh.AllAreas),"Start outside navigation");Require(NavMesh.SamplePosition(target,out var goal,1.2f,NavMesh.AllAreas),"Goal outside navigation");
   var path=new NavMeshPath();Require(NavMesh.CalculatePath(from.position,goal.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"No complete path to "+target);
   float start=Time.time;d.Player.SmokeSprint=sprint;
   foreach(var corner in path.corners){Debug.Log("V5_CORNER start="+d.Scene.Walker.transform.position+" target="+corner+" sprint="+sprint);float until=Time.time+35;while(Vector2.Distance(new Vector2(d.Scene.Walker.transform.position.x,d.Scene.Walker.transform.position.z),new Vector2(corner.x,corner.z))>.15f&&Time.time<until){if(sprint&&Vector3.Distance(d.Scene.View.transform.position,d.Scene.Car.position+Vector3.up)<3.2f){d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeSprint=false;Debug.Log("V5_WALK escape seconds="+(Time.time-start));yield break;}Require(!d.Horror.Caught,"Caught during escape at property "+d.Horror.PropertyIndex+" position="+d.Scene.Walker.transform.position+" target="+corner+" elapsed="+(Time.time-start));d.Player.SmokeFace(corner);d.Player.SmokeWalk=Vector2.up;yield return null;}d.Player.SmokeWalk=Vector2.zero;Require(Time.time<until,"Physical route blocked: "+d.Scene.Walker.transform.position+" toward "+corner);}
   Require(Mathf.Abs(d.Scene.Walker.transform.position.y-goal.position.y)<.45f,"Wrong floor at "+target);Debug.Log("V5_WALK "+(sprint?"escape":"approach")+" seconds="+(Time.time-start));d.Player.SmokeSprint=false;
  }
  IEnumerator Check(){
   yield return new WaitForSeconds(1);yield return Shot("00-title");d.BeginShift(0);Require(d.Docket.Count==5,"First shift needs five stops");Require(d.Docket.Select(e=>d.Property(e.Property).Template).Distinct().Count()==5,"Repeated building template");yield return Shot("01-docket");d.MapOpen=true;yield return Shot("02-map");d.PaperOpen=false;
   d.Player.StartEngine();yield return new WaitForSeconds(.8f);d.Player.SmokeThrottle=1;yield return new WaitForSeconds(2);d.Player.SmokeThrottle=0;Require(d.Player.Speed>1,"Car moves");var needle=d.Scene.SpeedNeedle.up;var axis=d.Scene.SpeedNeedle.parent;float angle=Mathf.Lerp(220,-40,d.Player.Speed*2.237f/60)*Mathf.Deg2Rad;Require(Vector3.Dot(needle,axis.TransformDirection(new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)))>.98f,"Speed needle follows dial");d.Player.StopEngine();d.Player.SmokeLook(0,24);yield return Shot("03-cockpit");
   Require(Resources.LoadAll<AudioClip>("Audio/V5/grass").Length>4&&Resources.LoadAll<AudioClip>("Audio/V5/wood").Length>4,"Footstep variations");
   foreach(int index in new[]{0,3,1,4,5}){
    var p=d.Property(index);d.Player.TeleportCar(p.Gate.position-p.Gate.forward*3,Quaternion.LookRotation(p.Gate.forward));d.Player.SmokePlaceWalker(p.Gate.position);d.Player.SmokeFace(p.Door.position);yield return Shot("property-"+index+"-approach");
    yield return Navigate(p.TableApproach.position,false);Require(d.NearbyDoor()==index,"Delivery unavailable at property "+index);Require(d.Audio.SurfaceAt(d.Scene.Walker.transform.position)=="wood","Interior wood surface "+index);d.Player.SmokeFace(p.DeliveryPoint.position);yield return Shot("property-"+index+"-interior");d.Attempt(index,ServiceResult.LeftAtDoor);Require(d.Horror.Active&&d.Horror.PropertyIndex==index,"Encounter did not start "+index);
    d.Pause();float elapsed=d.Horror.Elapsed;yield return new WaitForSecondsRealtime(.15f);Require(AudioListener.pause&&Mathf.Abs(elapsed-d.Horror.Elapsed)<.01f,"Pause freezes audio and encounter");d.Resume();
    if(index==3){
     yield return new WaitForSeconds(1.2f);d.Player.SmokeWalk=Vector2.up;d.Player.SmokeSprint=true;yield return new WaitForSeconds(.25f);d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeSprint=false;Require(d.Horror.Caught,"Running must fail watcher");yield return Shot("watcher-failure");yield return new WaitForSeconds(4);Require(d.ResultAt(0)!=ServiceResult.Pending&&d.ResultAt(3)==ServiceResult.Pending,"Retry preserves other deliveries");
     yield return Navigate(p.TableApproach.position,false);d.Attempt(index,ServiceResult.LeftAtDoor);d.Player.SmokeFace(d.Scene.Walker.transform.position+(d.Scene.Walker.transform.position-d.Horror.Agent.transform.position));yield return new WaitForSeconds(9.6f);Require(d.Horror.Phase==PursuitPhase.Escaped,"Look away eight seconds must dismiss watcher");yield return Shot("watcher-dismissed");d.Player.EnterCar();
    }else{
     d.Player.SmokeFace(d.Horror.Agent.transform.position);yield return new WaitForSeconds(3.3f);Require(d.Horror.Phase==PursuitPhase.Chase,"Pursuit starts "+index);yield return Shot("property-"+index+"-presence");yield return Navigate(p.Gate.position-p.Gate.forward*2,true);Require(d.Horror.Active,"Must escape alive "+index);Require(Vector3.Distance(d.Scene.View.transform.position,d.Scene.Car.position+Vector3.up)<3.5f,"Reach car interaction "+index);d.Player.EnterCar();yield return null;Require(d.Horror.Phase==PursuitPhase.Escaped,"Car escape "+index);
    }
   }
   Require(d.AllResolved,"All five notices resolved");d.Player.TeleportCar(d.Scene.Depot.position,Quaternion.identity);Require(d.TryFinishShift(),"File five-stop report");yield return Shot("09-report");d.Title();Require(d.HasSavedRoute,"Route saved");d.ContinueRoute();Require(d.NightIndex==1&&d.Docket.Count==5,"Saved second shift");d.BeginShift(0);Require(d.Horror.Phase==PursuitPhase.Dormant,"Restart resets encounters");
  }
 }
}
