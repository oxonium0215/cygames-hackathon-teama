Shader "Custom/WireframeShader"
{
    Properties
    {
        _Color ("Wireframe Color", Color) = (1,1,1,1)
        _WireframeWidth ("Wireframe Width", Range(0.0, 10.0)) = 1.0
        _WireframeSmoothness ("Wireframe Smoothness", Range(0.0, 10.0)) = 1.0
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
            #pragma geometry geom
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float3 barycentric : TEXCOORD0;
            };

            fixed4 _Color;
            float _WireframeWidth;
            float _WireframeSmoothness;

            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> outStream)
            {
                g2f output;
                
                output.vertex = input[0].vertex;
                output.barycentric = float3(1, 0, 0);
                outStream.Append(output);
                
                output.vertex = input[1].vertex;
                output.barycentric = float3(0, 1, 0);
                outStream.Append(output);
                
                output.vertex = input[2].vertex;
                output.barycentric = float3(0, 0, 1);
                outStream.Append(output);
                
                outStream.RestartStrip();
            }

            fixed4 frag (g2f i) : SV_Target
            {
                // Calculate wireframe
                float3 barys = i.barycentric;
                float3 deltas = fwidth(barys);
                float3 smoothing = deltas * _WireframeSmoothness;
                float3 thickness = deltas * _WireframeWidth;
                
                barys = smoothstep(thickness, thickness + smoothing, barys);
                float minBary = min(barys.x, min(barys.y, barys.z));
                
                fixed4 col = _Color;
                col.a *= (1.0 - minBary);
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}