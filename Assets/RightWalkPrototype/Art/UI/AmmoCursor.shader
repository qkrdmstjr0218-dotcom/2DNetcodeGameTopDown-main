Shader "RightWalk/AmmoCursor"
{
    Properties { _Ink("Ink",Color)=(.3,.9,1,1) _Ammo("Ammo",Float)=12 _Capacity("Capacity",Float)=12 _Empty("Empty",Float)=0 _Blocked("Blocked",Float)=0 _Wait("Wait",Float)=0 }
    SubShader
    {
        Tags {"Queue"="Overlay" "RenderType"="Transparent"}
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
            float4 _Ink;float _Ammo,_Capacity,_Empty,_Blocked,_Wait;
            float stroke(float distance,float halfWidth){return 1-smoothstep(halfWidth-.55,halfWidth+.55,distance);}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=(i.uv-.5)*64;float radius=length(p);
                // Analytic curves with pixel-wide antialiasing, rather than joined line pieces.
                float angle=frac(atan2(p.x,p.y)/6.28318530718+1);
                float unit=angle*_Capacity;float segment=floor(unit);float part=frac(unit);
                float gap=1.5*_Capacity/(6.28318530718*22);
                float aa=max(fwidth(unit),.001);
                float ends=smoothstep(gap,gap+aa,part)*(1-smoothstep(1-gap-aa,1-gap,part));
                float ring=stroke(abs(radius-22),1.6)*ends;
                float filled=saturate(_Ammo-segment);
                float amount=1-smoothstep(filled-aa,filled+aa,part);
                float3 tint=lerp(_Ink.rgb,float3(1,.23,.17),_Empty);
                float3 rgb=lerp(float3(.12,.22,.25),tint,max(amount,_Empty*.65));
                float alpha=ring*lerp(.66,.98,amount);
                float cross=max(stroke(abs(p.x),.65)*smoothstep(3,4,abs(p.y))*(1-smoothstep(7,8,abs(p.y))),stroke(abs(p.y),.65)*smoothstep(3,4,abs(p.x))*(1-smoothstep(7,8,abs(p.x))));
                float cooldown=stroke(abs(radius-12),.8)*(1-smoothstep(_Wait-.008,_Wait+.008,angle))*step(.001,_Wait);
                if(cross>alpha){rgb=lerp(float3(.92,.97,1),float3(.55,.62,.65),_Blocked);rgb=lerp(rgb,float3(1,.23,.17),_Empty);alpha=cross;}
                if(cooldown>alpha){rgb=float3(.7,.83,.85);alpha=cooldown*.8;}
                return float4(rgb,alpha);
            }
            ENDCG
        }
    }
}
