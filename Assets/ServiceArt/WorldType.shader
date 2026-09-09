Shader "Service/World Type" {
 Properties { _MainTex("Font atlas",2D)="white" {} }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
 Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off ZTest LEqual Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
 struct Input { float4 position:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; };
 struct Output { float4 position:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR; };
 Output vert(Input i){Output o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;o.color=i.color;return o;}
 half4 frag(Output i):SV_Target {return half4(i.color.rgb,i.color.a*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a);}
 ENDHLSL
 } }
}
