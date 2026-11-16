// RE:CRAFT 공용 표면 셰이더.
// 정점 색 = 기본 색(a = 도색 가능), uv1 = (메탈릭, 스무스니스).
// _MaskTex: R = 녹, G = 먼지, B = 새 도색. 마스크를 지우거나 칠해서 복원 상태를 표현한다.
// 녹·균열은 화면 공간 미분으로 법선을 흔들어 실제 요철처럼 보이게 한다.
// _Pattern: 0 없음, 1 마루, 2 회반죽, 3 타공판, 4 커팅 매트 (공방 환경용, 오브젝트 좌표 기준)
Shader "ReCraft/Surface"
{
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _MaskTex ("Mask (R rust, G dust, B paint)", 2D) = "black" {}
        _PaintColor ("Paint Color", Color) = (1,1,1,1)
        _UsePaintColor ("Use Paint Color", Float) = 0
        _PaintMetal ("Paint Metallic Boost", Float) = 0
        _Worn ("Worn (unpainted fade)", Float) = 0
        _Broken ("Broken", Float) = 0
        _Wood ("Wood Grain", Float) = 0
        _Pattern ("Pattern", Float) = 0
        _NoiseScale ("Noise Scale", Float) = 70
        _Bump ("Bump Strength", Float) = 1
        [HDR] _Emission ("Emission", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            float4 _MaskTex_ST;
            half4 _PaintColor;
            half _UsePaintColor;
            half _PaintMetal;
            half _Worn;
            half _Broken;
            half _Wood;
            half _Pattern;
            half _NoiseScale;
            half _Bump;
            half4 _Emission;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 pbr : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                half4 color : COLOR;
            };

            float hash3(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float hash1(float n) { return frac(sin(n * 127.1) * 43758.5453); }

            float vnoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash3(i + float3(0,0,0)), hash3(i + float3(1,0,0)), f.x),
                                 lerp(hash3(i + float3(0,1,0)), hash3(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash3(i + float3(0,0,1)), hash3(i + float3(1,0,1)), f.x),
                                 lerp(hash3(i + float3(0,1,1)), hash3(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            float fbm(float3 p)
            {
                return vnoise(p) * 0.55 + vnoise(p * 2.13 + 7.1) * 0.3 + vnoise(p * 4.7 + 3.3) * 0.15;
            }

            // 높이 h(월드 미터)로 법선을 흔든다 (Mikkelsen surface gradient).
            float3 BumpNormal(float3 N, float3 posWS, float h)
            {
                float3 dpdx = ddx(posWS), dpdy = ddy(posWS);
                float hx = ddx(h), hy = ddy(h);
                float3 r1 = cross(dpdy, N), r2 = cross(N, dpdx);
                float det = dot(dpdx, r1);
                float3 grad = sign(det) * (hx * r1 + hy * r2);
                return normalize(abs(det) * N - grad);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                o.pbr = v.uv1;
                o.positionOS = v.positionOS.xyz;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                float3 po = i.positionOS;
                float3 np = po * _NoiseScale;
                half n1 = vnoise(np);
                half n2 = vnoise(np * 3.7 + 11.0);
                half micro = vnoise(po * 900.0);

                half3 baseCol = i.color.rgb * _Tint.rgb;
                half metallic = i.pbr.x;
                half smooth = i.pbr.y;
                half paintable = step(0.5, i.color.a);
                float height = 0.0;

                // ── 환경 패턴 ──
                if (_Pattern > 0.5 && _Pattern < 1.5)          // 마루: X 방향 판재 + 이음 홈 + 판마다 색 차이
                {
                    float w = 0.16;
                    float idx = floor(po.z / w);
                    float fz = frac(po.z / w);
                    float jointLen = 1.1 + hash1(idx) * 0.9;
                    float fx = frac((po.x + hash1(idx + 5.0) * 3.0) / jointLen);
                    half groove = min(smoothstep(0.0, 0.02, fz) * smoothstep(1.0, 0.98, fz), smoothstep(0.0, 0.006, fx) * smoothstep(1.0, 0.994, fx));
                    baseCol *= (0.82 + 0.3 * hash1(idx * 3.1)) * lerp(0.35, 1.0, groove);
                    height += (1.0 - groove) * -0.002;
                    smooth *= lerp(0.4, 1.0, groove);
                }
                else if (_Pattern > 1.5 && _Pattern < 2.5)     // 회반죽: 큰 얼룩 + 잔요철
                {
                    half big = fbm(po * 1.6);
                    baseCol *= 0.88 + 0.2 * big;
                    height += (vnoise(po * 60.0) - 0.5) * 0.0012;
                }
                else if (_Pattern > 2.5 && _Pattern < 3.5)     // 타공판: 2.5cm 간격 구멍
                {
                    float2 g = frac(po.xy / 0.025) - 0.5;
                    half hole = smoothstep(0.17, 0.13, length(g));
                    baseCol *= lerp(1.0, 0.12, hole);
                    height += hole * -0.002;
                    smooth *= 1.0 - hole;
                }
                else if (_Pattern > 3.5)                       // 커팅 매트: 1cm 가는 선 + 5cm 굵은 선
                {
                    float2 g1 = abs(frac(po.xz / 0.01 + 0.5) - 0.5) * 0.01;
                    float2 g5 = abs(frac(po.xz / 0.05 + 0.5) - 0.5) * 0.05;
                    half thin = 1.0 - smoothstep(0.0002, 0.0005, min(g1.x, g1.y));
                    half thick = 1.0 - smoothstep(0.0005, 0.0009, min(g5.x, g5.y));
                    baseCol = lerp(baseCol, half3(0.75, 0.85, 0.78), saturate(thin * 0.35 + thick * 0.75));
                }

                // 나뭇결
                half grain = 0.88 + 0.12 * sin(po.x * 140.0 + fbm(po * float3(2, 30, 30)) * 9.0);
                baseCol *= lerp(1.0, grain, _Wood);
                height += _Wood * (grain - 0.9) * 0.0008;

                // 금속: 결 방향 헤어라인 + 미세 거칠기
                half brushed = vnoise(po * float3(30.0, 2200.0, 2200.0));
                smooth = lerp(smooth, smooth * (0.86 + 0.18 * brushed), saturate(metallic * 1.5));
                smooth *= 0.95 + 0.05 * micro;

                // 낡은 도장: 색이 바래고 광택이 죽는다
                half lum = dot(baseCol, half3(0.3, 0.59, 0.11));
                half worn = paintable * _Worn;
                half3 wornCol = lerp(baseCol, lum.xxx, 0.5) * (0.74 + 0.2 * n2);
                half3 albedo = lerp(baseCol, wornCol, worn);
                smooth = lerp(smooth, 0.18, worn);

                // 새 도색 (클리어코트처럼 광택)
                half paint = paintable * smoothstep(0.25, 0.75, mask.b);
                half3 freshCol = lerp(baseCol, _PaintColor.rgb, _UsePaintColor);
                albedo = lerp(albedo, freshCol, paint);
                smooth = lerp(smooth, max(i.pbr.y, 0.78), paint);
                metallic = lerp(metallic, saturate(metallic + _PaintMetal), paint);

                // 녹: 얼룩진 색 + 부풀어 오른 요철
                half rustN = fbm(np * 0.9 + 4.0);
                half rust = smoothstep(0.1, 0.6, mask.r * (0.7 + 0.6 * n1));
                half3 rustCol = lerp(half3(0.2, 0.075, 0.03), half3(0.62, 0.29, 0.09), rustN);
                rustCol = lerp(rustCol, half3(0.42, 0.2, 0.08), smoothstep(0.6, 0.9, micro) * 0.6);
                albedo = lerp(albedo, rustCol, rust);
                metallic = lerp(metallic, 0.0, rust);
                smooth = lerp(smooth, 0.06 + 0.08 * micro, rust);
                height += rust * (rustN * 0.0011 + micro * 0.0004);

                // 먼지: 위를 향한 면에 더 두껍게
                half up = saturate(normalize(i.normalWS).y * 0.5 + 0.5);
                half dust = mask.g * (0.5 + 0.5 * n1) * lerp(0.4, 0.75, up);
                albedo = lerp(albedo, half3(0.5, 0.46, 0.4), dust);
                smooth *= (1.0 - dust * 0.9);
                metallic *= (1.0 - dust);

                // 파손: 그을음 + 균열
                half crack = smoothstep(0.47, 0.5, n1) * smoothstep(0.53, 0.5, n1);
                half burn = _Broken * (0.55 + 0.45 * n2);
                albedo = lerp(albedo, half3(0.05, 0.045, 0.04), burn * 0.75);
                albedo = lerp(albedo, half3(0.0, 0.0, 0.0), _Broken * crack);
                smooth = lerp(smooth, 0.1, _Broken * 0.8);
                height += _Broken * crack * -0.0008;

                float3 N = normalize(i.normalWS);
                N = BumpNormal(N, i.positionWS, height * _Bump * length(UNITY_MATRIX_M._m00_m10_m20));

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.fogCoord = 0;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.metallic = metallic;
                s.specular = half3(0, 0, 0);
                s.smoothness = saturate(smooth);
                s.occlusion = 1.0;
                s.emission = _Emission.rgb;
                s.alpha = 1.0;
                s.normalTS = half3(0, 0, 1);

                return UniversalFragmentPBR(inputData, s);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 pw = TransformObjectToWorld(v.positionOS.xyz);
                float3 nw = TransformObjectToWorldNormal(v.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 ld = normalize(_LightPosition - pw);
            #else
                float3 ld = _LightDirection;
            #endif
                float4 pc = TransformWorldToHClip(ApplyShadowBias(pw, nw, ld));
            #if UNITY_REVERSED_Z
                pc.z = min(pc.z, UNITY_NEAR_CLIP_VALUE);
            #else
                pc.z = max(pc.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                o.positionCS = pc;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return half4(normalize(i.normalWS), 0.0); }
            ENDHLSL
        }
    }
}
