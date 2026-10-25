using UnityEngine;
namespace RightWalkPrototype
{
    // Shared, texture-free hologram. The existing SpriteRenderer remains the
    // capture-bounds source; this child is presentation only, with no collider.
    public sealed class ArenaHackingField
    {
        static Material material;static Mesh quad;
        readonly MeshRenderer renderer;readonly SpriteRenderer source;
        readonly MaterialPropertyBlock properties=new();
        public bool Ready=>renderer!=null;
        public ArenaHackingField(SpriteRenderer source)
        {
            if(source==null||source.sprite==null)return;this.source=source;
            if(material==null)
            {
                var shader=Resources.Load<Shader>("FoundryArt/HackingField");if(shader==null||!shader.isSupported)return;
                material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            }
            if(quad==null)
            {
                quad=new Mesh{name="Shared hacking field quad",hideFlags=HideFlags.HideAndDontSave};
                quad.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(-.5f,.5f),new Vector3(.5f,.5f),new Vector3(.5f,-.5f)};
                quad.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();
            }
            var go=new GameObject("Hacking hologram");go.transform.SetParent(source.transform,false);
            var bounds=source.sprite.bounds;go.transform.localPosition=bounds.center;go.transform.localScale=new Vector3(bounds.size.x,bounds.size.y,1);
            go.AddComponent<MeshFilter>().sharedMesh=quad;renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;
        }
        public void Show(bool active,bool next,Color color,float pulse,double now)
        {
            if(!Ready)return;renderer.enabled=active||next;if(!renderer.enabled)return;
            var size=source.bounds.size;properties.SetVector("_FieldSize",new Vector4(size.x,size.y,0,0));
            properties.SetColor("_Tint",color);properties.SetFloat("_Active",active?1:0);properties.SetFloat("_Pulse",pulse);properties.SetFloat("_Phase",(float)(now%64));renderer.SetPropertyBlock(properties);
        }
    }
}
