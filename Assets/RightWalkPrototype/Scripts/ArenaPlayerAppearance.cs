using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    [DefaultExecutionOrder(10000)]
    public sealed class ArenaPlayerAppearance : MonoBehaviour
    {
        sealed class Part {public Renderer renderer;public Material source,replacement;}
        readonly List<Part> parts=new();
        readonly Dictionary<Material,Material> materials=new();
        static readonly int TextureId=Shader.PropertyToID("_IdentityTexture"),EnabledId=Shader.PropertyToID("_IdentityEnabled"),ColorId=Shader.PropertyToID("_Color");
        static ArenaPlayerPaletteLibrary library;
        MaterialPropertyBlock block;
        SideScrollNetworkPlayer player;
        float nextScan;uint lastLife;bool wasDead;
        public static Color Accent(int number)=>number switch {
            2=>new Color(1,.44f,.49f),3=>new Color(.35f,.73f,1),4=>new Color(.85f,.52f,1),_=>new Color(1,.82f,.25f)};
        void Awake(){block=new MaterialPropertyBlock();player=GetComponent<SideScrollNetworkPlayer>();}
        void Scan()
        {
            var previous=new Dictionary<Renderer,Part>();foreach(var p in parts)if(p.renderer!=null)previous[p.renderer]=p;
            parts.Clear();
            foreach(var r in GetComponentsInChildren<Renderer>(true))
            {
                if(r is not SpriteRenderer && r is not MeshRenderer||r.GetComponent<TextMesh>()!=null)continue;
                if(previous.TryGetValue(r,out var known)){parts.Add(known);continue;}
                parts.Add(new Part{renderer=r});
            }
        }
        public bool RefreshPalette()
        {
            if(player==null||!player.IsSpawned)return false;
            int number=player.PlayerNumber;
            if(number<2||number>4)return true; // P1 keeps original pixels/materials.
            if(library==null)library=Resources.Load<ArenaPlayerPaletteLibrary>("PlayerPaletteLibrary");
            if(library==null)return false;
            if(lastLife!=player.LifeVersion||wasDead!=player.IsDead){lastLife=player.LifeVersion;wasDead=player.IsDead;nextScan=0;}
            if(Time.unscaledTime>=nextScan){Scan();nextScan=Time.unscaledTime+.15f;}
            foreach(var part in parts)
            {
                var r=part.renderer;if(r==null)continue;
                if(r.sharedMaterial!=part.replacement||part.source==null)part.source=r.sharedMaterial;
                var source=part.source;if(source==null)continue;
                r.GetPropertyBlock(block);
                var sr=r as SpriteRenderer;
                Texture texture=sr!=null&&sr.sprite!=null?sr.sprite.texture:block.GetTexture("_MainTex")??source.mainTexture;
                var variant=library.Variant(texture,number);
                // Preserve the procedural dark waist underlay.
                // Runtime-created resting sprites inherit Unity's URP default.
                // They join this rig's unlit sprite pass, including native fade.
                if(variant!=null&&(source.shader.name=="Sprites/Default"||source.shader.name=="Universal Render Pipeline/2D/Sprite-Lit-Default"))
                {
                    if(!materials.TryGetValue(source,out var replacement))
                    {replacement=new Material(source){name=source.name+" player palette",hideFlags=HideFlags.DontSave};replacement.shader=library.spriteShader;materials[source]=replacement;}
                    part.replacement=replacement;r.sharedMaterial=replacement;
                }
                bool supported=r.sharedMaterial!=null&&r.sharedMaterial.HasProperty(EnabledId);
                if(!supported){block.Clear();continue;}
                block.SetFloat(EnabledId,variant!=null?1:0);
                if(variant!=null)block.SetTexture(TextureId,variant);
                // A live native-death material drives its own fade; do not freeze alpha.
                if(part.replacement!=null&&r.sharedMaterial==part.replacement&&source.HasProperty(ColorId))block.SetColor(ColorId,source.GetColor(ColorId));
                r.SetPropertyBlock(block);block.Clear();
            }
            return true;
        }
        void LateUpdate(){RefreshPalette();}
        void OnDestroy(){foreach(var m in materials.Values)if(m!=null)Destroy(m);}
    }
}
