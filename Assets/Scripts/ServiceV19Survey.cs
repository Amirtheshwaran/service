using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
namespace ServiceGameV2 {
 // V19 visual survey: photographs every place a player can see, so layout defects can be reviewed image by image.
 // Run: Service.exe -serviceSmoke -serviceSurvey [-surveySet=props,plans,roads,signs,forest,trees,depot,look] with SERVICE_CAPTURE_DIR set.
 public sealed class ServiceV19Survey:MonoBehaviour {
  ServiceDirector d;string dir;Camera cam;UniversalAdditionalCameraData camData;Light lamp,fill;readonly StringBuilder manifest=new StringBuilder();int shots;
  HashSet<string> sets;bool bright=true;
  public void Run(ServiceDirector director){d=director;dir=Environment.GetEnvironmentVariable("SERVICE_CAPTURE_DIR");Directory.CreateDirectory(dir);StartCoroutine(Guarded());}
  static string Arg(string name,string fallback){var a=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith("-"+name+"="));return a==null?fallback:a.Substring(name.Length+2);}
  IEnumerator Guarded(){var e=Main();while(true){object cur;try{if(!e.MoveNext())break;cur=e.Current;}catch(Exception ex){Debug.LogException(ex);File.AppendAllText(Path.Combine(dir,"survey-errors.txt"),ex+"\n");break;}yield return cur;}
   File.WriteAllText(Path.Combine(dir,"manifest.jsonl"),manifest.ToString());File.WriteAllText(Path.Combine(dir,"done.txt"),"shots="+shots);Application.Quit();}
  IEnumerator Main(){
   yield return new WaitForSeconds(1.5f);
   sets=new HashSet<string>(Arg("surveySet","props,plans,roads,signs,forest,trees,depot,look").Split(','));
   var ui=d.transform.Find("Service interface");if(ui)ui.gameObject.SetActive(false);
   cam=new GameObject("Survey camera").AddComponent<Camera>();cam.CopyFrom(d.Scene.View);camData=cam.gameObject.AddComponent<UniversalAdditionalCameraData>();camData.renderPostProcessing=true;cam.fieldOfView=68;cam.nearClipPlane=.05f;cam.farClipPlane=400;
   d.Scene.View.enabled=false;if(d.Camcorder)d.Camcorder.Preview(0);
   lamp=new GameObject("Survey work light").AddComponent<Light>();lamp.type=LightType.Spot;lamp.spotAngle=80;lamp.innerSpotAngle=50;lamp.range=35;lamp.intensity=6;lamp.color=new Color(1f,.96f,.9f);lamp.shadows=LightShadows.Soft;lamp.transform.SetParent(cam.transform,false);
   fill=new GameObject("Survey fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.35f;fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(60,30,0);
   if(sets.Contains("catalog")){yield return Catalog();if(sets.Count==1)yield break;}
   if(sets.Contains("pov")){yield return Pov();if(sets.Count==1)yield break;}
   foreach(int night in new[]{0,1,2}){
    d.BeginShift(night);d.PaperOpen=false;yield return null;Brighten(true);
    if(sets.Contains("look")&&night==0)yield return TrueLook();
    Brighten(true);
    foreach(var p in d.Scene.Properties.OrderBy(x=>x.Index)){
     bool exists=p.gameObject.activeInHierarchy;if(!exists)continue;
     if(night==1&&p.Index!=4)continue;           // night two only differs at Bell's (no answer)
     if(night==2&&p.Index!=2)continue;           // the late parcel only exists on night three
     if(sets.Contains("props"))yield return Property(p,night);
     if(sets.Contains("plans"))yield return Plans(p,night);
    }
    if(night==0){if(sets.Contains("signs"))yield return Signs(night);if(sets.Contains("roads"))yield return Roads(night);if(sets.Contains("forest"))yield return Forest();if(sets.Contains("trees"))yield return Trees();if(sets.Contains("depot"))yield return Depot();}
    if(night==2&&sets.Contains("roads"))yield return Roads(night);
   }
  }
  void Brighten(bool on){bright=on;fill.enabled=on;lamp.enabled=true;if(on){RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.34f,.36f,.4f);RenderSettings.fogDensity=Mathf.Min(RenderSettings.fogDensity,.006f);}}
  // The player's own camera, HUD, hands and flashlight under the intended camcorder filter, as a player sees them.
  IEnumerator PovShot(string name,string info){yield return new WaitForSeconds(.35f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));shots++;manifest.AppendLine("{\"id\":\""+name+"\",\"cat\":\"pov\",\"info\":\""+info+"\"}");yield return new WaitForSeconds(.25f);}
  IEnumerator PovFace(Vector3 target){var delta=target-d.Scene.View.transform.position;d.Player.SmokeLook(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);yield return new WaitForSeconds(.3f);}
  IEnumerator Pov(){
   d.BeginShift(0);d.PaperOpen=false;cam.enabled=false;d.Scene.View.enabled=true;fill.enabled=false;lamp.enabled=false;
   var ui=d.transform.Find("Service interface");if(ui)ui.gameObject.SetActive(true);
   if(d.Scene.Flashlight)d.Scene.Flashlight.enabled=true;if(d.Camcorder)d.Camcorder.Preview(2);
   var car=d.Scene.Car;d.Player.SmokePlaceWalker(car.position-car.right*2.2f-car.forward*1.5f);yield return new WaitForSeconds(5.4f);
   yield return PovFace(car.position+Vector3.up*.8f);yield return PovShot("pov-depot-car","depot, standing by the county car, Strong filter");
   var depotSign=GameObject.Find("Depot sign — Hollis County Road Department");if(depotSign){yield return PovFace(depotSign.transform.position+Vector3.up*1.5f);yield return PovShot("pov-depot-sign","depot sign");}
   var street=GameObject.Find("Street sign — Millbrook Rd");if(street){d.Player.SmokePlaceWalker(street.transform.position+street.transform.forward*4f);yield return new WaitForSeconds(.4f);yield return PovFace(street.transform.position+Vector3.up*2.2f);yield return PovShot("pov-sign-millbrook","Millbrook Rd street sign from the road");}
   d.Player.SmokeLook(d.Scene.Walker.transform.eulerAngles.y,38);yield return new WaitForSeconds(.4f);yield return PovShot("pov-hands-down","looking down: hands and torch");
   d.Player.SmokeLook(d.Scene.Walker.transform.eulerAngles.y,-35);yield return new WaitForSeconds(.4f);yield return PovShot("pov-hands-up","looking up: nothing of the arms should show");
   d.Player.SmokeLook(d.Scene.Walker.transform.eulerAngles.y,0);var hands=d.Scene.View.GetComponentInChildren<ServiceHands>();
   if(hands){hands.Interact(true);yield return new WaitForSeconds(.45f);yield return PovShot("pov-hands-knock","mid knock (left fist)");yield return new WaitForSeconds(1.4f);hands.Interact(false);yield return new WaitForSeconds(.35f);yield return PovShot("pov-hands-reach","mid reach (left hand sets papers down)");yield return new WaitForSeconds(1.2f);}
   foreach(var p in d.Scene.Properties.Where(x=>x.gameObject.activeInHierarchy)){
    if(p.Index==2&&d.Scene.LateRoad)d.Scene.LateRoad.SetActive(true);
    var o=Outward(p);d.Player.SmokePlaceWalker(p.Door.position+o*1.5f);yield return new WaitForSeconds(.5f);
    yield return PovFace(p.Door.position+Vector3.up*1.35f);yield return PovShot($"pov-p{p.Index}-door","at the front door, Strong filter");
    if(p.NoticePoint&&p.NoticePoint.gameObject.activeInHierarchy){yield return PovFace(p.NoticePoint.position);yield return PovShot($"pov-p{p.Index}-note-aim","aiming at the taped note (prompt + ring)");
     int n=d.NearbyNotice();if(n==p.Index){d.ReadNotice(n);yield return PovShot($"pov-p{p.Index}-note-read","the note held up to read");d.CloseNote();}else{manifest.AppendLine("{\"id\":\"pov-p"+p.Index+"-note-unreachable\",\"cat\":\"pov\",\"info\":\"NearbyNotice="+n+"\"}");}}
    if(d.Life)d.Life.OpenDoor(p,true);yield return new WaitForSeconds(1.6f);yield return PovFace(p.Door.position-o*3+Vector3.up*1.3f);yield return PovShot($"pov-p{p.Index}-open","door open, looking in");
    d.Player.SmokePlaceWalker(p.TableApproach.position);yield return new WaitForSeconds(.5f);yield return PovFace(p.DeliveryPoint.position);yield return PovShot($"pov-p{p.Index}-table","at the delivery table");
    if(d.Life)d.Life.OpenDoor(p,false);
   }
   if(d.Camcorder)d.Camcorder.Preview(3);d.Player.SmokePlaceWalker(car.position-car.right*2.2f-car.forward*1.5f);yield return new WaitForSeconds(.5f);yield return PovFace(car.position+Vector3.up*.8f);yield return PovShot("pov-depot-car-extreme","depot, Extreme filter");
   if(d.Camcorder)d.Camcorder.Preview(0);cam.enabled=true;d.Scene.View.enabled=false;if(ui)ui.gameObject.SetActive(false);lamp.enabled=true;
  }
  IEnumerator TrueLook(){
   // The player's actual view: no fill, game ambient restored by re-running the shift setup.
   d.BeginShift(0);d.PaperOpen=false;fill.enabled=false;lamp.enabled=false;if(d.Camcorder)d.Camcorder.Preview(2);yield return null;
   var car=d.Scene.Car;yield return Shot("look-depot-car",car.position+car.forward*-6+Vector3.up*1.7f,car.position+Vector3.up,"look",0,-1,"true game lighting, no work light");
   foreach(var p in d.Scene.Properties.Where(x=>x.gameObject.activeInHierarchy)){lamp.enabled=true;lamp.intensity=2.2f;lamp.spotAngle=45;yield return Shot($"look-p{p.Index}-porch",Porch(p,5),p.Door.position+Vector3.up*1.3f,"look",0,p.Index,"true game lighting with a flashlight-strength light");}
   lamp.intensity=6;lamp.spotAngle=80;if(d.Camcorder)d.Camcorder.Preview(0);
  }
  Vector3 Outward(ServiceProperty p){var c=p.InteriorBounds.size.sqrMagnitude>1?p.InteriorBounds.center:(p.Building?p.Building.position:p.Door.position-p.Door.forward);var o=p.Door.position-c;o.y=0;return o.sqrMagnitude<.01f?p.Door.forward:o.normalized;}
  Vector3 Porch(ServiceProperty p,float dist){return p.Door.position+Outward(p)*dist+Vector3.up*1.6f;}
  IEnumerator Property(ServiceProperty p,int n){
   string id=$"p{p.Index}-n{n}";var door=p.Door.position;var outw=Outward(p);
   yield return Shot(id+"-ext-parking",p.Gate.position+Vector3.up*1.6f,door+Vector3.up*1.5f,"exterior",n,p.Index,"from the roadside parking spot looking at the front door");
   var route=NavPath(p.Gate.position,door);
   if(route.Count>1){var mid=At(route,Length(route)*.5f);yield return Shot(id+"-ext-drive",mid+Vector3.up*1.6f,door+Vector3.up*1.5f,"exterior",n,p.Index,"halfway up the drive looking at the front door");}
   yield return Shot(id+"-ext-porch",Porch(p,4),door+Vector3.up*1.3f,"exterior",n,p.Index,"4 m in front of the entrance looking at the door");
   yield return Shot(id+"-ext-porch-left",Porch(p,3)+Vector3.Cross(Vector3.up,outw)*3,door+Vector3.up*1.3f,"exterior",n,p.Index,"porch from the left");
   yield return Shot(id+"-ext-porch-right",Porch(p,3)-Vector3.Cross(Vector3.up,outw)*3,door+Vector3.up*1.3f,"exterior",n,p.Index,"porch from the right");
   if(p.NoticePoint)yield return Shot(id+"-notice",p.NoticePoint.position-p.NoticePoint.forward*1.8f+Vector3.up*.1f,p.NoticePoint.position,"exterior",n,p.Index,"the posted notice/sign as a player reads it");
   if(d.Life)d.Life.OpenDoor(p,true);yield return new WaitForSeconds(1.6f);
   yield return Shot(id+"-door-open",door+outw*1.4f+Vector3.up*1.6f,door-outw*3+Vector3.up*1.3f,"entrance",n,p.Index,"front door opened, looking into the entrance");
   var inside=NavPath(door-outw*.9f,p.TableApproach.position);float len=Length(inside);int k=0;
   for(float s=1.2f;s<len;s+=2.4f){var a=At(inside,s);var b=At(inside,Mathf.Min(len,s+1.5f));var dirv=b-a;dirv.y=0;if(dirv.sqrMagnitude<.01f)continue;dirv.Normalize();var eye=a+Vector3.up*1.6f;
    yield return Shot($"{id}-walk{k:00}-ahead",eye,eye+dirv*4-Vector3.up*.35f,"interior",n,p.Index,$"walking from the door to the delivery table, {s:F0} m in, looking ahead");
    yield return Shot($"{id}-walk{k:00}-left",eye,eye+Quaternion.Euler(0,-75,0)*dirv*4-Vector3.up*.35f,"interior",n,p.Index,"same spot looking left");
    yield return Shot($"{id}-walk{k:00}-right",eye,eye+Quaternion.Euler(0,75,0)*dirv*4-Vector3.up*.35f,"interior",n,p.Index,"same spot looking right");k++;}
   var t=p.TableApproach.position+Vector3.up*1.6f;
   yield return Shot(id+"-table",t,p.DeliveryPoint.position,"interior",n,p.Index,"looking at the delivery table from where the player stands");
   for(int a=0;a<4;a++){var fwd=Quaternion.Euler(0,a*90,0)*Vector3.forward;yield return Shot($"{id}-room-{a*90:000}",t,t+fwd*4-Vector3.up*.6f,"interior",n,p.Index,$"delivery room, standing at the table, facing world yaw {a*90}");}
   if(d.Life)d.Life.OpenDoor(p,false);
  }
  IEnumerator Plans(ServiceProperty p,int n){
   var b=p.InteriorBounds;if(b.size.sqrMagnitude<1)yield break;
   var floors=new List<float>();foreach(var probe in new[]{p.Door.position-Outward(p)*1.2f,p.TableApproach.position})if(Physics.Raycast(probe+Vector3.up*.8f,Vector3.down,out var hit,3,~0,QueryTriggerInteraction.Ignore)&&!floors.Any(f=>Mathf.Abs(f-hit.point.y)<1.2f))floors.Add(hit.point.y);
   var prevFog=RenderSettings.fog;RenderSettings.fog=false;lamp.enabled=false;fill.enabled=true;fill.intensity=.9f;fill.transform.rotation=Quaternion.Euler(90,0,0);camData.renderPostProcessing=false;
   foreach(var f in floors){
    float half=Mathf.Max(b.extents.z,b.extents.x/cam.aspect)+1.5f;cam.orthographic=true;cam.orthographicSize=half;cam.nearClipPlane=.05f;cam.farClipPlane=2.6f;
    var pos=new Vector3(b.center.x,f+2.25f,b.center.z);cam.transform.SetPositionAndRotation(pos,Quaternion.Euler(90,0,0));
    yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
    string name=$"p{p.Index}-n{n}-plan-floor{f:F1}";ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir,name+".png"));shots++;
    manifest.AppendLine("{\"id\":\""+name+"\",\"cat\":\"plan\",\"night\":"+n+",\"prop\":"+p.Index+",\"center\":["+b.center.x.ToString("F2")+","+b.center.z.ToString("F2")+"],\"halfHeight\":"+half.ToString("F2")+",\"aspect\":"+cam.aspect.ToString("F4")+",\"floorY\":"+f.ToString("F2")+",\"door\":["+p.Door.position.x.ToString("F2")+","+p.Door.position.z.ToString("F2")+"],\"table\":["+p.DeliveryPoint.position.x.ToString("F2")+","+p.DeliveryPoint.position.z.ToString("F2")+"],\"info\":\"orthographic top-down floor plan cut 2.2 m above the floor; image top = +Z (north), right = +X\"}");
    yield return new WaitForSeconds(.15f);
   }
   cam.orthographic=false;cam.nearClipPlane=.05f;cam.farClipPlane=400;RenderSettings.fog=prevFog;lamp.enabled=true;fill.intensity=.35f;fill.transform.rotation=Quaternion.Euler(60,30,0);camData.renderPostProcessing=true;Brighten(bright);
  }
  IEnumerator Signs(int n){
   int i=0;foreach(var tm in d.Scene.GetComponentsInChildren<TextMesh>(false)){if(tm.transform.IsChildOf(d.Scene.Car))continue;var t=tm.transform;var label=tm.text.Replace("\n"," / ");
    yield return Shot($"sign{i:00}-front",t.position-t.forward*2.4f+Vector3.up*.1f,t.position,"sign",n,-1,$"text [{label}] viewed from its reading side");
    yield return Shot($"sign{i:00}-back",t.position+t.forward*2.4f+Vector3.up*.1f,t.position,"sign",n,-1,$"text [{label}] viewed from behind (text should not show through)");
    yield return Shot($"sign{i:00}-wide",t.position-t.forward*7f+Vector3.up*.3f,t.position,"sign",n,-1,$"sign [{label}] in context from 7 m");i++;}
  }
  List<Vector3> RoutePoints(){var pts=d.Scene.Route.Where(r=>r).Select(r=>r.position).ToList();return pts;}
  IEnumerator Roads(int n){
   var pts=RoutePoints();if(n==2){var late=d.Scene.LateRoad?d.Scene.LateRoad.GetComponentsInChildren<Transform>().Select(t=>t.position).ToList():new List<Vector3>();}
   float len=Length(pts);int k=0;
   for(float s=0;s<len;s+=22){var a=At(pts,s);var b=At(pts,Mathf.Min(len,s+6));var dirv=b-a;dirv.y=0;if(dirv.sqrMagnitude<.01f)continue;dirv.Normalize();var eye=Ground(a)+Vector3.up*1.35f;
    if(n==2&&s<len-120)continue;
    yield return Shot($"road-n{n}-{k:00}-ahead",eye,eye+dirv*20-Vector3.up*1.2f,"road",n,-1,$"driver height on the main road, {s:F0} m from the depot, looking ahead");
    if(k%2==0){yield return Shot($"road-n{n}-{k:00}-back",eye,eye-dirv*20-Vector3.up*1.2f,"road",n,-1,"same spot looking back");
     var side=Vector3.Cross(Vector3.up,dirv);yield return Shot($"road-n{n}-{k:00}-verge",eye+side*3,eye+side*3+dirv*6+side*6-Vector3.up*1.3f,"road",n,-1,"road verge and edge blending");}
    k++;}
   foreach(var p in d.Scene.Properties.Where(x=>x.gameObject.activeInHierarchy)){var drive=NavPath(p.Gate.position,p.Door.position);if(drive.Count<2)continue;float dl=Length(drive);int j=0;for(float s=4;s<dl;s+=14){var a=At(drive,s);var b=At(drive,Mathf.Min(dl,s+4));var dirv=b-a;dirv.y=0;if(dirv.sqrMagnitude<.01f)continue;dirv.Normalize();var eye=a+Vector3.up*1.6f;yield return Shot($"drive-p{p.Index}-n{n}-{j:00}",eye,eye+dirv*10-Vector3.up*1.2f,"driveway",n,p.Index,$"walking up the drive, {s:F0} m from the road");j++;}}
  }
  IEnumerator Forest(){
   var pts=RoutePoints();float len=Length(pts);int k=0;
   for(float s=30;s<len;s+=45){var a=At(pts,s);var b=At(pts,Mathf.Min(len,s+6));var dirv=b-a;dirv.y=0;dirv.Normalize();var side=Vector3.Cross(Vector3.up,dirv);
    foreach(float sgn in new[]{-1f,1f}){var spot=Ground(a+side*sgn*16)+Vector3.up*1.6f;yield return Shot($"forest-{k:00}",spot,spot+side*sgn*12-Vector3.up*.9f,"forest",0,-1,$"16 m off the road looking into the woods");k++;}}
  }
  IEnumerator Trees(){
   var pts=RoutePoints();var picks=new List<Transform>();
   foreach(var t in d.Scene.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("PF Conifer")||t.name.StartsWith("TreeCreator_Tall_C_Dead")||t.name.StartsWith("V16 retained woodland")))
    if(pts.Any(p=>Vector3.Distance(Flat(p),Flat(t.position))<45)&&!picks.Any(q=>Vector3.Distance(q.position,t.position)<25))picks.Add(t);
   int k=0;foreach(var t in picks.Take(24)){var baseP=t.position;var view=baseP+new Vector3(3.2f,1.1f,3.2f);yield return Shot($"tree-{k:00}-{Clean(t.name)}",view,baseP+Vector3.up*.4f,"tree",0,-1,$"base of tree {t.name} (root flare vs ground)");k++;}
   var terrain=d.Scene.GetComponentInChildren<Terrain>();if(terrain){var td=terrain.terrainData;var inst=td.treeInstances;int j=0;var used=new List<Vector3>();
    foreach(var ti in inst){var w=Vector3.Scale(ti.position,td.size)+terrain.transform.position;if(!pts.Any(p=>Vector3.Distance(Flat(p),Flat(w))<40)||used.Any(u=>Vector3.Distance(u,w)<30))continue;used.Add(w);var proto=td.treePrototypes[ti.prototypeIndex].prefab;yield return Shot($"ttree-{j:00}-{Clean(proto?proto.name:"proto")}",w+new Vector3(3.2f,1.1f,3.2f),w+Vector3.up*.4f,"tree",0,-1,$"base of terrain tree {(proto?proto.name:"")}");if(++j>=16)break;}}
  }
  IEnumerator Depot(){var c=d.Scene.Depot?d.Scene.Depot.position:Vector3.zero;int k=0;foreach(var a in new[]{0,70,140,210,280}){var dirv=Quaternion.Euler(0,a,0)*Vector3.forward;var eye=Ground(c+dirv*14)+Vector3.up*1.7f;yield return Shot($"depot-{k++:00}",eye,c+Vector3.up*1.5f,"depot",0,-1,"depot yard");}}
  IEnumerator Catalog(){
   var shelf=d.transform.Find("V19 catalog shelf");if(!shelf)yield break;shelf.gameObject.SetActive(true);RenderSettings.fog=false;fill.enabled=true;fill.intensity=1.1f;fill.transform.rotation=Quaternion.Euler(50,20,0);lamp.enabled=false;camData.renderPostProcessing=false;
   foreach(Transform item in shelf){if(!item.name.StartsWith("Catalog "))continue;var rs=item.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    float dist=Mathf.Max(b.size.x,b.size.y,b.size.z)*1.5f+.8f;var c=b.center;string n=Clean(item.name.Substring(8));
    yield return Shot($"cat-{n}-fromPlusZ",c+new Vector3(0,b.extents.y*.6f+.3f,dist),c,"catalog",0,-1,$"{item.name.Substring(8)} seen from +Z (camera looking toward -Z)");
    yield return Shot($"cat-{n}-fromPlusX",c+new Vector3(dist,b.extents.y*.6f+.3f,0),c,"catalog",0,-1,$"{item.name.Substring(8)} seen from +X (camera looking toward -X)");}
   shelf.gameObject.SetActive(false);RenderSettings.fog=true;camData.renderPostProcessing=true;fill.intensity=.35f;lamp.enabled=true;
  }
  static string Clean(string s)=>new string(s.Where(char.IsLetterOrDigit).Take(24).ToArray());
  static Vector3 Flat(Vector3 v)=>new Vector3(v.x,0,v.z);
  Vector3 Ground(Vector3 p){if(Physics.Raycast(new Vector3(p.x,p.y+40,p.z),Vector3.down,out var h,80,~0,QueryTriggerInteraction.Ignore))return h.point;return p;}
  List<Vector3> NavPath(Vector3 a,Vector3 b){var list=new List<Vector3>();if(NavMesh.SamplePosition(a,out var ha,3,NavMesh.AllAreas)&&NavMesh.SamplePosition(b,out var hb,3,NavMesh.AllAreas)){var np=new NavMeshPath();if(NavMesh.CalculatePath(ha.position,hb.position,NavMesh.AllAreas,np)&&np.corners.Length>1)list.AddRange(np.corners);}if(list.Count<2){list.Clear();list.Add(a);list.Add(b);}return list;}
  static float Length(List<Vector3> pts){float l=0;for(int i=1;i<pts.Count;i++)l+=Vector3.Distance(pts[i-1],pts[i]);return l;}
  static Vector3 At(List<Vector3> pts,float s){for(int i=1;i<pts.Count;i++){float seg=Vector3.Distance(pts[i-1],pts[i]);if(s<=seg)return Vector3.Lerp(pts[i-1],pts[i],seg<1e-4f?0:s/seg);s-=seg;}return pts[pts.Count-1];}
  IEnumerator Shot(string name,Vector3 eye,Vector3 target,string cat,int night,int prop,string info){
   cam.transform.position=eye;var dirv=target-eye;if(dirv.sqrMagnitude<1e-4f)dirv=Vector3.forward;cam.transform.rotation=Quaternion.LookRotation(dirv);
   yield return null;yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
   ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir,name+".png"));shots++;
   manifest.AppendLine("{\"id\":\""+name+"\",\"cat\":\""+cat+"\",\"night\":"+night+",\"prop\":"+prop+",\"eye\":["+eye.x.ToString("F2")+","+eye.y.ToString("F2")+","+eye.z.ToString("F2")+"],\"target\":["+target.x.ToString("F2")+","+target.y.ToString("F2")+","+target.z.ToString("F2")+"],\"info\":\""+info.Replace("\"","'")+"\"}");
   yield return new WaitForSeconds(.12f);
  }
 }
}
