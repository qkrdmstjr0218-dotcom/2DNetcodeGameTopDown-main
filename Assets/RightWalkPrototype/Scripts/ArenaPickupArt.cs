using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    public static class ArenaPickupArt
    {
        static readonly Dictionary<string,Sprite> sprites=new();
        public static bool Build(GameObject root,ArenaObjectiveMatch.AmmoPickup item,bool healing)
        {
            string kind=healing?item.Shield?"Shield":"Health":ArenaObjectiveMatch.SupplyInitial(item.Ammo);
            if(!sprites.TryGetValue(kind,out var sprite)){sprite=Resources.Load<Sprite>("FoundryArt/Pickup_"+kind);sprites[kind]=sprite;}
            if(sprite==null)return false;
            var go=new GameObject("Pickup artwork");go.transform.SetParent(root.transform,false);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=40;
            if(!healing)
            {
                var label=new GameObject("Ammo initial");label.transform.SetParent(root.transform,false);label.transform.localPosition=new Vector3(0,-.13f,0);
                var text=label.AddComponent<TextMesh>();text.text=kind;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=72;text.characterSize=.047f;text.anchor=TextAnchor.MiddleCenter;text.color=ArenaObjectiveMatch.SupplyColor(item.Ammo);
                var ink=label.GetComponent<MeshRenderer>();ink.sharedMaterial=text.font.material;ink.sortingOrder=43;
            }
            return true;
        }
    }
}
