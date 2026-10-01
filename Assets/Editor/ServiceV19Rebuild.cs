using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ServiceGameV2.Editor {
 // V19: return to the monsters-only story and remove everything that reads as randomly placed.
 // Every stage is idempotent and writes what it touched to work/v19-rebuild.txt.
 public static partial class ServiceV19Rebuild {
  const string ScenePath="Assets/Scenes/HollisCounty.unity";
  static readonly StringBuilder log=new StringBuilder();
  static string Work=>Directory.GetParent(Application.dataPath).Parent.FullName;
  static CountyScene county;
  static CountyScene Open(){var scene=EditorSceneManager.OpenScene(ScenePath);county=Object.FindAnyObjectByType<CountyScene>();return county;}
  static void Save(string stage){EditorSceneManager.MarkSceneDirty(county.gameObject.scene);EditorSceneManager.SaveScene(county.gameObject.scene);AssetDatabase.SaveAssets();File.AppendAllText(Path.Combine(Work,"v19-rebuild.txt"),"==== "+stage+"\n"+log);log.Clear();}
  static string PathOf(Transform t){var s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s.Replace("\n"," ");}
  static void Kill(Transform t,string why){if(!t)return;log.AppendLine($"REMOVED {PathOf(t)}  ({why})");Object.DestroyImmediate(t.gameObject);}
  static IEnumerable<Transform> All(bool includeInactive=true)=>county.GetComponentsInChildren<Transform>(includeInactive);

  public static void Cleanup(){Open();CleanupStage();Save("cleanup");}
  static void CleanupStage(){
   // V18 story and furniture.
   foreach(var t in All().Where(t=>t&&t.name.StartsWith("V18 furniture — ")).ToList())Kill(t,"V18 wall-scan furniture");
   foreach(var t in All().Where(t=>t&&t.name=="The man — tree line figure").ToList())Kill(t,"monsters-only story: no human stalker");
   if(county.EntityVariants!=null){var keep=new List<GameObject>();foreach(var v in county.EntityVariants){if(v&&v.name.StartsWith("The man")){Kill(v.transform,"monsters-only story: no human pursuer");continue;}if(v)keep.Add(v);}county.EntityVariants=keep.ToArray();}
   // Residents: no depot worker wandering the yard; unused hidden duplicates go too.
   var res=Object.FindAnyObjectByType<ServiceResidents>();
   if(res)foreach(Transform c in res.transform.Cast<Transform>().ToList())if(c.name.StartsWith("Sophia")||c.name.StartsWith("Nathan"))Kill(c,c.name.StartsWith("Nathan")?"depot worker wandering the yard":"unused hidden resident duplicate");
   // One dog: the V17 model follows the transform that ServiceLife walks and barks with.
   var oldDog=All().FirstOrDefault(t=>t.name=="Correll yard dog");var newDog=All().FirstOrDefault(t=>t.name.StartsWith("Correll yard dog — German Shepherd"));
   if(oldDog&&newDog&&newDog.parent!=oldDog){newDog.SetParent(oldDog,true);newDog.localPosition=Vector3.zero;log.AppendLine("DOG V17 model now follows the walking yard-dog transform");}
   // Yard props that do not belong at a private house in the woods.
   foreach(var p in county.Properties){
    foreach(var t in p.GetComponentsInChildren<Transform>(true).Where(t=>t&&t.parent==p.transform).ToList()){
     var n=t.name;
     if(n.StartsWith("Prop_ParkBench"))Kill(t,"park bench at a private house");
     else if(n.StartsWith("Prop_Gravestone"))Kill(t,"gravestone grid beside a house");
     else if(n.StartsWith("Struct_FlowerBox")||(n.StartsWith("Grass_Tall_A")&&t.position.y>.2f))Kill(t,"municipal flower box in the woods");
     else if(n.StartsWith("Prop_Car_A")&&p.Index!=2)Kill(t,"duplicate of the player's county sedan parked at the house");
     else if(n=="County sign")Kill(t,"black DELIVERIES/PLEASE KNOCK board (notes move to the door)");
     else if(n=="Room directions"||n.StartsWith("STUDY"))Kill(t,"sign inside a private house");
    }
    if(p.NoticePoint==null||!p.NoticePoint)p.NoticePoint=null;
    foreach(var tm in p.GetComponentsInChildren<TextMesh>(true).Where(x=>x.text.Contains("STUDY")).ToList())Kill(tm.transform,"sign inside a private house");
    EditorUtility.SetDirty(p);
   }
   // Kit road pieces, highway sign and duplicate depot sign from the old return loop.
   var lane=All().FirstOrDefault(t=>t.name=="Depot return lane");if(lane)Kill(lane,"car-kit return loop replaced by one county road");
   var dup=All().FirstOrDefault(t=>t.name=="Depot sign — return approach");if(dup)Kill(dup,"second depot sign on the removed loop");
  }
 }
}
