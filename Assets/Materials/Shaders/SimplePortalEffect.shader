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
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        LOD 100

        // ------------------------------------------------------------
        // Forward Pass (XR SAFE)
        // ------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _SecondaryColor;
                float  _Speed;
                float  _NoiseScale;
                float  _WaveFrequency;
                float  _WaveAmplitude;
                float  _Brightness;
            CBUFFER_END

            // ------------------------------------------------------------
            // Noise helpers
            // ------------------------------------------------------------
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1,0));
                float c = hash(i + float2(0,1));
                float d = hash(i + float2(1,1));

                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                float f = 1.0;

                for (int i = 0; i < 5; i++)
                {
                    v += a * noise(p * f);
                    f *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            Varyings vert (Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos =
                    GetVertexPositionInputs(input.positionOS.xyz);

                VertexNormalInputs norm =
                    GetVertexNormalInputs(input.normalOS);

                output.positionCS = pos.positionCS;
                output.uv = input.uv;
                output.normalWS = norm.normalWS;
                output.viewDirWS =
                    GetWorldSpaceNormalizeViewDir(pos.positionWS);
                output.fogFactor =
                    ComputeFogFactor(pos.positionCS.z);

                return output;
            }

            float4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float time = _Time.y * _Speed;
                float2 uv = input.uv;

                float2 flowUV1 = float2(
                    uv.x * _NoiseScale,
                    uv.y * _NoiseScale * 2.0 + time * 0.5);

                float2 flowUV2 = float2(
                    uv.x * _NoiseScale * 1.3,
                    uv.y * _NoiseScale * 1.8 - time * 0.3);

                float2 flowUV3 = float2(
                    uv.x * _NoiseScale * 0.8,
                    uv.y * _NoiseScale * 2.5 + time * 0.7);

                float n1 = fbm(flowUV1);
                float n2 = fbm(flowUV2);
                float n3 = fbm(flowUV3);

                float combinedNoise =
                    n1 * 0.5 + n2 * 0.3 + n3 * 0.2;

                float waves =
                    sin((uv.y + combinedNoise * _WaveAmplitude) *
                        _WaveFrequency - time * 2.0) * 0.5 + 0.5;
                waves = pow(waves, 2.0);

                float waves2 =
                    sin((uv.y + n2 * _WaveAmplitude * 0.5) *
                        _WaveFrequency * 1.5 + time * 1.5) * 0.5 + 0.5;
                waves2 = pow(waves2, 3.0);

                float3 color =
                    lerp(_MainColor.rgb, _SecondaryColor.rgb, combinedNoise);

                color += _SecondaryColor.rgb * waves * 0.5;
                color += _MainColor.rgb * waves2 * 0.3;

                float streaks =
                    noise(float2(uv.x * 3.0, uv.y * 10.0 - time * 2.0));
                streaks = pow(streaks, 6.0);
                color += _SecondaryColor.rgb * streaks * 0.8;

                float pulse = sin(time * 1.5) * 0.15 + 0.85;
                color *= pulse;

                float3 nWS = normalize(input.normalWS);
                float3 vWS = normalize(input.viewDirWS);
                float fresnel =
                    pow(1.0 - saturate(dot(nWS, vWS)), 2.0);

                color +=
                    lerp(_MainColor.rgb, _SecondaryColor.rgb, 0.5) *
                    fresnel * 0.4;

                color *= _Brightness;

                float4 finalColor = float4(color, 1.0);
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------
        // ShadowCaster Pass (XR SAFE)
        // ------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings ShadowPassVertex (Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS =
                    TransformObjectToWorld(input.positionOS.xyz);

                output.positionCS =
                    TransformWorldToHClip(positionWS);

                return output;
            }

            half4 ShadowPassFragment (Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
