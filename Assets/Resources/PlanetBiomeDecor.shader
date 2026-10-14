Shader "Brainrot/Biome decor"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;fixed4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION;half3 normal:TEXCOORD0;fixed4 color:COLOR;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target {half light=.78+.22*abs(dot(normalize(i.normal),normalize(half3(-.4,.8,-.3))));return fixed4(i.color.rgb*light,1);}
            ENDCG
        }
    }
}
