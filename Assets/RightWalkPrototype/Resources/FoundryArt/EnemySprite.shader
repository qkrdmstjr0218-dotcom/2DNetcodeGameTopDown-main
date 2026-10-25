Shader "RightWalk/SecurityUnitSprite"
{
 Properties {
  [PerRendererData] _MainTex("Sprite Texture",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
  [PerRendererData] _AlphaTex("External Alpha",2D)="white"{}
  [PerRendererData] _EnableExternalAlpha("Enable External Alpha",Float)=0
  [PerRendererData] _HitFlash("Hit Flash",Range(0,1))=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
  Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex SpriteVert
   #pragma fragment SecurityFrag
   #pragma multi_compile_instancing
   #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
   #include "UnitySprites.cginc"
   float _HitFlash;
   fixed4 SecurityFrag(v2f i):SV_Target {
    fixed4 c=SampleSpriteTexture(i.texcoord);
    c.rgb=lerp(c.rgb,fixed3(1,.95,.8),_HitFlash);c*=i.color;
    c.rgb*=c.a;return c;
   }
   ENDCG
  }
 }
}
