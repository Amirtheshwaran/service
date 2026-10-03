using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V21 final review: Vale's encyclopedia row stood across the middle of the hall table, so the papers set down there
  // lay hidden behind it from where the player stands. The row goes to the back of the table, against the wall.
  public static void Fixes21(){Open();Fixes21Stage();Save("fixes21");}
  // The dice hang 23 cm from the dashboard spill (a point light; URP's clustered lighting ignores its layer mask), which
  // lit white dice into glowing blobs. Compared in game (Audit/tour00f/dice-sheet.jpg): white at .34 blows out, ambient
  // only (Baked Lit) leaves black shapes; a dim grey albedo reads as soft fuzz in the spill.
  static void DiceMaterials21(){const string dir="Assets/ServiceArt/V21/FuzzyDice";var lit=Shader.Find("Universal Render Pipeline/Lit");
   foreach(var (name,col) in new[]{("V21 fuzzy die",new Color(.17f,.165f,.158f)),("V21 die fuzz",new Color(.155f,.15f,.144f))}){var m=AssetDatabase.LoadAssetAtPath<Material>($"{dir}/{name}.mat");if(!m||!lit)continue;
    var tex=m.GetTexture("_BaseMap");m.shader=lit;if(tex)m.SetTexture("_BaseMap",tex);m.SetColor("_BaseColor",col);m.SetFloat("_Smoothness",0);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);log.AppendLine($"FIXES21 {name}: Lit, colour {col}");}
   AssetDatabase.SaveAssets();}
  static void Fixes21Stage(){
   // every delivery table: a row of books standing over the delivery spot goes to the back edge, away from the player
   foreach(var v in county.Properties){if(!v.DeliveryPoint||!v.TableApproach)continue;var dp=v.DeliveryPoint.position;
    Bounds Of(Transform t){var rs=t.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    var desk=v.transform.GetComponentsInChildren<Renderer>(true).Where(r=>{var b=r.bounds;return b.max.y<dp.y+.03f&&b.max.y>dp.y-.06f&&b.size.y>.15f&&dp.x>b.min.x&&dp.x<b.max.x&&dp.z>b.min.z&&dp.z<b.max.z;}).OrderBy(r=>r.bounds.size.x*r.bounds.size.z).FirstOrDefault();
    if(!desk)continue;var db=desk.bounds;
    foreach(var books in v.transform.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("books")&&t.parent&&!t.parent.name.Contains("books")).ToArray()){
     var bb=Of(books);if(bb.min.y<db.max.y-.05f||bb.min.y>db.max.y+.05f||bb.max.x<db.min.x||bb.min.x>db.max.x||bb.max.z<db.min.z||bb.min.z>db.max.z)continue;
     var away=new Vector3(db.center.x-v.TableApproach.position.x,0,db.center.z-v.TableApproach.position.z);
     bool alongX=Mathf.Abs(away.x)>Mathf.Abs(away.z);float sign=alongX?Mathf.Sign(away.x):Mathf.Sign(away.z);
     float target=alongX?(sign>0?db.max.x-.035f-bb.max.x:db.min.x+.035f-bb.min.x):(sign>0?db.max.z-.035f-bb.max.z:db.min.z+.035f-bb.min.z);
     var shift=alongX?new Vector3(target,0,0):new Vector3(0,0,target);
     if(Vector3.Dot(shift,away)>0){books.position+=shift;EditorUtility.SetDirty(books);}
     log.AppendLine($"FIXES21 p{v.Index} '{books.name}' {bb.min}..{bb.max} on desk '{desk.name}' {db.min}..{db.max}, moved {(Vector3.Dot(shift,away)>0?shift:Vector3.zero)}");}
   }
   // the dice hang beside the dashboard spill light (layer 10 only), which lit them into white blobs at night: they
   // move to the default layer, lit by the night like the rest of the view through the windscreen
   var diceRoot=county.Car.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.StartsWith("Fuzzy dice"));
   if(diceRoot){foreach(var t in diceRoot.GetComponentsInChildren<Transform>(true))t.gameObject.layer=0;log.AppendLine("FIXES21 dice on layer 0");}
   DiceMaterials21();
   // what lights the dice
   var dice=county.Car.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.StartsWith("Fuzzy dice"));
   if(dice)foreach(var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>l.type!=LightType.Directional&&Vector3.Distance(l.transform.position,dice.position)<Mathf.Max(l.range,1)))
    log.AppendLine($"FIXES21 light near dice: '{PathOf(l.transform)}' {l.type} int {l.intensity} range {l.range} dist {Vector3.Distance(l.transform.position,dice.position):F2} enabled {l.enabled}/{l.gameObject.activeInHierarchy} mask {l.cullingMask}");
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Driving arms as the head turns: baked (batch renders do not re-skin), photographed from the eye at the game's
  // in-car yaw/pitch, with and without the dashboard spill light; and which bones the vertices in view belong to.
  public static void DriveLook21(){Open();var seat=county.DriverSeat;var dir=Path.Combine(Work,"Audit","drivelook21");Directory.CreateDirectory(dir);var muted=MuteFeatures();
   var arms=seat.GetComponentsInChildren<ServiceDrivingHands>(true).First();var anim=arms.GetComponent<Animation>();var smr=arms.GetComponentInChildren<SkinnedMeshRenderer>();
   var cam=new GameObject("dl cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~(1<<8);
   var torchVm=county.View.transform.Find("V19 viewmodel");var hidden=torchVm?torchVm.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray():new Renderer[0];foreach(var r in hidden)r.enabled=false;
   var spill=county.Car.GetComponentsInChildren<Light>(true).FirstOrDefault(l=>l.name.Contains("ambient spill"));var sb=new System.Text.StringBuilder();
   var key=new GameObject("dl key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=.25f;key.transform.rotation=Quaternion.Euler(50,30,0);
   try{if(county.Cockpit)county.Cockpit.SetActive(true);
    foreach(var k in new[]{4,8,0}){anim.GetClip(arms.Clips[k]).SampleAnimation(arms.gameObject,0);
     var baked=new Mesh();smr.BakeMesh(baked,true);var bo=new GameObject("baked");bo.transform.SetParent(smr.transform,false);bo.AddComponent<MeshFilter>().sharedMesh=baked;bo.AddComponent<MeshRenderer>().sharedMaterials=smr.sharedMaterials;bo.layer=smr.gameObject.layer;smr.enabled=false;
     try{foreach(var yaw in new[]{0f,45f,65f,90f,-60f}){cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(4,yaw,0));
       if(spill)spill.enabled=true;Shoot(cam,Path.Combine(dir,$"d{k}-y{yaw:+0;-0;0}.png"),640,360);
       if(spill)spill.enabled=false;Shoot(cam,Path.Combine(dir,$"d{k}-y{yaw:+0;-0;0}-nospill.png"),640,360);if(spill)spill.enabled=true;
       // vertices in view, by dominant bone
       var counts=new System.Collections.Generic.Dictionary<string,int>();var bw=smr.sharedMesh.boneWeights;var vs=baked.vertices;
       for(int i=0;i<vs.Length;i++){var wp=bo.transform.TransformPoint(vs[i]);var vp=cam.WorldToViewportPoint(wp);if(vp.z<cam.nearClipPlane||vp.x<0||vp.x>1||vp.y<0||vp.y>1)continue;var bn=smr.bones[bw[i].boneIndex0].name;counts[bn]=counts.TryGetValue(bn,out var c)?c+1:1;}
       sb.AppendLine($"Drive_{k} yaw {yaw}: "+string.Join(", ",counts.OrderByDescending(x=>x.Value).Select(x=>x.Key+" "+x.Value)));}}
     finally{smr.enabled=true;Object.DestroyImmediate(bo);Object.DestroyImmediate(baked);}}
    foreach(var t in arms.GetComponentsInChildren<Transform>(true))if(t.name.StartsWith("shoulder")||t.name.StartsWith("upper_arm"))sb.AppendLine($"{t.name} seat-local {seat.InverseTransformPoint(t.position)}");
    sb.AppendLine($"spill {(spill?spill.transform.position.ToString()+" seat-local "+seat.InverseTransformPoint(spill.transform.position):"none")}; cam fov {cam.fieldOfView} near {cam.nearClipPlane}");
   }finally{foreach(var r in hidden)r.enabled=true;Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(key.gameObject);Restore(muted);}
   File.WriteAllText(Path.Combine(dir,"drivelook21.txt"),sb.ToString());}
 }
}
