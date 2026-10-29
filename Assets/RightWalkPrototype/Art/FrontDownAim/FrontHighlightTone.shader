Shader "RightWalk/FrontHighlightTone"
{
    Properties
    {
        [PerRendererData] _IdentityTexture("Identity Texture",2D)="white"{}
        [PerRendererData] _IdentityEnabled("Identity Enabled",Float)=0
        [PerRendererData] _SpawnProtectionAlpha("Spawn protection alpha",Float)=1

        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HighlightReduction ("Highlight Reduction", Range(0,0.2)) = 0.08
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment ToneFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            #include "../PlayerIdentity.cginc"
            half _HighlightReduction;
            fixed4 ToneFrag(v2f input) : SV_Target
            {
                fixed4 color=PlayerIdentitySample(SampleSpriteTexture(input.texcoord),input.texcoord)*input.color;
                half luminance=dot(color.rgb,half3(0.2126,0.7152,0.0722));
                // Compress bright paint/skin while retaining dark outlines and alpha.
                color.rgb*=1-_HighlightReduction*smoothstep(0.35,0.9,luminance);
                color.rgb*=color.a;
                return color;
            }
            ENDCG
        }
    }
}
