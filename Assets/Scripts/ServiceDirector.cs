using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ServiceGameV2
{
    public enum ServicePhase { Title, Playing, Paused, Report, Finished }
    public enum ServiceResult { Pending, Served, LeftAtDoor, Unable }
    [Serializable] public sealed class DocketEntry
    {
        public int Property;
        public string Address, Case;
        public ServiceResult Result;
        public DocketEntry(int property, string address, string brief) { Property = property; Address = address; Case = brief; }
    }

    public sealed class ServiceDirector : MonoBehaviour
    {
        public CountyScene Scene { get; private set; }
        public ServicePlayer Player { get; private set; }
        public ServiceAudio Audio { get; private set; }
        public ServiceHUD HUD { get; private set; }
        public ServiceHorror Horror {get;private set;}
        public readonly List<DocketEntry> Docket = new List<DocketEntry>();
        public ServicePhase Phase { get; private set; } = ServicePhase.Title;
        public int NightIndex { get; private set; }
        public int EnvironmentChanges { get; private set; }
        public bool PaperOpen, MapOpen, EndedEarly, OdometerFrozen;
        public float TripMiles { get; private set; }
        public string Notice { get; private set; } = "";
        public string Date => new[] { "OCTOBER 01", "OCTOBER 04", "OCTOBER 09" }[NightIndex];
        public bool Busy { get; private set; }
        public bool IsSmoke { get; private set; }
        public bool InputBlocked => Phase != ServicePhase.Playing || PaperOpen || (Horror!=null&&Horror.Caught);
        public bool AllResolved => Docket.Count > 0 && Docket.TrueForAll(e => e.Result != ServiceResult.Pending);
        public bool CanFinish => (AllResolved || EndedEarly) && !Horror.Active && !Horror.Caught && Player.InCar && Player.Speed < .6f && Vector3.Distance(Scene.Car.position, Scene.Depot.position) < 13;
        readonly float[] linger = new float[6], unseen = new float[6];
        readonly bool[] changed = new bool[6], dogHeard = new bool[6];
        readonly ServiceResult[] lastResults = new ServiceResult[6];
        readonly Quaternion[] doorRest = new Quaternion[6];
        readonly Quaternion[][] curtainRest = new Quaternion[6][];
        Vector3 lastCarPosition;
        Vector3 depotStart;
        Quaternion depotRotation;
        float noticeUntil, nextDog;

        void Start()
        {
            IsSmoke = Array.IndexOf(Environment.GetCommandLineArgs(), "-serviceSmoke") >= 0;
            Scene = GetComponent<CountyScene>();
            if (Scene == null || Scene.Car == null || Scene.View == null || Scene.Properties == null || Scene.Properties.Length < 3)
                throw new InvalidOperationException("Service requires the authored CountyScene references.");
            depotStart = Scene.Car.position;
            depotRotation = Scene.Car.rotation;
            for (int i = 0; i < Scene.Properties.Length; i++)
            {
                ServiceProperty p = Property(i);
                doorRest[i] = p.DoorPanel == null ? Quaternion.identity : p.DoorPanel.localRotation;
                curtainRest[i] = new Quaternion[p.Curtains == null ? 0 : p.Curtains.Length];
                for (int j = 0; j < curtainRest[i].Length; j++) curtainRest[i][j] = p.Curtains[j].transform.localRotation;
            }
            Audio = gameObject.AddComponent<ServiceAudio>();
            Audio.Initialize(this);
            Player = gameObject.AddComponent<ServicePlayer>();
            Player.Initialize(this);
            Horror=gameObject.AddComponent<ServiceHorror>();Horror.Initialize(this);
            HUD = gameObject.AddComponent<ServiceHUD>();
            HUD.Initialize(this);
            ConfigureNight(0);
            Player.EnterCar();
            SetCursor();
            IsSmoke = Array.IndexOf(Environment.GetCommandLineArgs(), "-serviceSmoke") >= 0;
            if (IsSmoke) gameObject.AddComponent<ServiceV5Smoke>().Run(this);
        }

        public ServiceProperty Property(int index)
        {
            foreach (ServiceProperty p in Scene.Properties) if (p.Index == index) return p;
            throw new InvalidOperationException("Missing ServiceProperty " + index);
        }

        void Update()
        {
            if (Scene == null || Player == null) return;
            if (Pressed(Key.Escape))
            {
                if (PaperOpen) { PaperOpen = false; SetCursor(); }
                else if (Phase == ServicePhase.Playing) Pause();
                else if (Phase == ServicePhase.Paused) Resume();
            }
            if (Phase != ServicePhase.Playing) return;
            if (Player.InCar && (Pressed(Key.Tab) || Pressed(Key.M)))
            {
                if (Pressed(Key.M)) { MapOpen = true; PaperOpen = true; }
                else { PaperOpen = !PaperOpen; MapOpen = false; }
                SetCursor();
            }
            float distance = Vector3.Distance(lastCarPosition, Scene.Car.position);
            if (distance < 12 && !OdometerFrozen) TripMiles += distance / 1609.344f;
            lastCarPosition = Scene.Car.position;
            if (NightIndex == 2 && Scene.LateThreshold != null && Vector3.Dot(Scene.Car.position - Scene.LateThreshold.position, Scene.LateThreshold.forward) > 0)
                OdometerFrozen = true;
            if (Scene.Odometer != null) Scene.Odometer.text = TripMiles.ToString("000.0") + " mi";
            if (Time.unscaledTime > noticeUntil) Notice = "";
            if (!PaperOpen && !Horror.Active && !Horror.Caught) EvaluateCues(Time.deltaTime);
            if (InputBlocked || Busy) return;
            if (Pressed(Key.F) && Scene.Flashlight != null) Scene.Flashlight.enabled = !Scene.Flashlight.enabled;
            if (Pressed(Key.E))
            {
                if (CanFinish) TryFinishShift();
                else if (Player.InCar) Player.TryExitCar();
                else if (Vector3.Distance(Scene.View.transform.position, Scene.Car.position + Vector3.up) < 3.5f) Player.EnterCar();
                else { int i = NearbyDoor(); if (i >= 0) Attempt(i, ServiceResult.Served); }
            }
            if (!Player.InCar)
            {
                int door = NearbyDoor();
                if (door >= 0 && Pressed(Key.R)) Attempt(door, ServiceResult.LeftAtDoor);
                int gate = NearbyProperty();
                if (gate >= 0 && Pressed(Key.U)) Attempt(gate, ServiceResult.Unable);
            }
        }

        static bool Pressed(Key key) { return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame; }

        public void BeginShift(int index)
        {
            StopAllCoroutines();
            Time.timeScale = 1;
            Phase = ServicePhase.Playing;
            NightIndex = Mathf.Clamp(index, 0, 2);
            Docket.Clear();
            int[] stops=NightIndex==0?new[]{0,3,1,4,5}:NightIndex==1?new[]{0,3,1,4,5}:new[]{1,2};
            foreach(int i in stops){var property=Property(i);Docket.Add(new DocketEntry(i,property.Address,NightIndex==0?property.Brief+" · "+property.Instructions:Prior(i,property.Brief+" · "+property.Instructions)));}
            EndedEarly = OdometerFrozen = Busy = false;
            TripMiles = 0;
            EnvironmentChanges = 0;
            Notice = "";
            Array.Clear(linger, 0, 6); Array.Clear(unseen, 0, 6);
            Array.Clear(changed, 0, 6); Array.Clear(dogHeard, 0, 6);
            ConfigureNight(NightIndex);
            Horror.ResetEncounter();
            Player.ResetForShift(depotStart, depotRotation);
            lastCarPosition = Scene.Car.position;
            PaperOpen = true; MapOpen = false;
            nextDog = Time.time + 4;
            SetCursor();
        }

        string Prior(int index, string usual) { return lastResults[index] == ServiceResult.Unable ? "Reattempt · Previous visit: unable to serve" : usual; }

        void ConfigureNight(int night)
        {
            if (Scene.LateRoad != null) Scene.LateRoad.SetActive(night == 2);
            if (Scene.Moon != null) { Scene.Moon.intensity = night == 0 ? .22f : night == 1 ? .19f : .16f; Scene.Moon.transform.rotation = Quaternion.Euler(23 - night * 5, -28, 0); }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = night == 0 ? .006f : night == 1 ? .007f : .011f;
            RenderSettings.fogColor = new Color(.065f, .077f, .08f);
            if (Scene.Rain != null)
            {
                if (night == 1) Scene.Rain.Play();
                else Scene.Rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            for (int i = 0; i < Scene.Properties.Length; i++)
            {
                ServiceProperty p = Property(i);
                if (p.PorchLight != null) p.PorchLight.enabled = true;
                if (p.WindowLight != null) p.WindowLight.enabled = i < 2 || night == 0;
                if (p.PostedPaper != null) p.PostedPaper.SetActive(false);
                if (p.DoorPanel != null) p.DoorPanel.localRotation = doorRest[i];
                if (p.AddressLabel != null) p.AddressLabel.gameObject.SetActive(false);
                for (int j = 0; j < curtainRest[i].Length; j++) if (p.Curtains[j] != null) p.Curtains[j].transform.localRotation = curtainRest[i][j];
            }
        }

        void EvaluateCues(float dt)
        {
            if (Player.InCar) { Audio.StopDog(); return; }
            for (int i = 0; i < Scene.Properties.Length; i++)
            {
                ServiceProperty p = Property(i);
                float distance = Vector3.Distance(Scene.View.transform.position, p.Door.position);
                if (distance > 32) continue;
                linger[i] += dt;
                if (NightIndex > 0 && linger[i] > 24) Player.IgnitionDelayPending = true;
                if (i == 0 && distance > 11 && Time.time > nextDog)
                {
                    Audio.DogAt(p.SoundPoint != null ? p.SoundPoint.position : p.Door.position, .31f);
                    nextDog = Time.time + (dogHeard[i] ? 11 : 5.4f);
                    dogHeard[i] = true;
                }
                if (i == 1 && !dogHeard[i] && distance > 11)
                {
                    Audio.DogAt(p.SoundPoint != null ? p.SoundPoint.position : p.Door.position, .25f);
                    dogHeard[i] = true;
                }
                if (distance < 11) Audio.StopDog();
                if (NightIndex == 0)
                {
                    if (i == 0 && linger[i] > 3 && p.Curtains != null)
                        for (int j = 0; j < p.Curtains.Length; j++) if (p.Curtains[j] != null)
                            p.Curtains[j].transform.localRotation = curtainRest[i][j] * Quaternion.Euler(0, Mathf.Sin(Mathf.Min(linger[i] - 3, 2) * Mathf.PI) * 4, 0);
                    continue;
                }
                Vector3 target = p.PorchLight != null ? p.PorchLight.transform.position : p.Door.position + Vector3.up * 2;
                bool looking = Vector3.Dot(Scene.View.transform.forward, (target - Scene.View.transform.position).normalized) > .35f;
                unseen[i] = looking ? 0 : unseen[i] + dt;
                if (!changed[i] && unseen[i] > 3.8f && p.PorchLight != null)
                {
                    p.PorchLight.enabled = !p.PorchLight.enabled;
                    if (p.WindowLight != null) p.WindowLight.enabled = !p.WindowLight.enabled;
                    changed[i] = true;
                    EnvironmentChanges++;
                }
            }
        }

        public void SmokeAdvanceCues(float seconds) { if (IsSmoke) EvaluateCues(seconds); }
        public ServiceResult ResultAt(int index) { DocketEntry entry = Docket.Find(e => e.Property == index); return entry == null ? ServiceResult.Pending : entry.Result; }
        public int NearbyDoor()
        {
            if (Player.InCar) return -1;
            foreach (DocketEntry e in Docket)
                if (e.Result == ServiceResult.Pending && Vector3.Distance(Scene.View.transform.position, Property(e.Property).DeliveryPoint != null ? Property(e.Property).DeliveryPoint.position : Property(e.Property).Door.position + Vector3.up) < (e.Property==1?2.5f:3.1f)) return e.Property;
            return -1;
        }
        public int NearbyProperty()
        {
            if (Player.InCar) return -1;
            foreach (DocketEntry e in Docket)
                if (e.Result == ServiceResult.Pending && (Property(e.Property).InteriorBounds.Contains(Scene.Walker.transform.position+Vector3.up*.3f) || Vector3.Distance(Scene.View.transform.position, Property(e.Property).Gate.position) < 13 || Vector3.Distance(Scene.View.transform.position, Property(e.Property).Door.position) < 18)) return e.Property;
            return -1;
        }

        public void Attempt(int index, ServiceResult result)
        {
            if (Phase != ServicePhase.Playing || Busy || Horror.Active || Horror.Caught || ResultAt(index) != ServiceResult.Pending) return;
            DocketEntry entry = Docket.Find(e => e.Property == index);
            if (entry == null) return;
            ServiceProperty p = Property(index);
            if(p.HasEncounter && result!=ServiceResult.Unable){
                if(NearbyDoor()!=index){Say(p.Instructions);return;}
                entry.Result=ServiceResult.LeftAtDoor;p.PostedPaper.SetActive(true);Audio.Paper();Horror.Begin(index);return;
            }
            if (result == ServiceResult.Served) { StartCoroutine(Knock(entry, p)); return; }
            entry.Result = result;
            if (result == ServiceResult.LeftAtDoor)
            {
                if (p.PostedPaper != null) p.PostedPaper.SetActive(true);
                Audio.Paper();
                Say("Left at door. Docket updated.");
            }
            else Say("Unable to serve. Docket updated.");
        }

        IEnumerator Knock(DocketEntry entry, ServiceProperty p)
        {
            Busy = true;
            Audio.KnockAt(p.Door.position);
            yield return new WaitForSeconds(1.35f);
            if (entry.Property == 0)
            {
                if (p.DoorPanel != null) p.DoorPanel.localRotation = doorRest[0] * Quaternion.Euler(0, 13, 0);
                yield return new WaitForSeconds(.8f);
                entry.Result = ServiceResult.Served;
                if (p.DoorPanel != null) p.DoorPanel.localRotation = doorRest[0];
                Audio.DoorAt(p.Door.position);
                Say("Papers accepted. Docket updated.");
            }
            else if (NightIndex > 0)
            {
                yield return new WaitForSeconds(.75f);
                Audio.KnockAt(p.SoundPoint != null ? p.SoundPoint.position : p.Door.position + p.Door.forward * .6f, .52f);
            }
            else Say("No answer.");
            Busy = false;
        }

        public void Say(string text) { Notice = text; noticeUntil = Time.unscaledTime + 3.2f; }
        public void RetryVilla(){RetryProperty(1);}
        public void RetryProperty(int index){var entry=Docket.Find(e=>e.Property==index);if(entry!=null)entry.Result=ServiceResult.Pending;if(Property(index).PostedPaper)Property(index).PostedPaper.SetActive(false);Busy=false;PaperOpen=false;}
        public void RequestEarlyFinish() { if (Phase == ServicePhase.Playing) { EndedEarly = true; PaperOpen = false; Say("Route closed. Return to the depot."); SetCursor(); } }
        public bool TryFinishShift()
        {
            if (!CanFinish || Busy) return false;
            foreach (DocketEntry e in Docket) lastResults[e.Property] = e.Result;
            SaveRoute();
            Phase = ServicePhase.Report;
            PaperOpen = false;
            Player.StopEngine();
            Audio.StopDog();
            SetCursor();
            return true;
        }
        public void NextShift() { if (NightIndex < 2) BeginShift(NightIndex + 1); else { Phase = ServicePhase.Finished; SetCursor(); } }
        public bool InsideVilla {get {if(Player==null||Player.InCar)return false;return Array.Exists(Scene.Properties,p=>p.InteriorBounds.Contains(Scene.Walker.transform.position+Vector3.up*.3f));}}
        string SavePrefix=>IsSmoke?"SERVICE.test.":"SERVICE.v5.";
        public bool HasSavedRoute=>PlayerPrefs.HasKey(SavePrefix+"night");
        void SaveRoute(){if(NightIndex<2){PlayerPrefs.SetInt(SavePrefix+"night",NightIndex+1);for(int i=0;i<6;i++)PlayerPrefs.SetInt(SavePrefix+"result"+i,(int)lastResults[i]);}else PlayerPrefs.DeleteKey(SavePrefix+"night");PlayerPrefs.Save();}
        public void ContinueRoute(){for(int i=0;i<6;i++)lastResults[i]=(ServiceResult)PlayerPrefs.GetInt(SavePrefix+"result"+i,0);BeginShift(Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix+"night",0),0,2));}
        public void NewRoute(){PlayerPrefs.DeleteKey(SavePrefix+"night");Array.Clear(lastResults,0,lastResults.Length);PlayerPrefs.Save();BeginShift(0);}
        public void SaveOptions(){if(IsSmoke)return;PlayerPrefs.SetFloat("SERVICE.volume",Audio.Volume);PlayerPrefs.SetFloat("SERVICE.sensitivity",Player.Sensitivity);PlayerPrefs.Save();}
        void OnApplicationFocus(bool focused){if(!focused&&!IsSmoke&&Phase==ServicePhase.Playing)Pause();}
        public void Pause() { if (Phase != ServicePhase.Playing) return; Phase = ServicePhase.Paused; Time.timeScale = 0; SetCursor(); }
        public void Resume() { if (Phase != ServicePhase.Paused) return; Phase = ServicePhase.Playing; Time.timeScale = 1; SetCursor(); }
        public void Title() { StopAllCoroutines(); Busy = false; Phase = ServicePhase.Title; PaperOpen = false; Time.timeScale = 1; Player.StopEngine(); Horror.ResetEncounter(); SetCursor(); }
        public void SetCursor() { bool locked = Phase == ServicePhase.Playing && !PaperOpen && !IsSmoke; Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnDestroy() { if(Audio&&Player)SaveOptions();AudioListener.pause=false;Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}






