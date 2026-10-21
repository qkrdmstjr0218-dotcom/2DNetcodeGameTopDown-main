using UnityEngine;
namespace RightWalkPrototype
{
    // Cosmetic only: no collider, damage, camera shake, or gameplay time changes.
    public sealed class ArenaPatrolBurst:MonoBehaviour
    {
        ArenaObjectiveMatch match;float age;
        readonly SpriteRenderer[] pieces=new SpriteRenderer[9];
        readonly Vector2[] velocities=new Vector2[9];
        public void Initialize(ArenaObjectiveMatch owner,Sprite sprite)
        {
            match=owner;
            for(int i=0;i<pieces.Length;i++)
            {
                var go=new GameObject(i==0?"Flash":"Shard");go.transform.SetParent(transform,false);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=60;pieces[i]=sr;
                float angle=(i-1)*Mathf.PI*.25f;velocities[i]=i==0?Vector2.zero:new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*2.3f;
                go.transform.localScale=Vector3.one*(i==0?.3f:.1f);
            }
            Draw();
        }
        void Update()
        {
            if(match==null||!match.IsSpawned||match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Playing){Destroy(gameObject);return;}
            age+=Time.deltaTime;if(age>=.35f){Destroy(gameObject);return;}Draw();
        }
        void Draw()
        {
            float fade=1-age/.35f;
            for(int i=0;i<pieces.Length;i++)
            {
                pieces[i].transform.localPosition=(Vector3)(velocities[i]*age+Vector2.down*age*age*2);
                pieces[i].transform.localRotation=Quaternion.Euler(0,0,i*35+age*180);
                pieces[i].color=i==0?new Color(1,1,.8f,Mathf.Clamp01(1-age/.12f)):new Color(1,.55f,.15f,fade);
                pieces[i].transform.localScale=Vector3.one*(i==0?.3f*fade:.1f*fade);
            }
        }
    }
}
