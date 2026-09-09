Shader "Birkov/VisionOverlay"
{
    // 시야 콘 밖(스텐실 != 1)의 "바닥"만 어둡게 덮는다.
    // ZTest LEqual 이라 벽/장애물처럼 오버레이 쿼드(바닥 높이)보다 앞에 있는 물체는 안 덮인다.
    Properties
    {
        _Color ("Overlay Color", Color) = (0, 0, 0, 0.8)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+2"
        }

        Pass
        {
            Name "VisionOverlay"

            // 스텐실이 1이 아닌 곳(= 시야 콘 밖)만 통과
            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
