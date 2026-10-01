using System.IO;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static class ServiceV19Setup {
  const string Store=@"C:\Users\Amirthesh\AppData\Roaming\Unity\Asset Store-5.x\";
  public static void ImportPackages(){
   foreach(var p in new[]{@"AndaSoft\3D ModelsCharacters\EasyRoads3D Free v3.unitypackage",@"Jake Sullivan\3D ModelsPropsInterior\Kitchen Props Free.unitypackage"}){
    var full=Store+p;Debug.Log("V19 import "+full+" exists="+File.Exists(full));if(File.Exists(full))AssetDatabase.ImportPackage(full,false);
   }
   AssetDatabase.Refresh();
  }
 }
}
