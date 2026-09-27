using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
namespace ServiceGameV2 {
 public sealed class ServiceVehiclePresentation:MonoBehaviour {
  ServiceDirector d;Camera mirror;RenderTexture image;Material mirrorMaterial;AudioSource radio;AudioClip[] stations;TextMesh display;
  Transform[] wipers;Quaternion[] wiperRest;AudioSource wiperAudio,roadAudio;float wiperTime;
  public float WiperAngle {get;private set;}
  public bool RadioOn {get;private set;} public int Station {get;private set;} public bool MirrorLive=>mirror&&mirror.enabled&&image.IsCreated();
  public bool StationsAvailable=>stations!=null&&stations.Length>=2&&stations.All(c=>c);
  public float RadioVolume=.65f; public RenderTexture MirrorTexture=>image;
  public string StationName=>stationNames[Station];
  readonly string[] stationNames={"88.5 · Night jazz","104.2 · Easy listening","91.7 · After hours"};
  public void Initialize(ServiceDirector director){
   d=director;var glass=d.Scene.Cockpit.transform.Find("Rearview glass");
   if(glass){image=new RenderTexture(768,192,16){name="Rearview reflection"};image.Create();var o=new GameObject("Rearview camera");mirror=o.AddComponent<Camera>();mirror.transform.SetParent(d.Scene.Car,false);mirror.transform.localPosition=new Vector3(0,1.35f,-1.14f);mirror.transform.localRotation=Quaternion.Euler(0,180,0);mirror.fieldOfView=28;mirror.nearClipPlane=.15f;mirror.farClipPlane=100;mirror.cullingMask=~((1<<8)|(1<<10));mirror.clearFlags=CameraClearFlags.SolidColor;mirror.targetTexture=image;mirror.depth=-10;mirror.GetUniversalAdditionalCameraData().renderShadows=false;
    // UV orientation belongs to the fitted glass, not to the camera feed.
    var scale=d.Scene.MirrorUVScale;
    mirrorMaterial=new Material(glass.GetComponent<Renderer>().sharedMaterial);mirrorMaterial.SetTexture("_BaseMap",image);mirrorMaterial.SetTextureScale("_BaseMap",scale);mirrorMaterial.SetTextureOffset("_BaseMap",new Vector2(scale.x<0?1:0,scale.y<0?1:0));glass.GetComponent<Renderer>().sharedMaterial=mirrorMaterial;}
   var audio=new GameObject("Dashboard radio");audio.transform.SetParent(d.Scene.Car,false);audio.transform.localPosition=new Vector3(.12f,.83f,.3f);radio=audio.AddComponent<AudioSource>();radio.playOnAwake=false;radio.loop=true;radio.spatialBlend=1;radio.minDistance=1.8f;radio.maxDistance=10;radio.rolloffMode=AudioRolloffMode.Linear;radio.dopplerLevel=0;audio.AddComponent<AudioLowPassFilter>().cutoffFrequency=3200;audio.AddComponent<AudioHighPassFilter>().cutoffFrequency=260;
   stations=new[]{Resources.Load<AudioClip>("Audio/Radio/George Street Shuffle"),Resources.Load<AudioClip>("Audio/Radio/Local Forecast - Elevator"),Resources.Load<AudioClip>("Audio/Radio/After Hours")};
   foreach(var text in d.Scene.Cockpit.GetComponentsInChildren<TextMesh>())if((text.text.Contains("88.5")||text.text.Contains("RADIO OFF"))){display=text;break;}
   wipers=d.Scene.Cockpit.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Rain wiper ")).ToArray();wiperRest=wipers.Select(t=>t.localRotation).ToArray();
   wiperAudio=Loop("Rain wiper motor","wiper",.10f);roadAudio=Loop("Tyres on wet road","road",0);
   Refresh();
  }
  AudioSource Loop(string name,string clip,float volume){var o=new GameObject(name);o.transform.SetParent(d.Scene.Car,false);var source=o.AddComponent<AudioSource>();source.clip=Resources.LoadAll<AudioClip>("Audio/V12/"+clip).FirstOrDefault();source.loop=true;source.playOnAwake=false;source.volume=volume;source.spatialBlend=0;return source;}
  public void ToggleRadio(){d.Audio.HorrorAt("switch",d.Scene.View.transform.position,.12f);RadioOn=!RadioOn;Refresh();}
  public void NextStation(){Station=(Station+1)%stations.Length;RadioOn=true;Refresh();}
  void Refresh(){if(display)display.text=RadioOn?(Station==0?"88.5 FM":Station==1?"104.2 FM":"91.7 FM"):"RADIO OFF";if(RadioOn&&stations[Station]){radio.clip=stations[Station];radio.Play();}else radio.Stop();}
  void Update(){if(!d)return;bool playing=d.Phase==ServicePhase.Playing&&!d.PaperOpen;
   bool wiping=wipers.Length>0&&playing&&d.Player.InCar&&d.Player.EngineRunning&&d.Storm.IsRaining;
   if(wiping)wiperTime+=Time.deltaTime;
   WiperAngle=wiping?Mathf.Sin(wiperTime*3.7f-Mathf.PI*.5f)*41+41:Mathf.MoveTowards(WiperAngle,0,Time.deltaTime*120);
   for(int i=0;i<wipers.Length;i++)wipers[i].localRotation=wiperRest[i]*Quaternion.Euler(0,0,WiperAngle);
   if(wiping&&!wiperAudio.isPlaying)wiperAudio.Play();else if(!wiping)wiperAudio.Stop();
   bool rolling=playing&&d.Player.InCar&&d.Player.Speed>.3f;
   if(rolling&&!roadAudio.isPlaying)roadAudio.Play();else if(!rolling)roadAudio.Stop();roadAudio.volume=Mathf.Clamp01(d.Player.Speed/14)*.12f;
   if(mirror){mirror.enabled=playing&&d.Player.InCar;mirror.backgroundColor=RenderSettings.fogColor;}
   radio.volume=Mathf.MoveTowards(radio.volume,playing&&RadioOn?(d.Player.InCar?.22f:.1f)*RadioVolume:0,Time.unscaledDeltaTime);
   if(!playing||!d.Player.InCar||d.InputBlocked)return;var k=Keyboard.current;if(k!=null){if(k[Key.V].wasPressedThisFrame)ToggleRadio();if(k[Key.B].wasPressedThisFrame)NextStation();}
  }
  void OnDestroy(){if(image){image.Release();Destroy(image);}if(mirrorMaterial)Destroy(mirrorMaterial);}
 }
}
