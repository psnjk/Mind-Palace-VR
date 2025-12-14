Shader "Custom/URP/PortalEffect"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.2, 0.5, 1.0, 1.0)
        _SecondaryColor ("Secondary Color", Color) = (0.8, 0.3, 1.0, 1.0)
        _DepthColor ("Depth Color", Color) = (0.05, 0.1, 0.3, 1.0)
        _Speed ("Animation Speed", Range(0.1, 5.0)) = 1.0
        _DistortionStrength ("Distortion Strength", Range(0, 2)) = 0.3
        _VortexTightness ("Vortex Tightness", Range(0.1, 10)) = 3.0
        _DepthLayers ("Depth Layers", Range(1, 5)) = 3.0
        _Brightness ("Brightness", Range(0, 3)) = 1.5
        _EdgeGlow ("Edge Glow", Range(0, 2)) = 0.8
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
                float4 _DepthColor;
                float  _Speed;
                float  _DistortionStrength;
                float  _VortexTightness;
                float  _DepthLayers;
                float  _Brightness;
                float  _EdgeGlow;
            CBUFFER_END

            // ------------------------------------------------------------
            // Noise helpers
            // ------------------------------------------------------------
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898,78.233))) * 43758.5453);
            }

            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = noise(i);
                float b = noise(i + float2(1,0));
                float c = noise(i + float2(0,1));
                float d = noise(i + float2(1,1));

                return lerp(lerp(a,b,f.x), lerp(c,d,f.x), f.y);
            }

            float fbm(float2 uv)
            {
                float v = 0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * smoothNoise(uv);
                    uv *= 2;
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

                float2 uv = input.uv * 2.0 - 1.0;
                float dist = length(uv);
                float angle = atan2(uv.y, uv.x);

                float spiral = angle + dist * _VortexTightness - time * 2.0;

                float3 color = _DepthColor.rgb;

                for (float i = 0; i < _DepthLayers; i++)
                {
                    float layerDepth = i / _DepthLayers;
                    float layerDist =
                        dist - layerDepth * 0.3 + time * 0.1 * (i + 1);

                    float2 spiralUV = float2(
                        cos(spiral + layerDepth * 6.28318) * layerDist,
                        sin(spiral + layerDepth * 6.28318) * layerDist
                    );

                    float2 distortedUV =
                        spiralUV * 3.0 + time * 0.5 * (1.0 - layerDepth);

                    float n = fbm(distortedUV + float2(time * 0.3, 0));

                    float rings =
                        sin(layerDist * 15.0 - time * 3.0 +
                            n * _DistortionStrength) * 0.5 + 0.5;

                    rings = pow(rings, 3.0);

                    float3 layerColor =
                        lerp(_MainColor.rgb, _SecondaryColor.rgb, layerDepth);

                    color += layerColor * rings *
                             (1.0 - layerDepth * 0.7) * n;
                }

                float centerGlow =
                    1.0 - smoothstep(0.0, 0.5, dist);
                centerGlow *= (sin(time * 2.0) * 0.3 + 0.7);
                color += _MainColor.rgb * centerGlow * 2.0;

                float edgeGlow = smoothstep(0.7, 1.0, dist);
                color += _SecondaryColor.rgb * edgeGlow * _EdgeGlow;

                float3 nWS = normalize(input.normalWS);
                float3 vWS = normalize(input.viewDirWS);
                float fresnel =
                    pow(1.0 - saturate(dot(nWS, vWS)), 3.0);

                color += _MainColor.rgb * fresnel * 0.5;
                color *= _Brightness;

                float2 pUV = uv * 5.0 + time * 0.5;
                float particles =
                    pow(abs(smoothNoise(pUV + float2(time, time * 0.5))), 10.0);

                color += particles * _SecondaryColor.rgb * 2.0;

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
