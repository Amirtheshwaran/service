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
  public void ResetForShift(){StopAllCoroutines();
   foreach(var m in moved){if(!m.book)continue;var rb=m.book.GetComponent<Rigidbody>();if(rb)Destroy(rb);var bc=m.book.GetComponent<BoxCollider>();if(bc)Destroy(bc);m.book.SetParent(m.parent,false);m.book.localPosition=m.pos;m.book.localRotation=m.rot;}
   moved.Clear();System.Array.Clear(done,0,6);for(int i=0;i<6;i++)last[i]=ServiceResult.Pending;pending=-1;at=-1;saidTonight=false;}
  void Update(){
   if(!d||d.Phase!=ServicePhase.Playing)return;
   for(int i=0;i<6;i++){var r=d.ResultAt(i);var p=d.Property(i);
    if(last[i]==ServiceResult.Pending&&r!=ServiceResult.Pending&&!done[i]&&p&&ServiceLife.Indoors(p,d.Scene.Walker.transform.position)){done[i]=true;if(Rows(p).Count>0&&(Force||Random.value<.7f)){pending=i;at=Time.time+Random.Range(2.2f,3.4f);}}
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
