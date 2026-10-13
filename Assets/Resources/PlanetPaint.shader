Shader "Brainrot/Planet Paint Detail"
{
    Properties { _MainTex("Territory",2D)="white"{} _DetailTex("Fine grain",2D)="gray"{} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            // Single bounded-light pass; character lights cannot bleach the map.
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex,_DetailTex;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; half3 normal:TEXCOORD2; };
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.uv=v.uv;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                half3 n=normalize(i.normal),weights=abs(n);weights/=max(.001,weights.x+weights.y+weights.z);
                half detail=tex2D(_DetailTex,i.world.yz*.6).r*weights.x+tex2D(_DetailTex,i.world.xz*.6).r*weights.y+tex2D(_DetailTex,i.world.xy*.6).r*weights.z;
                half key=saturate(dot(n,normalize(half3(-.5,.8,-.4))));
                half fill=saturate(dot(n,normalize(_WorldSpaceCameraPos-i.world)));
                half light=min(1.06,.79+.13*key+.18*fill);
                fixed3 color=tex2D(_MainTex,i.uv).rgb;
                return fixed4(color*light*(.90+.20*detail),1);
            }
            ENDCG
        }
    }
}
