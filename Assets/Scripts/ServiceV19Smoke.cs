using System;using System.IO;using System.Linq;using System.Collections;using System.Collections.Generic;using UnityEngine;using UnityEngine.AI;
namespace ServiceGameV2 {
 // V19 regression: monsters-only story, hands and torch, taped door notes, knock/talk flow, walking every house on foot
 // from the door to its delivery table through the furnished rooms, the Harrow watcher rules, depot reporting, and the
 // last night's unsurveyed parcel. Run: Service.exe -serviceSmoke -serviceV19 with SERVICE_CAPTURE_DIR set.
 public sealed class ServiceV19Smoke:MonoBehaviour {
  ServiceDirector d;string dir;readonly List<string> checks=new List<string>();
  public void Run(ServiceDirector director){d=director;dir=Environment.GetEnvironmentVariable("SERVICE_CAPTURE_DIR");Directory.CreateDirectory(dir);StartCoroutine(Guard());}
  IEnumerator Guard(){var stack=new Stack<IEnumerator>();stack.Push(Check());bool failed=false;
   while(stack.Count>0){object next=null;bool more=false;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){Debug.LogException(e);checks.Add("FAIL "+e.Message);failed=true;break;}if(!more){stack.Pop();continue;}if(next is IEnumerator nested)stack.Push(nested);else yield return next;}
   File.WriteAllLines(Path.Combine(dir,"results.txt"),new[]{failed||checks.Any(c=>c.StartsWith("FAIL"))?"FAIL":"PASS"}.Concat(checks));Application.Quit(failed?1:0);}
  void Require(bool ok,string message){if(!ok)throw new Exception(message);checks.Add("PASS "+message);File.WriteAllLines(Path.Combine(dir,"progress.txt"),checks);}
  void Soft(bool ok,string message){checks.Add((ok?"PASS ":"FAIL ")+message);File.WriteAllLines(Path.Combine(dir,"progress.txt"),checks);}
  IEnumerator Face(Vector3 target){var delta=target-d.Scene.View.transform.position;d.Player.SmokeLook(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);yield return new WaitForSeconds(.25f);}
  IEnumerator Shot(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));yield return new WaitForSeconds(.2f);}
  static Vector3 Outward(ServiceProperty p){var o=p.Door.position-p.InteriorBounds.center;o.y=0;return o.sqrMagnitude<.01f?p.Door.forward:o.normalized;}
  // Walk the player's own capsule along the navmesh path: furniture that blocks the way shows up as a physical obstruction.
  IEnumerator Walk(Vector3 target,string label){
   Require(NavMesh.SamplePosition(d.Scene.Walker.transform.position,out var from,2,NavMesh.AllAreas),"Navigation start "+label);
   Require(NavMesh.SamplePosition(target,out var to,2,NavMesh.AllAreas),"Navigation goal "+label);
   var path=new NavMeshPath();Require(NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Connected route "+label);
   foreach(var corner in path.corners){float timeout=Time.time+25;
    while(Vector2.Distance(new Vector2(d.Scene.Walker.transform.position.x,d.Scene.Walker.transform.position.z),new Vector2(corner.x,corner.z))>.22f){
     if(Time.time>timeout){d.Player.SmokeWalk=Vector2.zero;yield return Shot("obstructed-"+label.Replace(' ','-'));throw new Exception("Physical obstruction on "+label+" at "+d.Scene.Walker.transform.position+" toward "+corner);}
     d.Player.SmokeFace(corner);d.Player.SmokeWalk=Vector2.up;yield return null;}
    d.Player.SmokeWalk=Vector2.zero;}
   yield return new WaitForSeconds(.2f);
  }
  IEnumerator Check(){
   yield return new WaitForSeconds(1);d.BeginShift(0);d.PaperOpen=false;yield return new WaitForSeconds(.2f);
   // Story and cast: monsters only.
   Require(d.Scene.GetComponentsInChildren<Transform>(true).All(t=>!t.name.Contains("stalker")&&!t.name.Contains("Depot worker")),"No human stalker or depot worker in the county");
   Require(ServiceScript.For(1)!=null&&ServiceScript.Note(3,0).Contains("lamp"),"Script text loaded (notes)");
   // Hands, torch and flashlight.
   var hands=d.Scene.View.GetComponentInChildren<ServiceHands>();Require(hands!=null&&hands.Rig!=null&&hands.Torch!=null,"First-person hands with torch installed");
   Require(d.Scene.Flashlight&&d.Scene.Flashlight.transform.IsChildOf(hands.Torch),"Flashlight rides on the torch lens");
   d.Player.SmokePlaceWalker(d.Scene.Car.position-d.Scene.Car.right*2.4f);yield return new WaitForSeconds(5.2f);Require(hands.Visible,"Hands visible on foot");yield return Shot("hands-on-foot");
   Require(d.Camcorder!=null&&d.Camcorder.Level>=1,"Camcorder filter active by default (level "+(d.Camcorder?d.Camcorder.Level:-1)+")");
   // Every house: note, knock, talk or let yourself in, walk to the table on foot, deliver.
   foreach(int index in new[]{0,1,3,4,5}){
    d.BeginShift(index==4?1:0);d.PaperOpen=false;var p=d.Property(index);var o=Outward(p);
    d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.4f);
    Require(p.DoorPanel&&p.KnockPoint,"Working door at property "+index);
    if(p.NoticePoint&&!string.IsNullOrEmpty(p.NoticeText)){yield return Face(p.NoticePoint.position);Require(d.NearbyNotice()==index,"Taped note readable at "+index);d.ReadNotice(index);yield return null;Require(d.NoteOpen==index,"Note held up to read at "+index);yield return Shot(index+"-note");d.CloseNote();}
    yield return Face(p.KnockPoint.position);Require(d.NearbyKnockDoor()==index,"Can knock at "+index+(p.NoticePoint?" (after reading the note)":""));
    yield return Face(d.Scene.View.transform.position+o*4);Require(d.NearbyKnockDoor()!=index,"No knock while facing away "+index);yield return Face(p.KnockPoint.position);
    d.Attempt(index,ServiceResult.Served);float until=Time.time+25;while(d.Busy&&Time.time<until)yield return null;yield return new WaitForSeconds(.5f);
    if(d.IsFriendly(index)){Require(d.ResultAt(index)==ServiceResult.Served,"Resident served in person at "+index);yield return Shot(index+"-served");continue;}
    Require(d.AccessGranted(index),"Knock unlatches the door at "+index);yield return Shot(index+"-door-open");
    d.Player.SmokePlaceWalker(p.Door.position+o*1.0f);yield return new WaitForSeconds(.3f);
    yield return Walk(p.TableApproach.position,"door to table "+index);
    yield return Face(p.DeliveryPoint.position);yield return Shot(index+"-table");
    Soft(d.NearbyDoor()==index,"Delivery table reachable at "+index);
    {var blockers=Physics.OverlapSphere(p.TableApproach.position+Vector3.up*1.0f,.22f,~0,QueryTriggerInteraction.Ignore).Where(c=>!(c is CharacterController)&&!c.transform.IsChildOf(d.Scene.Walker.transform)&&!c.transform.IsChildOf(d.Scene.View.transform)).Select(c=>c.name).ToArray();Require(blockers.Length==0,"Standing spot at the table is clear at "+index+(blockers.Length>0?" (blocked by "+string.Join(", ",blockers)+")":""));}
    if(d.NearbyDoor()==index){d.Attempt(index,ServiceResult.LeftAtDoor);yield return new WaitForSeconds(.3f);Require(d.ResultAt(index)!=ServiceResult.Pending,"Papers left at "+index);d.Horror.ResetEncounter();}
   }
   // Harrow Lodge watcher: staring kills, stillness facing away survives, walking kills.
   var watcher=d.Property(3);d.BeginShift(1);d.PaperOpen=false;d.Player.SmokePlaceWalker(watcher.TableApproach.position);yield return null;d.Horror.Begin(3);yield return new WaitForSeconds(.2f);
   yield return Face(d.Horror.Agent.transform.position+Vector3.up*1.15f);yield return new WaitForSeconds(2.5f);Require(d.Horror.Caught,"Staring at the watcher is fatal");yield return new WaitForSeconds(5);
   d.Horror.ResetEncounter();d.Player.SmokePlaceWalker(watcher.TableApproach.position);yield return null;d.Horror.Begin(3);yield return null;var away=d.Scene.View.transform.position-(d.Horror.Agent.transform.position-d.Scene.View.transform.position);yield return Face(away);yield return new WaitForSeconds(9.2f);
   Require(!d.Horror.Caught&&!d.Horror.Active,"Standing still facing away survives the watcher");
   d.Horror.ResetEncounter();d.Player.SmokePlaceWalker(watcher.TableApproach.position);yield return null;d.Horror.Begin(3);yield return Face(away);yield return new WaitForSeconds(1.2f);d.Player.SmokeWalk=Vector2.up;yield return new WaitForSeconds(.4f);d.Player.SmokeWalk=Vector2.zero;Require(d.Horror.Caught,"Walking during the watcher is fatal");d.Horror.ResetEncounter();
   // Depot report.
   d.BeginShift(0);d.PaperOpen=false;foreach(var entry in d.Docket)entry.Result=ServiceResult.Served;d.Player.TeleportCar(new Vector3(-10,.08f,0),Quaternion.identity);d.Player.EnterCar();d.Player.StopEngine();yield return null;Require(d.CanFinish,"Report can be filed at the depot");Require(d.TryFinishShift(),"Report accepted");
   // Last night: the unsurveyed parcel on the closed road.
   d.BeginShift(2);d.PaperOpen=false;yield return new WaitForSeconds(.3f);var parcel=d.Property(2);Require(parcel.gameObject.activeInHierarchy,"Unsurveyed parcel present on the last night");
   var po=Outward(parcel);d.Player.SmokePlaceWalker(parcel.Gate.position);yield return new WaitForSeconds(.3f);yield return Walk(parcel.Door.position+po*1.4f,"parcel drive");
   if(parcel.NoticePoint){yield return Face(parcel.NoticePoint.position);Require(d.NearbyNotice()==2,"Parcel note readable");d.ReadNotice(2);yield return null;yield return Shot("2-note");d.CloseNote();}
   yield return Face(parcel.KnockPoint.position);d.Attempt(2,ServiceResult.Served);float t2=Time.time+15;while(d.Busy&&Time.time<t2)yield return null;yield return new WaitForSeconds(.5f);Require(d.AccessGranted(2),"Parcel door opens");
   d.Player.SmokePlaceWalker(parcel.Door.position+po*1.0f);yield return new WaitForSeconds(.3f);yield return Walk(parcel.TableApproach.position,"door to table 2");yield return Face(parcel.DeliveryPoint.position);yield return Shot("2-table");Soft(d.NearbyDoor()==2,"Parcel table reachable");
  }
 }
}
