Shader "Brainrot/Cosmetic Spark"
{
    Properties { _Color("Tint",Color)=(1,1,1,1) _Style("Style",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
            fixed4 _Color;float _Style;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;float r=length(p);float alpha;
                if(_Style<.5)alpha=(1-smoothstep(.07,.13,abs(r-.69)))*.8+(1-smoothstep(.07,.16,length(p-float2(-.25,.4))));
                else if(_Style<1.5)alpha=1-smoothstep(.65,.85,length(float2(p.x*.8,p.y*1.5)));
                else {float a=atan2(p.y,p.x);float edge=.43+.3*pow(abs(cos(a*2.5)),3);alpha=1-smoothstep(edge-.035,edge+.035,r);}
                return fixed4(i.color.rgb,i.color.a*saturate(alpha));
            }
            ENDCG
        }
    }
}
