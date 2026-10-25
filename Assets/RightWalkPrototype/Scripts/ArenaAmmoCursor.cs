using UnityEngine;
namespace RightWalkPrototype
{
    public sealed class ArenaAmmoCursor
    {
        Material material;
        public bool Draw(Vector2 at,float ammo,int capacity,Color ink,bool empty,bool blocked,float wait)
        {
            var shader=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.ammoCursorShader:null;if(shader==null||!shader.isSupported)return false;
            if(material==null)material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            material.SetFloat("_Ammo",ammo);material.SetFloat("_Capacity",Mathf.Max(1,capacity));material.SetColor("_Ink",ink);
            material.SetFloat("_Empty",empty?1:0);material.SetFloat("_Blocked",blocked?1:0);material.SetFloat("_Wait",wait);
            Graphics.DrawTexture(new Rect(at.x-32,at.y-32,64,64),Texture2D.whiteTexture,material);return true;
        }
        public void Dispose(){if(material!=null)Object.Destroy(material);material=null;}
    }
}
