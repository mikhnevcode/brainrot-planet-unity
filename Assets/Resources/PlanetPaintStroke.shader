Shader "Brainrot/Planet Paint Stroke"
{
    Properties { _Color("Paint color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float3 world : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.color=v.color*_Color;
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // The floating trail and transient brush share the same bounded
                // planet lighting as the permanent territory texture.
                half3 n=normalize(i.world);
                half key=saturate(dot(n,normalize(half3(-.5,.8,-.4))));
                half fill=saturate(dot(n,normalize(_WorldSpaceCameraPos-i.world)));
                half light=min(1.06,.79+.13*key+.18*fill);
                return fixed4(i.color.rgb*(light*.96),i.color.a);
            }
            ENDCG
        }
    }
}
