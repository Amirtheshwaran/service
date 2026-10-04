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
        // V19: the handwritten note currently held up to read (property index), or -1.
        public int NoteOpen { get; private set; } = -1;
        public string Date => new[] { "OCTOBER 01", "OCTOBER 04", "OCTOBER 09" }[NightIndex];
        public string LongDate => new[] { "Thursday, October 1st, 1998", "Sunday, October 4th, 1998", "Friday, October 9th, 1998" }[NightIndex];
        public string ShiftTime => new[] { "9:48 PM", "10:21 PM", "11:57 PM" }[NightIndex];
        float shiftStartedAt = -99;
        public float ShiftCardTime => Time.unscaledTime - shiftStartedAt;
        public bool Busy { get; private set; }
        public ServiceVehiclePresentation Vehicle {get;private set;}
        public ServiceLife Life {get;private set;}
        public ServiceDread Dread {get;private set;}
        public ServiceTimecard Timecard {get;private set;}
        public ServiceFoliage Foliage {get;private set;}
        public ServiceRouteGuide Guide {get;private set;}
        public ServiceOmens Omens {get;private set;}
        public ServiceDialogue Dialogue {get;private set;}
        public ServiceCamcorder Camcorder {get;private set;}
        public bool BellGone;
        readonly bool[] arrived = new bool[6], inside = new bool[6];
        bool dogLine, lightsLine, docketLine, barricadeLine, surveyLine; float depotIntroAt = -1;
        public int KnockCount {get;private set;}
        public bool IsFriendly(int index)=>index==0||(index==4&&NightIndex==0);
        // V20 slow burn: night one has no monsters. Night two: Harrow's watcher, the Morrow chase, and Bell's door on the
        // walk back (armed when the papers are left in his back room). Night three: Vale.
        public bool EncounterTonight(int index){switch(index){case 3:return NightIndex==1;case 5:return NightIndex==1;case 1:return NightIndex==2;case 4:return false;default:return Property(index).HasEncounter;}}
        public bool IsSmoke { get; private set; }
        public bool IsTour { get; private set; }
        public bool IsChaos { get; private set; }
        public bool InputBlocked => Phase != ServicePhase.Playing || PaperOpen || (Horror!=null&&(Horror.Caught||Horror.ForcedLook)) || (Timecard!=null&&Timecard.Blocking);
        public bool AllResolved => Docket.Count > 0 && Docket.TrueForAll(e => e.Result != ServiceResult.Pending);
        public bool CanFinish => (AllResolved || EndedEarly) && !Horror.Active && !Horror.Caught && Player.InCar && Player.Speed < .6f && ServiceInteraction.InDepot(Scene.Car.position, Scene.Depot.position);
        readonly bool[] noticeRead = new bool[6];
        public bool NoticeRead(int index)=>index>=0&&index<6&&noticeRead[index];
        public string VisitNotes(int index)=>NoticeRead(index)?Property(index).Instructions:"Visit the address. Speak to the occupant or check for a posted notice.";
        readonly bool[] accessGranted = new bool[6];
        readonly bool[] shutByPlayer = new bool[6]; // V22: doors the player pulled shut (they may open them again)
        public bool ShutByPlayer(int i)=>i>=0&&i<6&&shutByPlayer[i];
        public int DoorsShut {get;private set;}
        public bool AccessGranted(int index)=>index>=0&&index<accessGranted.Length&&accessGranted[index];
        readonly float[] linger = new float[6], unseen = new float[6]; bool stallArmed;
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
            Camcorder=gameObject.AddComponent<ServiceCamcorder>();
            ApplyScriptText(0);
            ConfigureNight(0);
            Player.EnterCar();
            Storm=gameObject.AddComponent<ServiceStorm>();Storm.Initialize(this);
            Presentation=gameObject.AddComponent<ServicePresentation>();Presentation.Initialize(this);Guide=gameObject.AddComponent<ServiceRouteGuide>();Guide.Initialize(this);Omens=gameObject.AddComponent<ServiceOmens>();Omens.Initialize(this);Dread=gameObject.AddComponent<ServiceDread>();Dread.Initialize(this);Timecard=gameObject.AddComponent<ServiceTimecard>();Timecard.Initialize(this);Foliage=gameObject.AddComponent<ServiceFoliage>();Foliage.Initialize(this);gameObject.AddComponent<ServiceWorldEdge>().Initialize(this);
            Vehicle=gameObject.AddComponent<ServiceVehiclePresentation>();Vehicle.Initialize(this);
            Life=gameObject.AddComponent<ServiceLife>();Life.Initialize(this);
            SetCursor();
            IsSmoke = Array.IndexOf(Environment.GetCommandLineArgs(), "-serviceSmoke") >= 0;
            IsChaos = !IsSmoke && Array.IndexOf(Environment.GetCommandLineArgs(), "-serviceChaos") >= 0;if(IsChaos)gameObject.AddComponent<ServiceV20Chaos>().Run(this);
            if(IsSmoke){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-serviceSurvey")>=0)gameObject.AddComponent<ServiceV19Survey>().Run(this);else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-serviceTour")>=0){IsTour=true;gameObject.AddComponent<ServiceV19Tour>().Run(this);}else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-serviceV19")>=0)gameObject.AddComponent<ServiceV19Smoke>().Run(this);else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-serviceV16")>=0)gameObject.AddComponent<ServiceV16Smoke>().Run(this);else if(Array.IndexOf(Environment.GetCommandLineArgs(),"-serviceVisual")>=0)gameObject.AddComponent<ServiceV15VisualSmoke>().Run(this);else gameObject.AddComponent<ServiceV5Smoke>().Run(this);}
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
                // V21: a paused game resumes first (to the clipboard if it was open), then Esc closes the clipboard.
                if (Phase == ServicePhase.Paused) Resume();
                else if (NoteOpen >= 0) { NoteOpen = -1; Audio.Paper(); } // V22: Esc puts a held note down first
                else if (PaperOpen) { PaperOpen = false; SetCursor(); }
                else if (Phase == ServicePhase.Playing) Pause();
            }
            if (Phase != ServicePhase.Playing) return;
            // V20: no drawn map. Tab takes up the clipboard, but only with the car stopped.
            if (Player.InCar && Pressed(Key.Tab))
            {
                if (!PaperOpen && Player.Speed > 1.5f) Say(ServiceScript.NotWhileDriving);
                else ToggleDocument(false);
            }
            float distance = Vector3.Distance(lastCarPosition, Scene.Car.position);
            if (distance < 12 && !OdometerFrozen) TripMiles += distance / 1609.344f;
            lastCarPosition = Scene.Car.position;
            if (NightIndex == 2 && Scene.LateThreshold != null && Vector3.Dot(Scene.Car.position - Scene.LateThreshold.position, Scene.LateThreshold.forward) > 0)
                OdometerFrozen = true;
            ShiftLines();
            if (Scene.Odometer != null) Scene.Odometer.text = TripMiles.ToString("000.0") + " mi";
            if (Time.unscaledTime > noticeUntil) Notice = "";
            if (NoteOpen >= 0) { var np = Property(NoteOpen); if (Player.InCar || Horror.Active || Horror.Caught || np.NoticePoint == null || Vector3.Distance(Scene.View.transform.position, np.NoticePoint.position) > 3.2f) NoteOpen = -1; }
            if (!PaperOpen && !Horror.Active && !Horror.Caught) EvaluateCues(Time.deltaTime);
            if (InputBlocked || Busy) return;
            if (Pressed(Key.F)) ToggleTorch();
            // E also looks over the right shoulder while sprinting, so reaching the car must not depend on releasing Shift first.
            bool nearCar = CanEnterCar;
            if (Pressed(Key.E) && nearCar) Player.EnterCar();
            else if (Pressed(Key.E)&&!Player.InteractionSuppressed)
            {
                if (CanFinish) TryFinishShift();
                else if (Player.InCar) Player.TryExitCar();
                else if(NoteOpen>=0){NoteOpen=-1;Audio.Paper();}
                else if(NearbyNotice()>=0)ReadNotice(NearbyNotice());
                else { int front=NearbyKnockDoor();if(front>=0)Attempt(front,ServiceResult.Served);else {int i=NearbyDoor();if(i>=0)Attempt(i,ServiceResult.LeftAtDoor);else{int leaf=NearbyLeaf();if(leaf>=0)ToggleDoor(leaf);}} }
            }
            if (!Player.InCar)
            {
                int door = NearbyDoor();
                if (door >= 0 && Pressed(Key.R)) Attempt(door, ServiceResult.LeftAtDoor);
                int gate = NearbyProperty();
                if (gate >= 0 && Pressed(Key.U)) Attempt(gate, ServiceResult.Unable);
            }
        }

        // V21: the car is entered from outside, with a clear line to it; a door, note or table being looked at wins.
        public bool CanEnterCar=>!Player.InCar&&!InsideVilla&&Vector3.Distance(Scene.View.transform.position,Scene.Car.position+Vector3.up)<3.5f&&ServiceInteraction.Clear(Scene.View.transform.position,Scene.Car.position+Vector3.up*.9f,Scene.Car,Scene.Walker.transform)&&NoteOpen<0&&NearbyNotice()<0&&NearbyKnockDoor()<0&&NearbyDoor()<0&&NearbyLeaf()<0;
        // V22: the door of a house you let yourself into, from the step outside: shut it behind you, or open it again.
        // Only at non-friendly doors (residents close their own), never while busy, reading, in an encounter or a car.
        public int NearbyLeaf(){
            if(Player.InCar||Busy||Horror.Active||Horror.Caught||NoteOpen>=0||!Life||(Dialogue&&Dialogue.Active))return -1;
            var w=Scene.Walker.transform.position;
            foreach(var e in Docket){var p=Property(e.Property);int i=p.Index;
                if(!p.DoorPanel||IsFriendly(i)||!AccessGranted(i)||Life.DoorMoving(i))continue;
                bool open=Life.DoorOpenDegrees(i)>25;if(!open&&!shutByPlayer[i])continue;
                // closing an open leaf needs you clear of its sweep; a shut leaf (they all swing inward) opens from right against it
                float step=Vector3.Dot(w-p.OpeningCentre,-p.Inward);if(step<(open?.55f:0f)||step>2.6f)continue;
                var flat=w-p.OpeningCentre;flat.y=0;if(flat.magnitude>3.2f)continue;
                var eye=Scene.View.transform.position;var aimAt=p.OpeningCentre+Vector3.up*Mathf.Clamp(eye.y-p.OpeningCentre.y,.9f,1.6f);
                if(!ServiceInteraction.Reachable(Scene.View,aimAt,3.2f,p.DoorPanel))continue;
                return i;}
            return -1;}
        public bool ToggleDoor(int i){if(NearbyLeaf()!=i)return false;StartCoroutine(SwingDoor(i));return true;}
        IEnumerator SwingDoor(int i){
            Busy=true;var p=Property(i);bool close=Life.DoorOpenDegrees(i)>25;
            var hands=Scene.View.GetComponentInChildren<ServiceHands>();if(hands)hands.Play("Push");
            if(hands&&hands.Visible)yield return new WaitForSeconds(hands.PushLead);
            shutByPlayer[i]=close;if(close)DoorsShut++;if(Horror)Horror.DoorChanged(i,!close);
            if(!close)Audio.DoorAt(p.Door.position);
            Life.OpenDoor(p,!close,false,close?.9f:.8f);
            yield return new WaitForSeconds(close?.9f:.8f);
            if(close)Audio.DoorAt(p.Door.position);
            Busy=false;}
        public void ToggleTorch(){ if (Scene.Flashlight == null || Player.InCar) return; Scene.Flashlight.enabled = !Scene.Flashlight.enabled; Gesture("Torch"); var click = Resources.Load<AudioClip>("Audio/V19/torch_click"); if (click) AudioSource.PlayClipAtPoint(click, Scene.View.transform.position, .45f); }
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
            NoteOpen=-1;Array.Clear(noticeRead,0,6); Array.Clear(accessGranted,0,6); Array.Clear(shutByPlayer,0,6); Array.Clear(linger, 0, 6); stallArmed = false; Array.Clear(unseen, 0, 6);
            Array.Clear(changed, 0, 6); Array.Clear(dogHeard, 0, 6); Array.Clear(arrived, 0, 6); Array.Clear(inside, 0, 6); BellGone = false;
            dogLine = lightsLine = docketLine = barricadeLine = surveyLine = false; depotIntroAt = Time.unscaledTime + (IsSmoke && !IsTour ? 0 : 8.4f);
            if(Dialogue)Dialogue.Cancel();
            ResetResidents();
            ApplyScriptText(NightIndex);
            ConfigureNight(NightIndex);if(Life)Life.ResetForShift();if(Guide)Guide.ResetForShift();if(Omens)Omens.ResetForShift();if(Dread)Dread.ResetForShift();
            Horror.ResetEncounter();
            Player.ResetForShift(depotStart, depotRotation);
            lastCarPosition = Scene.Car.position;
            PaperOpen = true; MapOpen = false;
            nextDog = Time.time + 4;
            shiftStartedAt = IsSmoke && !IsTour ? -99 : Time.unscaledTime;
            // V21: the night opens on a typed time card (date, then the time) over black.
            if(Timecard){Timecard.Clear();if(!(IsSmoke&&!IsTour))Timecard.NightStart();}
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
        }

        // V19: all narrative text comes from ServiceScript (Sources/V19/script.json).
        void ApplyScriptText(int night)
        {
            foreach (var p in Scene.Properties)
            {
                var t = ServiceScript.For(p.Index); if (t == null) continue;
                p.Brief = t.Docket; p.Instructions = t.Instructions; p.RevealLine = t.Reveal;
                if (!string.IsNullOrEmpty(t.Death)) p.DeathLine = t.Death;
                p.NoticeText = ServiceScript.Note(p.Index, night);
                if (p.NoticePoint != null && p.NoticePoint.name == "Door note") p.NoticePoint.gameObject.SetActive(!string.IsNullOrEmpty(p.NoticeText));
            }
        }

        // Inner voice at the depot, at the barricade and past the survey line, and once the docket is done.
        void ShiftLines()
        {
            if (depotIntroAt > 0 && Time.unscaledTime > depotIntroAt && !PaperOpen) { depotIntroAt = -1; Say(ServiceScript.DepotIntro[NightIndex]); }
            if (NightIndex == 2 && !barricadeLine && Scene.LateThreshold != null && Player.InCar && Vector3.Distance(Scene.Car.position, Scene.LateThreshold.position) < 38) { barricadeLine = true; Say(ServiceScript.BarricadeGone); }
            if (OdometerFrozen && !surveyLine) { surveyLine = true; Say(ServiceScript.PastTheSurvey); }
            if (AllResolved && !docketLine && !Horror.Active && !Horror.Caught && !Busy && string.IsNullOrEmpty(Notice)) { docketLine = true; Say(ServiceScript.AllVisitsRecorded); }
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
                bool pending = Docket.Exists(e => e.Property == p.Index && e.Result == ServiceResult.Pending);
                if (distance < 16 && !arrived[i] && pending) { arrived[i] = true; var line = ServiceScript.Arrival(p.Index, NightIndex); if (line != null && string.IsNullOrEmpty(Notice)) Say(line); }
                if (!inside[i] && pending && p.InteriorBounds.Contains(Scene.Walker.transform.position + Vector3.up * .3f) && Vector3.Dot(Scene.Walker.transform.position - p.Door.position, Outward(p)) < -0.8f) { inside[i] = true; var t = ServiceScript.For(p.Index); if (t != null && !string.IsNullOrEmpty(t.Inside)) Say(t.Inside); if (p.Index == 2) StartCoroutine(Later(7.5f, ServiceScript.ParcelInside)); }
                if (NightIndex > 0 && !stallArmed && !IsFriendly(p.Index) && !(p.Index == 4 && NightIndex == 1) && linger[i] > 24) { stallArmed = true; Player.IgnitionDelayPending = true; }
                if (i == 0 && distance > 11 && Time.time > nextDog && !(Life && Life.DogBusy))
                {
                    if(Life)Life.Bark();
                    SayDogLine();
                    nextDog = Time.time + (dogHeard[i] ? 11 : 5.4f);
                    dogHeard[i] = true;
                }
                if (distance < 11 && !(Life && Life.DogBusy)) Audio.StopDog();
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
                // V21: only out in the yard (the light is the porch's) and never in the middle of an action or a line
                if (!changed[i] && unseen[i] > 3.8f && p.PorchLight != null && !Busy && !p.InteriorBounds.Contains(Scene.Walker.transform.position + Vector3.up * .3f))
                {
                    p.PorchLight.enabled = !p.PorchLight.enabled;
                    if (p.WindowLight != null) p.WindowLight.enabled = !p.WindowLight.enabled;
                    changed[i] = true;
                    EnvironmentChanges++;
                    if (!lightsLine && distance < 26 && string.IsNullOrEmpty(Notice)) { lightsLine = true; Say(ServiceScript.LightsChanged); }
                }
            }
        }

        public void SmokeAdvanceCues(float seconds) { if (IsSmoke) EvaluateCues(seconds); }
        public ServiceResult ResultAt(int index) { DocketEntry entry = Docket.Find(e => e.Property == index); return entry == null ? ServiceResult.Pending : entry.Result; }
        public int NearbyNotice(){
            if(Player.InCar||Busy||Horror.Active)return -1;
            // The note is taped to the door, next to where you knock: whichever of the two sits nearer the crosshair wins,
            // and once read the note has to be looked at squarely to pick it up again.
            var eye=Scene.View.transform;
            foreach(var entry in Docket){var p=Property(entry.Property);if(entry.Result!=ServiceResult.Pending||!p.NoticePoint||!p.NoticePoint.gameObject.activeInHierarchy||string.IsNullOrEmpty(p.NoticeText)||!ServiceInteraction.Reachable(Scene.View,p.NoticePoint.position,2.7f,p.NoticePoint.parent))continue;
             if(Life&&Life.DoorOpenDegrees(p.Index)>25)continue; // V21: the door is open; the note went with it
             float aim=Vector3.Dot(eye.forward,(p.NoticePoint.position-eye.position).normalized);
             if(noticeRead[p.Index]&&aim<.975f)continue;
             if(!AccessGranted(p.Index)&&p.KnockPoint&&p.DoorPanel&&ServiceInteraction.Reachable(Scene.View,p.KnockPoint.position,2.4f,p.DoorPanel)&&Vector3.Dot(eye.forward,(p.KnockPoint.position-eye.position).normalized)>aim+.004f)continue;
             return p.Index;}
            return -1;
        }
        public void CloseNote(){NoteOpen=-1;}
        static Vector3 Outward(ServiceProperty p){var o=p.Door.position-p.InteriorBounds.center;o.y=0;return o.sqrMagnitude<.01f?p.Door.forward:o.normalized;}
        public void ReadNotice(int index){if(NearbyNotice()!=index)return;noticeRead[index]=true;NoteOpen=index;Notice="";Audio.Paper();}
        public int NearbyKnockDoor(){
            if(Player.InCar)return -1;
            foreach(var entry in Docket){var p=Property(entry.Property);if(entry.Result==ServiceResult.Pending&&!AccessGranted(p.Index)&&p.DoorPanel&&p.KnockPoint&&ServiceInteraction.Reachable(Scene.View,p.KnockPoint.position,2.4f,p.DoorPanel))return p.Index;}
            return -1;
        }
        public const float TableReach=1.8f;
        public int NearbyDoor(){
            if(Player.InCar)return -1;
            foreach(var e in Docket){var p=Property(e.Property);if(e.Result==ServiceResult.Pending&&AccessGranted(e.Property)&&!IsFriendly(e.Property)&&p.DeliveryPoint&&ServiceInteraction.Reachable(Scene.View,p.DeliveryPoint.position+Vector3.up*.09f,TableReach,null))return e.Property;}
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
            if(result==ServiceResult.LeftAtDoor&&(!AccessGranted(index)||IsFriendly(index))){Say(ServiceScript.KnockFirst);return;}
            if(p.HasEncounter && EncounterTonight(index) && !IsFriendly(index) && result!=ServiceResult.Unable){
                if(NearbyDoor()!=index)return;
                StartCoroutine(Place(entry,p,true));return;
            }
            if (result == ServiceResult.LeftAtDoor) { StartCoroutine(Place(entry,p,false)); return; }
            entry.Result = result;
            Say(ServiceScript.NoContact);
        }
        // V21: leaving the papers is the hand's doing. The player holds still while the left hand brings the papers down;
        // at the moment it lets go (PlaceLead) the copy slides from just under the view onto its clear patch of the table,
        // lands with the paper sound, and only then does the house answer (the line, the docket, the encounter).
        public float PlaceSlide=.32f;
        IEnumerator Place(DocketEntry entry,ServiceProperty p,bool encounter){
            Busy=true;int index=p.Index;int captures=Horror.Captures;
            // a capture (or anything that reset the house) while the hand is busy cancels the delivery
            bool Interrupted()=>Horror.Caught||Horror.Captures!=captures||entry.Result!=ServiceResult.Pending;
            var hands=Scene.View.GetComponentInChildren<ServiceHands>();if(hands)hands.Play("Place");
            if(hands&&hands.Visible)yield return new WaitForSeconds(hands.PlaceLead);
            if(hands)hands.ReleasePaper();
            if(Interrupted()){Busy=false;yield break;}
            if(p.PostedPaper){
                var paper=p.PostedPaper;var tr=paper.transform;paper.SetActive(true);ServicePaperPlacement.Settle(paper,Scene.View.transform.position);
                Vector3 endP=tr.position;Quaternion endR=tr.rotation;
                if(hands&&hands.Visible){
                    // from just below the bottom of the frame, where the hand took the papers, onto the table
                    var cam=Scene.View.transform;var cx=Scene.View;float half=Mathf.Tan(cx.fieldOfView*.5f*Mathf.Deg2Rad);
                    var flat=Vector3.ProjectOnPlane(endP-cam.position,Vector3.up);float reach=Mathf.Clamp(flat.magnitude*.55f,.35f,.8f);
                    Vector3 startP=cam.position+cam.forward*reach-cam.up*(reach*half*1.25f);
                    if(startP.y<endP.y+.05f)startP.y=endP.y+.05f;
                    Quaternion startR=Quaternion.AngleAxis(-28,Vector3.Cross(Vector3.up,(endP-cam.position).normalized))*endR;
                    for(float t=0;t<PlaceSlide;t+=Time.deltaTime){float k=t/PlaceSlide;float e=1-(1-k)*(1-k)*(1-k);
                        tr.SetPositionAndRotation(Vector3.Lerp(startP,endP,e)+Vector3.up*Mathf.Sin(k*Mathf.PI)*.04f,Quaternion.Slerp(startR,endR,e));yield return null;}
                    tr.SetPositionAndRotation(endP,endR);
                }
            }
            Busy=false;
            if(Interrupted()){if(p.PostedPaper)p.PostedPaper.SetActive(false);yield break;}
            if(Phase!=ServicePhase.Playing&&Phase!=ServicePhase.Paused)yield break;
            entry.Result=ServiceResult.LeftAtDoor;Audio.Paper();PapersPlaced++;
            if(encounter){if(!Horror.Active&&!Horror.Caught)Horror.Begin(index);yield break;}
            if (index == 4 && NightIndex == 1) Horror.ArmReturnAmbush();
            if (!p.HasEncounter && !IsFriendly(index) && NightIndex > 0 && !string.IsNullOrEmpty(p.RevealLine))
            {
                Audio.KnockAt(p.SoundPoint != null ? p.SoundPoint.position : p.Door.position, .35f);
                Say(p.RevealLine);
            }
            else { Audio.DeliveryComplete(); Say(ServiceScript.EnvelopeLeft); }
        }
        public int PapersPlaced {get;private set;}

        ServiceResidents residents;
        IEnumerator Knock(DocketEntry entry, ServiceProperty p)
        {
            Busy = true;
            KnockCount++;
            var hands=Scene.View.GetComponentInChildren<ServiceHands>();if(hands)hands.Play("Knock");
            if(hands&&hands.Visible)yield return new WaitForSeconds(hands.KnockLead);
            Audio.KnockAt(p.Door.position);
            yield return new WaitForSeconds(1.2f);
            accessGranted[p.Index]=true;
            if (IsFriendly(entry.Property))
            {
                if(p.WindowLight)p.WindowLight.enabled=true;
                Audio.DoorAt(p.Door.position);
                Say("A floorboard creaks beyond the door.");
                if(!residents)residents=FindAnyObjectByType<ServiceResidents>();if(residents)residents.Answer(p,true);
                if(Life)Life.OpenDoor(p,true);
                // V21: the resident waits back in the hall until the leaf is open, then steps up into the doorway.
                float answerWait=0;while(answerWait<2.6f&&residents&&!residents.InDoorway(p.Index)){answerWait+=Time.deltaTime;yield return null;}
                yield return new WaitForSeconds(.25f);
                var talk=ServiceScript.Doorstep(entry.Property,NightIndex);
                if(entry.Property==0&&Life)Life.Hush(); // V22: "Rex, hush."
                if(talk.HasValue&&Dialogue)yield return Dialogue.Run(talk.Value.speaker,talk.Value.steps);
                string after=talk.HasValue?talk.Value.after:"";
                entry.Result = ServiceResult.Served;
                if(hands)hands.Play("Give");
                if(hands&&hands.Visible)yield return new WaitForSeconds(hands.GiveLead);
                Audio.Paper();
                if(entry.Property==0)Audio.DeliveryComplete();
                // V21: they step back clear of the leaf before shutting it.
                if(residents)yield return residents.StepBack(p);
                if(Life)Life.OpenDoor(p,false);
                Audio.DoorAt(p.Door.position);
                if(!string.IsNullOrEmpty(after))Say(after);
                yield return new WaitForSeconds(.7f);if(residents)residents.Answer(p,false);
            }
            else if (NightIndex > 0)
            {
                yield return new WaitForSeconds(.75f);
                Audio.KnockAt(p.SoundPoint != null ? p.SoundPoint.position : p.Door.position + p.Door.forward * .6f, .52f);
            }
            else
            {
                // V21: let the silence land before the latch gives.
                Say(ServiceScript.NoAnswer);
                yield return new WaitForSeconds(2.2f);
                if (Phase != ServicePhase.Playing) { Busy = false; yield break; }
            }
            if(!IsFriendly(entry.Property)&&p.DoorPanel){if(hands)hands.Play("Push");if(hands&&hands.Visible)yield return new WaitForSeconds(hands.PushLead);if(Life)Life.OpenDoor(p,true);Say(NoticeRead(p.Index)?ServiceScript.LatchGivesNoteRead:ServiceScript.LatchGives);}
            Busy = false;
        }

        // V21: the copy appears on the table when the hand lets go of it, not while the hand is still carrying it in.
        void Gesture(string name){var hands=Scene.View.GetComponentInChildren<ServiceHands>();if(hands)hands.Play(name);}
        void ResetResidents(){if(!residents)residents=FindAnyObjectByType<ServiceResidents>();if(residents)residents.ResetAll();}
        public void SayDogLine(){if(!dogLine&&string.IsNullOrEmpty(Notice)){dogLine=true;Say(ServiceScript.DogBarks);}}
        public void Say(string text) { if (string.IsNullOrEmpty(text)) return; Notice = text; noticeUntil = Time.unscaledTime + Mathf.Clamp(1.6f + text.Length * .055f, 3.2f, 6.5f); }
        public void RetryVilla(){RetryProperty(1);}
        public void RetryProperty(int index){if(index>=0&&index<6)shutByPlayer[index]=false;var entry=Docket.Find(e=>e.Property==index);if(entry!=null)entry.Result=IsFriendly(index)?ServiceResult.Served:ServiceResult.Pending;if(Property(index).PostedPaper)Property(index).PostedPaper.SetActive(false);Busy=false;PaperOpen=MapOpen=false;accessGranted[index]=true;if(Life)Life.OpenDoor(Property(index),true);Player.IgnitionDelayPending=false;}
        public void RequestEarlyFinish() { if (Phase == ServicePhase.Playing) { EndedEarly = true; PaperOpen = false; Say(ServiceScript.RouteClosedEarly); SetCursor(); } }
        IEnumerator Later(float seconds, string line) { yield return new WaitForSeconds(seconds); if (Phase == ServicePhase.Playing && !Horror.Active) Say(line); }
        public bool TryFinishShift()
        {
            if (!CanFinish || Busy) return false;
            foreach (DocketEntry e in Docket) lastResults[e.Property] = e.Result;
            SaveRoute();
            Phase = ServicePhase.Report;
            PaperOpen = false;
            if(Timecard)Timecard.NightEnd();
            Player.StopEngine();
            Audio.StopDog();
            SetCursor();
            return true;
        }
        public void NextShift() { if (NightIndex < 2) BeginShift(NightIndex + 1); else { Phase = ServicePhase.Finished; SetCursor(); } }
        public bool InsideVilla {get {if(Player==null||Player.InCar)return false;return Array.Exists(Scene.Properties,p=>p.InteriorBounds.Contains(Scene.Walker.transform.position+Vector3.up*.3f));}}
        string SavePrefix=>IsSmoke?"SERVICE.test.":IsChaos?"SERVICE.chaos.":"SERVICE.v5.";
        public bool HasSavedRoute=>PlayerPrefs.HasKey(SavePrefix+"night");
        void SaveRoute(){if(NightIndex<2){PlayerPrefs.SetInt(SavePrefix+"night",NightIndex+1);for(int i=0;i<6;i++)PlayerPrefs.SetInt(SavePrefix+"result"+i,(int)lastResults[i]);}else PlayerPrefs.DeleteKey(SavePrefix+"night");PlayerPrefs.Save();}
        public void ContinueRoute(){for(int i=0;i<6;i++)lastResults[i]=(ServiceResult)PlayerPrefs.GetInt(SavePrefix+"result"+i,0);BeginShift(Mathf.Clamp(PlayerPrefs.GetInt(SavePrefix+"night",0),0,2));}
        public void NewRoute(){PlayerPrefs.DeleteKey(SavePrefix+"night");Array.Clear(lastResults,0,lastResults.Length);PlayerPrefs.Save();BeginShift(0);}
        public void SaveOptions(){if(IsSmoke||IsChaos)return;PlayerPrefs.SetFloat("SERVICE.volume",Audio.Volume);PlayerPrefs.SetFloat("SERVICE.music",Audio.MusicVolume);PlayerPrefs.SetFloat("SERVICE.sensitivity",Player.Sensitivity);PlayerPrefs.SetFloat("SERVICE.motion",Player.CameraMotion);PlayerPrefs.SetFloat("SERVICE.blur",Player.MotionBlurAmount);if(Storm)Storm.Save();PlayerPrefs.Save();}
        void OnApplicationFocus(bool focused){if(IsChaos)return;if(!focused&&!IsSmoke&&Phase==ServicePhase.Playing)Pause();}
        public void Pause() { if (Phase != ServicePhase.Playing) return; Phase = ServicePhase.Paused; Time.timeScale = 0; SetCursor(); }
        public int ResumedFrame = -1;
        public void Resume() { if (Phase != ServicePhase.Paused) return; Phase = ServicePhase.Playing; Time.timeScale = 1; ResumedFrame = Time.frameCount; SetCursor(); }
        public void Title() { StopAllCoroutines(); Busy = false; if(Timecard)Timecard.Clear(); if(Dread)Dread.ResetForShift(); ResetResidents(); if(Life)Life.ResetForShift(); if(Vehicle)Vehicle.ResetRadio(); if(Dialogue)Dialogue.Cancel(); Phase = ServicePhase.Title; PaperOpen = MapOpen = false; Time.timeScale = 1; Player.StopEngine(); Horror.ResetEncounter(); SetCursor(); }
        public void SetCursor() { bool locked = Phase == ServicePhase.Playing && !PaperOpen && !IsSmoke; Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnDestroy() { if(Audio&&Player)SaveOptions();AudioListener.pause=false;Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}








