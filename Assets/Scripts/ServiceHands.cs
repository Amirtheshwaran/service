using System.Collections;using System.Linq;using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceHands:MonoBehaviour {
  public Animation Rig;ServiceDirector d;string idle;Coroutine action;bool gesturing;
  void Start(){d=FindAnyObjectByType<ServiceDirector>();idle=Find("|relax");Play(idle,true);gesturing=false;}
  string Find(string suffix)=>Rig?Rig.Cast<AnimationState>().Select(s=>s.name).FirstOrDefault(s=>s.EndsWith(suffix)):null;
  void Play(string name,bool loop){if(Rig&&!string.IsNullOrEmpty(name)){Rig[name].wrapMode=loop?WrapMode.Loop:WrapMode.Once;Rig.CrossFade(name,.12f);}}
  void LateUpdate(){if(!d)return;bool visible=d.Phase==ServicePhase.Playing&&!d.Player.InCar&&!d.PaperOpen&&!d.Horror.Caught&&gesturing;foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=visible;}
  public void Interact(bool knock){if(action!=null)StopCoroutine(action);action=StartCoroutine(Gesture(knock));}
  IEnumerator Gesture(bool knock){gesturing=true;var name=Find(knock?"|jab.R":"|grab.R");if(string.IsNullOrEmpty(name)){gesturing=false;yield break;}Rig[name].speed=knock?1.6f:1;Play(name,false);yield return new WaitForSeconds(Rig[name].length/Rig[name].speed);Play(idle,true);gesturing=false;}
 }
}
