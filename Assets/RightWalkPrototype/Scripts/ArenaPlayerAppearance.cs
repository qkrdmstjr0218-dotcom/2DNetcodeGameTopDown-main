using UnityEngine;
namespace RightWalkPrototype
{
    // HUD identity colors only. Character pixels/materials are deliberately untouched.
    public static class ArenaPlayerAppearance
    {
        public static Color Accent(int number)=>number switch {
            2=>new Color(1,.55f,.22f),3=>new Color(.3f,.68f,1),4=>new Color(.87f,.48f,1),_=>new Color(.3f,.9f,.55f)};
    }
}
