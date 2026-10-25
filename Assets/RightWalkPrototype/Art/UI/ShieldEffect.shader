Shader "RightWalk/ShieldEffect"
{
    Properties { _MainTex("Texture",2D)="white"{} _Color("Color",Color)=(.3,.7,1,1) _Strength("Strength",Float)=1 _Pulse("Pulse",Float)=0 _Flash("Flash",Float)=0 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
        Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            v2f vert(appdata_full v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;o.color=v.color;return o;}
            float4 _Color;float _Strength,_Pulse,_Flash;
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=(i.uv-.5)*2;float r=length(p);float aa=max(fwidth(r),.002);
                float outer=(1-smoothstep(.012,.012+aa,abs(r-.94)));
                float inner=(1-smoothstep(.005,.005+aa,abs(r-.885)))*.28;
                float glow=exp(-abs(r-.94)*38)*.18;
                // Fine hexagonal energy cells, kept faint over the character.
                float2 uv=p*float2(4,6);uv.x+=floor(uv.y)*.5;
                float2 cell=abs(frac(uv)-.5);float hex=max(cell.x*.866+cell.y*.5,cell.y);
                float grid=(1-smoothstep(.012,.025,abs(hex-.45)))*.065;
                float scan=pow(saturate(1-abs(p.y-sin(_Pulse*.65)*.8)*8),3)*.05;
                float inside=1-smoothstep(.88,.9,r);
                float a=outer*(.58+.12*sin(_Pulse*2))+inner+glow+inside*(.018+grid+scan+_Flash*.2);
                a*=lerp(.55,1,_Strength)*i.color.a;
                return float4(lerp(_Color.rgb,float3(.8,.96,1),_Flash),saturate(a));
            }
            ENDCG
        }
    }
}
