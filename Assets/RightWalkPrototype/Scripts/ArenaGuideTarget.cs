using UnityEngine;
namespace RightWalkPrototype
{
    // Virtual shot target: never adds a movement collider to the ready room.
    public sealed class ArenaGuideTarget:MonoBehaviour
    {
        public int Topic{get;private set;}
        SpriteRenderer lamp;
        float flashUntil;
        public Bounds HitBounds=>new Bounds(transform.position,new Vector3(2.92f*Mathf.Abs(transform.lossyScale.x),.60f*Mathf.Abs(transform.lossyScale.y),.5f));
        internal void Initialize(int topic,SpriteRenderer light){Topic=topic;lamp=light;}
        internal void Show(bool selected,bool flash)
        {
            if(flash)flashUntil=Time.time+.22f;
            if(lamp!=null)lamp.color=Time.time<flashUntil?new Color(.9f,1,1):selected?new Color(.3f,.95f,.83f):new Color(.24f,.36f,.41f);
        }
    }
}
