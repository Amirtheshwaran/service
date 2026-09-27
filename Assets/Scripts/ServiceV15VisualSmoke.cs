using System.Collections;using System.IO;using UnityEngine;
namespace ServiceGameV2 {public sealed class ServiceV15VisualSmoke:MonoBehaviour {
 ServiceDirector d;string dir;public void Run(ServiceDirector director){d=director;dir=System.Environment.GetEnvironmentVariable("SERVICE_CAPTURE_DIR");Directory.CreateDirectory(dir);StartCoroutine(Check());}
 IEnumerator Shot(string name){yield return new WaitForSeconds(.4f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));yield return new WaitForSeconds(.2f);}
 IEnumerator Check(){yield return new WaitForSeconds(1);d.BeginShift(0);d.PaperOpen=false;var dog=d.Scene.transform.Find("Correll yard dog");d.Player.SmokePlaceWalker(dog.position+d.Property(0).Door.forward*3);d.Player.SmokeFace(dog.position);yield return null;d.Player.SmokeLook(d.Scene.Walker.transform.eulerAngles.y,20);d.Scene.Flashlight.enabled=true;yield return new WaitForSeconds(1);d.Life.Bark();yield return Shot("dog");
 foreach(var r in dog.GetComponentsInChildren<SkinnedMeshRenderer>()){
  var mesh=new Mesh();r.BakeMesh(mesh);var b=new Bounds(r.transform.TransformPoint(mesh.vertices[0]),Vector3.zero);foreach(var v in mesh.vertices)b.Encapsulate(r.transform.TransformPoint(v));
  Debug.Log("V15_DOG bounds="+r.bounds+" baked="+b+" root="+dog.position+" camera="+d.Scene.View.transform.position+" screen="+d.Scene.View.WorldToViewportPoint(b.center)+" scale="+r.transform.lossyScale+" visible="+r.isVisible);
  var mat=r.material;if(mat.HasProperty("_Cull"))mat.SetFloat("_Cull",0);
  d.Player.SmokeFace(b.center);yield return null;d.Player.SmokeLook(d.Scene.Walker.transform.eulerAngles.y,20);
 }yield return Shot("dog-two-sided");
 d.Player.TeleportCar(d.Scene.Depot.position,Quaternion.identity);d.Player.SmokePlaceWalker(d.Scene.Car.position-Vector3.forward*5);d.Player.SmokeFace(d.Scene.Car.position);yield return Shot("car-rear");d.Player.EnterCar();d.Player.SmokeLook(0,24);yield return Shot("cabin");
 d.Player.SmokePlaceWalker(new Vector3(-4,0,375));d.Player.SmokeFace(new Vector3(-10,1,385));yield return Shot("return-junction");d.Player.SmokePlaceWalker(new Vector3(-32,0,180));d.Player.SmokeFace(new Vector3(-32,1,150));yield return Shot("return-lane");Application.Quit();}
}}
