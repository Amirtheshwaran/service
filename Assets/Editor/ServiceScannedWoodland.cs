using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 public static partial class ServiceBuild {
  const string Scans="Assets/ServiceArt/ScannedWoodland/";
  static readonly string[] ScanModels={"fern_02","dead_tree_trunk_02","tree_stump_01","dry_branches_medium_01","rock_moss_set_01"};
  public static void InspectScans(){
   var lines=new List<string>();
   foreach(var id in ScanModels){var model=AssetDatabase.LoadAssetAtPath<GameObject>(Scans+id+"/"+id+".fbx");if(!model)throw new Exception("Missing model "+id);
    lines.Add(id);foreach(var mesh in model.GetComponentsInChildren<MeshFilter>(true))lines.Add(mesh.name+" verts="+mesh.sharedMesh.vertexCount+" bounds="+mesh.sharedMesh.bounds+" mats="+string.Join(",",mesh.GetComponent<Renderer>().sharedMaterials.Select(m=>m?m.name:"NULL")));
   }
   File.WriteAllLines(Path.Combine(Work,"scans-inventory.txt"),lines);
  }
  static Texture2D ScanTexture(string id,string channel,bool readable=false){
   string path=Scans+id+"/"+channel+(channel=="arm"||channel=="Alpha"?".png":".jpg");
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new Exception("Missing scan map: "+path);
   importer.textureType=channel=="nor_gl"?TextureImporterType.NormalMap:TextureImporterType.Default;
   importer.sRGBTexture=channel=="Diffuse";importer.isReadable=readable;importer.maxTextureSize=2048;importer.anisoLevel=8;importer.mipmapEnabled=true;
   importer.textureCompression=readable?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
   return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static Texture2D PackedScan(string id,bool terrain){
   string path=Scans+id+(terrain?"/Terrain mask.png":"/Surface mask.png");
   var arm=ScanTexture(id,"arm",true);var pixels=arm.GetPixels32();
   for(int i=0;i<pixels.Length;i++){var p=pixels[i];byte smooth=(byte)Mathf.Clamp((255-p.g)*.7f+(terrain?28:12),0,165);pixels[i]=terrain?new Color32(0,p.r,128,smooth):new Color32(0,p.r,0,smooth);}
   var packed=new Texture2D(arm.width,arm.height,TextureFormat.RGBA32,false,true);packed.SetPixels32(pixels);packed.Apply();File.WriteAllBytes(path,packed.EncodeToPNG());Object.DestroyImmediate(packed);AssetDatabase.ImportAsset(path);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.anisoLevel=8;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
   ScanTexture(id,"arm");return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static Material ScanMaterial(string id){
   var diffuse=ScanTexture(id,"Diffuse");var normal=ScanTexture(id,"nor_gl");var mask=PackedScan(id,false);
   bool leaf=id=="fern_02";
   if(leaf){
    var rgb=ScanTexture(id,"Diffuse",true);var alpha=ScanTexture(id,"Alpha",true);var pixels=rgb.GetPixels32();var a=alpha.GetPixels32();if(a.Length!=pixels.Length)throw new Exception("Fern atlas mismatch");
    for(int i=0;i<pixels.Length;i++)pixels[i].a=a[i].r;
    var rgba=new Texture2D(rgb.width,rgb.height,TextureFormat.RGBA32,false);rgba.SetPixels32(pixels);rgba.Apply();string atlas=Scans+id+"/Foliage rgba.png";File.WriteAllBytes(atlas,rgba.EncodeToPNG());Object.DestroyImmediate(rgba);AssetDatabase.ImportAsset(atlas);
    var importer=(TextureImporter)AssetImporter.GetAtPath(atlas);importer.alphaIsTransparency=true;importer.anisoLevel=8;importer.maxTextureSize=2048;importer.SaveAndReimport();diffuse=AssetDatabase.LoadAssetAtPath<Texture2D>(atlas);ScanTexture(id,"Diffuse");ScanTexture(id,"Alpha");
   }
   string path=Scans+id+"/Surface.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
   mat.SetTexture("_BaseMap",diffuse);mat.SetTexture("_BumpMap",normal);mat.SetFloat("_BumpScale",.8f);mat.EnableKeyword("_NORMALMAP");mat.SetTexture("_MetallicGlossMap",mask);mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",1);mat.SetTexture("_OcclusionMap",mask);mat.EnableKeyword("_OCCLUSIONMAP");mat.SetFloat("_OcclusionStrength",.65f);mat.SetColor("_BaseColor",leaf?new Color(.7f,.75f,.64f):new Color(.82f,.83f,.85f));mat.enableInstancing=true;
   if(leaf){mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.45f);mat.SetFloat("_Cull",0);mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;}
   EditorUtility.SetDirty(mat);return mat;
  }
  static GameObject ScanProp(string id,int variant,Vector3 at,float yaw,float height,Transform parent,Material mat){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(Scans+id+"/"+id+".fbx");
   var o=Object.Instantiate(source,parent);o.name=id;o.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
   foreach(var group in o.GetComponentsInChildren<LODGroup>())Object.DestroyImmediate(group);
   var all=o.GetComponentsInChildren<MeshRenderer>();bool hasLods=all.Any(r=>r.name.Contains("_LOD"));
   if(hasLods){foreach(var r in all.Where(r=>r.name.EndsWith("LOD0")))Object.DestroyImmediate(r.gameObject);}
   else if(all.Length>1){var selected=all[variant%all.Length];foreach(var r in all.Where(r=>r!=selected))Object.DestroyImmediate(r.gameObject);}
   foreach(var r in o.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.On;}
   ResizePlant(o,height);var bounds=BoundsOf(o);o.transform.position+=at-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   var renderers=o.GetComponentsInChildren<Renderer>();var lod=o.AddComponent<LODGroup>();
   if(hasLods)lod.SetLODs(new[]{new LOD(.12f,renderers.Where(r=>r.name.EndsWith("LOD1")).ToArray()),new LOD(.055f,renderers.Where(r=>r.name.EndsWith("LOD2")).ToArray()),new LOD(.018f,renderers.Where(r=>r.name.EndsWith("LOD3")).ToArray())});
   else lod.SetLODs(new[]{new LOD(id=="fern_02"?.014f:.018f,renderers)});
   lod.RecalculateBounds();
   if(id=="fern_02"){var zone=o.AddComponent<ServiceBrushZone>();zone.Center=at;zone.Radius=Mathf.Clamp(height,.4f,1);}
   return o;
  }
  public static void BuildScannedWoodland(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");scene=Object.FindAnyObjectByType<CountyScene>();world=scene.transform;route=Curve(scene.Route.Select(p=>p.position).ToArray());
   var old=world.Find("Scanned woodland details");if(old)Object.DestroyImmediate(old.gameObject);var root=Empty("Scanned woodland details",Vector3.zero,world);
   var materials=ScanModels.ToDictionary(id=>id,id=>ScanMaterial(id));var terrain=world.GetComponentInChildren<Terrain>();var data=terrain.terrainData;
   string[] surfaces={"leaves_forest_ground","forest_floor","brown_mud_03"};var layers=new List<TerrainLayer>();
   foreach(var id in surfaces){string path=Scans+id+"/Woodland.terrainlayer";var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);if(!layer){layer=new TerrainLayer();AssetDatabase.CreateAsset(layer,path);}layer.diffuseTexture=ScanTexture(id,"Diffuse");layer.normalMapTexture=ScanTexture(id,"nor_gl");layer.maskMapTexture=PackedScan(id,true);layer.tileSize=new Vector2(id=="forest_floor"?3:2,id=="forest_floor"?3:2);layer.normalScale=.85f;layer.maskMapRemapMin=Vector4.zero;layer.maskMapRemapMax=Vector4.one;EditorUtility.SetDirty(layer);layers.Add(layer);}
   data.terrainLayers=layers.ToArray();int res=data.alphamapResolution;var splat=new float[res,res,3];
   for(int z=0;z<res;z++)for(int x=0;x<res;x++){var at=terrain.transform.position+new Vector3(x*data.size.x/(res-1),0,z*data.size.z/(res-1));float n=Mathf.PerlinNoise((at.x+301)*.13f,at.z*.13f);float mud=Mathf.Clamp01(1-Mathf.InverseLerp(3,8,RoadDistance(at)))*.65f;float moss=Mathf.SmoothStep(0,.7f,n);splat[z,x,0]=(1-mud)*(1-moss);splat[z,x,1]=(1-mud)*moss;splat[z,x,2]=mud;}data.SetAlphamaps(0,0,splat);terrain.basemapDistance=140;terrain.heightmapPixelError=4;EditorUtility.SetDirty(data);
   var pathMat=ScanMaterial("brown_mud_03");pathMat.SetTextureScale("_BaseMap",Vector2.one*2.5f);pathMat.SetColor("_BaseColor",new Color(.72f,.74f,.77f));pathMat.SetFloat("_Smoothness",1);EditorUtility.SetDirty(pathMat);
   foreach(var r in world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Garden path ")))r.sharedMaterial=pathMat;
   var bark=ScanMaterial("bark_brown_02");bark.SetTextureScale("_BaseMap",new Vector2(1,3));bark.SetColor("_BaseColor",new Color(.63f,.65f,.7f));EditorUtility.SetDirty(bark);
   var canopy=world.Find("Winter woodland — bare trunks and mist");
   foreach(var tree in canopy.GetComponentsInChildren<CapsuleCollider>().Where(c=>c.transform.parent==canopy)){
    foreach(var r in tree.GetComponentsInChildren<Renderer>()){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]&&mats[i].name.StartsWith("Winter")&&!mats[i].IsKeywordEnabled("_ALPHATEST_ON"))mats[i]=bark;r.sharedMaterials=mats;}
   }
   var rng=new System.Random(16102026);float R(float lo,float hi)=>(float)(lo+rng.NextDouble()*(hi-lo));int count=0;
   // Compose low foreground layers along the paths, with a clear sprint corridor.
   foreach(var property in scene.Properties){Vector3 last=Vector3.one*9999;var path=property.ApproachRoute;
    for(int i=2;i<path.Length-3;i++){var at=path[i];if(Vector3.Distance(at,last)<2.8f||Vector3.Distance(at,property.Door.position)<8)continue;last=at;var side=Vector3.Cross(Vector3.up,(path[i+1]-path[i-1]).normalized);
     foreach(int sign in new[]{-1,1}){
      for(int j=0;j<3;j++){var q=at+side*sign*(1.75f+j*1.05f+R(-.2f,.2f))+new Vector3(R(-.6f,.6f),0,R(-.6f,.6f));if(RoadDistance(q)<4.6f)continue;q.y=terrain.SampleHeight(q)+terrain.transform.position.y;ScanProp("fern_02",count++,q,R(0,360),R(.35f,.75f),root,materials["fern_02"]);}
      if(i%3==0){var q=at+side*sign*R(3.4f,5.8f);if(!ClearForPlay(q,0))continue;q.y=terrain.SampleHeight(q)+terrain.transform.position.y;string id=i%9==0?"tree_stump_01":i%6==0?"dead_tree_trunk_02":"rock_moss_set_01";ScanProp(id,count++,q,R(0,360),id=="tree_stump_01"?R(.6f,1):R(.35f,.65f),root,materials[id]);}
     }
    }
   }
   // Patchy forest-floor detail instead of evenly spaced individual props.
   for(int n=0;n<240;n++){var at=new Vector3(R(-125,145),0,R(25,420));if(!ClearForPlay(at,.6f))continue;at.y=terrain.SampleHeight(at)+terrain.transform.position.y;
    string id=n%7==0?"dry_branches_medium_01":n%5==0?"rock_moss_set_01":"fern_02";ScanProp(id,count++,at,R(0,360),id=="dry_branches_medium_01"?R(.18f,.32f):R(.45f,.8f),root,materials[id]);
   }
   var rp=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");var settings=new SerializedObject(rp);settings.FindProperty("m_MainLightShadowmapResolution").intValue=4096;settings.FindProperty("m_AdditionalLightsShadowmapResolution").intValue=4096;settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(rp);
   var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");foreach(var feature in renderer.rendererFeatures.Where(f=>f.name=="ScreenSpaceAmbientOcclusion")){var so=new SerializedObject(feature);so.FindProperty("m_Settings.Intensity").floatValue=.65f;so.FindProperty("m_Settings.Radius").floatValue=.6f;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(feature);}
   ServiceForestMood.Apply(scene,0);
   var missing=world.GetComponentsInChildren<Renderer>(true).Where(r=>r.sharedMaterials.Any(m=>!m)).ToArray();if(missing.Length>0)throw new Exception("Missing scene materials");
   if(count<200||root.GetComponentsInChildren<LODGroup>().Length!=count)throw new Exception("Scan placement/LOD contract failed");
   EditorSceneManager.MarkSceneDirty(scene.gameObject.scene);if(!EditorSceneManager.SaveScene(scene.gameObject.scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();
   File.WriteAllText(Path.Combine(Work,"scans-validation.txt"),"PASS: nine CC0 scan assets; "+count+" detail instances with distance culling; three terrain layers; valid materials; clear route placement");
   ServiceQuickBuild.Build();
  }
  public static void FinishScanComposition(){
   EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");scene=Object.FindAnyObjectByType<CountyScene>();world=scene.transform;route=Curve(scene.Route.Select(p=>p.position).ToArray());
   var terrain=world.GetComponentInChildren<Terrain>();var data=terrain.terrainData;data.alphamapResolution=512;int res=data.alphamapResolution;var splat=new float[res,res,3];
   // Paint the paths directly into leaf litter with soft, irregular shoulders.
   var paths=scene.Properties.Select(p=>p.ApproachRoute).ToArray();
   for(int z=0;z<res;z++)for(int x=0;x<res;x++){
    var at=terrain.transform.position+new Vector3(x*data.size.x/(res-1),0,z*data.size.z/(res-1));at.y=0;
    float best=1000;
    foreach(var path in paths)for(int i=1;i<path.Length;i++){var a=path[i-1];a.y=0;var b=path[i];b.y=0;var delta=b-a;float t=Mathf.Clamp01(Vector3.Dot(at-a,delta)/Mathf.Max(delta.sqrMagnitude,.001f));best=Mathf.Min(best,Vector3.Distance(at,a+delta*t));}
    float n=Mathf.PerlinNoise((at.x+301)*.13f,at.z*.13f);float width=.78f+Mathf.PerlinNoise(at.x*.65f,at.z*.65f)*.35f;
    float trail=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width*.55f,width+1,best));
    float roadside=Mathf.Clamp01(1-Mathf.InverseLerp(3,8,RoadDistance(at)))*.65f;float mud=Mathf.Max(roadside,trail*.95f);float moss=Mathf.SmoothStep(0,.7f,n);
    splat[z,x,0]=(1-mud)*(1-moss);splat[z,x,1]=(1-mud)*moss;splat[z,x,2]=mud;
   }
   data.SetAlphamaps(0,0,splat);EditorUtility.SetDirty(data);
   foreach(var r in world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Garden path ")))r.enabled=false;
   var canopy=world.Find("Winter woodland — bare trunks and mist");var trunks=canopy.GetComponentsInChildren<CapsuleCollider>().Where(c=>c.transform.parent==canopy).ToArray();
   var rng=new System.Random(17102026);float R(float lo,float hi)=>(float)(lo+rng.NextDouble()*(hi-lo));var bark=AssetDatabase.LoadAssetAtPath<Material>(Scans+"bark_brown_02/Surface.mat");int count=0;
   foreach(var trunk in trunks){var at=trunk.transform.TransformPoint(trunk.center)-Vector3.up*4;at.y=terrain.SampleHeight(at)+terrain.transform.position.y;Object.DestroyImmediate(trunk.gameObject);
    int type=count%10;bool hardwood=type>=8;string path=hardwood?FG+"Prefabs/Nature/Trees/TreeCreator_Tall_C_Dead.prefab":Conifers+(type==7?"PF Conifer Medium BOTD URP.prefab":"PF Conifer Bare BOTD URP.prefab");
    var tree=Sourced(path,at,R(0,360),R(17,29),canopy);
    if(hardwood){tree.transform.localScale=Vector3.Scale(tree.transform.localScale,new Vector3(.85f,1,.85f));foreach(var r in tree.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>m.name.ToLowerInvariant().Contains("leaf")?WinterMaterial(m):bark).ToArray();}
    foreach(var c in tree.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
    var collider=tree.AddComponent<CapsuleCollider>();collider.radius=.48f/Mathf.Abs(tree.transform.lossyScale.x);collider.height=8/tree.transform.lossyScale.y;collider.center=tree.transform.InverseTransformPoint(at+Vector3.up*4);count++;
   }
   EditorSceneManager.MarkSceneDirty(scene.gameObject.scene);if(!EditorSceneManager.SaveScene(scene.gameObject.scene))throw new Exception("Composition save failed");AssetDatabase.SaveAssets();
   File.AppendAllText(Path.Combine(Work,"scans-validation.txt"),"; soft painted paths; "+count+" varied canopy trees with preserved trunk collisions");ServiceQuickBuild.Build();
  }
 }
}
