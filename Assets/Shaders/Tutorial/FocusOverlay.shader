Shader "Tutorial/FocusOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (0, 0, 0, 0.5)
        _FocusPosition ("Focus Position (Viewport)", Vector) = (0.5, 0.5, 0, 0)
        _FocusRadius ("Focus Radius", Range(0, 1)) = 0.1
        _Feather ("Feather", Range(0.01, 1)) = 0.1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        // This blend mode prevents darkening when overlays are stacked
        Blend One OneMinusSrcAlpha 
        
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
            };

            fixed4 _Color;
            float4 _FocusPosition;
            float _FocusRadius;
            float _Feather;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 scaledUV = (i.uv - 0.5) * float2(aspectRatio, 1) + 0.5;
                float2 scaledFocus = (_FocusPosition.xy - 0.5) * float2(aspectRatio, 1) + 0.5;
                
                float dist = distance(scaledUV, scaledFocus);

                // Use smoothstep to create a soft, blurred edge
                float mask = smoothstep(_FocusRadius, _FocusRadius + _Feather, dist);

                fixed4 finalColor = _Color;
                finalColor.a *= mask;

                // Pre-multiply alpha for the new blend mode
                finalColor.rgb *= finalColor.a;

                return finalColor;
            }
            ENDCG
        }
    }
}