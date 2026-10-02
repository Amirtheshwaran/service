using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace ServiceGameV2 {
 // Drives the Fears to Fathom style camera: internal resolution follows the screen so the pixel size stays
 // constant (~400 px tall frame at full strength), and the camcorder material strength follows the option.
 public sealed class ServiceCamcorder:MonoBehaviour {
  public const string Pref="SERVICE.camcorder";
  public static readonly string[] Names={"Off","Subtle","Strong","Extreme"};
  static readonly float[] Heights={0,720,380,270};
  public int Level {get;private set;}=2;
  // V20: picture brightness (Options > Video), multiplies the camcorder exposure. Saved as SERVICE.brightness.
  public static float Brightness {get;private set;}=1;
  public void SetBrightness(float value){Brightness=Mathf.Clamp(value,.6f,1.6f);PlayerPrefs.SetFloat("SERVICE.brightness",Brightness);Apply();}
  Material mat;int lastHeight=-1,lastLevel=-1;
  void Start(){Level=Mathf.Clamp(PlayerPrefs.GetInt(Pref,2),0,3);Brightness=Mathf.Clamp(PlayerPrefs.GetFloat("SERVICE.brightness",1),.6f,1.6f);foreach(var r in Resources.FindObjectsOfTypeAll<Material>())if(r.shader&&r.shader.name=="Service/Camcorder"){mat=r;break;}Apply();}
  // Survey and tests preview a level without touching the player's saved choice.
  public void Preview(int level){Level=Mathf.Clamp(level,0,3);Apply();}
  public void Cycle(){Level=(Level+1)%Names.Length;PlayerPrefs.SetInt(Pref,Level);Apply();}
  public void Set(int level){Level=Mathf.Clamp(level,0,3);PlayerPrefs.SetInt(Pref,Level);Apply();}
  void Update(){if(Screen.height!=lastHeight||Level!=lastLevel)Apply();}
  void Apply(){
   lastHeight=Screen.height;lastLevel=Level;
   var asset=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
   // Whole-number upscale factors keep every low-res pixel the same size on screen.
   if(asset){int factor=Level==0?1:Mathf.Max(1,Mathf.RoundToInt(Screen.height/Heights[Level]));asset.renderScale=1f/factor;asset.upscalingFilter=Level==0||factor==1?UpscalingFilterSelection.Auto:UpscalingFilterSelection.Point;}
   if(mat){mat.SetFloat("_Strength",Level==0?0:1);mat.SetFloat("_Grain",Level==1?.022f:Level==2?.04f:.06f);mat.SetFloat("_Levels",Level==1?160:Level==2?72:44);mat.SetFloat("_Dither",Level==3?.8f:.6f);mat.SetFloat("_Fringe",Level==1?.6f:Level==2?1.1f:1.5f);mat.SetFloat("_Vignette",Level==1?.8f:Level==2?1.05f:1.25f);
    mat.SetFloat("_Toe",Level==1?.25f:Level==2?.35f:.4f);mat.SetFloat("_ToePower",Level==1?1.4f:Level==2?1.9f:2.1f);mat.SetFloat("_Contrast",1.04f);mat.SetFloat("_Exposure",1.18f*Brightness);mat.SetColor("_Lift",new Color(.010f,.013f,.020f,1));mat.SetFloat("_Grain",Level==1?.02f:Level==2?.045f:.07f);mat.SetFloat("_Levels",Level==1?160:Level==2?64:36);}
  }
 }
}
