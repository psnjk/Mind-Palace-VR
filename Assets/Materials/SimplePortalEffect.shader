Shader "Custom/URP/SimplePortalEffect"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.4, 0.2, 0.8, 1.0)
        _SecondaryColor ("Secondary Color", Color) = (0.7, 0.4, 1.0, 1.0)
        _Speed ("Animation Speed", Range(0.1, 3.0)) = 1.0
        _NoiseScale ("Noise Scale", Range(0.5, 5.0)) = 2.0
        _WaveFrequency ("Wave Frequency", Range(1, 20)) = 8.0
        _WaveAmplitude ("Wave Amplitude", Range(0, 1)) = 0.3
        _Brightness ("Brightness", Range(0, 3)) = 1.2
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _SecondaryColor;
                float _Speed;
                float _NoiseScale;
                float _WaveFrequency;
                float _WaveAmplitude;
                float _Brightness;
            CBUFFER_END

            // Hash function for noise
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Layered noise
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 5; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = positionInputs.positionCS;
                output.uv = input.uv;
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y * _Speed;
                float2 uv = input.uv;
                

                float2 flowUV1 = float2(uv.x * _NoiseScale, uv.y * _NoiseScale * 2.0 + time * 0.5);
                float2 flowUV2 = float2(uv.x * _NoiseScale * 1.3, uv.y * _NoiseScale * 1.8 - time * 0.3);
                float2 flowUV3 = float2(uv.x * _NoiseScale * 0.8, uv.y * _NoiseScale * 2.5 + time * 0.7);
                

                float noise1 = fbm(flowUV1);
                float noise2 = fbm(flowUV2);
                float noise3 = fbm(flowUV3);
                

                float combinedNoise = noise1 * 0.5 + noise2 * 0.3 + noise3 * 0.2;
                

                float waves = sin((uv.y + combinedNoise * _WaveAmplitude) * _WaveFrequency - time * 2.0) * 0.5 + 0.5;
                waves = pow(waves, 2.0);
                

                float waves2 = sin((uv.y + noise2 * _WaveAmplitude * 0.5) * _WaveFrequency * 1.5 + time * 1.5) * 0.5 + 0.5;
                waves2 = pow(waves2, 3.0);
                

                float3 color = lerp(_MainColor.rgb, _SecondaryColor.rgb, combinedNoise);
                

                color += _SecondaryColor.rgb * waves * 0.5;
                color += _MainColor.rgb * waves2 * 0.3;
                

                float streaks = noise(float2(uv.x * 3.0, uv.y * 10.0 - time * 2.0));
                streaks = pow(streaks, 6.0);
                color += _SecondaryColor.rgb * streaks * 0.8;
                

                float pulse = sin(time * 1.5) * 0.15 + 0.85;
                color *= pulse;
                

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, 2.0);
                color += lerp(_MainColor.rgb, _SecondaryColor.rgb, 0.5) * fresnel * 0.4;
                

                color *= _Brightness;
                
                float4 finalColor = float4(color, 1.0);
                

                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
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
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionCS = TransformWorldToHClip(positionWS);
                
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}