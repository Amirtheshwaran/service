using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V21 roads ("make the roads better, don't leave weird road blocks lying around"):
 //  - the 5.8 m-wide gravel pull-off pads at every drive mouth read as pale slabs beside the road in the headlights; each
 //    drive now meets the road in a short apron that flares the way a real driveway does and narrows to the lane within
 //    a few metres (same centre line, same surface and collider);
 //  - the gravel is toned down toward the asphalt and the forest floor;
 //  - the four concrete barriers at the survey closure stood staggered and skewed like dumped blocks: they now stand in
 //    one straight, evenly spaced line square across the road;
 //  - tall grass grows along both verges of the county road and Route 9 in natural clumps (Flooded Grounds grass,
 //    already used across the county), so the asphalt no longer ends in a hard line on bare dirt.
 public static partial class ServiceV19Rebuild {
  public static void Roads21(){Open();Roads21Stage();Save("roads21");}
  static float ApronHalf(float s)=>1.55f+1.1f*Mathf.Exp(-s/1.7f);
  static void Roads21Stage(){
   AssetDatabase.Refresh();var t=county.GetComponentInChildren<Terrain>();var outDir="Assets/ServiceArt/V21/Road";Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName,outDir));
   // 1. drive aprons
   foreach(var p in county.Properties){
    var mf=county.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m=>m.name.StartsWith($"V19 drive {p.Index} "));if(!mf){log.AppendLine($"ROADS21 no drive mesh for {p.Index}");continue;}
    var src=mf.sharedMesh;var v=src.vertices;int n=v.Length/2;var P=new List<Vector3>();var S=new List<float>();float s=0;
    for(int i=0;i<n;i++){var c=(mf.transform.TransformPoint(v[i*2])+mf.transform.TransformPoint(v[i*2+1]))*.5f;if(i>0)s+=Vector2.Distance(new Vector2(P[i-1].x,P[i-1].z),new Vector2(c.x,c.z));P.Add(c);S.Add(s);}
    var verts=new List<Vector3>();var uvs=new List<Vector2>();var tris=new List<int>();
    for(int i=0;i<n;i++){var f=(i<n-1?P[i+1]-P[i]:P[i]-P[i-1]);f.y=0;f.Normalize();var r=new Vector3(f.z,0,-f.x);float h=ApronHalf(S[i])*1.18f;
     foreach(var (lat,u) in new[]{(-h,0f),(h,1f)}){var w=P[i]+r*lat;verts.Add(mf.transform.InverseTransformPoint(w));uvs.Add(new Vector2(u,S[i]/3.2f));}}
    for(int i=0;i<n-1;i++){int a=i*2;tris.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
    var mesh=new Mesh{name=$"V21 drive {p.Index}",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(verts);mesh.SetUVs(0,uvs);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();
    var nm=mesh.normals;if(nm.Length>0&&nm.Average(x=>x.y)<0){var tr=mesh.triangles;for(int i=0;i<tr.Length;i+=3){var x=tr[i+1];tr[i+1]=tr[i+2];tr[i+2]=x;}mesh.triangles=tr;mesh.RecalculateNormals();}
    mesh.RecalculateTangents();mesh.RecalculateBounds();var path=$"{outDir}/V21 drive {p.Index}.asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);
    mf.sharedMesh=mesh;var mc=mf.GetComponent<MeshCollider>();if(mc)mc.sharedMesh=mesh;EditorUtility.SetDirty(mf);
    log.AppendLine($"ROADS21 drive {p.Index}: apron {ApronHalf(0)*2:F1} m at the road narrowing to {ApronHalf(6)*2:F1} m by 6 m (was a {2.9f*2:F1} m pad for 6 m), {s:F0} m long");}
   // 2. gravel toned toward the asphalt and the forest floor
   var gravel=AssetDatabase.LoadAssetAtPath<Material>($"{V19}/Road/V19 gravel drive.mat");if(gravel){gravel.SetColor("_BaseColor",new Color(.6f,.58f,.55f));gravel.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(gravel);log.AppendLine("ROADS21 gravel darkened");}
   // 3. the survey closure: one straight line square across the road
   var main=Centre(MainRoad,t,.8f,9);var closure=county.transform.Find("North survey closure/Barricade");
   if(closure){var end=At(main,main.Length-2.2f,out var right);var fwd=Vector3.Cross(right,Vector3.up);var kids=closure.Cast<Transform>().ToList();
    float[] offs={-2.45f,-.82f,.82f,2.45f};for(int i=0;i<kids.Count&&i<offs.Length;i++){var pos=end+right*offs[i];pos.y=GroundAt(pos+Vector3.up*2)-.01f;kids[i].SetPositionAndRotation(pos,Quaternion.LookRotation(fwd));}
    log.AppendLine($"ROADS21 closure: {kids.Count} barriers squared up across the road at {end}");}
   // 4. verge grass
   var old=county.transform.Find("V21 verge grass");if(old)Object.DestroyImmediate(old.gameObject);var holder=new GameObject("V21 verge grass").transform;holder.SetParent(county.transform,false);
   var prefabs=new[]{"Grass_Tall_A","Grass_Tall_C","Grass_Tall_B","Grass_Small_C"}.Select(nm2=>AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Flooded_Grounds/Prefabs/Nature/Grass/{nm2}.prefab")).Where(x=>x).ToArray();
   var late=Centre(LateRoadPath,t,.8f,9);
   var gates=county.Properties.Select(p=>p.Gate.position).ToList();var mailboxes=county.Properties.Where(p=>p.Mailbox).Select(p=>p.Mailbox.position).ToList();
   var blockers=county.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Utility pole")||x.name.StartsWith("Street sign")||x.name.StartsWith("County sign")||x.name.StartsWith("Depot sign")).Select(x=>x.position).ToList();
   var rng=new System.Random(2109);int placed=0;var unconverted=new HashSet<string>();
   void Verge(Line l,float half,float from,float to,Transform parent){
    for(float s2=from;s2<to;s2+=.4f){var c=At(l,s2,out var right);
     foreach(int side in new[]{-1,1}){
      float clump=Mathf.PerlinNoise(s2*.11f+side*31.7f,side*4.2f+7.1f);if(clump<.36f)continue;
      int count=clump>.68f?4:clump>.52f?3:2;
      for(int k=0;k<count;k++){
       float lat=half+.06f+Mathf.Pow((float)rng.NextDouble(),1.6f)*1.4f*(clump>.6f?1:.7f);var pos=c+right*side*lat+Vector3.Cross(right,Vector3.up)*((float)rng.NextDouble()-.5f)*.45f;
       if(gates.Any(g=>Vector2.Distance(new Vector2(g.x,g.z),new Vector2(pos.x,pos.z))<7))continue;
       if(mailboxes.Any(m=>Vector2.Distance(new Vector2(m.x,m.z),new Vector2(pos.x,pos.z))<1.6f))continue;
       if(blockers.Any(b=>Vector2.Distance(new Vector2(b.x,b.z),new Vector2(pos.x,pos.z))<1.2f))continue;
       pos.y=TerrainY(t,pos);var pf=prefabs[rng.Next(prefabs.Length)];var g2=(GameObject)PrefabUtility.InstantiatePrefab(pf,parent);g2.transform.SetPositionAndRotation(pos,Quaternion.Euler(0,(float)rng.NextDouble()*360,0));
       float sc=.6f+(float)rng.NextDouble()*.45f*(clump>.65f?1.15f:1);g2.transform.localScale*=sc;g2.isStatic=true;placed++;
       // the county's grass wears URP conversions of the Flooded Grounds materials (the prefab's own shader is magenta in URP)
       foreach(var rd in g2.GetComponentsInChildren<Renderer>()){var ms=rd.sharedMaterials;for(int m=0;m<ms.Length;m++){if(!ms[m])continue;var conv=AssetDatabase.LoadAssetAtPath<Material>($"Assets/ServiceArt/Materials/{ms[m].name}.mat")??(ms[m].name=="NAT_Grass"?AssetDatabase.LoadAssetAtPath<Material>("Assets/ServiceArt/Materials/NAT_Grass_14.mat"):null);if(conv)ms[m]=conv;else unconverted.Add(ms[m].name);}rd.sharedMaterials=ms;rd.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}}}}
   }
   if(prefabs.Length>0){Verge(main,MainHalf,16,main.Length-9,holder);
    var lateParent=holder;if(county.LateRoad){var oldLate=county.LateRoad.transform.Find("V21 verge grass (Route 9)");if(oldLate)Object.DestroyImmediate(oldLate.gameObject);lateParent=new GameObject("V21 verge grass (Route 9)").transform;lateParent.SetParent(county.LateRoad.transform,false);}
    Verge(late,LateHalf,6,late.Length-4,lateParent);}
   if(unconverted.Count>0)log.AppendLine("ROADS21 grass materials without a URP conversion: "+string.Join(", ",unconverted));
   log.AppendLine($"ROADS21 verge grass: {placed} clumps along the county road and Route 9 ({prefabs.Length} Flooded Grounds grass prefabs)");
  }
 }
}
