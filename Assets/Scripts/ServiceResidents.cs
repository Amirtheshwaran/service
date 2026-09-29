using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceResidents:MonoBehaviour {
  public GameObject Correll,Bell;public Transform DepotWorker;public Animation WorkerAnimation;public Vector3 WalkFrom,WalkTo;
  ServiceDirector d;bool returning;float turn;
  void Start(){d=GetComponentInParent<ServiceDirector>();}
  void Update(){if(!d)return;if(Correll)Correll.SetActive(d.IsFriendly(0));if(Bell)Bell.SetActive(d.IsFriendly(4));if(!DepotWorker)return;bool active=d.Phase==ServicePhase.Playing&&!d.PaperOpen;var player=d.Scene.Walker.transform.position;bool obstructed=!d.Player.InCar&&Vector2.Distance(new Vector2(player.x,player.z),new Vector2(DepotWorker.position.x,DepotWorker.position.z))<1.15f;foreach(AnimationState state in WorkerAnimation)state.speed=active&&!obstructed&&turn<=0?.93f:0;if(!active||obstructed)return;var target=returning?WalkFrom:WalkTo;var delta=target-DepotWorker.position;delta.y=0;if(delta.magnitude<.08f){returning=!returning;turn=.9f;return;}DepotWorker.rotation=Quaternion.RotateTowards(DepotWorker.rotation,Quaternion.LookRotation(delta),160*Time.deltaTime);if(turn>0){turn-=Time.deltaTime;return;}DepotWorker.position=Vector3.MoveTowards(DepotWorker.position,target,Time.deltaTime*1.2f);}
 }
}
