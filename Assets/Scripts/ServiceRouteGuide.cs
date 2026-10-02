using UnityEngine;
namespace ServiceGameV2 {
 // V20 navigation, part 2 (runtime): replaces the drawn route map with what a process server in 1998 would have:
 // the next address and where it lies from here (a small objective line, top left, in the manner of Fears to Fathom),
 // the numbered mailboxes at every drive (ServiceV20Guide), and an inner-voice line as the right mailbox comes up or
 // goes by. Positions are measured along the county road from the depot (ServiceProperty.RoadDistance), so "ahead",
 // "behind" and "on your left" are true for whichever way the car is pointing.
 public sealed class ServiceRouteGuide:MonoBehaviour {
  ServiceDirector d;int saidApproach=-1,saidMissed=-1,lastTarget=-2;float nextCheck;
  public int Target {get;private set;}=-1;   // property index, or -1 for the depot when the docket is clear
  public string Objective {get;private set;}=""; public string Direction {get;private set;}="";
  static readonly string[] Address={"214 MILLBROOK RD","77 LATIGO TRAIL","1 COUNTY ROUTE 9","236 MILLBROOK RD","91 LATIGO TRAIL","108 LATIGO TRAIL"};
  static readonly string[] Who={"CORRELL","VALE HOUSE","","HARROW LODGE","BELL","MORROW HOUSE"};
  // Written directions, as the dispatcher wrote them on the docket (night three's last stop is in someone else's hand).
  public static readonly string[] Directions={"North on Millbrook. First drive on the LEFT. Dog in the yard.","Past the LATIGO TRAIL sign. Next drive on the RIGHT.","Past the end of the road. You know the way.","Keep north. First drive on the RIGHT.","Long drive on the LEFT after Vale.","Last drive on the RIGHT before the barricade."};
  public void Initialize(ServiceDirector director){d=director;}
  public void ResetForShift(){saidApproach=saidMissed=-1;lastTarget=-2;Objective=Direction="";}
  // The next stop in docket order that is still pending (the docket is written in road order from the depot).
  int NextStop(){foreach(var e in d.Docket)if(e.Result==ServiceResult.Pending)return e.Property;return -1;}
  // Position along the road: main road from the depot, then County Route 9 past the barricade.
  float Along(Vector3 at,out Vector3 tangent){
   var tg=Vector3.forward;float best=float.MaxValue,run=0,arc=0;var q=at;q.y=0;
   void Walk(Transform[] pts,float offset){if(pts==null)return;float r=0;for(int i=1;i<pts.Length;i++){if(!pts[i-1]||!pts[i])continue;var a=pts[i-1].position;var b=pts[i].position;a.y=b.y=0;var ab=b-a;float len=ab.magnitude;if(len<1e-4f)continue;float t=Mathf.Clamp01(Vector3.Dot(q-a,ab)/(len*len));float dd=(q-(a+ab*t)).sqrMagnitude;if(dd<best){best=dd;arc=offset+r+t*len;tg=ab/len;}r+=len;}run=offset+r;}
   Walk(d.Scene.Route,0);if(d.NightIndex>=2&&d.Scene.LateRoute!=null&&d.Scene.LateRoute.Length>1)Walk(d.Scene.LateRoute,run);
   tangent=tg;return arc;}
  void Update(){
   if(!d||d.Phase!=ServicePhase.Playing||Time.time<nextCheck)return;nextCheck=Time.time+.25f;
   Target=NextStop();if(Target!=lastTarget){lastTarget=Target;saidApproach=saidMissed=-1;}
   var car=d.Scene.Car;bool inCar=d.Player.InCar;var me=inCar?car.position:d.Scene.Walker.transform.position;
   if(Target<0){Objective="RETURN TO THE DEPOT";Direction=d.CanFinish?"":!inCar?"BACK TO THE CAR":Heading(me,car.forward,0,0,false);return;}
   var p=d.Property(Target);Objective="NEXT  "+Address[Target]+(Who[Target].Length>0?"  ·  "+Who[Target]:"");
   if(!p.Mailbox){Direction="";return;}
   // On foot at the property, the drive and the porch light do the work.
   if(!inCar&&Vector3.Distance(me,p.Door.position)<p.ApproachRouteLength()+12){Direction="UP THE DRIVE TO THE HOUSE";return;}
   // on foot anywhere else, the way on is the car
   if(!inCar){Direction="BACK TO THE CAR";return;}
   Direction=Heading(me,inCar?car.forward:d.Scene.View.transform.forward,p.RoadDistance,p.RoadSide,true);
   if(!inCar||d.Busy||d.Horror.Active||(d.Dialogue&&d.Dialogue.Active)||!string.IsNullOrEmpty(d.Notice))return;
   float sCar=Along(me,out var tan);float ahead=(p.RoadDistance-sCar)*Mathf.Sign(Vector3.Dot(car.forward,tan));
   if(saidApproach!=Target&&ahead>4&&ahead<38){saidApproach=Target;d.Say(Approach(Target));}
   else if(saidMissed!=Target&&saidApproach==Target&&ahead<-22&&d.Player.Speed>2){saidMissed=Target;d.Say("Went past it. "+Address[Target].Split(' ')[0]+" was back there.");}
  }
  string Heading(Vector3 me,Vector3 forward,float targetS,int side,bool house){
   float sMe=Along(me,out var tan);float dir=Mathf.Sign(Vector3.Dot(forward,tan));if(Mathf.Abs(Vector3.Dot(forward,tan))<.3f)dir=1;
   float ahead=(targetS-sMe)*dir;
   if(!house)return ahead>=0?"AHEAD":"BEHIND YOU · TURN AROUND";
   string lr=side*dir>0?"RIGHT":"LEFT";
   if(Mathf.Abs(ahead)<12)return "THIS DRIVE · ON YOUR "+lr;
   if(ahead>0)return ahead<60?"COMING UP ON THE "+lr:"FURTHER ALONG · ON THE "+lr;
   return "BEHIND YOU · TURN AROUND";
  }
  static string Approach(int i){switch(i){case 0:return "That's 214. Correll's. Dog's going to start.";case 3:return "236. That's Harrow Lodge.";case 1:return "77 on the box. Vale House.";case 4:return "91. Bell's drive, the long one.";case 5:return "108. Morrow's. Last one before the barricade.";default:return "There's a box. 1. Same as Correll's.";}}
 }
 public static class ServicePropertyGuideExtensions {
  public static float ApproachRouteLength(this ServiceProperty p){if(p.ApproachRoute==null||p.ApproachRoute.Length<2)return 20;float l=0;for(int i=1;i<p.ApproachRoute.Length;i++)l+=Vector3.Distance(p.ApproachRoute[i-1],p.ApproachRoute[i]);return l;}
 }
}
