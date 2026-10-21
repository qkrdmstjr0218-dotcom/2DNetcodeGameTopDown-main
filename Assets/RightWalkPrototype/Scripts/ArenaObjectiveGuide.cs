using UnityEngine;

namespace RightWalkPrototype
{
    // Local presentation only: the existing server state decides which sites exist.
    public static class ArenaObjectiveGuide
    {
        public static readonly Color NextColor=new Color(1f,.76f,.3f);
        static readonly Color CurrentColor=new Color(.3f,.9f,1f);
        static GUIStyle caption;
        static SideScrollNetworkPlayer owner;

        public static void Draw(ArenaObjectiveMatch match)
        {
            var arena=RelayArenaPrototype.Instance;
            if(Event.current.type!=EventType.Repaint||arena==null||arena.MenuOpen||arena.overview||
                match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Playing)return;
            var camera=arena.arenaCamera;if(camera==null)return;
            if(owner==null||!owner.IsSpawned||!owner.IsOwner)
            {
                owner=null;
                foreach(var player in Object.FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                    if(player.IsSpawned&&player.IsOwner){owner=player;break;}
            }
            if(owner==null)return;
            if(caption==null)caption=new GUIStyle(GUI.skin.label){fontSize=13,alignment=TextAnchor.MiddleCenter,wordWrap=false};
            int index=match.State.Value.Point;
            Rect current=DrawSite(camera,match.Points[index],owner.transform.position,false,0,default);
            if(match.PreviewNext&&match.NextPoint!=index)
                DrawSite(camera,match.Points[match.NextPoint],owner.transform.position,true,
                    Mathf.Max(0,Mathf.CeilToInt((float)(match.State.Value.Switch-match.Now))),current);
        }

        static Rect DrawSite(Camera camera,ArenaObjectivePoint point,Vector3 origin,bool next,int seconds,Rect occupied)
        {
            Vector3 target=point.CaptureBounds.center;
            Vector3 projected=camera.WorldToScreenPoint(target);
            Vector2 pixel=new Vector2(projected.x,Screen.height-projected.y);
            Rect viewport=camera.pixelRect;viewport.y=Screen.height-viewport.yMax;
            // Leave the prototype's top HUD and bottom HP/ammo panels readable.
            float top=Mathf.Min(235,viewport.height*.3f),bottom=Mathf.Min(150,viewport.height*.22f);
            Rect safe=Rect.MinMaxRect(viewport.xMin+94,viewport.yMin+top,viewport.xMax-94,viewport.yMax-bottom);
            Vector2 centre=safe.center,direction=pixel-centre;
            if(projected.z<0)direction=-direction;
            bool outside=projected.z<0||!viewport.Contains(pixel);
            Vector2 anchor;
            if(outside)
            {
                if(direction.sqrMagnitude<.001f)direction=Vector2.down;
                float t=Mathf.Min(safe.width*.5f/Mathf.Max(.001f,Mathf.Abs(direction.x)),
                    safe.height*.5f/Mathf.Max(.001f,Mathf.Abs(direction.y)));
                anchor=centre+direction*t;
            }
            else
            {
                Vector3 above=camera.WorldToScreenPoint(new Vector3(target.x,point.CaptureBounds.max.y+.24f,target.z));
                anchor=new Vector2(Mathf.Clamp(above.x,safe.xMin,safe.xMax),Mathf.Clamp(Screen.height-above.y,safe.yMin,safe.yMax));
            }
            anchor=AvoidHud(anchor,viewport,bottom);
            Rect panel=new Rect(anchor.x-86,anchor.y+15,172,43);
            if(next&&panel.Overlaps(occupied))
            {
                float shift=anchor.y+112<viewport.yMax-bottom?72:-72;
                anchor.y+=shift;anchor=AvoidHud(anchor,viewport,bottom);panel.y=anchor.y+15;
            }
            Color tint=next?NextColor:CurrentColor;Color old=GUI.color;
            GUI.color=new Color(.035f,.065f,.08f,.9f);GUI.DrawTexture(panel,Texture2D.whiteTexture);
            GUI.color=tint;
            if(outside)
            {
                Vector2 forward=direction.normalized,side=new Vector2(-forward.y,forward.x);
                Line(anchor-forward*9,anchor+forward*9,2);
                Line(anchor+forward*9,anchor+side*6,2);Line(anchor+forward*9,anchor-side*6,2);
            }
            else
            {
                Line(anchor+Vector2.up*8,anchor+Vector2.right*8,2);Line(anchor+Vector2.right*8,anchor+Vector2.down*8,2);
                Line(anchor+Vector2.down*8,anchor+Vector2.left*8,2);Line(anchor+Vector2.left*8,anchor+Vector2.up*8,2);
            }
            caption.normal.textColor=tint;GUI.color=Color.white;
            float distance=Vector2.Distance(origin,point.transform.position);
            GUI.Label(panel,(next?"다음 · "+seconds+"초 후":"현재 해킹 지점")+"\n"+point.displayName+" · 거리 "+distance.ToString("0"),caption);
            GUI.color=old;return panel;
        }
        static Vector2 AvoidHud(Vector2 anchor,Rect viewport,float bottom)
        {
            float ceiling=anchor.x<viewport.xMin+330?330:anchor.x>viewport.xMax-240?350:0;
            anchor.y=Mathf.Max(anchor.y,viewport.yMin+Mathf.Min(ceiling,viewport.height-bottom-65));
            return anchor;
        }
        static void Line(Vector2 a,Vector2 b,float width)
        {
            Matrix4x4 old=GUI.matrix;Vector2 delta=b-a;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);
            GUI.DrawTexture(new Rect(a.x,a.y-width*.5f,delta.magnitude,width),Texture2D.whiteTexture);GUI.matrix=old;
        }
    }
}
