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
        public ServiceStorm Storm {get;private set;}
        public ServicePresentation Presentation {get;private set;}
        public readonly List<DocketEntry> Docket = new List<DocketEntry>();
        public ServicePhase Phase { get; private set; } = ServicePhase.Title;
        public int NightIndex { get; private set; }
        public int EnvironmentChanges { get; private set; }
        public bool PaperOpen, MapOpen, EndedEarly, OdometerFrozen;
        public float TripMiles { get; private set; }
        public string Notice { get; private set; } = "";
        public string Date => new[] { "OCTOBER 01", "OCTOBER 04", "OCTOBER 09" }[NightIndex];
        public string LongDate => new[] { "Thursday, October 1st, 1998", "Sunday, October 4th, 1998", "Friday, October 9th, 1998" }[NightIndex];
        public string ShiftTime => new[] { "9:48 PM", "10:21 PM", "11:57 PM" }[NightIndex];
        float shiftStartedAt = -99;
        public float ShiftCardTime => Time.unscaledTime - shiftStartedAt;
        public bool Busy { get; private set; }
        public ServiceVehiclePresentation Vehicle {get;private set;}
        public ServiceLife Life {get;private set;}
        public ServiceDialogue Dialogue {get;private set;}
        public ServiceStalker Stalker {get;private set;}
        public int HumanVariant {get;private set;}=-1;
        public bool BellGone;
        EncounterKind[] baseKind; int[] baseVariant; readonly bool[] arrived = new bool[6];
        public int KnockCount {get;private set;}
        public bool IsFriendly(int index)=>index==0||(index==4&&NightIndex==0);
        public bool IsSmoke { get; private set; }
        public bool InputBlocked => Phase != ServicePhase.Playing || PaperOpen || (Horror!=null&&(Horror.Caught||Horror.ForcedLook));
        public bool AllResolved => Docket.Count > 0 && Docket.TrueForAll(e => e.Result != ServiceResult.Pending);
        public bool CanFinish => (AllResolved || EndedEarly) && !Horror.Active && !Horror.Caught && Player.InCar && Player.Speed < .6f && ServiceInteraction.InDepot(Scene.Car.position, Scene.Depot.position);
        readonly bool[] noticeRead = new bool[6];
        public bool NoticeRead(int index)=>index>=0&&index<6&&noticeRead[index];
        public string VisitNotes(int index)=>NoticeRead(index)?Property(index).Instructions:"Visit the address. Speak to the occupant or check for a posted notice.";
        readonly bool[] accessGranted = new bool[6];
        public bool AccessGranted(int index)=>index>=0&&index<accessGranted.Length&&accessGranted[index];
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
            Dialogue=gameObject.AddComponent<ServiceDialogue>();Dialogue.Initialize(this);
            baseKind=new EncounterKind[Scene.Properties.Length];baseVariant=new int[Scene.Properties.Length];for(int i=0;i<Scene.Properties.Length;i++){baseKind[i]=Scene.Properties[i].Encounter;baseVariant[i]=Scene.Properties[i].CreatureVariant;}
            if(Scene.EntityVariants!=null)HumanVariant=Array.FindIndex(Scene.EntityVariants,v=>v&&v.name.StartsWith("The man"));
            ConfigureNight(0);
            Player.EnterCar();
            Storm=gameObject.AddComponent<ServiceStorm>();Storm.Initialize(this);
            Presentation=gameObject.AddComponent<ServicePresentation>();Presentation.Initialize(this);
            Vehicle=gameObject.AddComponent<ServiceVehiclePresentation>();Vehicle.Initialize(this);
            Life=gameObject.AddComponent<ServiceLife>();Life.Initialize(this);
            Stalker=gameObject.AddComponent<ServiceStalker>();Stalker.Initialize(this,Scene.StalkerFigure);
            SetCursor();
            IsSmoke = Array.IndexOf(Environment.GetCommandLineArgs(), "-serviceSmoke") >= 0;
            if(IsSmoke){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-serviceV16")>=0)gameObject.AddComponent<ServiceV16Smoke>().Run(this);else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-serviceVisual")>=0)gameObject.AddComponent<ServiceV15VisualSmoke>().Run(this);else gameObject.AddComponent<ServiceV5Smoke>().Run(this);}
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
                if(HUD&&HUD.Back())return;
                if (PaperOpen) { PaperOpen = false; SetCursor(); }
                else if (Phase == ServicePhase.Playing) Pause();
                else if (Phase == ServicePhase.Paused) Resume();
            }
            if (Phase != ServicePhase.Playing) return;
            if (Player.InCar && (Pressed(Key.Tab) || Pressed(Key.M)))
            {
                ToggleDocument(Pressed(Key.M));
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
            if (Pressed(Key.F) && Scene.Flashlight != null && !Player.InCar) Scene.Flashlight.enabled = !Scene.Flashlight.enabled;
            // E also looks over the right shoulder while sprinting, so reaching the car must not depend on releasing Shift first.
            bool nearCar = !Player.InCar && Vector3.Distance(Scene.View.transform.position, Scene.Car.position + Vector3.up) < 3.5f;
            if (Pressed(Key.E) && nearCar) Player.EnterCar();
            else if (Pressed(Key.E)&&!Player.InteractionSuppressed)
            {
                if (CanFinish) TryFinishShift();
                else if (Player.InCar) Player.TryExitCar();
                else if(NearbyNotice()>=0)ReadNotice(NearbyNotice());
                else { int front=NearbyKnockDoor();if(front>=0)Attempt(front,ServiceResult.Served);else {int i=NearbyDoor();if(i>=0)Attempt(i,ServiceResult.LeftAtDoor);} }
            }
            if (!Player.InCar)
            {
                int door = NearbyDoor();
                if (door >= 0 && Pressed(Key.R)) Attempt(door, ServiceResult.LeftAtDoor);
                int gate = NearbyProperty();
                if (gate >= 0 && Pressed(Key.U)) Attempt(gate, ServiceResult.Unable);
            }
        }

        public void ToggleDocument(bool map){if(Horror.Active||Horror.Caught){Say("No time for the map.");return;}if(map){bool close=PaperOpen&&MapOpen;MapOpen=PaperOpen=!close;}else{PaperOpen=!PaperOpen||MapOpen;MapOpen=false;}if(PaperOpen)Player.HoldVehicle();Audio.SyncVehicleAudio();SetCursor();}
        static bool Pressed(Key key) { return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame; }

        public void BeginShift(int index)
        {
            StopAllCoroutines();
            Time.timeScale = 1;
            Phase = ServicePhase.Playing;
            NightIndex = Mathf.Clamp(index, 0, 2);
            Docket.Clear();
            int[] stops=NightIndex==0?new[]{0,3,1,4,5}:NightIndex==1?new[]{0,3,1,4,5}:new[]{1,2};
            foreach(int i in stops){var property=Property(i);Docket.Add(new DocketEntry(i,property.Address,NightIndex==0?property.Brief:Prior(i,property.Brief)));}
            EndedEarly = OdometerFrozen = Busy = false;
            TripMiles = 0;
            EnvironmentChanges = 0;
            Notice = "";
            Array.Clear(noticeRead,0,6); Array.Clear(accessGranted,0,6); Array.Clear(linger, 0, 6); Array.Clear(unseen, 0, 6);
            Array.Clear(changed, 0, 6); Array.Clear(dogHeard, 0, 6); Array.Clear(arrived, 0, 6); BellGone = false;
            if(Dialogue)Dialogue.Cancel(); if(Stalker)Stalker.ResetForShift();
            ConfigureNight(NightIndex);if(Life)Life.ResetForShift();
            Horror.ResetEncounter();
            Player.ResetForShift(depotStart, depotRotation);
            lastCarPosition = Scene.Car.position;
            PaperOpen = true; MapOpen = false;
            nextDog = Time.time + 4;
            shiftStartedAt = IsSmoke ? -99 : Time.unscaledTime;
            SetCursor();
        }

        string Prior(int index, string usual) { return lastResults[index] == ServiceResult.Unable ? "Reattempt · Previous visit: unable to serve" : usual; }

        void ConfigureNight(int night)
        {
            if (Scene.LateRoad != null) Scene.LateRoad.SetActive(night == 2);
            ServiceForestMood.Apply(Scene,night);
            if (Scene.Rain != null)
            {
                Scene.Rain.Play();
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
            Schedule(night);
        }

        // V18: night one is ordinary work and Bell (the man) is the only threat. Night two mixes the man with the creatures; night three is the creatures.
        void Schedule(int night)
        {
            if (baseKind == null) return;
            for (int i = 0; i < Scene.Properties.Length; i++)
            {
                var p = Scene.Properties[i]; p.Encounter = baseKind[i]; p.CreatureVariant = baseVariant[i];
                if (HumanVariant < 0) continue;
                if (night == 0) { if (p.Index == 1 || p.Index == 3 || p.Index == 5) p.Encounter = EncounterKind.None; if (p.Index == 4) p.CreatureVariant = HumanVariant; }
                else if (night == 1 && (p.Index == 1 || p.Index == 5)) p.CreatureVariant = HumanVariant;
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
                if (distance < 16 && !arrived[i] && Docket.Exists(e => e.Property == p.Index && e.Result == ServiceResult.Pending)) { arrived[i] = true; var line = ServiceScript.Arrival(p.Index, NightIndex); if (line != null && string.IsNullOrEmpty(Notice)) Say(line); }
                if (NightIndex > 0 && linger[i] > 24) Player.IgnitionDelayPending = true;
                if (i == 0 && distance > 11 && Time.time > nextDog)
                {
                    if(Life)Life.Bark();
                    nextDog = Time.time + (dogHeard[i] ? 11 : 5.4f);
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
        public int NearbyNotice(){
            if(Player.InCar||Busy||Horror.Active)return -1;
            foreach(var entry in Docket){var p=Property(entry.Property);if(entry.Result==ServiceResult.Pending&&p.NoticePoint&&ServiceInteraction.Reachable(Scene.View,p.NoticePoint.position,2.7f,p.NoticePoint.parent))return p.Index;}
            return -1;
        }
        public void ReadNotice(int index){if(NearbyNotice()!=index)return;noticeRead[index]=true;Say(Property(index).NoticeText);noticeUntil=Time.unscaledTime+9;Audio.Paper();}
        public int NearbyKnockDoor(){
            if(Player.InCar)return -1;
            foreach(var entry in Docket){var p=Property(entry.Property);if(entry.Result==ServiceResult.Pending&&!AccessGranted(p.Index)&&p.DoorPanel&&p.KnockPoint&&ServiceInteraction.Reachable(Scene.View,p.KnockPoint.position,2.4f,p.DoorPanel))return p.Index;}
            return -1;
        }
        public int NearbyDoor(){
            if(Player.InCar)return -1;
            foreach(var e in Docket){var p=Property(e.Property);if(e.Result==ServiceResult.Pending&&AccessGranted(e.Property)&&!IsFriendly(e.Property)&&p.DeliveryPoint&&ServiceInteraction.Reachable(Scene.View,p.DeliveryPoint.position+Vector3.up*.09f,2.25f,null))return e.Property;}
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
            if(result==ServiceResult.Served){if(NearbyKnockDoor()==index)StartCoroutine(Knock(entry,p));return;}
            if(result==ServiceResult.LeftAtDoor&&(!AccessGranted(index)||IsFriendly(index))){Say("Knock at the front door first.");return;}
            if(p.HasEncounter && !IsFriendly(index) && result!=ServiceResult.Unable){
                if(NearbyDoor()!=index)return;
                entry.Result=ServiceResult.LeftAtDoor;if(p.PostedPaper)p.PostedPaper.SetActive(true);Audio.Paper();var hands=Scene.View.GetComponentInChildren<ServiceHands>();if(hands)hands.Interact(false);Horror.Begin(index);return;
            }
            entry.Result = result;
            if (result == ServiceResult.LeftAtDoor)
            {
                if (p.PostedPaper != null) p.PostedPaper.SetActive(true);
                Audio.Paper();
                if (!p.HasEncounter && !IsFriendly(index) && NightIndex > 0 && !string.IsNullOrEmpty(p.RevealLine))
                {
                    Audio.KnockAt(p.SoundPoint != null ? p.SoundPoint.position : p.Door.position, .35f);
                    Say(p.RevealLine);
                }
                else { Audio.DeliveryComplete(); Say(NightIndex == 0 && !IsFriendly(index) ? ServiceScript.QuietDelivery(index) : "Notice left."); }
            }
            else Say("No contact. Visit recorded.");
        }

        IEnumerator Knock(DocketEntry entry, ServiceProperty p)
        {
            Busy = true;
            KnockCount++;
            var hands=Scene.View.GetComponentInChildren<ServiceHands>();if(hands)hands.Interact(true);
            Audio.KnockAt(p.Door.position);
            yield return new WaitForSeconds(1.35f);
            accessGranted[p.Index]=true;
            if (IsFriendly(entry.Property))
            {
                if(p.WindowLight)p.WindowLight.enabled=true;
                Audio.DoorAt(p.Door.position);
                Say("A floorboard creaks beyond the door.");
                if(Life)Life.OpenDoor(p,true);
                yield return new WaitForSeconds(1.1f);
                var talk=ServiceScript.Doorstep(entry.Property,NightIndex);
                if(talk.HasValue&&Dialogue)yield return Dialogue.Run(talk.Value.speaker,talk.Value.steps);
                entry.Result = ServiceResult.Served;
                if(hands)hands.Interact(false);
                Audio.Paper();
                if(entry.Property==0)Audio.DeliveryComplete();
                if(entry.Property==4&&NightIndex==0)Horror.ArmReturnAmbush();
                if(Life)Life.OpenDoor(p,false);
                Audio.DoorAt(p.Door.position);
                Say(entry.Property==0?"He takes the envelope and shuts the door.":"He takes it without looking at it. The door stays open a moment longer than it should.");
            }
            else if (NightIndex > 0)
            {
                yield return new WaitForSeconds(.75f);
                Audio.KnockAt(p.SoundPoint != null ? p.SoundPoint.position : p.Door.position + p.Door.forward * .6f, .52f);
            }
            else Say("No answer.");
            if(!IsFriendly(entry.Property)&&p.DoorPanel){if(Life)Life.OpenDoor(p,true);Say(NoticeRead(p.Index)?"The latch gives. "+p.Instructions:"The latch gives. A note has been left by the entrance.");}
            Busy = false;
        }

        public void Say(string text) { Notice = text; noticeUntil = Time.unscaledTime + 3.2f; }
        public void RetryVilla(){RetryProperty(1);}
        public void RetryProperty(int index){var entry=Docket.Find(e=>e.Property==index);if(entry!=null)entry.Result=IsFriendly(index)?ServiceResult.Served:ServiceResult.Pending;if(Property(index).PostedPaper)Property(index).PostedPaper.SetActive(false);Busy=false;PaperOpen=MapOpen=false;accessGranted[index]=true;if(Life)Life.OpenDoor(Property(index),true);Player.IgnitionDelayPending=false;}
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
        public void SaveOptions(){if(IsSmoke)return;PlayerPrefs.SetFloat("SERVICE.volume",Audio.Volume);PlayerPrefs.SetFloat("SERVICE.music",Audio.MusicVolume);PlayerPrefs.SetFloat("SERVICE.sensitivity",Player.Sensitivity);PlayerPrefs.SetFloat("SERVICE.motion",Player.CameraMotion);PlayerPrefs.SetFloat("SERVICE.blur",Player.MotionBlurAmount);if(Storm)Storm.Save();PlayerPrefs.Save();}
        void OnApplicationFocus(bool focused){if(!focused&&!IsSmoke&&Phase==ServicePhase.Playing)Pause();}
        public void Pause() { if (Phase != ServicePhase.Playing) return; Phase = ServicePhase.Paused; Time.timeScale = 0; SetCursor(); }
        public void Resume() { if (Phase != ServicePhase.Paused) return; Phase = ServicePhase.Playing; Time.timeScale = 1; SetCursor(); }
        public void Title() { StopAllCoroutines(); Busy = false; if(Dialogue)Dialogue.Cancel(); Phase = ServicePhase.Title; PaperOpen = MapOpen = false; Time.timeScale = 1; Player.StopEngine(); Horror.ResetEncounter(); SetCursor(); }
        public void SetCursor() { bool locked = Phase == ServicePhase.Playing && !PaperOpen && !IsSmoke; Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnDestroy() { if(Audio&&Player)SaveOptions();AudioListener.pause=false;Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}








