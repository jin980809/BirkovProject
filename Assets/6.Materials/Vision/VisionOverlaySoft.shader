Shader "Birkov/VisionOverlaySoft"
{
    // 시야 경계 그라데이션.
    // FOV 메시가 덮은 곳(스텐실 == 1)에만 그려지고, 플레이어 기준 거리/각도로
    // "얼마나 밝은지"를 수식(smoothstep)으로 계산해 경계에서 부드럽게 어두워진다.
    // 전역 값은 PlayerVision.UpdateVisionShaderGlobals() 에서 매 프레임 설정.
    // (VisionOverlay = 하드 어둠 / 이 셰이더 = 경계 페이드. 오버레이 쿼드에 두 머티리얼을 같이 넣는다)
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
            "Queue" = "Geometry+3"
        }

        Pass
        {
            Name "VisionOverlaySoft"

            Stencil
            {
                Ref 1
                Comp Equal
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

            // 전역 (PlayerVision 이 설정)
            float4 _VisionPlayerPos;   // xyz 월드 위치
            float4 _VisionPlayerDir;   // xz 정면 방향 (정규화됨)
            float4 _VisionParams;      // x=viewRadius, y=nearRadius, z=edgeFade
            float4 _VisionConeAngles;  // x=cos(halfAngle), y=cos(halfAngle - angleFade)

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 toFrag = IN.positionWS.xz - _VisionPlayerPos.xz;
                float dist = length(toFrag);

                // 근접 원: 안쪽 1 → 경계에서 0
                float nearVis = smoothstep(_VisionParams.y, _VisionParams.y - _VisionParams.z, dist);

                // 정면 부채꼴: 각도 + 거리
                float2 dir = dist > 1e-4 ? toFrag / dist : _VisionPlayerDir.xz;
                float cosAng = dot(dir, _VisionPlayerDir.xz);
                float angular = smoothstep(_VisionConeAngles.x, _VisionConeAngles.y, cosAng);
                float radial = smoothstep(_VisionParams.x, _VisionParams.x - _VisionParams.z, dist);
                float coneVis = angular * radial;

                float visibility = max(nearVis, coneVis);

                half4 col = _Color;
                col.a *= saturate(1.0 - visibility);
                return col;
            }
            ENDHLSL
        }
    }
}
