using System.Collections.Generic;
using UnityEngine;

namespace ServiceGameV2
{
    public sealed class ServiceAudio : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        ServiceDirector d;
        AudioSource engine, dog, ambience, cabin, wind, tension;
        bool pursuing;
        public float Volume = .8f;
        public void Initialize(ServiceDirector director)
        {
            d = director;
            tension=Source("Horror pursuit score",d.Scene.View.transform,0);tension.clip=Clip("tension");tension.loop=true;tension.volume=0;
            engine = Source("Recorded engine", d.Scene.Car, 0);
            dog = Source("Recorded distant dog", transform, 1);
            ambience = Source("Recorded rural ambience", d.Scene.View.transform, 0);
            cabin = Source("Recorded cabin sounds", d.Scene.View.transform, 0);
            wind = Source("Recorded wind through trees", d.Scene.View.transform, 0);
            wind.clip = Clip("wind"); wind.loop = true; wind.volume = .09f;
            if (wind.clip != null) wind.Play();
            engine.clip = Clip("engine"); engine.loop = true; engine.volume = .17f;
            ambience.clip = Clip("night"); ambience.loop = true; ambience.volume = .065f;
            if (ambience.clip != null) ambience.Play();
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
        void Update()
        {
            AudioListener.volume = Volume;
            if(tension!=null){tension.volume=Mathf.MoveTowards(tension.volume,pursuing?.28f:0,Time.unscaledDeltaTime*.2f);if(!pursuing&&tension.volume<=0&&tension.isPlaying)tension.Stop();}
            bool inside = d.Player == null || d.Player.InCar;
            if(wind != null) wind.volume = Mathf.MoveTowards(wind.volume, inside ? .035f : .13f, Time.unscaledDeltaTime * .15f);
            if(ambience != null) ambience.volume = Mathf.MoveTowards(ambience.volume, d.NightIndex == 2 ? .012f : inside ? .045f : .13f, Time.unscaledDeltaTime * .1f);
        }
        void At(string name, Vector3 point, float volume)
        {
            AudioClip clip = Clip(name);
            if (clip == null) return;
            AudioSource source = Source("Recorded " + name, transform, 1);
            source.transform.position = point;
            source.clip = clip; source.volume = volume; source.Play();
            Destroy(source.gameObject, clip.length + .1f);
        }
        public void KnockAt(Vector3 position, float volume = .7f) { At("knock", position, volume); }
        public void DoorAt(Vector3 position) { At("door", position, .43f); }
        public void Footstep(Vector3 position) { At("footstep", position, .18f); }
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
        public void Pursuit(bool on){pursuing=on;if(on&&tension.clip!=null&&!tension.isPlaying)tension.Play();}
    }
}
