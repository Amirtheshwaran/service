using UnityEngine;

namespace ServiceGameV2
{
    // Unity's native immediate-mode UI: no generated textures, illustrations or UI art.
    public sealed class ServiceHUD : MonoBehaviour
    {
        ServiceDirector d;
        GUIStyle title, menu, small, prompt, heading, paper, paperSmall, paperBold, button, paperButton, alarm;
        bool options;
        readonly Color ink = new Color(.17f, .18f, .17f), sheet = new Color(.84f, .83f, .77f), white = new Color(.85f, .86f, .82f);
        public void Initialize(ServiceDirector director) { d = director; }
        public void SmokeOptions(bool open){if(d.IsSmoke)options=open;}

        void Styles()
        {
            if (title != null) return;
            Font courier = Resources.Load<Font>("Fonts/CourierPrime-Regular");
            title = Style(66, white, TextAnchor.MiddleLeft);
            alarm = Style(36, white, TextAnchor.MiddleCenter);
            menu = Style(18, white, TextAnchor.MiddleLeft);
            small = Style(16, new Color(.72f, .75f, .73f), TextAnchor.MiddleLeft);
            prompt = Style(19, white, TextAnchor.MiddleCenter);
            heading = Style(27, ink, TextAnchor.MiddleLeft, courier);
            paper = Style(18, ink, TextAnchor.MiddleLeft, courier);
            paperSmall = Style(16, ink, TextAnchor.MiddleLeft, courier);
            paperBold = Style(18, ink, TextAnchor.MiddleLeft, courier); paperBold.fontStyle = FontStyle.Bold;
            button = new GUIStyle(GUI.skin.button) { font=Resources.Load<Font>("Fonts/Barlow-Regular"), fontSize = 20, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16, 12, 9, 9) };
            button.normal.textColor = white; button.hover.textColor = Color.white; button.active.textColor = white;
            button.normal.background = Texture2D.whiteTexture; button.hover.background = Texture2D.whiteTexture; button.active.background = Texture2D.whiteTexture;
            paperButton = new GUIStyle(button) { font = courier, fontSize = 15 };
            paperButton.normal.textColor = paperButton.hover.textColor = paperButton.active.textColor = ink;
        }
        GUIStyle Style(int size, Color color, TextAnchor align, Font font = null)
        {
            GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = align, wordWrap = true, font = font ? font : Resources.Load<Font>("Fonts/Barlow-Regular") };
            s.normal.textColor = color;
            return s;
        }
        void OnGUI()
        {
            if (d == null || d.Scene == null) return;
            Styles();
            Matrix4x4 before = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            if (d.Phase == ServicePhase.Title) Title();
            else if (d.Phase == ServicePhase.Paused) Pause();
            else if (d.Phase == ServicePhase.Report || d.Phase == ServicePhase.Finished) Report();
            else if (d.PaperOpen) Docket();
            else Playing();
            GUI.matrix = before;
        }
        void Rect(Rect rect, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        bool Button(Rect rect, string label, bool onPaper = false)
        {
            Color before = GUI.backgroundColor;
            bool hover=rect.Contains(Event.current.mousePosition);
            GUI.backgroundColor = onPaper ? (hover?new Color(.66f,.67f,.6f):new Color(.73f, .73f, .67f)) : (hover?new Color(.22f,.25f,.23f):new Color(.11f, .135f, .14f, .95f));
            bool clicked = GUI.Button(rect, label, onPaper ? paperButton : button);
            GUI.backgroundColor = before;
            return clicked;
        }
        void Dim(float opacity) { Rect(new Rect(0, 0, 1280, 720), new Color(.025f, .035f, .039f, opacity)); }

        void Title()
        {
            Dim(.44f);
            Rect(new Rect(0, 0, 525, 720), new Color(.025f, .035f, .039f, .76f));
            GUI.Label(new Rect(82, 126, 400, 40), "HOLLIS COUNTY / CIVIL PROCESS", small);
            GUI.Label(new Rect(77, 174, 420, 90), "SERVICE", title);
            GUI.Label(new Rect(82, 278, 340, 52), "Evening assignments\nOctober 01 — October 09", menu);
            if (options) { Options(82, 355); return; }
            if (Button(new Rect(82, 365, 315, 47), d.HasSavedRoute?"Continue route":"Begin shift")) {if(d.HasSavedRoute)d.ContinueRoute();else d.NewRoute();}
            if (Button(new Rect(82, 426, 315, 47), "Preview Vale House encounter")) {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(1);d.Player.TeleportCar(p.Gate.position+p.Door.forward*3,Quaternion.LookRotation(-p.Door.forward));d.SetCursor();}
            if (Button(new Rect(82, 487, 315, 47), "Options / controls")) options = true;
            if (Button(new Rect(82, 548, 315, 47), "Quit")) Application.Quit();
            if(d.HasSavedRoute && Button(new Rect(82, 605, 315, 37),"Start a new route"))d.NewRoute();
            GUI.Label(new Rect(82, 661, 400, 34), "Drive the route. Record each attempt. Return.", small);
        }
        void Pause()
        {
            Dim(.79f);
            GUI.Label(new Rect(110, 82, 720, 60), "SHIFT PAUSED", title);
            if (options) { Options(112, 210); return; }
            if (Button(new Rect(112, 214, 340, 47), "Resume")) d.Resume();
            if (Button(new Rect(112, 277, 340, 47), "Options / controls")) options = true;
            if (Button(new Rect(112, 340, 340, 47), "Restart this shift")) d.BeginShift(d.NightIndex);
            if (Button(new Rect(112, 403, 340, 47), "Return to title")) d.Title();
        }
        void Options(float x, float y)
        {
            GUI.Label(new Rect(x, y, 320, 27), "Master volume", menu);
            d.Audio.Volume = GUI.HorizontalSlider(new Rect(x, y + 39, 315, 20), d.Audio.Volume, 0, 1);
            GUI.Label(new Rect(x, y + 70, 320, 27), "Mouse sensitivity", menu);
            d.Player.Sensitivity = GUI.HorizontalSlider(new Rect(x, y + 109, 315, 20), d.Player.Sensitivity, .03f, .2f);
            string[] keys={"WASD","Mouse","E","R","U","F","Space","Shift","Tab","M","Esc"};
            string[] actions={"Walk / drive","Look around","Interact / enter vehicle","Leave a copy","Record unsuccessful visit","Flashlight","Start engine","Sprint / brake","Docket","County map","Pause / close document"};
            for(int i=0;i<keys.Length;i++){GUI.Label(new Rect(625,190+i*28,100,28),keys[i],menu);GUI.Label(new Rect(740,190+i*28,440,28),actions[i],menu);}
            if (Button(new Rect(x, y + 150, 315, 44), Screen.fullScreen ? "Use windowed display" : "Use full screen")) Screen.fullScreen = !Screen.fullScreen;
            if (Button(new Rect(x, y + 210, 315, 44), "Back")){d.SaveOptions();options = false;}
        }

        void PaperBase(string name, string right)
        {
            Dim(.24f);
            Rect(new Rect(203, 63, 887, 602), new Color(0, 0, 0, .3f));
            Rect(new Rect(190, 53, 887, 602), sheet);
            Rect(new Rect(190, 53, 887, 8), new Color(.32f, .33f, .29f));
            GUI.Label(new Rect(228, 82, 570, 45), name, heading);
            GUI.Label(new Rect(820, 90, 217, 35), right, paperSmall);
            Rect(new Rect(230, 137, 807, 1), ink);
        }
        void Docket()
        {
            if (d.MapOpen) { Map(); return; }
            PaperBase("HOLLIS COUNTY · FIELD DOCKET", "FORM CIV–04");
            GUI.Label(new Rect(230, 155, 790, 35), d.Date + "  /  EVENING ROUTE", paperBold);
            GUI.Label(new Rect(230, 197, 790, 30), "Record the outcome of each attempt. Return copies to the depot.", paperSmall);
            for (int i = 0; i < d.Docket.Count; i++)
            {
                DocketEntry e = d.Docket[i];
                float y = 220 + i * 64;
                GUI.Label(new Rect(230, y, 60, 35), (i + 1).ToString("00"), paperBold);
                GUI.Label(new Rect(290, y, 540, 26), e.Address, paperBold);
                GUI.Label(new Rect(290, y + 25, 700, 36), d.Property(e.Property).Brief + " · " + (e.Property==1||e.Property==5?"Deliver upstairs":"Deliver inside"), paperSmall);
                GUI.Label(new Rect(834, y, 195, 27), "[ " + Status(e.Result).ToUpperInvariant() + " ]", paperSmall);
                Rect(new Rect(230, y + 60, 807, 1), new Color(.55f, .56f, .51f));
            }
            GUI.Label(new Rect(230, 541, 800, 24), d.EndedEarly ? "ROUTE CLOSED. Remaining stops will be carried forward." : d.AllResolved ? "Route complete. Return to the depot to file this sheet." : "At a door: E attempt service · R leave copy · U unable to serve", paperSmall);
            if (Button(new Rect(229, 586, 205, 43), "County map [M]", true)) d.MapOpen = true;
            if (Button(new Rect(450, 586, 264, 43), "Return to road [TAB]", true)) { d.PaperOpen = false; d.SetCursor(); }
            if (Button(new Rect(750, 586, 287, 43), "Close route early", true)) d.RequestEarlyFinish();
        }

        void Map()
        {
            PaperBase("HOLLIS COUNTY · ROAD INDEX", "REV. 08/12");
            Rect bounds=new Rect(245,179,390,350);float minX=-112,maxX=144,minZ=-25,maxZ=410;
            Vector2 previous=Vector2.zero;bool have=false;
            foreach(var point in d.Scene.Route){if(BeyondMap(point.position))continue;var now=MapPoint(point.position,bounds,minX,maxX,minZ,maxZ);if(have)Line(previous,now,3,ink);previous=now;have=true;}
            var depot=MapPoint(d.Scene.Depot.position,bounds,minX,maxX,minZ,maxZ);GUI.Label(new Rect(depot.x+8,depot.y-13,130,27),"DEPOT",paperSmall);
            for(int i=0;i<d.Docket.Count;i++){
                var e=d.Docket[i];var property=d.Property(e.Property);float y=192+i*65;
                GUI.Label(new Rect(685,y,350,25),(i+1).ToString("00")+"  "+e.Address,paperBold);
                GUI.Label(new Rect(721,y+25,310,32),property.Brief,paperSmall);
                if(BeyondMap(property.Door.position))continue;
                var point=MapPoint(property.Door.position,bounds,minX,maxX,minZ,maxZ);Rect(new Rect(point.x-9,point.y-9,18,18),ink);
                var numeral=Style(14,sheet,TextAnchor.MiddleCenter);GUI.Label(new Rect(point.x-9,point.y-12,18,24),(i+1).ToString(),numeral);
                var gate=MapPoint(property.Gate.position,bounds,minX,maxX,minZ,maxZ);Line(point,gate,1,new Color(.4f,.42f,.37f));
            }
            var car=MapPoint(d.Scene.Car.position,bounds,minX,maxX,minZ,maxZ);Rect(new Rect(car.x-3,car.y-3,6,6),new Color(.55f,.23f,.16f));GUI.Label(new Rect(car.x+7,car.y+7,70,26),"CAR",paperSmall);
            GUI.Label(new Rect(235,537,780,32),"Roadside numbers match the docket. Park outside the garden gates.",paperSmall);
            if(Button(new Rect(230,586,250,43),"Return to docket",true))d.MapOpen=false;
            if(Button(new Rect(502,586,300,43),"Return to road [TAB]",true)){d.PaperOpen=false;d.SetCursor();}
        }
        bool BeyondMap(Vector3 point) { return d.Scene.LateThreshold != null && Vector3.Dot(point - d.Scene.LateThreshold.position, d.Scene.LateThreshold.forward) > .5f; }
        Vector2 MapPoint(Vector3 p, Rect bounds, float minX, float maxX, float minZ, float maxZ)
        {
            return new Vector2(bounds.x + Mathf.InverseLerp(minX, maxX, p.x) * bounds.width, bounds.yMax - Mathf.InverseLerp(minZ, maxZ, p.z) * bounds.height);
        }
        void Line(Vector2 a, Vector2 b, float width, Color color)
        {
            Matrix4x4 before = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, a);
            Rect(new Rect(a.x, a.y - width * .5f, Vector2.Distance(a, b), width), color);
            GUI.matrix = before;
        }

        void Playing()
        {
            string context = "";
            if (d.Busy) context = "";
            else if (d.Player.InCar)
            {
                if (d.CanFinish) context = "[E] File shift report at depot";
                else if (d.Player.Speed < .8f) context = d.Player.IsStarting ? "Starting engine…" : d.Player.EngineRunning ? "E  Exit vehicle" : "E  Exit vehicle     /     Space  Start engine";
                GUI.Label(new Rect(1030,30,220,28),"Tab  Docket     /     M  Map",small);
            }
            else if (Vector3.Distance(d.Scene.View.transform.position, d.Scene.Car.position + Vector3.up) < 3.5f) context = "[E] Get in";
            else if (d.NearbyDoor() >= 0) context = "E / R  Leave the notice";
            else if (d.NearbyDoor() >= 0) context = "[E] Knock    [R] Leave papers    [U] Unable to serve";
            else if (d.NearbyProperty()>=0) context=d.Property(d.NearbyProperty()).Instructions;
            else if (d.NearbyProperty() >= 0) context = "U  Record unsuccessful visit";
            if (context.Length > 0)
            {
                float width=Mathf.Min(900,prompt.CalcSize(new GUIContent(context)).x+40);
                Rect(new Rect(640-width/2,660,width,36), new Color(.03f, .04f, .04f, .8f));
                GUI.Label(new Rect(640-width/2,660,width,36), context, prompt);
            }
            if (!string.IsNullOrEmpty(d.Notice)&&!d.Horror.Active&&!d.Horror.Caught){Rect(new Rect(280,602,720,40),new Color(0,0,0,.65f));GUI.Label(new Rect(300,602,680,40),d.Notice,prompt);}
            if(d.Horror.Active){GUI.Label(new Rect(290,56,700,55),d.Horror.Headline,alarm);GUI.Label(new Rect(250,113,780,52),d.Horror.Instruction,prompt);}
            if(d.Horror.Caught){Dim(1);GUI.Label(new Rect(240,290,800,60),d.Horror.DeathLine,prompt);GUI.Label(new Rect(330,365,620,55),"The road brings you back.",small);}
        }

        void Report()
        {
            PaperBase("RETURN OF SERVICE", d.Date);
            GUI.Label(new Rect(230, 157, 790, 40), d.EndedEarly ? "Route closed before completion." : "Evening route filed.", paperBold);
            for (int i = 0; i < d.Docket.Count; i++)
            {
                float y = 207 + i * 53;
                GUI.Label(new Rect(230, y, 540, 32), d.Docket[i].Address, paperBold);
                GUI.Label(new Rect(230, y + 23, 730, 26), d.Docket[i].Result == ServiceResult.Pending ? "Not attempted · carry forward" : Status(d.Docket[i].Result), paperSmall);
                Rect(new Rect(230, y + 49, 807, 1), new Color(.55f, .56f, .51f));
            }
            GUI.Label(new Rect(230, 493, 780, 52), "Trip record: " + d.TripMiles.ToString("0.0") + " mi\nFiled by: route holder", paperSmall);
            GUI.Label(new Rect(230, 537, 790, 24), d.NightIndex == 2 ? "No further assignments on this sheet." : "The next docket will be issued at the start of your next shift.", paperSmall);
            if (d.NightIndex < 2)
            {
                if (Button(new Rect(230, 563, 400, 43), d.NightIndex == 0 ? "Next shift · October 04" : "Next shift · October 09", true)) d.NextShift();
            }
            else if (Button(new Rect(230, 563, 400, 43), "Return to title", true)) d.Title();
            if (Button(new Rect(652, 563, 385, 43), "Restart this shift", true)) d.BeginShift(d.NightIndex);
        }
        public static string Status(ServiceResult result)
        {
            switch (result)
            {
                case ServiceResult.Served: return "Served directly";
                case ServiceResult.LeftAtDoor: return "Copy left at property";
                case ServiceResult.Unable: return "Unable to serve";
                default: return "Pending";
            }
        }
    }
}





