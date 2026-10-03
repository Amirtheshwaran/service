using System.Linq;
using UnityEngine;
namespace ServiceGameV2 {
 // V20: the delivered papers lie on a clear patch of the table, never under a book or a lamp. The interior layouts put
 // desk clutter (books, lamps, frames) on the same tabletop as the delivery spot, so when the papers are put down they
 // slide to the nearest free patch of that tabletop, judged from the renderers standing on it.
 public static class ServicePaperPlacement {
  public static Vector3 LastShift {get;private set;}
  // V21: with a viewer, a patch the papers would lie hidden behind (a row of books between them and the player) is
  // passed over for one in sight whenever the tabletop has one.
  public static bool LastHidden {get;private set;}
  public static Bounds LastBlocker {get;private set;}
  public static bool LastTable {get;private set;}
  public static bool LastClear {get;private set;}
  public static void Settle(GameObject paper,Vector3? viewer=null){
   LastShift=Vector3.zero;LastHidden=false;LastTable=false;
   if(!paper)return;var own=paper.GetComponentsInChildren<Renderer>();if(own.Length==0)return;
   var pb=own[0].bounds;foreach(var r in own)pb.Encapsulate(r.bounds);
   float top=pb.min.y;var c=pb.center;Vector2 half=new Vector2(pb.extents.x+.025f,pb.extents.z+.025f);
   var near=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&!own.Contains(r)&&!(r is ParticleSystemRenderer)&&!(r is TrailRenderer)&&!(r is LineRenderer)).Where(r=>{var b=r.bounds;return b.max.x>c.x-1.4f&&b.min.x<c.x+1.4f&&b.max.z>c.z-1.4f&&b.min.z<c.z+1.4f&&b.max.y>top-.06f&&b.min.y<top+.6f;}).ToArray();
   // The tabletop: the smallest renderer under the papers whose top is at the papers' underside.
   Renderer table=null;float area=float.MaxValue;
   foreach(var r in near){var b=r.bounds;if(b.max.y<top-.04f||b.max.y>top+.025f||b.size.y<.15f)continue;if(c.x<b.min.x||c.x>b.max.x||c.z<b.min.z||c.z>b.max.z)continue;float a=b.size.x*b.size.z;if(a<area){area=a;table=r;}}
   if(!table)return;var tb=table.bounds;LastTable=true;
   // Things standing on that tabletop.
   var clutter=near.Where(r=>r!=table).Select(r=>r.bounds).Where(b=>b.min.y>top-.035f&&b.min.y<top+.25f&&b.max.y>top+.004f&&b.size.x<1.2f&&b.size.z<1.2f).ToArray();
   bool Clear(Vector3 at){foreach(var b in clutter)if(b.max.x>at.x-half.x&&b.min.x<at.x+half.x&&b.max.z>at.z-half.y&&b.min.z<at.z+half.y)return false;return true;}
   // hidden: something standing taller than the papers between this patch and the viewer's eye
   var tall=clutter.Where(b=>b.max.y>top+.06f).ToArray();
   bool Hidden(Vector3 at){if(!viewer.HasValue)return false;var v=viewer.Value;var dir=new Vector3(v.x-at.x,0,v.z-at.z);float len=dir.magnitude;if(len<.05f)return false;dir/=len;
    for(float s=0;s<Mathf.Min(len,1.6f);s+=.02f){var q=at+dir*s;foreach(var b in tall)if(q.x>b.min.x&&q.x<b.max.x&&q.z>b.min.z&&q.z<b.max.z){
     // the papers show over it if the eye looks down past its top
     float rise=(v.y-top)*s/len;if(b.max.y-top>rise){LastBlocker=b;return true;}}}return false;}
   if(Clear(c)&&!Hidden(c)){LastClear=true;return;}
   Vector3 best=c;float bestD=float.MaxValue;bool bestHidden=true;
   for(float x=tb.min.x+half.x+.02f;x<=tb.max.x-half.x-.02f;x+=.03f)for(float z=tb.min.z+half.y+.02f;z<=tb.max.z-half.y-.02f;z+=.03f){
    var at=new Vector3(x,c.y,z);if(!Clear(at))continue;bool h=Hidden(at);float dd=(at-c).sqrMagnitude;if((bestHidden&&!h)||(h==bestHidden&&dd<bestD)){bestD=dd;best=at;bestHidden=h;}}
   if(bestD==float.MaxValue){LastHidden=Hidden(c);LastClear=false;return;}
   LastHidden=bestHidden;LastClear=true;
   var shift=best-c;shift.y=0;paper.transform.position+=shift;LastShift=shift;
  }
 }
}
