Shader "Unlit/CombineSideBySide"
{
    Properties
    {
        _MainTex ("Texture 1 (Left)", 2D) = "white" {}
        _Tex2 ("Texture 2 (Right)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _Tex2;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Check if the current pixel is on the left or right half
                if (i.uv.x < 0.5)
                {
                    // Remap UVs from [0, 0.5] to [0, 1] for the first texture
                    float2 uv = i.uv * float2(2.0, 1.0);
                    return tex2D(_MainTex, uv);
                }
                else
                {
                    // Remap UVs from [0.5, 1] to [0, 1] for the second texture
                    float2 uv = (i.uv - float2(0.5, 0.0)) * float2(2.0, 1.0);
                    return tex2D(_Tex2, uv);
                }
            }
            ENDCG
        }
    }
}