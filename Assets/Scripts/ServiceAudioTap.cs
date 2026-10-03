using System.IO;
using System.Collections.Concurrent;
using UnityEngine;
namespace ServiceGameV2 {
 // V21 review tour: records the game's final sound mix (what the listener hears) to a WAV, so review videos carry the
 // score, the dread layers and the foley. Sits on the AudioListener's GameObject; started and stopped by the tour.
 public sealed class ServiceAudioTap:MonoBehaviour {
  readonly ConcurrentQueue<float[]> blocks=new ConcurrentQueue<float[]>();volatile bool on;FileStream file;int channels=2,rate=48000;long samples;
  public void StartFile(string path){StopFile();rate=AudioSettings.outputSampleRate;channels=AudioSettings.speakerMode==AudioSpeakerMode.Mono?1:2;file=new FileStream(path,FileMode.Create);file.Write(new byte[44],0,44);samples=0;while(blocks.TryDequeue(out _)){}on=true;}
  void OnAudioFilterRead(float[] data,int ch){if(!on)return;channels=ch;var copy=new float[data.Length];System.Array.Copy(data,copy,data.Length);blocks.Enqueue(copy);}
  void Update(){Flush();}
  void Flush(){if(file==null)return;while(blocks.TryDequeue(out var b)){var bytes=new byte[b.Length*2];for(int i=0;i<b.Length;i++){short v=(short)Mathf.Clamp(b[i]*32767f,-32768,32767);bytes[i*2]=(byte)v;bytes[i*2+1]=(byte)(v>>8);}file.Write(bytes,0,bytes.Length);samples+=b.Length;}}
  public void StopFile(){if(file==null)return;on=false;Flush();long dataBytes=samples*2;file.Seek(0,SeekOrigin.Begin);var w=new BinaryWriter(file);
   w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write((int)(36+dataBytes));w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)channels);w.Write(rate);w.Write(rate*channels*2);w.Write((short)(channels*2));w.Write((short)16);
   w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write((int)dataBytes);w.Flush();file.Close();file=null;}
  void OnDestroy(){StopFile();}
 }
}
