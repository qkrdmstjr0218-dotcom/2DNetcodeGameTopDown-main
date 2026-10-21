using UnityEngine;
namespace RightWalkPrototype
{
    // Visual landmark only: no collider and no change to capture/interaction rules.
    public sealed class ArenaHackingConsole : MonoBehaviour
    {
        public SpriteRenderer screen,status,glow;
        public void Show(bool active,bool next,Color captureColor)
        {
            Color on=active?captureColor:next?new Color(1,.65f,.2f):new Color(.14f,.23f,.27f);
            screen.color=on;status.color=on;
            var c=on;c.a=active?.12f:next?.06f:0;glow.color=c;
        }
    }
}
