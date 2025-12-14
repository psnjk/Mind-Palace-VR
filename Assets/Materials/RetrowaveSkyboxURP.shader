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
        _ScanlineStart ("Scanline Start Height (0–1)", Range(0, 1)) = 0.7
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS : TEXCOORD0;
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
            TEXTURE2D(_GroundTex);
            SAMPLER(sampler_GroundTex);
            float4 _GroundColor;
            float _GroundScale;
            float _GlowStrength;
            float _GradientPower;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dirWS = TransformObjectToWorld(v.positionOS.xyz);
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
                float height01 = saturate((uv.y - sunBottom) / (sunTop - sunBottom));

                float depth = 1.0 - height01;

                float jitter = sin((uv.y + time) * 40) * _ScanlineJitter * depth;

                float frequency = lerp(_ScanlineCount, _ScanlineCount * (1.0 - _ScanlineThicknessBoost), depth);

                float y = uv.y + jitter + time;
                float scan = sin(y * frequency * 3.14159);
                scan = scan * 0.5 + 0.5;

                float sharpness = lerp(_ScanlineSharpness, _ScanlineSharpness * 0.6, depth);
                return pow(scan, sharpness);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 dir = normalize(i.dirWS);
                float t = saturate(dir.y * 0.5 + 0.5);
                t = pow(t, _GradientPower);

                float3 skyColor = lerp(_HorizonColor.rgb, _TopColor.rgb, t);

                float2 sunUV;
                sunUV.x = dir.x;
                sunUV.y = dir.y - _SunHeight;

                float sunMask = SunMask(sunUV);

                // Smooth fade-in for scanlines near the center to avoid hard cutoff
                // Fade scanlines in starting higher up the sun (about 70% from bottom)
                // sunUV.y is negative at bottom, positive at top
                float ridgeStart = lerp(0.0, 0.7, 1.0); // conceptual 70% height
                // Fade scanlines in starting at ~70% of the sun height (independent of radius)
                // Normalized height inside sun: 0 = bottom, 1 = top
                float sunBottom = -_SunSize;
                float sunTop = _SunSize;
                float height01 = saturate((sunUV.y - sunBottom) / (sunTop - sunBottom));

                // Enable scanlines only from bottom up to 70% height
                float ridgeRegion = smoothstep(_ScanlineStart, _ScanlineStart - 0.1, height01);
                // Allow scanlines well above center (approx 70% from bottom)
                float ridgeMask = SunRidges(sunUV) * ridgeRegion;

                // Invert scanlines so lines become transparent cutouts
                float sun = sunMask * (1.0 - ridgeMask);
                float glow = smoothstep(_SunSize * 1.5, 0, length(sunUV)) * _GlowStrength;

                float3 finalColor = skyColor;

                // Ground projection (procedural skybox-style)
                if (dir.y < 0)
                {
                    float tGround = -dir.y;
                    float2 groundUV = dir.xz / max(tGround, 0.001);
                    groundUV /= _GroundScale;
                    float3 grid = SAMPLE_TEXTURE2D(_GroundTex, sampler_GroundTex, groundUV).rgb;
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
