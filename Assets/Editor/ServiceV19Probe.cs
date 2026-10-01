using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static class ServiceV19Probe {
  public static void EasyRoads(){
   var sb=new StringBuilder();
   foreach(var asm in System.AppDomain.CurrentDomain.GetAssemblies().Where(a=>a.GetName().Name.Contains("EasyRoads"))){
    sb.AppendLine("ASSEMBLY "+asm.GetName().Name);
    System.Type[] types;try{types=asm.GetTypes();}catch(System.Reflection.ReflectionTypeLoadException e){types=e.Types.Where(t=>t!=null).ToArray();}
    foreach(var t in types.Where(t=>t.IsPublic).OrderBy(t=>t.FullName)){
     sb.AppendLine("TYPE "+t.FullName+(t.BaseType!=null?" : "+t.BaseType.Name:""));
     if(!t.FullName.Contains("ER"))continue;
     foreach(var m in t.GetMethods(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.DeclaredOnly))sb.AppendLine("   M "+(m.IsStatic?"static ":"")+m.ReturnType.Name+" "+m.Name+"("+string.Join(", ",m.GetParameters().Select(p=>p.ParameterType.Name+" "+p.Name))+")");
     foreach(var f in t.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.DeclaredOnly))sb.AppendLine("   F "+f.FieldType.Name+" "+f.Name);
     foreach(var p in t.GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.DeclaredOnly))sb.AppendLine("   P "+p.PropertyType.Name+" "+p.Name);
    }
   }
   File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName,"Audit","easyroads-api.txt"),sb.ToString());
  }
 }
}
