Shader "RightWalk/HackingField"
{
    Properties { _Tint("Tint",Color)=(.25,.85,1,1) _FieldSize("World size",Vector)=(4,2.35,0,0) _Active("Active",Float)=1 _Pulse("Warning",Float)=1 _Phase("Shared time",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
            struct Varying {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
            float4 _Tint,_FieldSize;float _Active,_Pulse,_Phase;
            Varying Vert(Input v){Varying o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
            fixed4 Frag(Varying v):SV_Target
            {
                float2 halfSize=max(_FieldSize.xy*.5,float2(.1,.1));
                float2 p=(v.uv-.5)*halfSize*2,q=abs(p)-halfSize;
                // Chamfered perimeter: broad corner brackets, never platform-like dashes.
                float d=max(max(q.x,q.y),(q.x+q.y+.12)*.7071);
                float aa=max(fwidth(d),.001),inside=1-smoothstep(-aa,aa,d);
                float edge=1-smoothstep(.008,.008+aa,abs(d));
                float corner=step(halfSize.x-.3,abs(p.x))*step(halfSize.y-.3,abs(p.y));
                float bracket=(1-smoothstep(.024,.024+aa,abs(d)))*corner;
                float glow=exp(-abs(d)*26)*inside;
                float floorLight=1-smoothstep(.012,.036,abs(p.y+halfSize.y-.055));
                float shaft=max(0,1-abs(p.x)/(.28+.2*v.uv.y))*(1-v.uv.y)*v.uv.y;
                float beamX=sin(_Phase*.785398163397)*halfSize.x;
                float beam=exp(-pow((p.x-beamX)*2.5,2));
                float fill=(.018+.015*(1-v.uv.y)+beam*.012+shaft*.08)*_Active;
                float alpha=inside*(fill+edge*.26+bracket*.55+glow*.07+floorLight*.35);
                alpha*=lerp(.32,1,_Active)*_Pulse;
                float3 color=lerp(_Tint.rgb,lerp(_Tint.rgb,float3(.9,1,1),.38),saturate(bracket+floorLight));
                return fixed4(color,saturate(alpha));
            }
            ENDCG
        }
    }
}
