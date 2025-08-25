Shader "Custom/WireframeShader"
{
    Properties
    {
        _Color ("Wireframe Color", Color) = (1,1,1,1)
        _WireThickness ("Wire Thickness", Range(0.0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+200"
            "IgnoreProjector"="True"
        }
        
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        ZTest LEqual
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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            float _WireThickness;

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
                // Create wireframe effect using UV coordinates
                float2 grid = abs(frac(i.uv * 20.0) - 0.5) / fwidth(i.uv * 20.0);
                float line = min(grid.x, grid.y);
                
                // Create wireframe lines
                float wireframe = 1.0 - min(line, 1.0);
                wireframe = smoothstep(0.0, _WireThickness * 100.0, wireframe);
                
                // Distance fade
                float3 cameraPos = _WorldSpaceCameraPos;
                float distance = length(i.worldPos - cameraPos);
                float fade = 1.0 - saturate(distance / 50.0);
                
                fixed4 col = _Color;
                col.a *= wireframe * fade;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}