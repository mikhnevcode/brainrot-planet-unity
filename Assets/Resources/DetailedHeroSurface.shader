Shader "Brainrot/DetailedHeroSurface"
{
    Properties
    {
        _Color ("Cosmetic palette", Color) = (1,1,1,1)
        _BaseTint ("Authored palette", Color) = (1,1,1,1)
        _MainTex ("Baked sculpt color", 2D) = "white" {}
        _TintMask ("Cosmetic region", 2D) = "white" {}
        _BumpMap ("Baked sculpt normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Range(0,1)) = .55
        _Glossiness ("Smoothness", Range(0,1)) = .35
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
        sampler2D _MainTex,_TintMask,_BumpMap;
        fixed4 _Color,_BaseTint,_EmissionColor;
        half _BumpScale,_Glossiness,_Metallic;
        struct Input {float2 uv_MainTex;};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            half mask=tex2D(_TintMask,IN.uv_MainTex).r;
            half3 authored=tex2D(_MainTex,IN.uv_MainTex).rgb;
            half detail=dot(authored,half3(.2126,.7152,.0722))/max(.02,dot(_BaseTint.rgb,half3(.2126,.7152,.0722)));
            half changed=saturate(length(_Color.rgb-_BaseTint.rgb)*1000);
            o.Albedo=saturate(lerp(authored,_Color.rgb*detail,mask*changed));
            half3 normal=UnpackNormal(tex2D(_BumpMap,IN.uv_MainTex));
            normal.xy*=_BumpScale;o.Normal=normalize(normal);
            o.Smoothness=_Glossiness;o.Metallic=_Metallic;
            o.Emission=_EmissionColor.rgb;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
