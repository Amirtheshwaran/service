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
   if(Environment.GetCommandLineArgs().Contains("-smokeV22Only")){yield return V22();yield break;} // iteration aid
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
    if(p.NoticePoint&&!string.IsNullOrEmpty(p.NoticeText)){yield return Face(p.NoticePoint.position);Require(d.NearbyNotice()==index,"Taped note readable at "+index);yield return null;Require(d.HUD.NoteMarked==index,"The note being aimed at is marked on screen at "+index);
     if(d.Scene.Flashlight){bool was=d.Scene.Flashlight.enabled;d.Scene.Flashlight.enabled=true;yield return Shot(index+"-note-prompt-torch");d.Scene.Flashlight.enabled=was;}
     d.ReadNotice(index);yield return null;Require(d.NoteOpen==index,"Note held up to read at "+index);yield return null;
     var firstWord=p.NoticeText.Split(new[]{' ','\n'},StringSplitOptions.RemoveEmptyEntries)[0];Require(d.HUD.NoteTranscript.Length>10&&d.HUD.NoteTranscript.Contains(firstWord),"Typed transcript under the note at "+index);
     yield return Shot(index+"-note");d.CloseNote();}
    yield return Face(p.KnockPoint.position);Require(d.NearbyKnockDoor()==index,"Can knock at "+index+(p.NoticePoint?" (after reading the note)":""));
    yield return Face(d.Scene.View.transform.position+o*4);Require(d.NearbyKnockDoor()!=index,"No knock while facing away "+index);yield return Face(p.KnockPoint.position);
    yield return new WaitForSeconds(.35f);Require(d.Guide.Direction=="KNOCK AT THE DOOR","Route line on the step says to knock at "+index+" ('"+d.Guide.Direction+"')");
    d.Attempt(index,ServiceResult.Served);float until=Time.time+25;while(d.Busy&&Time.time<until)yield return null;yield return new WaitForSeconds(.5f);
    if(d.IsFriendly(index)){Require(d.ResultAt(index)==ServiceResult.Served,"Resident served in person at "+index);yield return Shot(index+"-served");continue;}
    Require(d.AccessGranted(index),"Knock unlatches the door at "+index);yield return Shot(index+"-door-open");
    if(p.NoticePoint&&!string.IsNullOrEmpty(p.NoticeText)){yield return Face(p.NoticePoint.position);Require(d.NearbyNotice()!=index,"No note prompt once the door is open at "+index);}
    d.Player.SmokePlaceWalker(p.Door.position+o*1.0f);yield return new WaitForSeconds(.3f);
    yield return Walk(p.TableApproach.position,"door to table "+index);
    yield return Face(p.DeliveryPoint.position);yield return Shot(index+"-table");
    Soft(d.NearbyDoor()==index,"Delivery table reachable at "+index);
    yield return new WaitForSeconds(.35f);Require(d.Guide.Direction=="LEAVE THE PAPERS ON THE TABLE","Route line indoors says to leave the papers at "+index+" ('"+d.Guide.Direction+"')");
    {var blockers=Physics.OverlapSphere(p.TableApproach.position+Vector3.up*1.0f,.22f,~0,QueryTriggerInteraction.Ignore).Where(c=>!(c is CharacterController)&&!c.transform.IsChildOf(d.Scene.Walker.transform)&&!c.transform.IsChildOf(d.Scene.View.transform)).Select(c=>c.name).ToArray();Require(blockers.Length==0,"Standing spot at the table is clear at "+index+(blockers.Length>0?" (blocked by "+string.Join(", ",blockers)+")":""));}
    if(d.NearbyDoor()==index){int placed=d.PapersPlaced;d.Attempt(index,ServiceResult.LeftAtDoor);
     // V21: the hand sets the papers down before the house answers; the copy ends on its table, in sight
     Require(d.Busy&&d.ResultAt(index)==ServiceResult.Pending&&!p.PostedPaper.activeSelf,"Papers wait for the hand at "+index);
     float placeUntil=Time.time+4;while(d.Busy&&Time.time<placeUntil)yield return null;
     Require(d.ResultAt(index)!=ServiceResult.Pending&&d.PapersPlaced==placed+1,"Papers left at "+index);
     Require(p.PostedPaper.activeSelf&&Vector3.Distance(p.PostedPaper.transform.position,p.DeliveryPoint.position)<.6f&&Mathf.Abs(p.PostedPaper.transform.position.y-p.DeliveryPoint.position.y)<.02f,"Papers came to rest on the table at "+index);
     Require(!ServicePaperPlacement.LastHidden,"Papers on the table are in sight from where they were left at "+index);
     yield return Shot(index+"-papers-down");d.Horror.ResetEncounter();}
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
   yield return V21();
   yield return V22();
  }
  // ---- V22: the playtest round.
  IEnumerator Sprint(Vector3 target,bool sprint,float giveUp,Func<bool> stop){
   bool sa=NavMesh.SamplePosition(d.Scene.Walker.transform.position,out var a,2.5f,NavMesh.AllAreas),sb=NavMesh.SamplePosition(target,out var b,2.5f,NavMesh.AllAreas);Require(sa&&sb,"Navmesh under the flight to "+target);
   var path=new NavMeshPath();Require(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Connected flight to "+target);float end=Time.time+giveUp;
   float traceAt=0;
   foreach(var c in path.corners){while(Vector2.Distance(new Vector2(d.Scene.Walker.transform.position.x,d.Scene.Walker.transform.position.z),new Vector2(c.x,c.z))>.35f){if(Time.time>end||stop()){d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeSprint=false;yield break;}d.Player.SmokeFace(c);d.Player.SmokeSprint=sprint;d.Player.SmokeWalk=Vector2.up;
     if(Time.time>traceAt){traceAt=Time.time+.5f;var ag=d.Horror.Agent;File.AppendAllText(Path.Combine(dir,"trace.txt"),$"{Time.time:F1} walker {d.Scene.Walker.transform.position} v {d.Player.HorizontalSpeed:F1} stam {d.Player.Stamina:F2} sprint {d.Player.Sprinting} | creature {(ag?ag.transform.position.ToString():"-")} gap {(ag?Vector3.Distance(ag.transform.position,d.Scene.Walker.transform.position):-1):F1} speed {(ag?ag.velocity.magnitude:0):F1} phase {d.Horror.Phase}"+System.Environment.NewLine);}
     yield return null;}}
   d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeSprint=false;}
  static Vector3 FlatV(Vector3 v)=>new Vector3(v.x,0,v.z);
  // what the dog stands on, judged from the colliders (not the mover's own rules): the ground, or part of a building
  static bool OnBuilding(Vector3 at){if(!Physics.Raycast(at+Vector3.up*1f,Vector3.down,out var hit,3f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))return false;if(hit.collider is TerrainCollider)return false;var s=hit.collider.GetComponentInParent<ServiceSurface>();return s==null||s.Kind=="wood";}
  IEnumerator V22(){
   // -- the door behind you: Harrow, night one. Knock, step out, pull it shut, find it shut, open it again.
   {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(3);var o=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.4f);
    yield return Face(p.KnockPoint.position);d.Attempt(3,ServiceResult.Served);float t=Time.time+15;while(d.Busy&&Time.time<t)yield return null;yield return new WaitForSeconds(.5f);
    Require(d.AccessGranted(3)&&d.Life.DoorOpenDegrees(3)>60,"Harrow unlatched and open");
    var step=p.OpeningCentre-p.Inward*1.6f;d.Player.SmokePlaceWalker(step);yield return new WaitForSeconds(.3f);yield return Face(p.OpeningCentre+Vector3.up*1.2f);yield return null;
    Require(d.NearbyLeaf()==3&&!d.CanEnterCar,"Close-the-door offered on Harrow's step");yield return Shot("v22-close-door-prompt");
    yield return Face(d.Scene.View.transform.position+(d.Scene.View.transform.position-p.OpeningCentre));Require(d.NearbyLeaf()<0,"No door prompt facing away");
    yield return Face(p.OpeningCentre+Vector3.up*1.2f);Require(d.ToggleDoor(3),"Pull the door shut");yield return new WaitForSeconds(1.6f);
    Require(d.Life.DoorOpenDegrees(3)<2&&d.ShutByPlayer(3),"Harrow's door shut behind you ("+d.Life.DoorOpenDegrees(3).ToString("F0")+" deg)");
    float until=Time.time+1.5f;while(Time.time<until){d.Player.SmokeFace(p.OpeningCentre+Vector3.up*1.2f);d.Player.SmokeWalk=Vector2.up;yield return null;}d.Player.SmokeWalk=Vector2.zero;
    Require(Vector3.Dot(d.Scene.Walker.transform.position-p.OpeningCentre,-p.Inward)>.15f,"A shut door keeps you out");
    yield return Face(p.OpeningCentre+Vector3.up*1.4f);yield return null;Require(d.NearbyLeaf()==3,"Open-the-door offered right up against the shut door ("+Vector3.Dot(d.Scene.Walker.transform.position-p.OpeningCentre,-p.Inward).ToString("F2")+" m out)");
    d.Player.SmokePlaceWalker(step);yield return new WaitForSeconds(.3f);yield return Face(p.OpeningCentre+Vector3.up*1.2f);yield return null;
    Require(d.NearbyLeaf()==3&&d.Guide.Direction=="OPEN THE DOOR","Open-the-door offered on a door you shut ('"+d.Guide.Direction+"')");
    Require(d.ToggleDoor(3),"Open it again");yield return new WaitForSeconds(1.5f);Require(d.Life.DoorOpenDegrees(3)>60&&!d.ShutByPlayer(3),"Harrow's door open again for the papers");}
   // -- Bell, night two: shut his door yourself and the return ambush still bangs it open behind you.
   {d.BeginShift(1);d.PaperOpen=false;foreach(var i in new[]{0,3,1})d.Docket.Find(e=>e.Property==i).Result=ServiceResult.Served;var p=d.Property(4);var o=Outward(p);
    d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.4f);yield return Face(p.KnockPoint.position);d.Attempt(4,ServiceResult.Served);float t=Time.time+15;while(d.Busy&&Time.time<t)yield return null;yield return new WaitForSeconds(.4f);
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.3f);yield return Face(p.DeliveryPoint.position);d.Attempt(4,ServiceResult.LeftAtDoor);t=Time.time+4;while(d.Busy&&Time.time<t)yield return null;
    Require(d.ResultAt(4)==ServiceResult.LeftAtDoor,"Papers left at Bell's on night two");int before=d.Horror.ReturnAmbushes;
    d.Player.SmokePlaceWalker(p.OpeningCentre-p.Inward*1.6f);yield return new WaitForSeconds(2.3f);yield return Face(p.OpeningCentre+Vector3.up*1.2f);yield return null;
    Require(d.ToggleDoor(4),"Pull Bell's door shut");yield return new WaitForSeconds(1.4f);
    var A=p.ApproachRoute;Vector3 spot=A[0];for(int i=A.Length-1;i>=0;i--)if(Vector3.Distance(A[i],p.Door.position)>14){spot=A[i];break;}
    d.Player.SmokePlaceWalker(spot);float t3=Time.time+4;while(d.Horror.ReturnAmbushes==before&&Time.time<t3)yield return null;yield return new WaitForSeconds(.4f);
    Require(d.Horror.ReturnAmbushes==before+1&&d.Horror.Active&&d.Life.DoorOpenDegrees(4)>60,"Bell's door bangs open behind you even after you shut it");d.Horror.ResetEncounter();}
   // -- the car keeps to the road and the drive pull-offs
   {d.BeginShift(0);d.PaperOpen=false;yield return new WaitForSeconds(.3f);var r=d.Scene.Route;var a=r[8].position;var along=FlatV(r[9].position-a).normalized;var side=Vector3.Cross(Vector3.up,along);
    d.Player.TeleportCar(a+Vector3.up*.3f,Quaternion.LookRotation(side));d.Player.StartEngine();yield return new WaitForSeconds(1.2f);int kerb=d.Player.KerbContacts;int hits=d.Audio.CollisionsPlayed;
    float until=Time.time+3;while(Time.time<until){d.Player.SmokeThrottle=1;yield return null;}d.Player.SmokeThrottle=0;d.Player.SmokeBrake=true;yield return new WaitForSeconds(.8f);d.Player.SmokeBrake=false;
    float lateral=Vector3.Dot(d.Scene.Car.position-a,side);Require(d.Player.KerbContacts>kerb&&d.Player.OnCorridor&&lateral<4.6f&&d.Audio.CollisionsPlayed==hits,"Driving at the verge stops at the road's edge, no crash ("+lateral.ToString("F1")+" m from the centre)");
    // ...and from there, full lock swings the nose round along the edge (the start of a three-point turn)
    float yaw0=d.Scene.Car.eulerAngles.y;hits=d.Audio.CollisionsPlayed;until=Time.time+5;while(Time.time<until){d.Player.SmokeThrottle=1;d.Player.SmokeSteering=1;yield return null;}d.Player.SmokeThrottle=0;d.Player.SmokeSteering=0;
    float turned=Mathf.Abs(Mathf.DeltaAngle(yaw0,d.Scene.Car.eulerAngles.y));Require(turned>40&&d.Player.OnCorridor&&d.Audio.CollisionsPlayed==hits,"Nose to the kerb, full lock turns the car round along it ("+turned.ToString("F0")+" degrees in 5 s)");
    var p=d.Property(3);d.Player.TeleportCar(p.Gate.position+Vector3.up*.3f,Quaternion.LookRotation(FlatV(p.Gate.forward)));d.Player.StartEngine();yield return new WaitForSeconds(1.2f);bool said=false;
    until=Time.time+4;while(Time.time<until){d.Player.SmokeThrottle=1;if(d.Notice==ServiceScript.ParkAndWalk)said=true;yield return null;}d.Player.SmokeThrottle=0;d.Player.SmokeBrake=true;yield return new WaitForSeconds(.8f);d.Player.SmokeBrake=false;
    float up=Vector3.Distance(FlatV(d.Scene.Car.position),FlatV(p.Gate.position));Require(up<7f&&d.Player.OnCorridor&&said,"The car stops at Harrow's pull-off, short of the lawn ("+up.ToString("F1")+" m past the gate)");yield return Shot("v22-car-pull-off");
    d.Player.TeleportCar(p.Gate.position+Vector3.up*.3f,Quaternion.LookRotation(FlatV(p.Gate.forward)));d.Player.StartEngine();yield return new WaitForSeconds(1.2f);float peak=0;
    until=Time.time+3;while(Time.time<until){d.Player.SmokeThrottle=-1;peak=Mathf.Max(peak,d.Player.Speed);yield return null;}d.Player.SmokeThrottle=0;
    Require(peak>3f,"Reversing out of a pull-off reaches escape speed ("+peak.ToString("F1")+" m/s)");}
   // -- stamina: a sprint runs out; winded is a laboured run; breath comes back; no backwards sprint
   {d.BeginShift(0);d.PaperOpen=false;var start=d.Scene.Depot.position+Vector3.up*.1f;d.Player.SmokePlaceWalker(start);yield return null;d.Player.SmokeFace(start+Vector3.forward*60);yield return null;
    Require(d.Player.Stamina>.99f&&!d.Player.Winded,"Full of breath on foot");
    d.Player.SmokeSprint=true;d.Player.SmokeWalk=Vector2.up;yield return new WaitForSeconds(3f);
    Require(d.Player.Sprinting&&d.Player.Stamina<.75f&&d.Player.HorizontalSpeed>5f,"Sprinting spends breath ("+d.Player.Stamina.ToString("F2")+" left at "+d.Player.HorizontalSpeed.ToString("F1")+" m/s)");
    float u=Time.time+5;while(Time.time<u&&!d.Player.Winded){d.Player.SmokeFace(d.Scene.Walker.transform.position+Vector3.forward*20);yield return null;}Require(d.Player.Winded,"A sprint runs out after about six seconds");yield return new WaitForSeconds(.4f);
    Require(d.Player.HorizontalSpeed>4.2f&&d.Player.HorizontalSpeed<5f&&!d.Player.Sprinting,"Out of breath, Shift is a laboured run ("+d.Player.HorizontalSpeed.ToString("F1")+" m/s)");
    d.Player.SmokeSprint=false;d.Player.SmokeWalk=Vector2.zero;yield return new WaitForSeconds(1.7f);Require(!d.Player.Winded&&d.Player.Stamina>=.3f,"Breath comes back standing still ("+d.Player.Stamina.ToString("F2")+")");
    float s0=d.Player.Stamina;d.Player.SmokeSprint=true;yield return new WaitForSeconds(1f);Require(d.Player.Stamina>=s0-.005f,"Holding Shift standing still costs nothing");
    d.Player.SmokeWalk=Vector2.down;yield return new WaitForSeconds(.6f);Require(d.Player.HorizontalSpeed<3.5f,"No sprinting backwards ("+d.Player.HorizontalSpeed.ToString("F1")+" m/s)");d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeSprint=false;}
   // -- the Morrow flight on night two: running gets you to the car; walking does not
   foreach(bool sprint in new[]{true,false}){
    d.BeginShift(1);d.PaperOpen=false;foreach(var i in new[]{0,3,1,4})d.Docket.Find(e=>e.Property==i).Result=ServiceResult.Served;var p=d.Property(5);var o=Outward(p);
    d.Player.TeleportCar(p.Gate.position+Vector3.up*.3f,Quaternion.LookRotation(FlatV(p.Gate.forward)));yield return new WaitForSeconds(.3f);
    d.Life.OpenDoor(p,true);yield return new WaitForSeconds(.8f); // as after the latch gives
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.3f);d.Horror.Begin(5);yield return new WaitForSeconds(1.5f);float t0=Time.time;
    yield return Sprint(p.Door.position+o*1.8f,sprint,30,()=>d.Horror.Caught);
    var car=d.Scene.Car;yield return Sprint(car.position-car.right*2.2f,sprint,45,()=>d.Horror.Caught||d.CanEnterCar);
    float gap=d.Horror.Agent?Vector3.Distance(d.Horror.Agent.transform.position,d.Scene.Walker.transform.position):-1;
    if(sprint){Require(!d.Horror.Caught&&d.CanEnterCar,$"Running from Morrow's reaches the car ({Time.time-t0:F1} s, creature {gap:F1} m behind, winded {d.Player.WindedCount} times)");d.Player.EnterCar();yield return null;Require(d.Horror.Phase==PursuitPhase.Escaped,"Escaped in the car");}
    else{bool reachedCar=!d.Horror.Caught&&d.CanEnterCar;Require(d.Horror.Caught&&!reachedCar,$"Walking away from it gets you caught before the car ({Time.time-t0:F1} s, reached the car {reachedCar})");}
    d.Horror.ResetEncounter();yield return new WaitForSeconds(.3f);}
   // -- Rex charges on night one; "Rex, hush" quiets him; only two barks on night two
   {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(0);var A=p.ApproachRoute;yield return new WaitForSeconds(.3f);Require(d.Life.Mode==ServiceLife.DogMode.Home,"Rex at home before you come");
    d.Player.SmokePlaceWalker(A[8]);yield return null;bool left=false,far=false,inside=false,porch=false;float farthest=0,firstMove=-1,startT=Time.time;string modes="";int escortFrames=0,escortInView=0;
    for(int i=9;i<A.Length;i++){var c=A[i];float tt=Time.time+8;while(Vector2.Distance(new Vector2(d.Scene.Walker.transform.position.x,d.Scene.Walker.transform.position.z),new Vector2(c.x,c.z))>.4f&&Time.time<tt){d.Player.SmokeFace(c);d.Player.SmokeWalk=Vector2.up;
      if(d.Life.Mode!=ServiceLife.DogMode.Home){if(!left)firstMove=Time.time-startT;left=true;}if(d.Life.Mode==ServiceLife.DogMode.Escort){var dv=d.Scene.View.WorldToViewportPoint(d.Life.DogPosition+Vector3.up*.35f);escortFrames++;if(dv.z>0&&dv.x>.1f&&dv.x<.9f&&dv.y>0&&dv.y<1)escortInView++;if(escortFrames%15==1)File.AppendAllText(Path.Combine(dir,"rex.txt"),$"{Time.time:F2} walker {d.Scene.Walker.transform.position} wfwd {d.Scene.Walker.transform.forward} vfwd {d.Scene.View.transform.forward} dog {d.Life.DogPosition} vp {dv}"+System.Environment.NewLine);}if(!modes.EndsWith(d.Life.Mode.ToString()))modes+=" "+d.Life.Mode;farthest=Mathf.Max(farthest,Vector3.Distance(d.Life.DogPosition,d.Life.DogHome));if(Vector3.Distance(d.Life.DogPosition,d.Life.DogHome)>8)far=true;if(OnBuilding(d.Life.DogPosition))inside=porch=true;yield return null;}
     if(i==16)yield return Shot("v22-rex-escort");}
    d.Player.SmokeWalk=Vector2.zero;
    Require(escortFrames>0&&escortInView>escortFrames*.6f,$"Rex runs in front of you up the drive, in view {escortInView} of {escortFrames} escort frames (steps refused: ground {d.Life.BlockedBy[0]} indoors {d.Life.BlockedBy[1]} high {d.Life.BlockedBy[2]} jump {d.Life.BlockedBy[3]} navmesh {d.Life.BlockedBy[4]}, travel {d.Life.DogTravel:F1} m)");
    {var dl=d.Life.DogPosition;var stand=dl+FlatV(d.Scene.Walker.transform.position-dl).normalized*3f;d.Player.SmokePlaceWalker(stand);yield return new WaitForSeconds(.2f);yield return Face(dl+Vector3.up*.35f);yield return new WaitForSeconds(.3f);
     Require(d.Life.DogSeen,"Rex's model renders when you look at him ("+d.Life.Mode+" at "+d.Life.DogPosition+")");yield return Shot("v22-rex-look");}
    Require(left&&far,$"Rex charges out to meet you on Correll's drive (modes{modes}, first move {firstMove:F1} s, farthest {farthest:F1} m from his spot, home {d.Life.DogHome}, door {p.Door.position})");Require(d.Life.ClosestApproach>=1.5f,"Rex keeps his distance ("+d.Life.ClosestApproach.ToString("F1")+" m at closest)");
    Require(!inside&&!porch,$"Rex never gets into the house or onto the porch (inside {inside}, porch {porch})");Require(d.Life.Barks>=3,"Rex barks the whole way ("+d.Life.Barks+")");
    d.Player.SmokePlaceWalker(p.Door.position+Outward(p)*1.6f);float tp=Time.time+3;while(Time.time<tp){if(!modes.EndsWith(d.Life.Mode.ToString()))modes+=" "+d.Life.Mode;if(OnBuilding(d.Life.DogPosition))inside=porch=true;yield return null;}
    Require(modes.Contains("Porch"),"Rex goes to the foot of the steps when you reach the door (modes"+modes+")");Require(!inside,"Rex never stands on the porch, the steps or the house");yield return Shot("v22-rex-porch");
    yield return Face(p.KnockPoint.position);Require(d.NearbyKnockDoor()==0,"Knock at Correll's with Rex at the steps");d.Attempt(0,ServiceResult.Served);float tw=Time.time+12;while(!(d.Dialogue&&d.Dialogue.Active)&&Time.time<tw){if(OnBuilding(d.Life.DogPosition))inside=true;yield return null;}yield return new WaitForSeconds(.5f);Require(!inside,"...nor while you knock");
    Require(d.Life.Mode==ServiceLife.DogMode.Hushed&&!d.Audio.DogPlaying,$"'Rex, hush' quiets him (mode {d.Life.Mode}, barking {d.Audio.DogPlaying}, talking {(d.Dialogue&&d.Dialogue.Active)})");float te=Time.time+45;while(d.Busy&&Time.time<te)yield return null;Require(d.ResultAt(0)==ServiceResult.Served,"Walter served with Rex about");
    d.BeginShift(1);d.PaperOpen=false;yield return new WaitForSeconds(.3f);d.Player.SmokePlaceWalker(A[8]);yield return null;
    for(int i=9;i<A.Length-2;i++){var c=A[i];float tt=Time.time+8;while(Vector2.Distance(new Vector2(d.Scene.Walker.transform.position.x,d.Scene.Walker.transform.position.z),new Vector2(c.x,c.z))>.4f&&Time.time<tt){d.Player.SmokeFace(c);d.Player.SmokeWalk=Vector2.up;yield return null;}}
    d.Player.SmokeWalk=Vector2.zero;yield return new WaitForSeconds(2f);Require(d.Life.Barks>=1&&d.Life.Barks<=2,"Night two: \"he only barked twice\" ("+d.Life.Barks+")");}
   // -- night one at Vale: something at the end of the upstairs hall
   // first the way a player goes: in at the front door, up the stairs and along to the study, looking where you walk
   {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(1);yield return new WaitForSeconds(.3f);d.Player.SmokePlaceWalker(p.OpeningCentre+p.Inward*1.4f);yield return new WaitForSeconds(.3f);
    yield return Sprint(p.TableApproach.position,false,40,()=>d.Omens.ValeStage>0);d.Player.SmokeWalk=Vector2.zero;
    Require(d.Omens.ValeStage>0&&d.Scene.Entity.activeSelf,"Night one, walking in and up to Vale's study: something steps out at the end of the upstairs hall");
    var seenAt=d.Scene.View.transform.position;float te=Time.time+9;while(d.Omens.ValeStage>0&&Time.time<te)yield return null;
    Require(d.Omens.ValeStage==0&&d.Omens.ValeGlimpses==1&&!d.Scene.Entity.activeSelf,"...and backs away out of sight ("+Vector3.Distance(seenAt,d.Omens.ValeTo).ToString("F1")+" m away)");}
   {Vector3 eyeSpot=Vector3.zero,E=Vector3.zero;bool found=false;
    for(int attempt=0;attempt<3;attempt++){
     d.BeginShift(attempt==2?1:0);d.PaperOpen=false;var p=d.Property(1);yield return new WaitForSeconds(.3f);
     if(!found){var path=new NavMeshPath();NavMesh.SamplePosition(p.Door.position,out var a,2,NavMesh.AllAreas);NavMesh.SamplePosition(p.TableApproach.position,out var b,2,NavMesh.AllAreas);NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path);
      for(int i=1;i<path.corners.Length&&!found;i++)for(float k=0;k<1&&!found;k+=.1f){var at=Vector3.Lerp(path.corners[i-1],path.corners[i],k);if(at.y<p.Door.position.y+2.8f)continue;var eye=at+Vector3.up*1.65f;
       if(ServiceOmens.ValePoints(p,eye,out var e,out _,d.Scene.Walker.transform)){float dist=Vector3.Distance(eye,e);if(dist>6&&dist<15&&ServiceInteraction.Clear(eye,e+Vector3.up*1.4f,null,d.Scene.Walker.transform)){eyeSpot=at;E=e;found=true;}}}}
     Require(found,"A spot upstairs at Vale with the end of the hall in sight");
     d.Player.SmokePlaceWalker(eyeSpot);yield return new WaitForSeconds(.2f);yield return Face(E+Vector3.up*1.2f);float tw=Time.time+3;while(d.Omens.ValeStage==0&&Time.time<tw){d.Player.SmokeLook(Mathf.Atan2(E.x-d.Scene.View.transform.position.x,E.z-d.Scene.View.transform.position.z)*Mathf.Rad2Deg,0);yield return null;}
     if(attempt==2){Require(d.Omens.ValeStage==0&&!d.Scene.Entity.activeSelf,"No hall glimpse on night two");break;}
     Require(d.Omens.ValeStage>0&&d.Scene.Entity.activeSelf&&d.Horror.Phase==PursuitPhase.Dormant,"Night one: something steps out at the end of Vale's upstairs hall (no chase)");
     if(attempt==0){yield return new WaitForSeconds(1.8f);yield return Shot("v22-vale-glimpse");float te=Time.time+8;while(d.Omens.ValeStage>0&&Time.time<te)yield return null;
      var ag=d.Scene.Entity.GetComponent<NavMeshAgent>();Require(d.Omens.ValeStage==0&&!d.Scene.Entity.activeSelf&&ag&&ag.enabled&&d.Omens.ValeGlimpses==1,"...it backs away out of sight, once");
      yield return Face(E+Vector3.up*1.2f);yield return new WaitForSeconds(3f);Require(d.Omens.ValeGlimpses==1&&d.Omens.ValeStage==0,"...and does not come back");}
     else{var at=d.Scene.Entity.transform.position;d.Player.SmokePlaceWalker(at+FlatV(d.Scene.Walker.transform.position-at).normalized*3f);float tr=Time.time+1.6f;while(d.Omens.ValeStage>0&&Time.time<tr)yield return null;
      Require(d.Omens.ValeStage==0&&!d.Scene.Entity.activeSelf,"Rush it and it is gone at once");}}}
   d.Player.SmokeSprint=false;d.Player.SmokeWalk=Vector2.zero;
  }
  // ---- V21: doorstep choreography, gestures, time cards, dread, grass foley, dice, parked-car body.
  IEnumerator V21(){
   var res=FindAnyObjectByType<ServiceResidents>();var hands=d.Scene.View.GetComponentInChildren<ServiceHands>();
   foreach(var c in new[]{"Knock","Give","Place","Push","Torch"})Require(hands.Rig.GetClip(c)!=null,"Hands have a "+c+" gesture");
   Require(hands.Envelope&&hands.EnvelopeGive,"Papers rigged to the left hand");
   foreach(int index in new[]{0,4}){
    d.BeginShift(0);d.PaperOpen=false;var p=d.Property(index);var o=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.4f);
    yield return Face(p.KnockPoint.position);res.ResetClearance();int g0=hands.Gestures;
    d.Attempt(index,ServiceResult.Served);bool sawInDoorway=false;float until=Time.time+40;while(d.Busy&&Time.time<until){if(res.InDoorway(index))sawInDoorway=true;yield return null;}yield return new WaitForSeconds(1);
    Require(d.ResultAt(index)==ServiceResult.Served,"Served in person at "+index);
    Require(sawInDoorway,"Resident steps up into the doorway at "+index);
    Require(res.LastClearance>0,"The door leaf never passes through the resident at "+index+" (closest "+res.LastClearance.ToString("F2")+" m)");
    Require(hands.Gestures-g0>=2,"Knock and hand-over gestures played at "+index+" ("+(hands.Gestures-g0)+")");
    Require(!res.Answering(index),"Resident gone once the door is shut at "+index);}
   // a night-one stranger's door: the knock, a beat of silence, then the hand pushes the door as the latch gives
   {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(3);var o=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.4f);yield return Face(p.KnockPoint.position);
    d.Attempt(3,ServiceResult.Served);float t0=Time.time;float until=t0+20;string heard="";while(d.Busy&&Time.time<until){if(!string.IsNullOrEmpty(d.Notice)&&!heard.Contains(d.Notice))heard+=d.Notice+"|";yield return null;}
    Require(heard.Contains(ServiceScript.NoAnswer),"The 'no answer' line is shown before the latch gives");Require(Time.time-t0>3,"A beat of silence before the door gives ("+(Time.time-t0).ToString("F1")+" s)");Require(hands.Current=="Push"||hands.Gestures>0,"Hand pushes the door");}
   // time cards
   {var tc=d.Timecard;Require(tc!=null,"Time cards installed");tc.NightEnd();yield return null;Require(tc.Blocking&&d.InputBlocked,"A time card blocks input while it is up");float until=Time.time+12;while(tc.CardUp&&Time.time<until)yield return null;
    Require(!tc.CardUp&&!tc.Blocking,"The time card types, holds and fades ("+tc.LastCard+")");Require(tc.LastCard.EndsWith("AM")||tc.LastCard.EndsWith("PM"),"The card reads as a time ("+tc.LastCard+")");
    Require(ServiceTimecard.Time12(new DateTime(1998,10,2,1,40,0))=="1:40 AM","Clock format reads like 1:40 AM");
    // the corner stamp and the card text sit on the screen (the V21 tour caught the stamp 340 px off the left edge)
    tc.Stamp("77 LATIGO TRAIL","10:14 PM");yield return new WaitForSeconds(1.2f);var corners=new Vector3[4];tc.StampRect.GetWorldCorners(corners);
    Require(corners[0].x>=0&&corners[0].y>=0&&corners[2].x<=Screen.width&&corners[2].y<=Screen.height&&corners[0].x<Screen.width*.2f&&corners[0].y<Screen.height*.25f,"Arrival stamp sits in the bottom-left corner of the screen ("+corners[0]+")");
    tc.CardRect.GetWorldCorners(corners);var mid=(corners[0]+corners[2])*.5f;Require(Mathf.Abs(mid.x-Screen.width*.5f)<Screen.width*.1f&&Mathf.Abs(mid.y-Screen.height*.5f)<Screen.height*.2f,"Card time is centred on the screen ("+mid+")");}
   // dread: a pursuit tears the tape, and it heals afterwards
   {d.BeginShift(1);d.PaperOpen=false;var p=d.Property(5);d.Player.SmokePlaceWalker(p.TableApproach.position);yield return null;d.Horror.Begin(5);yield return new WaitForSeconds(4);
    Require(d.Dread.GlitchLevel>.05f&&ServiceCamcorder.Glitch>.05f,"A pursuit damages the picture (glitch "+d.Dread.GlitchLevel.ToString("F2")+", threat "+d.Dread.Threat.ToString("F2")+")");
    d.Horror.ResetEncounter();yield return new WaitForSeconds(3.5f);Require(ServiceCamcorder.Glitch<.25f,"The picture recovers after the pursuit (glitch "+ServiceCamcorder.Glitch.ToString("F2")+")");}
   // grass foley
   {Require(d.Foliage&&d.Foliage.Clumps>1500,"Grass and brush indexed for footsteps ("+(d.Foliage?d.Foliage.Clumps:0)+")");
    var grass=d.Scene.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name.StartsWith("Grass_Tall")&&r.bounds.size.y>.3f);Require(grass&&d.Foliage.Depth(grass.bounds.center)>0,"Standing in grass registers");}
   // fuzzy dice swing with the car
   {var dice=d.Scene.GetComponentInChildren<ServiceDice>(true);Require(dice&&dice.Pivots!=null&&dice.Pivots.Length==2,"Fuzzy dice hang from the mirror");
    d.BeginShift(0);d.PaperOpen=false;yield return new WaitForSeconds(.3f);d.Player.StartEngine();yield return new WaitForSeconds(1.2f);float peak=0;float until=Time.time+2.5f;while(Time.time<until){d.Player.SmokeThrottle=1;peak=Mathf.Max(peak,dice.Swing);yield return null;}
    until=Time.time+1.5f;while(Time.time<until){d.Player.SmokeThrottle=0;d.Player.SmokeBrake=true;peak=Mathf.Max(peak,dice.Swing);yield return null;}d.Player.SmokeBrake=false;
    Require(peak>2,"The dice swing when the car pulls away and brakes ("+peak.ToString("F1")+" deg)");
    // the dashboard spill dims as the head turns to a side window (it lit the near sleeve into a flat sheet)
    var spill=d.Scene.Car.GetComponentsInChildren<Light>(true).FirstOrDefault(l=>l.name=="Dashboard ambient spill");
    if(spill){d.Player.SmokeLook(0,4);yield return new WaitForSeconds(.3f);float ahead=spill.intensity;d.Player.SmokeLook(70,4);yield return new WaitForSeconds(.3f);float side=spill.intensity;d.Player.SmokeLook(0,4);yield return new WaitForSeconds(.2f);
     Require(ahead>0&&side<ahead*.3f&&Mathf.Abs(spill.intensity-ahead)<.001f,"Dash spill dims looking out of the side window ("+ahead.ToString("F3")+" -> "+side.ToString("F3")+")");}
    else Require(false,"Dashboard spill light present");}
   // grass under the car is laid flat while you sit in it, and stands again when the car moves off
   {d.BeginShift(0);d.PaperOpen=false;yield return new WaitForSeconds(.3f);
    var grass=d.Scene.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.name.StartsWith("Grass_")&&r.bounds.size.y>.4f&&d.Scene.Properties.All(h=>Vector3.Distance(h.Door.position,r.bounds.center)>40)).OrderBy(r=>Vector3.Distance(r.bounds.center,d.Scene.Car.position)).FirstOrDefault();
    if(grass){var spot=grass.bounds.center;spot.y=grass.bounds.min.y+.3f;d.Player.TeleportCar(spot,d.Scene.Car.rotation);yield return new WaitForSeconds(.4f);
     Require(d.Player.InCar&&!grass.enabled&&d.Foliage.Flattened>0,"Grass under the car is not drawn through the cabin ("+d.Foliage.Flattened+" laid flat)");
     d.Player.TeleportCar(spot+d.Scene.Car.forward*14+Vector3.up*.3f,d.Scene.Car.rotation);yield return new WaitForSeconds(.4f);
     Require(grass.enabled,"Grass stands again once the car has moved off");}
    else Require(false,"Found roadside grass for the cabin test");}
   // the parked car's body stops the walker (not only a capsule at its centre)
   {d.BeginShift(0);d.PaperOpen=false;var car=d.Scene.Car;d.Player.SmokePlaceWalker(car.position+car.forward*4.2f);yield return null;d.Player.SmokeFace(car.position);float until=Time.time+2.5f;while(Time.time<until){d.Player.SmokeFace(car.position);d.Player.SmokeWalk=Vector2.up;yield return null;}d.Player.SmokeWalk=Vector2.zero;
    var rel=car.InverseTransformPoint(d.Scene.Walker.transform.position);Require(Mathf.Abs(rel.z)>1.95f,"Walking into the parked car stops at the bumper ("+Mathf.Abs(rel.z).ToString("F2")+" m from its centre)");}
  }
 }
}
