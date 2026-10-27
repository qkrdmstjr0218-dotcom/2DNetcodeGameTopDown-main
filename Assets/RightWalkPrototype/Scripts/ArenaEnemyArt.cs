using System.Collections.Generic;
using UnityEngine;
namespace RightWalkPrototype
{
    // Presentation only. The server keeps its existing hit bounds and .68/.6 gun origin.
    public static class ArenaEnemyArt
    {
        static readonly Dictionary<string,Sprite> sprites=new();
        static Material material;static MaterialPropertyBlock flashBlock;
        public static void SetFlash(List<SpriteRenderer> parts,bool active)
        {
            if(flashBlock==null)flashBlock=new MaterialPropertyBlock();
            foreach(var part in parts){part.GetPropertyBlock(flashBlock);flashBlock.SetFloat("_HitFlash",active?1:0);part.SetPropertyBlock(flashBlock);}
        }
        public static void SetEntryFloor(List<SpriteRenderer> parts,float floor)
        {
            if(flashBlock==null)flashBlock=new MaterialPropertyBlock();
            foreach(var part in parts){part.GetPropertyBlock(flashBlock);flashBlock.SetFloat("_EntryClipY",floor);part.SetPropertyBlock(flashBlock);}
        }
        static Sprite Load(string name)
        {
            if(!sprites.TryGetValue(name,out var sprite)){sprite=Resources.Load<Sprite>("FoundryArt/"+name);sprites[name]=sprite;}
            return sprite;
        }
        static void BindMaterial(SpriteRenderer renderer)
        {
            if(material==null)return;renderer.sharedMaterial=material;
            var block=new MaterialPropertyBlock();block.SetTexture("_MainTex",renderer.sprite.texture);renderer.SetPropertyBlock(block);
        }
        public static bool BuildSupport(Transform root,List<SpriteRenderer> parts,List<Color> colors)
        {
            if(Load("SupportHull")==null||Load("SupportCargo")==null)return false;
            if(material==null){var shader=Resources.Load<Shader>("FoundryArt/EnemySprite");if(shader!=null)material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};}
            foreach(string name in new[]{"SupportHull","SupportCargo"})
            {
                var go=new GameObject(name);go.transform.SetParent(root,false);var r=go.AddComponent<SpriteRenderer>();r.sprite=Load(name);r.sortingOrder=name=="SupportHull"?12:11;
                BindMaterial(r);parts.Add(r);colors.Add(Color.white);
            }
            return true;
        }
        public static bool Build(Transform root,bool turret,bool flying,List<SpriteRenderer> parts,List<Color> colors,
            out Transform barrel,out SpriteRenderer sensor,out SpriteRenderer rush,out Transform facing)
        {
            barrel=null;sensor=null;rush=null;facing=null;
            string main=turret?"TurretBase":flying?"DroneHull":"GroundHull";
            if(Load(main)==null)return false;
            if(material==null){var shader=Resources.Load<Shader>("FoundryArt/EnemySprite");if(shader!=null)material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};}
            SpriteRenderer Add(Transform parent,string name,int order)
            {
                var sprite=Load(name);if(sprite==null)return null;
                var go=new GameObject(name);go.transform.SetParent(parent,false);
                var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;
                BindMaterial(r);
                parts.Add(r);colors.Add(Color.white);return r;
            }
            var graphic=new GameObject("Security unit artwork").transform;graphic.SetParent(root,false);
            Add(graphic,main,10);
            if(turret||flying)
            {
                barrel=new GameObject("Aimed barrel").transform;barrel.SetParent(root,false);barrel.localPosition=Vector2.up*.68f;
                Add(barrel,turret?"TurretBarrel":"DroneBarrel",12);
                sensor=Add(turret?barrel:graphic,turret?"TurretSensor":"DroneSensor",13);
            }
            else
            {
                facing=graphic;sensor=Add(graphic,"GroundSensor",13);
                sensor.color=new Color(1,.32f,.12f);colors[parts.IndexOf(sensor)]=sensor.color;
                // Charge light is mounted on the front bumper and mirrors with the hull.
                rush=Add(graphic,"GroundCharge",14);rush.gameObject.SetActive(false);
            }
            return true;
        }
    }
}
