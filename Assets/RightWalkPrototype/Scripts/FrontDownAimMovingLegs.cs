using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RightWalkPrototype
{
    // Front torso and hip equipment above the original animated side-facing legs.
    [DisallowMultipleComponent]
    public sealed class FrontDownAimMovingLegs : MonoBehaviour
    {
        private GameObject torso,lowerBody;
        private Mesh mesh;
        private MeshRenderer gait,lowerRenderer;
        private MeshFilter lowerFilter;
        private MaterialPropertyBlock lowerProperties;
        private SpriteRenderer[] transitionParts=new SpriteRenderer[0];
        private Material[] transitionMaterials;
        private readonly List<Vector2> points=new List<Vector2>();
        private readonly List<int> triangles=new List<int>();
        private Vector3[] vertices;
        private static Vector2[] Rect(float x0,float y0,float x1,float y1)
        {return new[]{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),new Vector2(x0,y1)};}
        private void Piece(Vector2[] shape)
        {
            int first=points.Count;points.AddRange(shape);
            for(int i=2;i<shape.Length;i++){triangles.Add(first);triangles.Add(first+i-1);triangles.Add(first+i);}
        }
        private void Initialize(FrontDownAimOverlay owner)
        {
            var native=transform.Find("Pelvis connection preview mesh");
            if(native!=null)gait=native.GetComponent<MeshRenderer>();
            if(gait==null)return;
            var transition=transform.Find("Jump transition original leg parts");
            if(transition!=null)transitionParts=transition.GetComponentsInChildren<SpriteRenderer>(true);
            transitionMaterials=new Material[transitionParts.Length];
            for(int i=0;i<transitionParts.Length;i++)transitionMaterials[i]=transitionParts[i].sharedMaterial;
            // Abdomen, belt/buckle and pouches belong to this upper-body cutout.
            // The narrow white crotch panel and the black trousers stay with the lower body.
            Piece(Rect(21,1,1004,650));
            Piece(new[]{new Vector2(421,650),new Vector2(613,650),new Vector2(613,665),new Vector2(589,675),new Vector2(558,684),new Vector2(548,690),new Vector2(540,696),new Vector2(532,699),new Vector2(520,699),new Vector2(510,694),new Vector2(506,688),new Vector2(490,684),new Vector2(462,680),new Vector2(433,672),new Vector2(421,665)});
            // Insets follow the actual dark hems, excluding neighboring white thigh armor.
            Piece(new[]{new Vector2(250,650),new Vector2(448,650),new Vector2(432,680),new Vector2(410,715),new Vector2(389,744),new Vector2(373,760),new Vector2(356,765),new Vector2(338,763),new Vector2(322,756),new Vector2(308,746),new Vector2(295,731),new Vector2(280,722),new Vector2(260,720)});
            Piece(new[]{new Vector2(615,650),new Vector2(800,650),new Vector2(800,735),new Vector2(780,740),new Vector2(764,753),new Vector2(745,763),new Vector2(725,766),new Vector2(705,766),new Vector2(686,761),new Vector2(670,748),new Vector2(652,722),new Vector2(632,686)});
            var layer=owner.Data.layers[0];foreach(var candidate in owner.Data.layers)if(candidate.id=="body")layer=candidate;
            var b=layer.bounds;var a=layer.atlas;var uv=new Vector2[points.Count];var colors=new Color[points.Count];vertices=new Vector3[points.Count];
            for(int i=0;i<points.Count;i++)
            {
                colors[i]=Color.white;var p=points[i];
                uv[i]=new Vector2((a[0]+(p.x-b[0])/(b[2]-b[0])*a[2])/owner.Data.atlasSize[0],
                    1-(a[1]+(p.y-b[1])/(b[3]-b[1])*a[3])/owner.Data.atlasSize[1]);
            }
            mesh=new Mesh{name="Moving down complete torso and hip gear",hideFlags=HideFlags.DontSave};
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles.ToArray();
            torso=new GameObject("Moving down aim upper body");torso.hideFlags=HideFlags.DontSave;torso.layer=gameObject.layer;torso.transform.SetParent(transform,false);
            torso.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=torso.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=owner.material;renderer.sortingOrder=12;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;torso.SetActive(false);
            lowerBody=new GameObject("Moving down original lower body");lowerBody.hideFlags=HideFlags.DontSave;lowerBody.layer=gameObject.layer;lowerBody.transform.SetParent(transform,false);
            lowerFilter=lowerBody.AddComponent<MeshFilter>();lowerRenderer=lowerBody.AddComponent<MeshRenderer>();
            lowerRenderer.sharedMaterial=gait.sharedMaterial;lowerRenderer.shadowCastingMode=ShadowCastingMode.Off;lowerRenderer.receiveShadows=false;
            lowerProperties=new MaterialPropertyBlock();lowerBody.SetActive(false);
        }
        public void Show(bool visible,FrontDownAimOverlay owner)
        {
            if(!visible)
            {
                Hide();
                if(owner.IsActive)
                {
                    if(gait!=null)gait.enabled=false;
                    foreach(var part in transitionParts)if(part!=null)part.enabled=false;
                }
                return;
            }
            if(torso==null)Initialize(owner);if(torso==null)return;
            for(int i=0;i<points.Count;i++)vertices[i]=owner.BodyPoint(points[i]);mesh.vertices=vertices;mesh.RecalculateBounds();
            // Copy the original gait mesh, material and color unchanged, including white trousers.
            lowerFilter.sharedMesh=gait.GetComponent<MeshFilter>().sharedMesh;
            lowerRenderer.sortingLayerID=gait.sortingLayerID;lowerRenderer.sortingOrder=gait.sortingOrder;
            lowerRenderer.sharedMaterial=gait.sharedMaterial;
            gait.GetPropertyBlock(lowerProperties);
            lowerRenderer.SetPropertyBlock(lowerProperties);
            gait.enabled=false;lowerBody.SetActive(true);
            // Landing retains its eight original leg parts and transition timing.
            for(int i=0;i<transitionParts.Length;i++)
            {
                var part=transitionParts[i];if(part==null)continue;
                part.sharedMaterial=transitionMaterials[i];part.enabled=true;
            }
            torso.SetActive(true);
        }
        public void Hide()
        {
            if(torso!=null)torso.SetActive(false);if(lowerBody!=null)lowerBody.SetActive(false);
            if(transitionMaterials!=null)for(int i=0;i<transitionParts.Length;i++)if(transitionParts[i]!=null)transitionParts[i].sharedMaterial=transitionMaterials[i];
        }
        private void OnDisable(){Hide();}
        private static void Release(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy(){Hide();Release(torso);Release(lowerBody);Release(mesh);}
    }
}
