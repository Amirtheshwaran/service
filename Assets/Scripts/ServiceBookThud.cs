using UnityEngine;
namespace ServiceGameV2 {
 // V26: the stair book's thuds at Bell's - one for each stair it strikes on the way down (the Nox Sound wood landing the
 // shelf books use), louder the harder it lands.
 public sealed class ServiceBookThud:MonoBehaviour {
  ServiceBooks books;ServiceDirector d;float next;
  public void Init(ServiceBooks owner,ServiceDirector director){books=owner;d=director;}
  void OnCollisionEnter(Collision c){if(!d||!books)return;float v=c.relativeVelocity.magnitude;if(v<.7f||Time.time<next)return;next=Time.time+.08f;
   books.Thud();d.Audio.HorrorAt("bookfall",transform.position,Mathf.Clamp(v*.17f,.16f,.7f));}
 }
}
