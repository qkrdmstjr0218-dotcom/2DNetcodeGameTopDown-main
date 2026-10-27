using UnityEngine;
namespace RightWalkPrototype
{
    // Presentation only. Indeterminate activity is never shown as load percentage.
    public static class ArenaTransitionHud
    {
        static readonly Color Cyan=new(.48f,.8f,.85f),Muted=new(.52f,.66f,.7f);
        static void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        static GUIStyle Label(int size,Color color)=>ArenaUiTheme.Static(new GUIStyle(GUI.skin.label){fontSize=size,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(0,0,0,0),normal={textColor=color}});
        static void Activity(Rect rect)
        {
            int lit=Mathf.FloorToInt(Time.unscaledTime*3)%4;
            for(int i=0;i<4;i++)Fill(new Rect(rect.x+i*(rect.width+5)/4,rect.y,(rect.width-15)/4,3),i==lit?Cyan:new Color(.15f,.26f,.3f));
        }
        public static void Transition(bool returning)
        {
            int depth=GUI.depth;GUI.depth=-100;
            Fill(new Rect(0,0,Screen.width,Screen.height),new Color(.025f,.045f,.06f));
            using(ArenaUiTheme.Use())
            {
                var panel=new Rect((Screen.width-Mathf.Min(480,Screen.width-40))/2,Screen.height*.5f-90,Mathf.Min(480,Screen.width-40),180);
                ArenaHudSkin.Panel(panel,Cyan);
                GUI.Label(new Rect(panel.x+20,panel.y+24,panel.width-40,40),returning?"대기방 복귀":"전장 진입",Label(28,new Color(.88f,.93f,.9f)));
                GUI.Label(new Rect(panel.x+20,panel.y+75,panel.width-40,30),returning?"대기방으로 돌아가는 중":"전투 지역으로 이동 중",Label(16,Muted));
                Activity(new Rect(panel.center.x-62,panel.y+139,124,3));
            }
            GUI.depth=depth;
        }
        public static void Countdown(double end,double now)
        {
            using(ArenaUiTheme.Use())
            {
                bool waiting=end<=0;float width=waiting?340:176;
                var panel=new Rect((Screen.width-width)/2,Screen.height*.5f-92,width,176);ArenaHudSkin.Panel(panel,Cyan);
                GUI.Label(new Rect(panel.x+12,panel.y+14,width-24,24),"출격 준비",Label(15,Muted));
                int count=Mathf.Clamp(Mathf.CeilToInt((float)(end-now)),1,99);
                GUI.Label(new Rect(panel.x+12,panel.y+43,width-24,87),waiting?"참가자 대기":count.ToString(),Label(waiting?26:72,new Color(.89f,.95f,.93f)));
                if(waiting)Activity(new Rect(panel.center.x-45,panel.y+148,90,3));
                else for(int i=0;i<3;i++)Fill(new Rect(panel.center.x-41+i*30,panel.y+148,22,3),i<Mathf.Min(3,count)?Cyan:new Color(.14f,.24f,.28f));
            }
        }
        public static void Go()
        {
            using(ArenaUiTheme.Use())
            {
                var rect=new Rect(Screen.width*.5f-100,Screen.height*.5f-42,200,84);ArenaHudSkin.Panel(rect,Cyan);
                GUI.Label(rect,"전투 개시",Label(28,new Color(.9f,.95f,.88f)));
            }
        }
        public static void RoundEnd()
        {
            using(ArenaUiTheme.Use())
            {
                var rect=new Rect(Screen.width*.5f-180,Screen.height*.5f-58,360,116);
                ArenaHudSkin.Panel(rect,Cyan);
                GUI.Label(new Rect(rect.x,rect.y+10,rect.width,60),"경기 종료",Label(38,new Color(.9f,.95f,.88f)));
                GUI.Label(new Rect(rect.x,rect.y+76,rect.width,24),"결과를 확인합니다",Label(15,Muted));
            }
        }
        public static void Connection()
        {
            var rect=GUILayoutUtility.GetRect(0,10000,32,32,GUILayout.ExpandWidth(true));GUI.Label(new Rect(rect.x,rect.y,rect.width-88,27),"연결 중 · 잠시 기다려 주세요",ArenaUiTheme.Muted);
            Activity(new Rect(rect.xMax-80,rect.y+12,72,3));
        }
    }
}
