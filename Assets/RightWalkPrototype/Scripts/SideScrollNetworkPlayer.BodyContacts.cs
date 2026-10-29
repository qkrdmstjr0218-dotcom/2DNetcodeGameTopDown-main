using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public const float ReadyIdentityHeight=2.85f;
        void IgnorePlayerBodyContacts()
        {
            var own=GetComponent<BoxCollider2D>();if(own==null)return;
            foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                if(p!=this&&p.IsSpawned&&p.TryGetComponent<BoxCollider2D>(out var other))Physics2D.IgnoreCollision(own,other,true);
        }
    }
}
