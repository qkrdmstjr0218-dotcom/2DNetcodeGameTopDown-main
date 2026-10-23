using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class ArenaTutorial
    {
        static readonly string[] Titles={"이동과 달리기","더블점프","점선 발판 내려가기","일반탄과 자동충전","샷건의 사거리와 밀치기","라이플의 지형 관통","유탄과 자해 · 회복"};
        static readonly string[] Instructions={
            "A/D 또는 ←/→로 MOVE 패드를 지나세요. Shift를 누른 채 SPRINT 패드까지 달려보세요. 달리면 스태미나가 줄고, 쉬면 회복됩니다.",
            "Space로 점프한 뒤 공중에서 Space를 한 번 더 누르세요. 두 번째 점프를 사용해 오른쪽 높은 점선 발판 위에 올라가세요.",
            "발판 위에서 S/↓ 또는 왼쪽 Ctrl로 앉은 채 Space를 누르세요. 점선 발판 아래 바닥으로 내려갈 수 있습니다.",
            "우클릭을 떼고 좌클릭으로 표적을 파괴하세요. 사격을 멈추고 일반탄이 자동으로 가득 차는 것을 확인하세요.",
            "Q/E로 S를 선택하고 우클릭을 유지한 채 좌클릭하세요. 표적을 사거리 안에서 맞혀 오른쪽 구멍으로 밀어보세요.",
            "Q/E로 R을 선택하고 우클릭+좌클릭으로 상자 안 표적을 쏘세요. 일반탄은 벽에 막히지만 라이플은 지형을 관통합니다.",
            "Q/E로 G 선택 → 우클릭 유지 → 좌클릭. 예상 궤적으로 엄폐물 너머 표적을 파괴하세요. 가까운 폭발은 자신도 다칩니다. 자해 후 왼쪽 의료패드에서 회복해 보세요."
        };
        static Rect Card=>new Rect((Screen.width-Mathf.Min(660,Screen.width-32))*.5f,12,Mathf.Min(660,Screen.width-32),Active&&Instance.Lesson>=3&&Instance.Lesson<LessonCount?210:155);
        static Rect Navigation=>new Rect((Screen.width-Mathf.Min(660,Screen.width-32))*.5f,Screen.height-103,Mathf.Min(660,Screen.width-32),88);
        public static bool PointerOverUI(Vector2 point)
        {
            var gui=new Vector2(point.x,Screen.height-point.y);
            return Card.Contains(gui)||Navigation.Contains(gui)||new Rect(Screen.width-230,15,215,160).Contains(gui);
        }
        string ProgressText()
        {
            if(LessonComplete)return "목표 달성! 다음 구간으로 이동할 수 있습니다.";
            if(Lesson==0)return (walked?"이동 확인  ":"")+(sprinted?"달리기 확인":"Shift로 달려보세요.");
            if(Lesson==1)return doubleJump?"두 번째 점프 확인 · 높은 발판 위에 착지하세요.":"공중에서 Space를 한 번 더 누르세요.";
            if(Lesson==3&&Target!=null&&Target.Health.Value==0)return "사격을 멈추고 일반탄의 자동충전을 기다리세요.";
            if(Lesson==6)return (Target!=null&&Target.Health.Value==0?"표적 파괴 ✓  ":"표적 파괴 대기  ")+(selfHurt?"자해 확인 ✓  ":"가까운 폭발의 자해 확인 대기  ")+(healed&&player!=null&&player.Health==player.MaxHealth?"회복 ✓":"왼쪽 의료패드에서 회복");
            return "언제든 구간 재시도로 체력·탄약을 복구할 수 있습니다.";
        }
        void OnGUI()
        {
            if(!Active||player==null||!player.IsSpawned||RelayArenaPrototype.Instance==null||RelayArenaPrototype.Instance.MenuOpen)return;
            GUILayout.BeginArea(Card,GUI.skin.box);
            GUILayout.Label(Lesson<LessonCount?"튜토리얼 "+(Lesson+1)+" / "+LessonCount+" · "+Titles[Lesson]:"튜토리얼 둘러보기 완료",new GUIStyle(GUI.skin.label){fontSize=20,fontStyle=FontStyle.Bold});
            var wrap=new GUIStyle(GUI.skin.label){wordWrap=true,fontSize=15};
            GUILayout.Label(Lesson<LessonCount?Instructions[Lesson]:"직접 달성한 구간: "+CompletedCount+" / "+LessonCount+". 실제 맵의 혼자 연습으로 이어가거나, 이전 구간에 다시 도전할 수 있습니다.",wrap);
            if(Lesson>=3&&Lesson<LessonCount)GUILayout.Label("실제 경기에서 특수탄은 자동충전되지 않습니다. 보급이나 적 드롭으로 얻으세요. 여기와 대기방에서는 반복 연습을 위해 보충됩니다.",wrap);
            if(Lesson<LessonCount)GUILayout.Label(ProgressText(),wrap);
            if(Time.unscaledTime<noticeUntil)GUILayout.Label(notice,wrap);
            GUILayout.EndArea();
            GUILayout.BeginArea(Navigation,GUI.skin.box);GUILayout.BeginHorizontal();
            var enabled=GUI.enabled;GUI.enabled=Lesson>0;if(GUILayout.Button("이전",GUILayout.Height(30)))pendingLesson=Lesson-1;GUI.enabled=enabled;
            if(GUILayout.Button("구간 재시도",GUILayout.Height(30)))pendingLesson=Lesson;
            GUI.enabled=LessonComplete&&Lesson<LessonCount;if(GUILayout.Button("다음 구간",GUILayout.Height(30)))pendingLesson=Lesson+1;GUI.enabled=enabled;
            if(Lesson<LessonCount&&GUILayout.Button("건너뛰기",GUILayout.Height(30)))pendingLesson=Lesson+1;
            if(Lesson==LessonCount&&GUILayout.Button("실제 맵 혼자 연습",GUILayout.Height(30)))pendingExit=2;
            if(GUILayout.Button("로비로",GUILayout.Height(30)))pendingExit=1;
            GUILayout.EndHorizontal();GUILayout.Label("ESC: 메뉴 · 낙하 시 현재 구간으로 복귀 · 연습 중 체력은 최소 1 유지");GUILayout.EndArea();
        }
    }
}
