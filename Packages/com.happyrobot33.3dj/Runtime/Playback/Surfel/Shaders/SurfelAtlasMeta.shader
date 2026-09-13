Shader "SurfelAtlas/Meta" {
    Properties {
        [Header(Edge Rejection)]
        [Tooltip(Reject depth edges entirely.)]
        [Toggle(_SOBEL_ON)] _Sobel ("Sobel", Float) = 1
        [Tooltip(How hard an edge counts)]
        _SobelThreshold ("Sobel Threshold", Float) = 0.1

        [Header(Fit)]
        [Tooltip(Drop texels other faces contradict.)]
        [Toggle(_REPROJECT_ON)] _ReprojectOn ("Reprojection reject", Float) = 1
        [Tooltip(Plane fit neighbourhood size)]
        [KeywordEnum(R1, R2, R3, R4)] _FitWindow ("Fit window radius", Float) = 1
        [Tooltip(Contradiction slack, in metres)]
        _ReprojectTolerance ("Reproject tolerance (m)", Float) = 0.02
        [Tooltip(How many faces must disagree)]
        _ReprojectViews ("Contradicting faces to reject", Int) = 1

        [Header(Radius)]
        [Tooltip(Scale surfels by surface detail.)]
        [Toggle(_ADAPTIVE_RADIUS_ON)] _AdaptiveRadius ("Adaptive radius", Float) = 0
        [Tooltip(Multiplier where detail is high)]
        _RadiusMin ("Radius min", Float) = 1
        [Tooltip(Multiplier where surface is flat)]
        _RadiusMax ("Radius max", Float) = 1
        [Tooltip(Residual that means detailed, metres)]
        _DetailScale ("Detail scale (m)", Float) = 0.02

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
            #pragma shader_feature_local _ADAPTIVE_RADIUS_ON
            #pragma shader_feature_local _SOBEL_ON
            #pragma shader_feature_local _REPROJECT_ON
            #pragma shader_feature_local _FITWINDOW_R1 _FITWINDOW_R2 _FITWINDOW_R3 _FITWINDOW_R4

            #include "UnityCustomRenderTexture.cginc"
            #include "SurfelAtlasDecode.cginc"

            float _SobelThreshold;
            float _ReprojectTolerance;
            int _ReprojectViews;
            float _RadiusMin;
            float _RadiusMax;
            float _DetailScale;

            float4 SolveNormal(float N, float Su, float Sv, float Suu, float Svv, float Suv, float Sz, float Suz, float Svz,
                               float3 right, float3 up, float3 axis, float su, float sv, float sgn, out float gu, out float gv) {
                float det = N * (Suu * Svv - Suv * Suv) - Su * (Su * Svv - Suv * Sv) + Sv * (Su * Suv - Suu * Sv);
                gu = 0; gv = 0;

                if (N >= 3 && abs(det) >= 1e-12) {
                    float detU = N * (Suz * Svv - Suv * Svz) - Sz * (Su * Svv - Suv * Sv) + Sv * (Su * Svz - Suz * Sv);
                    float detV = N * (Suu * Svz - Suz * Suv) - Su * (Su * Svz - Suz * Sv) + Sz * (Su * Suv - Suu * Sv);
                    gu = detU / det;
                    gv = detV / det;
                }

                float3 tu = right * su + axis * gu;
                float3 tv = up * sv + axis * gv;

                float3 c = cross(tu, tv);
                float cl = length(c);
                float3 nrm = axis * sgn;

                if (cl > 1e-12) {
                    nrm = c / cl;
                }

                if (dot(nrm, axis) * sgn < 0) {
                    nrm = -nrm;
                }

                return float4(OctEncode((half3)nrm), length(tu), length(tv));
            }

            float Sobel3DJ(int texelX, int texelY, int tileU, int tileV, int tileW, int tileH) {
                float d[9];

                [unroll] for (int j = 0; j < 3; j++) {
                    [unroll] for (int i = 0; i < 3; i++) {
                        d[j * 3 + i] = Depth3DJAt(texelX + clamp(tileU + i - 1, 0, tileW - 1) - tileU,
                                                  texelY + clamp(tileV + j - 1, 0, tileH - 1) - tileV);
                    }
                }

                float gx = (d[0] + 2 * d[3] + d[6]) - (d[2] + 2 * d[5] + d[8]);
                float gy = (d[6] + 2 * d[7] + d[8]) - (d[0] + 2 * d[1] + d[2]);
                return length(float2(gx, gy));
            }

            float4 Fit3DJ(int texelX, int texelY, int k, int tileU, int tileV, int tileW, int tileH, float raw0) {
                float s3dj = Scale3DJ();

                #ifdef _SOBEL_ON
                if (Sobel3DJ(texelX, texelY, tileU, tileV, tileW, tileH) > _SobelThreshold) {
                    return float4(0.5, 0.5, 0, 0);
                }
                #endif

                float3 right, up, fwd, camPos;
                float w, h, d0;
                CubeFace(k, s3dj, right, up, fwd, camPos, w, h, d0);

                float su = w / tileW;
                float sv = h / tileH;
                float z0 = Lin3DJ(raw0) * d0;

                float3 P0 = Pos3DJ(k, s3dj, (tileU + 0.5) / tileW, (tileV + 0.5) / tileH, Lin3DJ(raw0));

                #ifdef _REPROJECT_ON
                if (Contradictions3DJ(P0, k, s3dj, true, _ReprojectTolerance) >= _ReprojectViews) {
                    return float4(0.5, 0.5, 0, 0);
                }
                #endif

                float N = 0, Su = 0, Sv = 0, Suu = 0, Svv = 0, Suv = 0, Sz = 0, Suz = 0, Svz = 0;
                #ifdef _ADAPTIVE_RADIUS_ON
                float Szz = 0;
                #endif

                #if defined(_FITWINDOW_R1)
                static const int W = 1;
                #elif defined(_FITWINDOW_R2)
                static const int W = 2;
                #elif defined(_FITWINDOW_R3)
                static const int W = 3;
                #elif defined(_FITWINDOW_R4)
                static const int W = 4;
                #endif

                [unroll] for (int dv = -W; dv <= W; dv++) {
                    [unroll] for (int du = -W; du <= W; du++) {
                        int un = tileU + du;
                        int vn = tileV + dv;
                        float inside = 0.0;

                        if (un >= 0 && un < tileW && vn >= 0 && vn < tileH) {
                            inside = 1.0;
                        }

                        float rn = Depth3DJAt(texelX + clamp(un, 0, tileW - 1) - tileU, texelY + clamp(vn, 0, tileH - 1) - tileV);
                        float emptyScale = 1.0;

                        if (Empty3DJ(rn)) {
                            emptyScale = 0.0;
                        }

                        float m = inside * emptyScale;
                        float dz = (Lin3DJ(rn) * d0 - z0) * m;

                        float fu = du * m;
                        float fv = dv * m;
                        N += m;
                        Su += fu; Sv += fv;
                        Suu += fu * du; Svv += fv * dv; Suv += fu * dv;
                        Sz += dz; Suz += du * dz; Svz += dv * dz;
                        #ifdef _ADAPTIVE_RADIUS_ON
                        Szz += dz * dz;
                        #endif
                    }
                }

                float gu, gv;
                float4 fit = SolveNormal(N, Su, Sv, Suu, Svv, Suv, Sz, Suz, Svz, right, up, fwd, su, sv, -1, gu, gv);

                #ifdef _ADAPTIVE_RADIUS_ON
                float a = (Sz - gu * Su - gv * Sv) / max(N, 1);
                float ssr = max(Szz - a * Sz - gu * Suz - gv * Svz, 0);
                float rms = sqrt(ssr / max(N, 1));
                float detail = saturate(rms / _DetailScale);
                float scale = lerp(_RadiusMax, _RadiusMin, detail);
                return float4(fit.xy, fit.z * scale, fit.w * scale);
                #else
                return fit;
                #endif
            }

            float4 frag(v2f_customrendertexture i) : SV_Target {
                float4 empty = float4(0.5, 0.5, 0, 0);

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

                return Fit3DJ(texelX, texelY, k, tileU, tileV, tileW, tileH, raw0);
            }

            ENDCG
        }
    }

    Fallback Off
    CustomEditor "com.happyrobot33.holographicreprojector.Editor.SurfelMaterialGUI"
}
