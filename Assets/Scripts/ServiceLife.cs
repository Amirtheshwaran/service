using System.Collections;
using System.Linq;
using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceLife:MonoBehaviour {
  ServiceDirector d;Transform dog,model;Animator dogAnim;Vector3 home;float nextIdle,barkUntil,turnSpeed;bool playing;bool[] seen=new bool[6];Quaternion[] rest=new Quaternion[6];float[] porch=new float[6];Coroutine[] doors=new Coroutine[6];
  public bool DogPresent=>dog&&model;public float DogTravel {get;private set;}
  // V22: the charge (see Sources/V22/patch_dog.py)
  public enum DogMode{Home,Charge,Escort,Porch,Hushed,Return}
  public DogMode Mode {get;private set;}=DogMode.Home;
  public bool DogBusy=>Mode!=DogMode.Home;
  public int Barks {get;private set;}
  public float ClosestApproach {get;private set;}=99;
  public Vector3 DogPosition=>dog?dog.position:Vector3.zero;
  CapsuleCollider dogBody;bool ghost;float pressedFor;public bool BodyYielded=>ghost;public int Yields {get;private set;}
  bool charged;Vector3 escortGoal;float nextBark,nextPath,modeSince,porchSince,weaveFlip;int weaveSide=1;Vector3 porchSpot,goalCache;bool porchPicked;string curState;
  readonly UnityEngine.AI.NavMeshPath path=new UnityEngine.AI.NavMeshPath();Vector3[] corners=new Vector3[0];int corner;
  const float RunRef=4f; // ground speed the run loop shows at speed 1: Audit/dog22.txt measures the paw's reach (0.53 m per 0.4 s cycle); a gallop covers about three times that
  public Vector3 DogHome=>home;
  // V20: friendly residents swing the door well open and stand clear of it (ServiceResidents).
  public const float FriendlyOpen=85;
  // V22: when each door's current swing finishes (a door is not offered to the player while it is still moving)
  readonly float[] settleAt=new float[6];
  public bool DoorMoving(int i)=>i>=0&&i<6&&Time.time<settleAt[i];
  public void Initialize(ServiceDirector director){d=director;
   for(int i=0;i<6;i++){var p=d.Property(i);rest[i]=p.DoorPanel?p.DoorPanel.localRotation:Quaternion.identity;porch[i]=p.PorchLight?p.PorchLight.intensity:0;p.MeasureDoorway();}
   dog=transform.Find("Correll yard dog");
   if(dog){
    // The old static seated model (V16) duplicated the animated V17 shepherd on the same spot: retire it.
    foreach(Transform c in dog)if(c.name.Contains("seated"))c.gameObject.SetActive(false);
    dogAnim=dog.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.runtimeAnimatorController);model=dogAnim?dogAnim.transform:(dog.childCount>0?dog.GetChild(0):null);
    home=YardSpot(d.Property(0));dog.position=home;FaceWalk(true);
    // V23: a body you cannot walk through (you used to shove him along by walking into him)
    var body=new GameObject("V23 dog body");body.transform.SetParent(dog,false);body.transform.localPosition=new Vector3(0,.42f,0);
    var cap=body.AddComponent<CapsuleCollider>();cap.direction=2;cap.radius=.24f;cap.height=1.05f;var rb=body.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;dogBody=cap;
   }
  }
  // Off to the side of Correll's porch, on the grass, away from the walk from the drive to the door.
  Vector3 YardSpot(ServiceProperty p){
   var c=p.OpeningCentre;var outw=-p.Inward;outw.y=0;outw.Normalize();var side=Vector3.Cross(Vector3.up,outw);
   Vector3 best=dog.position;float score=-1;
   foreach(float s in new[]{1f,-1f})foreach(float o in new[]{3.4f,4.4f}){
    var at=c+outw*o+side*s*3.3f;float clear=9;
    if(p.ApproachRoute!=null)foreach(var q in p.ApproachRoute){var a=q;a.y=at.y;clear=Mathf.Min(clear,Vector3.Distance(a,at));}
    if(Physics.CheckSphere(at+Vector3.up*.5f,.45f,~0,QueryTriggerInteraction.Ignore))clear-=4;
    if(clear>score){score=clear;best=at;}}
   if(Physics.Raycast(best+Vector3.up*3,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore))best.y=hit.point.y;
   else{var t=Terrain.activeTerrain;if(t)best.y=t.SampleHeight(best)+t.transform.position.y;}
   return best;
  }
  void FaceWalk(bool snap){var p=d.Property(0);var target=p.ApproachRoute!=null&&p.ApproachRoute.Length>0?p.ApproachRoute[p.ApproachRoute.Length/2]:p.Door.position;var f=target-dog.position;f.y=0;if(f.sqrMagnitude>.01f&&snap)dog.rotation=Quaternion.LookRotation(f);}
  public void ResetForShift(){StopAllCoroutines();Mode=DogMode.Home;charged=false;Barks=0;ClosestApproach=99;porchPicked=false;curState=null;if(dogAnim)dogAnim.speed=1;System.Array.Clear(settleAt,0,6);System.Array.Clear(seen,0,seen.Length);if(dog){dog.position=home;FaceWalk(true);}for(int i=0;i<6;i++){if(d.Property(i).DoorPanel)d.Property(i).DoorPanel.localRotation=rest[i];if(d.Property(i).PorchLight)d.Property(i).PorchLight.intensity=porch[i];}}
  void State(string name){if(name==curState)return;if(dogAnim&&dogAnim.isActiveAndEnabled&&dogAnim.HasState(0,Animator.StringToHash(name))){curState=name;dogAnim.CrossFadeInFixedTime(name,name=="Run"?.15f:.35f);}}
  public void Bark(){if(!dog||DogBusy)return;d.Audio.DogAt(dog.position+Vector3.up*.4f,.32f);barkUntil=Time.time+2.2f;if(!playing){playing=true;State("Playing");}}
  void Update(){if(!d||d.Phase!=ServicePhase.Playing||d.PaperOpen)return;
   // V25 the Correll branch: from night two Rex is simply gone
   bool gone=d.CorrellBranch&&d.NightIndex>=1;if(dog&&dog.gameObject.activeSelf==gone)dog.gameObject.SetActive(!gone);
   bool rex=!gone&&dog&&model&&(petting||Rex()); // V23: a pet owns him while it lasts
   YieldBody();
   if(dog&&model&&!rex&&!gone){
    // The dog watches whoever is walking up: it turns its body toward the player in short, unhurried turns and
    // shifts between its breathing and playful idles, rather than sitting in the path like a statue.
    var to=d.Scene.View.transform.position-dog.position;to.y=0;float distance=to.magnitude;
    if(distance<34&&to.sqrMagnitude>.01f){float off=Vector3.SignedAngle(dog.forward,to,Vector3.up);
     if(Mathf.Abs(off)>28)turnSpeed=Mathf.MoveTowards(turnSpeed,85,Time.deltaTime*160);else if(Mathf.Abs(off)<6)turnSpeed=Mathf.MoveTowards(turnSpeed,0,Time.deltaTime*200);
     if(turnSpeed>0){var before=dog.rotation;dog.rotation=Quaternion.RotateTowards(dog.rotation,Quaternion.LookRotation(to),turnSpeed*Time.deltaTime);DogTravel+=Quaternion.Angle(before,dog.rotation)*.01f;}}
    if(Time.time>nextIdle&&Time.time>barkUntil){playing=!playing&&distance<20&&Random.value<.6f;State(playing?"Playing":"Breathing");nextIdle=Time.time+(playing?3.2f:Random.Range(6f,11f));}
   }
   if(d.Player.InCar)return;
   foreach(var p in d.Scene.Properties){if(d.IsFriendly(p.Index)||seen[p.Index]||Vector3.Distance(d.Scene.Walker.transform.position,p.Door.position)>14)continue;seen[p.Index]=true;StartCoroutine(Flicker(p));}
  }
  // V23: he is solid, but never a wall: pressed against him while he cannot get out of the way (steps, a porch rail),
  // his body lets you by until you are clear of him
  void YieldBody(){if(!dogBody||!d.Scene.Walker)return;float gap=Flat(dog.position-d.Scene.Walker.transform.position).magnitude;
   bool pressing=!d.Player.InCar&&gap<dogBody.radius+d.Scene.Walker.radius+.12f&&d.Player.HorizontalSpeed<.35f;
   pressedFor=pressing?pressedFor+Time.deltaTime:0;
   if(!ghost&&pressedFor>.35f){ghost=true;Yields++;Physics.IgnoreCollision(d.Scene.Walker,dogBody,true);}
   else if(ghost&&gap>1.1f){ghost=false;Physics.IgnoreCollision(d.Scene.Walker,dogBody,false);}}
  // V22: true while the charge owns the dog this frame.
  bool Rex(){
   var p=d.Property(0);var w=d.Scene.Walker.transform.position;var door=p.Door.position;var dw=Flat(w-door).magnitude;
   bool pending=d.Docket.Exists(e=>e.Property==0&&e.Result==ServiceResult.Pending);
   bool calm=!d.Player.InCar&&!d.Horror.Active&&!d.Horror.Caught&&!(d.Dialogue&&d.Dialogue.Active)&&d.NoteOpen<0;
   if(Mode==DogMode.Home){
    if(charged||d.NightIndex>1||!pending||!calm)return false;
    if((dw<32&&dw>5)||Flat(w-dog.position).magnitude<14){charged=true;Go(DogMode.Charge);Bark(true);d.SayDogLine();}else return false;}
   // walking or calling it off
   if(d.Horror.Active||d.Horror.Caught)Go(DogMode.Return);
   if(d.Dialogue&&d.Dialogue.Active&&d.Dialogue.Speaker.StartsWith("Walter"))Hush();
   if((Mode==DogMode.Charge||Mode==DogMode.Escort||Mode==DogMode.Porch)&&(d.Player.InCar||dw>40))Go(DogMode.Return);
   Vector3 goal=dog.position;float speed=0;var toW=Flat(w-dog.position);float dist=toW.magnitude;
   switch(Mode){
    case DogMode.Charge:goal=w+Flat3(dog.position-w).normalized*2.6f;speed=6.5f;if(dist<3.4f)Go(DogMode.Escort);break;
    case DogMode.Escort:{bool moving=d.Player.HorizontalSpeed>.4f;if(moving&&Time.time>weaveFlip){weaveSide=-weaveSide;weaveFlip=Time.time+Random.Range(2.6f,4f);}
     // V23: ahead of you on the way to the door - by where you are headed, not where you look (by your facing he ran in
     // circles whenever you looked round); a new spot only once the old one has drifted, and none while you stand still
     var way=Flat3(door-w);if(way.sqrMagnitude<.01f)way=Flat3(d.Scene.Walker.transform.forward);way.Normalize();
     var want=w+Quaternion.AngleAxis(weaveSide*22,Vector3.up)*way*4f;
     if(escortGoal==Vector3.zero||(moving&&Flat(want-escortGoal).magnitude>1f)||Flat(escortGoal-w).magnitude<2.2f)escortGoal=want;goal=escortGoal;
     // your pace plus the gap (by the gap alone he trailed his spot by 1.5 m at a walk, under the bottom of the view)
     speed=Mathf.Clamp(d.Player.HorizontalSpeed+Flat(goal-dog.position).magnitude*2f,0,6.5f);if(dw<4.5f)Go(DogMode.Porch);break;}
    case DogMode.Porch:{if(!porchPicked){porchPicked=true;porchSpot=PorchSpot(p,w);porchSince=Time.time;}goal=porchSpot;speed=3f;
     if(dw>8)Go(DogMode.Escort);break;}
    case DogMode.Hushed:goal=dog.position;speed=0;
     if(!pending&&(d.Player.InCar||dw>20)&&!AnyVisible()){dog.position=home;FaceWalk(true);Go(DogMode.Home);return false;}break;
    case DogMode.Return:goal=home;speed=4.5f;if(Flat(home-dog.position).magnitude<.4f){dog.position=home;FaceWalk(true);Go(DogMode.Home);return false;}break;
   }
   // move along the navmesh toward the goal, on the ground, never onto the porch or into the house, never into you
   float moved=0;
   if(speed>.05f){
    // repath on the navmesh four times a second, and at once when the goal has moved (the weave, a turn of the head)
    if(Time.time>nextPath||Vector3.Distance(goalCache,goal)>1.5f){nextPath=Time.time+.25f;goalCache=goal;
     if(UnityEngine.AI.NavMesh.SamplePosition(dog.position,out var s0,.6f,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.SamplePosition(goal,out var s1,1.5f,UnityEngine.AI.NavMesh.AllAreas)&&UnityEngine.AI.NavMesh.CalculatePath(s0.position,s1.position,UnityEngine.AI.NavMesh.AllAreas,path)&&path.corners.Length>1){corners=path.corners;corner=1;}
     else if(corners.Length<2){corners=new[]{dog.position,dog.position};corner=1;}} // no path: keep the last one, or stand
    var target=corners.Length>corner?corners[corner]:dog.position;if(Flat(target-dog.position).magnitude<.35f&&corner<corners.Length-1){corner++;target=corners[corner];}
    var step=Vector3.ClampMagnitude(Flat3(target-dog.position),speed*Time.deltaTime);var next=dog.position+step;
    bool g=Ground(ref next),ins=g&&(Indoors(p,next)||OnBuilt),hi=g&&next.y>=door.y-.4f,jump=g&&Mathf.Abs(next.y-dog.position.y)>=.35f,edge=false; // the step runs between navmesh path corners, so it is on the mesh already; a NavMesh.Raycast from a corner
    // (corners sit on the mesh edge) grazed the boundary and refused every step - Rex froze mid-charge (V22 tour)
    // never a step of his own to inside the 1.5 m he keeps from you (a 6.5 m/s charge on a slow frame ended at 1.1)
    bool onYou=Flat(next-w).magnitude<1.5f&&Flat(next-w).magnitude<Flat(dog.position-w).magnitude;
    if(g&&!ins&&!hi&&!jump&&!edge&&!onYou){moved=step.magnitude;dog.position=next;}else BlockedBy[!g?0:ins?1:hi?2:jump?3:4]++;
   }
   // V23: no shove (walking into him used to slide him along): he steps aside on his own when you come close
   var away=Flat3(dog.position-w);
   // V25: 1.5 m from someone standing (petting reach is 1.75), more from someone running at him - a sprint closed faster than his step
   float closing=away.sqrMagnitude>.01f?Mathf.Max(0,Vector3.Dot(Flat3(d.Scene.Walker.velocity),away.normalized)):0;float keep=1.5f+Mathf.Min(.35f,closing*.07f);
   if(away.magnitude<keep&&!petting){var a=away.sqrMagnitude>.01f?away.normalized:-Flat3(d.Scene.Walker.transform.forward).normalized;
    // straight away first; where that is the steps or the porch (the foot of the steps), sideways off the line you walk, or back past you
    var head=Flat3(d.Scene.Walker.velocity);if(head.sqrMagnitude<.04f)head=Flat3(door-w);if(head.sqrMagnitude<.01f)head=a;head.Normalize();var perp=Vector3.Cross(Vector3.up,head);if(Vector3.Dot(perp,a)<0)perp=-perp;
    float len=Mathf.Min(6f*Time.deltaTime,keep+.1f-away.magnitude);
    foreach(var dir in new[]{a,perp,(perp-head*.6f).normalized,-perp,(-perp-head*.6f).normalized}){var to=dog.position+dir*len;
     if(Ground(ref to)&&!OnBuilt&&!Indoors(p,to)&&to.y<door.y-.4f&&Mathf.Abs(to.y-dog.position.y)<.35f){moved+=Flat(to-dog.position).magnitude;dog.position=to;break;}}}
   ClosestApproach=Mathf.Min(ClosestApproach,Flat(dog.position-w).magnitude);
   float v=moved/Mathf.Max(Time.deltaTime,1e-4f);DogTravel+=moved;
   // face where he runs, or you when he stands
   var faceDir=v>.4f&&moved>0?Flat3(goal-dog.position):Flat3(w-dog.position);if(faceDir.sqrMagnitude>.01f)dog.rotation=Quaternion.RotateTowards(dog.rotation,Quaternion.LookRotation(faceDir),(v>.4f?540:220)*Time.deltaTime);
   if(v>.6f){State("Run");if(dogAnim)dogAnim.speed=Mathf.Clamp(v/RunRef,.6f,1.5f);}
   else{if(dogAnim)dogAnim.speed=1;State(Mode==DogMode.Hushed||Mode==DogMode.Return?"Breathing":"Playing");}
   // barking: a volley on night one until Walter answers (or a while at the steps); twice on night two
   bool barking=Mode==DogMode.Charge||Mode==DogMode.Escort||Mode==DogMode.Porch&&Time.time-porchSince<30;
   if(barking&&Time.time>nextBark&&(d.NightIndex==0||Barks<2))Bark(true);
   d.Audio.DogFollow(dog.position+Vector3.up*.45f);
   return true;
  }
  void Go(DogMode m){if(Mode==m)return;Mode=m;modeSince=Time.time;nextPath=0;escortGoal=Vector3.zero;if(m!=DogMode.Porch)porchPicked=false;if(m==DogMode.Escort)weaveFlip=Time.time+Random.Range(1.2f,2.2f);if(m==DogMode.Home){if(dogAnim)dogAnim.speed=1;State("Breathing");nextIdle=Time.time+Random.Range(3f,6f);}}
  // V23: pet Rex - only when he is calm (at home, or hushed, or at the steps once Walter has him) and you are right by him
  bool petting;public bool Petting=>petting;public int Pets {get;private set;}
  public bool CanPet(Vector3 w){if(!dog||!model||!dog.gameObject.activeInHierarchy||petting||d.Player.InCar||d.Busy||d.Horror.Active||d.Horror.Caught)return false;if(Time.time<barkUntil+.4f)return false;
   bool calm=Mode==DogMode.Home||Mode==DogMode.Hushed||(Mode==DogMode.Porch&&Time.time-porchSince>30f);if(!calm)return false;
   // on his level: from the porch or the steps he cannot come up to your hand (he never climbs them), so step down to him
   var cc=d.Scene.Walker;if(cc&&Mathf.Abs(cc.bounds.min.y-dog.position.y)>.3f)return false;
   var to=Flat3(dog.position-w);return to.magnitude<1.75f&&to.magnitude>.4f;}
  public Transform DogTransform=>dog;
  public void SmokePlaceDog(Vector3 at){if(!dog)return;var q=at;if(Ground(ref q))dog.position=q;}
  public Vector3 PetPoint=>dog?dog.position+Vector3.up*.55f:Vector3.zero;
  public string PetStop="";string lastTop="";
  public System.Collections.IEnumerator Pet(){PetStop="reached";petting=true;Pets++;if(dogAnim)dogAnim.speed=1;float t=0;
   // he comes in under your hand first: up to your feet over open ground (he keeps a step off you otherwise)
   var home=d.Property(0);float tc=0;
   while(tc<1.4f){tc+=Time.deltaTime;var to=Flat3(d.Scene.Walker.transform.position-dog.position);if(to.magnitude<=.75f)break;
    if(to.sqrMagnitude>.01f)dog.rotation=Quaternion.RotateTowards(dog.rotation,Quaternion.LookRotation(to),420*Time.deltaTime);
    var next=dog.position+to.normalized*Mathf.Min(1.5f*Time.deltaTime,to.magnitude-.75f);
    bool gok=Ground(ref next);if(!gok||OnBuilt||Indoors(home,next)||Mathf.Abs(next.y-dog.position.y)>=.35f){PetStop=!gok?"no ground":OnBuilt?"built "+lastTop:Indoors(home,next)?"indoors":$"height {next.y-dog.position.y:F2}";break;}
    dog.position=next;State("Playing");yield return null;}
   State("Breathing");
   while(t<2.2f){t+=Time.deltaTime;var to=Flat3(d.Scene.Walker.transform.position-dog.position);if(to.sqrMagnitude>.01f)dog.rotation=Quaternion.RotateTowards(dog.rotation,Quaternion.LookRotation(to),200*Time.deltaTime);yield return null;}
   State("Playing");nextIdle=Time.time+3.2f;petting=false;}
  public void Hush(){if(Mode==DogMode.Home||Mode==DogMode.Hushed)return;Go(DogMode.Hushed);d.Audio.StopDog();}
  public bool DogSeen=>AnyVisible();
  public string PathInfo=>$"path {path.status} {corners.Length} corners, at {corner}, end {(corners.Length>0?corners[corners.Length-1]:Vector3.zero)}, goal {goalCache}, refused {string.Join("/",BlockedBy)}";
  public readonly int[] BlockedBy=new int[5]; // steps refused: no ground, indoors or on a porch/steps, too high, a jump, off the navmesh
  bool AnyVisible(){if(!model)return false;foreach(var r in model.GetComponentsInChildren<Renderer>())if(r.isVisible)return true;return false;}
  void Bark(bool loud){d.Audio.DogAt(dog.position+Vector3.up*.45f,loud?.45f:.32f,Random.Range(.93f,1.07f));Barks++;nextBark=Time.time+(d.NightIndex==0?Random.Range(1.0f,1.9f):Random.Range(2.5f,4f));barkUntil=Time.time+1.2f;}
  // by the bottom of the steps, off to the side of the walk to the door, facing you
  Vector3 PorchSpot(ServiceProperty p,Vector3 w){var outw=-p.Inward;outw.y=0;outw.Normalize();var side=Vector3.Cross(Vector3.up,outw);var c=p.OpeningCentre;Vector3 best=c+outw*3.4f;float score=-1;
   foreach(float s in new[]{1f,-1f})foreach(float o in new[]{3.2f,4.2f}){var at=c+outw*o+side*s*2.4f;var test=at;if(!Ground(ref test)||OnBuilt||test.y>p.Door.position.y-.4f||Indoors(p,test))continue;float sc=Flat(test-w).magnitude;if(sc>score){score=sc;best=test;}}
   return best;}
  bool Ground(ref Vector3 at){var hits=Physics.RaycastAll(at+Vector3.up*1.5f,Vector3.down,4f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore);float best=float.NegativeInfinity;
   Collider top=null;foreach(var h in hits){if(h.collider is CharacterController||h.collider.transform.IsChildOf(d.Scene.Walker.transform)||dog&&h.collider.transform.IsChildOf(dog))continue;if(h.point.y>best){best=h.point.y;top=h.collider;}}
   if(float.IsNegativeInfinity(best)){var t=Terrain.activeTerrain;if(!t)return false;best=t.SampleHeight(at)+t.transform.position.y;}
   // a porch, its steps or a house floor: wood (or unmarked) built ground, not the yard, the drive or the terrain
   OnBuilt=top!=null&&!(top is TerrainCollider)&&(top.GetComponentInParent<ServiceSurface>() is var sf&&(sf==null||sf.Kind=="wood"));lastTop=top?top.name:"terrain";
   at.y=best;return true;}
  bool OnBuilt;
  // inside the house = within its bounds and not out in front of the door's wall (the bounds take in the porch and yard)
  public static bool Indoors(ServiceProperty p,Vector3 at)=>p.InteriorBounds.Contains(at+Vector3.up*.3f)&&Vector3.Dot(at-p.OpeningCentre,-p.Inward)<.8f;
  static Vector2 Flat(Vector3 v)=>new Vector2(v.x,v.z);
  static Vector3 Flat3(Vector3 v)=>new Vector3(v.x,0,v.z);
  IEnumerator Flicker(ServiceProperty p){if(!p.PorchLight)yield break;var light=p.PorchLight;float baseline=porch[p.Index];foreach(float value in new[]{.15f,1f,.1f,.2f,1f}){light.intensity=baseline*value;yield return new WaitForSeconds(.12f);}light.intensity=baseline;}
  public float DoorOpenDegrees(int index){var p=d?d.Property(index):null;return p&&p.DoorPanel?Quaternion.Angle(rest[index],p.DoorPanel.localRotation):0;}
  public void OpenDoor(ServiceProperty p,bool open,bool wide=false,float seconds=.65f){if(p.DoorPanel){settleAt[p.Index]=Time.time+Mathf.Max(.05f,seconds);if(doors[p.Index]!=null)StopCoroutine(doors[p.Index]);doors[p.Index]=StartCoroutine(Door(p,open,wide,seconds));}}
  // A bang (short seconds) is thrown open and decelerates; an ordinary swing eases in and out.
  IEnumerator Door(ServiceProperty p,bool open,bool wide,float seconds){var start=p.DoorPanel.localRotation;var goal=rest[p.Index]*Quaternion.Euler(0,open?(d.IsFriendly(p.Index)&&!wide?Mathf.Sign(p.DoorSwing)*FriendlyOpen:p.DoorSwing):0,0);seconds=Mathf.Max(.05f,seconds);float time=0;while(time<seconds){time+=Time.deltaTime;float t=Mathf.Clamp01(time/seconds);t=seconds<.3f?1-(1-t)*(1-t):Mathf.SmoothStep(0,1,t);p.DoorPanel.localRotation=Quaternion.Slerp(start,goal,t);yield return null;}p.DoorPanel.localRotation=goal;}
 }
}
