using UnityEngine;
namespace ServiceGameV2 {
 // V28b (review): the paper taped to a front door shows that night's note - the night-two hand where the resident left a
 // new one (it showed the first night's paper every night, and Vale's lacked her "SHUT THE DOOR")
 public sealed class ServiceDoorNote : MonoBehaviour {
  public Renderer Paper;
  public Material FirstNight, LaterNights;
  public void Show(int night){if(!Paper)return;var m=night>0&&LaterNights?LaterNights:FirstNight;if(m&&Paper.sharedMaterial!=m)Paper.sharedMaterial=m;}
  public static void ShowAll(int night){foreach(var n in FindObjectsByType<ServiceDoorNote>(FindObjectsInactive.Include,FindObjectsSortMode.None))n.Show(night);}
 }
}
