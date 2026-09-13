Shader "SurfelAtlas/Decode" {
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
        [HideInInspector] _StencilRef ("", Float) = 1
        [HideInInspector] _StencilComp ("", Float) = 3
        [HideInInspector] _Strip3DJ ("", 2D) = "black" {}
        [HideInInspector] _SurfelDepth ("", 2D) = "black" {}
        [HideInInspector] _SurfelColor ("", 2D) = "black" {}
    }

    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        Cull Off

        CGINCLUDE
        #include "SurfelAtlasDecode.cginc"
        ENDCG

        Pass {
            ZWrite On
            ZTest LEqual
            Stencil { Ref [_StencilRef] Comp Always Pass Replace }

            CGPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma target 4.0
            #pragma multi_compile_local _MODE_TWOPASS _MODE_ZWRITE _MODE_DITHER _MODE_CUTOUT
            #pragma shader_feature_local _IGNOREGLOBALPLAYBACKCONTROL_ON
            #pragma shader_feature_local _LOCKPOSITION_ON
            #pragma shader_feature_local _LOCKROTATION_ON
            #pragma shader_feature_local _DITHER_BAYER _DITHER_BLUENOISE _DITHER_FRACTAL
            #pragma shader_feature_local _FALLOFFTEX_ON

            v2f vertDepth(appdata v) {
                #if !defined(_MODE_TWOPASS)
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                ApplyCollapse(o);
                return o;
                #else
                return VertCommon(v, _DepthOffset);
                #endif
            }

            ENDCG
        }

        Pass {
            ZWrite [_ZWrite]
            ZTest LEqual
            Blend [_SrcBlend] [_DstBlend]
            Stencil { Ref [_StencilRef] Comp [_StencilComp] }

            CGPROGRAM
            #pragma vertex vertColor
            #pragma fragment fragColor
            #pragma target 4.0
            #pragma multi_compile_local _MODE_TWOPASS _MODE_ZWRITE _MODE_DITHER _MODE_CUTOUT
            #pragma shader_feature_local _IGNOREGLOBALPLAYBACKCONTROL_ON
            #pragma shader_feature_local _LOCKPOSITION_ON
            #pragma shader_feature_local _LOCKROTATION_ON
            #pragma shader_feature_local _DITHER_BAYER _DITHER_BLUENOISE _DITHER_FRACTAL
            #pragma shader_feature_local _FALLOFFTEX_ON

            v2f vertColor(appdata v) { return VertCommon(v, 0); }

            half4 fragColor(v2f i) : SV_Target {
                #if defined(_MODE_CUTOUT)
                half fall = 2.5;
                #else
                half fall = (half)_Falloff;
                #endif
                half cov = Coverage(i.corner, i.uv1, fall);
                clip(cov - 1e-4);
                cov *= i.hull;
                clip(cov - 1e-4);
                half alpha = (half)saturate(cov * i.weight * _FalloffMult);

                #if defined(_MODE_DITHER)
                clip(DitherClip3DJ(i, alpha));
                return half4(i.col, 1);
                #elif defined(_MODE_CUTOUT)
                clip(alpha - _Cutoff);
                return half4(i.col, 1);
                #else
                return half4(i.col, alpha);
                #endif
            }

            ENDCG
        }
    }

    Fallback Off
    CustomEditor "com.happyrobot33.holographicreprojector.Editor.SurfelDecodeGUI"
}
