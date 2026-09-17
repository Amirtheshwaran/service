using System.Collections;
using System.Linq;
using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceLife:MonoBehaviour {
  ServiceDirector d;Transform dog,model;Animation animationPlayer;Vector3 home,end;string walk,idle,bark;float pace,wait;int direction=1;bool moving;bool[] seen=new bool[6];Quaternion[] rest=new Quaternion[6];
  public bool DogPresent=>dog&&model;public float DogTravel {get;private set;}
  public void Initialize(ServiceDirector director){d=director;dog=transform.Find("Correll yard dog");if(dog){home=dog.position;end=home+d.Property(0).Door.right*2.5f;model=dog.GetChild(0);animationPlayer=dog.GetComponentInChildren<Animation>();if(animationPlayer){var names=animationPlayer.Cast<AnimationState>().Select(s=>s.name).ToArray();walk=names.FirstOrDefault(n=>n.ToLower().Contains("walk"))??names.FirstOrDefault(n=>n.ToLower().Contains("run"));if(walk!=null)animationPlayer[walk].speed=.45f;idle=names.FirstOrDefault(n=>n.ToLower().Contains("idle"))??names.FirstOrDefault();bark=names.FirstOrDefault(n=>n.ToLower().Contains("bark"));Play(idle);}}
   for(int i=0;i<6;i++)rest[i]=d.Property(i).DoorPanel?d.Property(i).DoorPanel.localRotation:Quaternion.identity;
  }
  public void ResetForShift(){StopAllCoroutines();System.Array.Clear(seen,0,seen.Length);if(dog)dog.position=home;for(int i=0;i<6;i++)if(d.Property(i).DoorPanel)d.Property(i).DoorPanel.localRotation=rest[i]*Quaternion.Euler(0,i==4&&!d.IsFriendly(i)?100:0,0);}
  void Play(string name){if(animationPlayer&&!string.IsNullOrEmpty(name)&&!animationPlayer.IsPlaying(name))animationPlayer.CrossFade(name,.2f);}
  public void Bark(){if(!dog)return;d.Audio.DogAt(dog.position+Vector3.up*.4f,.32f);if(!string.IsNullOrEmpty(bark)){Play(bark);wait=1.8f;}}
  void Update(){if(!d||d.Phase!=ServicePhase.Playing||d.PaperOpen)return;
   if(dog){float distance=Vector3.Distance(d.Scene.View.transform.position,dog.position);if(distance<36){wait-=Time.deltaTime;
     if(wait<=0){var target=direction>0?end:home;var before=dog.position;dog.position=Vector3.MoveTowards(before,target,Time.deltaTime*.65f);DogTravel+=Vector3.Distance(before,dog.position);var facing=target-before;facing.y=0;if(facing.sqrMagnitude>.01f)dog.rotation=Quaternion.Slerp(dog.rotation,Quaternion.LookRotation(facing),Time.deltaTime*4);Play(walk??idle);if(Vector3.Distance(dog.position,target)<.08f){direction*=-1;wait=4;Play(idle);}}
     else if(string.IsNullOrEmpty(bark)||!animationPlayer.IsPlaying(bark))Play(idle);
    }}
   if(d.Player.InCar)return;
   foreach(var p in d.Scene.Properties){if(d.IsFriendly(p.Index)||seen[p.Index]||Vector3.Distance(d.Scene.Walker.transform.position,p.Door.position)>14)continue;seen[p.Index]=true;StartCoroutine(Flicker(p));}
  }
  IEnumerator Flicker(ServiceProperty p){if(!p.PorchLight)yield break;var light=p.PorchLight;float baseline=light.intensity;foreach(float value in new[]{.15f,1f,.1f,.2f,1f}){light.intensity=baseline*value;yield return new WaitForSeconds(.12f);}light.intensity=baseline;}
  public void OpenDoor(ServiceProperty p,bool open){if(p.DoorPanel)StartCoroutine(Door(p,open));}
  IEnumerator Door(ServiceProperty p,bool open){var start=p.DoorPanel.localRotation;var goal=rest[p.Index]*Quaternion.Euler(0,open?(d.IsFriendly(p.Index)?23:100):0,0);float time=0;while(time<.65f){time+=Time.deltaTime;p.DoorPanel.localRotation=Quaternion.Slerp(start,goal,Mathf.SmoothStep(0,1,time/.65f));yield return null;}p.DoorPanel.localRotation=goal;}
 }
}
