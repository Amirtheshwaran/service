using UnityEngine;
namespace ServiceGameV2 {
 // Residents turn their head and eyes to follow you while you stand near them (humanoid look-at IK).
 [RequireComponent(typeof(Animator))]
 public sealed class ServiceLookAt:MonoBehaviour {
  Animator a;Transform view;float weight;
  void Start(){a=GetComponent<Animator>();var scene=FindAnyObjectByType<CountyScene>();if(scene&&scene.View)view=scene.View.transform;}
  void OnAnimatorIK(int layer){
   if(!a||!view)return;
   var to=view.position-transform.position;float range=to.magnitude;to.y=0;
   bool facing=Vector3.Dot(transform.forward,to.normalized)>-.1f;
   weight=Mathf.MoveTowards(weight,range<9&&facing?1:0,Time.deltaTime*1.6f);
   a.SetLookAtWeight(weight,.2f,.8f,1,.6f);a.SetLookAtPosition(view.position);
  }
 }
}
