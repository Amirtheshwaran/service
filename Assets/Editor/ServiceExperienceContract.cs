using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static class ServiceExperienceContract {
  public static void Run(){
   var assembly=typeof(ServicePlayer).Assembly;
   var map=assembly.GetType("ServiceGameV2.ServiceRouteMap");if(map==null)throw new Exception("Map projection contract missing");
   var project=map.GetMethod("Project",BindingFlags.Public|BindingFlags.Static);var bounds=new Rect(-100,-40,260,500);var panel=new Rect(40,80,560,400);
   Vector2 bottom=(Vector2)project.Invoke(null,new object[]{new Vector3(-100,0,-40),bounds,panel});Vector2 top=(Vector2)project.Invoke(null,new object[]{new Vector3(160,0,460),bounds,panel});
   if(bottom.x<panel.x||top.x>panel.xMax||bottom.y<=top.y)throw new Exception("Map containment/north orientation failed");
   if(Mathf.Abs((top.x-bottom.x)/(bottom.y-top.y)-260f/500)> .01f)throw new Exception("Map must preserve world aspect ratio");
   foreach(string property in new[]{"Crouched","CameraMotion","MotionBlurAmount"})if(typeof(ServicePlayer).GetProperty(property)==null&&typeof(ServicePlayer).GetField(property)==null)throw new Exception("Movement contract missing: "+property);
   if(typeof(ServiceDirector).GetMethod("NearbyKnockDoor")==null)throw new Exception("Knocking unavailable at front door");
   if(assembly.GetType("ServiceGameV2.ServiceVehiclePresentation")==null)throw new Exception("Radio/mirror integration missing");
   File.WriteAllText(Path.GetFullPath(Application.dataPath+"/../../experience-contract.txt"),"PASS: map bounds/aspect; movement options; knocking; vehicle integration");
  }
 }
}
