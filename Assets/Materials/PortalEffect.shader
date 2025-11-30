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
                float4 _DepthColor;
                float _Speed;
                float _DistortionStrength;
                float _VortexTightness;
                float _DepthLayers;
                float _Brightness;
                float _EdgeGlow;
            CBUFFER_END

            // Noise function
            float noise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // Smooth noise
            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(1.0, 0.0));
                float c = noise(i + float2(0.0, 1.0));
                float d = noise(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal Brownian Motion
            float fbm(float2 uv)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * smoothNoise(uv);
                    uv *= 2.0;
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
                
                // Center UV coordinates
                float2 uv = input.uv * 2.0 - 1.0;
                float dist = length(uv);
                float angle = atan2(uv.y, uv.x);
                
                // Create swirling vortex effect
                float spiral = angle + dist * _VortexTightness - time * 2.0;
                
                // Multiple depth layers
                float3 color = _DepthColor.rgb;
                
                for(float i = 0; i < _DepthLayers; i++)
                {
                    float layerDepth = i / _DepthLayers;
                    float layerDist = dist - layerDepth * 0.3 + time * 0.1 * (i + 1.0);
                    
                    // Rotating spiral pattern
                    float2 spiralUV = float2(
                        cos(spiral + layerDepth * 6.28) * layerDist,
                        sin(spiral + layerDepth * 6.28) * layerDist
                    );
                    
                    // Add distortion with noise
                    float2 distortedUV = spiralUV * 3.0 + time * 0.5 * (1.0 - layerDepth);
                    float noiseValue = fbm(distortedUV + float2(time * 0.3, 0));
                    
                    // Create energy rings
                    float rings = sin(layerDist * 15.0 - time * 3.0 + noiseValue * _DistortionStrength) * 0.5 + 0.5;
                    rings = pow(rings, 3.0);
                    
                    // Blend colors based on depth
                    float3 layerColor = lerp(_MainColor.rgb, _SecondaryColor.rgb, layerDepth);
                    color += layerColor * rings * (1.0 - layerDepth * 0.7) * noiseValue;
                }
                
                // Add pulsing center glow
                float centerGlow = 1.0 - smoothstep(0.0, 0.5, dist);
                centerGlow *= (sin(time * 2.0) * 0.3 + 0.7);
                color += _MainColor.rgb * centerGlow * 2.0;
                
                // Edge glow effect
                float edgeGlow = smoothstep(0.7, 1.0, dist);
                color += _SecondaryColor.rgb * edgeGlow * _EdgeGlow;
                
                // Fresnel rim light for depth perception
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, 3.0);
                color += _MainColor.rgb * fresnel * 0.5;
                
                // Apply brightness
                color *= _Brightness;
                
                // Add subtle animated particles
                float2 particleUV = uv * 5.0 + time * 0.5;
                float particles = smoothNoise(particleUV + float2(time, time * 0.5));
                particles = pow(particles, 10.0);
                color += particles * _SecondaryColor.rgb * 2.0;
                
                float4 finalColor = float4(color, 1.0);
                
                // Apply fog
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
                
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
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