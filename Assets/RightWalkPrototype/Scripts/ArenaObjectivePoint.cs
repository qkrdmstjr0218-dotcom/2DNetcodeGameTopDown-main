using UnityEngine;
namespace RightWalkPrototype
{
    public sealed class ArenaObjectivePoint : MonoBehaviour
    {
        public string displayName;
        public float halfWidth=1.4f;
        public SpriteRenderer[] area;
        public TextMesh label;
        public ArenaHackingConsole console;
        // The fill is the visible capture area. Test the centre of the feet,
        // allowing the rest of the character to extend beyond its outline.
        public Bounds CaptureBounds=>area!=null&&area.Length>0&&area[0]!=null?area[0].bounds:
            new Bounds(transform.position+Vector3.up*1.175f,new Vector3(halfWidth*2,2.35f,.1f));
        public bool Contains(SideScrollNetworkPlayer player)
        {
            if(!player.IsSpawned||player.IsDead||!player.HasCurrentLifePose)return false;
            var movement=player.GetComponent<SideScrollMapPreview>();
            var feet=player.GetComponent<BoxCollider2D>().bounds.min;
            var region=CaptureBounds;float x=player.transform.position.x;
            return movement.Grounded&&x>=region.min.x&&x<=region.max.x&&feet.y>=region.min.y-.05f&&feet.y<=region.max.y;
        }
        public void Show(bool active,bool next,Color color)
        {
            if(console!=null)console.Show(active,next,color);
            if(next&&!active)color=ArenaObjectiveGuide.NextColor;
            foreach(var r in area)if(r!=null){r.enabled=active||next;var c=color;c.a=r.name=="Zone fill"?(active?.07f:.025f):(active?.8f:.3f);r.color=c;}
            if(label!=null){label.gameObject.SetActive(active||next);label.text=active?"UPLINK":"NEXT";label.color=color;}
        }
    }
}
