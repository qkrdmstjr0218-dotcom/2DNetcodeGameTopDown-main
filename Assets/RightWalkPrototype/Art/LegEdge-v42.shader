Shader "RightWalk/LegEdge"
{
    Properties {
        [PerRendererData] _IdentityTexture("Identity Texture",2D)="white"{}
        [PerRendererData] _IdentityEnabled("Identity Enabled",Float)=0
 [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "PlayerIdentity.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize; fixed4 _Color;
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=PlayerIdentitySample(tex2D(_MainTex,i.uv),i.uv);float2 t=_MainTex_TexelSize.xy;
                float edge=max(max(tex2D(_MainTex,i.uv+float2(t.x,0)).a,tex2D(_MainTex,i.uv-float2(t.x,0)).a),
                    max(tex2D(_MainTex,i.uv+float2(0,t.y)).a,tex2D(_MainTex,i.uv-float2(0,t.y)).a));
                float alpha=max(c.a,edge*.85);
                float2 localUV=frac(i.uv*float2(5,4));
                // The restored rear thigh already carries a baked black contour.
                // Avoid growing a second translucent fringe over that contour.
                if(localUV.x>.39 && localUV.x<.55 && localUV.y>.35 && localUV.y<.55)
                {
                    c.a=smoothstep(.4,.75,c.a);
                    if(c.a<.01) return fixed4(0,0,0,0);
                    return c*i.color;
                }
                c.rgb=lerp(float3(.035,.03,.03),c.rgb,c.a/max(alpha,.0001));c.a=alpha;
                return c*i.color;
            }
            ENDCG
        }
    }
}
