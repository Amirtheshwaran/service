using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ServiceGameV2.Editor {public static class ServiceV12Inspect {public static void Run(){
EditorSceneManager.OpenScene("Assets/Scenes/HollisCounty.unity");
var s=Object.FindAnyObjectByType<CountyScene>();
var lines=s.Properties.Select(p=>$"PROPERTY {p.Index} {p.Address} door={p.Door.position} gate={p.Gate.position} spawn={p.EntitySpawn.position} table={p.TableApproach.position} kind={p.Encounter}\n{p.Brief}\n{p.Instructions}\n{p.RevealLine}\n{p.DeathLine}").ToList();
foreach(var id in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Creep Horror Creature","Assets/Demon Horror Creature with Weapon"})){var path=AssetDatabase.GUIDToAssetPath(id);foreach(var c in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")))lines.Add(path+" :: "+c.name+" :: "+c.length);}
File.WriteAllLines(Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"v12-inspect.txt"),lines);
}}}
