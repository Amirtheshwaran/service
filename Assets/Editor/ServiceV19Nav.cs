using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceBuild {
  // V19: doors swing into the houses, so bake with every working door standing open. The open leaf then carves
  // the floor it occupies; the doorway itself stays walkable.
  public static void RebakeOpenDoors(CountyScene s){
   scene=s;world=s.transform;var rest=s.Properties.Where(p=>p.DoorPanel).ToDictionary(p=>p,p=>p.DoorPanel.localRotation);
   foreach(var kv in rest)kv.Key.DoorPanel.localRotation=kv.Value*Quaternion.Euler(0,kv.Key.DoorSwing,0);Physics.SyncTransforms();
   try{BakeNavigation();}finally{foreach(var kv in rest)kv.Key.DoorPanel.localRotation=kv.Value;Physics.SyncTransforms();}
  }
 }
 public static partial class ServiceV19Rebuild {
  public static void NavOpen(){Open();ServiceBuild.RebakeOpenDoors(county);Save("nav-open-doors");}
  // The authored car cabin stops a hand's width above eye level, so from the driver's seat the night sky showed where
  // the roof should be. Close it with a dark cloth headliner fitted under the cabin's roof line.
  public static void CarHeadliner(){Open();var ck=county.Cockpit?county.Cockpit.transform:null;if(!ck){log.AppendLine("HEADLINER no cockpit");Save("headliner");return;}
   var old=ck.Find("V19 headliner");if(old)Object.DestroyImmediate(old.gameObject);var seat=county.DriverSeat;
   var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/ServiceArt/V19/V19 headliner.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,"Assets/ServiceArt/V19/V19 headliner.mat");}
   mat.SetColor("_BaseColor",new Color(.16f,.155f,.15f));mat.SetFloat("_Smoothness",.08f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);
   var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="V19 headliner";Object.DestroyImmediate(g.GetComponent<Collider>());g.layer=ck.gameObject.layer;g.GetComponent<Renderer>().sharedMaterial=mat;
   g.transform.SetParent(ck,true);g.transform.position=seat.TransformPoint(new Vector3(.375f,.16f,-.4f));g.transform.rotation=seat.rotation;g.transform.localScale=Vector3.one;
   var lossy=g.transform.lossyScale;g.transform.localScale=new Vector3(1.86f/lossy.x,.03f/lossy.y,2.5f/lossy.z);
   log.AppendLine($"HEADLINER at {g.transform.position} spanning seat-local z -1.65..0.85, y 0.145..0.175");Save("headliner");}
  public static void BatchE(){CarHeadliner();NavOpen();}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void BatchF(){CarHeadliner();Hands();NavOpen();ServiceV17Apply.Build();}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // What does the driver actually see in the top of the frame? Temporary mesh colliders on every car renderer,
  // then rays from the in-car camera pose through the upper screen.
  public static void CarSightProbe(){
   Open();var seat=county.DriverSeat;var added=new System.Collections.Generic.List<Component>();var sb=new System.Text.StringBuilder();
   foreach(var mf in county.Car.GetComponentsInChildren<MeshFilter>(true)){if(!mf.sharedMesh||!mf.GetComponent<Renderer>()||!mf.GetComponent<Renderer>().enabled)continue;var mc=mf.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=mf.sharedMesh;added.Add(mc);}
   Physics.SyncTransforms();var cam=new GameObject("probe cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(5,0,0));cam.aspect=16f/9f;
   int mask=county.View.cullingMask&~(1<<8);sb.AppendLine($"in-car culling mask {System.Convert.ToString(mask,2)} fov {cam.fieldOfView}");
   foreach(float vy in new[]{.98f,.94f,.9f,.86f,.82f,.78f,.74f})foreach(float vx in new[]{.15f,.5f,.85f}){var ray=cam.ViewportPointToRay(new Vector3(vx,vy,0));var hits=Physics.RaycastAll(ray,40,~0,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance).Take(4);
    sb.AppendLine($"vp({vx:F2},{vy:F2}) dir {ray.direction:F2}: "+string.Join(" | ",hits.Select(h=>$"{h.collider.name} L{h.collider.gameObject.layer} {(((1<<h.collider.gameObject.layer)&mask)!=0?"VIS":"hid")} d{h.distance:F2}")));}
   foreach(var c in added)Object.DestroyImmediate(c);Object.DestroyImmediate(cam.gameObject);
   System.IO.File.WriteAllText(System.IO.Path.Combine(Work,"Audit","car-sight.txt"),sb.ToString());
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  public static void CabinMats(){Open();var sb=new System.Text.StringBuilder();var ck=county.Cockpit.transform;
   foreach(var n in new[]{"Cylinder.007","Cylinder.011","Cube.020","Cylinder.008","Cylinder.012"}){var t=ck.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name==n);if(!t)continue;var r=t.GetComponent<Renderer>();
    foreach(var m in r.sharedMaterials){if(!m)continue;sb.AppendLine($"{n}: {m.name} shader {m.shader.name} base {(m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString():"-")} tex {(m.HasProperty("_BaseMap")&&m.GetTexture("_BaseMap")?m.GetTexture("_BaseMap").name:"-")} emis {(m.IsKeywordEnabled("_EMISSION")?m.GetColor("_EmissionColor").ToString():"off")} surface {(m.HasProperty("_Surface")?m.GetFloat("_Surface"):-1)} cull {(m.HasProperty("_Cull")?m.GetFloat("_Cull"):-1)}");}}
   foreach(var l in county.Car.GetComponentsInChildren<Light>(true))sb.AppendLine($"LIGHT {l.name} {l.type} int {l.intensity} range {l.range} at {county.DriverSeat.InverseTransformPoint(l.transform.position)} enabled {l.enabled}");
   System.IO.File.WriteAllText(System.IO.Path.Combine(Work,"Audit","cabin-mats.txt"),sb.ToString());}
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // The "dashboard ambient spill" light sat at the driver's head, 16 cm under the charcoal roof lining, so the lining
  // glowed as a bright band across the top of the in-car view. Seat it down at the instrument cluster instead.
  public static void DashLight(){Open();var seat=county.DriverSeat;var ck=county.Cockpit?county.Cockpit.transform:null;var hl=ck?ck.Find("V19 headliner"):null;if(hl)Kill(hl,"not needed: the band was a light, the cabin has its own lining");
   foreach(var l in county.Car.GetComponentsInChildren<Light>(true).Where(l=>l.name=="Dashboard ambient spill")){l.transform.position=seat.TransformPoint(new Vector3(.3f,-.36f,.62f));l.range=1.0f;l.intensity=.45f;log.AppendLine("DASH light moved to the instrument cluster "+l.transform.position);}
   Save("dash-light");}
  public static void BatchG(){DashLight();ServiceV17Apply.Build();}
  // The low beams were tipped 7 degrees down from 0.65 m, so their pool landed 5 m ahead, under the hood, and the
  // driver saw an unlit road. Aim them like real dipped beams: about 2 degrees down, reaching 15-30 m.
  public static void Headlights(){Open();
   foreach(var l in county.Car.GetComponentsInChildren<Light>(true).Where(l=>l.name=="Low beam")){l.transform.localRotation=Quaternion.Euler(2.2f,0,0);l.spotAngle=60;l.innerSpotAngle=26;l.range=70;l.intensity=16;log.AppendLine($"BEAM {l.transform.localPosition} aimed 2.2 deg down, intensity {l.intensity}");}
   Save("headlights");}
  public static void BatchH(){Headlights();ServiceV17Apply.Build();}
  // At the cluster the spill light was 10 cm from the radio face and blew it out. It is only a fill: sit it above the
  // wheel, 40 cm from the dash, faint enough that the gauges' own glow stays the brightest thing in the cabin.
  public static void DashFill(){Open();var seat=county.DriverSeat;
   foreach(var l in county.Car.GetComponentsInChildren<Light>(true).Where(l=>l.name=="Dashboard ambient spill")){l.transform.position=seat.TransformPoint(new Vector3(.22f,-.1f,.38f));l.range=.8f;l.intensity=.1f;log.AppendLine("DASH fill "+l.transform.position+" intensity "+l.intensity);}
   Save("dash-fill");}
  public static void BatchI(){DashFill();ServiceV17Apply.Build();}
  // Where do the beams land? One shot from above and behind the car, one from the driver's eye with the exterior
  // body culled exactly as the game does in the car. The car is moved onto a straight stretch of the road first.
  public static void BeamShots(){Open();var car=county.Car;var keep=(car.position,car.rotation);
   var route=county.Route.Where(t=>t).Select(t=>t.position).ToList();int i=route.Count/3;var a=route[i];var b=route[i+1];var dir=b-a;dir.y=0;
   car.SetPositionAndRotation(a+Vector3.Cross(Vector3.up,dir.normalized)*1.6f+Vector3.up*.05f,Quaternion.LookRotation(dir));Physics.SyncTransforms();
   var muted=MuteFeatures();try{var cam=new GameObject("beam cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
    cam.cullingMask=~0;cam.transform.SetPositionAndRotation(car.TransformPoint(new Vector3(0,4.5f,-7)),car.rotation*Quaternion.Euler(18,0,0));Shoot(cam,System.IO.Path.Combine(Work,"Audit","beam-above.png"),960,540);
    cam.cullingMask=~(1<<8);var seat=county.DriverSeat;cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(4,0,0));if(county.Cockpit)county.Cockpit.SetActive(true);Shoot(cam,System.IO.Path.Combine(Work,"Audit","beam-driver.png"),960,540);
    Object.DestroyImmediate(cam.gameObject);}finally{Restore(muted);car.SetPositionAndRotation(keep.position,keep.rotation);}
   log.AppendLine("BEAM shots at route point "+i);}
 }
}
