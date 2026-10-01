using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V17 Fears-to-Fathom pass: licensed residents and dog, no punching hands, dense Ironbark-style pine forest.
 // Every placement is grounded on the terrain and kept clear of roads, buildings, approaches and existing trees.
 public static class ServiceV17Apply {
  const string R="Assets/ServiceArt/V17",ScenePath="Assets/Scenes/HollisCounty.unity";
  static readonly StringBuilder log=new StringBuilder();
  static string Work=>Directory.GetParent(Application.dataPath).Parent.FullName;

  public static void RunAndBuild(){Run();Build();}
  public static void GroundAndBuild(){Ground();Build();}
  // Replace the pale "Woodland" ground with scanned ambientCG needle litter and mossy floor (CC0).
  public static void Ground(){
   var scene=EditorSceneManager.OpenScene(ScenePath);var td=Object.FindAnyObjectByType<Terrain>().terrainData;
   var layers=td.terrainLayers.ToArray();var maps=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);var weight=new float[layers.Length];
   for(int y=0;y<td.alphamapHeight;y+=4)for(int x=0;x<td.alphamapWidth;x+=4)for(int i=0;i<layers.Length;i++)weight[i]+=maps[y,x,i];
   log.AppendLine("OLD LAYERS "+string.Join(" | ",layers.Select((l,i)=>(l?AssetDatabase.GetAssetPath(l):"null")+" w="+weight[i].ToString("F0"))));
   TerrainLayer Layer(string name,string g,float tile){var path=$"{R}/Materials/{name}.terrainlayer";var l=AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);if(!l){l=new TerrainLayer();AssetDatabase.CreateAsset(l,path);}l.diffuseTexture=T($"{R}/Ground/{g}/{g}_2K-JPG_Color.jpg");l.normalMapTexture=T($"{R}/Ground/{g}/{g}_2K-JPG_NormalGL.jpg");l.normalScale=1;l.tileSize=new Vector2(tile,tile);l.smoothness=.04f;l.metallic=0;l.diffuseRemapMax=new Vector4(.85f,.85f,.85f,1);EditorUtility.SetDirty(l);return l;}
   var order=Enumerable.Range(0,layers.Length).OrderByDescending(i=>weight[i]).ToArray();
   if(order.Length>0)layers[order[0]]=Layer("V17 Pine needle floor","PineNeedles001",3.2f);
   if(order.Length>1)layers[order[1]]=Layer("V17 Mossy forest floor","Ground037",4.5f);
   td.terrainLayers=layers;EditorUtility.SetDirty(td);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.AppendAllText(Path.Combine(Work,"v17-apply.txt"),log.ToString());
  }
  public static void Run(){
   Directory.CreateDirectory(R+"/Materials");Directory.CreateDirectory(R+"/Prefabs");
   Clips();
   var scene=EditorSceneManager.OpenScene(ScenePath);
   var county=Object.FindAnyObjectByType<CountyScene>();
   var terrain=Object.FindAnyObjectByType<Terrain>();
   var tdPath=AssetDatabase.GetAssetPath(terrain.terrainData);
   Directory.CreateDirectory(Path.Combine(Work,"Backups/V17"));
   File.Copy(Path.Combine(Work,"ServiceProject",tdPath),Path.Combine(Work,"Backups/V17",Path.GetFileName(tdPath)),true);
   Cast(county);Dog(county);Hands();Forest(county,terrain);
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText(Path.Combine(Work,"v17-apply.txt"),log.ToString());
  }
  public static void Build(){
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(Work,"BuildV17/Service.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
   File.WriteAllText(Path.Combine(Work,"build-summary.txt"),report.summary.result+" / errors "+report.summary.totalErrors+" / warnings "+report.summary.totalWarnings);
   if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Build failed");
  }

  // ---------- materials ----------
  static Texture2D T(string path)=>AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  static Material Lit(string name,string baseTex,string normal=null,bool clip=false,bool twoSided=false,float smooth=.2f,Color? tint=null){
   var path=$"{R}/Materials/{name}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   if(baseTex!=null)m.SetTexture("_BaseMap",T(baseTex));m.SetColor("_BaseColor",tint??Color.white);
   if(normal!=null&&T(normal)){m.SetTexture("_BumpMap",T(normal));m.EnableKeyword("_NORMALMAP");}
   m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;
   if(clip){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.42f);m.EnableKeyword("_ALPHATEST_ON");m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;}
   if(twoSided){m.SetFloat("_Cull",0);m.doubleSidedGI=true;}
   EditorUtility.SetDirty(m);return m;
  }

  // ---------- animation ----------
  public static void Loop(string path,bool inPlace){
   var mi=(ModelImporter)AssetImporter.GetAtPath(path);if(!mi)return;
   var clips=(mi.clipAnimations.Length>0?mi.clipAnimations:mi.defaultClipAnimations);
   foreach(var c in clips){c.loopTime=true;c.lockRootRotation=true;c.keepOriginalOrientation=true;c.lockRootHeightY=true;c.keepOriginalPositionY=true;if(inPlace){c.lockRootPositionXZ=true;c.keepOriginalPositionXZ=true;}}
   mi.clipAnimations=clips;mi.SaveAndReimport();
  }
  public static AnimationClip Clip(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
  static void Clips(){Loop(R+"/Anim/Stand--Idle.anim.fbx",true);Loop(R+"/Anim/Locomotion--Walk_N.anim.fbx",true);Loop(R+"/Dog/A_Breathing.fbx",true);}
  public static AnimatorController Controller(string name,params (string state,string clip)[] states){
   var path=$"{R}/Prefabs/{name}.controller";AssetDatabase.DeleteAsset(path);
   var c=AnimatorController.CreateAnimatorControllerAtPath(path);var sm=c.layers[0].stateMachine;
   foreach(var (state,clip) in states){var s=sm.AddState(state);s.motion=Clip(clip);if(sm.defaultState==null)sm.defaultState=s;}
   return c;
  }

  // ---------- residents ----------
  public static GameObject Human(string fbx,string name,Transform parent,Vector3 pos,Vector3 face,RuntimeAnimatorController ctrl,System.Action<GameObject> dress){
   var root=new GameObject(name);root.transform.SetParent(parent,true);root.transform.position=pos;var dir=face-pos;dir.y=0;root.transform.rotation=Quaternion.LookRotation(dir.sqrMagnitude>.01f?dir:Vector3.forward);
   var m=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));m.transform.SetParent(root.transform,false);
   var an=m.GetComponent<Animator>();if(!an)an=m.AddComponent<Animator>();an.runtimeAnimatorController=ctrl;an.applyRootMotion=false;an.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
   dress?.Invoke(m);return root;
  }
  public static void DressJustMan(GameObject m,bool worker){
   const string J=R+"/Characters/JustMan/";
   Material Part(string part,float smooth,Color tint)=>Lit((worker?"Worker ":"JustMan ")+part,J+part+"_Base_Color.png",J+part+"_Normal_OpenGL.png",smooth:smooth,tint:tint);
   var jacket=Part("jacket",.25f,worker?new Color(.42f,.47f,.36f):Color.white);var legs=Part("pants_shirt",.15f,worker?new Color(.62f,.64f,.7f):Color.white);
   var skin=Part("body",.3f,Color.white);var head=Part("head",.32f,Color.white);var mouth=Part("mouth",.4f,Color.white);var shoes=Part("shoes",.3f,Color.white);
   var eye=Lit("JustMan eye",J+"Eye_Base_Color.png",J+"Eye_Normal.png",smooth:.7f);
   foreach(var r in m.GetComponentsInChildren<Renderer>(true)){
    var n=r.name.ToLower();Material mat=n.Contains("jacket")?jacket:n.Contains("pants")||n.Contains("shirt")?legs:n.Contains("shoes")?shoes:n.Contains("head")?head:n.Contains("jaw")||n.Contains("tongue")?mouth:n.Contains("eye_alibedo")?eye:n=="body"?skin:null;
    if(n.Contains("eye_optical")){r.enabled=false;continue;}
    if(mat)r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();else log.AppendLine("  unmapped JustMan renderer "+r.name);
   }
  }
  static void DressElderly(GameObject m){
   foreach(var r in m.GetComponentsInChildren<Renderer>(true)){if(!r.name.Contains("Glasses"))continue;var src=r.sharedMaterial;var t=src.GetTexture("_BaseMap");var mat=Lit("Elderly glasses",null,clip:true,smooth:.6f);mat.SetTexture("_BaseMap",t);r.sharedMaterial=mat;}
  }
  static void Cast(CountyScene county){
   var res=Object.FindAnyObjectByType<ServiceResidents>();var parent=res.transform;
   var resident=Controller("V17 Resident",("Idle",R+"/Anim/Stand--Idle.anim.fbx"));
   var worker=Controller("V17 Worker",("Idle",R+"/Anim/Stand--Idle.anim.fbx"),("Walk",R+"/Anim/Locomotion--Walk_N.anim.fbx"));
   ServiceProperty P(int i)=>county.Properties.First(p=>p.Index==i);
   Vector3 Outside(ServiceProperty p)=>p.Door.position+p.Door.forward*3;
   var oldC=res.Correll;var oldB=res.Bell;
   res.Correll=Human(R+"/Characters/ElderlyMan/ElderlyMan.fbx","Correll — elderly resident (Ready Player Me / BELAZ, CC-BY)",parent,oldC.transform.position,Outside(P(0)),resident,DressElderly);
   res.Bell=Human(R+"/Characters/JustMan/JustMan.fbx","Bell — resident (vrimen, CC-BY)",parent,oldB.transform.position,Outside(P(4)),resident,m=>DressJustMan(m,false));
   // V19: the depot worker was removed; V17 installed him here.
   
   oldC.SetActive(false);oldB.SetActive(false);EditorUtility.SetDirty(res);
   log.AppendLine($"CAST correll {res.Correll.transform.position} bell {res.Bell.transform.position}");
  }

  // ---------- dog ----------
  static void Dog(CountyScene county){
   var old=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Correll yard dog");
   if(!old){log.AppendLine("DOG old dog not found");return;}
   foreach(var r in old.GetComponentsInChildren<Renderer>(true))r.enabled=false;
   var mask=R+"/Dog/T_GermanShepherd_MaskMap.png";
   var mat=Lit("German Shepherd coat",R+"/Dog/T_GermanShepherd_B.png",R+"/Dog/T_GermanShepherd_N.png",smooth:.45f);
   mat.SetTexture("_MetallicGlossMap",T(mask));mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_SmoothnessTextureChannel",0);mat.SetTexture("_OcclusionMap",T(mask));mat.EnableKeyword("_OCCLUSIONMAP");
   var ctrl=Controller("V17 Dog",("Breathing",R+"/Dog/A_Breathing.fbx"),("Playing",R+"/Dog/A_Idle_Playing.fbx"));
   var gate=county.Properties.First(p=>p.Index==0).Gate.position;
   var dog=Human(R+"/Dog/SK_GermanShepherd_01.fbx","Correll yard dog — German Shepherd (RetroStyle Games)",old.parent,old.position,gate,ctrl,m=>{foreach(var r in m.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();});
   log.AppendLine($"DOG placed {dog.transform.position}");
  }
  static void Hands(){foreach(var h in Object.FindObjectsByType<ServiceHands>(FindObjectsInactive.Include,FindObjectsSortMode.None)){log.AppendLine("HANDS removed "+h.name);Object.DestroyImmediate(h.gameObject);}}

  // ---------- forest ----------
  static int Tris(Renderer r){var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)return 0;int n=0;for(int i=0;i<f.sharedMesh.subMeshCount;i++)n+=(int)f.sharedMesh.GetIndexCount(i)/3;return n;}
  static (GameObject prefab,float height) TreePrefab(Transform src,string name,System.Func<Material,Material> remap,bool lods){
   var root=new GameObject(name);var inst=Object.Instantiate(src.gameObject);inst.name="Model";inst.transform.SetParent(root.transform,false);inst.transform.localRotation=src.localRotation;inst.transform.localScale=src.localScale;inst.transform.localPosition=Vector3.zero;
   var rends=inst.GetComponentsInChildren<MeshRenderer>().OrderByDescending(Tris).ToArray();
   foreach(var r in rends)r.sharedMaterials=r.sharedMaterials.Select(remap).ToArray();
   var b0=rends[0].bounds;
   foreach(var r in rends.Skip(1)){var b=r.bounds;r.transform.position+=new Vector3(b0.center.x-b.center.x,b0.min.y-b.min.y,b0.center.z-b.center.z);}
   inst.transform.position+=new Vector3(-b0.center.x,-b0.min.y-.18f,-b0.center.z);
   if(lods&&rends.Length>1){var g=root.AddComponent<LODGroup>();var cut=new[]{.32f,.13f,.045f,.008f};g.SetLODs(rends.Take(4).Select((r,i)=>new LOD(cut[i],new Renderer[]{r})).ToArray());g.RecalculateBounds();foreach(var r in rends.Skip(4))Object.DestroyImmediate(r.gameObject);}
   var col=root.AddComponent<CapsuleCollider>();col.radius=Mathf.Max(.15f,b0.size.x*.035f);col.height=Mathf.Min(8,b0.size.y);col.center=new Vector3(0,col.height/2,0);
   var path=$"{R}/Prefabs/{name}.prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return (prefab,b0.size.y);
  }
  static GameObject PropPrefab(Transform src,string name,Material mat,bool collider){
   var root=new GameObject(name);var inst=Object.Instantiate(src.gameObject);inst.name="Model";inst.transform.SetParent(root.transform,false);inst.transform.localRotation=src.localRotation;inst.transform.localScale=src.localScale;inst.transform.localPosition=Vector3.zero;
   foreach(Transform c in inst.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(c.gameObject);
   var r=inst.GetComponent<MeshRenderer>();r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();var b=r.bounds;
   inst.transform.position+=new Vector3(-b.center.x,-b.min.y,-b.center.z);
   if(collider){var mc=inst.AddComponent<MeshCollider>();mc.convex=true;}
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,$"{R}/Prefabs/{name}.prefab");Object.DestroyImmediate(root);return prefab;
  }
  static float SegDist(Vector2 p,Vector2 a,Vector2 b){var ab=b-a;float t=ab.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);return Vector2.Distance(p,a+ab*t);}
  static Vector2 XZ(Vector3 v)=>new Vector2(v.x,v.z);
  sealed class Hash{readonly Dictionary<(int,int),List<Vector2>> cells=new Dictionary<(int,int),List<Vector2>>();const float C=4;
   (int,int) K(Vector2 p)=>(Mathf.FloorToInt(p.x/C),Mathf.FloorToInt(p.y/C));
   public void Add(Vector2 p){var k=K(p);if(!cells.TryGetValue(k,out var l))cells[k]=l=new List<Vector2>();l.Add(p);}
   public bool Near(Vector2 p,float r){var k=K(p);int s=Mathf.CeilToInt(r/C);for(int x=-s;x<=s;x++)for(int y=-s;y<=s;y++)if(cells.TryGetValue((k.Item1+x,k.Item2+y),out var l))foreach(var q in l)if((q-p).sqrMagnitude<r*r)return true;return false;}}

  static void Forest(CountyScene county,Terrain terrain){
   const string PP=R+"/Forest/PinePack/";
   var bark=Lit("Pine bark",PP+"Bark_basecolor.png",PP+"Bark_normal.png",smooth:.12f);
   var needles=Lit("Pine needles",PP+"Cluster_full_basecolor_alpha.png",PP+"Cluster_full_normal.png",clip:true,twoSided:true,smooth:.1f,tint:new Color(.78f,.84f,.78f));
   Material PineRemap(Material m){if(!m)return m;if(m.name.Contains("Billboard")){var t=m.name.Replace("_Mat","");return Lit(t,PP+t+"_Color.png",PP+t+"_Normal.png",clip:true,twoSided:true,smooth:.05f,tint:new Color(.78f,.84f,.78f));}return m.name.Contains("Cluster")?needles:bark;}
   var pack=AssetDatabase.LoadAssetAtPath<GameObject>(PP+"PinePack.fbx");
   var target=new Dictionary<string,(float lo,float hi,float w)>{{"big",(25,31,.15f)},{"large",(21,26,.2f)},{"medium",(14,19,.25f)},{"small",(8,12,.17f)},{"sapling",(2.2f,4,.15f)}};
   var protos=new List<(GameObject prefab,float h,float lo,float hi,float w)>();
   foreach(Transform t in pack.transform){if(!t.name.StartsWith("Pine_"))continue;var kind=t.name.Split('_')[1];var (pf,h)=TreePrefab(t,"V17 "+t.name,PineRemap,true);var tg=target[kind];protos.Add((pf,h,tg.lo,tg.hi,tg.w/3));log.AppendLine($"TREE {t.name} height {h:F1}");}
   var deadSrc=AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Forest/DeadPine/DeadPine.fbx");
   var deadMat=Lit("Dead pine bark",R+"/Forest/DeadPine/Dead_PineTree.png",R+"/Forest/DeadPine/Dead_PineTree_Normal.png",smooth:.1f);
   var (deadPf,deadH)=TreePrefab(deadSrc.transform,"V17 Dead pine",m=>deadMat,false);protos.Add((deadPf,deadH,11,16,.08f));
   // exclusion data
   var roads=new Hash();int roadRenderers=0;var roadWords=new[]{"road","lane","asphalt","gravel","drive","tarmac","verge","shoulder","kerb","curb","junction","corner","track","street","bridge","apron"};
   foreach(var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
    var mr=mf.GetComponent<MeshRenderer>();if(!mr||!mf.sharedMesh)continue;var names=(mf.name+" "+(mf.transform.parent?mf.transform.parent.name:"")+" "+string.Join(" ",mr.sharedMaterials.Where(x=>x).Select(x=>x.name))).ToLower();
    if(!roadWords.Any(names.Contains))continue;roadRenderers++;var v=mf.sharedMesh.vertices;var tr=mf.sharedMesh.triangles;var M=mf.transform.localToWorldMatrix;
    for(int i=0;i<tr.Length;i+=3){var a=M.MultiplyPoint3x4(v[tr[i]]);var b=M.MultiplyPoint3x4(v[tr[i+1]]);var c=M.MultiplyPoint3x4(v[tr[i+2]]);float span=Mathf.Max(Vector3.Distance(a,b),Vector3.Distance(b,c),Vector3.Distance(a,c));int n=Mathf.Clamp(Mathf.CeilToInt(span/2),1,60);
     for(int u=0;u<=n;u++)for(int w2=0;w2<=n-u;w2++){float fu=u/(float)n,fw=w2/(float)n;roads.Add(XZ(a+(b-a)*fu+(c-a)*fw));}}
   }
   var existing=new Hash();int existingCount=0;
   foreach(var root in county.transform.Cast<Transform>().Where(t=>{var n=t.name.ToLower();return n.Contains("woodland")||n.Contains("thicket")||n.Contains("grove")||n.Contains("forest");}))
    foreach(var r in root.GetComponentsInChildren<Renderer>()){existing.Add(XZ(r.transform.position));existingCount++;}
   var route=county.Route.Where(t=>t).Select(t=>XZ(t.position)).ToArray();
   var props=county.Properties.Select(p=>{var rs=p.Building?p.Building.GetComponentsInChildren<Renderer>():new Renderer[0];var b=rs.Length>0?rs[0].bounds:new Bounds(p.Door.position,Vector3.one*8);foreach(var r in rs)b.Encapsulate(r.bounds);return (p,b);}).ToArray();
   var depot=county.Depot?XZ(county.Depot.position):Vector2.zero;
   var td=terrain.terrainData;var tp=terrain.transform.position;var size=td.size;
   bool Clear(Vector2 p,float roadGap,float houseGap,float treeGap){
    if(p.x<tp.x+3||p.y<tp.z+3||p.x>tp.x+size.x-3||p.y>tp.z+size.z-3)return false;
    if(roads.Near(p,roadGap)||existing.Near(p,treeGap))return false;
    for(int i=1;i<route.Length;i++)if(SegDist(p,route[i-1],route[i])<roadGap+3)return false;
    if(Vector2.Distance(p,depot)<42)return false;
    foreach(var (pr,b) in props){
     if(p.x>b.min.x-houseGap&&p.x<b.max.x+houseGap&&p.y>b.min.z-houseGap&&p.y<b.max.z+houseGap)return false;
     if(pr.Gate&&SegDist(p,XZ(pr.Gate.position),XZ(pr.Door.position))<6)return false;
     if(pr.ApproachRoute!=null)for(int i=1;i<pr.ApproachRoute.Length;i++)if(SegDist(p,XZ(pr.ApproachRoute[i-1]),XZ(pr.ApproachRoute[i]))<5)return false;
     if(pr.TableApproach&&Vector2.Distance(p,XZ(pr.TableApproach.position))<8)return false;
    }
    var y=terrain.SampleHeight(new Vector3(p.x,0,p.y))+tp.y;
    foreach(var c in Physics.OverlapSphere(new Vector3(p.x,y+1.2f,p.y),2.2f,~0,QueryTriggerInteraction.Ignore))if(!(c is TerrainCollider))return false;
    if(Physics.Raycast(new Vector3(p.x,y+80,p.y),Vector3.down,out var hit,120,~0,QueryTriggerInteraction.Ignore)&&!(hit.collider is TerrainCollider))return false;
    return true;
   }
   var rng=new System.Random(1717);float Rand(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
   var prototypes=protos.Select(p=>new TreePrototype{prefab=p.prefab}).ToArray();td.treePrototypes=prototypes;td.RefreshPrototypes();
   var trees=new List<TreeInstance>();var placed=new Hash();float total=protos.Sum(p=>p.w);var counts=new int[protos.Count];
   const float step=5.4f;
   for(float x=tp.x+2;x<tp.x+size.x-2;x+=step)for(float z=tp.z+2;z<tp.z+size.z-2;z+=step){
    var p=new Vector2(x+Rand(-.45f,.45f)*step,z+Rand(-.45f,.45f)*step);
    if(!Clear(p,4.5f,9,2.6f)||placed.Near(p,3.2f))continue;
    float pick=Rand(0,total);int k=0;while(k<protos.Count-1&&(pick-=protos[k].w)>0)k++;
    var pr=protos[k];float s=Rand(pr.lo,pr.hi)/pr.h;placed.Add(p);counts[k]++;
    trees.Add(new TreeInstance{prototypeIndex=k,position=new Vector3((p.x-tp.x)/size.x,0,(p.y-tp.z)/size.z),heightScale=s,widthScale=s*Rand(.9f,1.1f),rotation=Rand(0,Mathf.PI*2),color=Color.white,lightmapColor=Color.white});
   }
   td.SetTreeInstances(trees.ToArray(),true);terrain.treeDistance=190;terrain.Flush();EditorUtility.SetDirty(td);
   log.AppendLine($"FOREST roads={roadRenderers} existingTrees={existingCount} route={route.Length} placed={trees.Count} "+string.Join(", ",protos.Select((p,i)=>p.prefab.name.Replace("V17 ","")+"="+counts[i])));
   // forest floor scans as grounded scene objects
   var floor=new GameObject("V17 forest floor — Poly Haven scans (CC0)").transform;floor.SetParent(county.transform,true);
   var trunkMat=Lit("Dead tree trunk",R+"/Forest/DeadTrunk/dead_tree_trunk_diff_4k.jpg",R+"/Forest/DeadTrunk/dead_tree_trunk_nor_gl_4k.exr",smooth:.1f);
   var branchMat=Lit("Dry branches",R+"/Forest/DryBranches/dry_branches_medium_01_diff_4k.jpg",R+"/Forest/DryBranches/dry_branches_medium_01_nor_gl_4k.exr",smooth:.08f);
   var rock1=Lit("Mossy rocks 01",R+"/Forest/RockMoss01/rock_moss_set_01_diff_4k.jpg",R+"/Forest/RockMoss01/rock_moss_set_01_nor_gl_4k.exr",smooth:.15f);
   var rock2=Lit("Mossy rocks 02",R+"/Forest/RockMoss02/rock_moss_set_02_diff_4k.jpg",R+"/Forest/RockMoss02/rock_moss_set_02_nor_gl_4k.exr",smooth:.15f);
   var floorKinds=new List<(GameObject pf,int count,float lo,float hi)>();
   floorKinds.Add((PropPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Forest/DeadTrunk/dead_tree_trunk.fbx").transform,"V17 Fallen trunk",trunkMat,true),90,1.6f,2.6f));
   foreach(Transform c in AssetDatabase.LoadAssetAtPath<GameObject>(R+"/Forest/DryBranches/dry_branches_medium_01.fbx").transform)floorKinds.Add((PropPrefab(c,"V17 "+c.name,branchMat,false),55,1.2f,2f));
   foreach(var (fbx,mat) in new[]{(R+"/Forest/RockMoss01/rock_moss_set_01.fbx",rock1),(R+"/Forest/RockMoss02/rock_moss_set_02.fbx",rock2)})
    foreach(Transform c in AssetDatabase.LoadAssetAtPath<GameObject>(fbx).transform)floorKinds.Add((PropPrefab(c,"V17 "+c.name,mat,true),14,.8f,1.8f));
   int floorPlaced=0;
   foreach(var (pf,count,lo,hi) in floorKinds)for(int i=0,tries=0;i<count&&tries<count*40;tries++){
    var p=new Vector2(Rand(tp.x+4,tp.x+size.x-4),Rand(tp.z+4,tp.z+size.z-4));
    if(!Clear(p,7,12,1.6f)||placed.Near(p,1.4f))continue;
    var y=terrain.SampleHeight(new Vector3(p.x,0,p.y))+tp.y;var nrm=td.GetInterpolatedNormal((p.x-tp.x)/size.x,(p.y-tp.z)/size.z);
    var go=(GameObject)PrefabUtility.InstantiatePrefab(pf,floor);go.transform.position=new Vector3(p.x,y-.06f,p.y);
    go.transform.rotation=Quaternion.FromToRotation(Vector3.up,nrm)*Quaternion.Euler(0,Rand(0,360),0);go.transform.localScale=Vector3.one*Rand(lo,hi);
    go.isStatic=true;placed.Add(p);i++;floorPlaced++;
   }
   log.AppendLine($"FLOOR placed={floorPlaced} kinds={floorKinds.Count}");
  }
 }
}
