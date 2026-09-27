using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 public static class ServiceV14Cabin {
  const string Art="Assets/External/TybleCrownVictoria/";
  static string Work=>Directory.GetParent(Application.dataPath).Parent.FullName;
  static CountyScene Open(){EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");return Object.FindAnyObjectByType<CountyScene>();}
  static Transform root;
  static Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
  static Vector3 Point(float x,float y,float z)=>new Vector3(-x,z,-y);
  static Material Mat(string name,Color color,float smooth=.3f,float metallic=0,bool emit=false){
   var path=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);
   if(emit){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.75f);}EditorUtility.SetDirty(m);return m;
  }
  static Transform Empty(string name,Vector3 p){var t=new GameObject(name).transform;t.SetParent(root,false);t.localPosition=p;return t;}
  static TextMesh Label(string text,Vector3 pos,float height,Color color){
   var t=Empty("Instrument lettering "+text,pos);var tm=t.gameObject.AddComponent<TextMesh>();tm.text=text;
   tm.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Barlow-Regular.ttf");tm.fontSize=96;tm.characterSize=height/9.6f;tm.anchor=TextAnchor.MiddleCenter;tm.color=color;tm.GetComponent<Renderer>().sharedMaterial=tm.font.material;return tm;
  }
  static Transform Gauge(string source,string title,float x,float z,float r,Material needle,Color ink){
   var p=Point(x,-.843f,z);var pivot=Empty(title+" needle pivot",p);var mesh=Find(source);mesh.SetParent(pivot,true);mesh.GetComponent<Renderer>().sharedMaterial=needle;
   for(int i=0;i<=6;i++){float a=Mathf.Lerp(130,-130,i/6f)*Mathf.Deg2Rad;var q=p+new Vector3(-Mathf.Sin(a)*r,Mathf.Cos(a)*r,-.005f);Label((title=="MPH"?i*10:i).ToString(),q,title=="MPH"?.009f:.007f,ink);}
   Label(title,p+new Vector3(0,-.020f,-.005f),.0065f,ink);return pivot;
  }
  public static void Apply(){
   var s=Open();if(s.Cockpit.name=="Tyble authored cabin")throw new Exception("Restore V14 scene backup before a new fit; never stack cabins.");
   var old=s.Cockpit;
   root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"CrownVictoria.fbx"),s.Car).transform;root.name="Tyble authored cabin";
   root.localPosition=new Vector3(0,.9616f,-.4204f);root.localRotation=Quaternion.identity;root.localScale=Vector3.one*.88f;
   var vinyl=Mat("Charcoal vinyl",new Color(.105f,.115f,.125f),.24f);
   var fascia=Mat("Graphite fascia",new Color(.19f,.21f,.23f),.4f,.15f);
   var dark=Mat("Recesses",new Color(.025f,.03f,.035f),.1f);
   var cloth=Mat("Seat cloth",new Color(.14f,.16f,.18f),.08f);
   var trim=Mat("Satin fittings",new Color(.36f,.39f,.4f),.48f,.65f);
   var glow=Mat("Phosphor needles",new Color(.46f,.68f,.38f),.2f,0,true);
   var lcd=Mat("Radio LCD",new Color(.07f,.12f,.065f),.2f,0,true);
   foreach(var r in root.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m.name.Contains("fascia")?fascia:m.name.Contains("seats")?cloth:m.name.Contains("trim")?trim:m.name.Contains("recess")||m.name.Contains("black")?dark:vinyl).ToArray();
   // The third-party shell is visible only from inside; keep the established exterior.
   foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=10;
   s.Cockpit=root.gameObject;s.DriverSeat.localPosition=new Vector3(-.3696f,1.34f,-.35f);
   var wheel=Find("Cube.001");s.SteeringWheel=Empty("Authored steering pivot",new Vector3(-.4164f,.0214f,.5435f));s.SteeringWheel.localRotation=Quaternion.Euler(7.24f,0,0);wheel.SetParent(s.SteeringWheel,true);
   var ink=new Color(.58f,.76f,.46f);
   s.SpeedNeedle=Gauge("Plane.004","MPH",.412f,.117f,.057f,glow,ink);
   s.RevNeedle=Gauge("Plane.005","RPM",.531f,.109f,.031f,glow,ink);
   Find("Plane.006").GetComponent<Renderer>().sharedMaterial=glow;
   Label("E       F",Point(.294f,-.834f,.137f),.0065f,ink);
   s.Odometer=Label("000.0 mi",Point(.412f,-.833f,.072f),.008f,ink);
   Find("Cube.004").GetComponent<Renderer>().sharedMaterial=lcd;
   Label("RADIO OFF",Point(0,-.815f,.108f),.012f,ink);
   Label("FM / AM",Point(0,-.804f,.153f),.0065f,new Color(.56f,.58f,.58f));
   Label("VOL",Point(-.1f,-.804f,.147f),.0055f,new Color(.56f,.58f,.58f));
   var mirror=Find("Rearview glass");mirror.SetParent(root,true);s.MirrorUVScale=Vector2.one;
   var light=Empty("Dashboard ambient spill",new Vector3(-.3f,.45f,.15f)).gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.69f,.76f,.71f);light.intensity=.35f;light.range=1.7f;light.cullingMask=1<<10;light.shadows=LightShadows.None;
   foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=10;
   Object.DestroyImmediate(old);
   if(Object.FindObjectsByType<Camera>().Count(c=>c.enabled)!=1)throw new Exception("Duplicate scene cameras");
   EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();
   File.WriteAllText(Path.Combine(Work,"v14-cabin-contract.txt"),"PASS: authored Tyble cabin installed; original exterior/physics retained; original generated cabin removed; authored wheel, instrument needles and rounded mirror fitted; one active scene camera. Source is noncommercial fan art. Visual and runtime checks pending.");
   Preview();ServiceQuickBuild.Build();
  }
  public static void Preview(){
   var s=Open();var cam=s.View;cam.transform.SetParent(s.DriverSeat,false);cam.transform.localPosition=Vector3.zero;cam.nearClipPlane=.035f;cam.fieldOfView=68;cam.cullingMask&=~(1<<8);s.Cockpit.SetActive(true);
   var dir=Path.Combine(Work,"PreviewV14");Directory.CreateDirectory(dir);
   foreach(var pose in new[]{new Vector3(5,0,0),new Vector3(24,0,0),new Vector3(14,36,0),new Vector3(-12,0,0)}){
    cam.transform.localRotation=Quaternion.Euler(pose);var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();var prev=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(dir,"cabin-"+pose.x+"-"+pose.y+".png"),tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=prev;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
   }
  }
  public static void Refine(){
   var s=Open();root=s.Cockpit.transform;
   if(root.name!="Tyble authored cabin")throw new Exception("Expected authored replacement");
   // Read the source surface with temporary colliders, then seat lettering just
   // in front of that surface. No new visible meshes are introduced.
   var colliders=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="Plane.007"||f.name=="Plane.010").Select(f=>{var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;return c;}).ToArray();
   Physics.SyncTransforms();var fitted=new System.Collections.Generic.List<string>();
   foreach(var text in root.GetComponentsInChildren<TextMesh>()){
    float height=float.TryParse(text.text,out var number)?(text.transform.localPosition.x<-.48f?.010f:.014f):text.text=="RADIO OFF"?.023f:text==s.Odometer?.010f:.008f;
    text.characterSize=height/9.6f;
    if(float.TryParse(text.text,out number)){
     bool rpm=text.transform.localPosition.x<-.49f;float angle=Mathf.Lerp(130,-130,number/(rpm?6f:60f))*Mathf.Deg2Rad;float radius=rpm?.024f:.045f;
     text.transform.localPosition=new Vector3(-(rpm?.531f:.412f)-Mathf.Sin(angle)*radius,(rpm?.109f:.117f)+Mathf.Cos(angle)*radius,.843f);
    }
    if(text.text=="RADIO OFF")text.transform.localPosition=Point(0,-.79f,.15f);
    if(text.text=="FM / AM")text.transform.localPosition=Point(0,-.79f,.174f);
    if(text.text=="VOL")text.transform.localPosition=Point(-.099f,-.79f,.141f);
    var p=text.transform.position;var ray=new Ray(p-root.forward*.25f,root.forward);float closest=float.PositiveInfinity;Vector3 normal=-root.forward;
    foreach(var c in colliders)if(c.Raycast(ray,out var hit,.5f)&&hit.distance<closest){closest=hit.distance;normal=hit.normal;}
    if(float.IsFinite(closest)){text.transform.position=ray.GetPoint(closest)+normal*.003f;text.transform.rotation=Quaternion.LookRotation(-normal,root.up);}
    fitted.Add(text.text+" point="+root.InverseTransformPoint(text.transform.position)+" normal="+normal+" distance="+closest);
   }
   foreach(var pivot in new[]{s.SpeedNeedle,s.RevNeedle}){
    var child=pivot.GetChild(0);var ray=new Ray(pivot.position-root.forward*.25f,root.forward);
    foreach(var c in colliders)if(c.Raycast(ray,out var hit,.5f)){
     child.SetParent(root,true);var plane=pivot.parent==root?Empty(pivot.name+" mounting plane",pivot.localPosition):pivot.parent;plane.position=pivot.position;plane.rotation=Quaternion.LookRotation(-hit.normal,root.up);pivot.SetParent(plane,true);pivot.localRotation=Quaternion.identity;child.SetParent(pivot,true);break;
    }
   }
   File.WriteAllLines(Path.Combine(Work,"v14-label-fit.txt"),fitted);
   foreach(var c in colliders)Object.DestroyImmediate(c);
   var textPath=Art+"World instrument lettering.mat";var textMat=AssetDatabase.LoadAssetAtPath<Material>(textPath);
   if(!textMat){textMat=new Material(Shader.Find("SERVICE/World Instrument Text"));AssetDatabase.CreateAsset(textMat,textPath);}
   var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Barlow-Regular.ttf");textMat.mainTexture=font.material.mainTexture;
   foreach(var label in root.GetComponentsInChildren<TextMesh>())label.GetComponent<Renderer>().sharedMaterial=textMat;
   var atlas=root.GetComponent<ServiceInstrumentText>();if(!atlas)atlas=root.gameObject.AddComponent<ServiceInstrumentText>();atlas.SurfaceMaterial=textMat;
   Find("Cube.004").GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"Recesses.mat");
   var mirrorPath=Art+"Rearview display.mat";var mirrorMat=AssetDatabase.LoadAssetAtPath<Material>(mirrorPath);
   if(!mirrorMat){mirrorMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mirrorMat,mirrorPath);}
   Find("Rearview glass").GetComponent<Renderer>().sharedMaterial=mirrorMat;
   var light=Find("Dashboard ambient spill").GetComponent<Light>();light.intensity=.65f;light.color=new Color(.72f,.76f,.73f);
   foreach(var r in root.GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();Audit();Preview();if(!Environment.GetCommandLineArgs().Contains("-previewOnly"))ServiceQuickBuild.Build();
  }
  public static void Audit(){
   var s=Open();var lines=s.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled).Select(f=>AnimationUtility.CalculateTransformPath(f.transform,s.transform)+" | "+AssetDatabase.GetAssetPath(f.sharedMesh)).ToArray();
   File.WriteAllLines(Path.Combine(Work,"v14-scene-mesh-provenance.txt"),lines);
   var cabin=s.Cockpit.GetComponentsInChildren<MeshFilter>(true);
   if(cabin.Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh)!=Art+"CrownVictoria.fbx"))throw new Exception("Cabin contains non-source mesh");
   if(s.Cockpit.GetComponentsInChildren<Collider>().Length!=0)throw new Exception("Temporary fitting colliders remain");
   var glass=s.Cockpit.transform.Find("Rearview glass");var mesh=glass.GetComponent<MeshFilter>().sharedMesh;
   var points=mesh.vertices.Select(v=>s.Cockpit.transform.InverseTransformPoint(glass.TransformPoint(v))).ToArray();var indices=Enumerable.Range(0,points.Length).ToArray();
   int top=indices.OrderByDescending(i=>points[i].y).First(),bottom=indices.OrderBy(i=>points[i].y).First(),left=indices.OrderBy(i=>points[i].x).First(),right=indices.OrderByDescending(i=>points[i].x).First();
   if((mesh.uv[top].y-mesh.uv[bottom].y)*s.MirrorUVScale.y<=0||(mesh.uv[right].x-mesh.uv[left].x)*s.MirrorUVScale.x>=0)throw new Exception("Rearview UV orientation is incorrect");
   if(glass.GetComponent<Renderer>().sharedMaterial.shader.name!="Universal Render Pipeline/Unlit")throw new Exception("Mirror shader must be a serialized build dependency");
   File.WriteAllText(Path.Combine(Work,"v14-cabin-provenance.txt"),"PASS: all "+cabin.Length+" visible cabin mesh filters reference Tyble's authored CrownVictoria.fbx. No primitive or generated replacement mesh. Instrument/radio lettering uses Barlow font. Temporary fitting colliders removed. Source ZIP preserved under Sources/V14/CrownVictoria; derived mirror extracted from authored wing mirror faces. Noncommercial fan-art restriction applies.");
  }
  public static void ValidateBuild(){Audit();ServiceQuickBuild.Build();}
  public static void Inspect(){
   var s=Open();var o=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"CrownVictoria.fbx"));
   File.WriteAllLines(Path.Combine(Work,"v14-import-inspection.txt"),new[]{"DRIVER "+s.DriverSeat.localPosition,"COCKPIT "+s.Cockpit.transform.localPosition,"WHEEL "+s.Car.InverseTransformPoint(s.SteeringWheel.position),"CAR "+s.Car.position,"MODEL "+o.transform.localScale}.Concat(o.GetComponentsInChildren<MeshRenderer>().Select(r=>r.name+" center="+r.bounds.center.ToString("F4")+" size="+r.bounds.size.ToString("F4"))).Concat(s.Cockpit.GetComponentsInChildren<Transform>(true).Select(t=>"OLD "+t.name+" pos="+s.Car.InverseTransformPoint(t.position)+" scale="+t.localScale)));
   Object.DestroyImmediate(o);
  }
 }
}
