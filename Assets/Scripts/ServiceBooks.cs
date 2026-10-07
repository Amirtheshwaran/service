using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ServiceGameV2 {
 // V23: "add random books falling from the shelf after serving". When you leave the papers in a house, a few seconds
 // later a volume (sometimes two) slides off the end of a shelf row you can see or hear - real falling bodies onto the
 // floor, a thud (Nox Sound wood landing, CC0). Not every time: the house picks. Rows are "V23 shelf books" placed by the
 // editor stage World23; everything is put back for the next shift.
 public sealed class ServiceBooks:MonoBehaviour {
  ServiceDirector d;readonly bool[] done=new bool[6];readonly ServiceResult[] last=new ServiceResult[6];int pending=-1;float at=-1;bool saidTonight;
  sealed class Moved{public Transform book,parent;public Vector3 pos;public Quaternion rot;}
  readonly List<Moved> moved=new List<Moved>();
  public static bool Force; // tests: always drop
  public int Falls {get;private set;}public int LastHouse {get;private set;}=-1;public Vector3 LastLanding {get;private set;}public Transform LastBook {get;private set;}public float LastOut {get;private set;}Vector3 lastRowAt,lastFw;
  public void Initialize(ServiceDirector director){d=director;}
  // V26 playtest: "91 Latigo - a book should fall when the player goes near the stairs". Night two at Bell's (his door is
  // open, the house empty): the first time you come to the foot of his stairs, a board creaks upstairs and one of his
  // encyclopedia volumes comes off the landing and tumbles down the flight to you - a real falling body, a thud on the
  // stairs it strikes (ServiceBookThud).
  public const float StairNear=2.6f;
  public int StairFalls {get;private set;} public int StairThuds {get;private set;} public Transform StairBook {get;private set;}
  public Vector3 StairTop {get;private set;} public Vector3 StairBottom {get;private set;} public bool StairKnown {get;private set;}
  bool stairDone,stairTried;float stairNearFor;GameObject stairClone;
  public void Thud(){StairThuds++;}
  // the flight: from its lowest point up the way it rises to where it levels off (Bell's is a U - the lower flight to
  // a half-landing, the upper one back over it). The stair's collider is a smooth ramp for walking, so it is sampled on
  // a grid from above; the book is let go from the edge of that landing.
  public string FlightInfo {get;private set;}="";
  public bool Flight(ServiceProperty p){if(StairKnown||stairTried)return StairKnown;stairTried=true;
   var mr=p?p.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name.Contains("IntStairs")):null;var col=mr?mr.GetComponent<Collider>():null;if(!col){FlightInfo="no stair collider";return false;}
   stairRenderer=mr;stairRamp=col;
   var b=col.bounds;var f=mr.transform.forward;f.y=0;f.Normalize();var side=Vector3.Cross(Vector3.up,f);
   float ext=Mathf.Abs(b.extents.x*f.x)+Mathf.Abs(b.extents.z*f.z),wid=Mathf.Abs(b.extents.x*side.x)+Mathf.Abs(b.extents.z*side.z);const float st=.1f;
   int nk=Mathf.CeilToInt(2*ext/st)+1,ns=Mathf.CeilToInt(2*wid/st)+1;var hy=new float[ns,nk];int bi=-1,bj=-1;float low=float.MaxValue;
   for(int i=0;i<ns;i++)for(int j=0;j<nk;j++){var o=b.center+side*(-wid+i*st)+f*(-ext+j*st);o.y=b.max.y+.5f;
    hy[i,j]=col.Raycast(new Ray(o,Vector3.down),out var h,b.size.y+1f)?h.point.y:float.NaN;if(!float.IsNaN(hy[i,j])&&hy[i,j]<low){low=hy[i,j];bi=i;bj=j;}}
   if(bi<0){FlightInfo="no hits";return false;}
   bool Up(int i,int j,int dj)=>j+dj>=0&&j+dj<nk&&!float.IsNaN(hy[i,j+dj])&&hy[i,j+dj]>hy[i,j]+.02f&&hy[i,j+dj]<hy[i,j]+.16f; // a stair's rise, not a jump to a beam overhead
   int dir=Up(bi,bj,1)?1:-1;int tj=bj;while(Up(bi,tj,dir))tj+=dir;
   int jm=(bj+tj)/2;float ym=hy[bi,jm];int i0=bi,i1=bi;
   while(i0-1>=0&&!float.IsNaN(hy[i0-1,jm])&&Mathf.Abs(hy[i0-1,jm]-ym)<.08f)i0--;while(i1+1<ns&&!float.IsNaN(hy[i1+1,jm])&&Mathf.Abs(hy[i1+1,jm]-ym)<.08f)i1++;
   float sc=-wid+(i0+i1)*.5f*st;
   Vector3 P(int j,float y){var q=b.center+side*sc+f*(-ext+j*st);q.y=y;return q;}
   StairBottom=P(bj,hy[bi,bj]);StairTop=P(tj,hy[bi,tj]);StairKnown=StairTop.y-StairBottom.y>1.2f&&Mathf.Abs(tj-bj)>=8;
   FlightInfo=$"grid {ns}x{nk}, lowest {low:F2} at ({bi},{bj}), rises {dir} to ({bi},{tj}) {hy[bi,tj]:F2}, width {(i1-i0+1)*st:F1} m";return StairKnown;}
  MeshRenderer stairRenderer;Collider stairRamp;MeshCollider treads;public bool TreadsUsed {get;private set;}
  void StairCheck(){if(stairDone||d.NightIndex!=1||d.Player.InCar||d.Horror.Active||d.Horror.Caught||d.Busy)return;var p=d.Property(4);if(!p)return;
   var w=d.Scene.Walker.transform.position;if(!ServiceLife.Indoors(p,w))return;if(!Flight(p)){stairDone=true;return;}
   var down=StairBottom-StairTop;down.y=0;var foot=StairBottom+down.normalized*.5f;var off=w-foot;off.y=0;float feet=d.Scene.Walker.bounds.min.y;
   if(off.magnitude>StairNear||Mathf.Abs(feet-StairBottom.y)>1.2f){stairNearFor=0;return;}stairNearFor+=Time.deltaTime;
   var vp=d.Scene.View.WorldToViewportPoint(StairTop+Vector3.up*.3f);bool inView=vp.z>0&&vp.x>.08f&&vp.x<.92f&&vp.y>.05f&&vp.y<.97f;
   if(inView||stairNearFor>1.5f){stairDone=true;StartCoroutine(StairFall(p));}}
  IEnumerator StairFall(ServiceProperty p){StairFalls++;
   d.Audio.HorrorAt("woodstress",StairTop+Vector3.up*.6f,.32f); // a board upstairs first
   yield return new WaitForSeconds(.55f);
   var src=Rows(p).SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).FirstOrDefault(r=>r.GetComponent<MeshFilter>()&&r.name.Contains("book"));
   if(!src)src=FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(r=>r.name.Contains("book_encyclopedia")&&r.GetComponent<MeshFilter>());
   if(!src)yield break;
   var down=StairBottom-StairTop;down.y=0;down.Normalize();
   var g=new GameObject("V26 stair book");g.transform.SetPositionAndRotation(StairTop-down*.12f+Vector3.up*.16f,Quaternion.LookRotation(Vector3.up,down));g.transform.localScale=src.transform.lossyScale;
   g.AddComponent<MeshFilter>().sharedMesh=src.GetComponent<MeshFilter>().sharedMesh;g.AddComponent<MeshRenderer>().sharedMaterials=src.sharedMaterials;
   g.transform.SetParent(p.transform,true);stairClone=g;StairBook=g.transform;
   var box=g.AddComponent<BoxCollider>();
   // the book lands on the treads themselves: the stair's drawn mesh (baked as "V26 stair treads" by the editor stage
   // StairTreads26, off until now) stands in for the smooth walking ramp, for the book alone
   var tt=stairRenderer?stairRenderer.transform.Find("V26 stair treads"):null;treads=tt?tt.GetComponent<MeshCollider>():null;TreadsUsed=treads&&treads.sharedMesh;
   if(TreadsUsed){treads.enabled=true;Physics.IgnoreCollision(d.Scene.Walker,treads,true);if(stairRamp)Physics.IgnoreCollision(box,stairRamp,true);}
   var rb=g.AddComponent<Rigidbody>();rb.mass=.9f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.interpolation=RigidbodyInterpolation.Interpolate;
   // off the edge of the landing, end over end down the flight
   rb.linearVelocity=down*1.7f+Vector3.up*.5f;rb.angularVelocity=Vector3.Cross(Vector3.up,down)*7f+Random.insideUnitSphere*1.2f;
   g.AddComponent<ServiceBookThud>().Init(this,d);
   float t=0,still=0;while(t<7f&&rb){t+=Time.deltaTime;still=rb.linearVelocity.sqrMagnitude<.01f&&rb.angularVelocity.sqrMagnitude<.05f?still+Time.deltaTime:0;if(still>.6f)break;yield return null;}
   if(rb)rb.isKinematic=true;if(treads)treads.enabled=false;
   yield return new WaitForSeconds(.3f);if(!d.Horror.Active&&!d.Horror.Caught&&!(d.Dialogue&&d.Dialogue.Active))d.Say(ServiceScript.StairBook);}
  public void ResetForShift(){StopAllCoroutines();
   foreach(var m in moved){if(!m.book)continue;var rb=m.book.GetComponent<Rigidbody>();if(rb)Destroy(rb);var bc=m.book.GetComponent<BoxCollider>();if(bc)Destroy(bc);m.book.SetParent(m.parent,false);m.book.localPosition=m.pos;m.book.localRotation=m.rot;}
   if(stairClone)Destroy(stairClone);stairClone=null;if(treads)treads.enabled=false;StairBook=null;stairDone=false;stairNearFor=0;
   moved.Clear();System.Array.Clear(done,0,6);for(int i=0;i<6;i++)last[i]=ServiceResult.Pending;pending=-1;at=-1;saidTonight=false;}
  void Update(){
   if(!d||d.Phase!=ServicePhase.Playing)return;
   StairCheck();
   for(int i=0;i<6;i++){var r=d.ResultAt(i);var p=d.Property(i);
    if(last[i]==ServiceResult.Pending&&r!=ServiceResult.Pending&&!done[i]&&p&&ServiceLife.Indoors(p,d.Scene.Walker.transform.position)){done[i]=true;if(Rows(p).Count>0&&!(i==4&&d.NightIndex==1)&&(Force||Random.value<.7f)){pending=i; /* V26: not at Bell's on night two - his stairs have the book */at=Time.time+Random.Range(2.2f,3.4f);}}
    last[i]=r;}
   if(pending>=0&&Time.time>at){var p=d.Property(pending);pending=-1;if(p&&!d.Horror.Active&&!d.Horror.Caught&&ServiceLife.Indoors(p,d.Scene.Walker.transform.position))Drop(p);}
  }
  static List<Transform> Rows(ServiceProperty p)=>p.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("V23 shelf books")&&t.gameObject.activeInHierarchy).ToList();
  void Drop(ServiceProperty p){
   var w=d.Scene.Walker.transform.position;var row=Rows(p).Where(r=>Mathf.Abs(r.position.y-w.y)<3.2f).OrderBy(r=>Vector3.Distance(r.position,w)).FirstOrDefault();if(!row||Vector3.Distance(row.position,w)>12f)return;
   // the end volumes of the row (rows run along their own right axis; the shelf front is their forward)
   var vols=row.GetComponentsInChildren<MeshRenderer>().Select(r=>r.transform).Where(t=>t!=row).Distinct().ToList();if(vols.Count==0)return;
   bool fromRight=Random.value<.5f;var ordered=vols.OrderBy(v=>Vector3.Dot(v.position-row.position,row.right)*(fromRight?-1:1)).ToList();
   int count=Random.value<.4f?2:1;
   for(int k=0;k<count&&k<ordered.Count;k++){var b=ordered[k];moved.Add(new Moved{book=b,parent=b.parent,pos=b.localPosition,rot=b.localRotation});
    b.SetParent(p.transform,true);if(k==0)LastBook=b;StartCoroutine(Slide(b,row,k));}
   Falls++;LastHouse=p.Index;
  }
  // out of the row by itself (nothing touches it), past the board's front edge, then it tips and goes
  IEnumerator Slide(Transform b,Transform row,int k){
   yield return new WaitForSeconds(k*.4f);if(!b)yield break;
   var fw=row.forward;fw.y=0;fw.Normalize();if(b==LastBook){lastRowAt=row.position;lastFw=fw;}var from=b.position+Vector3.up*.006f;var to=from+fw*(.34f+Random.Range(0f,.06f));float t=0,dur=Random.Range(.55f,.8f);
   while(t<dur&&b){t+=Time.deltaTime;float e=t/dur;e=e*e*(3-2*e);b.position=Vector3.Lerp(from,to,e);yield return null;}
   if(!b)yield break;b.gameObject.AddComponent<BoxCollider>();
   var rb=b.gameObject.AddComponent<Rigidbody>();rb.mass=.9f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.interpolation=RigidbodyInterpolation.Interpolate;
   // a nudge out and a tip forward (positive turn about the row's right axis brings the top of the spine forward)
   rb.linearVelocity=fw*.55f;rb.angularVelocity=Vector3.Cross(Vector3.up,fw)*3.5f+Random.insideUnitSphere*.8f;
   StartCoroutine(Land(b,rb,0));}
  IEnumerator Land(Transform b,Rigidbody rb,float delay){
   float t=0;float top=b.position.y;yield return new WaitForSeconds(.12f+delay);
   while(t<1.6f&&rb&&!(rb.linearVelocity.y>-.4f&&b.position.y<top-.3f)){t+=Time.deltaTime;yield return null;}
   if(b){d.Audio.HorrorAt("bookfall",b.position,.7f);LastLanding=b.position;if(b==LastBook){var o=b.position-lastRowAt;o.y=0;LastOut=Vector3.Dot(o,lastFw);}}
   if(!saidTonight){saidTonight=true;yield return new WaitForSeconds(.9f);if(!d.Horror.Active&&!(d.Dialogue&&d.Dialogue.Active))d.Say(ServiceScript.BookFell);}
   yield return new WaitForSeconds(2.5f);if(rb){rb.isKinematic=true;}
  }
 }
}
