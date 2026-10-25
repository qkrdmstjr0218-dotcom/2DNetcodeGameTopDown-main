using UnityEngine;
namespace RightWalkPrototype
{
    // Small code-drawn instrument details, inside the existing HUD footprint.
    public static class ArenaHudSkin
    {
        public static readonly Color Frame=new(.23f,.36f,.42f,.9f);
        static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        static void Line(Vector2 a,Vector2 b,Color c,float thickness=1)
        {var old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(a,Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,-thickness/2,Vector2.Distance(a,b),thickness),c);GUI.matrix=old;}
        public static void Panel(Rect r,Color accent)
        {
            var dark=new Color(.025f,.055f,.075f,.94f);
            Fill(new Rect(r.x+4,r.y,r.width-8,r.height),dark);Fill(new Rect(r.x,r.y+4,r.width,r.height-8),dark);
            for(int i=1;i<4;i++){Fill(new Rect(r.x+4-i,r.y+i,r.width-8+2*i,1),dark);Fill(new Rect(r.x+4-i,r.yMax-i-1,r.width-8+2*i,1),dark);}
            Vector2 a=new(r.x+.5f,r.y+4),b=new(r.x+4,r.y+.5f),c=new(r.xMax-4,r.y+.5f),d=new(r.xMax-.5f,r.y+4);
            Line(a,b,Frame);Line(b,c,Frame);Line(c,d,Frame);Line(d,new Vector2(d.x,r.yMax-4),Frame);
            Line(new Vector2(d.x,r.yMax-4),new Vector2(r.xMax-4,r.yMax-.5f),Frame);Line(new Vector2(r.xMax-4,r.yMax-.5f),new Vector2(r.x+4,r.yMax-.5f),Frame);
            Line(new Vector2(r.x+4,r.yMax-.5f),new Vector2(a.x,r.yMax-4),Frame);Line(new Vector2(a.x,r.yMax-4),a,Frame);
            var muted=accent;muted.a=.65f;Fill(new Rect(r.x+9,r.y+1,Mathf.Min(32,r.width-18),1),muted);
        }
        public static void Meter(Rect r,float value,Color color,float ghost=0)
        {
            Fill(r,new Color(.14f,.23f,.28f,.95f));var inner=new Rect(r.x+1,r.y+1,r.width-2,Mathf.Max(1,r.height-2));Fill(inner,new Color(.025f,.065f,.09f,.98f));
            float amount=Mathf.Clamp01(value),trail=Mathf.Clamp01(ghost);
            if(trail>amount){var t=inner;t.width*=trail;Fill(t,new Color(.82f,.68f,.48f,.65f));}
            inner.width*=amount;if(inner.width<=0)return;Fill(inner,color);
            if(r.height>=6){var shine=Color.Lerp(color,Color.white,.28f);shine.a=color.a;Fill(new Rect(inner.x,inner.y,inner.width,1),shine);}
        }
        public static void AmmoGlyph(Rect r,int slot,Color color)
        {
            var old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(r.position,Quaternion.identity,new Vector3(r.width/20,r.height/20,1));
            if(slot==0||slot==2)
            {
                float x=slot==0?7:8,w=slot==0?6:4;
                Fill(new Rect(x,7,w,10),color);Fill(new Rect(x-1,17,w+2,2),color);
                Line(new Vector2(x,7),new Vector2(10,1),color,1.6f);Line(new Vector2(10,1),new Vector2(x+w,7),color,1.6f);
            }
            else if(slot==1)
            {
                for(int i=0;i<3;i++){float x=2+i*6;Fill(new Rect(x,5,4,12),color);Fill(new Rect(x-1,16,6,2),color);Fill(new Rect(x,3,4,1),Color.Lerp(color,Color.white,.35f));}
            }
            else
            {
                Vector2[] points={new(7,7),new(13,7),new(16,11),new(15,17),new(5,17),new(4,11),new(7,7)};
                for(int i=1;i<points.Length;i++)Line(points[i-1],points[i],color,1.7f);
                Fill(new Rect(8,3,5,3),color);Line(new Vector2(12,3),new Vector2(16,6),color,1.5f);
            }
            GUI.matrix=old;
        }
    }
}
