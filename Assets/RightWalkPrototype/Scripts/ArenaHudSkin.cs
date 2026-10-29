using UnityEngine;
namespace RightWalkPrototype
{
    // Small code-drawn instrument details, inside the existing HUD footprint.
    public static class ArenaHudSkin
    {
        public static readonly Color Frame=new(.23f,.36f,.42f,.9f);
        static Texture2D readyCheck;
        public static void SelfPointer(Vector2 at)
        {
            for(int y=0;y<9;y++)Fill(new Rect(at.x-(9-y),at.y+y,2*(9-y),1),new Color(.015f,.03f,.04f,.95f));
            for(int y=0;y<6;y++)Fill(new Rect(at.x-(6-y),at.y+1+y,2*(6-y),1),new Color(.65f,.96f,1));
        }
        public static void ImpactMarker(Vector2 at,Color color,bool hit)
        {
            color.a=hit?.8f:.4f;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})
            {
                var corner=at+new Vector2(x*6,y*6);
                Fill(new Rect(corner.x-(x>0?3:0)-1,corner.y-1,5,3),new Color(.015f,.035f,.045f,.75f));
                Fill(new Rect(corner.x-1,corner.y-(y>0?3:0)-1,3,5),new Color(.015f,.035f,.045f,.75f));
                Fill(new Rect(corner.x-(x>0?3:0),corner.y,4,1),color);
                Fill(new Rect(corner.x,corner.y-(y>0?3:0),1,4),color);
            }
            if(hit)Fill(new Rect(at.x-1,at.y-1,2,2),color);
        }
        static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        static void Line(Vector2 a,Vector2 b,Color c,float thickness=1)
        {var old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(a,Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);Fill(new Rect(0,-thickness/2,Vector2.Distance(a,b),thickness),c);GUI.matrix=old;}
        public static void ReadyBadge(Rect r,bool ready)
        {
            var accent=ready?new Color(.42f,.93f,.72f):new Color(.57f,.67f,.72f);
            // Rects and a single AA icon also work inside GUILayout clipping groups.
            Fill(new Rect(r.x+3,r.y,r.width-6,r.height),new Color(.025f,.055f,.075f,.94f));
            Fill(new Rect(r.x,r.y+3,r.width,r.height-6),new Color(.025f,.055f,.075f,.94f));
            Fill(new Rect(r.x+3,r.y,r.width-6,1),Frame);Fill(new Rect(r.x+3,r.yMax-1,r.width-6,1),Frame);
            Fill(new Rect(r.x,r.y+3,1,r.height-6),Frame);Fill(new Rect(r.xMax-1,r.y+3,1,r.height-6),Frame);
            for(int i=0;i<3;i++){Fill(new Rect(r.x+2-i,r.y+i,1,1),Frame);Fill(new Rect(r.xMax-3+i,r.y+i,1,1),Frame);Fill(new Rect(r.x+2-i,r.yMax-i-1,1,1),Frame);Fill(new Rect(r.xMax-3+i,r.yMax-i-1,1,1),Frame);}
            Fill(new Rect(r.x+1,r.y+6,2,r.height-12),accent);
            float x=r.x+12,y=r.center.y;
            if(ready)
            {
                if(readyCheck==null)
                {
                    readyCheck=new Texture2D(32,32,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                    var pixels=new Color[1024];Vector2 a=new(5,16),b=new(12,9),c=new(27,24);
                    float Distance(Vector2 p,Vector2 from,Vector2 to){var d=to-from;return Vector2.Distance(p,from+d*Mathf.Clamp01(Vector2.Dot(p-from,d)/d.sqrMagnitude));}
                    for(int j=0;j<32;j++)for(int i=0;i<32;i++){var p=new Vector2(i+.5f,j+.5f);pixels[j*32+i]=new Color(1,1,1,Mathf.Clamp01(2.1f-Mathf.Min(Distance(p,a,b),Distance(p,b,c))));}
                    readyCheck.SetPixels(pixels);readyCheck.Apply();
                }
                var old=GUI.color;GUI.color=accent;GUI.DrawTexture(new Rect(x-7,y-7,14,14),readyCheck);GUI.color=old;
            }
            else Fill(new Rect(x-3,y-1,8,2),accent);
            GUI.Label(new Rect(r.x+25,r.y,r.width-29,r.height),ready?"준비 완료":"준비 전",ArenaUiTheme.Static(new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=13,padding=new RectOffset(),normal={textColor=accent}}));
        }
        public static void PlayerBadge(Rect r,int number)
        {
            var accent=ArenaPlayerAppearance.Accent(number);Fill(r,new Color(.025f,.055f,.075f,.96f));
            var edge=accent;edge.a=.5f;
            Fill(new Rect(r.x,r.y,r.width,1),edge);Fill(new Rect(r.x,r.yMax-1,r.width,1),edge);
            Fill(new Rect(r.x,r.y,1,r.height),edge);Fill(new Rect(r.xMax-1,r.y,1,r.height),edge);
            GUI.Label(r,"P"+number,ArenaUiTheme.Static(new GUIStyle(GUI.skin.label){fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset(),normal={textColor=accent}}));
        }
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
                for(int i=0;i<3;i++){float x=2+i*6;Fill(new Rect(x,5,4,12),color);Fill(new Rect(x-1,16,6,2),color);var highlight=Color.Lerp(color,Color.white,.35f);highlight.a=color.a;Fill(new Rect(x,3,4,1),highlight);}
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
