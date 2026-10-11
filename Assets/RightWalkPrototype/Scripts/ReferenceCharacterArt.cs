using UnityEngine;

namespace RightWalkPrototype
{
    [DefaultExecutionOrder(100)]
    public sealed class ReferenceCharacterArt : MonoBehaviour
    {
        public Sprite[] legSprites;
        public Sprite upperBody, pelvis;
        public Material material;
        public bool showBones;
        private readonly SpriteRenderer[] art=new SpriteRenderer[6];
        private readonly LineRenderer[] bones=new LineRenderer[6];
        private SpriteRenderer torso, belt;
        private readonly System.Collections.Generic.List<Material> ownedMaterials=new System.Collections.Generic.List<Material>();
        private readonly float[] axes={-108.62f,-84.83f,0,-67.73f,-95.58f,0};
        private void Start()
        {
            for(int i=0;i<6;i++)
            {
                bones[i]=transform.Find((i<3?"A":"B")+" "+new[]{"Thigh","Calf","Foot"}[i%3]).GetComponent<LineRenderer>();
                art[i]=Create("Character part "+i,legSprites[i],i<3?22:10);
                art[i].color=i<3?Color.white:new Color(.72f,.76f,.83f);
            }
            torso=Create("Aiming upper body",upperBody,25);
            belt=Create("Pelvis cover",pelvis,24);
            torso.transform.localScale=belt.transform.localScale=Vector3.one*2.4f;
        }
        private SpriteRenderer Create(string name,Sprite sprite,int order)
        {
            var sr=new GameObject(name).AddComponent<SpriteRenderer>();sr.transform.SetParent(transform,false);
            sr.sprite=sprite;
            // LineRenderer writes texture properties; isolate sprite materials from its shared instance.
            var own=new Material(material);ownedMaterials.Add(own);sr.sharedMaterial=own;
            sr.sortingOrder=order;return sr;
        }
        private void OnDestroy(){foreach(var m in ownedMaterials)if(m!=null)Destroy(m);}
        private void LateUpdate()
        {
            if(torso==null)return;
            for(int i=0;i<6;i++)
            {
                var a=bones[i].GetPosition(0);var b=bones[i].GetPosition(1);var d=b-a;
                art[i].transform.localPosition=a;
                float angle=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
                art[i].transform.localRotation=Quaternion.Euler(0,0,angle-axes[i]);
                float sourceLength=i%3==0?.125f:i%3==1?.165f:i<3?.113f:.119f;
                art[i].transform.localScale=Vector3.one*d.magnitude/sourceLength;
                art[i].flipX=i==5;
                bones[i].enabled=showBones;
            }
            var hip=(bones[0].GetPosition(0)+bones[3].GetPosition(0))*.5f;
            torso.transform.localPosition=hip+new Vector3(0,.14f,0);
            belt.transform.localPosition=hip+new Vector3(0,.02f,0);
        }
    }
}
