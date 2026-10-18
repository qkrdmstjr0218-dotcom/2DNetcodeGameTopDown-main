using System.Collections.Generic;
using UnityEngine;

namespace RightWalkPrototype
{
    // A reversible display deformation: the accepted sheet and gait remain untouched.
    [ExecuteAlways, DefaultExecutionOrder(95), RequireComponent(typeof(SpriteRenderer))]
    public sealed class RightFacingPelvisPreview : MonoBehaviour
    {
        [Range(0,6)] public float spreadPixels=3f;
        public bool SuppressDisplay { get; set; }
        private SpriteRenderer source;
        private MeshFilter filter;
        private MeshRenderer display;
        private Sprite current;
        private float cachedSpread=-1;
        private readonly Dictionary<Sprite,Mesh> meshes=new Dictionary<Sprite,Mesh>();
        private MaterialPropertyBlock properties;
        private bool createdInPlayMode;
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void ScheduleDetachedPreviewCleanup()
        {
            // A domain reload can discard a deferred Destroy from the old
            // scene. Only this component's detached, non-asset display is stale.
            UnityEditor.EditorApplication.delayCall+=()=>
            {
                foreach(var renderer in Resources.FindObjectsOfTypeAll<MeshRenderer>())
                    if(renderer.name=="Pelvis connection preview mesh"&&renderer.transform.parent==null
                        &&!renderer.gameObject.scene.IsValid()&&!UnityEditor.EditorUtility.IsPersistent(renderer))
                        DestroyImmediate(renderer.gameObject);
            };
        }
#endif
        private static readonly float[] X={0,180,210,220,230,240,245,248,250,251,252,253,254,256,260,270,280,290,310,350,512};
        private static readonly float[] Y={0,190,225,235,240,245,250,255,260,265,270,280,290,300,310,315,320,335,400,512};
        private void OnEnable() { UpdateGeometry(); }
        private void LateUpdate() { UpdateGeometry(); }
        private float Ease(float a,float b,float value) { return Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value)); }
        public void UpdateGeometry()
        {
            // ExecuteAlways also runs when a prefab asset is loaded. Such an
            // asset cannot own scene children and would leave a floating copy.
            if(!gameObject.scene.IsValid())return;
            if(source==null)source=GetComponent<SpriteRenderer>();
            if(source.sprite==null)return;
            if(filter==null)
            {
                var child=new GameObject("Pelvis connection preview mesh");child.transform.SetParent(transform,false);
                child.hideFlags=HideFlags.DontSave;
                filter=child.AddComponent<MeshFilter>();display=child.AddComponent<MeshRenderer>();
                createdInPlayMode=Application.isPlaying;
                properties=new MaterialPropertyBlock();
            }
            source.forceRenderingOff=true;
            display.forceRenderingOff=SuppressDisplay;
            if(cachedSpread!=spreadPixels)
            {
                ReleaseMeshes();cachedSpread=spreadPixels;current=null;
            }
            var sprite=source.sprite;
            if(current!=sprite)
            {
                Mesh mesh;
                if(!meshes.TryGetValue(sprite,out mesh))
                {
                    mesh=BuildMesh(sprite);meshes.Add(sprite,mesh);
                }
                filter.sharedMesh=mesh;current=sprite;
            }
            display.sharedMaterial=source.sharedMaterial;display.sortingLayerID=source.sortingLayerID;display.sortingOrder=source.sortingOrder;
            properties.SetTexture("_MainTex",sprite.texture);properties.SetColor("_Color",source.color);display.SetPropertyBlock(properties);
        }
        private Mesh BuildMesh(Sprite sprite)
        {
            var vertices=new Vector3[X.Length*Y.Length];var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];
            var triangles=new int[(X.Length-1)*(Y.Length-1)*6];
            for(int j=0;j<Y.Length;j++)for(int i=0;i<X.Length;i++)
            {
                float x=X[i],y=Y[j],offset=x-252f;
                float envelope=Ease(235,250,y)*(1f-Ease(275,315,y))*(1f-Ease(20,55,Mathf.Abs(offset)));
                float shift=spreadPixels*offset/Mathf.Sqrt(offset*offset+1f)*envelope;
                int index=j*X.Length+i;
                vertices[index]=new Vector3((x+shift-sprite.pivot.x)/sprite.pixelsPerUnit,(sprite.rect.height-y-sprite.pivot.y)/sprite.pixelsPerUnit,0);
                uv[index]=new Vector2((sprite.rect.x+x)/sprite.texture.width,(sprite.rect.y+sprite.rect.height-y)/sprite.texture.height);
                colors[index]=Color.white;
            }
            int t=0;
            for(int j=0;j<Y.Length-1;j++)for(int i=0;i<X.Length-1;i++)
            {
                int a=j*X.Length+i,b=a+1,c=a+X.Length,d=c+1;
                triangles[t++]=a;triangles[t++]=b;triangles[t++]=c;triangles[t++]=b;triangles[t++]=d;triangles[t++]=c;
            }
            var mesh=new Mesh();mesh.name="Local pelvis spread - "+sprite.name;mesh.hideFlags=HideFlags.DontSave;
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
        }
        private void ReleaseMeshes()
        {
            foreach(var mesh in meshes.Values){if(createdInPlayMode&&Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}meshes.Clear();
        }
        private void OnDisable()
        {
            if(source!=null)source.forceRenderingOff=false;
            // On entering Play, Application.isPlaying can already be true for
            // the old edit-mode instance. Deferred destruction there leaves a
            // detached DontSave renderer alive beside the actual character.
            if(filter!=null){if(createdInPlayMode&&Application.isPlaying)Destroy(filter.gameObject);else DestroyImmediate(filter.gameObject);filter=null;display=null;}
            ReleaseMeshes();current=null;
        }
    }
}
