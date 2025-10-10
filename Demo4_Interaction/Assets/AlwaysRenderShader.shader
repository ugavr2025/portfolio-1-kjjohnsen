Shader "Unlit/AlwaysRenderShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            // --- Key Settings ---
            ZWrite Off          // Don't write to the depth buffer
            ZTest Always        // Always render, ignore what's already in the depth buffer
            Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending
            Cull Off            // Render both back and front faces

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Just return the solid color
                return _Color;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
