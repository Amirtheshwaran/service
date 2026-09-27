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
        public bool FailedIgnition {get;private set;}
        public bool OnStairs {get;private set;}
        public float HorizontalSpeed {get;private set;}
        float nextCollision;
        public string LastVehicleObstruction {get;private set;}
        public float Speed => Mathf.Abs(speed);
        public float Sensitivity = .095f;
        public float SmokeThrottle, SmokeSteering;
        public bool SmokeBrake;
        public float SteeringInput=>steer;
        public float SignedSpeed=>speed;
        float steer, acceleration, previousSpeed;
        Quaternion wheelRest;
        public Vector2 SmokeWalk;
        public bool SmokeSprint;
        public int SmokeLookBack;
        public bool SmokeCrouch, SmokeJump;
        public bool Crouched { get; private set; }
        public float CameraMotion=.65f, MotionBlurAmount=.16f;
        public float VerticalSpeed=>gravity;
        float gait, motionBlend, landing, lastVertical, groundedUntil, jumpUntil;
        UnityEngine.Rendering.Universal.MotionBlur blur;
        public float LookBackAngle=>lookBack;
        float lookBack;
        public static float ResolveLookBack(bool running,bool car,bool blocked,bool left,bool right)=>!running||car||blocked||left==right?0:left?-155:155;
        float LookBackTarget=>ResolveLookBack(Sprinting,InCar,d.InputBlocked,d.IsSmoke?SmokeLookBack<0:Keyboard.current!=null&&Keyboard.current[Key.Q].isPressed,d.IsSmoke?SmokeLookBack>0:Keyboard.current!=null&&Keyboard.current[Key.E].isPressed);
        public bool InteractionSuppressed=>LookBackTarget!=0||Mathf.Abs(lookBack)>1;
        public bool Sprinting => !InCar && !Crouched && (d.IsSmoke ? SmokeSprint : Keyboard.current != null && Keyboard.current[Key.LeftShift].isPressed);
        ServiceDirector d;
        CountyScene s;
        float yaw, pitch, speed, gravity, footstep, startAt;
        int fullMask;

        public void Initialize(ServiceDirector director)
        {
            d = director; s = d.Scene;
            if(!d.IsSmoke)Sensitivity=Mathf.Clamp(PlayerPrefs.GetFloat("SERVICE.sensitivity",.095f),.03f,.2f);
            wheelRest=s.SteeringWheel.localRotation;
            fullMask = s.View.cullingMask | (1 << 8);
            if(!d.IsSmoke){CameraMotion=PlayerPrefs.GetFloat("SERVICE.motion",.65f);MotionBlurAmount=PlayerPrefs.GetFloat("SERVICE.blur",.16f);}
            var volume=GetComponentInChildren<UnityEngine.Rendering.Volume>();
            if(volume){var profile=volume.profile;if(!profile.TryGet(out blur))blur=profile.Add<UnityEngine.Rendering.Universal.MotionBlur>(true);blur.mode.Override(UnityEngine.Rendering.Universal.MotionBlurMode.CameraOnly);blur.intensity.Override(MotionBlurAmount);blur.clamp.Override(.035f);}
            s.View.nearClipPlane = .035f;
            s.View.fieldOfView = 68;
        }

        void Update()
        {
            if (s == null || d.Phase != ServicePhase.Playing) return;
            if (IsStarting && !d.PaperOpen && Time.time >= startAt) { IsStarting = false; EngineRunning = true; d.Audio.Engine(true); }
            if (d.InputBlocked) { HoldVehicle(); return; }
            Vector2 look = Mouse.current == null || d.IsSmoke ? Vector2.zero : Mouse.current.delta.ReadValue();
            float backTarget=LookBackTarget;
            if(backTarget==0&&Mathf.Abs(lookBack)<5)yaw += look.x * Sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * Sensitivity, -65, 70);
            if (InCar)
            {
                yaw = Mathf.Clamp(yaw, -100, 100);
                s.View.transform.localRotation = Quaternion.Euler(Mathf.Clamp(pitch,-35,48)+5, yaw, 0);
                if (Pressed(Key.Space)) StartEngine();
                UpdateGauges();
                float throttle = d.IsSmoke ? SmokeThrottle : Axis(Key.S, Key.W);
                if (Mathf.Abs(throttle) > .1f && !EngineRunning && !IsStarting) StartEngine();
                Drive(throttle, d.IsSmoke ? SmokeSteering : Axis(Key.A, Key.D));
            }
            else
            {
                s.Walker.transform.rotation = Quaternion.Euler(0, yaw, 0);
                lookBack=Mathf.MoveTowards(lookBack,backTarget,720*Time.deltaTime);
                s.View.transform.localRotation = Quaternion.Euler(pitch, lookBack, 0);
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
            steer=Mathf.MoveTowards(steer,steering,Time.deltaTime*(Mathf.Abs(steering)<.1f?3.3f:2.2f));
            s.SteeringWheel.localRotation=wheelRest*Quaternion.Euler(0,0,-steer*135);
            
            bool brake=d.IsSmoke?SmokeBrake:Keyboard.current!=null&&Keyboard.current[Key.LeftShift].isPressed;
            // Opposite pedal input brakes first; reversal starts only after reaching rest.
            bool opposing=Mathf.Abs(speed)>.12f&&throttle*speed<0;
            float target=!EngineRunning||brake||opposing?0:throttle*(throttle>0?14:3.5f);
            float rate=brake?14:opposing?9:Mathf.Abs(throttle)<.1f?2.1f:3.3f;
            speed=Mathf.MoveTowards(speed,target,rate*Time.deltaTime);
            acceleration=Mathf.Lerp(acceleration,(speed-previousSpeed)/Mathf.Max(.001f,Time.deltaTime),1-Mathf.Exp(-Time.deltaTime*5));previousSpeed=speed;
            float wheelAngle=steer*Mathf.Lerp(29,15,Mathf.InverseLerp(3,14,Speed));
            float yawRate=speed/2.5f*Mathf.Tan(wheelAngle*Mathf.Deg2Rad)*Mathf.Rad2Deg;
            s.Car.Rotate(0,yawRate*Time.deltaTime,0,Space.World);
            gravity=s.CarBody.isGrounded?-2:Mathf.Max(-20,gravity-20*Time.deltaTime);
            Vector3 before=s.Car.position;
            float impactSpeed=Speed;
            Vector3 travel=s.Car.forward*speed*Time.deltaTime;
            bool estate=d.Scene.Properties.AnyBuildingContains(before+travel,1.35f);
            bool solid=Physics.BoxCast(before+Vector3.up*.85f,new Vector3(.78f,.42f,1.85f),speed<0?-s.Car.forward:s.Car.forward,out var hit,s.Car.rotation,travel.magnitude+.06f,~((1<<8)|(1<<9)|(1<<10)),QueryTriggerInteraction.Ignore);
            var collision=s.CarBody.Move((estate||solid?Vector3.zero:travel)+Vector3.up*gravity*Time.deltaTime);
            if(estate||solid||(collision&CollisionFlags.Sides)!=0){
                LastVehicleObstruction=estate?"Building clearance":solid?hit.collider.name:"Controller side contact";
                if(impactSpeed>1.2f&&Time.time>nextCollision){d.Audio.CollisionAt(s.Car.position+s.Car.forward*1.8f,impactSpeed);nextCollision=Time.time+.8f;landing=.06f;}
                speed=0;
            }
        }

        void Walk(Vector2 input)
        {
            input = Vector2.ClampMagnitude(input, 1);
            bool crouch=d.IsSmoke?SmokeCrouch:Keyboard.current!=null&&Keyboard.current[Key.LeftCtrl].isPressed;
            if(crouch)Crouched=true;
            else if(Crouched&&CanStand())Crouched=false;
            s.Walker.height=Crouched?1.12f:1.8f;s.Walker.center=new Vector3(0,s.Walker.height*.5f,0);
            OnStairs=ServiceStairZone.Contains(s.Walker.transform.position);
            if(OnStairs)jumpUntil=groundedUntil=0;
            Vector3 move = (s.Walker.transform.right * input.x + s.Walker.transform.forward * input.y) * (Crouched?1.55f:Sprinting ? 5.5f : 3.25f);
            if(s.Walker.isGrounded){groundedUntil=Time.time+.1f;if(gravity<0){if(lastVertical< -4)landing=Mathf.Min(.1f,-lastVertical*.008f);gravity=-2;}}
            if(d.IsSmoke?SmokeJump:Pressed(Key.Space)){if(!OnStairs)jumpUntil=Time.time+.12f;SmokeJump=false;}
            if(jumpUntil>Time.time&&groundedUntil>Time.time&&!Crouched&&!OnStairs){gravity=5.8f;jumpUntil=groundedUntil=0;}
            else gravity=Mathf.Max(-25,gravity-20*Time.deltaTime);
            Vector3 prior=s.Walker.transform.position;lastVertical=gravity;var flags=s.Walker.Move((move+Vector3.up*gravity)*Time.deltaTime);if((flags&CollisionFlags.Above)!=0&&gravity>0)gravity=0;
            var delta=s.Walker.transform.position-prior;delta.y=0;HorizontalSpeed=delta.magnitude/Mathf.Max(Time.deltaTime,.001f);
            motionBlend=Mathf.MoveTowards(motionBlend,input.magnitude*(s.Walker.isGrounded?1:0),Time.deltaTime*6);gait+=Time.deltaTime*(Sprinting?12:Crouched?6:8);
            footstep -= Time.deltaTime;
            if (input.sqrMagnitude > .1f && s.Walker.isGrounded && footstep <= 0)
            {
                d.Audio.Footstep(s.Walker.transform.position);
                footstep = Crouched?.8f:Sprinting ? .32f : .53f;
            }
        }
        bool CanStand(){foreach(var hit in Physics.OverlapCapsule(s.Walker.transform.position+Vector3.up*.35f,s.Walker.transform.position+Vector3.up*1.52f,.27f,~((1<<8)|(1<<9)),QueryTriggerInteraction.Ignore))if(hit!=s.Walker&&!hit.transform.IsChildOf(s.Walker.transform))return false;return true;}
        void LateUpdate(){
            if(!s)return;bool active=d.Phase==ServicePhase.Playing&&!d.PaperOpen;
            if(blur)blur.intensity.value=active?MotionBlurAmount:0;
            if(!active)return;
            s.View.fieldOfView=Mathf.Lerp(s.View.fieldOfView,!InCar&&Sprinting?74:68,1-Mathf.Exp(-Time.deltaTime*6));
            if(InCar){
                float motion=CameraMotion*Mathf.Clamp01(Speed/6);
                var offset=new Vector3(-steer*motion*.018f,Mathf.Sin(Time.time*17)*motion*.0015f,-acceleration*CameraMotion*.0015f);
                s.View.transform.localPosition=Vector3.Lerp(s.View.transform.localPosition,offset,1-Mathf.Exp(-Time.deltaTime*8));
                s.View.transform.localRotation=Quaternion.Euler(Mathf.Clamp(pitch,-35,48)+5+acceleration*CameraMotion*.09f,yaw,steer*motion*.65f);return;
            }
            if(d.Horror.Caught||d.Horror.ForcedLook)return;
            float amount=motionBlend*CameraMotion, bob=Mathf.Sin(gait*2)*(Sprinting?.025f:.014f)*amount;
            var target=new Vector3(Mathf.Sin(gait)*.018f*amount,(Crouched?1.02f:1.65f)+bob-landing*CameraMotion,0);
            s.View.transform.localPosition=Vector3.Lerp(s.View.transform.localPosition,target,1-Mathf.Exp(-Time.deltaTime*14));
            s.View.transform.localRotation=Quaternion.Euler(pitch+Mathf.Sin(gait)*amount*.45f,lookBack,Mathf.Cos(gait)*amount*(Sprinting?1.15f:.45f));landing=Mathf.MoveTowards(landing,0,Time.deltaTime*.45f);
        }

        public void StartEngine()
        {
            if (!InCar || IsStarting || EngineRunning) return;
            IsStarting = true;
            FailedIgnition=IgnitionDelayPending;startAt = Time.time + (FailedIgnition ? 3.8f : .7f);
            if(FailedIgnition)d.Say("Come on. Turn over.");
            d.Audio.Ignition(IgnitionDelayPending);
            IgnitionDelayPending = false;
        }
        public void HoldVehicle(){speed=previousSpeed=acceleration=0;HorizontalSpeed=0;UpdateGauges();}
        void UpdateGauges(){ServiceGauge.Set(s.SpeedNeedle,Mathf.Clamp01(Speed*2.237f/60));ServiceGauge.Set(s.RevNeedle,EngineRunning?Mathf.Clamp01(.14f+Speed/30):0);}
        public void StopEngine() { FailedIgnition=false; EngineRunning = IsStarting = false; speed = 0; if (d.Audio != null) d.Audio.Engine(false); }

        public void EnterCar()
        {
            Crouched=false;motionBlend=landing=0;SmokeCrouch=SmokeJump=false;
            lookBack=0;SmokeLookBack=0;
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
            Crouched=false;motionBlend=landing=0;SmokeCrouch=SmokeJump=false;s.Walker.height=1.8f;s.Walker.center=Vector3.up*.9f;
            lookBack=0;SmokeLookBack=0;
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
            yaw = angle; pitch = gravity = 0;HorizontalSpeed=0;
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
            speed = gravity = previousSpeed = acceleration = steer = 0;
        }
        public void SmokePlaceWalker(Vector3 point)
        {
            if (!d.IsSmoke) return;
            StopEngine(); PlaceWalker(point + Vector3.up * .08f, 0);
        }
        public void RestoreApproach(Vector3 point,float angle) { StopEngine(); SmokeWalk=Vector2.zero;SmokeSprint=false;PlaceWalker(point+Vector3.up*.08f,angle); }
        public void GlanceAt(Vector3 point,bool returning){
            var target=returning?s.Walker.transform.rotation*Quaternion.Euler(pitch,0,0):Quaternion.LookRotation(point-s.View.transform.position);
            s.View.transform.rotation=Quaternion.Slerp(s.View.transform.rotation,target,1-Mathf.Exp(-Time.deltaTime*(returning?14:7)));
        }
        public void FocusOn(Vector3 point,float blend){
            var offset=point-s.View.transform.position;
            float targetYaw=Mathf.Atan2(offset.x,offset.z)*Mathf.Rad2Deg;
            float targetPitch=-Mathf.Atan2(offset.y,new Vector2(offset.x,offset.z).magnitude)*Mathf.Rad2Deg;
            yaw=Mathf.LerpAngle(yaw,targetYaw,blend);pitch=Mathf.Lerp(pitch,Mathf.Clamp(targetPitch,-50,50),blend);
            s.Walker.transform.rotation=Quaternion.Euler(0,yaw,0);s.View.transform.localRotation=Quaternion.Euler(pitch,0,0);
        }
        public void SmokeFace(Vector3 point)
        {
            if (!d.IsSmoke) return;
            Vector3 offset=point-s.Walker.transform.position;
            yaw=Mathf.Atan2(offset.x,offset.z)*Mathf.Rad2Deg; pitch=0;
        }
        public void SmokeLook(float horizontal,float vertical){if(d.IsSmoke){yaw=horizontal;pitch=vertical;}}
    }
}
