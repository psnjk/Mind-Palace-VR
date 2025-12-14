Shader "Custom/RetrowaveSkyboxURP"
{
    Properties
    {
        _TopColor ("Sky Top Color", Color) = (0.05, 0.02, 0.15, 1)
        _HorizonColor ("Horizon Color", Color) = (0.8, 0.2, 0.6, 1)
        _SunColor ("Sun Color", Color) = (1.0, 0.6, 0.2, 1)
        _SunSize ("Sun Radius", Range(0.05, 0.5)) = 0.25
        _SunHeight ("Sun Height", Range(-0.5, 0.5)) = -0.1

        _ScanlineCount ("Scanline Count", Range(5, 60)) = 24
        _ScanlineSharpness ("Scanline Sharpness", Range(1, 20)) = 10
        _ScanlineSpeed ("Scanline Drift Speed", Range(0, 5)) = 0.5
        _ScanlineJitter ("Scanline Jitter", Range(0, 0.2)) = 0.05
        _ScanlineStart ("Scanline Start Height", Range(0, 1)) = 0.7
        _ScanlineThicknessBoost ("Bottom Thickness Boost", Range(0, 5)) = 0.5

        _GlowStrength ("Sun Glow", Range(0, 5)) = 1.5
        _GradientPower ("Sky Gradient Power", Range(0.1, 5)) = 1.5

        _GroundTex ("Ground Grid Texture", 2D) = "white" {}
        _GroundColor ("Ground Tint", Color) = (0.4, 0.1, 0.6, 1)
        _GroundScale ("Ground Tiling", Float) = 40
    }

    SubShader
    {
        Tags
        {
            "Queue"="Background"
            "RenderType"="Background"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewDirWS  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _TopColor;
            float4 _HorizonColor;
            float4 _SunColor;
            float _SunSize;
            float _SunHeight;
            float _ScanlineCount;
            float _ScanlineSharpness;
            float _ScanlineSpeed;
            float _ScanlineJitter;
            float _ScanlineStart;
            float _ScanlineThicknessBoost;
            float _GlowStrength;
            float _GradientPower;

            TEXTURE2D(_GroundTex);
            SAMPLER(sampler_GroundTex);
            float4 _GroundColor;
            float _GroundScale;

            Varyings vert (Attributes v)
            {
                Varyings o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);

                // Correct XR-safe sky direction
                o.viewDirWS =
                    normalize(TransformObjectToWorldDir(v.positionOS.xyz));

                return o;
            }

            float SunMask(float2 uv)
            {
                float d = length(uv);
                return smoothstep(_SunSize, _SunSize - 0.01, d);
            }

            float SunRidges(float2 uv)
            {
                float time = _Time.y * _ScanlineSpeed;

                float sunBottom = -_SunSize;
                float sunTop = _SunSize;
                float height01 =
                    saturate((uv.y - sunBottom) / (sunTop - sunBottom));

                float depth = 1.0 - height01;
                float jitter =
                    sin((uv.y + time) * 40) * _ScanlineJitter * depth;

                float frequency =
                    lerp(_ScanlineCount,
                         _ScanlineCount * (1.0 - _ScanlineThicknessBoost),
                         depth);

                float y = uv.y + jitter + time;
                float scan = sin(y * frequency * PI);
                scan = scan * 0.5 + 0.5;

                float sharpness =
                    lerp(_ScanlineSharpness,
                         _ScanlineSharpness * 0.6,
                         depth);

                return pow(scan, sharpness);
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 dir = normalize(i.viewDirWS);

                float t = saturate(dir.y * 0.5 + 0.5);
                t = pow(t, _GradientPower);

                float3 skyColor =
                    lerp(_HorizonColor.rgb, _TopColor.rgb, t);

                float2 sunUV;
                sunUV.x = dir.x;
                sunUV.y = dir.y - _SunHeight;

                float sunMask = SunMask(sunUV);

                float sunBottom = -_SunSize;
                float sunTop = _SunSize;
                float height01 =
                    saturate((sunUV.y - sunBottom) / (sunTop - sunBottom));

                float ridgeRegion =
                    smoothstep(_ScanlineStart,
                               _ScanlineStart - 0.1,
                               height01);

                float ridgeMask =
                    SunRidges(sunUV) * ridgeRegion;

                float sun = sunMask * (1.0 - ridgeMask);

                float glow =
                    smoothstep(_SunSize * 1.5, 0,
                               length(sunUV)) * _GlowStrength;

                float3 finalColor = skyColor;

                if (dir.y < 0)
                {
                    float tGround = -dir.y;
                    float2 groundUV = dir.xz / max(tGround, 0.001);
                    groundUV /= _GroundScale;

                    float3 grid =
                        SAMPLE_TEXTURE2D(_GroundTex,
                                         sampler_GroundTex,
                                         groundUV).rgb;

                    finalColor = grid * _GroundColor.rgb;
                }

                finalColor += _SunColor.rgb * sun;
                finalColor += _SunColor.rgb * glow * sunMask;

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
}
