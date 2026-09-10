using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ServiceGameV2.Editor
{
    [InitializeOnLoad]
    public static class ServiceTools
    {
        static string Work => Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        static ServiceTools() { EditorApplication.update += Tick; }
        public static void OpenCreature(){UnityEditor.PackageManager.UI.Window.Open("244853");}
        static void Tick()
        {
            string job=Path.Combine(Work,"editor-job.txt");
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(job))return;
            string command=File.ReadAllText(job).Trim(); File.Delete(job);
            try {
                if(command=="inventory") Inventory();
                if(command=="v5inspect") V5Inspect.Run();
                if(command=="build") ServiceBuild.Build();
                if(command=="quit") EditorApplication.Exit(0);
                if(command=="packages") UnityEditor.PackageManager.UI.Window.Open("244853");
                if(command=="navigation") V3Regression.Navigation();
                if(command=="walk") V3Regression.Walk();
                if(command=="creature") CreatureDownload.Run();
                if(command=="import-creature") AssetDatabase.ImportPackage(Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Unity/Asset Store-5.x/AC Game Assets"),"*.unitypackage",SearchOption.AllDirectories).Single(),false);
                if(command=="regression") V3Regression.Check();
                if(command=="props") InspectArt.Props();
                if(command=="scene"){ServiceBuild.SceneOnly=true;ServiceBuild.Build();ServiceBuild.SceneOnly=false;}
                File.WriteAllText(Path.Combine(Work,"job-result.txt"),"OK "+command);
            }catch(Exception ex){Debug.LogException(ex);File.WriteAllText(Path.Combine(Work,"job-result.txt"),ex.ToString());}
        }
        static Bounds BoundsOf(GameObject o)
        {
            var rr=o.GetComponentsInChildren<Renderer>();
            var b=rr.Length>0?rr[0].bounds:new Bounds(o.transform.position,Vector3.zero);
            foreach(var r in rr)b.Encapsulate(r.bounds);return b;
        }
        public static void Inventory()
        {
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene("Assets/Flooded_Grounds/Scenes/PreAsembeld_Buildings.unity");
            var lines=new List<string>();
            foreach(var root in scene.GetRootGameObjects())
            {
                lines.Add(root.name+" | "+root.transform.position+" | "+BoundsOf(root));
                foreach(Transform child in root.transform) lines.Add("  "+child.name+" | "+child.position+" | "+BoundsOf(child.gameObject));
            }
            File.WriteAllLines(Path.Combine(Work,"buildings.txt"),lines);
            var details=new List<string>();
            foreach(var root in scene.GetRootGameObjects().Where(o=>o.name.Contains("Cabin")))
                foreach(var tr in root.GetComponentsInChildren<Transform>())
                    details.Add(root.name+" / "+tr.name+" local "+root.transform.InverseTransformPoint(tr.position)+" rot "+tr.localEulerAngles+" bounds "+BoundsOf(tr.gameObject));
            File.WriteAllLines(Path.Combine(Work,"cabin-details.txt"),details);
            var list=new List<string>();
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Flooded_Grounds/Prefabs"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var o=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(o)list.Add(path+" | "+BoundsOf(o));
            }
            File.WriteAllLines(Path.Combine(Work,"prefabs.txt"),list);
        }
    }
}
