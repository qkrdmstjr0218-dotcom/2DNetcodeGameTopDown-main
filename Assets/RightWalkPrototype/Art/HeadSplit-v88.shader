Shader "RightWalk/HeadSplit"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _RearLayer("Rear hair layer",Float)=0
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
            sampler2D _MainTex;float4 _MainTex_TexelSize;fixed4 _Color;float _RearLayer;
            struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 pixel=float2(i.uv.x,1-i.uv.y)*_MainTex_TexelSize.zw;
                // Separate the lower rear hair along the outside of the ear.
                // Complementary fragment selection preserves the original pixels exactly.
                float boundary=lerp(242,245,saturate((pixel.y-111)/17));
                // The skin below the rear of the ear belongs behind the collar too.
                if(pixel.y>123)boundary=lerp(244.12,261,saturate((pixel.y-123)/10));
                bool rear=pixel.y>=108 && pixel.x<boundary;
                if(rear != (_RearLayer>.5))discard;
                fixed4 color=tex2D(_MainTex,i.uv)*i.color;
                color.rgb*=color.a;
                return color;
            }
            ENDCG
        }
    }
}
