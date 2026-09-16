using System.Collections;
using UnityEngine;
namespace ServiceGameV2 {
 public sealed class ServiceStorm:MonoBehaviour {
  ServiceDirector d;Light flash;float nextFlash,exposureCheck,flashTime=-1;Color baseFog;
  public bool Sheltered {get;private set;}
  public bool IsRaining=>d&&d.Scene.Rain;
  public bool FlashesEnabled=true;
  public int LightningCount {get;private set;}
  public float FlashStrength {get;private set;}
  public void Initialize(ServiceDirector director){
   d=director;baseFog=RenderSettings.fogColor;FlashesEnabled=PlayerPrefs.GetInt("SERVICE.lightning",1)!=0;
   var o=new GameObject("Distant storm illumination");o.transform.SetParent(transform);flash=o.AddComponent<Light>();flash.type=LightType.Directional;flash.color=new Color(.76f,.83f,1);flash.shadows=LightShadows.None;flash.intensity=0;flash.transform.rotation=Quaternion.Euler(35,115,0);nextFlash=Time.time+8;
  }
  public bool Covered(Vector3 feet){
   if(d.InsideVilla)return true;
   return Physics.Raycast(feet+Vector3.up*2,Vector3.up,10,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore);
  }
  void Update(){
   if(!d||d.Phase==ServicePhase.Paused)return;
   if(Time.time>=exposureCheck){exposureCheck=Time.time+.12f;Sheltered=d.Phase!=ServicePhase.Title&&(d.Player.InCar||Covered(d.Scene.Walker.transform.position));}
   var rain=d.Scene.Rain;
   if(rain){rain.transform.position=d.Scene.View.transform.position+Vector3.up*9;
    bool indoors=d.Phase!=ServicePhase.Title&&!d.Player.InCar&&Sheltered;
    if(indoors){if(rain.isPlaying)rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    else if(!rain.isPlaying)rain.Play();
   }
   if(Time.time>=nextFlash&&(!d.Presentation||!d.Presentation.IntroVisible))TriggerLightning();
   FlashStrength=flashTime<0?0:Mathf.Pow(Mathf.Clamp01(1-(Time.time-flashTime)/.65f),2);
   if(!FlashesEnabled)FlashStrength=0;
   flash.intensity=FlashStrength*1.8f;
   RenderSettings.fogColor=Color.Lerp(baseFog,new Color(.77f,.80f,.89f),FlashStrength*.4f);d.Scene.View.backgroundColor=RenderSettings.fogColor;
  }
  public void TriggerLightning(){flashTime=Time.time;nextFlash=Time.time+Random.Range(24f,46f);LightningCount++;StartCoroutine(ThunderAfter(Random.Range(1.4f,3.4f)));}
  IEnumerator ThunderAfter(float delay){yield return new WaitForSeconds(delay);if(d&&d.Audio)d.Audio.Thunder(Sheltered);}
  public void Save(){PlayerPrefs.SetInt("SERVICE.lightning",FlashesEnabled?1:0);}
 }
}
