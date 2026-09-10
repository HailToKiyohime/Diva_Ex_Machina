Shader "Hidden/MixAlbedoBaker"
{
    Properties
    {
        _Base ("Base", 2D) = "white" {}
        _Layer1 ("Layer1", 2D) = "black" {}
        _Layer2 ("Layer2", 2D) = "black" {}
        _Layer3 ("Layer3", 2D) = "black" {}
        _Layer4 ("Layer4", 2D) = "black" {}

        _BaseColor ("BaseColor", Color) = (1,1,1,1)
        _Layer1Color ("Layer1Color", Color) = (1,1,1,1)
        _Layer2Color ("Layer2Color", Color) = (1,1,1,1)
        _Layer3Color ("Layer3Color", Color) = (1,1,1,1)
        _Layer4Color ("Layer4Color", Color) = (1,1,1,1)

        _LayerCount ("Layer Count", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _Base;
            sampler2D _Layer1;
            sampler2D _Layer2;
            sampler2D _Layer3;
            sampler2D _Layer4;

            float4 _Base_ST;
            float4 _Layer1_ST;
            float4 _Layer2_ST;
            float4 _Layer3_ST;
            float4 _Layer4_ST;

            fixed4 _BaseColor;
            fixed4 _Layer1Color;
            fixed4 _Layer2Color;
            fixed4 _Layer3Color;
            fixed4 _Layer4Color;

            float _LayerCount;

            float2 ApplyST(float2 uv, float4 st)
            {
                return uv * st.xy + st.zw;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 baseTex = tex2D(_Base, ApplyST(i.uv, _Base_ST));
                fixed3 result = baseTex.rgb * _BaseColor.rgb;

                if (_LayerCount >= 1.0)
                {
                    fixed4 layer = tex2D(_Layer1, ApplyST(i.uv, _Layer1_ST));
                    fixed3 layerRGB = layer.rgb * _Layer1Color.rgb;
                    result = lerp(result, layerRGB, layer.a);
                }

                if (_LayerCount >= 2.0)
                {
                    fixed4 layer = tex2D(_Layer2, ApplyST(i.uv, _Layer2_ST));
                    fixed3 layerRGB = layer.rgb * _Layer2Color.rgb;
                    result = lerp(result, layerRGB, layer.a);
                }

                if (_LayerCount >= 3.0)
                {
                    fixed4 layer = tex2D(_Layer3, ApplyST(i.uv, _Layer3_ST));
                    fixed3 layerRGB = layer.rgb * _Layer3Color.rgb;
                    result = lerp(result, layerRGB, layer.a);
                }

                if (_LayerCount >= 4.0)
                {
                    fixed4 layer = tex2D(_Layer4, ApplyST(i.uv, _Layer4_ST));
                    fixed3 layerRGB = layer.rgb * _Layer4Color.rgb;
                    result = lerp(result, layerRGB, layer.a);
                }

                // Your Mix materials are used as opaque equipment albedo,
                // so the baked Base Map is written opaque as well.
                return fixed4(result, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
