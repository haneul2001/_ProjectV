Shader "Custom/JettSmokeURP_Opaque"
{
    // 발로란트 제트 연막 스타일 - 불투명 버전
    // 알파는 항상 1(255)로 고정. 노이즈는 "색상/밝기"에만 적용해서
    // 특정 각도에서 뚫려 보이는(비어보이는) 문제를 없앰.

    Properties
    {
        [Header(Color)]
        _Color ("Smoke Base Color", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0.5, 3)) = 1.4
        _DarkColor ("Smoke Dark Color", Color) = (0.75,0.78,0.8,1)

        [Header(Noise)]
        _NoiseScale ("Noise Scale", Range(1, 20)) = 4.0
        _NoiseOctaves ("Noise Detail (Octaves)", Range(1, 6)) = 4
        _NoiseContrast ("Noise Contrast", Range(0.5, 4)) = 1.5
        _NoiseOffset ("Noise Seed Offset (layer 구분용)", Vector) = (0, 0, 0, 0)

        [Header(Swirl)]
        _SwirlAmount ("Swirl Amount", Range(0, 20)) = 6.0
        _SwirlSpeed ("Swirl Speed", Range(-5, 5)) = 0.8
        _PanSpeedA ("Pan Speed A (x,y)", Vector) = (0.05, 0.08, 0, 0)
        _PanSpeedB ("Pan Speed B (x,y)", Vector) = (-0.06, 0.05, 0, 0)

        [Header(Rim)]
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        // 알파 블렌딩 없음 -> 어떤 각도에서도 뚫려 보이지 않음
        // Cull Off: 앞면/뒷면 둘 다 렌더링 -> 연막 안에 들어가도 뒷면(안쪽 면)이 그대로 그려져서
        //           밖이 뚫려 보이지 않고 시야가 막힘
        ZWrite On
        Cull Off

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Brightness;
                float4 _DarkColor;
                float _NoiseScale;
                float _NoiseOctaves;
                float _NoiseContrast;
                float4 _NoiseOffset;
                float _SwirlAmount;
                float _SwirlSpeed;
                float4 _PanSpeedA;
                float4 _PanSpeedB;
                float _RimPower;
                float _RimStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float freq = 1.0;

                [loop]
                for (int i = 0; i < 6; i++)
                {
                    if (i >= octaves) break;
                    value += amplitude * valueNoise(p * freq);
                    freq *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            float2 RotateUV(float2 uv, float2 center, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                uv -= center;
                float2 rotated = float2(
                    uv.x * c - uv.y * s,
                    uv.x * s + uv.y * c
                );
                return rotated + center;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vertexInput.positionCS;
                OUT.uv = IN.uv;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float2 uv = IN.uv;
                float dist = distance(uv, center);

                float swirlAngle = (1.0 - saturate(dist * 2.0)) * _SwirlAmount
                                    + _Time.y * _SwirlSpeed;

                float2 swirlUV = RotateUV(uv, center, swirlAngle);

                float2 uvA = swirlUV * _NoiseScale + _Time.y * _PanSpeedA.xy + _NoiseOffset.xy;
                float2 uvB = swirlUV * _NoiseScale * 1.3 - _Time.y * _PanSpeedB.xy + _NoiseOffset.zw;

                int octaves = (int)round(_NoiseOctaves);
                float noiseA = fbm(uvA, octaves);
                float noiseB = fbm(uvB, octaves);

                // 더해서(Add) 합치면 곱하기보다 어두운 빈틈이 훨씬 덜 생김 -> 촘촘하게 채워짐
                float density = saturate((noiseA + noiseB) * 0.5 * _NoiseContrast);

                // 밝은 흰 연기 ~ 살짝 어두운 회색 사이를 노이즈로 보간 (알파는 건드리지 않음)
                float3 color = lerp(_DarkColor.rgb, _Color.rgb, density) * _Brightness;

                // 가장자리 림 라이트로 입체감만 살짝 추가
                // 뒷면(안쪽 면)일 때는 노멀을 뒤집어줘야 안에서 봤을 때도 림 라이트가 정상적으로 보임
                float3 normalWS = normalize(IN.normalWS) * (isFrontFace ? 1.0 : -1.0);
                float3 viewDirWS = normalize(IN.viewDirWS);
                float rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _RimPower) * _RimStrength;

                color += rim;

                // 알파는 항상 1 (완전 불투명, 255 고정)
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
