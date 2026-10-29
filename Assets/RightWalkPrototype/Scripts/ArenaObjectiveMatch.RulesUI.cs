using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        int rulesTab;
        public void OpenRules(){if(!PracticeRoom)return;markerGuide=true;markerGuideVersion=State.Value.Version;rulesTab=0;}
        public void CloseRules(){markerGuide=false;}
        void DrawRules()
        {
            int depth=GUI.depth;GUI.depth=-100;
            var color=GUI.color;GUI.color=new Color(0,.015f,.025f,.86f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=color;
            float w=Mathf.Min(720,Screen.width-40),h=Mathf.Min(510,Screen.height-40);
            var rect=new Rect((Screen.width-w)/2,(Screen.height-h)/2,w,h);GUI.Box(rect,GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x+24,rect.y+18,w-48,h-36));
            GUILayout.Label("규칙 설명",ArenaUiTheme.Title);GUILayout.Space(12);
            GUILayout.BeginHorizontal();string[] tabs={"경기 규칙","무기와 보급","위치 안내"};
            for(int i=0;i<tabs.Length;i++){bool old=GUI.enabled;GUI.enabled=rulesTab!=i;if(ArenaUiTheme.Button(tabs[i],GUILayout.Height(34)))rulesTab=i;GUI.enabled=old;}
            GUILayout.EndHorizontal();GUILayout.Space(18);
            var body=ArenaUiTheme.Static(new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true,richText=false});
            if(rulesTab==0)
            {
                GUILayout.Label("해킹 구역을 단독으로 점유하면 초당 1점",body);GUILayout.Space(12);
                GUILayout.Label("2명 이상이 들어오면 득점이 중단됩니다. 구역은 경기 중 다른 위치로 이동합니다.",body);GUILayout.Space(12);
                GUILayout.Label("플레이어 처치 +5점 · 지원 드론 격추 +5점\n경기 종료 시 최고 점수의 플레이어가 승리합니다.",body);GUILayout.Space(12);
                GUILayout.Label("사망하면 자동 부활합니다. 경기 시작·부활 후 2초 동안 보호되며, 사격하면 보호가 해제됩니다.",body);GUILayout.Space(12);
                GUILayout.Label("천장 위 주제 표적을 관통 라이플로 맞히면 모두가 보는 벽 안내가 바뀝니다. 이 규칙 창은 각자 독립적으로 볼 수 있습니다.",body);
            }
            else if(rulesTab==1)
            {
                GUILayout.Label("일반탄 · 최대 12발 / 사격을 멈춘 뒤 1초 후부터 초당 4발 자동 충전",body);GUILayout.Space(12);
                GUILayout.Label("특수탄 선택 · Q/E 또는 마우스 휠\n1 샷건 · 2 관통 라이플 · 3 유탄\n우클릭을 누르는 동안 선택한 특수탄을 발사합니다.",body);GUILayout.Space(12);
                GUILayout.Label("실제 경기의 특수탄은 보급 상자와 적이 떨어뜨린 아이템으로 보충합니다. 대기실에서는 연습을 위해 자동 보충됩니다.",body);GUILayout.Space(12);
                GUILayout.Label("지원 드론 · 비무장 보급 기체\n격추 시 5점과 보호막을 얻을 수 있습니다. 보호막은 최대 50이며 중첩되지 않습니다.",body);
            }
            else
            {
                GUILayoutUtility.GetRect(w-48,190);
                GUILayout.Space(14);GUILayout.Label("Tab을 누르면 전장 현황과 구역 위치를 볼 수 있습니다.",body);
            }
            GUILayout.FlexibleSpace();if(ArenaUiTheme.Button("닫기 · ESC",GUILayout.Height(38)))CloseRules();GUILayout.EndArea();
            if(rulesTab==2)ArenaCombatHud.DrawMarkerGuide(new Rect(rect.x+24,rect.y+132,w-48,190),false,16);
            GUI.depth=depth;
        }
    }
}
