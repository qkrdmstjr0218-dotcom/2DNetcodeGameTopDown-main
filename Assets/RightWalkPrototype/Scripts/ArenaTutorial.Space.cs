using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaTutorial
    {
        static readonly Color Steel=new(.19f,.29f,.35f),Cyan=new(.25f,.8f,.76f),Amber=new(.96f,.63f,.23f);
        Collider2D goalLedge;
        void BuildLesson()
        {
            geometry=new GameObject("Offline tutorial lesson "+Lesson);geometry.transform.SetParent(transform,false);
            sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            Box("Training backdrop",205,4,30,22,new Color(.055f,.085f,.12f),-100);
            for(int i=0;i<5;i++)Box("Wall bay",196+i*4,4,3.8f,8,new Color(.08f,.14f,.18f),-95);
            Box("West wall",193.8f,4,.4f,12,Steel,0,true);Box("East wall",216.2f,4,.4f,12,Steel,0,true);
            if(Lesson==4){Floor(194,209);Floor(213,216);Label("FALL / RESET",211,-1.5f,Amber);}
            else Floor(194,216);
            if(Lesson==0){Beacon(204,"MOVE",Cyan);Beacon(210,"SPRINT",Amber);}
            if(Lesson==1||Lesson==2)
            {
                Ledge(196.5f,1.2f,2);goalLedge=Ledge(208.5f,3.2f,4);Label("DOUBLE JUMP",208.5f,4,Cyan);
            }
            if(Lesson==3)Label("NORMAL / AUTO RECHARGE",206,3,Cyan);
            if(Lesson==4){Label("S / PUSH",207,3,Amber);Box("Pit warning",209,.08f,.18f,.12f,Amber,4);}
            if(Lesson==5)
            {
                Box("Solid rifle enclosure",207.5f,1.6f,4,3.2f,Steel,2,true);
                Box("Enclosure face",207.5f,1.6f,3.7f,2.9f,new Color(.11f,.19f,.23f),3);
                Box("Solid top edge",207.5f,3.2f,4,.04f,Amber,4);
                Label("R / THROUGH TERRAIN",207.5f,3.7f,new Color(.8f,.5f,1));
            }
            if(Lesson==6)
            {
                Box("Grenade cover",204,1,.7f,2,Steel,2,true);Box("Cover solid edge",204,2,.7f,.04f,Amber,3);
                Label("G / ARC & BLAST",207,3,new Color(.65f,1,.3f));
                Box("Medical station",198,.8f,1,1.6f,new Color(.12f,.3f,.24f),1);
                Box("Medical cross vertical",198,1.1f,.12f,.55f,new Color(.3f,1,.6f),2);
                Box("Medical cross horizontal",198,1.1f,.55f,.12f,new Color(.3f,1,.6f),2);
                Box("Medical pad",198,.03f,1.8f,.05f,new Color(.3f,1,.6f),3);Label("MED / +25 HP",198,3.1f,new Color(.3f,1,.6f));
            }
            if(Lesson==LessonCount)Label("READY FOR THE ARENA",205,3,Cyan);
        }
        GameObject Box(string name,float x,float y,float w,float h,Color color,int order,bool solid=false)
        {
            var go=new GameObject(name);go.transform.SetParent(geometry.transform,false);go.transform.position=new Vector3(x,y,0);go.transform.localScale=new Vector3(w,h,1);
            var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.color=color;r.sortingOrder=order;if(solid)go.AddComponent<BoxCollider2D>();return go;
        }
        void Floor(float left,float right){Box("Tutorial floor",(left+right)*.5f,-.25f,right-left,.5f,Steel,0,true);Box("Solid floor edge",(left+right)*.5f,0,right-left,.035f,Amber,1);}
        Collider2D Ledge(float x,float y,float width)
        {
            var go=Box("Tutorial one-way ledge",x,y-.08f,width,.16f,Steel,0,true);
            var effector=go.AddComponent<PlatformEffector2D>();effector.useOneWay=true;effector.useOneWayGrouping=true;effector.surfaceArc=180;go.GetComponent<BoxCollider2D>().usedByEffector=true;
            for(float at=x-width*.5f;at<x+width*.5f;at+=.3f)Box("Passable dash",at+.1f,y+.01f,.2f,.035f,Cyan,1);
            return go.GetComponent<Collider2D>();
        }
        void Label(string value,float x,float y,Color color)
        {
            var go=new GameObject(value);go.transform.SetParent(geometry.transform,false);go.transform.position=new Vector3(x,y,0);
            var text=go.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text=value;text.anchor=TextAnchor.MiddleCenter;text.fontSize=48;text.characterSize=.055f;text.color=color;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=text.font.material;renderer.sortingOrder=-20;
        }
        void Beacon(float x,string title,Color color){Box(title+" pad",x,.04f,1,.05f,color,2);Label(title,x,.6f,color);}
    }
}
