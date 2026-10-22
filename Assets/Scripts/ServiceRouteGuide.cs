using UnityEngine;
namespace ServiceGameV2 {
 // V20 navigation, part 2 (runtime): replaces the drawn route map with what a process server in 1998 would have:
 // the next address and where it lies from here (a small objective line, top left, in the manner of Fears to Fathom),
 // the numbered mailboxes at every drive (ServiceV20Guide), and an inner-voice line as the right mailbox comes up or
 // goes by. Positions are measured along the county road from the depot (ServiceProperty.RoadDistance), so "ahead",
 // "behind" and "on your left" are true for whichever way the car is pointing.
 public sealed class ServiceRouteGuide:MonoBehaviour {
  Transform[] bridged; // V21: the last county-road point joined to County Route 9, so distances past the barricade count the gap
  ServiceDirector d;int saidApproach=-1,saidMissed=-1,lastTarget=-2;float nextCheck;
  public int Target {get;private set;}=-1;   // property index, or -1 for the depot when the docket is clear
  public string Objective {get;private set;}=""; public string Direction {get;private set;}="";
  static readonly string[] Address={"214 MILLBROOK RD","77 LATIGO TRAIL","1 COUNTY ROUTE 9","236 MILLBROOK RD","91 LATIGO TRAIL","108 LATIGO TRAIL"};
  static readonly string[] Who={"CORRELL","VALE HOUSE","","HARROW LODGE","BELL","MORROW HOUSE"};
  // Written directions, as the dispatcher wrote them on the docket (night three's last stop is in someone else's hand).
  public static readonly string[] Directions={"North on Millbrook. First drive on the LEFT. Dog in the yard.","Past the LATIGO TRAIL sign. Next drive on the RIGHT.","Past the end of the road. You know the way.","Keep north. First drive on the RIGHT.","Long drive on the LEFT after Vale.","Last drive on the RIGHT before the barricade."};
  public void Initialize(ServiceDirector director){d=director;}
  public void ResetForShift(){saidApproach=saidMissed=wasNear=-1;lastTarget=-2;Objective=Direction="";lastBearing="AHEAD";lastWay=0;ToCar=false;}
  // V28: the way back on foot is the car (the marker points at it then)
  public bool ToCar {get;private set;}
  int wasNear=-1;string lastBearing="AHEAD";int lastWay;
  // The next stop in docket order that is still pending (the docket is written in road order from the depot).
  int NextStop(){if(d.EndedEarly)return -1;foreach(var e in d.Docket)if(e.Result==ServiceResult.Pending)return e.Property;return -1;}
  // Position along the road: main road from the depot, then County Route 9 past the barricade.
  float Along(Vector3 at,out Vector3 tangent){
   var tg=Vector3.forward;float best=float.MaxValue,run=0,arc=0;var q=at;q.y=0;
   void Walk(Transform[] pts,float offset){if(pts==null)return;float r=0;for(int i=1;i<pts.Length;i++){if(!pts[i-1]||!pts[i])continue;var a=pts[i-1].position;var b=pts[i].position;a.y=b.y=0;var ab=b-a;float len=ab.magnitude;if(len<1e-4f)continue;float t=Mathf.Clamp01(Vector3.Dot(q-a,ab)/(len*len));float dd=(q-(a+ab*t)).sqrMagnitude;if(dd<best){best=dd;arc=offset+r+t*len;tg=ab/len;}r+=len;}run=offset+r;}
   Walk(d.Scene.Route,0);if(d.NightIndex>=2&&d.Scene.LateRoute!=null&&d.Scene.LateRoute.Length>1){if(bridged==null){bridged=new Transform[d.Scene.LateRoute.Length+1];bridged[0]=d.Scene.Route[d.Scene.Route.Length-1];System.Array.Copy(d.Scene.LateRoute,0,bridged,1,d.Scene.LateRoute.Length);}Walk(bridged,run);}
   tangent=tg;return arc;}
  void Update(){
   if(!d||d.Phase!=ServicePhase.Playing||Time.time<nextCheck)return;nextCheck=Time.time+.25f;
   var car=d.Scene.Car;bool inCar=d.Player.InCar;var me=inCar?car.position:d.Scene.Walker.transform.position;
   Target=NextStop();ToCar=false;
   // V21: at the door of (or inside) a house still on the docket, visited out of order, that house is the one being
   // guided; anywhere else the docket order stands (so walking past a drive does not flip the line back and forth)
   if(!inCar&&Target>=0){float best=float.MaxValue;foreach(var e in d.Docket){if(e.Result!=ServiceResult.Pending)continue;var h=d.Property(e.Property);if(!h||!h.Door)continue;
     float dist=Vector3.Distance(me,h.Door.position);if((h.InteriorBounds.Contains(me+Vector3.up*.3f)||dist<6)&&dist<best){best=dist;Target=e.Property;}}}
   if(Target!=lastTarget){lastTarget=Target;saidApproach=saidMissed=-1;}
   var look=d.Scene.View.transform.forward;
   if(Target<0){Objective="RETURN TO THE DEPOT";if(d.CanFinish)Direction="";else if(!inCar){ToCar=true;Direction="BACK TO THE CAR"+Tail(Bearing(me,look,car.position));}else Direction=Heading(me,car.forward,0,0,false,d.Scene.Depot?d.Scene.Depot.position:me);return;}
   var p=d.Property(Target);Objective="NEXT  "+Address[Target]+(Who[Target].Length>0?"  ·  "+Who[Target]:"");
   if(!p.Mailbox){Direction="";return;}
   // On foot at the property, the drive and the porch light do the work.
   if(!inCar&&Vector3.Distance(me,p.Door.position)<p.ApproachRouteLength()+12){
    // V21: indoors and on the step the line says what is left to do there
    // inside means past the door line too (the interior bounds take in the porch)
    // V28: in from the door along the doorway's own inward (the house's middle is not straight in from every door - at
    // Vale's her upstairs bedroom, along the same wall as the door, counted as outside: "UP THE DRIVE TO THE HOUSE")
    var outward=-p.Inward;outward.y=0;outward=outward.sqrMagnitude<.01f?p.Door.forward:outward.normalized;
    bool inside=p.InteriorBounds.Contains(me+Vector3.up*.3f)&&Vector3.Dot(me-p.Door.position,outward)< -.3f,open=d.AccessGranted(Target)&&!d.IsFriendly(Target);
    Direction=inside?(open?"LEAVE THE PAPERS ON THE TABLE":""):Vector3.Distance(me,p.Door.position)<3.2f?(open?(d.ShutByPlayer(Target)?"OPEN THE DOOR":"LEAVE THE PAPERS INSIDE"):d.AccessGranted(Target)?"":"KNOCK AT THE DOOR"):"UP THE DRIVE TO THE HOUSE"+Tail(Bearing(me,look,p.Door.position));return;}
   // on foot anywhere else, the way on is the car
   if(!inCar){ToCar=true;Direction="BACK TO THE CAR"+Tail(Bearing(me,look,car.position));return;}
   var spot=p.ApproachRoute!=null&&p.ApproachRoute.Length>0?p.ApproachRoute[0]:p.Door.position;
   Direction=Heading(me,car.forward,p.RoadDistance,p.RoadSide,true,spot);
   float sCar=Along(me,out var tan);float cAlong=Vector3.Dot(car.forward,tan);float ahead=(p.RoadDistance-sCar)*Mathf.Sign(cAlong);
   if(Mathf.Abs(p.RoadDistance-sCar)<38)wasNear=Target; // V28: "went past it" even when another line was on screen as it went by
   if(d.Busy||d.Horror.Active||(d.Dialogue&&d.Dialogue.Active)||!string.IsNullOrEmpty(d.Notice)||Mathf.Abs(cAlong)<.45f)return;
   if(saidApproach!=Target&&ahead>4&&ahead<38){saidApproach=Target;d.Say(Approach(Target));}
   else if(saidMissed!=Target&&wasNear==Target&&ahead<-22&&d.Player.Speed>2){saidMissed=Target;d.Say("Went past it. "+Address[Target].Split(' ')[0]+" was back there.");}
  }
  // V28 ("direction don't indicate when you face opposite way"): which way to go is read from where the car's nose (or
  // your face, on foot) actually points - along the road toward the stop, across it (which way to pull out), or away.
  // A little stickiness so the line does not flicker on a slow three-point turn or a glance round.
  string Heading(Vector3 me,Vector3 forward,float targetS,int side,bool house,Vector3 spot){
   float sMe=Along(me,out var tan);forward.y=0;forward=forward.sqrMagnitude<1e-4f?tan:forward.normalized;float gap=targetS-sMe;
   if(Mathf.Abs(gap)<12)return (house?"THIS DRIVE  ·  ":"THE DEPOT  ·  ")+Bearing(me,forward,spot);
   var way=tan*Mathf.Sign(gap);float c=Vector3.Dot(forward,way);
   int w=c<(lastWay==-1?-.3f:-.5f)?-1:c<(lastWay==0?.55f:.4f)?0:1;lastWay=w;
   if(w<0)return "BEHIND YOU  ·  TURN AROUND";
   if(w==0)return Vector3.SignedAngle(forward,way,Vector3.up)>0?"TURN RIGHT ONTO THE ROAD":"TURN LEFT ONTO THE ROAD";
   if(!house)return "AHEAD";
   string lr=side*Vector3.Dot(forward,tan)>0?"RIGHT":"LEFT";
   return Mathf.Abs(gap)<60?"COMING UP ON THE "+lr:"FURTHER ALONG  ·  ON THE "+lr;}
  string Bearing(Vector3 me,Vector3 forward,Vector3 at){var to=at-me;to.y=0;forward.y=0;if(to.sqrMagnitude<.25f)return "HERE";float a=Vector3.SignedAngle(forward,to,Vector3.up),abs=Mathf.Abs(a);
   string b=abs>(lastBearing=="BEHIND YOU"?120f:140f)?"BEHIND YOU":abs<(lastBearing=="AHEAD"?50f:35f)?"AHEAD":a>0?"ON YOUR RIGHT":"ON YOUR LEFT";lastBearing=b;return b;}
  static string Tail(string b)=>b=="AHEAD"||b=="HERE"?"":"  ·  "+b;
  static string Approach(int i){switch(i){case 0:return "That's 214. Correll's. Dog's going to start.";case 3:return "236. That's Harrow Lodge.";case 1:return "77 on the box. Vale House.";case 4:return "91. Bell's drive, the long one.";case 5:return "108. Morrow's. Last one before the barricade.";default:return "There's a box. 1. Same as Correll's.";}}
 }
 public static class ServicePropertyGuideExtensions {
  public static float ApproachRouteLength(this ServiceProperty p){if(p.ApproachRoute==null||p.ApproachRoute.Length<2)return 20;float l=0;for(int i=1;i<p.ApproachRoute.Length;i++)l+=Vector3.Distance(p.ApproachRoute[i-1],p.ApproachRoute[i]);return l;}
 }
}
