using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
namespace ServiceGameV2.Editor {
// Invoke the editor's ordinary package download service; Unity retains all authentication handling.
public static class CreatureDownload {
 static object manager;static Type managerType;
 public static void Run(){
  UnityEditor.PackageManager.UI.Window.Open("244853");
  Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.PackageManager.UI.Internal."+name)).First(t=>t!=null);
  var ct=Find("ServicesContainer");managerType=Find("AssetStoreDownloadManager");
  var instance=ct.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
  manager=ct.GetMethods(BindingFlags.Instance|BindingFlags.Public).First(m=>m.Name=="Resolve"&&m.IsGenericMethod&&m.GetParameters().Length==0).MakeGenericMethod(managerType).Invoke(instance,null);
  managerType.GetMethods(BindingFlags.Instance|BindingFlags.Public).Single(m=>m.Name=="Download").Invoke(manager,new object[]{new long[]{244853}});
  File.WriteAllText(Path.GetFullPath(Application.dataPath+"/../../creature-download.txt"),"Requested through Unity Package Manager");
 }
}}
