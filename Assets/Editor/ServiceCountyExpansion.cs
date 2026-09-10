using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace ServiceGameV2.Editor {
public static partial class ServiceBuild {
 static readonly Dictionary<string,string> propPaths=new Dictionary<string,string>();
 static readonly Vector3[] PropertyCenters={V(-67,0,92),V(102,0,235),V(-28,0,469),V(85,0,152),V(-76,0,294),V(88,0,359),V(-17,0,-3)};
 static void CreateFirstShift(){
  scene.Properties=new ServiceProperty[6];
  scene.Properties[0]=StockHome(0,"Pref_Cabin1_A",PropertyCenters[0],90,.82f,V(3.5f,1.05f,4.05f),V(-2,1.05f,-1),V(2.5f,1.05f,-1));
  scene.Properties[1]=Property(1,"Pref_Villa1_A",PropertyCenters[1],270);
  scene.Properties[2]=StockHome(2,"Pref_Cabin1_A",PropertyCenters[2],90,.72f,V(3.5f,1.05f,4.05f),V(-2,1.05f,-1),V(2.5f,1.05f,-1));
  scene.Properties[3]=StockHome(3,"Pref_Cabin2_A",PropertyCenters[3],270,.7f,V(3,1.05f,8.5f),V(3,1.05f,3),V(9,1.05f,3));
  scene.Properties[4]=StockHome(4,"Pref_BrickHouse_A",PropertyCenters[4],270,.72f,V(-6,1.05f,-5.6f),V(-14,1.05f,1),V(-8,1.05f,1),true);
  scene.Properties[5]=StockHome(5,"Pref_Villa2_A",PropertyCenters[5],90,.62f,V(8,4.08f,-4.5f),V(30,10.08f,5),V(38,10.08f,5),true);
  string[] addresses={"214 Millbrook Road","77 Latigo Trail","1 County Route 9","236 Millbrook Road","91 Latigo Trail","108 Latigo Trail"};
  string[] names={"Correll residence","Vale House","Unsurveyed parcel","Harrow Lodge","Bell residence","Morrow House"};
  string[] instructions={"Leave the notice on the kitchen table.","Upstairs study. Take the right staircase.","Leave the copy inside.","Leave the copy inside. If you hear breathing: look away. Stay still.","Leave the copy on the dining table.","Take the central stairs. Deliver to the upstairs gallery."};
  string[] reveals={"That was not the door settling.","The footsteps have stopped. Yours haven't.","You have been here before.","Do not give it your face.","Someone is walking through the rooms behind you.","The house is awake."};
  string[] deaths={"It came through before you reached the porch.","You never made it down the stairs.","There is no record of your return.","It heard you move.","The footsteps stopped beside you.","It knew which door you would choose."};
  foreach(var p in scene.Properties){
   p.Address=addresses[p.Index];p.Brief=names[p.Index];p.Instructions=instructions[p.Index];p.RevealLine=reveals[p.Index];p.DeathLine=deaths[p.Index];p.Encounter=p.Index==2?EncounterKind.None:p.Index==3?EncounterKind.LookAway:EncounterKind.Pursuit;p.CreatureVariant=p.Index==3?2:p.Index==4?1:p.Index==5?2:0;
   if(string.IsNullOrEmpty(p.Template))p.Template="Pref_Villa1_A";
   p.InteriorBounds=BoundsOf(p.Building.gameObject);
   p.Building.gameObject.AddComponent<ServiceSurface>().Kind="wood";
   DressPropertyGrounds(p);
  }
 }
 static ServiceProperty StockHome(int index,string template,Vector3 position,float yaw,float scale,Vector3 entrance,Vector3 desk,Vector3 spawn,bool reverse=false){
  var holder=Empty("Delivery property "+index,position,world);var p=holder.gameObject.AddComponent<ServiceProperty>();p.Index=index;p.Template=template;
  var house=Building(template,position,yaw,scale,holder);p.Building=house.transform;
  var outward=house.transform.forward*(reverse?-1:1);var at=house.transform.TransformPoint(entrance);
  p.Door=Empty("Original entrance",at,holder);p.Door.rotation=Quaternion.LookRotation(outward);
  p.Gate=Empty("Roadside parking",at+outward*30,holder);p.Gate.rotation=Quaternion.LookRotation(-outward);
  var table=Prop("Prop_SmallTable_B",house.transform.TransformPoint(desk),yaw,.58f,holder);var b=BoundsOf(table);
  p.DeliveryPoint=Empty("Leave notice here",V(b.center.x,b.max.y+.015f,b.center.z),holder);
  p.TableApproach=Empty("Desk approach",house.transform.TransformPoint(desk+V(0,0,1.6f)),holder);
  p.EntitySpawn=Empty("Presence",house.transform.TransformPoint(spawn),holder);
  p.SoundPoint=Empty("Sound behind the wall",house.transform.TransformPoint(spawn)+Vector3.up,holder);
  p.PostedPaper=Box("Service copy",Vector3.zero,V(.23f,.005f,.31f),paper,holder,false);p.PostedPaper.transform.SetPositionAndRotation(p.DeliveryPoint.position,Quaternion.Euler(0,yaw+7,0));p.PostedPaper.SetActive(false);
  p.Curtains=new Renderer[0];
  p.PorchLight=LightAt("Porch light",at+outward*.2f+Vector3.up*2.2f,new Color(1,.68f,.37f),2,9,holder);
  p.WindowLight=LightAt("Occupied room",p.DeliveryPoint.position+Vector3.up*1.4f,new Color(1,.67f,.34f),1.5f,7,holder);
  Prop("Prop_Lamp_A",V(b.center.x,b.max.y,b.center.z),yaw,.23f,holder);
  Prop("Prop_Rug_B",house.transform.TransformPoint(desk+V(0,.025f,1.6f)),yaw,.55f,holder);
  if(index==0||index==2)Prop("Prop_ParkBench_A",house.transform.TransformPoint(V(-1,1.05f,3.35f)),yaw,.62f,holder);
  var lights=new List<Light>();
  foreach(var v in new[]{entrance+V(0,2,0),desk+V(0,3,0)})lights.Add(LightAt("House practical light",house.transform.TransformPoint(v),new Color(1,.68f,.40f),1.0f,6,holder));
  p.EncounterLights=lights.ToArray();
  return p;
 }
 static void DressPropertyGrounds(ServiceProperty p){
  var holder=p.transform;var at=p.Door.position;var outward=p.Door.forward;var right=Vector3.Cross(Vector3.up,outward);
  var roadPoint=route.OrderBy(v=>Vector3.Distance(v,at)).First();var inward=at-roadPoint;inward.y=0;inward.Normalize();
  var gate=roadPoint+inward*11;gate.y=.04f;p.Gate.SetPositionAndRotation(gate,Quaternion.LookRotation(inward));
  var end=at+outward*2;end.y=.045f;var bend=Vector3.Lerp(gate,end,.55f)+right*(p.Index%2==0?6:-6);bend.y=.045f;
  var path=Curve(new[]{gate,bend,end});p.ApproachRoute=path.ToArray();
  var gravel=Mat("Driveway gravel "+p.Index,new Color(.67f,.65f,.58f),"GR_Dirt1_AS");Ribbon("Garden path "+p.Index,path,1.65f,gravel,holder);
  var pathObject=holder.Find("Garden path "+p.Index);if(pathObject){pathObject.gameObject.AddComponent<MeshCollider>().sharedMesh=pathObject.GetComponent<MeshFilter>().sharedMesh;pathObject.gameObject.AddComponent<ServiceSurface>().Kind="gravel";}
  // The pedestrian gate keeps the service vehicle on the road side of the garden.
  var across=Vector3.Cross(Vector3.up,inward);var fenceLine=gate+inward*3;
  for(int side=-1;side<=1;side+=2)for(int n=0;n<3;n++)Prop("Struct_Fence2_Mid_A",fenceLine+across*side*(2.45f+n*3.2f),Quaternion.LookRotation(inward).eulerAngles.y,.48f,holder);
  Sign(p.Address.ToUpperInvariant()+"\n"+p.Brief.ToUpperInvariant(),gate+across*2.4f+outward*2+Vector3.up*1.55f,Quaternion.LookRotation(inward).eulerAngles.y,.082f,holder);
  var park=Prop("Prop_Car_A",gate+across*6-outward*.5f,Quaternion.LookRotation(inward).eulerAngles.y+90,.68f,holder);
  foreach(var c in park.GetComponentsInChildren<Collider>())if(c is MeshCollider m)m.convex=false;
  for(int i=3;i<path.Count-2;i+=5){
   var pos=path[i];var tangent=(path[i+1]-path[i-1]).normalized;var side=Vector3.Cross(Vector3.up,tangent);
   foreach(int s in new[]{-1,1}){
    var boxAt=pos+side*s*2.1f;boxAt.y=LandHeight(boxAt.x,boxAt.z);Prop("Struct_FlowerBox_A",boxAt,Quaternion.LookRotation(tangent).eulerAngles.y,.38f,holder);
    ResizePlant(Prop("Grass_Tall_A",boxAt+Vector3.up*.33f,i*41+s*17,.42f,holder),.55f);
   }
   if(i%10==3){var lampAt=pos+side*2.6f;var post=Box("Garden lamp post",Vector3.zero,V(.055f,1.55f,.055f),metal,holder);post.transform.position=lampAt+Vector3.up*.77f;Prop("Prop_Lamp_E",lampAt+Vector3.up*1.55f,0,.24f,holder,false);LightAt("Garden amber pool",lampAt+Vector3.up*1.6f,new Color(1,.64f,.30f),.8f,6,holder);}
  }
  var benchAt=at+outward*5+right*4;benchAt.y=.03f;Prop("Prop_ParkBench_A",benchAt,p.Door.eulerAngles.y,.62f,holder);
  Prop("Prop_SmallTable_A",benchAt+outward*1.3f,p.Door.eulerAngles.y,.45f,holder);
  for(int side=-1;side<=1;side+=2){var b=at+outward*4+right*side*7;b.y=0;ResizePlant(Prop("DecoBush_C",b,p.Index*33,.9f,holder),1.05f);}
  if(p.Index==4){for(int n=0;n<7;n++){var g=at+right*(8+n%3*2.5f)+outward*(8+n/3*3);g.y=0;Prop("Prop_Gravestone_"+(n%2==0?"B":"D"),g,p.Door.eulerAngles.y,.65f,holder);}}
  var reverb=Empty("Room acoustics",p.InteriorBounds.center,holder).gameObject.AddComponent<AudioReverbZone>();reverb.reverbPreset=p.Index<4?AudioReverbPreset.Livingroom:AudioReverbPreset.StoneCorridor;reverb.minDistance=3;reverb.maxDistance=9;
 }
 static bool ClearForPlay(Vector3 at,float margin){
  if(RoadDistance(at)<4+margin)return false;
  foreach(var p in scene.Properties){var b=p.InteriorBounds;b.Expand(6+margin*2);if(at.x>b.min.x&&at.x<b.max.x&&at.z>b.min.z&&at.z<b.max.z)return false;
   if(Vector3.Distance(at,p.Gate.position)<10+margin)return false;
   if(p.ApproachRoute.Any(v=>Vector3.Distance(at,v)<3+margin))return false;}
  return Vector3.Distance(at,V(-17,0,-3))>26&&Vector3.Distance(at,V(-91,0,211))>18;
 }
 static void DressDenseCounty(){
  var plants=Empty("Forest canopy and understory",Vector3.zero,world);var rng=new System.Random(21477);
  float R(float lo,float hi)=>(float)(lo+(hi-lo)*rng.NextDouble());
  string[] trees={"TreeCreator_Tall_C","TreeCreator_Tall_B","TreeCreator_Crinkly_A","TreeCreator_Small_A","TreeCreator_Crinkly_B"};
  for(float z=18;z<426;z+=8)for(float x=-128;x<150;x+=8){var at=V(x+R(-3,3),0,z+R(-3,3));if(!ClearForPlay(at,1.8f))continue;at.y=LandHeight(at.x,at.z);var tree=Prop(trees[rng.Next(trees.Length)],at,R(0,360),R(.75f,1.08f),plants);ResizePlant(tree,R(12,21));GameObjectUtility.SetStaticEditorFlags(tree,StaticEditorFlags.BatchingStatic);
   var bushAt=at+V(R(-3,3),0,R(-3,3));bushAt.y=0;if(ClearForPlay(bushAt,.8f)){bushAt.y=LandHeight(bushAt.x,bushAt.z);var bush=Prop("TreeCreator_Small_A",bushAt,R(0,360),.15f,plants);ResizePlant(bush,R(1.5f,2.5f));foreach(var c in bush.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);}
  }
  for(int i=0;i<route.Count;i++)foreach(int s in new[]{-1,1}){var at=route[i]+Vector3.right*s*4.2f;if(!ClearForPlay(at,0))continue;at.y=LandHeight(at.x,at.z);var grass=Prop(i%2==0?"Grass_Small_C":"Grass_Tall_A",at,i*47,R(.75f,1.2f),plants);foreach(var r in grass.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;}
  Building("Pref_Barn1_A",V(-91,0,211),25,.6f,world);
  foreach(var p in scene.Properties.Where(p=>p.HasEncounter)){var yard=p.Door.position+p.Door.right*12;yard.y=0;Prop(p.Index%2==0?"Struct_Pavilion_A":"Struct_Kiosk_A",yard,p.Door.eulerAngles.y,.45f,world);}
 }
 static void ResizePlant(GameObject plant,float height){var b=BoundsOf(plant);var bottom=V(b.center.x,b.min.y,b.center.z);plant.transform.localScale*=height/Mathf.Max(.01f,b.size.y);b=BoundsOf(plant);plant.transform.position+=bottom-V(b.center.x,b.min.y,b.center.z);}
 static void CreatureVariants(){
  var root=scene.Entity;var original=root.transform.GetChild(0).gameObject;var controller=original.GetComponentInChildren<Animator>(true).runtimeAnimatorController;
  var variants=new List<GameObject>{original};
  for(int i=2;i<=3;i++){
   var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Creep Horror Creature/Prefabs/Creep"+i+".prefab"));PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);model.transform.SetParent(root.transform,false);model.transform.localPosition=Vector3.zero;
   var b=BoundsOf(model);model.transform.localScale*=2.15f/b.size.y;b=BoundsOf(model);model.transform.position+=V(root.transform.position.x-b.center.x,root.transform.position.y-b.min.y,root.transform.position.z-b.center.z);Convert(model);
   foreach(var a in model.GetComponentsInChildren<Animator>(true)){a.runtimeAnimatorController=controller;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
   foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=9;foreach(var c in model.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);model.SetActive(false);variants.Add(model);
  }
  scene.EntityVariants=variants.ToArray();
 }
}}
