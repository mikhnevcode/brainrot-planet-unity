Shader "Brainrot/OriginalHeroSurface"
{
    Properties
    {
        _Color ("Original palette", Color) = (1,1,1,1)
        _MainTex ("Detail coordinates", 2D) = "white" {}
        _Pattern ("Material family", Float) = 0
        _Glossiness ("Smoothness", Range(0,1)) = .25
        _Metallic ("Metallic", Range(0,1)) = 0
        _EmissionColor ("Cosmetic emission", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        ZWrite On
        Cull Back
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color,_EmissionColor;
        half _Glossiness,_Metallic,_Pattern;
        struct Input {float2 uv_MainTex;float3 viewDir;};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 uv=IN.uv_MainTex;
            float frequency=_Pattern==2?95:67;
            float2 q=uv*frequency;
            float detailFade=1-saturate(max(fwidth(q.x),fwidth(q.y))*.8);
            float fine=sin(q.x*6.283+sin(q.y*4.1))*sin(q.y*6.283);
            if(_Pattern==2)fine=sin(q.x*6.283)*sin(q.y*6.283);
            if(_Pattern==3)fine=sin(q.x*3.4+sin(q.y*7.1))*sin(q.y*8.7);
            float3 color=_Color.rgb*(.97+.035*fine*detailFade);
            float rim=pow(1-saturate(dot(normalize(IN.viewDir),float3(0,0,1))),3);
            if(_Pattern==6)color=lerp(color,color*.65+float3(.30,.17,.30),rim*.28);
            o.Albedo=color;
            o.Normal=normalize(float3(.035*detailFade*sin(q.x*6.283),.035*detailFade*cos(q.y*6.283),1));
            o.Metallic=_Metallic;o.Smoothness=_Glossiness;
            o.Emission=_EmissionColor.rgb;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
