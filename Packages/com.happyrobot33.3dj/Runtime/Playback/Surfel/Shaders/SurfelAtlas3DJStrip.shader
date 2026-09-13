Shader "SurfelAtlas/ThreeDJStrip" {
    Properties { }

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

            #include "UnityCustomRenderTexture.cginc"
            #include "SurfelAtlasDecode.cginc"

            float4 frag(v2f_customrendertexture i) : SV_Target {
                float3 djPos;
                float djRot, djScale;
                Decode3DJTransform(djPos, djRot, djScale);
                float4 result = float4(djRot, djScale, 0, 1);

                if (i.localTexcoord.x < 0.5) {
                    result = float4(djPos, 1);
                }

                return result;
            }

            ENDCG
        }
    }

    Fallback Off
}
