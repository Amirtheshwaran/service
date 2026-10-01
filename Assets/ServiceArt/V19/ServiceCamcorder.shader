// SERVICE V19 - low-fidelity "found footage" camera look in the manner of Fears to Fathom:
// the frame is rendered at a low internal resolution (URP render scale, point upscaling) and this pass,
// running at that resolution, crushes and tints the shadows, reduces colour depth with dithering,
// adds animated grain, a little chromatic fringing and a strong vignette.
// All grading happens in gamma (display) space. V19.0 graded and quantized in linear space, where the bottom
// colour step is already sRGB 31: dim skin rounded to "red only" and every dark area broke into coloured speckle.
Shader "Service/Camcorder"
{
    Properties
    {
        _Grain ("Grain amount", Range(0, 0.4)) = 0.04
        _GrainFps ("Grain frame rate", Range(1, 60)) = 24
        _Levels ("Colour levels per channel", Range(4, 256)) = 72
        _Dither ("Dither strength", Range(0, 2)) = 0.6
        _Lift ("Shadow lift", Color) = (0.010, 0.013, 0.020, 1)
        _Tint ("Tint", Color) = (0.86, 0.95, 1.02, 1)
        _Saturation ("Saturation", Range(0, 1.5)) = 0.72
        _Contrast ("Contrast", Range(0.5, 1.6)) = 1.04
        _Toe ("Shadow toe (display luma)", Range(0.01, 0.8)) = 0.35
        _ToePower ("Shadow toe steepness", Range(1, 3)) = 1.9
        _Exposure ("Exposure", Range(0.2, 3)) = 1.18
        _Vignette ("Vignette", Range(0, 2)) = 1.05
        _Fringe ("Chromatic fringe (texels)", Range(0, 4)) = 1.1
        _Strength ("Overall strength", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off
        Pass
        {
            Name "Camcorder"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Grain, _GrainFps, _Levels, _Dither, _Saturation, _Contrast, _Exposure, _Vignette, _Fringe, _Strength, _Toe, _ToePower;
            float4 _Lift, _Tint;

            // Dave Hoskins' hash-without-sine: well distributed for integer pixel coordinates.
            float Hash(float2 p) { float3 p3 = frac(float3(p.xyx) * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return frac((p3.x + p3.y) * p3.z); }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 texel = _BlitTexture_TexelSize.x > 0 ? _BlitTexture_TexelSize.xy : _ScreenSize.zw;
                float2 fromCentre = uv - 0.5;
                // Chromatic fringe grows toward the frame edges, like a cheap lens.
                float2 shift = fromCentre * texel * _Fringe * 2.0 * saturate(length(fromCentre) * 2.2);
                float3 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv).rgb;
                float3 c;
                c.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv + shift).r;
                c.g = source.g;
                c.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv - shift).b;

                c *= _Exposure;
                float3 g = pow(max(c, 0.0), 1.0 / 2.2);
                float luma = dot(g, float3(0.2126, 0.7152, 0.0722));
                g = lerp(luma.xxx, g, _Saturation);
                // Night toe: below _Toe the response steepens, the way a cheap sensor sinks its shadows. The whole
                // colour is scaled by one factor from its luma, so hue survives; above _Toe nothing changes.
                float toe = luma < _Toe ? pow(max(luma, 1e-5) / max(_Toe, 1e-3), _ToePower - 1.0) : 1.0;
                g *= toe;
                g = max((g - 0.5) * _Contrast + 0.5, 0.0);
                g = g * _Tint.rgb + _Lift.rgb * (1.0 - saturate(luma * 4.0));
                float shown = luma * toe;

                // Grain: one luminance noise value per internal pixel, a new field a couple of dozen times a second.
                float2 pixel = floor(uv / texel);
                float frame = floor(_Time.y * _GrainFps);
                float2 seed = pixel + float2(frame * 37.0, frame * 91.0);
                float n = Hash(seed) + Hash(seed + 71.3) - 1.0;   // triangular noise, film-like
                // Strongest in the mid-darks, faint in true black (so the night stays black, not static) and in highlights.
                float grainWeight = smoothstep(0.0, 0.1, shown) * lerp(1.0, 0.55, saturate(shown * 1.6)) + 0.2;
                g += n * _Grain * grainWeight;

                float2 v = fromCentre * float2(1.0, 0.82);
                float vig = saturate(1.0 - dot(v, v) * _Vignette * 1.9);
                g *= vig * vig * (3.0 - 2.0 * vig);

                // Reduced colour depth in display steps, dithered with the same random field so banding breaks up.
                float threshold = Hash(seed + 13.7) - 0.5;
                float levels = max(_Levels - 1.0, 1.0);
                g = floor(saturate(g) * levels + 0.5 + threshold * _Dither) / levels;
                c = pow(g, 2.2);

                return float4(lerp(source, saturate(c), _Strength), 1);
            }
            ENDHLSL
        }
    }
}
