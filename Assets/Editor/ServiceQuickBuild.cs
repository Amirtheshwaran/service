using System.IO;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static class ServiceQuickBuild {
  public static void Build(){
   var root=Directory.GetParent(Application.dataPath).Parent.FullName;
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/HollisCounty.unity"},locationPathName=Path.Combine(root,"Build/Service.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   File.WriteAllText(Path.Combine(root,"build-summary.txt"),report.summary.result+" / errors "+report.summary.totalErrors+" / warnings "+report.summary.totalWarnings);
   if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed");
  }
 }
}
