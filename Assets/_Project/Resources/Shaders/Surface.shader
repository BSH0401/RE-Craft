// RE:CRAFT 공용 표면 셰이더.
// 정점 색 = 기본 색(a = 도색 가능), uv1 = (메탈릭, 스무스니스).
// _MaskTex: R = 녹, G = 먼지, B = 새 도색. 마스크를 지우거나 칠해서 복원 상태를 표현한다.
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
        _NoiseScale ("Noise Scale", Float) = 70
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
            half _NoiseScale;
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
                float3 np = i.positionOS * _NoiseScale;
                half n1 = vnoise(np);
                half n2 = vnoise(np * 3.7 + 11.0);

                half3 baseCol = i.color.rgb * _Tint.rgb;
                half metallic = i.pbr.x;
                half smooth = i.pbr.y;
                half paintable = step(0.5, i.color.a);

                // 나뭇결
                half grain = 0.9 + 0.1 * sin(i.positionOS.z * 160.0 + vnoise(i.positionOS * float3(3, 20, 25)) * 5.0);
                baseCol *= lerp(1.0, grain, _Wood);

                // 낡은 도장: 색이 바래고 광택이 죽는다
                half lum = dot(baseCol, half3(0.3, 0.59, 0.11));
                half worn = paintable * _Worn;
                half3 wornCol = lerp(baseCol, lum.xxx, 0.55) * (0.72 + 0.2 * n2);
                half3 albedo = lerp(baseCol, wornCol, worn);
                smooth = lerp(smooth, 0.12, worn);

                // 새 도색
                half paint = paintable * smoothstep(0.25, 0.75, mask.b);
                half3 freshCol = lerp(baseCol, _PaintColor.rgb, _UsePaintColor);
                albedo = lerp(albedo, freshCol, paint);
                smooth = lerp(smooth, max(i.pbr.y, 0.7), paint);
                metallic = lerp(metallic, saturate(metallic + _PaintMetal), paint);

                // 녹
                half rust = smoothstep(0.1, 0.6, mask.r * (0.75 + 0.5 * n1));
                half3 rustCol = lerp(half3(0.22, 0.09, 0.04), half3(0.55, 0.25, 0.08), n2);
                albedo = lerp(albedo, rustCol, rust);
                metallic = lerp(metallic, 0.0, rust);
                smooth = lerp(smooth, 0.08, rust);

                // 먼지
                half dust = mask.g * (0.55 + 0.45 * n1) * 0.6;
                albedo = lerp(albedo, half3(0.42, 0.38, 0.32), dust);
                smooth *= (1.0 - dust);
                metallic *= (1.0 - dust);

                // 파손: 그을음 + 균열
                half crack = smoothstep(0.47, 0.5, n1) * smoothstep(0.53, 0.5, n1);
                half burn = _Broken * (0.55 + 0.45 * n2);
                albedo = lerp(albedo, half3(0.05, 0.045, 0.04), burn * 0.75);
                albedo = lerp(albedo, half3(0.0, 0.0, 0.0), _Broken * crack);
                smooth = lerp(smooth, 0.1, _Broken * 0.8);

                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.positionCS = i.positionCS;
                inputData.normalWS = normalize(i.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                inputData.fogCoord = 0;
                inputData.vertexLighting = half3(0, 0, 0);
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.metallic = metallic;
                s.specular = half3(0, 0, 0);
                s.smoothness = smooth;
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
