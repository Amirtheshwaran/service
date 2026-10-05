using UnityEngine;
namespace ServiceGameV2 {
 // V23: a fire in the hearth ("maybe add fire to the fireplace"). Authored flame meshes (the Poly Haven oil-lamp flame,
 // several, scaled up) over kindling, a warm light that breathes with them, and the Nox Sound campfire crackle (CC0).
 // Built by the editor stage World23; lit only on the nights someone keeps it (NightsLit: bit 0 night one ... bit 2 night
 // three). The night-one door beat snuffs it with the lights; every new shift relights it.
 public sealed class ServiceHearth:MonoBehaviour {
  public int Property=-1;
  public int NightsLit=7;
  public Light Glow;
  public Transform[] Flames=new Transform[0];
  Vector3[] baseScale;Quaternion[] baseRot;AudioSource crackle;float baseIntensity,seed;bool snuffed;ServiceDirector d;
  public bool Lit=>d&&!snuffed&&((NightsLit>>Mathf.Clamp(d.NightIndex,0,2))&1)==1;
  public void Snuff(){snuffed=true;}
  public void Relight(){snuffed=false;}
  void Start(){
   d=FindAnyObjectByType<ServiceDirector>();seed=Random.value*100f;if(Glow)baseIntensity=Glow.intensity;
   baseScale=new Vector3[Flames.Length];baseRot=new Quaternion[Flames.Length];for(int i=0;i<Flames.Length;i++)if(Flames[i]){baseScale[i]=Flames[i].localScale;baseRot[i]=Flames[i].localRotation;}
   crackle=gameObject.AddComponent<AudioSource>();crackle.clip=Resources.Load<AudioClip>("Audio/V23/fire/Ambiance_Firecamp_Small_Loop_Mono");crackle.loop=true;crackle.playOnAwake=false;
   crackle.spatialBlend=1;crackle.rolloffMode=AudioRolloffMode.Logarithmic;crackle.minDistance=1.2f;crackle.maxDistance=16f;crackle.dopplerLevel=0;crackle.volume=.55f;crackle.time=Random.Range(0f,5f);}
  void Update(){
   bool lit=Lit;if(Glow)Glow.enabled=lit;
   for(int i=0;i<Flames.Length;i++)if(Flames[i]&&Flames[i].gameObject.activeSelf!=lit)Flames[i].gameObject.SetActive(lit);
   if(crackle&&crackle.clip){if(lit&&!crackle.isPlaying)crackle.Play();else if(!lit&&crackle.isPlaying)crackle.Stop();}
   if(!lit)return;float t=Time.time;
   if(Glow){float n=Mathf.PerlinNoise(seed,t*3.1f)*.6f+Mathf.PerlinNoise(seed+7.3f,t*9.7f)*.4f;Glow.intensity=baseIntensity*(.72f+.56f*n);}
   for(int i=0;i<Flames.Length;i++){if(!Flames[i])continue;float n=Mathf.PerlinNoise(seed+i*3.7f,t*(4.2f+i*.6f));float h=Mathf.PerlinNoise(seed-i*1.9f,t*6.3f);
    Flames[i].localScale=Vector3.Scale(baseScale[i],new Vector3(.88f+.24f*h,.78f+.44f*n,.88f+.24f*h));Flames[i].localRotation=baseRot[i]*Quaternion.Euler(0,(n-.5f)*24f,(h-.5f)*8f);}
  }
 }
}
