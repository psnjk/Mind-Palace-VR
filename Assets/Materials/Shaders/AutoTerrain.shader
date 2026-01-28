Shader "Prototype/AutoTerrain"
{
    Properties
    {
        _GrassTex ("Grass Texture", 2D) = "white" {}
        _GroundTex ("Ground Texture", 2D) = "white" {}
        _RockTex ("Rock Texture", 2D) = "white" {}
        _SnowTex ("Snow Texture", 2D) = "white" {}

        _Tiling ("Texture Tiling", Float) = 10

        _SnowHeight ("Snow Height", Float) = 20
        _SnowBlend ("Snow Blend", Float) = 5

        _RockSlope ("Rock Slope", Range(0,1)) = 0.6
        _RockBlend ("Rock Blend", Float) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _GrassTex;
        sampler2D _GroundTex;
        sampler2D _RockTex;
        sampler2D _SnowTex;

        float _Tiling;
        float _SnowHeight;
        float _SnowBlend;
        float _RockSlope;
        float _RockBlend;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.worldPos.xz / _Tiling;

            fixed4 grass  = tex2D(_GrassTex, uv);
            fixed4 ground = tex2D(_GroundTex, uv);
            fixed4 rock   = tex2D(_RockTex, uv);
            fixed4 snow   = tex2D(_SnowTex, uv);

            // Slope: 0 = flat, 1 = vertical
            float slope = 1 - dot(normalize(IN.worldNormal), float3(0,1,0));

            // Height based snow mask
            float snowMask = saturate((IN.worldPos.y - _SnowHeight) / _SnowBlend);

            // Slope based rock mask
            float rockMask = saturate((slope - _RockSlope) / _RockBlend);

            // Base blend: grass → ground
            fixed4 baseTex = lerp(grass, ground, saturate(IN.worldPos.y * 0.05));

            // Add rock on steep slopes
            fixed4 slopeTex = lerp(baseTex, rock, rockMask);

            // Add snow on high areas
            fixed4 finalTex = lerp(slopeTex, snow, snowMask);

            o.Albedo = finalTex.rgb;
            o.Metallic = 0;
            o.Smoothness = 0.2;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
