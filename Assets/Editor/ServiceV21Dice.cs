using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Fuzzy dice (jediscoob, Sketchfab, CC-BY) hung from the rear-view mirror: two dice on cords of different length,
  // each a die mesh plus the author's own fuzz drawn as points, swinging with the car (ServiceDice).
  public static void Dice21(){Open();Dice21Stage();Save("dice21");DicePreview21();}
  public static void DiceLook21(){Open();DicePreview21();}
  static void Dice21Stage(){
   const string dir="Assets/ServiceArt/V21/FuzzyDice";AssetDatabase.Refresh();
   var mi=(ModelImporter)AssetImporter.GetAtPath(dir+"/fuzzy_die.fbx");mi.materialImportMode=ModelImporterMaterialImportMode.None;mi.importAnimation=false;mi.importCameras=mi.importLights=false;mi.SaveAndReimport();
   var dieMesh=AssetDatabase.LoadAllAssetsAtPath(dir+"/fuzzy_die.fbx").OfType<Mesh>().First();
   Material Mat(string name){var path=$"{dir}/{name}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}return m;}
   var dm=Mat("V21 fuzzy die");dm.SetTexture("_BaseMap",PropTex(dir+"/dice_texture.png",false,true,512));dm.SetColor("_BaseColor",new Color(.34f,.33f,.32f));dm.SetFloat("_Smoothness",0);dm.SetFloat("_Metallic",0);EditorUtility.SetDirty(dm);
   var fm=Mat("V21 die fuzz");fm.SetColor("_BaseColor",new Color(.36f,.35f,.34f));fm.SetFloat("_Smoothness",0);fm.SetFloat("_Metallic",0);EditorUtility.SetDirty(fm);
   // the author's fuzz points (14k of 500k, Sources/V21/fuzzy_dice/export_dice.py), normals pointing out from the die
   var bytes=File.ReadAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName,dir,"fuzz_points.bytes"));int n=bytes.Length/12;var vs=new Vector3[n];var ns=new Vector3[n];var idx=new int[n];
   for(int i=0;i<n;i++){float x=System.BitConverter.ToSingle(bytes,i*12),y=System.BitConverter.ToSingle(bytes,i*12+4),z=System.BitConverter.ToSingle(bytes,i*12+8);
    var p=new Vector3(-x,z,-y);vs[i]=p;ns[i]=p.normalized;idx[i]=i;} // Blender (x,y,z) -> Unity (-x,z,-y), as the FBX axis conversion does
   var fuzzPath=dir+"/V21 die fuzz points.asset";AssetDatabase.DeleteAsset(fuzzPath);var fuzz=new Mesh{name="die fuzz points"};fuzz.SetVertices(vs);fuzz.SetNormals(ns);fuzz.SetIndices(idx,MeshTopology.Points,0);fuzz.RecalculateBounds();AssetDatabase.CreateAsset(fuzz,fuzzPath);
   var mirror=county.Car.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Authored mirror housing");
   var old=mirror.Find("Fuzzy dice (jediscoob, CC-BY)");if(old)Object.DestroyImmediate(old.gameObject);
   var mr=mirror.GetComponentsInChildren<Renderer>(true);var b=mr[0].bounds;foreach(var r in mr)b.Encapsulate(r.bounds);
   var car=county.Car;var hang=new Vector3(b.center.x,b.min.y+.004f,b.center.z)+car.forward*.012f;
   var root=new GameObject("Fuzzy dice (jediscoob, CC-BY)").transform;root.SetParent(mirror,true);root.SetPositionAndRotation(hang,car.rotation);
   var cordMat=Mat("V21 dice cord");cordMat.SetColor("_BaseColor",new Color(.08f,.07f,.07f));cordMat.SetFloat("_Smoothness",.1f);EditorUtility.SetDirty(cordMat);
   var dice=root.gameObject.AddComponent<ServiceDice>();var pivots=new System.Collections.Generic.List<Transform>();var lengths=new System.Collections.Generic.List<float>();
   foreach(var (side,len,rot) in new[]{(-.016f,.085f,new Vector3(14,38,9)),(.018f,.112f,new Vector3(-22,-17,31))}){
    var pv=new GameObject("Cord pivot").transform;pv.SetParent(root,false);pv.localPosition=new Vector3(side,0,0);pv.localRotation=Quaternion.identity;
    var cord=pv.gameObject.AddComponent<LineRenderer>();cord.useWorldSpace=false;cord.positionCount=2;cord.SetPosition(0,Vector3.zero);cord.SetPosition(1,new Vector3(0,-(len-.034f),0));cord.widthMultiplier=.0016f;cord.sharedMaterial=cordMat;cord.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    var die=new GameObject("Fuzzy die").transform;die.SetParent(pv,false);die.localPosition=new Vector3(0,-len,0);die.localRotation=Quaternion.Euler(rot);
    var body=new GameObject("Die",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(die,false);body.GetComponent<MeshFilter>().sharedMesh=dieMesh;body.GetComponent<MeshRenderer>().sharedMaterial=dm;
    var fz=new GameObject("Fuzz",typeof(MeshFilter),typeof(MeshRenderer));fz.transform.SetParent(die,false);fz.GetComponent<MeshFilter>().sharedMesh=fuzz;fz.GetComponent<MeshRenderer>().sharedMaterial=fm;
    foreach(var r in die.GetComponentsInChildren<Renderer>(true))r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    pivots.Add(pv);lengths.Add(len);}
   dice.Pivots=pivots.ToArray();dice.Lengths=lengths.ToArray();DiceMaterials21();
   foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=0; // not the cabin layer: the dash spill blew them out
   var db=dieMesh.bounds;log.AppendLine($"DICE hung at seat {county.DriverSeat.InverseTransformPoint(hang)} from {PathOf(mirror)}; die mesh size {db.size}; fuzz points {n}");
   EditorUtility.SetDirty(county);
  }
  static void DicePreview21(){
   var dir=Path.Combine(Work,"Audit","dice21");Directory.CreateDirectory(dir);var seat=county.DriverSeat;var muted=MuteFeatures();
   var cam=new GameObject("dice cam").AddComponent<Camera>();cam.CopyFrom(county.View);cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;cam.cullingMask=~(1<<8);
   var key=new GameObject("dice key").AddComponent<Light>();key.type=LightType.Point;key.range=3;key.intensity=1.2f;
   try{
    cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(6,0,0));key.transform.position=seat.TransformPoint(new Vector3(0,.1f,.2f));Shoot(cam,Path.Combine(dir,"eye.png"),1280,720);
    cam.transform.SetPositionAndRotation(seat.position,seat.rotation*Quaternion.Euler(-4,24,0));cam.fieldOfView=32;Shoot(cam,Path.Combine(dir,"eye-zoom.png"),960,720);
    var root=county.Car.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Fuzzy dice (jediscoob, CC-BY)");
    cam.fieldOfView=40;cam.transform.position=root.position+seat.right*-.22f+seat.forward*-.12f+seat.up*-.06f;cam.transform.LookAt(root.position+seat.up*-.1f);Shoot(cam,Path.Combine(dir,"close.png"),960,720);
   }finally{Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(key.gameObject);Restore(muted);}
  }
 }
}
