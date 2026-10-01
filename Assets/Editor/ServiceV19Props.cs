using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V19 prop library: Poly Haven (CC0) models as URP prefabs with a bottom-centre pivot and a box collider,
 // plus the Flooded Grounds furniture already in the project. Writes Audit/prop-catalog.json with measured sizes.
 public static partial class ServiceV19Rebuild {
  const string PropRoot="Assets/ServiceArt/V19/Props";
  static readonly string[] FloodedProps={"Prop_Bed_A","Prop_Bed_B","Prop_Cabinet_A","Prop_Cabinet_B","Prop_Chair_A","Prop_Chair_B","Prop_Clock_A","Prop_Lamp_A","Prop_Lamp_B","Prop_Lamp_C","Prop_Lamp_D","Prop_LargeTable_A","Prop_Painting_A","Prop_Painting_B","Prop_Painting_C","Prop_Painting_D","Prop_Rug_A","Prop_Rug_B","Prop_Rug_C","Prop_Rug_D","Prop_SmallTable_A","Prop_SmallTable_B","Prop_Sofa_A","Prop_Vase_A","Prop_Vase_B"};
  public static void Props(){
   AssetDatabase.Refresh();var catalog=new List<string>();
   foreach(var dir in Directory.GetDirectories(PropRoot).OrderBy(d=>d)){var asset=Path.GetFileName(dir);var prefab=PolyPrefab(asset);if(prefab)catalog.Add(Entry(asset,"polyhaven",prefab,$"{PropRoot}/{asset}/{asset}.prefab"));}
   foreach(var name in FloodedProps){var path=$"Assets/Flooded_Grounds/Prefabs/Props/{name}.prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab)catalog.Add(Entry(name,"flooded",prefab,path));}
   foreach(var name in new[]{"ArmChair_01","ClassicNightstand_01","Rockingchair_01","Shelf_01","Sofa_01","WoodenChair_01","WoodenTable_01","Television_01","cardboard_box_01"}){var path=$"Assets/ServiceArt/V18/Furniture/{name}/{name}.prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab)catalog.Add(Entry(name,"polyhaven-v18",prefab,path));}
   Directory.CreateDirectory(Path.Combine(Work,"Audit"));File.WriteAllText(Path.Combine(Work,"Audit","prop-catalog.json"),"[\n"+string.Join(",\n",catalog)+"\n]");
   AssetDatabase.SaveAssets();
  }
  static string Entry(string name,string source,GameObject prefab,string path){var b=PrefabBounds(prefab);return $"{{\"name\":\"{name}\",\"source\":\"{source}\",\"path\":\"{path}\",\"size\":[{b.size.x:F2},{b.size.y:F2},{b.size.z:F2}],\"center\":[{b.center.x:F2},{b.center.y:F2},{b.center.z:F2}]}}";}
  static Bounds PrefabBounds(GameObject prefab){var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var rs=g.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();var lod=g.GetComponent<LODGroup>();if(lod){var first=lod.GetLODs()[0].renderers;rs=first.Where(r=>r).ToArray();}var b=rs.Length>0?rs[0].bounds:new Bounds();foreach(var r in rs)b.Encapsulate(r.bounds);Object.DestroyImmediate(g);return b;}
  static Texture2D PropTex(string path,bool normal,bool srgb,int max=1024){
   if(!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName,path)))return null;
   var imp=(TextureImporter)AssetImporter.GetAtPath(path);if(!imp)return null;bool dirty=false;
   var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
   if(imp.textureType!=type){imp.textureType=type;dirty=true;}
   if(!normal&&imp.sRGBTexture!=srgb){imp.sRGBTexture=srgb;dirty=true;}
   if(imp.maxTextureSize!=max){imp.maxTextureSize=max;dirty=true;}
   if(dirty)imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static Material PropMaterial(string dir,string set){
   var path=$"{dir}/{set}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   int max=set.Contains("torch")?2048:1024;
   m.SetTexture("_BaseMap",PropTex($"{dir}/{set}_diff.png",false,true,max));m.SetColor("_BaseColor",Color.white);
   var n=PropTex($"{dir}/{set}_normal.png",true,false,max);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",1);}
   var ms=PropTex($"{dir}/{set}_ms.png",false,false,max);if(ms){m.SetTexture("_MetallicGlossMap",ms);m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",1);m.SetFloat("_SmoothnessTextureChannel",0);}
   var ao=PropTex($"{dir}/{set}_ao.png",false,false,max);if(ao){m.SetTexture("_OcclusionMap",ao);m.EnableKeyword("_OCCLUSIONMAP");m.SetFloat("_OcclusionStrength",1);}
   if(set.Contains("glass")){m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetColor("_BaseColor",m.GetTexture("_BaseMap")?new Color(1,1,1,.35f):new Color(.85f,.9f,.92f,.1f));if(!m.GetTexture("_MetallicGlossMap"))m.SetFloat("_Smoothness",.9f);m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");}
   if(set.Contains("flame")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1f,.62f,.25f)*2.2f);m.SetTexture("_EmissionMap",m.GetTexture("_BaseMap"));}
   m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
  }
  static GameObject PolyPrefab(string asset){
   var dir=$"{PropRoot}/{asset}";var fbx=$"{dir}/{asset}.fbx";var mi=(ModelImporter)AssetImporter.GetAtPath(fbx);if(!mi){log.AppendLine("PROP missing fbx "+asset);return null;}
   bool dirty=false;
   if(mi.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard){mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;dirty=true;}
   if(mi.materialLocation!=ModelImporterMaterialLocation.InPrefab){mi.materialLocation=ModelImporterMaterialLocation.InPrefab;dirty=true;}
   if(mi.importAnimation){mi.importAnimation=false;dirty=true;}if(mi.importCameras||mi.importLights){mi.importCameras=mi.importLights=false;dirty=true;}
   if(dirty)mi.SaveAndReimport();
   var sets=Directory.GetFiles(Path.Combine(Directory.GetParent(Application.dataPath).FullName,dir),"*_diff.png").Select(f=>Path.GetFileName(f).Replace("_diff.png","")).OrderByDescending(s=>s.Length).ToArray();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
   var root=new GameObject(asset);var inst=(GameObject)Object.Instantiate(model);inst.name="Model";inst.transform.SetParent(root.transform,false);
   foreach(var r in inst.GetComponentsInChildren<Renderer>()){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){var src=mats[i]?mats[i].name.Replace(" (Instance)",""):"";var set=sets.FirstOrDefault(s=>src==s)??(src.ToLower().Contains("glass")?asset+"_glass":null)??sets.FirstOrDefault(s=>src.StartsWith(s)||s.StartsWith(src)&&src.Length>3)??sets.FirstOrDefault(s=>s==asset)??sets.Last();mats[i]=PropMaterial(dir,set);}r.sharedMaterials=mats;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
   var rs=inst.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
   inst.transform.position+=new Vector3(-b.center.x,-b.min.y,-b.center.z);
   var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,b.size.y*.5f,0);box.size=b.size;
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,$"{dir}/{asset}.prefab");Object.DestroyImmediate(root);return prefab;
  }
  // A hidden row of every catalog item far below the county, photographed by the survey to learn which way each one faces.
  public static void CatalogShelf(){
   Open();var old=county.transform.Find("V19 catalog shelf");if(old)Object.DestroyImmediate(old.gameObject);
   var shelf=new GameObject("V19 catalog shelf").transform;shelf.SetParent(county.transform,false);shelf.position=new Vector3(0,-300,0);
   var json=File.ReadAllText(Path.Combine(Work,"Audit","prop-catalog.json"));int i=0;
   foreach(Match m in Regex.Matches(json,"\"name\":\"([^\"]+)\",\"source\":\"[^\"]+\",\"path\":\"([^\"]+)\"")){
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(m.Groups[2].Value);if(!prefab)continue;
    var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,shelf);g.name="Catalog "+m.Groups[1].Value;g.transform.localPosition=new Vector3((i%8)*6f,0,(i/8)*6f);g.transform.localRotation=Quaternion.identity;i++;
   }
   var floor=GameObject.CreatePrimitive(PrimitiveType.Quad);floor.name="Catalog floor";floor.transform.SetParent(shelf,false);floor.transform.localPosition=new Vector3(21,-.01f,21);floor.transform.localRotation=Quaternion.Euler(90,0,0);floor.transform.localScale=new Vector3(60,60,1);
   shelf.gameObject.SetActive(false);log.AppendLine("CATALOG shelf items "+i);Save("catalog-shelf");
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // Flooded Grounds props keep their meshes far from the prefab origin and are modelled ~1.6x life size.
  // Wrap the ones worth using with a bottom-centre pivot at a real-world scale.
  static readonly (string name,float scale)[] FloodedWraps={("Prop_Rug_A",1f),("Prop_Rug_B",1f),("Prop_Rug_C",1f),("Prop_Rug_D",1f),("Prop_Painting_A",.62f),("Prop_Painting_B",.62f),("Prop_Painting_C",.62f),("Prop_Painting_D",.62f),("Prop_Lamp_A",.6f),
   ("Prop_Bed_A",.55f),("Prop_Bed_B",.55f),("Prop_Cabinet_A",.55f),("Prop_Cabinet_B",.58f),("Prop_LargeTable_A",.62f),("Prop_SmallTable_A",.62f),("Prop_Vase_A",.62f),("Prop_Vase_B",.62f)};
  // Flooded Grounds ships built-in Standard materials, which URP draws magenta: move them onto URP Lit in place.
  static void UpgradeStandard(Material m){
   if(!m||!m.shader)return;var sn=m.shader.name;if(sn!="Standard"&&sn!="Standard (Specular setup)"&&!sn.StartsWith("Legacy Shaders"))return;
   Texture T(string n)=>m.HasProperty(n)?m.GetTexture(n):null;float F(string n,float d)=>m.HasProperty(n)?m.GetFloat(n):d;
   var main=T("_MainTex");var col=m.HasProperty("_Color")?m.GetColor("_Color"):Color.white;var bump=T("_BumpMap");float bs=F("_BumpScale",1);
   var mg=T("_MetallicGlossMap");float met=F("_Metallic",0),gl=F("_Glossiness",.3f),gms=F("_GlossMapScale",1);var occ=T("_OcclusionMap");float os=F("_OcclusionStrength",1);
   var em=T("_EmissionMap");var ec=m.HasProperty("_EmissionColor")?m.GetColor("_EmissionColor"):Color.black;float mode=F("_Mode",0),cut=F("_Cutoff",.5f);
   m.shader=Shader.Find("Universal Render Pipeline/Lit");m.shaderKeywords=new string[0];
   m.SetTexture("_BaseMap",main);m.SetColor("_BaseColor",col);
   if(bump){m.SetTexture("_BumpMap",bump);m.SetFloat("_BumpScale",bs);m.EnableKeyword("_NORMALMAP");}
   if(mg){m.SetTexture("_MetallicGlossMap",mg);m.SetFloat("_Smoothness",gms);m.EnableKeyword("_METALLICSPECGLOSSMAP");}else{m.SetFloat("_Metallic",met);m.SetFloat("_Smoothness",gl);}
   if(occ){m.SetTexture("_OcclusionMap",occ);m.SetFloat("_OcclusionStrength",os);m.EnableKeyword("_OCCLUSIONMAP");}
   if(mode>=1&&mode<2){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",cut);m.EnableKeyword("_ALPHATEST_ON");}
   if(ec.maxColorComponent>.02f){m.SetColor("_EmissionColor",ec);if(em)m.SetTexture("_EmissionMap",em);m.EnableKeyword("_EMISSION");}
   EditorUtility.SetDirty(m);log.AppendLine("UPGRADED material "+m.name);
  }
  public static void WrapFlooded(){
   var dir="Assets/ServiceArt/V19/Props/Flooded";Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,dir));var lines=new List<string>();
   foreach(var (name,scale) in FloodedWraps){
    var src=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Flooded_Grounds/Prefabs/Props/{name}.prefab");if(!src)continue;
    var inst=(GameObject)PrefabUtility.InstantiatePrefab(src);PrefabUtility.UnpackPrefabInstance(inst,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    inst.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);var lod=inst.GetComponent<LODGroup>();
    var rs=lod?lod.GetLODs()[0].renderers.Where(r=>r).ToArray():inst.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    var root=new GameObject(name+" V19");inst.transform.SetParent(root.transform,true);inst.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);root.transform.localScale=Vector3.one;inst.transform.localScale*=scale;inst.transform.localPosition*=scale;
    foreach(var c in inst.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
    foreach(var r in inst.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)UpgradeStandard(m);
    if(!name.StartsWith("Prop_Rug")){var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,b.size.y*scale*.5f,0);box.size=b.size*scale;}
    var path=$"{dir}/{name}.prefab";PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
    lines.Add($"{name}\t{path}\t{b.size.x*scale:F2}\t{b.size.y*scale:F2}\t{b.size.z*scale:F2}");
   }
   // Picture-frame material names, to map the artwork set correctly.
   foreach(var a in new[]{"hanging_picture_frame_01","hanging_picture_frame_02","hanging_picture_frame_03","standing_picture_frame_01"}){var m=AssetDatabase.LoadAssetAtPath<GameObject>($"{PropRoot}/{a}/{a}.fbx");if(m)lines.Add("MATS "+a+": "+string.Join(", ",m.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(x=>x).Select(x=>x.name)));}
   File.WriteAllLines(Path.Combine(Work,"Audit","flooded-wraps.txt"),lines);AssetDatabase.SaveAssets();
  }
 }
}
