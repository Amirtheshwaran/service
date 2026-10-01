using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V19 signage and door notes. The old TextMesh boards (a dynamic font shared with the HUD, so their glyphs broke
 // whenever the atlas rebuilt) become lit, typeset sign faces: green street blades on galvanised posts, a county
 // depot board on timber posts, a ROAD CLOSED sign on the survey barricade, and handwritten notes taped to doors.
 public static partial class ServiceV19Rebuild {
  const string SignDir="Assets/ServiceArt/V19/Signs";
  public static void SignsAndNotes(){Open();SignsStage();NotesStage();Save("signs-notes");}
  static Texture2D SignTex(string file){var path=$"{SignDir}/{file}";var imp=(TextureImporter)AssetImporter.GetAtPath(path);if(!imp)return null;imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Clamp;imp.anisoLevel=4;imp.maxTextureSize=1024;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
  static Material SignMat(string name,string tex,Color tint,float smooth,float metal=0){
   var path=$"{SignDir}/{name}.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
   m.SetTexture("_BaseMap",tex==null?null:SignTex(tex));m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
  }
  static GameObject Piece(PrimitiveType type,string name,Transform parent,Vector3 local,Vector3 scale,Quaternion rot,Material m,bool keepCollider){
   var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.localRotation=rot;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;
   if(!keepCollider)Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;
  }
  static float GroundAt(Vector3 p){var t=county.GetComponentInChildren<Terrain>();float y=t?TerrainY(t,p):p.y;if(Physics.Raycast(new Vector3(p.x,y+3,p.z),Vector3.down,out var h,6,~0,QueryTriggerInteraction.Ignore))y=Mathf.Max(y,h.point.y);return y;}
  static void SignsStage(){
   Physics.SyncTransforms();
   var galv=SignMat("V19 galvanised post",null,new Color(.56f,.58f,.6f),.55f,.85f);
   var timber=SignMat("V19 sign timber",null,new Color(.30f,.22f,.15f),.12f);
   var green=SignMat("V19 sign green edge",null,new Color(.09f,.36f,.23f),.5f);
   var brown=SignMat("V19 sign brown back",null,new Color(.26f,.17f,.11f),.3f);
   var white=SignMat("V19 sign white back",null,new Color(.62f,.63f,.62f),.35f,.3f);
   void Street(string old,string file,string name){
    var tm=county.GetComponentsInChildren<TextMesh>(true).FirstOrDefault(x=>x.text==old);if(!tm){log.AppendLine("SIGN missing "+old);return;}
    var mount=tm.transform.parent;var read=-tm.transform.forward;read.y=0;read.Normalize();var at=mount.position;Kill(mount,"old TextMesh street sign");
    var root=new GameObject(name).transform;root.SetParent(county.transform,false);root.position=new Vector3(at.x,GroundAt(at)-.05f,at.z);root.rotation=Quaternion.LookRotation(read);
    var face=SignMat("V19 "+file.Replace(".png",""),file,Color.white,.55f);
    Piece(PrimitiveType.Cube,"Galvanised post",root,new Vector3(0,1.32f,0),new Vector3(.05f,2.64f,.05f),Quaternion.identity,galv,true);
    Piece(PrimitiveType.Cube,"Blade",root,new Vector3(0,2.38f,-.03f),new Vector3(.76f,.2f,.01f),Quaternion.identity,green,false);
    Piece(PrimitiveType.Quad,"Blade face",root,new Vector3(0,2.38f,-.03f+.0056f),new Vector3(.76f,.2f,1),Quaternion.Euler(0,180,0),face,false);
    Piece(PrimitiveType.Quad,"Blade face (reverse)",root,new Vector3(0,2.38f,-.03f-.0056f),new Vector3(.76f,.2f,1),Quaternion.identity,face,false);
    Piece(PrimitiveType.Cube,"Bracket",root,new Vector3(0,2.38f,-.012f),new Vector3(.07f,.24f,.03f),Quaternion.identity,galv,false);
    log.AppendLine($"SIGN {name} at {root.position} reading toward {read}");
   }
   Street("MILLBROOK RD","street-millbrook.png","Street sign — Millbrook Rd");
   Street("LATIGO TRAIL","street-latigo.png","Street sign — Latigo Trail");
   // County depot board.
   var depotText=county.GetComponentsInChildren<TextMesh>(true).FirstOrDefault(x=>x.text.Contains("DEPOT"));
   if(depotText){var mount=depotText.transform;while(mount.parent&&mount.parent!=county.transform)mount=mount.parent;var read=-depotText.transform.forward;read.y=0;read.Normalize();var at=mount.position;Kill(mount,"old TextMesh depot sign");
    var root=new GameObject("Depot sign — Hollis County Road Department").transform;root.SetParent(county.transform,false);root.position=new Vector3(at.x,GroundAt(at)-.05f,at.z);root.rotation=Quaternion.LookRotation(read);
    var face=SignMat("V19 depot sign","depot.png",Color.white,.35f);
    foreach(float x in new[]{-.78f,.78f})Piece(PrimitiveType.Cube,"Timber post",root,new Vector3(x,1.05f,-.06f),new Vector3(.1f,2.1f,.1f),Quaternion.identity,timber,true);
    Piece(PrimitiveType.Cube,"Board",root,new Vector3(0,1.5f,0),new Vector3(1.8f,1.08f,.035f),Quaternion.identity,brown,true);
    Piece(PrimitiveType.Quad,"Board face",root,new Vector3(0,1.5f,.0185f),new Vector3(1.8f,1.08f,1),Quaternion.Euler(0,180,0),face,false);
    log.AppendLine($"SIGN depot at {root.position}");}
   else log.AppendLine("SIGN depot text not found");
   // ROAD CLOSED on the survey barricade (it disappears with the barricade on the last night).
   var closure=county.transform.Find("North survey closure");var group=closure?closure.Find("Barricade"):null;
   if(group){var old=group.Find("Road closed sign");if(old)Object.DestroyImmediate(old.gameObject);
    var rs=group.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);var fwd=rs[0].transform.forward;fwd.y=0;fwd.Normalize();
    var root=new GameObject("Road closed sign").transform;root.SetParent(group,false);var at=b.center+fwd*.75f;root.position=new Vector3(at.x,GroundAt(at)-.03f,at.z);root.rotation=Quaternion.LookRotation(-fwd);
    var face=SignMat("V19 road closed","road-closed.png",Color.white,.5f);
    foreach(float x in new[]{-.5f,.5f})Piece(PrimitiveType.Cube,"Stand leg",root,new Vector3(x,.95f,-.02f),new Vector3(.045f,1.9f,.045f),Quaternion.identity,galv,true);
    Piece(PrimitiveType.Cube,"Sign plate",root,new Vector3(0,1.55f,0),new Vector3(1.22f,.76f,.012f),Quaternion.identity,white,true);
    Piece(PrimitiveType.Quad,"Sign face",root,new Vector3(0,1.55f,.0066f),new Vector3(1.22f,.76f,1),Quaternion.Euler(0,180,0),face,false);
    log.AppendLine($"SIGN road closed at {root.position}");}
   else log.AppendLine("SIGN barricade not found");
   // Any other TextMesh left in the world uses the HUD's dynamic font: report it.
   foreach(var tm in county.GetComponentsInChildren<TextMesh>(true))log.AppendLine($"TEXTMESH remaining '{tm.text.Replace("\n","/")}' at {tm.transform.position} active={tm.gameObject.activeInHierarchy}");
  }
  // Handwritten notes taped to the outside of the working front doors; they swing with the door.
  static void NotesStage(){
   Physics.SyncTransforms();
   var tape=SignMat("V19 note tape",null,new Color(.82f,.8f,.7f,1),.55f);
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){
    if(p.DoorPanel){var old=p.DoorPanel.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Door note").ToList();foreach(var o in old)Object.DestroyImmediate(o.gameObject);}
    var file=$"note-p{p.Index}.png";bool has=File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName,SignDir,file));
    if(!has||!p.DoorPanel){if(p.NoticePoint&&p.NoticePoint.name!="Door note")p.NoticePoint=null;EditorUtility.SetDirty(p);continue;}
    var rs=p.DoorPanel.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)).ToArray();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    var outward=OutwardOf(p);float half=Vector3.Dot(b.extents,new Vector3(Mathf.Abs(outward.x),0,Mathf.Abs(outward.z)));
    // The leaf is thin along one horizontal axis: snap outward onto it so the note lies flat on the face.
    var thin=b.extents.x<b.extents.z?new Vector3(Mathf.Sign(outward.x),0,0):new Vector3(0,0,Mathf.Sign(outward.z));half=b.extents.x<b.extents.z?b.extents.x:b.extents.z;
    var across=Vector3.Cross(Vector3.up,thin);
    var note=new GameObject("Door note").transform;note.SetParent(p.DoorPanel,true);
    note.position=new Vector3(b.center.x,b.min.y+1.43f,b.center.z)+thin*(half+.004f)+across*.06f;note.rotation=Quaternion.LookRotation(-thin)*Quaternion.Euler(0,0,-2.5f);
    var mat=SignMat($"V19 note p{p.Index}",file,Color.white,.12f);
    Piece(PrimitiveType.Quad,"Paper",note,Vector3.zero,new Vector3(.152f,.195f,1),Quaternion.identity,mat,false);
    Piece(PrimitiveType.Quad,"Tape",note,new Vector3(-.045f,.094f,-.0008f),new Vector3(.05f,.018f,1),Quaternion.Euler(0,0,12),tape,false);
    Piece(PrimitiveType.Quad,"Tape",note,new Vector3(.047f,.093f,-.0008f),new Vector3(.05f,.018f,1),Quaternion.Euler(0,0,-9),tape,false);
    foreach(var r in note.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.gameObject.isStatic=false;}
    p.NoticePoint=note;EditorUtility.SetDirty(p);log.AppendLine($"NOTE p{p.Index} on {p.DoorPanel.name} at {note.position} facing {-thin}");
   }
  }
 }
}
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // World TextMeshes (dashboard gauges, house number) shared dynamic fonts with the HUD, whose glyph requests rebuild
  // the atlas and leave world text with stale UVs (the "broken" lettering). Give them their own static ASCII atlases.
  public static void WorldFonts(){Open();WorldFontsStage();Save("world-fonts");}
  static void WorldFontsStage(){
   var dir="Assets/ServiceArt/V19/Fonts";var map=new Dictionary<Font,(Font font,Material mat)>();
   foreach(var tm in county.GetComponentsInChildren<TextMesh>(true)){
    if(!tm.font)continue;if(tm.font.name.EndsWith("-World")){continue;}
    if(!map.TryGetValue(tm.font,out var pair)){
     var src=AssetDatabase.GetAssetPath(tm.font);if(string.IsNullOrEmpty(src)||!src.EndsWith(".ttf")){log.AppendLine("WORLDFONT skip "+tm.font.name);continue;}
     var dst=$"{dir}/{Path.GetFileNameWithoutExtension(src)}-World.ttf";if(!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName,dst)))AssetDatabase.CopyAsset(src,dst);
     var imp=(TrueTypeFontImporter)AssetImporter.GetAtPath(dst);imp.fontTextureCase=FontTextureCase.ASCII;imp.fontSize=72;imp.fontRenderingMode=FontRenderingMode.Smooth;imp.SaveAndReimport();
     var f=AssetDatabase.LoadAssetAtPath<Font>(dst);var mpath=$"{dir}/{f.name} world type.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(mpath);if(!m){m=new Material(Shader.Find("Service/World Type"));AssetDatabase.CreateAsset(m,mpath);}
     m.mainTexture=f.material.mainTexture;EditorUtility.SetDirty(m);pair=(f,m);map[tm.font]=pair;log.AppendLine($"WORLDFONT {src} -> {dst} atlas {f.material.mainTexture?.width}x{f.material.mainTexture?.height}");
    }
    var r=tm.GetComponent<Renderer>();bool worldType=r.sharedMaterial&&r.sharedMaterial.shader.name=="Service/World Type";
    tm.font=pair.font;r.sharedMaterial=worldType?pair.mat:pair.font.material;EditorUtility.SetDirty(tm);
   }
  }
 }
}
