using System.Collections.Generic;
using UnityEngine;

namespace ServiceGameV2
{
    public sealed class ServiceAudio : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        ServiceDirector d;
        AudioSource engine, dog, ambience, cabin, wind, tension, room,rain,music;
        AudioLowPassFilter rainFilter;
        Vector3 lastBrushPosition;float brushCheck,brushCooldown;
        public int BrushEvents {get;private set;}
        public string LastFootstepPool {get;private set;}="grass";
        public float MusicVolume=.7f;
        public float ChaseLevel=>tension?tension.volume:0;
        readonly Dictionary<string,AudioClip[]> pools=new Dictionary<string,AudioClip[]>();
        readonly Dictionary<string,int> previous=new Dictionary<string,int>();
        float nextAtmosphere=12;
        public string LastSurface {get;private set;}="grass";
        bool pursuing;
        static bool silentTest;
        readonly List<AudioSource> forestPockets=new List<AudioSource>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void TestSilence(){silentTest=System.Array.Exists(System.Environment.GetCommandLineArgs(),a=>a=="-serviceSmoke"||a=="-silent")||System.Environment.GetEnvironmentVariable("SERVICE_SILENT_TEST")=="1";if(silentTest)AudioListener.volume=0;}
        public float Volume = .8f;
        public void Initialize(ServiceDirector director)
        {
            d = director;
            silentTest|=d.IsSmoke;AudioListener.volume=silentTest?0:Volume;
            AudioListener.pause=false;if(!d.IsSmoke)Volume=Mathf.Clamp01(PlayerPrefs.GetFloat("SERVICE.volume",.8f));
            if(!d.IsSmoke)MusicVolume=Mathf.Clamp01(PlayerPrefs.GetFloat("SERVICE.music",.7f));
            tension=Source("Horror pursuit score",d.Scene.View.transform,0);tension.clip=Pick("chase");tension.loop=true;tension.volume=0;
            music=Source("Traversal score",d.Scene.View.transform,0);music.clip=Pick("traversal");music.loop=true;music.volume=0;if(music.clip)music.Play();
            rain=Source("Recorded rainfall",d.Scene.View.transform,0);rain.clip=Pick("rain");rain.loop=true;rain.volume=0;rainFilter=rain.gameObject.AddComponent<AudioLowPassFilter>();if(rain.clip)rain.Play();
            engine = Source("Recorded engine", d.Scene.Car, 0);
            dog = Source("Recorded distant dog", transform, 1);
            ambience = Source("Recorded rural ambience", d.Scene.View.transform, 0);
            cabin = Source("Recorded cabin sounds", d.Scene.View.transform, 0);
            wind = Source("Recorded wind through trees", d.Scene.View.transform, 0);
            wind.clip = Pick("forestwind"); wind.loop = true; wind.volume = .09f;
            if (wind.clip != null) wind.Play();
            engine.clip = Clip("engine"); engine.loop = true; engine.volume = .17f;
            ambience.clip = Pick("night"); ambience.loop = true; ambience.volume = .065f;
            if (ambience.clip != null) ambience.Play();
            room=Source("House room tone",d.Scene.View.transform,0);room.clip=Pick("roomtone");room.loop=true;room.volume=0;if(room.clip)room.Play();
            for(int i=1;i<d.Scene.Route.Length;i+=Mathf.Max(1,d.Scene.Route.Length/5)){
                var pocket=Source("Recorded woodland insects",transform,1);pocket.transform.position=d.Scene.Route[i].position+new Vector3(i%2==0?19:-19,2,0);pocket.clip=Pick("insects");pocket.loop=true;pocket.volume=0;pocket.minDistance=4;pocket.maxDistance=32;
                if(pocket.clip){pocket.time=Random.Range(0,pocket.clip.length);pocket.Play();}forestPockets.Add(pocket);
            }
        }
        AudioSource Source(string name, Transform parent, float spatial)
        {
            GameObject o = new GameObject(name);
            o.transform.SetParent(parent, false);
            AudioSource source = o.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = spatial;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3; source.maxDistance = 42;
            source.dopplerLevel = 0;
            return source;
        }
        AudioClip Clip(string name)
        {
            if (!clips.TryGetValue(name, out AudioClip clip))
            {
                clip = Resources.Load<AudioClip>("Audio/" + name);
                clips[name] = clip;
                if (clip == null) Debug.LogWarning("Recorded audio unavailable; leaving silence: Resources/Audio/" + name);
            }
            return clip;
        }
        AudioClip Pick(string name){
            if(name=="monsterstep")name="wood";
            if(!pools.TryGetValue(name,out var pool)){pool=Resources.LoadAll<AudioClip>("Audio/V9/"+name);if(pool.Length==0)pool=Resources.LoadAll<AudioClip>("Audio/V5/"+name);pools[name]=pool;}
            if(pool.Length==0)return Clip(name);
            int last=previous.TryGetValue(name,out var n)?n:-1;int index=Random.Range(0,pool.Length);if(pool.Length>1&&index==last)index=(index+1)%pool.Length;previous[name]=index;return pool[index];
        }
        void Update()
        {
            AudioListener.volume = silentTest ? 0 : Volume;
            AudioListener.pause=d.Phase==ServicePhase.Paused;
            if(AudioListener.pause)return;
            if(tension!=null){tension.volume=Mathf.MoveTowards(tension.volume,pursuing?.38f*MusicVolume:0,Time.unscaledDeltaTime*.3f);if(!pursuing&&tension.volume<=0&&tension.isPlaying)tension.Stop();}
            if(music)music.volume=Mathf.MoveTowards(music.volume,pursuing?0:(d.Phase==ServicePhase.Title?.16f:.085f)*MusicVolume,Time.unscaledDeltaTime*.065f);
            bool sheltered=d.Storm&&d.Storm.Sheltered;
            if(rain){rain.volume=Mathf.MoveTowards(rain.volume,sheltered?.045f:.17f,Time.unscaledDeltaTime*.2f);rainFilter.cutoffFrequency=Mathf.MoveTowards(rainFilter.cutoffFrequency,sheltered?1600:18000,Time.unscaledDeltaTime*16000);}
            bool inside = d.Player == null || d.Player.InCar;
            if(wind != null) wind.volume = Mathf.MoveTowards(wind.volume, d.InsideVilla?.012f:inside ? .035f : .13f, Time.unscaledDeltaTime * .15f);
            if(ambience != null) ambience.volume = Mathf.MoveTowards(ambience.volume, d.InsideVilla?.018f:d.NightIndex == 2 ? .012f : inside ? .045f : .13f, Time.unscaledDeltaTime * .1f);
            if(room)room.volume=Mathf.MoveTowards(room.volume,d.InsideVilla?.065f:0,Time.unscaledDeltaTime*.06f);
            foreach(var pocket in forestPockets)pocket.volume=Mathf.MoveTowards(pocket.volume,inside||d.InsideVilla||d.Horror.Active?0:.045f,Time.unscaledDeltaTime*.025f);
            if(d.Phase==ServicePhase.Playing&&!inside&&!d.Horror.Active&&!d.PaperOpen){nextAtmosphere-=Time.deltaTime;if(nextAtmosphere<0){
                nextAtmosphere=Random.Range(9f,19f);var heading=Quaternion.Euler(0,Random.Range(70f,290f),0)*d.Scene.View.transform.forward;heading.y=0;
                var point=d.Scene.View.transform.position+heading.normalized*Random.Range(8f,17f);float choice=Random.value;
                At(d.InsideVilla?"taps":choice<.6f?"brush":choice<.88f?"rustle":"howl",point,d.InsideVilla?.09f:.16f);
            }}
            if(d.Phase==ServicePhase.Playing&&!inside&&!d.InputBlocked&&Time.time>brushCheck){brushCheck=Time.time+.12f;var at=d.Scene.Walker.transform.position;float moved=Vector3.Distance(at,lastBrushPosition);lastBrushPosition=at;if(moved>.06f&&moved<3&&Time.time>brushCooldown&&ServiceBrushZone.Contact(at,out var contact)){At("brush",contact,.2f);BrushEvents++;brushCooldown=Time.time+.85f;}}
        }
        void At(string name, Vector3 point, float volume)
        {
            AudioClip clip = Pick(name);
            if (clip == null) return;
            AudioSource source = Source("Recorded " + name, transform, 1);
            source.transform.position = point;
            source.clip = clip; source.volume = volume; source.Play();
            Destroy(source.gameObject, clip.length + .1f);
        }
        public void KnockAt(Vector3 position, float volume = .7f) { At("knock", position, volume); }
        public void DoorAt(Vector3 position) { At("door", position, .43f); }
        public string SurfaceAt(Vector3 position){
            var hits=Physics.RaycastAll(position+Vector3.up*.35f,Vector3.down,1.6f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits){if(hit.collider is CharacterController)continue;var surface=hit.collider.GetComponentInParent<ServiceSurface>();if(surface)return surface.Kind;if(hit.collider is TerrainCollider)return "grass";return "stone";}return "grass";
        }
        public void Footstep(Vector3 position) {
            LastSurface=SurfaceAt(position);bool wet=d.Storm&&d.Storm.IsRaining&&!d.Storm.Covered(position);
            LastFootstepPool=wet?(LastSurface=="grass"||LastSurface=="gravel"?"wetmud":LastSurface=="stone"?"wetstone":"wood"):LastSurface;
            At(LastFootstepPool,position,LastSurface=="wood"?.12f:.19f);
        }
        public void Thunder(bool sheltered){At("thunder",d.Scene.View.transform.position+Vector3.up*5,sheltered?.19f:.43f);}
        public void DogAt(Vector3 position, float volume)
        {
            dog.Stop(); dog.transform.position = position; dog.clip = Clip("dog"); dog.volume = volume;
            if (dog.clip != null) dog.Play();
        }
        public void StopDog() { if (dog != null && dog.isPlaying) dog.Stop(); }
        public void Engine(bool on) { if (on && engine.clip != null) engine.Play(); else engine.Stop(); }
        public void EngineSpeed(float speed) { engine.volume = Mathf.Lerp(.12f, .25f, speed / 12); }
        public void Ignition(bool delayed)
        {
            AudioClip clip = Clip(delayed ? "starter" : "start");
            if (clip != null) cabin.PlayOneShot(clip, .4f);
        }
        public void Paper() { AudioClip clip = Clip("paper"); if (clip != null) cabin.PlayOneShot(clip, .2f); }
        public void HorrorAt(string clip,Vector3 position,float volume){At(clip,position,volume);}
        public void SilenceThreat(){foreach(var source in GetComponentsInChildren<AudioSource>())if(source.name=="Recorded breath"||source.name=="Recorded growl"||source.name=="Recorded reveal"||source.name=="Recorded monsterstep"){source.Stop();Destroy(source.gameObject);}}
        public void Pursuit(bool on){pursuing=on;if(on&&tension.clip!=null&&!tension.isPlaying){tension.time=Mathf.Min(12,tension.clip.length*.15f);tension.Play();}}
    }
}
