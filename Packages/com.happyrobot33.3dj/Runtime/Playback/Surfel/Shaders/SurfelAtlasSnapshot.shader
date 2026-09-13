Shader "SurfelAtlas/Snapshot" {
    Properties { }

    SubShader {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }

        Pass {
            Cull Off
            ZWrite Off
            Blend Off

            CGPROGRAM
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCustomRenderTexture.cginc"

            uniform sampler2D _Udon_3DJ_Depth;

            float4 frag(v2f_customrendertexture i) : SV_Target {
                return tex2D(_Udon_3DJ_Depth, i.localTexcoord.xy);
            }

            ENDCG
        }

        Pass {
            Cull Off
            ZWrite Off
            Blend Off

            CGPROGRAM
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCustomRenderTexture.cginc"

            uniform sampler2D _Udon_3DJ_Color;

            float4 frag(v2f_customrendertexture i) : SV_Target {
                return tex2D(_Udon_3DJ_Color, i.localTexcoord.xy);
            }

            ENDCG
        }
    }

    Fallback Off
}
