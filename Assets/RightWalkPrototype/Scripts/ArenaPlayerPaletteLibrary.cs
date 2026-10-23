using System;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed class ArenaPlayerPaletteLibrary : ScriptableObject
    {
        [Serializable] public class Entry {public Texture2D source,p2,p3,p4;}
        public Entry[] entries;
        public Shader spriteShader;
        public Texture2D Variant(Texture source,int number)
        {
            if(number<2||number>4||entries==null)return null;
            foreach(var e in entries)if(e.source==source)return number==2?e.p2:number==3?e.p3:e.p4;
            return null;
        }
    }
}
