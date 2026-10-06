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
   if(Environment.GetCommandLineArgs().Contains("-smokeV23Only")){yield return V23();yield break;}
   if(Environment.GetCommandLineArgs().Contains("-smokeV25Only")){yield return V25();yield break;}
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
   d.Horror.ResetEncounter();d.Player.SmokePlaceWalker(watcher.TableApproach.position);yield return null;d.Horror.Begin(3);yield return null;var away=d.Scene.View.transform.position-(d.Horror.Agent.transform.position-d.Scene.View.transform.position);yield return Face(away);{float tw=Time.time+9.2f,nx=0;while(Time.time<tw){if(Time.time>nx){nx=Time.time+.3f;d.Horror.SteadyNerve();}yield return null;}}
   Require(!d.Horror.Caught&&!d.Horror.Active,"Standing still facing away, holding your nerve (E), survives the watcher ("+d.Horror.NervePresses+" presses)");
   // V25: ...but stand there doing nothing and your nerve goes - you turn round
   {d.Horror.ResetEncounter();d.Player.SmokePlaceWalker(watcher.TableApproach.position);yield return null;d.Horror.Begin(3);yield return Face(away);int nb=d.Horror.NerveBreaks;float tw=Time.time+9;while(Time.time<tw&&!d.Horror.Caught)yield return null;
    Require(d.Horror.Caught&&d.Horror.NerveBreaks==nb+1,"Not holding your nerve, you turn round and it has you ("+d.Horror.Elapsed.ToString("F1")+" s)");yield return new WaitForSeconds(4f);d.Horror.ResetEncounter();}
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
   yield return V23();
   yield return V25();
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
  // V23: Rex has a body now - skip it (and anything else of the dog) when asking what he stands on
  bool OnBuilding(Vector3 at){var dog=d&&d.Life?d.Life.DogTransform:null;RaycastHit hit=default;bool any=false;foreach(var h in Physics.RaycastAll(at+Vector3.up*1f,Vector3.down,3f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore)){if(dog&&h.collider.transform.IsChildOf(dog))continue;if(!any||h.distance<hit.distance){hit=h;any=true;}}
   if(!any||hit.collider is TerrainCollider)return false;var s=hit.collider.GetComponentInParent<ServiceSurface>();return s==null||s.Kind=="wood";}
  // ---- V23 (the playtest round after V22)
  // ---------------------------------------------------------------- V25
  static Vector3 Along25(Vector3[] c,float need){for(int i=1;i<c.Length;i++){float len=Vector3.Distance(c[i-1],c[i]);if(len>=need)return Vector3.Lerp(c[i-1],c[i],need/Mathf.Max(len,1e-4f));need-=len;}return c[c.Length-1];}
  IEnumerator Watch25(Vector3 at,float seconds,Func<bool> until){float t=Time.time+seconds;while(Time.time<t&&!until()){var dl=at-d.Scene.View.transform.position;d.Player.SmokeLook(Mathf.Atan2(dl.x,dl.z)*Mathf.Rad2Deg,-Mathf.Atan2(dl.y,new Vector2(dl.x,dl.z).magnitude)*Mathf.Rad2Deg);yield return null;}}
  IEnumerator V25(){
   // -- Bell's night-two ambush, leaving straight across the lawn (a playtester did, and nothing came)
   {d.BeginShift(1);d.PaperOpen=false;yield return null;var p=d.Property(4);int am=d.Horror.ReturnAmbushes;
    var ko=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+ko*1.6f);yield return new WaitForSeconds(.3f);yield return Face(p.KnockPoint.position);d.Attempt(4,ServiceResult.Served);{float tk=Time.time+15;while(d.Busy&&Time.time<tk)yield return null;} // knock first: the latch gives
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.3f);yield return Face(p.DeliveryPoint.position);d.Attempt(4,ServiceResult.LeftAtDoor);float t=Time.time+12;while(d.Busy&&Time.time<t)yield return null;
    var o=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+o*2.6f);yield return new WaitForSeconds(2.2f);
    var R=p.ApproachRoute;var down=FlatV(R[0]-R[R.Length-1]).normalized;var lawn=Vector3.Cross(Vector3.up,down);
    var target=p.Door.position+o*3f+lawn*28f;if(Physics.Raycast(p.Door.position+o*3f+Vector3.up*1.2f,lawn,12f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))target=p.Door.position+o*3f-lawn*28f;
    t=Time.time+16;while(Time.time<t&&d.Horror.ReturnAmbushes==am){d.Player.SmokeFace(target+Vector3.up*1.5f);d.Player.SmokeWalk=Vector2.up;yield return null;}d.Player.SmokeWalk=Vector2.zero;
    DriveProgress25(p,d.Scene.Walker.transform.position,out float lat);
    Require(d.Horror.ReturnAmbushes==am+1,"Bell's night-two ambush comes when you leave across the lawn too ("+lat.ToString("F1")+" m off the drive; "+d.Horror.ReturnState+")");d.Horror.ResetEncounter();}
   // -- the blink: night one, on Morrow's landing, someone is standing at the desk
   {d.BeginShift(0);d.PaperOpen=false;yield return null;var p=d.Property(5);int shows=d.Blink.Shows;var desk=p.TableApproach.position;Vector3 spot=desk;bool ok=false;
    var path=new UnityEngine.AI.NavMeshPath();
    if(UnityEngine.AI.NavMesh.SamplePosition(desk,out var a,1f,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.SamplePosition(p.Door.position-Outward(p)*1.5f,out var b,2f,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,UnityEngine.AI.NavMesh.AllAreas,path))
     for(float back=8f;back>=4.5f&&!ok;back-=.5f){var at=Along25(path.corners,back);if(Mathf.Abs(at.y-desk.y)>.6f)continue;if(ServiceInteraction.Clear(at+Vector3.up*1.6f,desk+Vector3.up*1.5f,null,d.Scene.Walker.transform)){spot=at;ok=true;}}
    Require(ok,"A place on Morrow's landing with the desk in sight");
    d.Player.SmokePlaceWalker(spot);yield return new WaitForSeconds(.3f);yield return Watch25(desk+Vector3.up*1.3f,6f,()=>d.Blink.Shows>shows);
    Require(d.Blink.Shows==shows+1&&d.Blink.LastSpot==0,"Night one on Morrow's landing: your character blinks, and someone is standing at the desk");
    float t=Time.time+1.5f;while(Time.time<t&&!d.Blink.FigureShown)yield return null;Require(d.Blink.FigureShown,"...she is there between the blinks");yield return new WaitForSeconds(.55f);yield return Shot("v25-blink-morrow");
    t=Time.time+7;while(Time.time<t&&d.Blink.FigureShown)yield return null;Require(!d.Blink.FigureShown&&d.Blink.Blinks>=2,"...and after the next blink she is gone ("+d.Blink.Blinks+" blinks)");}
   // -- the blink: night two at Bell's, papers down, someone in the hall between you and the front door
   {d.BeginShift(1);d.PaperOpen=false;yield return null;var p=d.Property(4);var ko=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+ko*1.6f);yield return new WaitForSeconds(.3f);yield return Face(p.KnockPoint.position);d.Attempt(4,ServiceResult.Served);{float tk=Time.time+15;while(d.Busy&&Time.time<tk)yield return null;} // knock first: the latch gives
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.3f);yield return Face(p.DeliveryPoint.position);
    d.Attempt(4,ServiceResult.LeftAtDoor);float t=Time.time+12;while(d.Busy&&Time.time<t)yield return null;d.Horror.ResetEncounter();int shows=d.Blink.Shows;
    bool spotOk=ServiceBlink.HallSpot(p,d.Scene.Walker.transform.position,out var hall);Vector3 view=d.Scene.Walker.transform.position;bool ok=false;
    if(spotOk){var path=new UnityEngine.AI.NavMeshPath();if(UnityEngine.AI.NavMesh.SamplePosition(hall,out var a,1f,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.SamplePosition(view,out var b,1.5f,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.CalculatePath(a.position,b.position,UnityEngine.AI.NavMesh.AllAreas,path))
      for(float back=7f;back>=3.6f&&!ok;back-=.4f){var at=Along25(path.corners,back);if(ServiceInteraction.Clear(at+Vector3.up*1.6f,hall+Vector3.up*1.5f,null,d.Scene.Walker.transform)&&ServiceLife.Indoors(p,at)){view=at;ok=true;}}}
    Require(spotOk&&ok,"Bell's hall: the spot between you and the door, and a place in the house that sees it");
    d.Player.SmokePlaceWalker(view);yield return new WaitForSeconds(.3f);yield return Watch25(hall+Vector3.up*1.3f,6f,()=>d.Blink.Shows>shows);
    Require(d.Blink.Shows==shows+1&&d.Blink.LastSpot==1,"Night two at Bell's, papers down: a blink, and someone is standing in the hall");{float tf=Time.time+1.5f;while(Time.time<tf&&!d.Blink.FigureShown)yield return null;}yield return new WaitForSeconds(.55f);yield return Shot("v25-blink-bell");
    t=Time.time+7;while(Time.time<t&&d.Blink.FigureShown)yield return null;Require(!d.Blink.FigureShown,"...gone after the next blink");d.Horror.ResetEncounter();}
   // -- the blink: night three, Route 9, a figure on the shoulder in the headlights
   {d.BeginShift(2);d.PaperOpen=false;yield return null;var L=d.Scene.LateRoute;int shows=d.Blink.Shows;
    d.Player.TeleportCar(L[1].position+Vector3.up*.3f,Quaternion.LookRotation(FlatV(L[2].position-L[1].position)));yield return new WaitForSeconds(.4f);d.Player.StartEngine();yield return new WaitForSeconds(1.2f);
    string closest="";float best=99;float t=Time.time+30;int at=2;
    while(Time.time<t&&d.Blink.Shows==shows){var c=d.Scene.Car.position;if(at<L.Length-1&&FlatV(L[at].position-c).magnitude<8f)at++;var to=FlatV(L[at].position-c);
     d.Player.SmokeSteering=Mathf.Clamp(Vector3.SignedAngle(FlatV(d.Scene.Car.forward),to,Vector3.up)/28f,-1,1);d.Player.SmokeThrottle=d.Player.Speed<8?.7f:.05f;
     float dd=Vector3.Distance(d.Scene.View.transform.position,d.Blink.RoadSpot());if(dd<best&&dd>14f){best=dd;closest=d.Blink.RoadDebug;}if(dd<12f&&best<99)break;yield return null;}
    d.Player.SmokeThrottle=0;d.Player.SmokeSteering=0;d.Player.SmokeBrake=true;yield return new WaitForSeconds(.8f);d.Player.SmokeBrake=false;
    Require(d.Blink.Shows==shows+1&&d.Blink.LastSpot==2,"Night three on Route 9: a blink, and someone is standing on the shoulder in the headlights ("+closest+")");}
   // -- the Correll branch
   {d.CorrellBranch=false;d.AllowBranchInTests=true;ServiceDialogue.ForcePick=2;
    d.BeginShift(0);d.PaperOpen=false;yield return null;var p=d.Property(0);d.Player.SmokePlaceWalker(p.Door.position+Outward(p)*1.6f);yield return new WaitForSeconds(.3f);yield return Face(p.KnockPoint.position);
    d.Attempt(0,ServiceResult.Served);float t=Time.time+40;while(d.Busy&&Time.time<t)yield return null;ServiceDialogue.ForcePick=-1;
    Require(d.CorrellBranch,"Night one: telling Walter somebody ought to do something about that dog starts the Correll branch");
    d.BeginShift(1);d.PaperOpen=false;yield return null;yield return null;
    Require(d.Life.DogTransform&&!d.Life.DogTransform.gameObject.activeInHierarchy,"Night two: Rex is gone");
    var blood=p.transform.Find("V25 blood");Require(blood&&blood.gameObject.activeInHierarchy,"...and there is blood on the porch and in the hall");
    d.Player.SmokePlaceWalker(p.Door.position+Outward(p)*1.6f);yield return new WaitForSeconds(.3f);yield return Face(p.KnockPoint.position);d.Attempt(0,ServiceResult.Served);
    t=Time.time+40;while(d.Busy&&Time.time<t)yield return null;yield return Shot("v25-walter-door");
    Require(d.Walter.Chasing&&d.CorrellTurns>=1,"...Walter tells you it's your turn and comes out after you");
    t=Time.time+14;while(Time.time<t&&d.Phase==ServicePhase.Playing)yield return null;
    Require(d.Phase==ServicePhase.Finished&&d.EndingText==ServiceScript.EndingWalter&&d.Walter.Catches>=1,"...stand there and he reaches you: the route ends with the Walter epilogue");yield return Shot("v25-walter-ending");
    // again - this time run for the car and drive
    d.BeginShift(1);d.PaperOpen=false;yield return null;yield return null;var road=FlatV(p.ApproachRoute[0]-p.ApproachRoute[p.ApproachRoute.Length-1]).normalized;
    d.Player.TeleportCar(p.Gate.position+Vector3.up*.3f,Quaternion.LookRotation(road));yield return new WaitForSeconds(.3f);
    d.Player.SmokePlaceWalker(p.Door.position+Outward(p)*1.6f);yield return new WaitForSeconds(.3f);yield return Face(p.KnockPoint.position);d.Attempt(0,ServiceResult.Served);
    t=Time.time+40;while(d.Busy&&Time.time<t)yield return null;Require(d.Walter.Chasing,"(Walter comes out again)");
    var car=d.Scene.Car;yield return Sprint(car.position-car.right*2.2f,true,40,()=>d.CanEnterCar||d.Phase!=ServicePhase.Playing);
    if(d.CanEnterCar){d.Player.EnterCar();yield return new WaitForSeconds(.4f);d.Player.StartEngine();yield return new WaitForSeconds(1.2f);t=Time.time+6;while(Time.time<t&&d.Walter.Chasing){d.Player.SmokeThrottle=1;yield return null;}d.Player.SmokeThrottle=0;}
    Require(d.Phase==ServicePhase.Playing&&d.Walter.Escaped&&d.Walter.Catches==1,"...run for the car and drive: he stops in the road and you get away (closest "+d.Walter.Closest.ToString("F1")+" m)");
    d.CorrellBranch=false;d.AllowBranchInTests=false;d.BeginShift(0);d.PaperOpen=false;yield return null;}
  }
  static void DriveProgress25(ServiceProperty p,Vector3 at,out float lateral){lateral=float.MaxValue;var r=p.ApproachRoute;for(int i=1;i<r.Length;i++){var a=r[i-1];var b=r[i];a.y=b.y=at.y;var ab=b-a;float len=ab.magnitude;if(len<1e-4f)continue;float k=Mathf.Clamp01(Vector3.Dot(at-a,ab)/(len*len));lateral=Mathf.Min(lateral,Vector3.Distance(at,a+ab*k));}}
  IEnumerator V23(){
   var hud=d.GetComponentInChildren<ServiceHUD>();
   // -- the stamina meter: there on foot, draining while you sprint, not shown in the car
   {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(3);d.Player.SmokePlaceWalker(p.ApproachRoute[2]);yield return new WaitForSeconds(1.6f);
    Require(hud&&hud.StaminaShown>.2f,"The stamina meter shows on foot ("+(hud?hud.StaminaShown:0).ToString("F2")+")");float w0=hud.StaminaFillWidth;
    yield return Sprint(p.ApproachRoute[p.ApproachRoute.Length-1],true,3f,()=>false);d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeSprint=false;
    Require(hud.StaminaFillWidth<w0-20&&hud.StaminaShown>.8f,"...it drains, bright, while you sprint ("+w0.ToString("F0")+" -> "+hud.StaminaFillWidth.ToString("F0")+" px)");yield return Shot("v23-stamina");
    d.Player.EnterCar();yield return new WaitForSeconds(1.6f);Require(hud.StaminaShown<.05f,"...and it is not shown in the car");}
   // -- the torch eases down on a door note at arm's length (it burned the note to blank white)
   {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(3);var o=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+o*.8f);d.Scene.Flashlight.enabled=true;yield return new WaitForSeconds(.4f);yield return Face(p.NoticePoint.position);yield return new WaitForSeconds(.8f);
    float near=d.TorchExposure.Scale;yield return Shot("v23-note-torch");d.Player.SmokePlaceWalker(p.Door.position+o*4.5f);yield return new WaitForSeconds(.3f);yield return Face(p.NoticePoint.position);yield return new WaitForSeconds(1.4f);float far=d.TorchExposure.Scale;
    Require(near<.45f&&far>.85f,$"The torch eases down on the door note at arm's length (x{near:F2} at 0.8 m, x{far:F2} at 4.5 m)");}
   // -- Rex: calm in front of you while you look round, solid, a call from inside, and petting once Walter has him
   {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(0);var A=p.ApproachRoute;yield return new WaitForSeconds(.3f);d.Player.SmokePlaceWalker(A[Mathf.Min(8,A.Length-1)]);
    float t=Time.time+9;while(d.Life.Mode!=ServiceLife.DogMode.Escort&&Time.time<t){d.Player.SmokeFace(p.Door.position);d.Player.SmokeWalk=Vector2.up;yield return null;}d.Player.SmokeWalk=Vector2.zero;
    Require(d.Life.Mode==ServiceLife.DogMode.Escort,"Rex comes out to walk you up ("+d.Life.Mode+")");yield return new WaitForSeconds(1.5f);
    float travel0=d.Life.DogTravel;float yaw=0,end=Time.time+4;while(Time.time<end){yaw+=Time.deltaTime*110;d.Player.SmokeLook(yaw,5);yield return null;}
    float moved=d.Life.DogTravel-travel0;Require(moved<3f,"Looking round on the spot does not send Rex running in circles ("+moved.ToString("F1")+" m in 4 s)");
    var dg=d.Life.DogPosition;var from=dg+FlatV(d.Scene.Walker.transform.position-dg).normalized*2.2f;d.Player.SmokePlaceWalker(from);yield return new WaitForSeconds(.2f);float minD=9,jump=0;var prev=d.Life.DogPosition;
    end=Time.time+2.2f;while(Time.time<end){d.Player.SmokeFace(d.Life.DogPosition);d.Player.SmokeWalk=Vector2.up;var dp=d.Life.DogPosition;jump=Mathf.Max(jump,Vector3.Distance(dp,prev)/Mathf.Max(Time.deltaTime,1e-3f));prev=dp;minD=Mathf.Min(minD,Vector3.Distance(FlatV(dp),FlatV(d.Scene.Walker.transform.position)));yield return null;}d.Player.SmokeWalk=Vector2.zero;
    Require(minD>.4f&&jump<8f,$"Walking into Rex does not go through or shove him ({minD:F2} m at closest, {jump:F1} m/s at most)");
    // V23: Rex at the foot of the steps (where the tour walked into him and stuck) - you still get up to the door
    {var R=p.ApproachRoute;var foot=R[R.Length-1];d.Life.SmokePlaceDog(foot);var start=R[Mathf.Max(0,R.Length-4)];d.Player.SmokePlaceWalker(start);yield return new WaitForSeconds(.2f);
     var np=new UnityEngine.AI.NavMeshPath();UnityEngine.AI.NavMesh.SamplePosition(start,out var ns,1.5f,UnityEngine.AI.NavMesh.AllAreas);UnityEngine.AI.NavMesh.SamplePosition(p.Door.position+Outward(p)*.9f,out var ne,2f,UnityEngine.AI.NavMesh.AllAreas);
     UnityEngine.AI.NavMesh.CalculatePath(ns.position,ne.position,UnityEngine.AI.NavMesh.AllAreas,np);var cs=np.corners.Length>1?np.corners:new[]{start,p.Door.position};int ci=1;float tw=Time.time+8;bool reached=false;int yields0=d.Life.Yields;
     while(Time.time<tw){var wp=d.Scene.Walker.transform.position;if(FlatV(p.Door.position-wp).magnitude<1.7f){reached=true;break;}if(ci<cs.Length-1&&FlatV(cs[ci]-wp).magnitude<.4f)ci++;var aim=ci<cs.Length?cs[ci]:p.Door.position;
      d.Player.SmokeFace(aim+Vector3.up*1.5f);d.Player.SmokeWalk=Vector2.up;yield return null;}d.Player.SmokeWalk=Vector2.zero;
     Require(reached,$"Rex at the foot of Correll's steps does not hold you off the door ({FlatV(p.Door.position-d.Scene.Walker.transform.position).magnitude:F1} m short, dog {d.Life.Mode}, body yielded {d.Life.Yields-yields0}x)");}
    // knock: somebody calls out before the door opens; Walter hushes the dog
    int calls=d.WhosThereCalls;d.Player.SmokePlaceWalker(p.Door.position+Outward(p)*1.6f);yield return new WaitForSeconds(.3f);yield return Face(p.KnockPoint.position);d.Attempt(0,ServiceResult.Served);
    bool heard=false;t=Time.time+20;while(d.Busy&&Time.time<t){if(d.Dialogue.Active&&d.Dialogue.Line==ServiceScript.WhosThere(0,0))heard=true;yield return null;}
    Require(d.WhosThereCalls==calls+1&&heard,"Knock at Correll's: \"Who's out there?\" from inside before the door opens");
    Require(ServiceResidents.LastWalkPlayback>=.55f&&ServiceResidents.LastWalkPlayback<=1.1f&&ServiceResidents.LastWalkPace<1.6f,$"Walter steps at his stride (pace {ServiceResidents.LastWalkPace:F2} m/s, cycle at x{ServiceResidents.LastWalkPlayback:F2})");
    t=Time.time+40;while(d.Life.Mode!=ServiceLife.DogMode.Hushed&&d.Life.Mode!=ServiceLife.DogMode.Home&&Time.time<t)yield return null;
    var dn=d.Life.DogPosition;d.Player.SmokePlaceWalker(dn+FlatV(dn-p.Door.position).normalized*1.5f); // the yard side of him (he never climbs the steps to you)
    yield return new WaitForSeconds(.4f);yield return Face(d.Life.PetPoint);yield return null;
    var hearth=p.GetComponentInChildren<ServiceHearth>();Require(hearth&&hearth.Lit&&hearth.Glow&&hearth.Glow.enabled,"A fire burning in Correll's hearth on night one");
    {float tf=Time.time+1.5f;while(hearth.LiveFlames<6&&Time.time<tf)yield return null;}
    Require(hearth.FireBox&&hearth.FireBox.activeInHierarchy&&hearth.LiveFlames>5,"...flames you can see in its firebox ("+hearth.LiveFlames+" flame sprites)");
    Require(d.CanPetDog,"\"Pet Rex\" offered once Walter has hushed him ("+d.Life.Mode+")");int pets=d.Life.Pets;Require(d.TryPetDog(),"Pet him");t=Time.time+4;float petNear=99;while(d.Busy&&Time.time<t){petNear=Mathf.Min(petNear,FlatV(d.Life.DogPosition-d.Scene.Walker.transform.position).magnitude);yield return null;}
    Require(petNear<1f,"Rex comes in under your hand to be petted ("+petNear.ToString("F2")+" m, "+d.Life.PetStop+")");
    Require(d.Life.Pets==pets+1&&(d.Notice==ServiceScript.PetRex[0]||d.Notice==ServiceScript.PetRexAgain),"...he stands for it ('"+d.Notice+"')");yield return Shot("v23-pet-rex");}
   // -- night one: Harrow served, its door shut behind you; a few steps on it creaks open and the lights die
   {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(3);var o=Outward(p);d.Docket.Find(e=>e.Property==0).Result=ServiceResult.Served;
    d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.4f);yield return Face(p.KnockPoint.position);d.Attempt(3,ServiceResult.Served);float t=Time.time+15;while(d.Busy&&Time.time<t)yield return null;yield return new WaitForSeconds(.4f);
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.3f);yield return Face(p.DeliveryPoint.position);d.Attempt(3,ServiceResult.LeftAtDoor);t=Time.time+6;while(d.Busy&&Time.time<t)yield return null;
    Require(d.ResultAt(3)!=ServiceResult.Pending,"Harrow served on night one");
    var step=p.OpeningCentre-p.Inward*1.6f;d.Player.SmokePlaceWalker(step);yield return new WaitForSeconds(.3f);yield return Face(p.OpeningCentre+Vector3.up*1.2f);yield return null;Require(d.ToggleDoor(3),"Shut Harrow's door behind you");yield return new WaitForSeconds(1.5f);
    int lit=p.GetComponentsInChildren<Light>().Count(l=>l.enabled);var R=p.ApproachRoute;Vector3 away=R[0];for(int i=R.Length-1;i>=0;i--)if(Vector3.Distance(R[i],p.Door.position)>8){away=R[i];break;}
    d.Player.SmokePlaceWalker(away);yield return null;yield return Face(p.Door.position+Vector3.up*1.2f);t=Time.time+8;while(d.Omens.DoorBeats==0&&Time.time<t)yield return null;
    Require(d.Omens.DoorBeats==1,"A few steps on, Harrow's door creaks");t=Time.time+10;while(d.Omens.DoorBeatRunning&&Time.time<t)yield return null;
    int after=p.GetComponentsInChildren<Light>().Count(l=>l.enabled);Require(d.Life.DoorOpenDegrees(3)>40&&!d.ShutByPlayer(3),"...and swings open by itself ("+d.Life.DoorOpenDegrees(3).ToString("F0")+" deg)");
    Require(lit>0&&after==0,$"...and the lights go out ({lit} lit before, {after} after)");yield return Shot("v23-door-beat");
    var hz=p.GetComponentInChildren<ServiceHearth>();
    if(hz){float tf=Time.time+2.5f;while(hz.LiveFlames>0&&Time.time<tf)yield return null;Require(!hz.Lit&&hz.LiveFlames==0&&hz.FireBox&&hz.FireBox.activeInHierarchy,"...and Harrow's fire dies down to embers ("+hz.LiveFlames+" flames left)");}
    d.BeginShift(0);yield return null;yield return null;Require(p.GetComponentsInChildren<Light>().Count(l=>l.enabled)>0,"The lights are back for the next shift");
    if(hz){float tf=Time.time+1.5f;while(hz.LiveFlames<6&&Time.time<tf)yield return null;Require(hz.Lit&&hz.LiveFlames>5,"...and the fire is going again ("+hz.LiveFlames+" flames)");
     d.BeginShift(1);yield return null;yield return null;Require(!hz.Tonight&&hz.FireBox&&!hz.FireBox.activeInHierarchy,"Night two nobody keeps Harrow's fire: the iron cover is back over the hearth");d.BeginShift(0);yield return null;}}
   // -- books: leave the papers in Vale's study and a volume comes off the shelf
   {ServiceBooks.Force=true;d.BeginShift(0);d.PaperOpen=false;var p=d.Property(1);var o=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.4f);yield return Face(p.KnockPoint.position);d.Attempt(1,ServiceResult.Served);float t=Time.time+15;while(d.Busy&&Time.time<t)yield return null;
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.3f);yield return Face(p.DeliveryPoint.position);int falls=d.Books.Falls;d.Attempt(1,ServiceResult.LeftAtDoor);t=Time.time+9;while(d.Books.Falls==falls&&Time.time<t)yield return null;
    Require(d.Books.Falls==falls+1&&d.Books.LastHouse==1,"Leave the papers at Vale's: a book comes off the shelf");yield return new WaitForSeconds(3f);
    Require(d.Books.LastLanding.y<p.TableApproach.position.y+.4f&&d.Books.LastOut>.3f,"...and lands on the floor in front of the shelf ("+(d.Books.LastLanding.y-p.TableApproach.position.y).ToString("F2")+" m above the study floor, "+d.Books.LastOut.ToString("F2")+" m out from the row)");yield return Shot("v23-book-fell");ServiceBooks.Force=false;}
   // -- Morrow's doormat: wipe your feet and nothing comes for you there on night two
   {d.BeginShift(1);d.PaperOpen=false;foreach(var i in new[]{0,3,1,4})d.Docket.Find(e=>e.Property==i).Result=ServiceResult.Served;var p=d.Property(5);Require(d.Doormat!=null,"A doormat at Morrow's door");
    Require(d.EncounterTonight(5),"Night two at Morrow's: something comes for you if you walk in on her clean floor");
    d.Player.SmokePlaceWalker(d.Doormat.position+Vector3.up*.08f);yield return new WaitForSeconds(.4f);Require(d.CanWipeFeet,"Standing on the mat offers \"Wipe your feet\"");yield return Shot("v23-doormat");
    Require(d.TryWipeFeet(),"Wipe them");float t=Time.time+3;while(d.Busy&&Time.time<t)yield return null;Require(d.FeetWiped&&!d.EncounterTonight(5),"Feet wiped: nothing comes for you at Morrow's tonight");
    var o=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.3f);yield return Face(p.KnockPoint.position);d.Attempt(5,ServiceResult.Served);t=Time.time+15;while(d.Busy&&Time.time<t)yield return null;if(d.Life)d.Life.OpenDoor(p,true);
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.3f);yield return Face(p.DeliveryPoint.position);d.Attempt(5,ServiceResult.LeftAtDoor);t=Time.time+8;while(d.Busy&&Time.time<t)yield return null;yield return new WaitForSeconds(3f);
    Require(d.ResultAt(5)!=ServiceResult.Pending&&!d.Horror.Active&&d.Horror.Phase==PursuitPhase.Dormant,"...and leaving the papers upstairs, nothing comes");d.Horror.ResetEncounter();}
   // -- the watcher at Harrow on night two stands well off
   {d.BeginShift(1);d.PaperOpen=false;d.Docket.Find(e=>e.Property==0).Result=ServiceResult.Served;var p=d.Property(3);var o=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.4f);yield return Face(p.KnockPoint.position);d.Attempt(3,ServiceResult.Served);float t=Time.time+15;while(d.Busy&&Time.time<t)yield return null;
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.3f);yield return Face(p.DeliveryPoint.position);d.Attempt(3,ServiceResult.LeftAtDoor);t=Time.time+8;while(!d.Horror.Active&&Time.time<t)yield return null;
    Require(d.Horror.Active&&d.Horror.WatcherDistance>=4.5f,"The watcher at Harrow's stands well off ("+d.Horror.WatcherDistance.ToString("F1")+" m)");d.Horror.ResetEncounter();}
   // -- Bell's return ambush has its chase score
   {d.BeginShift(1);d.PaperOpen=false;foreach(var i in new[]{0,3,1})d.Docket.Find(e=>e.Property==i).Result=ServiceResult.Served;var p=d.Property(4);var o=Outward(p);
    d.Player.SmokePlaceWalker(p.Door.position+o*1.6f);yield return new WaitForSeconds(.4f);yield return Face(p.KnockPoint.position);d.Attempt(4,ServiceResult.Served);float t=Time.time+15;while(d.Busy&&Time.time<t)yield return null;
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.3f);yield return Face(p.DeliveryPoint.position);d.Attempt(4,ServiceResult.LeftAtDoor);t=Time.time+4;while(d.Busy&&Time.time<t)yield return null;
    int before=d.Horror.ReturnAmbushes;var A=p.ApproachRoute;Vector3 spot=A[0];for(int i=A.Length-1;i>=0;i--)if(Vector3.Distance(A[i],p.Door.position)>14){spot=A[i];break;}
    d.Player.SmokePlaceWalker(spot);t=Time.time+6;while(d.Horror.ReturnAmbushes==before&&Time.time<t)yield return null;t=Time.time+4;while(d.Audio.ChaseLevel<.05f&&Time.time<t)yield return null;
    Require(d.Horror.ReturnAmbushes==before+1&&d.Audio.ChaseLevel>=.05f,"Bell's ambush runs with the chase score ("+d.Audio.ChaseLevel.ToString("F2")+")");d.Horror.ResetEncounter();}
   // -- stairs: run at Correll's steps on the diagonal and you go up them (no invisible wall)
   {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(0);var st=p.GetComponentsInChildren<Renderer>().FirstOrDefault(r=>r.name.Contains("Stairs"));Require(st!=null,"Correll's porch steps");var sb=st.bounds;
    // the way down the steps: the horizontal axis along which the treads drop the most
    Vector3 down=Vector3.forward;Vector3 lowEnd=sb.center,highEnd=sb.center;float best=-1;
    foreach(var ax in new[]{Vector3.right,Vector3.forward}){float ext=Mathf.Abs(Vector3.Dot(sb.extents,ax))-.12f;var e1=sb.center+ax*ext;var e2=sb.center-ax*ext;
     float h1=Physics.Raycast(new Vector3(e1.x,sb.max.y+1,e1.z),Vector3.down,out var r1,sb.size.y+3,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)?r1.point.y:sb.min.y;float h2=Physics.Raycast(new Vector3(e2.x,sb.max.y+1,e2.z),Vector3.down,out var r2,sb.size.y+3,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)?r2.point.y:sb.min.y;
     if(Mathf.Abs(h1-h2)>best){best=Mathf.Abs(h1-h2);lowEnd=h1<h2?e1:e2;highEnd=h1<h2?e2:e1;down=(h1<h2?ax:-ax);}}
    var side=Vector3.Cross(Vector3.up,down);var start=lowEnd+down*1.8f+side*1.5f;start.y=sb.min.y+.1f;var topAim=new Vector3(highEnd.x,sb.max.y,highEnd.z);
    d.Player.SmokePlaceWalker(start);yield return new WaitForSeconds(.3f);float y0=d.Scene.Walker.transform.position.y;float t=Time.time+4f;
    while(Time.time<t&&d.Scene.Walker.transform.position.y<sb.max.y-.15f){d.Player.SmokeFace(topAim);d.Player.SmokeSprint=true;d.Player.SmokeWalk=Vector2.up;yield return null;}d.Player.SmokeWalk=Vector2.zero;d.Player.SmokeSprint=false;
    string block="nothing";var wp=d.Scene.Walker.transform.position;foreach(var hh in new[]{.15f,.4f,.9f})if(Physics.Raycast(wp+Vector3.up*hh,d.Scene.Walker.transform.forward,out var bh,.8f,~((1<<9)|(1<<10)),QueryTriggerInteraction.Ignore)&&!(bh.collider is CharacterController)){block=bh.collider.name+" at "+hh+" m (normal "+bh.normal+")";break;}yield return Shot("v23-stairs-diagonal");
    Require(d.Scene.Walker.transform.position.y>=sb.max.y-.2f,"Running at Correll's steps on the diagonal takes you up them (rose "+(d.Scene.Walker.transform.position.y-y0).ToString("F2")+" m of "+(sb.max.y-y0).ToString("F2")+"; in front: "+block+")");}
  }

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
    // V24: an invisible wall a car length into the drive - the car pulls off the road and stops there; the rest is on foot
    var p=d.Property(3);d.Player.TeleportCar(p.Gate.position+Vector3.up*.3f,Quaternion.LookRotation(FlatV(p.Gate.forward)));d.Player.StartEngine();yield return new WaitForSeconds(1.2f);bool said=false;
    var R=p.ApproachRoute;var footEnd=R[R.Length-1];until=Time.time+16;float stuck=0;Vector3 lastCar=d.Scene.Car.position;int kerbAtGate=d.Player.KerbContacts;
    while(Time.time<until){var c=d.Scene.Car.position;int ni=0;float nd=1e9f;for(int i=0;i<R.Length;i++){float dd=Vector3.Distance(FlatV(R[i]),FlatV(c));if(dd<nd){nd=dd;ni=i;}}
     var tgt=R[Mathf.Min(R.Length-1,ni+3)];var fw=FlatV(d.Scene.Car.forward);var to=FlatV(tgt-c);d.Player.SmokeSteering=Mathf.Clamp(Vector3.SignedAngle(fw,to,Vector3.up)/28f,-1,1);d.Player.SmokeThrottle=d.Player.Speed<5?.8f:.1f;
     if(d.Notice==ServiceScript.ParkAndWalk)said=true;stuck=Vector3.Distance(c,lastCar)<.02f?stuck+Time.deltaTime:0;lastCar=c;if(stuck>1.2f)break;yield return null;}
    d.Player.SmokeThrottle=0;d.Player.SmokeSteering=0;d.Player.SmokeBrake=true;yield return new WaitForSeconds(.8f);d.Player.SmokeBrake=false;
    float upDrive=Vector3.Distance(FlatV(d.Scene.Car.position),FlatV(p.Gate.position));float toFoot=Vector3.Distance(FlatV(d.Scene.Car.position),FlatV(footEnd));
    Require(upDrive>2.5f&&upDrive<7f&&toFoot>25f&&d.Player.OnCorridor&&d.Player.KerbContacts>kerbAtGate,"Harrow's drive: full throttle up it, the car stops at the wall by the road ("+upDrive.ToString("F1")+" m past the gate, "+toFoot.ToString("F0")+" m left to walk to the steps, said '"+said+"')");yield return Shot("v24-car-drive-wall");
    {string reaches="";bool allShort=true;for(int i=0;i<6;i++){float rch=d.Player.Roads.ReachOf(i),wl=d.Player.Roads.WalkLeftOf(i);reaches+=$" p{i} {rch:F1} up/{wl:F0} to walk";if(rch>ServiceRoadCorridor.PullOff+.01f||rch<=0||wl<ServiceRoadCorridor.MinWalk-.05f)allShort=false;}
     Require(allShort,"Every drive's wall is a pull-off by the road, never the house (m up each drive / least walk from the bonnet:"+reaches+")");}
    // Route 9's drive is only 8 m: drive straight at the cabin and the bonnet stops well short of its steps
    {d.BeginShift(2);d.PaperOpen=false;yield return null;yield return null;var q=d.Property(2);var RQ=q.ApproachRoute;var steps=RQ[RQ.Length-1];
     d.Player.TeleportCar(q.Gate.position+Vector3.up*.3f,Quaternion.LookRotation(FlatV(steps-q.Gate.position)));d.Player.StartEngine();yield return new WaitForSeconds(1.2f);
     int k0=d.Player.KerbContacts;float u2=Time.time+9,st=0;var lc=d.Scene.Car.position;
     while(Time.time<u2){var c=d.Scene.Car.position;var to=FlatV(steps-c);d.Player.SmokeSteering=Mathf.Clamp(Vector3.SignedAngle(FlatV(d.Scene.Car.forward),to,Vector3.up)/28f,-1,1);d.Player.SmokeThrottle=d.Player.Speed<4?.8f:.1f;
      st=Vector3.Distance(c,lc)<.02f?st+Time.deltaTime:0;lc=c;if(st>1.2f)break;yield return null;}
     d.Player.SmokeThrottle=0;d.Player.SmokeSteering=0;d.Player.SmokeBrake=true;yield return new WaitForSeconds(.6f);d.Player.SmokeBrake=false;
     float nose=FlatV(steps-(d.Scene.Car.position+d.Scene.Car.forward*1.85f)).magnitude;
     Require(d.Player.KerbContacts>k0&&nose>=5f,$"Route 9: driving straight at the cabin, the bonnet stops {nose:F1} m short of its steps (wall {d.Player.Roads.ReachOf(2):F1} m up the drive)");yield return Shot("v24-route9-wall");
     d.BeginShift(0);d.PaperOpen=false;yield return null;}
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
    Require(d.Omens.ValeStage==0&&d.Omens.ValeGlimpses==1&&!d.Scene.Entity.activeSelf,"...and backs away out of sight ("+Vector3.Distance(seenAt,d.Omens.ValeTo).ToString("F1")+" m away)");
    Require(d.Omens.PeekHold>=2.9f,"V25: ...after holding there, watching you, "+d.Omens.PeekHold.ToString("F1")+" s ("+(d.Omens.ValePeeked?"peeking round the door frame":"stepped out")+") - not a one-frame glitch");}
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
