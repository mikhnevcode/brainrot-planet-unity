Shader "Brainrot/Biome decor"
{
    Properties { _Sway("Foliage sway",Float)=0 _Pulse("Detail glow",Float)=0 }
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
            float _Sway,_Pulse;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;half3 normal:TEXCOORD0;fixed4 color:COLOR;half glow:TEXCOORD1;};
            v2f vert(appdata v){v2f o;float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                float3 radial=normalize(world+float3(.001,.001,.001));
                float3 tangent=normalize(cross(radial,float3(0,1,0))+float3(.001,0,.001));
                world+=tangent*(sin(_Time.y*1.7+dot(world,float3(.7,.3,.8)))*v.uv.x*_Sway);
                o.vertex=mul(UNITY_MATRIX_VP,float4(world,1));o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;
                o.glow=v.uv.y*_Pulse*(.15+.08*sin(_Time.y*1.8+world.x));return o;}
            fixed4 frag(v2f i):SV_Target {half light=.78+.22*abs(dot(normalize(i.normal),normalize(half3(-.4,.8,-.3))));return fixed4(i.color.rgb*(light+i.glow),1);}
            ENDCG
        }
    }
}
