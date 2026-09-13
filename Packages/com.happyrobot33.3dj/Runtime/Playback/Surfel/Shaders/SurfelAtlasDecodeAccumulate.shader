Shader "SurfelAtlas/DecodeAccumulate" {
    Properties {
        [Header(Splat)]
        [Tooltip(Global surfel size multiplier)]
        _Radius ("Splat radius", Float) = 1.5
        [Tooltip(Texels per surfel, matches the bake)]
        _Stride ("Stride", Float) = 1

        [Header(Depth Prepass)]
        [Tooltip(How far the prepass pushes back)]
        _DepthOffset ("Depth offset", Float) = 1.0

        [Header(Playback)]
        [Tooltip(Draw even when playback is off)]
        [Toggle(_IGNOREGLOBALPLAYBACKCONTROL_ON)] _IgnoreGlobalPlaybackControl ("Ignore Global Playback Control", Float) = 0
        [Tooltip(Ignore the recorded position)]
        [Toggle(_LOCKPOSITION_ON)] _LockPosition ("Lock Position", Float) = 0
        [Tooltip(Ignore the recorded yaw)]
        [Toggle(_LOCKROTATION_ON)] _LockRotation ("Lock Rotation", Float) = 0

        [Header(Blending)]
        [Tooltip(Two pass, ZWrite, dither, cutout, or accumulate.)]
        [KeywordEnum(TwoPass, ZWrite, Dither, Cutout, Accumulate)] _Mode ("Blend mode", Float) = 0
        [Tooltip(Alpha test threshold)]
        _Cutoff ("Cutout", Range(0,1)) = 0.5

        [Header(Falloff)]
        [Tooltip(Overall alpha boost)]
        _FalloffMult ("Falloff Multiplier", Float) = 1
        [Tooltip(Texture instead of the curve)]
        [Toggle(_FALLOFFTEX_ON)] _UseFalloffTex ("Use falloff texture", Float) = 0
        [Tooltip(How fast a disc fades out.)]
        _Falloff ("Falloff", Float) = 2.5
        [Tooltip(Disc or the whole quad)]
        [Enum(Disc, 0, Rect, 1)] _Shape ("Shape", Float) = 0
        _FalloffTex ("Falloff texture", 2D) = "white" {}

        [Header(Fade)]
        [Tooltip(How far the camera fade reaches)]
        _FadeDistance ("Fade distance", Float) = 0.3
        [Tooltip(How hard it fades up close)]
        _FadeMultiplier ("Fade multiplier", Float) = 1

        [Header(LOD)]
        [Tooltip(Where the first halving starts)]
        _LodDistance ("LOD distance", Float) = 8

        [Header(Accumulation)]
        [Tooltip(Weight range for accumulate mode)]
        _AccScale ("Accumulation scale", Range(0.01, 1)) = 0.125

        [Header(Dither)]
        [Tooltip(Bayer, bayer plus blue noise, or fractal.)]
        [KeywordEnum(Bayer, BlueNoise, Fractal)] _Dither ("Dither", Float) = 1
        _BlueNoise ("Blue noise", 2D) = "gray" {}
        [Tooltip(How much blue noise over bayer.)]
        _BlueNoiseMix ("Blue noise mix", Range(0, 1)) = 0.0525
        [Tooltip(Animate the blue noise.)]
        [Toggle] _BlueNoiseJitter ("Blue noise jitter", Float) = 1
        [HideInInspector] _DitherTex ("Dither 3D Texture", 3D) = "" {}
        [HideInInspector] _DitherRampTex ("Dither Ramp Texture", 2D) = "white" {}
        [Tooltip(Fractal dot size on screen.)]
        _Scale ("Dot Scale", Range(2,10)) = 5.0
        [Tooltip(Dots change count or size.)]
        _SizeVariability ("Dot Size Variability", Range(0,1)) = 0
        [Tooltip(How crisp the dots are.)]
        _Contrast ("Dot Contrast", Range(0,2)) = 1
        [Tooltip(Smoothing on stretched dots.)]
        _StretchSmoothness ("Stretch Smoothness", Range(0,2)) = 1
        [Tooltip(Brighten before dithering.)]
        _InputExposure ("Exposure", Range(0,5)) = 1
        [Tooltip(Shift before dithering.)]
        _InputOffset ("Offset", Range(-1,1)) = 0

        [HideInInspector] _Meta ("", 2D) = "black" {}
        [HideInInspector] _MetaSplat ("", 2D) = "black" {}
        [HideInInspector] _ZWrite ("", Float) = 0
        [HideInInspector] _SrcBlend ("", Float) = 5
        [HideInInspector] _DstBlend ("", Float) = 10
        [HideInInspector] _Strip3DJ ("", 2D) = "black" {}
        [HideInInspector] _SurfelDepth ("", 2D) = "black" {}
        [HideInInspector] _SurfelColor ("", 2D) = "black" {}
    }

    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Cull Off

        CGINCLUDE
        #include "SurfelAtlasDecode.cginc"

        float _AccScale;
        UNITY_DECLARE_SCREENSPACE_TEXTURE(_SurfelBg);
        UNITY_DECLARE_SCREENSPACE_TEXTURE(_SurfelAcc);

        v2f vertPush(appdata v) { return VertCommon(v, _DepthOffset); }
        v2f vertFlat(appdata v) { return VertCommon(v, 0); }
        ENDCG

        Pass {
            ZWrite On
            ZTest LEqual
            Stencil { Ref 1 Comp Always Pass Replace }

            CGPROGRAM
            #pragma vertex vertPush
            #pragma fragment fragDepth
            #pragma target 4.0
            #pragma shader_feature_local _IGNOREGLOBALPLAYBACKCONTROL_ON
            #pragma shader_feature_local _LOCKPOSITION_ON
            #pragma shader_feature_local _LOCKROTATION_ON
            #pragma shader_feature_local _DITHER_BAYER _DITHER_BLUENOISE _DITHER_FRACTAL
            #pragma shader_feature_local _FALLOFFTEX_ON
            ENDCG
        }

        GrabPass { "_SurfelBg" }

        Pass {
            ZWrite Off
            ZTest LEqual
            Blend One Zero
            Stencil { Ref 1 Comp Equal }

            CGPROGRAM
            #pragma vertex vertFlat
            #pragma fragment fragClear
            #pragma target 4.0
            #pragma shader_feature_local _IGNOREGLOBALPLAYBACKCONTROL_ON
            #pragma shader_feature_local _LOCKPOSITION_ON
            #pragma shader_feature_local _LOCKROTATION_ON
            #pragma shader_feature_local _DITHER_BAYER _DITHER_BLUENOISE _DITHER_FRACTAL
            #pragma shader_feature_local _FALLOFFTEX_ON

            float4 fragClear(v2f i) : SV_Target {
                half cov = Coverage(i.corner, i.uv1, (half)_Falloff);
                clip(cov - 1e-4);
                clip(cov * i.hull - 1e-4);
                return float4(0, 0, 0, 0);
            }

            ENDCG
        }

        Pass {
            ZWrite Off
            ZTest LEqual
            Blend One One
            Stencil { Ref 1 Comp Equal }

            CGPROGRAM
            #pragma vertex vertFlat
            #pragma fragment fragAcc
            #pragma target 4.0
            #pragma shader_feature_local _IGNOREGLOBALPLAYBACKCONTROL_ON
            #pragma shader_feature_local _LOCKPOSITION_ON
            #pragma shader_feature_local _LOCKROTATION_ON
            #pragma shader_feature_local _DITHER_BAYER _DITHER_BLUENOISE _DITHER_FRACTAL
            #pragma shader_feature_local _FALLOFFTEX_ON

            float4 fragAcc(v2f i) : SV_Target {
                half cov = Coverage(i.corner, i.uv1, (half)_Falloff);
                clip(cov - 1e-4);
                cov *= i.hull;
                half w = cov * i.weight;
                return float4(i.col * w, w) * _AccScale;
            }

            ENDCG
        }

        GrabPass { "_SurfelAcc" }

        Pass {
            ZWrite Off
            ZTest LEqual
            Blend One Zero
            Stencil { Ref 1 Comp Equal }

            CGPROGRAM
            #pragma vertex vertFlat
            #pragma fragment fragResolve
            #pragma target 4.0
            #pragma shader_feature_local _IGNOREGLOBALPLAYBACKCONTROL_ON
            #pragma shader_feature_local _LOCKPOSITION_ON
            #pragma shader_feature_local _LOCKROTATION_ON
            #pragma shader_feature_local _DITHER_BAYER _DITHER_BLUENOISE _DITHER_FRACTAL
            #pragma shader_feature_local _FALLOFFTEX_ON

            float4 fragResolve(v2f i) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half cov = Coverage(i.corner, i.uv1, (half)_Falloff);
                clip(cov - 1e-4);
                clip(cov * i.hull - 1e-4);
                float2 guv = i.grabPos.xy / i.grabPos.w;
                float4 acc = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SurfelAcc, guv);
                float4 bg = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SurfelBg, guv);
                float wsum = acc.a / _AccScale;
                float3 col = acc.rgb / max(acc.a, 1e-5);
                return float4(lerp(bg.rgb, col, saturate(wsum)), 1);
            }

            ENDCG
        }
    }

    Fallback Off
    CustomEditor "com.happyrobot33.holographicreprojector.Editor.SurfelDecodeGUI"
}
