Shader "RightWalk/LooseRifle"
{
    Properties
    {
        [PerRendererData] _MainTex("Current rifle pixels",2D)="white"{}
        _MaskTex("Existing gun-only silhouette",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        [PerRendererData] _IdentityTexture("Identity Texture",2D)="white"{}
        [PerRendererData] _IdentityEnabled("Identity Enabled",Float)=0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "PlayerIdentity.cginc"
            sampler2D _MainTex,_MaskTex;fixed4 _Color;
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=PlayerIdentitySample(tex2D(_MainTex,i.uv),i.uv)*i.color;
                c.a*=tex2D(_MaskTex,i.uv).a;c.rgb*=c.a;return c;
            }
            ENDCG
        }
    }
}
