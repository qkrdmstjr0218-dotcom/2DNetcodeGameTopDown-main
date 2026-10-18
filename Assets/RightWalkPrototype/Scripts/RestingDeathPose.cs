using UnityEngine;

namespace RightWalkPrototype
{
    internal sealed class RestingDeathPose
    {
        private readonly Transform owner,headAnchor;
        private readonly Renderer[] native;
        private readonly SpriteRenderer body;
        private readonly Sprite bodySprite;
        private static readonly Vector2[] outline={new Vector2(32,649),new Vector2(80,691),new Vector2(360,698),new Vector2(640,703),new Vector2(1176,692),new Vector2(1688,675),new Vector2(1728,659),new Vector2(1740,645),new Vector2(1740,583)};
        private readonly SpriteRenderer gun,heldWeapon;
        private readonly Sprite gunSprite;
        private readonly Vector3 gunScale;
        private Vector3 releasePosition;
        private Quaternion releaseRotation;
        private bool released;
        public bool Ready => body!=null&&gun!=null&&heldWeapon!=null;
        public RestingDeathPose(Transform owner,Sprite reference,Sprite weapon,SpriteRenderer held,Transform headAnchor)
        {
            this.owner=owner;this.headAnchor=headAnchor;heldWeapon=held;native=owner.GetComponentsInChildren<Renderer>(true);
            if(reference==null||weapon==null||held==null)return;
            var texture=reference.texture;
            bodySprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(930f/texture.width,(texture.height-510f)/texture.height),650f,0,SpriteMeshType.FullRect);
            bodySprite.name="Coherent resting original at matched scale";
            body=Make("Death coherent resting body",bodySprite,30);
            // Keep actual handless RGB: a mask over the held image kept finger
            // colours wherever they overlapped the original rifle silhouette.
            var gunTexture=weapon.texture;
            gunSprite=Sprite.Create(gunTexture,new Rect(270,gunTexture.height-170,196,52),new Vector2(.5f,.5f),held.sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);
            gunSprite.name="Original loose rifle pixels";gun=Make("Death original loose rifle",gunSprite,31);
            var heldScale=held.transform.lossyScale;var ownerScale=owner.lossyScale;
            gunScale=new Vector3(heldScale.x/ownerScale.x,heldScale.y/ownerScale.y,1);gun.transform.localScale=gunScale;
        }
        private SpriteRenderer Make(string name,Sprite sprite,int order)
        {
            var go=new GameObject(name);go.hideFlags=HideFlags.DontSave;go.transform.SetParent(owner,false);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order;r.color=new Color(1,1,1,0);return r;
        }
        public void RestoreNative(){foreach(var r in native)r.forceRenderingOff=false;}
        private float Bottom(Vector3 position,Quaternion rotation)
        {
            float lowest=float.PositiveInfinity;
            foreach(var p in outline)lowest=Mathf.Min(lowest,(position+rotation*new Vector3((p.x-930)/650f,(510-p.y)/650f,0)).y);
            return lowest;
        }
        public void MatchHeadToLanding(float progress,float floor,Vector3 neckPoint,Vector3 drawingNeck)
        {
            if(headAnchor==null||progress<.45f)return;
            var original=headAnchor.localRotation;
            for(int i=0;i<20;i++)
            {
                var position=headAnchor.localPosition-headAnchor.localRotation*new Vector3((570f-930)/650f,(510f-550)/650f,0);
                if(Bottom(position,headAnchor.localRotation)>=floor)break;
                headAnchor.localRotation*=Quaternion.Euler(0,0,1);
                headAnchor.localPosition=neckPoint-headAnchor.localRotation*drawingNeck;
            }
            headAnchor.localRotation=Quaternion.Slerp(original,headAnchor.localRotation,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.62f,progress)));
            headAnchor.localPosition=neckPoint-headAnchor.localRotation*drawingNeck;
        }
        public void Apply(float progress,float floor,bool onFloor)
        {
            if(!Ready)return;
            // Opaque handover. Never superimpose two differently posed bodies.
            bool useBody=progress>=.62f;
            body.enabled=useBody;
            if(useBody)
            {
                float settle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,1f,progress));
                var rotation=headAnchor.localRotation;
                var position=headAnchor.localPosition-rotation*new Vector3((570f-930)/650f,(510f-550)/650f,0);
                position=Vector3.Lerp(position,new Vector3(-.65f,floor+(703f-510)/650f,0),settle);
                rotation=Quaternion.Slerp(rotation,Quaternion.identity,settle);
                if(onFloor)position.y+=Mathf.Max(0,floor-Bottom(position,rotation));
                body.transform.localPosition=position;body.transform.localRotation=rotation;body.color=Color.white;
                foreach(var r in native)r.forceRenderingOff=true;
            }
            if(progress<.62f){gun.color=new Color(1,1,1,0);return;}
            if(!released)
            {
                var sprite=heldWeapon.sprite;
                var localCenter=new Vector3((368f-sprite.pivot.x)/sprite.pixelsPerUnit,(sprite.texture.height-144f-sprite.pivot.y)/sprite.pixelsPerUnit,0);
                releasePosition=owner.InverseTransformPoint(heldWeapon.transform.TransformPoint(localCenter));
                releaseRotation=Quaternion.Inverse(owner.rotation)*heldWeapon.transform.rotation;released=true;
            }
            heldWeapon.forceRenderingOff=true;
            float t=Mathf.InverseLerp(.62f,1f,progress),e=Mathf.SmoothStep(0,1,t);
            var end=new Vector3(-.25f,floor+26f/gunSprite.pixelsPerUnit*Mathf.Abs(gunScale.y)+.008f,0);
            var gunPosition=Vector3.Lerp(releasePosition,end,e);gunPosition.y=Mathf.Lerp(releasePosition.y,end.y,t*t);
            gun.transform.localPosition=gunPosition;gun.transform.localRotation=Quaternion.Slerp(releaseRotation,Quaternion.identity,e);gun.color=Color.white;
            float minY=float.PositiveInfinity;var b=gunSprite.bounds;
            foreach(float x in new[]{b.min.x,b.max.x})foreach(float y in new[]{b.min.y,b.max.y})minY=Mathf.Min(minY,owner.InverseTransformPoint(gun.transform.TransformPoint(new Vector3(x,y,0))).y);
            if(minY<floor)gun.transform.localPosition+=Vector3.up*(floor-minY);
        }
        public void Dispose(){if(bodySprite!=null)Object.Destroy(bodySprite);if(gunSprite!=null)Object.Destroy(gunSprite);}
    }
}
