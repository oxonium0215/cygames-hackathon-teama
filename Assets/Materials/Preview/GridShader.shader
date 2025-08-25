Shader "Custom/GridShader"
{
    Properties
    {
        _Color ("Grid Color", Color) = (1,1,1,0.5)
        _GridScale ("Grid Scale", Float) = 10.0
        _LineWidth ("Line Width", Range(0.01, 0.1)) = 0.02
        _FadeDistance ("Fade Distance", Range(0.1, 10.0)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+100"
            "IgnoreProjector"="True"
        }
        
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
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
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            float _GridScale;
            float _LineWidth;
            float _FadeDistance;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate grid coordinates
                float2 gridUV = i.worldPos.xz * _GridScale;
                
                // Create grid lines
                float2 grid = abs(frac(gridUV - 0.5) - 0.5) / fwidth(gridUV);
                float line = min(grid.x, grid.y);
                
                // Sharp grid lines
                float gridLine = 1.0 - min(line, 1.0);
                gridLine = smoothstep(0.0, _LineWidth, gridLine);
                
                // Distance-based fade
                float3 cameraPos = _WorldSpaceCameraPos;
                float distance = length(i.worldPos - cameraPos);
                float fade = 1.0 - saturate(distance / _FadeDistance / 20.0);
                
                // Final color with fade
                fixed4 col = _Color;
                col.a *= gridLine * fade;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}