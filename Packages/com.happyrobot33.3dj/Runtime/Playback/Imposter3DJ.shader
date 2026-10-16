Shader "3DJ/Imposter" {
    Properties {
        [Tooltip(Uniform march steps across the cube.)]
        _Steps ("Steps", Int) = 128
        [Tooltip(Bisection steps after the first solid sample.)]
        _Refine ("Refine", Int) = 6
        [Tooltip(Depth slack in meters for the carve and shade tests.)]
        _Tolerance ("Tolerance (m)", Float) = 0.03
        [Tooltip(Solid when fewer faces than this see past the point.)]
        _CarveViews ("Carve views", Int) = 1
        [Tooltip(Higher favors the face most aligned with the view.)]
        _BlendPower ("Blend power", Float) = 4
    }

    SubShader {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }

        Pass {
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "Imposter3DJ.cginc"

            float _Steps;
            float _Refine;
            float _Tolerance;
            float _CarveViews;
            float _BlendPower;

            struct appdata {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 position : SV_POSITION;
                float3 cubeLocal : TEXCOORD0;
                nointerpolation float3 cameraLocal : TEXCOORD1;
                nointerpolation float djRotation : TEXCOORD3;
                nointerpolation float djScale : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata input) {
                v2f output;
                UNITY_INITIALIZE_OUTPUT(v2f, output);
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 djPosition;
                float djRotation;
                float djScale;
                DecodeTransform3DJ(djPosition, djRotation, djScale);

                if (_Udon_3DJ_PlaybackActive != 1.0 || djScale <= 0) {
                    output.position = float4(2, 2, 2, 1);
                    return output;
                }

                float3 cubeLocal = input.vertex.xyz * djScale;
                float3 cameraObject = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz;
                output.position = UnityObjectToClipPos(float4(RotateY3DJ(cubeLocal, djRotation), 1));
                output.cubeLocal = cubeLocal;
                output.cameraLocal = RotateY3DJ(cameraObject, -djRotation);
                output.djRotation = djRotation;
                output.djScale = djScale;
                return output;
            }

            float4 frag(v2f input, out float outDepth : SV_Depth) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 rayOrigin = input.cameraLocal;
                float3 rayDirection = normalize(input.cubeLocal - input.cameraLocal);
                float halfExtent = input.djScale * 0.5;
                float3 safeDirection = rayDirection;

                if (abs(safeDirection.x) < 1e-6) {
                    safeDirection.x = 1e-6;
                }

                if (abs(safeDirection.y) < 1e-6) {
                    safeDirection.y = 1e-6;
                }

                if (abs(safeDirection.z) < 1e-6) {
                    safeDirection.z = 1e-6;
                }

                float3 slabA = (-halfExtent - rayOrigin) / safeDirection;
                float3 slabB = (halfExtent - rayOrigin) / safeDirection;
                float3 slabNear = min(slabA, slabB);
                float3 slabFar = max(slabA, slabB);
                float tNear = max(max(max(slabNear.x, slabNear.y), slabNear.z), 0.0);
                float tFar = min(min(slabFar.x, slabFar.y), slabFar.z);

                if (tFar <= tNear) {
                    clip(-1.0);
                }

                int stepCount = max((int)_Steps, 1);
                int refineCount = max((int)_Refine, 0);
                int carveViews = (int)_CarveViews;
                float blendPower = max(_BlendPower, 1e-3);
                float stepLength = (tFar - tNear) / (float)stepCount;
                float lowT = tNear;
                float highT = tNear;
                bool hit = false;

                [loop] for (int stepIndex = 0; stepIndex <= stepCount; stepIndex++) {
                    float sampleT = tNear + stepLength * (float)stepIndex;

                    if (Solid3DJ(rayOrigin + rayDirection * sampleT, input.djScale, _Tolerance, carveViews)) {
                        highT = sampleT;
                        hit = true;
                        break;
                    }

                    lowT = sampleT;
                }

                if (!hit) {
                    clip(-1.0);
                }

                if (highT > lowT) {
                    [loop] for (int refineIndex = 0; refineIndex < refineCount; refineIndex++) {
                        float middleT = (lowT + highT) * 0.5;

                        if (Solid3DJ(rayOrigin + rayDirection * middleT, input.djScale, _Tolerance, carveViews)) {
                            highT = middleT;
                        } else {
                            lowT = middleT;
                        }
                    }
                }

                float3 hitLocal = rayOrigin + rayDirection * highT;
                float3 hitObject = RotateY3DJ(hitLocal, input.djRotation);
                float4 hitClip = UnityObjectToClipPos(float4(hitObject, 1));
                outDepth = hitClip.z / hitClip.w;
                #if !defined(UNITY_REVERSED_Z)
                outDepth = outDepth * 0.5 + 0.5;
                #endif

                float3 shadedColor = Shade3DJ(hitLocal, rayDirection, input.djScale, _Tolerance, blendPower);
                return float4(shadedColor, 1.0);
            }

            ENDCG
        }
    }
}
