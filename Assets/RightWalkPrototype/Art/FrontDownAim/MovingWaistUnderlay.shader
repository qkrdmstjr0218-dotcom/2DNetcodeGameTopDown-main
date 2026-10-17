Shader "RightWalk/MovingWaistUnderlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture",2D)="white" {}
        _Color ("Tint",Color)=(1,1,1,1)
        _WaistBottomY ("Waist lower edge in world Y",Float)=10000
        [HideInInspector] _RendererColor ("RendererColor",Color)=(1,1,1,1)
        [HideInInspector] _Flip ("Flip",Vector)=(1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha",2D)="white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex ClipVert
            #pragma fragment ClipFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            float _WaistBottomY;
            struct ClipOutput {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float worldY:TEXCOORD1;};
            ClipOutput ClipVert(appdata_t input)
            {
                v2f sprite=SpriteVert(input);ClipOutput output;
                output.vertex=sprite.vertex;output.color=sprite.color;output.uv=sprite.texcoord;
                output.worldY=mul(unity_ObjectToWorld,input.vertex).y;return output;
            }
            fixed4 ClipFrag(ClipOutput input):SV_Target
            {
                fixed4 color=SampleSpriteTexture(input.uv)*input.color;
                // The native side belly is now under the retained front belt/pouches. Keep
                // its silhouette and folds as dark cloth, with no additional skirt geometry.
                float luminance=dot(color.rgb,float3(.2126,.7152,.0722));
                float waist=smoothstep(_WaistBottomY,_WaistBottomY+.035,input.worldY);
                float oldTrim=smoothstep(.045,.18,color.r-color.b);
                color.rgb*=1-.985*waist*max(smoothstep(.22,.62,luminance),oldTrim);
                color.rgb*=color.a;return color;
            }
            ENDCG
        }
    }
}
