using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static class ServiceV6Import {
  public static void Run(){
   var cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Unity/Asset Store-5.x");
   foreach(var name in new[]{"Conifers BOTD.unitypackage","Demon Horror Creature with Weapon.unitypackage"})
    AssetDatabase.ImportPackage(Directory.GetFiles(cache,name,SearchOption.AllDirectories).Single(),false);
   AssetDatabase.Refresh();
  }
 }
}
