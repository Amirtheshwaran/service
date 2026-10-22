using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace ServiceGameV2.Editor {
 public static partial class ServiceV19Rebuild {
  // V28b (review): each door note gets its night-two paper (note-p<N>-n2.png, from signs.py) to show from night two on
  public static void DoorNotes28(){Open();var sb=new StringBuilder();
   foreach(var p in county.Properties.OrderBy(x=>x.Index)){var note=p.NoticePoint;if(!note||note.name!="Door note"){sb.AppendLine($"p{p.Index}: no door note");continue;}
    var paper=note.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name=="Paper");if(!paper){sb.AppendLine($"p{p.Index}: no paper");continue;}
    var dn=note.GetComponent<ServiceGameV2.ServiceDoorNote>()??note.gameObject.AddComponent<ServiceGameV2.ServiceDoorNote>();
    dn.Paper=paper;if(!dn.FirstNight)dn.FirstNight=paper.sharedMaterial;
    var file=$"note-p{p.Index}-n2.png";bool has=File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName,SignDir,file));
    dn.LaterNights=has?SignMat($"V19 note p{p.Index} n2",file,Color.white,.12f):null;EditorUtility.SetDirty(dn);
    sb.AppendLine($"p{p.Index}: first {dn.FirstNight?.name}, later {(dn.LaterNights?dn.LaterNights.name:"(same paper)")}");}
   log.Append(sb.ToString());File.WriteAllText(Path.Combine(Work,"Audit","v28","doornotes28.txt"),sb.ToString());Save("DoorNotes28");}
 }
}
