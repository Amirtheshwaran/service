using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
 public static class ServiceV16Scene {
  static List<string> log;static CountyScene s;static Terrain terrain;
  static Bounds BoundsOf(Transform t){var r=t.GetComponentsInChildren<Renderer>(true);var b=r[0].bounds;foreach(var x in r.Skip(1))b.Encapsulate(x.bounds);return b;}
  static void Remove(Transform t){if(!t)return;log.Add("Removed "+ServiceV16Audit.PathOf(t));Object.DestroyImmediate(t.gameObject);}
  public static void Apply(){
   s=ServiceV16Audit.Open();terrain=s.GetComponentInChildren<Terrain>();log=new List<string>();
   Remove(s.transform.Find("Return path roadworks"));
   foreach(var p in s.Properties){
    // The tiny table copies and fences in the access lanes have no purpose here.
    foreach(Transform t in p.transform.Cast<Transform>().ToArray()){
     if(t.name.StartsWith("Struct_Fence")||t.name.StartsWith("Prop_Chair"))Remove(t);
     else if(t.name.StartsWith("Prop_SmallTable_A")&&!p.InteriorBounds.Contains(BoundsOf(t).center))Remove(t);
    }
   }
   foreach(var t in s.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Struct_Kiosk")||t.name.StartsWith("Struct_Pavilion")).ToArray())Remove(t);
   foreach(var tree in s.GetComponentsInChildren<Transform>(true).Where(t=>(t.name.StartsWith("TreeCreator_")&&!t.name.Contains("Bush"))||t.name.StartsWith("PF Conifer")).ToArray()){
    float ground=terrain.SampleHeight(tree.position)+terrain.transform.position.y;
    // These authored trees have buried roots below their trunk-origin plane.
    // Seating the deepest root vertex on the surface exposed the rest of the root ring.
    float lowest=ground;foreach(var offset in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})lowest=Mathf.Min(lowest,terrain.SampleHeight(tree.position+offset*.48f)+terrain.transform.position.y);
    float y=Mathf.Min(tree.position.y,lowest-.16f);if(tree.position.y-y>.02f){log.Add("Trunk origin grounded "+tree.name+" "+tree.position+" -> "+y);tree.position=new Vector3(tree.position.x,y,tree.position.z);}
    var col=tree.GetComponent<CapsuleCollider>();if(col)col.center=tree.InverseTransformPoint(new Vector3(tree.position.x,ground+3,tree.position.z));
   }
   // Native door meshes from each building family, fitted to the measured clear openings.
   var vale=s.Properties.Single(p=>p.Index==1);FitDoor(vale,"Villa1/Villa1_Door_A",vale.Door.position,1.02f,2.65f);
   var morrow=s.Properties.Single(p=>p.Index==5);morrow.Door.position=new Vector3(83.01f,2.53f,359);FitDoor(morrow,"Villa2/Villa2_Door_B",morrow.Door.position,2.25f,3.10f);
   // The other matching opening is closed with the same authored door asset.
   var second=DoorMesh(morrow,"Villa2/Villa2_Door_B",new Vector3(83.01f,2.53f,363.96f),2.25f,3.10f);second.name="Locked matching side entrance";
   var late=s.Properties.Single(p=>p.Index==2);FitDoor(late,"Cabins/Cabin2_Door_A",late.Door.position,1.1f,2.2f);
   foreach(var p in s.Properties){
    if(p.KnockPoint)Remove(p.KnockPoint);var point=new GameObject("Door interaction target").transform;point.SetParent(p.DoorPanel,false);point.position=BoundsOf(p.DoorPanel).center+p.Door.forward*.08f;p.KnockPoint=point;
    string[] instructions={"Speak to the resident.","Right-hand stairs, then left along the landing. The study is the last room.","Leave the envelope on the table beyond the sitting room.","Leave the copy on the desk in the back room.","Speak to the resident.","Enter here. Take the stairs to the landing, then follow the hall to the lit desk."};p.Instructions=instructions[p.Index];
    string[] notes={"CORRELL\nPlease knock. I'm home.","VALE HOUSE\nDeliveries: use the right-hand stairs.\nAt the landing, turn left.\nLeave documents on the desk in the last room.","Please come in. Leave any post on the table\nbeyond the sitting room.","Leave documents on the back-room desk.\nIf you hear breathing behind you:\nturn away and stay still until it stops.","BELL RESIDENCE\nPlease knock at this entrance.","MORROW HOUSE\nCome in. Upstairs, follow the landing\nto the lamp at the far end.\nLeave the envelope on that desk."};p.NoticeText=notes[p.Index];
    var sign=p.transform.Cast<Transform>().Where(t=>t.name=="County sign").OrderBy(t=>Vector3.Distance(t.position,p.Door.position)).FirstOrDefault();
    if(sign){var text=sign.GetComponentInChildren<TextMesh>();if(text){text.text=p.Index==0||p.Index==4?"RESIDENT AT HOME\nPLEASE KNOCK":"DELIVERIES\nREAD NOTICE HERE";text.fontSize=64;text.characterSize=.021f;text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/Barlow-Regular.ttf");text.GetComponent<Renderer>().sharedMaterial=text.font.material;p.NoticePoint=text.transform;}
     sign.position=p.Door.position+p.Door.forward*1.25f+p.Door.right*1.45f+Vector3.up*.65f;
    }
   }
   var road=s.GetComponentInChildren<ServiceReturnRoad>();
   foreach(var t in road.transform.Cast<Transform>().Where(t=>t.name=="road-square").ToArray())Remove(t);
   // Source road tiles meet at their edges; trim the adjoining long sections first.
   var segments=road.transform.Cast<Transform>().Where(t=>t.name=="road-straight").ToArray();
   for(int i=0;i<segments.Length;i++){var t=segments[i];var a=road.Points[i];var b=road.Points[i+1];var dir=(b-a).normalized;float trimA=3.5f,trimB=i==segments.Length-1?0:3.5f;var from=a+dir*trimA;var to=b-dir*trimB;t.position=(from+to)*.5f+Vector3.up*.035f;t.localScale=new Vector3(7,1,Vector3.Distance(from,to));}
   Tile("road-bend",new Vector3(-32,.035f,385),180,road.transform);
   Tile("road-bend",new Vector3(-32,.035f,-18),90,road.transform);
   Tile("road-bend",new Vector3(0,.035f,-18),0,road.transform);
   // A single intersection replaces the overlapping northern square and county board.
   Tile("road-intersection",new Vector3(-4,.05f,385),0,road.transform);
   foreach(Transform t in s.transform.Cast<Transform>().Where(t=>t.name=="County sign"&&t.position.z>375).ToArray())Remove(t);
   var word=road.transform.Find("Depot direction lettering");var board=road.transform.Find("sign-highway");if(word&&board){Vector3 delta=new Vector3(1.8f,0,-7);word.position+=delta;board.position+=delta;word.GetComponent<TextMesh>().text="DEPOT\nRETURN ROAD  <";}
   // Depot direction must be readable on the incoming side as well.
   var depotSign=s.transform.Cast<Transform>().FirstOrDefault(t=>t.name=="County sign"&&t.position.z<0);if(depotSign){foreach(var text in depotSign.GetComponentsInChildren<TextMesh>())text.text="HOLLIS COUNTY\nDEPOT\nPARK TO FILE REPORT";var reverse=Object.Instantiate(depotSign.gameObject,s.transform);reverse.name="Depot sign — return approach";reverse.transform.position=new Vector3(-27,1.8f,-4);reverse.transform.rotation=Quaternion.Euler(0,270,0);}
   ServiceBuild.RebakeV13(s);EditorSceneManager.MarkSceneDirty(s.gameObject.scene);EditorSceneManager.SaveScene(s.gameObject.scene);AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(ServiceV16Audit.Work,"v16-scene-repairs.txt"),log);ServiceV16Audit.Preview(s,"After");
  }
  static void Tile(string asset,Vector3 pos,float yaw,Transform parent){var o=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/External/KenneyRoads/"+asset+".fbx"),parent);o.name="V16 joined "+asset;o.transform.position=pos;o.transform.rotation=Quaternion.Euler(0,yaw,0);o.transform.localScale=new Vector3(7,1,7);var material=s.transform.Find("Millbrook and Latigo").GetComponent<Renderer>().sharedMaterial;foreach(var r in o.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;foreach(var f in o.GetComponentsInChildren<MeshFilter>())f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;}
  static void FitDoor(ServiceProperty p,string asset,Vector3 floor,float width,float height){p.DoorPanel=DoorMesh(p,asset,floor,width,height);p.DoorPanel.name="Working authored front door";}
  static Transform DoorMesh(ServiceProperty p,string asset,Vector3 floor,float width,float height){
   var o=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Buildings/"+asset+".prefab"),p.transform);o.transform.localScale=Vector3.one;o.transform.rotation=Quaternion.Euler(0,90,0);var b=BoundsOf(o.transform);
   if(b.size.x>b.size.z)o.transform.Rotate(0,90,0);b=BoundsOf(o.transform);o.transform.localScale=new Vector3(width/b.size.z,height/b.size.y,1);b=BoundsOf(o.transform);o.transform.position+=floor+Vector3.up*height*.5f-b.center;
   // Pivot the original leaf about its hinge, keeping its authored appearance.
   var pivot=new GameObject("Door hinge").transform;pivot.SetParent(p.transform);pivot.position=floor+Vector3.forward*width*.5f;o.transform.SetParent(pivot,true);
   foreach(var r in o.GetComponentsInChildren<Renderer>()){foreach(var m in r.sharedMaterials){if(m&&m.shader.name=="Standard"){m.shader=Shader.Find("Universal Render Pipeline/Lit");if(m.HasProperty("_MainTex"))m.SetTexture("_BaseMap",m.GetTexture("_MainTex"));EditorUtility.SetDirty(m);}}}
   return pivot;
  }
 }
}
