using UnityEngine;
using UnityEngine.InputSystem;

namespace ServiceGameV2
{
    public sealed class ServicePlayer : MonoBehaviour
    {
        public bool InCar { get; private set; }
        public bool EngineRunning { get; private set; }
        public bool IsStarting { get; private set; }
        public bool IgnitionDelayPending;
        public float Speed => Mathf.Abs(speed);
        public float Sensitivity = .095f;
        public float SmokeThrottle;
        public Vector2 SmokeWalk;
        public bool SmokeSprint;
        public bool Sprinting => !InCar && (d.IsSmoke ? SmokeSprint : Keyboard.current != null && Keyboard.current[Key.LeftShift].isPressed);
        ServiceDirector d;
        CountyScene s;
        float yaw, pitch, speed, gravity, footstep, startAt;
        int fullMask;

        public void Initialize(ServiceDirector director)
        {
            d = director; s = d.Scene;
            if(!d.IsSmoke)Sensitivity=Mathf.Clamp(PlayerPrefs.GetFloat("SERVICE.sensitivity",.095f),.03f,.2f);
            fullMask = s.View.cullingMask | (1 << 8);
            s.View.nearClipPlane = .035f;
            s.View.fieldOfView = 68;
        }

        void Update()
        {
            if (s == null || d.Phase != ServicePhase.Playing) return;
            if (IsStarting && Time.time >= startAt) { IsStarting = false; EngineRunning = true; d.Audio.Engine(true); }
            if (d.InputBlocked) { speed = Mathf.MoveTowards(speed, 0, 9 * Time.deltaTime); return; }
            Vector2 look = Mouse.current == null || d.IsSmoke ? Vector2.zero : Mouse.current.delta.ReadValue();
            yaw += look.x * Sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * Sensitivity, -65, 70);
            if (InCar)
            {
                yaw = Mathf.Clamp(yaw, -100, 100);
                s.View.transform.localRotation = Quaternion.Euler(Mathf.Clamp(pitch,-35,48)+5, yaw, 0);
                if (Pressed(Key.Space)) StartEngine();
                float throttle = d.IsSmoke ? SmokeThrottle : Axis(Key.S, Key.W);
                if (Mathf.Abs(throttle) > .1f && !EngineRunning && !IsStarting) StartEngine();
                Drive(throttle, d.IsSmoke ? 0 : Axis(Key.A, Key.D));
            }
            else
            {
                s.Walker.transform.rotation = Quaternion.Euler(0, yaw, 0);
                s.View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
                Walk(d.IsSmoke ? SmokeWalk : new Vector2(Axis(Key.A, Key.D), Axis(Key.S, Key.W)));
            }
            d.Audio.EngineSpeed(Speed);
        }

        static bool Pressed(Key key) { return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame; }
        static float Axis(Key negative, Key positive)
        {
            if (Keyboard.current == null) return 0;
            return (Keyboard.current[positive].isPressed ? 1 : 0) - (Keyboard.current[negative].isPressed ? 1 : 0);
        }

        void Drive(float throttle, float steering)
        {
            s.SteeringWheel.localRotation=Quaternion.Slerp(s.SteeringWheel.localRotation,Quaternion.Euler(18,0,-steering*100),Time.deltaTime*8);
            s.SpeedNeedle.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-130,130,Mathf.Clamp01(Speed*2.237f/60)));
            s.RevNeedle.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-130,130,EngineRunning?Mathf.Clamp01(.14f+Speed/30+Mathf.Abs(throttle)*.12f):0));
            float target = !EngineRunning ? 0 : throttle > 0 ? throttle * 12 : throttle * 4;
            speed = Mathf.MoveTowards(speed, target, (Mathf.Abs(throttle) < .1f || Mathf.Sign(throttle) != Mathf.Sign(speed) ? 8 : 4.1f) * Time.deltaTime);
            if (Keyboard.current != null && Keyboard.current[Key.LeftShift].isPressed) speed = Mathf.MoveTowards(speed, 0, 14 * Time.deltaTime);
            s.Car.Rotate(0, steering * Mathf.Clamp(speed / 5, -1, 1) * 50 * Time.deltaTime, 0, Space.World);
            gravity = s.CarBody.isGrounded ? -2 : Mathf.Max(-20, gravity - 20 * Time.deltaTime);
            Vector3 before = s.Car.position;
            CollisionFlags collision = s.CarBody.Move((s.Car.forward * speed + Vector3.up * gravity) * Time.deltaTime);
            if ((collision & CollisionFlags.Sides) != 0 && Vector3.Distance(before, s.Car.position) < Mathf.Abs(speed) * Time.deltaTime * .35f) speed *= .75f;
        }

        void Walk(Vector2 input)
        {
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 move = (s.Walker.transform.right * input.x + s.Walker.transform.forward * input.y) * (Sprinting ? 5.7f : 3.25f);
            gravity = s.Walker.isGrounded ? -2 : Mathf.Max(-25, gravity - 20 * Time.deltaTime);
            s.Walker.Move((move + Vector3.up * gravity) * Time.deltaTime);
            footstep -= Time.deltaTime;
            if (input.sqrMagnitude > .1f && s.Walker.isGrounded && footstep <= 0)
            {
                d.Audio.Footstep(s.Walker.transform.position);
                footstep = Sprinting ? .32f : .53f;
            }
        }

        public void StartEngine()
        {
            if (!InCar || IsStarting || EngineRunning) return;
            IsStarting = true;
            startAt = Time.time + (IgnitionDelayPending ? 4.6f : .7f);
            d.Audio.Ignition(IgnitionDelayPending);
            IgnitionDelayPending = false;
        }
        public void StopEngine() { EngineRunning = IsStarting = false; speed = 0; if (d.Audio != null) d.Audio.Engine(false); }

        public void EnterCar()
        {
            InCar = true;
            if(s.Cockpit)s.Cockpit.SetActive(true);
            s.Walker.enabled = false;
            s.View.transform.SetParent(s.DriverSeat, false);
            s.View.transform.localPosition = Vector3.zero;
            s.View.transform.localRotation = Quaternion.Euler(5,0,0);
            s.View.cullingMask = fullMask & ~(1 << 8);
            yaw = pitch = 0;
            if (s.Flashlight != null) s.Flashlight.enabled = false;
            d.SetCursor();
        }

        public bool TryExitCar()
        {
            if (!InCar || Speed > .8f || IsStarting) return false;
            Transform exit = ClearExit(s.ExitLeft) ? s.ExitLeft : ClearExit(s.ExitRight) ? s.ExitRight : null;
            if (exit == null) { d.Say("No room to open the door."); return false; }
            Vector3 point = exit.position;
            if (Physics.Raycast(point + Vector3.up * 2, Vector3.down, out RaycastHit ground, 6, ~(1 << 8), QueryTriggerInteraction.Ignore)) point.y = ground.point.y + .05f;
            PlaceWalker(point, s.Car.eulerAngles.y);
            StopEngine();
            return true;
        }
        bool ClearExit(Transform exit)
        {
            if (exit == null) return false;
            Vector3 p = exit.position;
            return !Physics.CheckCapsule(p + Vector3.up * .4f, p + Vector3.up * 1.5f, .28f, ~(1 << 8), QueryTriggerInteraction.Ignore);
        }
        void PlaceWalker(Vector3 point, float angle)
        {
            InCar = false;
            if(s.Cockpit)s.Cockpit.SetActive(false);
            s.Walker.enabled = false;
            s.Walker.transform.position = point;
            s.Walker.transform.rotation = Quaternion.Euler(0, angle, 0);
            s.Walker.enabled = true;
            s.View.transform.SetParent(s.Walker.transform, false);
            s.View.transform.localPosition = new Vector3(0, 1.65f, 0);
            s.View.transform.localRotation = Quaternion.identity;
            s.View.cullingMask = fullMask;
            yaw = angle; pitch = gravity = 0;
            d.PaperOpen = false;
            d.SetCursor();
        }
        public void ResetForShift(Vector3 point, Quaternion rotation)
        {
            StopEngine(); IgnitionDelayPending = false; SmokeThrottle = 0;
            TeleportCar(point, rotation);
            EnterCar();
        }
        public void TeleportCar(Vector3 point, Quaternion rotation)
        {
            s.CarBody.enabled = false;
            s.Car.SetPositionAndRotation(point + Vector3.up * .04f, rotation);
            s.CarBody.enabled = true;
            speed = gravity = 0;
        }
        public void SmokePlaceWalker(Vector3 point)
        {
            if (!d.IsSmoke) return;
            StopEngine(); PlaceWalker(point + Vector3.up * .08f, 0);
        }
        public void RestoreApproach(Vector3 point,float angle) { StopEngine(); SmokeWalk=Vector2.zero;SmokeSprint=false;PlaceWalker(point+Vector3.up*.08f,angle); }
        public void SmokeFace(Vector3 point)
        {
            if (!d.IsSmoke) return;
            Vector3 offset=point-s.Walker.transform.position;
            yaw=Mathf.Atan2(offset.x,offset.z)*Mathf.Rad2Deg; pitch=0;
        }
        public void SmokeLook(float horizontal,float vertical){if(d.IsSmoke){yaw=horizontal;pitch=vertical;}}
    }
}
