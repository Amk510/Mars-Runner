Shader "Skybox/GradienteMarte"
{
    Properties
    {
        _ColorCenit      ("Color Cenit (arriba)", Color) = (0.478, 0.243, 0.212, 1)
        _ColorMedio      ("Color Medio", Color)          = (0.851, 0.537, 0.361, 1)
        _ColorHorizonte  ("Color Horizonte", Color)      = (0.949, 0.706, 0.549, 1)
        _ColorSuelo      ("Color bajo el horizonte", Color) = (0.541, 0.290, 0.180, 1)
        _AlturaMedio     ("Altura del color medio", Range(0.01, 1)) = 0.12
        _AlturaCenit     ("Altura del color cenit", Range(0.02, 1)) = 0.35
        _Suavidad        ("Suavidad bajo el horizonte", Range(0.001, 0.5)) = 0.05
        _Exposicion      ("Exposición", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _ColorCenit;
            fixed4 _ColorMedio;
            fixed4 _ColorHorizonte;
            fixed4 _ColorSuelo;
            float _AlturaMedio;
            float _AlturaCenit;
            float _Suavidad;
            float _Exposicion;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float y = normalize(i.dir).y;
                float3 c;

                if (y < 0.0)
                {
                    c = lerp(_ColorHorizonte.rgb, _ColorSuelo.rgb, saturate(-y / _Suavidad));
                }
                else if (y < _AlturaMedio)
                {
                    c = lerp(_ColorHorizonte.rgb, _ColorMedio.rgb, smoothstep(0.0, 1.0, y / _AlturaMedio));
                }
                else
                {
                    float t = saturate((y - _AlturaMedio) / max(_AlturaCenit - _AlturaMedio, 0.0001));
                    c = lerp(_ColorMedio.rgb, _ColorCenit.rgb, smoothstep(0.0, 1.0, t));
                }

                return fixed4(c * _Exposicion, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}