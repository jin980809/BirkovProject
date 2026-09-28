// 스나이퍼 조준 오버레이. 화면 전체를 덮는 UI Image 에 붙여서,
// 커서 자리에 원형 구멍을 뚫고 그 바깥을 반투명하게 덮는다. 구멍 안에는 십자선을 그린다.
//
// 좌표 기준: 이 머티리얼은 화면 전체로 늘린 Image 에 쓰는 것을 전제한다 (UV 0~1 = 화면 전체).
// 반지름·두께는 "화면 높이" 기준 비율이라 해상도가 바뀌어도 같은 크기로 보인다 (_Aspect 로 가로 보정).
//
// 모든 값은 SniperScopeOverlay.cs 가 런타임에 넣는다. 페이드는 UI 정점 색(알파)으로 처리한다.
Shader "Birkov/UI/ScopeMask"
{
    Properties
    {
        _Center ("Center (screen UV)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Hole Radius (screen height)", Float) = 0.18
        _Softness ("Edge Softness", Float) = 0.03
        _Aspect ("Screen Aspect (w/h)", Float) = 1.7778
        _DimColor ("Outside Color", Color) = (0, 0, 0, 0.85)
        _CrossColor ("Crosshair Color", Color) = (1, 1, 1, 0.8)
        _CrossThickness ("Crosshair Thickness (screen height)", Float) = 0.0015
        _CrossLength ("Crosshair Length (x radius)", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _Center;
            float _Radius;
            float _Softness;
            float _Aspect;
            float4 _DimColor;
            float4 _CrossColor;
            float _CrossThickness;
            float _CrossLength;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 화면 높이 기준 좌표로 바꾼다 (가로는 화면비로 보정)
                float2 offset = float2((input.uv.x - _Center.x) * _Aspect, input.uv.y - _Center.y);
                float distance = length(offset);

                // 구멍 안은 완전 투명, 바깥으로 갈수록 _DimColor 알파까지 올라간다
                float dim = smoothstep(_Radius, _Radius + max(_Softness, 0.0001), distance) * _DimColor.a;

                // 십자선: 구멍 반지름의 _CrossLength 배까지 그린다
                float armLength = _Radius * _CrossLength;
                float thickness = max(_CrossThickness, 0.0001);
                float horizontal = step(abs(offset.y), thickness) * step(abs(offset.x), armLength);
                float vertical = step(abs(offset.x), thickness) * step(abs(offset.y), armLength);
                float cross = saturate(horizontal + vertical) * _CrossColor.a;

                // 십자선이 있는 픽셀은 십자선 색으로, 나머지는 어둡게. 알파는 둘 중 큰 값.
                half3 rgb = lerp(_DimColor.rgb, _CrossColor.rgb, cross);
                half alpha = max(dim, cross);

                // UI 정점 색으로 전체 페이드 (SniperScopeOverlay 가 Image.color.a 로 조절)
                return half4(rgb, alpha * input.color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
