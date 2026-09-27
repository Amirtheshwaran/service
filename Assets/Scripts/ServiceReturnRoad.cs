using UnityEngine;
namespace ServiceGameV2 {public sealed class ServiceReturnRoad:MonoBehaviour {
 public Vector3[] Points;public GameObject NorthClosure;ServiceDirector director;
 void Start(){director=GetComponentInParent<ServiceDirector>();}
 void Update(){if(NorthClosure&&director)NorthClosure.SetActive(director.NightIndex<2);}
}}
