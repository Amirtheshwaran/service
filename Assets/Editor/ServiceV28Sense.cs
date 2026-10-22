using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V28 ("some furnitures were not properly placed - random places that make no sense, some not even attached to the
 // wall"): does each piece of furniture stand where a person would have put it? Against what is DRAWN (the wall's paper,
 // not its collision box), each standing piece is measured on all four sides in its own frame (front = the side it
 // faces, from the prop catalogue):
 //  . case goods (cabinets, dressers, shelves, beds, stoves...) must have their BACK to a wall, flush (4 cm), square to it,
 //    with room in front of them;
 //  . nothing big may stand alone in the open, away from every wall and every other piece.
 // Sense28 writes Audit/v28/sense/sense28.txt (findings), items.tsv (every piece, with its outline, for the plan
 // overlay) and a top-down cut-away plan of every storey of every house (plans.txt has the pixel mapping).
 public static partial class ServiceV19Rebuild {
  static readonly string[] CaseWords={"cabinet","commode","console","dresser","wardrobe","hutch","shelf","shelves","bookcase","sideboard","stove","fridge","refrigerator","sink","piano","cupboard","counter","drawer","vanity","washstand","bureau","armoire","buffet","bed","nightstand","bedside"};
  static readonly string[] SenseSkip={"fire","blood","apparition","v27 drive lamp","shelf books","hearth","rug","doormat","prop_car","mailbox","readable","v23 shelf","door note"};
  struct SideGap{public float house,prop,ang;public int houseHits,propHits;public string propName;}
  // eye-level views for a list of standpoints: SERVICE_VIEWS="name:x:z:floor:yaw;..." -> Audit/v28/sense/look-<name>.jpg
  public static void Look28(){Open();var dir=Path.Combine(Work,"Audit","v28","sense");Directory.CreateDirectory(dir);bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   var n0=NeutralLight();var muted=MuteFeatures();var cam=AuditCam();cam.aspect=4f/3f;cam.fieldOfView=80;cam.nearClipPlane=.03f;
   try{foreach(var v in (System.Environment.GetEnvironmentVariable("SERVICE_VIEWS")??"").Split(';').Where(x=>x.Count(c=>c==':')>=4)){var a=v.Split(':');
     cam.transform.position=new Vector3(float.Parse(a[1]),float.Parse(a[3])+1.6f,float.Parse(a[2]));cam.transform.rotation=Quaternion.Euler(a.Length>5?float.Parse(a[5]):12f,float.Parse(a[4]),0);Shoot(cam,Path.Combine(dir,"look-"+a[0]+".jpg"),960,720);}}
   finally{EndNeutral(n0);Restore(muted);Object.DestroyImmediate(cam.gameObject);if(county.LateRoad)county.LateRoad.SetActive(lateWas);}}
  public static void Sense28(){Open();Physics.SyncTransforms();var dir=Path.Combine(Work,"Audit","v28","sense");Directory.CreateDirectory(dir);/* V28b: only its own outputs (it used to wipe the eye-level and side renders kept here) */foreach(var f in Directory.GetFiles(dir).Where(f=>{var n=Path.GetFileName(f);return n.StartsWith("plan-")||n.StartsWith("walls-plan-")||n=="items.tsv"||n=="plans.txt"||n=="sense28.txt";}))File.Delete(f);
   var cat=new Dictionary<string,string>();
   try{foreach(var e in JsonUtility.FromJson<CatalogList>("{\"items\":"+File.ReadAllText(Path.Combine(Work,"Audit","prop-catalog-oriented.json"))+"}").items)if(e!=null&&!string.IsNullOrEmpty(e.name))cat[e.name]=e.front;}catch(System.Exception ex){log.AppendLine("catalog: "+ex.Message);}
   var sb=new StringBuilder();var tsv=new StringBuilder();int flagged=0,checkedN=0;
   tsv.AppendLine("p\tid\tprefab\ty\tfront\tknown\tcase\tc0x\tc0z\tc1x\tc1z\tc2x\tc2z\tc3x\tc3z\tfx\tfz\tgapFront\tgapBack\tgapLeft\tgapRight\tflags");
   bool lateWas=county.LateRoad&&county.LateRoad.activeSelf;if(county.LateRoad)county.LateRoad.SetActive(true);Physics.SyncTransforms();
   try{
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var lay=LayoutIds(p.Index);var roots=PropRoots(p).Where(t=>t.gameObject.activeInHierarchy).ToList();
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,roots,owner);
    try{foreach(var t in roots){var id=IdOf(t);var kind=KindOf(id,t.name,lay);if(kind!="floor")continue;var nm=t.name.ToLowerInvariant();if(SenseSkip.Any(nm.Contains))continue;
      var prefab=t.name.Length>id.Length?t.name.Substring(id.Length).Trim(' ','—'):t.name;var pl=prefab.ToLowerInvariant();
      var vs=DrawnVerts(t,20000).ToList();if(vs.Count<8)continue;
      string front=cat.TryGetValue(prefab,out var fr)?fr:null;bool known=!string.IsNullOrEmpty(front)&&front!="none";
      float fy=t.eulerAngles.y+(known?FrontYaw(front):0);var q=Quaternion.Euler(0,fy,0);var iq=Quaternion.Inverse(q);var o=t.position;
      float x0=1e9f,x1=-1e9f,z0=1e9f,z1=-1e9f,y0=1e9f,y1=-1e9f;foreach(var v in vs){var l=iq*(v-o);x0=Mathf.Min(x0,l.x);x1=Mathf.Max(x1,l.x);z0=Mathf.Min(z0,l.z);z1=Mathf.Max(z1,l.z);y0=Mathf.Min(y0,v.y);y1=Mathf.Max(y1,v.y);}
      float w=x1-x0,dp=z1-z0,h=y1-y0;if(h<.3f&&Mathf.Max(w,dp)<.45f)continue;checkedN++;
      bool caseGood=(lay.TryGetValue(id,out var li)&&li.againstWall)||CaseWords.Any(pl.Contains);
      var tree=new HashSet<Transform>{t};
      Vector3 At(float lx,float lz,float y){var r=o+q*new Vector3(lx,0,lz);r.y=y;return r;}
      var gaps=new Dictionary<string,SideGap>();
      foreach(var (sn,ln) in new[]{("front",Vector3.forward),("back",Vector3.back),("right",Vector3.right),("left",Vector3.left)}){var wn=q*ln;var hd=new List<float>();var pd=new List<float>();var an=new List<float>();string pname="";
       bool spansX=Mathf.Abs(ln.z)>.5f;
       foreach(float u in new[]{.15f,.5f,.85f})foreach(float hh in new[]{.12f,.5f,.85f}){
        float lx=spansX?Mathf.Lerp(x0,x1,u):(ln.x>0?x1:x0),lz=spansX?(ln.z>0?z1:z0):Mathf.Lerp(z0,z1,u);float y=Mathf.Min(y0+Mathf.Max(.08f,h*hh),y1-.02f);
        var start=At(lx,lz,y)-wn*.03f;
        foreach(var hit in Physics.RaycastAll(start,wn,1.53f,VisMask,QueryTriggerInteraction.Collide).OrderBy(x=>x.distance)){if(!owner.TryGetValue(hit.collider,out var ow)||tree.Contains(ow))continue;var on=ow.name.ToLowerInvariant();if(FlatWords.Any(on.Contains))continue;
         float dd=hit.distance-.03f;if(ow==p.Building){hd.Add(dd);an.Add(Vector3.Angle(-new Vector3(wn.x,0,wn.z),new Vector3(hit.normal.x,0,hit.normal.z)));}else{pd.Add(dd);if(pname=="")pname=IdOf(ow);}break;}}
       float Med(List<float> l)=>l.Count==0?9f:l.OrderBy(x=>x).ElementAt(l.Count/2);
       gaps[sn]=new SideGap{house=hd.Count>=3?Med(hd):9f,prop=pd.Count>=3?Med(pd):(pd.Count>0?pd.Min():9f),ang=Med(an),houseHits=hd.Count,propHits=pd.Count,propName=pname};}
      var flags=new List<string>();string F(float g)=>g>=9?"none":(g*100).ToString("F0")+" cm";
      if(caseGood){
       if(known){var b=gaps["back"];
        if(b.house<=.04f){if(b.ang>4f&&b.ang<60f)flags.Add($"not square to the wall behind it ({b.ang:F0} deg)");}
        else{var wall=new[]{"front","left","right"}.Where(s=>gaps[s].house<=.04f).ToList();
         if(b.prop<=.06f&&b.propHits>=4)flags.Add($"its back is to another piece ({b.propName}), not a wall");
         else if(wall.Count>0)flags.Add($"turned wrong: its {wall[0]} is against the wall and its back is {F(b.house)} from one");
         else if(b.house<.6f)flags.Add($"stands {F(b.house)} off the wall behind it");
         else flags.Add("not against any wall (nothing within 60 cm behind it)");}
        if(gaps["front"].house<.45f)flags.Add($"its front faces a wall {F(gaps["front"].house)} away");}
       else{float m=gaps.Values.Min(s=>s.house);if(m>.04f)flags.Add($"case piece {F(m)} from the nearest wall (front unknown)");}}
      // nothing big alone in the open
      {float mh=gaps.Values.Min(s=>s.house);var cen=At((x0+x1)*.5f,(z0+z1)*.5f,(y0+y1)*.5f);
       var near=Physics.OverlapBox(cen,new Vector3(w*.5f+.6f,h*.5f,dp*.5f+.6f),q,1<<VisProp,QueryTriggerInteraction.Collide).Select(c=>owner.TryGetValue(c,out var ow)?ow:null).Where(ow=>ow&&!tree.Contains(ow)&&!FlatWords.Any(ow.name.ToLowerInvariant().Contains)).Distinct().ToList();
       if(mh>.5f&&near.Count==0&&(Mathf.Max(w,dp)>.45f||h>.8f))flags.Add($"stands alone in the open (no wall within {F(mh)}, nothing within 60 cm)");}
      var c0=At(x0,z0,y0);var c1=At(x1,z0,y0);var c2=At(x1,z1,y0);var c3=At(x0,z1,y0);var fw=q*Vector3.forward;
      tsv.AppendLine(string.Join("\t",new[]{p.Index.ToString(),id,prefab,y0.ToString("F2"),fy.ToString("F0"),known?"1":"0",caseGood?"1":"0",c0.x.ToString("F3"),c0.z.ToString("F3"),c1.x.ToString("F3"),c1.z.ToString("F3"),c2.x.ToString("F3"),c2.z.ToString("F3"),c3.x.ToString("F3"),c3.z.ToString("F3"),fw.x.ToString("F2"),fw.z.ToString("F2"),F(gaps["front"].house),F(gaps["back"].house),F(gaps["left"].house),F(gaps["right"].house),string.Join(" | ",flags)}));
      if(flags.Count>0){flagged++;sb.AppendLine($"p{p.Index} {id} ({prefab}) at {o.x:F2},{o.z:F2} y {y0:F2}, {w:F2}x{dp:F2}x{h:F2} m, front {(known?fy.ToString("F0"):"?")}: {string.Join("; ",flags)}   [gaps front {F(gaps["front"].house)}, back {F(gaps["back"].house)}, left {F(gaps["left"].house)}, right {F(gaps["right"].house)}]");}}}
    finally{Unvisual(vis);}}
   // the plans
   var n0=NeutralLight();var cam=AuditCam();var map=new StringBuilder();
   try{foreach(var p in county.Properties.OrderBy(x=>x.Index)){if(!p.Building)continue;var ib=p.InteriorBounds;var lay=LayoutIds(p.Index);
     var floors=lay.Values.Where(i=>!i.wall&&!i.ceiling&&string.IsNullOrEmpty(i.on)).Select(i=>Mathf.Round(i.floor*10f)/10f).Distinct().OrderBy(x=>x).ToList();if(floors.Count==0)floors.Add(Mathf.Round(p.Door.position.y*10f)/10f);
     foreach(var fl in floors){cam.orthographic=true;float size=Mathf.Max(ib.extents.x,ib.extents.z)+1.5f;cam.orthographicSize=size;cam.transform.position=new Vector3(ib.center.x,fl+2.05f,ib.center.z);cam.transform.rotation=Quaternion.Euler(90,0,0);cam.nearClipPlane=.01f;cam.farClipPlane=2.9f;
      var file=$"plan-p{p.Index}-{fl:F1}.jpg";Shoot(cam,Path.Combine(dir,file),1400,1400);map.AppendLine($"{file}\t{p.Index}\t{fl:F2}\t{ib.center.x:F3}\t{ib.center.z:F3}\t{size:F3}");}}}
   finally{EndNeutral(n0);Object.DestroyImmediate(cam.gameObject);}
   File.WriteAllText(Path.Combine(dir,"plans.txt"),map.ToString());
   }finally{if(county.LateRoad)county.LateRoad.SetActive(lateWas);}
   // the walls for the plans: the cut-away cannot show them (their tops are open), so sample what is drawn at 1.2 m
   foreach(var line in File.ReadAllLines(Path.Combine(dir,"plans.txt")).Where(l=>l.Contains("\t"))){var a=line.Trim().Split('\t');var p=county.Properties.First(x=>x.Index==int.Parse(a[1]));float fl=float.Parse(a[2]),cx=float.Parse(a[3]),cz=float.Parse(a[4]),size=float.Parse(a[5]);
    var owner=new Dictionary<Collider,Transform>();var vis=Visuals(p,new List<Transform>(),owner);
    try{const int N=700;var tex=new Texture2D(N,N,TextureFormat.RGBA32,false);var px=new Color32[N*N];float cell=2*size/N;
     for(int j=0;j<N;j++)for(int i=0;i<N;i++){float x=cx-size+(i+.5f)*cell,z=cz-size+(j+.5f)*cell;bool hi=Physics.CheckSphere(new Vector3(x,fl+1.2f,z),cell*.5f,1<<VisHouse,QueryTriggerInteraction.Collide);bool lo=!hi&&Physics.CheckSphere(new Vector3(x,fl+.45f,z),cell*.5f,1<<VisHouse,QueryTriggerInteraction.Collide);
      px[j*N+i]=hi?new Color32(255,255,255,255):lo?new Color32(150,150,255,255):new Color32(0,0,0,0);}
     tex.SetPixels32(px);tex.Apply();File.WriteAllBytes(Path.Combine(dir,"walls-"+a[0].Replace(".jpg",".png")),tex.EncodeToPNG());Object.DestroyImmediate(tex);}
    finally{Unvisual(vis);}}
   sb.Insert(0,$"Sense28: {checkedN} standing pieces, {flagged} with findings"+System.Environment.NewLine);
   File.WriteAllText(Path.Combine(dir,"sense28.txt"),sb.ToString());File.WriteAllText(Path.Combine(dir,"items.tsv"),tsv.ToString());}
 }
}
