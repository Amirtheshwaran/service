using UnityEngine;
namespace ServiceGameV2 {
 // V27 ("ambience sound will go a long way - rustling leaves, owl noises"; "with the apparitions you could add creepy
 // floorboards, or house noises"). Outside on foot: something moving through the dry leaves off in the dark, now and
 // then a far horn; a night bed under it. Inside a house: the boards - a step in the next room, a step overhead, a door
 // easing somewhere. More of it each night. Recorded sounds only (Nox Sound, Flooded Grounds, and great horned owls
 // recorded for xeno-canto). The owls call from up in the trees, on foot or in the car, and go quiet while something
 // is after you.
 public sealed class ServiceAmbience27:MonoBehaviour {
  ServiceDirector d;AudioSource bed;float nextOut,nextIn,nextHorn,nextOwl;int steps;Vector3 stepFrom;float stepAt;
  public int Rustles {get;private set;} public int HouseNoises {get;private set;} public int Owls {get;private set;}
  public void SmokeOwlNow(){nextOwl=0;}
  public void Initialize(ServiceDirector director){d=director;bed=gameObject.AddComponent<AudioSource>();bed.loop=true;bed.spatialBlend=0;bed.volume=0;bed.playOnAwake=false;var c=d.Audio.Pool("nightbed");if(c){bed.clip=c;bed.Play();}
   nextOut=Time.time+6;nextIn=Time.time+8;nextHorn=Time.time+80;nextOwl=Time.time+Random.Range(12f,22f);}
  void Update(){
   if(!d||d.Phase!=ServicePhase.Playing||d.Audio.Hush||d.PaperOpen){if(bed)bed.volume=Mathf.MoveTowards(bed.volume,0,Time.unscaledDeltaTime*.2f);return;}
   bool car=d.Player.InCar;var w=d.Scene.Walker.transform.position;bool indoors=!car&&ServiceLife.UnderRoof(w)&&d.InsideVilla;bool threat=d.Horror.Active||d.Horror.Caught;
   if(bed)bed.volume=Mathf.MoveTowards(bed.volume,car||threat?.0f:indoors?.012f:.05f,Time.unscaledDeltaTime*.05f);
   // an owl up in the trees, off the road; from the car it is fainter (the glass)
   if(!threat&&!indoors&&Time.time>nextOwl){nextOwl=Time.time+Random.Range(26f,60f);var cv=d.Scene.View.transform;var od=Quaternion.Euler(0,Random.Range(0f,360f),0)*Vector3.forward;
    Owls++;d.Audio.Far("owl",cv.position+od*Random.Range(26f,46f)+Vector3.up*Random.Range(6f,11f),car?.3f:.62f,Random.Range(.95f,1.03f));}
   if(car||threat)return;float pace=1+d.NightIndex*.45f;var cam=d.Scene.View.transform;
   if(!indoors&&Time.time>nextOut){nextOut=Time.time+Random.Range(7f,16f)/pace;
    // off to one side or behind, never in plain view ahead
    var dir=Quaternion.Euler(0,Random.Range(70f,290f),0)*cam.forward;dir.y=0;dir.Normalize();var at=cam.position+dir*Random.Range(9f,22f);
    if(Random.value<.7f){Rustles++;int n=Random.Range(2,5);for(int i=0;i<n;i++)StartCoroutine(Later(i*Random.Range(.28f,.5f),"leaves",at+Random.insideUnitSphere*.8f,Random.Range(.16f,.26f)));}
    else{Rustles++;d.Audio.HorrorAt("rustle",at,.14f);}}
   if(!indoors&&d.NightIndex>0&&Time.time>nextHorn){nextHorn=Time.time+Random.Range(110f,200f);var dir=Quaternion.Euler(0,Random.Range(0f,360f),0)*Vector3.forward;d.Audio.Far("horn",cam.position+dir*70f,.2f,1f);}
   if(indoors&&Time.time>nextIn){nextIn=Time.time+Random.Range(9f,20f)/pace;HouseNoises++;float pick=Random.value;
    var dir=Quaternion.Euler(0,Random.Range(0f,360f),0)*Vector3.forward;
    if(pick<.55f){// a few steps somewhere in the house - in the next room, or overhead
     bool above=Random.value<.4f;stepFrom=cam.position+dir*Random.Range(4f,8f)+(above?Vector3.up*1.6f:Vector3.down*1.3f);steps=Random.Range(2,5);stepAt=Time.time;}
    else if(pick<.8f)d.Audio.HorrorAt("woodstress",cam.position+dir*Random.Range(4f,9f),.13f,Random.Range(.75f,.95f));
    else d.Audio.HorrorAt("taps",cam.position+dir*Random.Range(3f,7f),.12f);}
   if(steps>0&&Time.time>stepAt){steps--;stepAt=Time.time+Random.Range(.55f,.8f);var side=Vector3.Cross(Vector3.up,(stepFrom-cam.position).normalized);stepFrom+=side*.55f;d.Audio.HorrorAt("floorstep",stepFrom,.16f,.85f);}
  }
  System.Collections.IEnumerator Later(float t,string pool,Vector3 at,float vol){yield return new WaitForSeconds(t);d.Audio.HorrorAt(pool,at,vol,Random.Range(.9f,1.08f));}
 }
}
