using UnityEngine;
namespace ServiceGameV2 {
 // V23: a fire in the hearth ("maybe add fire to the fireplace"): a warm light that breathes and the Nox Sound campfire
 // crackle (CC0); V24 adds the visible fire below. Built by the editor stages World23/Fire24; lit only on the nights
 // someone keeps it (NightsLit: bit 0 night one ... bit 2 night three). The night-one door beat snuffs it with the
 // lights; every new shift relights it. (Flames: unused since V23, kept for scenes built before V24.)
 public sealed class ServiceHearth:MonoBehaviour {
  public int Property=-1;
  public int NightsLit=7;
  public Light Glow;
  public Transform[] Flames=new Transform[0];
  // V24: flames you can see (Kenney Particle Pack, CC0): the fireplaces' iron covers are part of the cabin shells, so the
  // editor stage Fire24 lays a dark firebox over the cover with an ember bed and flame particles in it. The firebox shows
  // on the nights the fire is kept; snuffed (the night-one door beat), the flames die and the embers sink to a glow.
  public GameObject FireBox;public ParticleSystem[] FireParticles=new ParticleSystem[0];public Renderer Embers;
  MaterialPropertyBlock mpb;float ember=1;static readonly int IntensityId=Shader.PropertyToID("_Intensity");
  public bool Tonight=>d&&((NightsLit>>Mathf.Clamp(d.NightIndex,0,2))&1)==1;
  public int LiveFlames {get{int n=0;foreach(var ps in FireParticles)if(ps)n+=ps.particleCount;return n;}}
  Vector3[] baseScale;Quaternion[] baseRot;AudioSource crackle;float baseIntensity,seed;bool snuffed;ServiceDirector d;
  public bool Lit=>Tonight&&!snuffed;
  public void Snuff(){snuffed=true;}
  public void Relight(){snuffed=false;}
  void Start(){
   d=FindAnyObjectByType<ServiceDirector>();seed=Random.value*100f;if(Glow)baseIntensity=Glow.intensity;
   baseScale=new Vector3[Flames.Length];baseRot=new Quaternion[Flames.Length];for(int i=0;i<Flames.Length;i++)if(Flames[i]){baseScale[i]=Flames[i].localScale;baseRot[i]=Flames[i].localRotation;}
   crackle=gameObject.AddComponent<AudioSource>();crackle.clip=Resources.Load<AudioClip>("Audio/V23/fire/Ambiance_Firecamp_Small_Loop_Mono");crackle.loop=true;crackle.playOnAwake=false;
   crackle.spatialBlend=1;crackle.rolloffMode=AudioRolloffMode.Logarithmic;crackle.minDistance=1.2f;crackle.maxDistance=16f;crackle.dopplerLevel=0;crackle.volume=.55f;crackle.time=Random.Range(0f,5f);}
  void Update(){
   bool lit=Lit,tonight=Tonight;if(Glow)Glow.enabled=lit;
   if(FireBox&&FireBox.activeSelf!=tonight)FireBox.SetActive(tonight);
   foreach(var ps in FireParticles){if(!ps)continue;var em=ps.emission;if(em.enabled!=lit)em.enabled=lit;}
   if(Embers&&tonight){if(mpb==null)mpb=new MaterialPropertyBlock();float n=Mathf.PerlinNoise(seed+2.1f,Time.time*2.3f)*.6f+Mathf.PerlinNoise(seed+5.9f,Time.time*7.1f)*.4f;
    ember=Mathf.MoveTowards(ember,lit?.75f+.5f*n:.12f+.06f*n,Time.deltaTime*(lit?3f:.25f));Embers.GetPropertyBlock(mpb);mpb.SetFloat(IntensityId,ember);Embers.SetPropertyBlock(mpb);}
   for(int i=0;i<Flames.Length;i++)if(Flames[i]&&Flames[i].gameObject.activeSelf!=lit)Flames[i].gameObject.SetActive(lit);
   if(crackle&&crackle.clip){if(lit&&!crackle.isPlaying)crackle.Play();else if(!lit&&crackle.isPlaying)crackle.Stop();}
   if(!lit)return;float t=Time.time;
   if(Glow){float n=Mathf.PerlinNoise(seed,t*3.1f)*.6f+Mathf.PerlinNoise(seed+7.3f,t*9.7f)*.4f;Glow.intensity=baseIntensity*(.72f+.56f*n);}
   for(int i=0;i<Flames.Length;i++){if(!Flames[i])continue;float n=Mathf.PerlinNoise(seed+i*3.7f,t*(4.2f+i*.6f));float h=Mathf.PerlinNoise(seed-i*1.9f,t*6.3f);
    Flames[i].localScale=Vector3.Scale(baseScale[i],new Vector3(.88f+.24f*h,.78f+.44f*n,.88f+.24f*h));Flames[i].localRotation=baseRot[i]*Quaternion.Euler(0,(n-.5f)*24f,(h-.5f)*8f);}
  }
 }
}
