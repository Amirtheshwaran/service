using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ServiceGameV2
{
    // Invoked only with -serviceSmoke. Exercises the real state machine and controllers.
    public sealed class ServiceSmoke : MonoBehaviour
    {
        ServiceDirector d;
        string captureDir;
        public void Run(ServiceDirector director)
        {
            d = director;
            captureDir = Environment.GetEnvironmentVariable("SERVICE_CAPTURE_DIR");
            if (string.IsNullOrEmpty(captureDir)) captureDir = Path.Combine(Application.persistentDataPath, "Smoke");
            Directory.CreateDirectory(captureDir);
            StartCoroutine(Guard());
        }
        IEnumerator Guard()
        {
            IEnumerator test = Check();
            bool failed = false;
            while (true)
            {
                object next = null;
                bool more = false;
                try { more = test.MoveNext(); if (more) next = test.Current; }
                catch (Exception ex) { Debug.LogException(ex); failed = true; }
                if (failed || !more) break;
                yield return next;
            }
            string result = failed ? "FAIL: SERVICE_SMOKE" : "PASS: SERVICE_SMOKE (three shifts, service outcomes, movement, changed light, frozen odometer, delayed ignition, depot reports, early finish, captures)";
            File.WriteAllText(Path.Combine(captureDir, "smoke-result.txt"), result);
            Debug.Log(result);
            Application.Quit(failed ? 1 : 0);
        }
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("SERVICE_SMOKE: " + message); }
        void Capture(string name) { ScreenCapture.CaptureScreenshot(Path.Combine(captureDir, name + ".png")); }
        IEnumerator Check()
        {
            yield return null;
            d.BeginShift(0);
            Require(d.NightIndex == 0 && d.Docket.Count == 2 && d.Player.InCar, "First shift should start inside car with two stops");
            yield return new WaitForEndOfFrame();
            Capture("01-docket");
            yield return new WaitForSeconds(1);
            d.PaperOpen = false;
            yield return new WaitForEndOfFrame();
            Capture("00-driving");
            Vector3 start = d.Scene.Car.position;
            d.Player.SmokeThrottle = 1;
            yield return new WaitForSeconds(2);
            d.Player.SmokeThrottle = 0;
            Require(Vector3.Distance(start, d.Scene.Car.position) > 1, "Vehicle did not move");
            d.Player.TeleportCar(d.Scene.Properties[0].Gate.position - d.Scene.Properties[0].Gate.forward * 4, d.Scene.Car.rotation);
            Require(d.Player.TryExitCar(), "Car door exit is blocked in the parking approach");
            d.Player.SmokePlaceWalker(d.Scene.Properties[0].Gate.position);
            d.Player.SmokeFace(d.Scene.Properties[0].Door.position);
            d.Player.SmokeWalk=Vector2.up;
            float walkingUntil=Time.time+9;
            while(d.NearbyDoor()!=0 && Time.time<walkingUntil) yield return null;
            d.Player.SmokeWalk=Vector2.zero;
            Require(d.NearbyDoor() == 0, "Door approach does not reach service interaction");
            yield return new WaitForEndOfFrame(); Capture("01a-threshold");
            d.Attempt(0, ServiceResult.Served);
            yield return new WaitForSeconds(3.5f);
            Require(d.ResultAt(0) == ServiceResult.Served, "Ordinary direct serve failed");
            d.Attempt(1, ServiceResult.LeftAtDoor);
            Require(d.Scene.Properties[1].PostedPaper.activeSelf, "Leaving papers must change the door");
            Require(d.AllResolved, "First docket was not resolved");
            d.Player.EnterCar();
            d.Player.TeleportCar(d.Scene.Depot.position, d.Scene.Depot.rotation);
            Require(d.TryFinishShift(), "Completed shift must finish at depot");
            d.NextShift();
            Require(d.NightIndex == 1 && d.Phase == ServicePhase.Playing, "Second shift did not start");
            d.PaperOpen = false;
            ServiceProperty p = d.Scene.Properties[1];
            d.Player.SmokePlaceWalker(p.Gate.position);
            d.Scene.View.transform.rotation = Quaternion.LookRotation(d.Scene.View.transform.position - p.Door.position);
            bool light = p.PorchLight.enabled;
            d.SmokeAdvanceCues(30);
            Require(p.PorchLight.enabled != light && d.EnvironmentChanges > 0, "Unobserved porch light must actually change");
            d.Scene.View.transform.LookAt(p.Door.position + Vector3.up * 1.5f);
            yield return new WaitForEndOfFrame();
            Capture("02-property");
            yield return new WaitForSeconds(1);
            d.Player.SmokeFace(p.Door.position);d.Player.SmokeWalk=Vector2.up;
            walkingUntil=Time.time+9;
            while(d.NearbyDoor()!=1 && Time.time<walkingUntil)yield return null;
            d.Player.SmokeWalk=Vector2.zero;
            Require(d.NearbyDoor()==1,"Second property's driveway or stairs block the officer");
            Require(d.Player.IgnitionDelayPending, "Lingering should affect next ignition");
            d.Attempt(0, ServiceResult.LeftAtDoor);
            d.Attempt(1, ServiceResult.Unable);
            d.Player.EnterCar();
            d.Player.StartEngine();
            Require(d.Player.IsStarting && !d.Player.EngineRunning, "Delayed ignition must hold engine off");
            yield return new WaitForSeconds(5.5f);
            Require(d.Player.EngineRunning, "Delayed ignition must recover");
            d.Player.TeleportCar(d.Scene.Depot.position, d.Scene.Depot.rotation);
            Require(d.TryFinishShift(), "Second shift did not finish at depot");
            d.NextShift();
            Require(d.NightIndex == 2 && d.Scene.LateRoad.activeSelf, "Final road must open on night nine");
            d.PaperOpen = false;
            d.Player.TeleportCar(d.Scene.LateThreshold.position + d.Scene.LateThreshold.forward * 12, d.Scene.LateThreshold.rotation);
            yield return null;
            Require(d.OdometerFrozen, "Odometer must freeze beyond mapped road");
            float mileage = d.TripMiles;
            d.Player.SmokeThrottle = 1;
            yield return new WaitForSeconds(1.5f);
            d.Player.SmokeThrottle = 0;
            Require(Mathf.Approximately(mileage, d.TripMiles), "Frozen odometer changed");
            d.Attempt(1, ServiceResult.Unable);
            d.Attempt(2, ServiceResult.Served);
            yield return new WaitForSeconds(3.5f);
            Require(d.ResultAt(2) == ServiceResult.Pending, "Final knock must not manufacture confirmed service");
            d.Attempt(2, ServiceResult.LeftAtDoor);
            d.Player.TeleportCar(d.Scene.Depot.position, d.Scene.Depot.rotation);
            Require(d.TryFinishShift(), "Final completed route did not end");
            yield return new WaitForEndOfFrame();
            Capture("03-report");
            yield return new WaitForSeconds(1);
            d.BeginShift(2);
            d.RequestEarlyFinish();
            Require(!d.TryFinishShift() || d.Phase == ServicePhase.Report, "Early finish report state invalid");
            if (d.Phase != ServicePhase.Report)
            {
                d.Player.TeleportCar(d.Scene.Depot.position, d.Scene.Depot.rotation);
                Require(d.TryFinishShift(), "Early shift must finish at depot");
            }
            Require(d.EndedEarly, "Early ending was not retained in report");
            Require(File.Exists(Path.Combine(captureDir, "01-docket.png")) && File.Exists(Path.Combine(captureDir, "02-property.png")) && File.Exists(Path.Combine(captureDir, "03-report.png")), "Screenshot files missing");
        }
    }
}
