Shader "RightWalk/SecurityUnitSprite"
{
 Properties {
  [PerRendererData] _MainTex("Sprite Texture",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
  [PerRendererData] _AlphaTex("External Alpha",2D)="white"{}
  [PerRendererData] _EnableExternalAlpha("Enable External Alpha",Float)=0
  [PerRendererData] _EntryClipY("Entry floor",Float)=-10000
  [PerRendererData] _HitFlash("Hit Flash",Range(0,1))=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
  Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex SecurityVert
   #pragma fragment SecurityFrag
   #pragma multi_compile_instancing
   #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
   #include "UnitySprites.cginc"
   float _HitFlash, _EntryClipY;
   struct SecurityVaryings {
    float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 texcoord:TEXCOORD0;
    float worldY:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO
   };
   SecurityVaryings SecurityVert(appdata_t v) {
    v2f baseVertex=SpriteVert(v); SecurityVaryings o;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.vertex=baseVertex.vertex;o.color=baseVertex.color;o.texcoord=baseVertex.texcoord;
    o.worldY=mul(unity_ObjectToWorld,UnityFlipSprite(v.vertex,_Flip)).y;return o;
   }
   fixed4 SecurityFrag(SecurityVaryings i):SV_Target {
    clip(i.worldY-_EntryClipY);
    fixed4 c=SampleSpriteTexture(i.texcoord);
    c.rgb=lerp(c.rgb,fixed3(1,.95,.8),_HitFlash);c*=i.color;
    c.rgb*=c.a;return c;
   }
   ENDCG
  }
 }
}
