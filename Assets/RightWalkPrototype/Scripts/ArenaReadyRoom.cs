using UnityEngine;

namespace RightWalkPrototype
{
    // Both areas coexist so the existing NGO connection need not be rebuilt.
    // A new life generation teleports every owner when the server changes phase.
    public static class ArenaReadyRoom
    {
        public static Vector3 Spawn(int number)=>new Vector3(100+(Mathf.Clamp(number,1,4)-2.5f)*3,.04f,0);
        public static void Build(Transform parent)
        {
            var root=new GameObject("Ready room").transform;root.SetParent(parent,false);
            var sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            GameObject Box(string name,float x,float y,float w,float h,Color c,int order,bool solid=false)
            {
                var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=new Vector3(100+x,y,0);go.transform.localScale=new Vector3(w,h,1);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=c;sr.sortingOrder=order;
                if(solid)go.AddComponent<BoxCollider2D>();
                return go;
            }
            Color steel=new Color(.19f,.29f,.35f),cyan=new Color(.25f,.8f,.76f);
            Box("Back wall",0,4,28,20,new Color(.055f,.085f,.12f),-100);
            for(int i=-2;i<=2;i++){Box("Wall bay",i*4,4.1f,3.8f,7.6f,new Color(.08f,.14f,.18f),-95);Box("Wall vent",i*4,7,.35f,.3f,new Color(.14f,.23f,.28f),-90);}
            Box("Floor",0,-.25f,20,.5f,steel,0,true);Box("Floor edge",0,0,20,.035f,new Color(.96f,.63f,.23f),1);
            Box("West wall",-10.2f,4,.4f,8.5f,steel,0,true);Box("East wall",10.2f,4,.4f,8.5f,steel,0,true);
            Box("Ceiling",0,9,21,.3f,steel,0,true);
            foreach(float x in new[]{-7f,7f})
            {
                for(int level=0;level<3;level++)
                {
                    float lx=level==1?Mathf.Sign(x)*5.5f:x,y=1.5f+level*1.5f;
                    var ledge=Box("Jump ledge "+(x<0?"West ":"East ")+(level+1),lx,y,3,.16f,steel,0,true);
                    var effector=ledge.AddComponent<PlatformEffector2D>();effector.useOneWay=true;effector.useOneWayGrouping=true;effector.surfaceArc=180;ledge.GetComponent<BoxCollider2D>().usedByEffector=true;
                    for(int i=0;i<10;i++)Box("Ledge dash",lx-1.5f+i*.3f+.1f,y+.085f,.2f,.035f,cyan,1).transform.SetParent(ledge.transform,true);
                }
            }
            for(int i=1;i<=4;i++)Box("Arrival pad",Spawn(i).x-100,-.15f,1.5f,.06f,new Color(.27f,.4f,.46f),2);
            var title=new GameObject("Ready room title");title.transform.SetParent(root,false);title.transform.position=new Vector3(100,5.9f,0);
            var text=title.AddComponent<TextMesh>();text.text="RELAY / READY ROOM";text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=64;text.characterSize=.055f;text.color=new Color(.35f,.55f,.6f);
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");title.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;title.GetComponent<MeshRenderer>().sortingOrder=-80;
        }
    }
}
