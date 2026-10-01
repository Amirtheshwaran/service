using UnityEngine;
namespace ServiceGameV2 {
 // Keeps a humanoid's soles on the floor beneath it after animation, whatever the rig's root height or clip offset.
 [RequireComponent(typeof(Animator))]
 public sealed class ServiceGrounding:MonoBehaviour {
  Animator a;Transform[] feet;Vector3 rest;float offset;bool ready;
  public float FloorGap {get;private set;}
  void Start(){
   a=GetComponent<Animator>();rest=transform.localPosition;
   if(a.isHuman)feet=new[]{a.GetBoneTransform(HumanBodyBones.LeftToes),a.GetBoneTransform(HumanBodyBones.RightToes),a.GetBoneTransform(HumanBodyBones.LeftFoot),a.GetBoneTransform(HumanBodyBones.RightFoot)};
   ready=feet!=null&&feet[2]&&feet[3];
  }
  void LateUpdate(){
   if(!ready)return;
   float sole=float.MaxValue;
   for(int i=0;i<4;i++){var f=feet[i];if(!f)continue;sole=Mathf.Min(sole,f.position.y-(i<2?.025f:.075f));}
   var probe=(feet[2].position+feet[3].position)*.5f;probe.y=transform.position.y+1.2f;
   // The highest surface below the hips is the floor the resident stands on (not the storey beneath it).
   var self=transform.parent?transform.parent:transform;float floor=float.NegativeInfinity;
   foreach(var h in Physics.RaycastAll(probe,Vector3.down,3f,~0,QueryTriggerInteraction.Ignore)){if(h.transform.IsChildOf(self))continue;if(h.point.y>floor)floor=h.point.y;}
   if(float.IsNegativeInfinity(floor))return;
   FloorGap=sole-floor;
   offset=Mathf.Clamp(offset-FloorGap,-.6f,.6f);
   transform.localPosition=rest+transform.parent.InverseTransformDirection(Vector3.up)*offset;
  }
 }
}
