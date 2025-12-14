Shader "Custom/TriplanarAutoTilingBig"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlendSharpness ("Blend Sharpness", Range(1, 8)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _BlendSharpness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
                return o;
            }

            fixed4 SampleTriplanar(float3 worldPos, float3 normal)
            {
                // Normalize weights
                float3 blend = pow(abs(normal), _BlendSharpness);
                blend /= (blend.x + blend.y + blend.z);

                // Project world-space positions (1 unit = 1 tile)
                float2 uvX = worldPos.yz/4;
                float2 uvY = worldPos.xz/4;
                float2 uvZ = worldPos.xy/4;

                fixed4 xTex = tex2D(_MainTex, uvX);
                fixed4 yTex = tex2D(_MainTex, uvY);
                fixed4 zTex = tex2D(_MainTex, uvZ);

                return xTex * blend.x + yTex * blend.y + zTex * blend.z;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return SampleTriplanar(i.worldPos, i.worldNormal);
            }
            ENDCG
        }
    }
}
