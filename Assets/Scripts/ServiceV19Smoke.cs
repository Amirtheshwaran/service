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
