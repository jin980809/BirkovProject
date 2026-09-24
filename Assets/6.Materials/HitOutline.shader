// 피격 때 캐릭터 둘레에 튀는 하얗게 빛나는 아웃라인 (URP 용).
// 모델을 한 번 더 그리되 뒷면만 그리고, 정점을 화면상에서 바깥쪽으로 밀어서 몸통 뒤로 삐져나온 부분만 보이게 한다.
// 두께는 화면 픽셀 기준이라 카메라 거리와 상관없이 일정하다. 색은 HDR 로 1 보다 크게 잡으면 Bloom 이 번져서 빛나 보인다.
// PlayerHitEffect 가 Graphics.RenderMesh 로 이 재질을 그린다 - 씬 오브젝트에 직접 붙이는 재질이 아니다.
Shader "Birkov/HitOutline"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (4, 4, 4, 1)
        _Width ("Width (pixels)", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+10" }

        Pass
        {
            Name "HitOutline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(positionWS);

                // 법선을 화면 방향으로 옮겨서 그 방향으로 픽셀 단위만큼 민다 (w 를 곱해 원근 나눗셈을 되돌린다)
                float2 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                float2 direction = normalCS * rsqrt(max(dot(normalCS, normalCS), 1e-6));
                positionCS.xy += direction * (_Width * 2.0 / _ScreenParams.xy) * positionCS.w;

                output.positionCS = positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
