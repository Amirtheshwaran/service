using UnityEngine;

namespace ServiceGameV2
{
    // Unity's native immediate-mode UI: no generated textures, illustrations or UI art.
    public sealed class ServiceHUD : MonoBehaviour
    {
        ServiceDirector d;
        GUIStyle title, menu, small, prompt, heading, paper, paperSmall, paperBold, button, paperButton;
        bool options;
        readonly Color ink = new Color(.17f, .18f, .17f), sheet = new Color(.84f, .83f, .77f), white = new Color(.85f, .86f, .82f);
        public void Initialize(ServiceDirector director) { d = director; }

        void Styles()
        {
            if (title != null) return;
            Font courier = Resources.Load<Font>("Fonts/CourierPrime-Regular");
            title = Style(60, white, TextAnchor.MiddleLeft, courier);
            menu = Style(18, white, TextAnchor.MiddleLeft);
            small = Style(13, new Color(.65f, .69f, .68f), TextAnchor.MiddleLeft);
            prompt = Style(15, white, TextAnchor.MiddleCenter);
            heading = Style(27, ink, TextAnchor.MiddleLeft, courier);
            paper = Style(18, ink, TextAnchor.MiddleLeft, courier);
            paperSmall = Style(14, ink, TextAnchor.MiddleLeft, courier);
            paperBold = Style(18, ink, TextAnchor.MiddleLeft, courier); paperBold.fontStyle = FontStyle.Bold;
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16, 12, 9, 9) };
            button.normal.textColor = white; button.hover.textColor = Color.white; button.active.textColor = white;
            button.normal.background = Texture2D.whiteTexture; button.hover.background = Texture2D.whiteTexture; button.active.background = Texture2D.whiteTexture;
            paperButton = new GUIStyle(button) { font = courier, fontSize = 15 };
            paperButton.normal.textColor = paperButton.hover.textColor = paperButton.active.textColor = ink;
        }
        GUIStyle Style(int size, Color color, TextAnchor align, Font font = null)
        {
            GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = align, wordWrap = true, font = font };
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
            GUI.backgroundColor = onPaper ? new Color(.73f, .73f, .67f) : new Color(.11f, .135f, .14f, .95f);
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
            if (Button(new Rect(82, 365, 315, 47), "Begin October 01")) d.BeginShift(0);
            if (Button(new Rect(82, 426, 315, 47), "Preview Vale House encounter")) {d.BeginShift(0);d.PaperOpen=false;var p=d.Property(1);d.Player.TeleportCar(p.Gate.position+p.Door.forward*3,Quaternion.LookRotation(-p.Door.forward));d.SetCursor();}
            if (Button(new Rect(82, 487, 315, 47), "Options / controls")) options = true;
            if (Button(new Rect(82, 548, 315, 47), "Quit")) Application.Quit();
            GUI.Label(new Rect(82, 632, 400, 34), "Drive the route. Record each attempt. Return.", small);
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
            GUI.Label(new Rect(x, y, 320, 27), "Recorded sound volume", menu);
            d.Audio.Volume = GUI.HorizontalSlider(new Rect(x, y + 39, 315, 20), d.Audio.Volume, 0, 1);
            GUI.Label(new Rect(x, y + 70, 320, 27), "Mouse sensitivity", menu);
            d.Player.Sensitivity = GUI.HorizontalSlider(new Rect(x, y + 109, 315, 20), d.Player.Sensitivity, .03f, .2f);
            GUI.Label(new Rect(625, 185, 460, 330), "W A S D    Walk / drive\nMOUSE      Look\nE          Door / car / depot\nR          Leave papers / table delivery\nU          Mark unable to serve\nF          Flashlight\nSPACE      Turn ignition key\nSHIFT      Sprint on foot / brake in car\nTAB        Docket in car\nM          County map in car\nESC        Pause / close paper", menu);
            if (Button(new Rect(x, y + 150, 315, 44), Screen.fullScreen ? "Use windowed display" : "Use full screen")) Screen.fullScreen = !Screen.fullScreen;
            if (Button(new Rect(x, y + 210, 315, 44), "Back")) options = false;
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
                float y = 252 + i * 115;
                GUI.Label(new Rect(230, y, 60, 35), (i + 1).ToString("00"), paperBold);
                GUI.Label(new Rect(290, y, 620, 31), e.Address, paperBold);
                GUI.Label(new Rect(290, y + 31, 670, 31), e.Case, paperSmall);
                GUI.Label(new Rect(290, y + 60, 670, 27), "[ " + Status(e.Result).ToUpperInvariant() + " ]", paperSmall);
                Rect(new Rect(230, y + 99, 807, 1), new Color(.55f, .56f, .51f));
            }
            GUI.Label(new Rect(230, 497, 800, 51), d.EndedEarly ? "ROUTE CLOSED. Remaining stops will be carried forward." : d.AllResolved ? "Route complete. Return to the depot to file this sheet." : "At a door: E attempt service · R leave copy · U unable to serve", paperSmall);
            if (Button(new Rect(229, 563, 205, 43), "County map [M]", true)) d.MapOpen = true;
            if (Button(new Rect(450, 563, 264, 43), "Return to road [TAB]", true)) { d.PaperOpen = false; d.SetCursor(); }
            if (Button(new Rect(750, 563, 287, 43), "Close route early", true)) d.RequestEarlyFinish();
        }

        void Map()
        {
            PaperBase("HOLLIS COUNTY · ROAD INDEX", "REV. 08/12");
            GUI.Label(new Rect(230, 153, 780, 30), "Depot / Millbrook Road / Latigo Trail / County Route 9", paperSmall);
            Rect bounds = new Rect(260, 210, 540, 300);
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Transform point in d.Scene.Route)
            {
                if (BeyondMap(point.position)) continue;
                minX = Mathf.Min(minX, point.position.x); maxX = Mathf.Max(maxX, point.position.x);
                minZ = Mathf.Min(minZ, point.position.z); maxZ = Mathf.Max(maxZ, point.position.z);
            }
            if (float.IsInfinity(minX) || minX == float.MaxValue) { minX = -60; maxX = 60; minZ = -30; maxZ = 300; }
            minX -= 55; maxX += 55; minZ -= 20; maxZ += 20;
            Vector2 previous = Vector2.zero; bool havePrevious = false;
            foreach (Transform point in d.Scene.Route)
            {
                if (BeyondMap(point.position)) continue;
                Vector2 now = MapPoint(point.position, bounds, minX, maxX, minZ, maxZ);
                if (havePrevious) Line(previous, now, 3, ink);
                previous = now; havePrevious = true;
            }
            Vector2 depot = MapPoint(d.Scene.Depot.position, bounds, minX, maxX, minZ, maxZ);
            Rect(new Rect(depot.x - 4, depot.y - 4, 8, 8), ink);
            GUI.Label(new Rect(depot.x + 10, depot.y - 13, 170, 26), "DEPOT", paperSmall);
            for (int i = 0; i < 2; i++)
            {
                Vector2 p = MapPoint(d.Property(i).Door.position, bounds, minX, maxX, minZ, maxZ);
                Rect(new Rect(p.x - 3, p.y - 3, 6, 6), ink);
                GUI.Label(new Rect(p.x + 12, p.y - 15, 200, 46), i == 0 ? "214 MILLBROOK" : "77 LATIGO", paperSmall);
            }
            if (d.Scene.LateThreshold != null)
            {
                Vector2 end = MapPoint(d.Scene.LateThreshold.position, bounds, minX, maxX, minZ, maxZ);
                Line(end + Vector2.left * 7, end + Vector2.right * 7, 2, ink);
                GUI.Label(new Rect(end.x + 16, end.y - 14, 250, 46), "END COUNTY MAINTENANCE", paperSmall);
            }
            if (!BeyondMap(d.Scene.Car.position))
            {
                Vector2 car = MapPoint(d.Scene.Car.position, bounds, minX, maxX, minZ, maxZ);
                Rect(new Rect(car.x - 4, car.y - 4, 8, 8), new Color(.36f, .42f, .37f));
                GUI.Label(new Rect(car.x - 53, car.y - 14, 52, 26), "CAR", paperSmall);
            }
            GUI.Label(new Rect(850, 211, 145, 35), "N", paperBold);
            Line(new Vector2(858, 269), new Vector2(858, 245), 2, ink);
            GUI.Label(new Rect(837, 338, 190, 110), "County survey\n\nUnmaintained roads are not shown.", paperSmall);
            GUI.Label(new Rect(230, 516, 800, 31), d.NightIndex == 2 ? "Docket addendum: 1 County Route 9. No surveyed parcel reference." : "Use posted house numbers. Road distances are approximate.", paperSmall);
            if (Button(new Rect(230, 563, 250, 43), "Return to docket", true)) d.MapOpen = false;
            if (Button(new Rect(502, 563, 300, 43), "Return to road [TAB]", true)) { d.PaperOpen = false; d.SetCursor(); }
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
                else if (d.Player.Speed < .8f) context = d.Player.IsStarting ? "Starting…" : "[E] Step out    [SPACE] Turn key    [TAB] Docket    [M] Map";
                else context = "[TAB] Docket    [M] County map";
            }
            else if (Vector3.Distance(d.Scene.View.transform.position, d.Scene.Car.position + Vector3.up) < 3.5f) context = "[E] Get in";
            else if (d.NearbyDoor() == 1) context = "[E / R] Put notice on table";
            else if (d.NearbyDoor() >= 0) context = "[E] Knock    [R] Leave papers    [U] Unable to serve";
            else if (d.NearbyProperty() >= 0) context = "[U] Mark unable to serve";
            if (context.Length > 0)
            {
                Rect(new Rect(260, 641, 760, 34), new Color(.03f, .04f, .04f, .72f));
                GUI.Label(new Rect(270, 641, 740, 34), context, prompt);
            }
            if (!string.IsNullOrEmpty(d.Notice)) GUI.Label(new Rect(260, 599, 760, 35), d.Notice, prompt);
            if(d.Horror!=null&&d.Horror.Active){GUI.Label(new Rect(430,74,600,68),d.Horror.Phase==PursuitPhase.Chase?"RUN":"",title);GUI.Label(new Rect(300,145,680,30),d.Player.InCar?"Turn the key. Drive.":"Hold SHIFT to run. Get back to your car.",prompt);}
            if(d.Horror!=null&&d.Horror.Caught){Dim(1);GUI.Label(new Rect(220,300,950,80),"YOU WERE NOT ALONE",title);GUI.Label(new Rect(330,390,620,55),"Returning to the villa gate…",prompt);}
        }

        void Report()
        {
            PaperBase("RETURN OF SERVICE", d.Date);
            GUI.Label(new Rect(230, 157, 790, 40), d.EndedEarly ? "Route closed before completion." : "Evening route filed.", paperBold);
            for (int i = 0; i < d.Docket.Count; i++)
            {
                float y = 237 + i * 85;
                GUI.Label(new Rect(230, y, 540, 32), d.Docket[i].Address, paperBold);
                GUI.Label(new Rect(230, y + 31, 730, 29), d.Docket[i].Result == ServiceResult.Pending ? "Not attempted · carry forward" : Status(d.Docket[i].Result), paperSmall);
                Rect(new Rect(230, y + 70, 807, 1), new Color(.55f, .56f, .51f));
            }
            GUI.Label(new Rect(230, 438, 780, 52), "Trip record: " + d.TripMiles.ToString("0.0") + " mi\nFiled by: route holder", paperSmall);
            GUI.Label(new Rect(230, 505, 790, 35), d.NightIndex == 2 ? "No further assignments on this sheet." : "The next docket will be issued at the start of your next shift.", paperSmall);
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
