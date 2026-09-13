Shader "SurfelAtlas/MetaSplat" {
    Properties {
        [Header(Splat)]
        [Tooltip(Tilt discs to the surface.)]
        [Toggle(_SHEAR_ON)] _ShearOn ("Shear to normal", Float) = 1
        [Tooltip(Fade in where faces overlap)]
        _SeamBlend ("Seam blend", Float) = 1.4
        [Tooltip(Drop surfels seen edge on)]
        _GrazingCutoff ("Grazing cutoff", Range(0, 1)) = 0.15
        [Tooltip(Cap on tilted disc elongation)]
        _MaxStretch ("Max stretch", Range(1, 5)) = 5

        [Header(Hull)]
        [Tooltip(Reject surfels outside the silhouette.)]
        [Toggle(_HULL_ON)] _HullOn ("Hull", Float) = 1
        [Tooltip(How much hull counts as inside)]
        _HullThreshold ("Hull threshold", Float) = 0.5
        [Tooltip(Soften the hull edge)]
        _HullFeather ("Hull feather", Float) = 0

        [HideInInspector] _Meta ("", 2D) = "black" {}
        [HideInInspector] _Strip3DJ ("", 2D) = "black" {}
        [HideInInspector] _SurfelDepth ("", 2D) = "black" {}
    }

    SubShader {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        Pass {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend Off

            CGPROGRAM
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 4.0
            #pragma shader_feature_local _SHEAR_ON
            #pragma shader_feature_local _HULL_ON

            #include "UnityCustomRenderTexture.cginc"
            #include "SurfelAtlasDecode.cginc"

            float _SeamBlend;
            float _GrazingCutoff;
            float _MaxStretch;
            float _HullThreshold;
            float _HullFeather;

            float4 Splat3DJ(int k, int tileU, int tileV, int tileW, int tileH, float raw0, float2 uv) {
                float s3dj = Scale3DJ();
                float3 right, up, fwd, camPos;
                float w, h, d0;
                CubeFace(k, s3dj, right, up, fwd, camPos, w, h, d0);

                float3 P0 = Pos3DJ(k, s3dj, (tileU + 0.5) / tileW, (tileV + 0.5) / tileH, Lin3DJ(raw0));

                half4 meta = (half4)tex2Dlod(_Meta, float4(uv, 0, 0));
                half3 n = OctDecode(meta.rg);
                half3 fwdH = (half3)fwd;
                half inc = dot(n, -fwdH);

                half weight = (half)saturate(inc * _SeamBlend);

                if (inc < (half)_GrazingCutoff) {
                    weight = 0;
                }

                half k1 = 0;
                half k2 = 0;
                #ifdef _SHEAR_ON
                half nf = (half)min(dot(n, fwdH), -0.05);
                half kMax = (half)sqrt(_MaxStretch * _MaxStretch - 1.0);
                k1 = clamp(dot(n, (half3)right) / nf, -kMax, kMax);
                k2 = clamp(dot(n, (half3)up) / nf, -kMax, kMax);
                #endif

                half hull = 1;
                #ifdef _HULL_ON
                float hf = HullFactor3DJ(P0, k, s3dj);
                hull = (half)step(_HullThreshold, hf);

                if (_HullFeather > 0) {
                    hull = (half)smoothstep(_HullThreshold - _HullFeather, _HullThreshold + _HullFeather, hf);
                }
                #endif

                return float4(k1, k2, hull, weight);
            }

            float4 frag(v2f_customrendertexture i) : SV_Target {
                float4 empty = float4(0, 0, 0, 0);

                float2 atlas3dj = Atlas3DJ();
                int texelX = (int)(i.localTexcoord.x * atlas3dj.x);
                int texelY = (int)(i.localTexcoord.y * atlas3dj.y);
                float raw0 = Depth3DJAt(texelX, texelY);

                if (Empty3DJ(raw0)) {
                    return empty;
                }

                int k, tileU, tileV, tileW, tileH;

                if (!TexelToFace3DJ(texelX, texelY, k, tileU, tileV, tileW, tileH)) {
                    return empty;
                }

                return Splat3DJ(k, tileU, tileV, tileW, tileH, raw0, i.localTexcoord.xy);
            }

            ENDCG
        }
    }

    Fallback Off
    CustomEditor "com.happyrobot33.holographicreprojector.Editor.SurfelMaterialGUI"
}
