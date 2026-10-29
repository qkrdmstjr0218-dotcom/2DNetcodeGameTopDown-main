Shader "RightWalk/PlayerIdentitySprite"
{
 Properties {
  [PerRendererData] _MainTex("Sprite Texture",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  [MaterialToggle] PixelSnap("Pixel snap",Float)=0
  [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
  [PerRendererData] _AlphaTex("External Alpha",2D)="white"{}
  [PerRendererData] _EnableExternalAlpha("Enable External Alpha",Float)=0
  [PerRendererData] _IdentityTexture("Identity Texture",2D)="white"{}
  [PerRendererData] _IdentityEnabled("Identity Enabled",Float)=0
        [PerRendererData] _SpawnProtectionAlpha("Spawn protection alpha",Float)=1
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
  Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex SpriteVert
   #pragma fragment IdentityFrag
   #pragma multi_compile_instancing
   #pragma multi_compile _ PIXELSNAP_ON
   #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
   #include "UnitySprites.cginc"
   #include "PlayerIdentity.cginc"
   fixed4 IdentityFrag(v2f i):SV_Target {
    fixed4 c=PlayerIdentitySample(SampleSpriteTexture(i.texcoord),i.texcoord)*i.color;
    c.rgb*=c.a;return c;
   }
   ENDCG
  }
 }
}
