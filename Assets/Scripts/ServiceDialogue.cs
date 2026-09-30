using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
namespace ServiceGameV2 {
 // Fears to Fathom style doorstep conversation: the speaker's line as a named subtitle, then two or three short replies (1/2/3).
 public sealed class ServiceDialogue:MonoBehaviour {
  public sealed class Step {
   public readonly string Line;public readonly string[] Choices,Replies;
   public Step(string line,string[] choices=null,string[] replies=null){Line=line;Choices=choices??new string[0];Replies=replies;}
  }
  ServiceDirector d;
  public bool Active {get;private set;}
  public string Speaker {get;private set;}="";
  public string Line {get;private set;}="";
  public string[] Choices {get;private set;}=new string[0];
  public string StateKey=>Active?Speaker+"|"+Line+"|"+string.Join("/",Choices):"";
  public void Initialize(ServiceDirector director){d=director;}
  public void Cancel(){Active=false;Speaker=Line="";Choices=new string[0];}
  public IEnumerator Run(string speaker,Step[] steps,Action<int> onChoice=null){
   Active=true;Speaker=speaker;
   foreach(var s in steps){
    Speaker=speaker;Line=s.Line;Choices=new string[0];yield return Hold(Read(s.Line));
    if(s.Choices.Length==0)continue;
    Choices=s.Choices;int pick=-1;float auto=Time.time+.4f;
    while(pick<0){
     if(d.IsSmoke&&Time.time>auto)pick=0;
     var k=Keyboard.current;
     if(k!=null){if(k.digit1Key.wasPressedThisFrame||k.numpad1Key.wasPressedThisFrame)pick=0;else if(s.Choices.Length>1&&(k.digit2Key.wasPressedThisFrame||k.numpad2Key.wasPressedThisFrame))pick=1;else if(s.Choices.Length>2&&(k.digit3Key.wasPressedThisFrame||k.numpad3Key.wasPressedThisFrame))pick=2;}
     yield return null;
    }
    onChoice?.Invoke(pick);Choices=new string[0];
    Speaker="You";Line=s.Choices[pick];yield return Hold(1.5f);Speaker=speaker;
    if(s.Replies!=null&&pick<s.Replies.Length&&!string.IsNullOrEmpty(s.Replies[pick])){Line=s.Replies[pick];yield return Hold(Read(Line));}
   }
   Cancel();
  }
  static float Read(string line)=>Mathf.Clamp(line.Length*.05f,1.8f,4.6f);
  IEnumerator Hold(float seconds){
   if(d.IsSmoke)seconds=Mathf.Min(seconds,.35f);
   float start=Time.time;
   while(Time.time-start<seconds){var k=Keyboard.current;if(k!=null&&Time.time-start>.4f&&(k.eKey.wasPressedThisFrame||k.spaceKey.wasPressedThisFrame))break;yield return null;}
  }
 }
}
