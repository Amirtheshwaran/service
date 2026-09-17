using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
namespace ServiceGameV2 {
 public sealed class ServiceVehiclePresentation:MonoBehaviour {
  ServiceDirector d;Camera mirror;RenderTexture image;Material mirrorMaterial;AudioSource radio;AudioClip[] stations;TextMesh display;
  public bool RadioOn {get;private set;} public int Station {get;private set;} public bool MirrorLive=>mirror&&mirror.enabled&&image.IsCreated();
  public bool StationsAvailable=>stations!=null&&stations.Length==2&&stations[0]&&stations[1];
  public float RadioVolume=.65f; public RenderTexture MirrorTexture=>image;
  public string StationName=>Station==0?"88.5 · Night jazz":"104.2 · Easy listening";
  public void Initialize(ServiceDirector director){
   d=director;var glass=d.Scene.Cockpit.transform.Find("Rearview glass");
   if(glass){image=new RenderTexture(768,192,16){name="Rearview reflection"};image.Create();var o=new GameObject("Rearview camera");mirror=o.AddComponent<Camera>();mirror.transform.SetParent(d.Scene.Car,false);mirror.transform.localPosition=new Vector3(0,1.35f,-1.14f);mirror.transform.localRotation=Quaternion.Euler(0,180,0);mirror.fieldOfView=28;mirror.nearClipPlane=.15f;mirror.farClipPlane=100;mirror.cullingMask=~((1<<8)|(1<<10));mirror.clearFlags=CameraClearFlags.SolidColor;mirror.targetTexture=image;mirror.depth=-10;mirror.GetUniversalAdditionalCameraData().renderShadows=false;
    mirrorMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mirrorMaterial.SetTexture("_BaseMap",image);mirrorMaterial.SetTextureScale("_BaseMap",new Vector2(-1,1));mirrorMaterial.SetTextureOffset("_BaseMap",new Vector2(1,0));glass.GetComponent<Renderer>().sharedMaterial=mirrorMaterial;}
   var audio=new GameObject("Dashboard radio");audio.transform.SetParent(d.Scene.Car,false);audio.transform.localPosition=new Vector3(.12f,.83f,.3f);radio=audio.AddComponent<AudioSource>();radio.playOnAwake=false;radio.loop=true;radio.spatialBlend=1;radio.minDistance=1.8f;radio.maxDistance=10;radio.rolloffMode=AudioRolloffMode.Linear;radio.dopplerLevel=0;audio.AddComponent<AudioLowPassFilter>().cutoffFrequency=3200;audio.AddComponent<AudioHighPassFilter>().cutoffFrequency=260;
   stations=new[]{Resources.Load<AudioClip>("Audio/Radio/George Street Shuffle"),Resources.Load<AudioClip>("Audio/Radio/Local Forecast - Elevator")};
   foreach(var text in d.Scene.Cockpit.GetComponentsInChildren<TextMesh>())if((text.text.Contains("88.5")||text.text.Contains("RADIO OFF"))){display=text;break;}
   Refresh();
  }
  public void ToggleRadio(){RadioOn=!RadioOn;Refresh();}
  public void NextStation(){Station=(Station+1)%stations.Length;RadioOn=true;Refresh();}
  void Refresh(){if(display)display.text=RadioOn?(Station==0?"88.5 FM":"104.2 FM"):"RADIO OFF";if(RadioOn&&stations[Station]){radio.clip=stations[Station];radio.Play();}else radio.Stop();}
  void Update(){if(!d)return;bool playing=d.Phase==ServicePhase.Playing;
   if(mirror){mirror.enabled=playing&&d.Player.InCar;mirror.backgroundColor=RenderSettings.fogColor;}
   radio.volume=Mathf.MoveTowards(radio.volume,playing&&RadioOn?(d.Player.InCar?.22f:.1f)*RadioVolume:0,Time.unscaledDeltaTime);
   if(!playing||!d.Player.InCar||d.InputBlocked)return;var k=Keyboard.current;if(k!=null){if(k[Key.V].wasPressedThisFrame)ToggleRadio();if(k[Key.B].wasPressedThisFrame)NextStation();}
  }
  void OnDestroy(){if(image){image.Release();Destroy(image);}if(mirrorMaterial)Destroy(mirrorMaterial);}
 }
}
