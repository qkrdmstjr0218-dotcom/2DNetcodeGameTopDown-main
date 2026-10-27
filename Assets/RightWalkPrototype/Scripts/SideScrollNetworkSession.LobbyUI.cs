using Unity.Services.Multiplayer;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkSession
    {
        string nicknameDraft;
        string onlineRoomDraft="해킹 점령전";Vector2 roomScroll;bool localTools;
        static string RoomProperty(ISessionInfo room,string key,string fallback)
        {return room.Properties!=null&&room.Properties.TryGetValue(key,out var value)?value.Value:fallback;}
        public static bool OnlineRoomJoinable(ISessionInfo room)=>room!=null&&!room.IsLocked&&!room.HasPassword&&room.AvailableSlots>0&&RoomProperty(room,"phase","waiting")=="waiting";
        void DrawOnlineLobby()
        {
            bool enabled=GUI.enabled;
            GUI.enabled=enabled&&(RelayArenaPrototype.Instance==null||!RelayArenaPrototype.Instance.MenuOpen);
            using(ArenaUiTheme.Use())DrawStyledLobby();
            GUI.enabled=enabled;
            // Keep backdrop and popup in one draw pass, independent of component callback order.
            if(RelayArenaPrototype.Instance!=null&&RelayArenaPrototype.Instance.MenuOpen)RelayArenaPrototype.Instance.DrawGameMenu();
        }
        void DrawStyledLobby()
        {
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            var old=GUI.color;GUI.color=new Color(.035f,.055f,.075f,.98f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            float width=Mathf.Min(820,Screen.width-32),height=Mathf.Min(650,Screen.height-32);
            GUILayout.BeginArea(new Rect((Screen.width-width)*.5f,(Screen.height-height)*.5f,width,height),GUI.skin.box);
            var arena=RelayArenaPrototype.Instance;
            GUILayout.BeginHorizontal();GUILayout.Label("RELAY / ARENA",ArenaUiTheme.Title);GUILayout.FlexibleSpace();
            if(ArenaUiTheme.Button("설정",GUILayout.Width(80),GUILayout.Height(34)))arena.SetMenuOpen(true);
            if(ArenaUiTheme.Button("게임 종료",GUILayout.Width(105),GUILayout.Height(34)))arena.RequestQuit();
            GUILayout.EndHorizontal();
            GUILayout.Label("접속할 방을 선택하거나 직접 만들어 보세요.",ArenaUiTheme.Muted);GUILayout.Space(8);
            nicknameDraft??=ArenaPlayerNames.Local;
            GUILayout.BeginHorizontal();GUILayout.Label("닉네임",GUILayout.Width(60));
            string previousNicknameDraft=nicknameDraft;
            GUI.SetNextControlName("Arena.Nickname");
            nicknameDraft=GUILayout.TextField(nicknameDraft,48,GUILayout.MaxWidth(300));
            if(nicknameDraft!=previousNicknameDraft)nicknameSavedUntil=0;
            if(ArenaUiTheme.Button("저장",GUILayout.Width(70)))RequestNicknameSave();
            GUILayout.Label(NicknameSaveNotice,ArenaUiTheme.Muted);GUILayout.EndHorizontal();GUILayout.Space(8);
            ProcessNicknameSave();
            var enabled=GUI.enabled;GUI.enabled=enabled&&!OnlineBusy&&!onlineLeaving&&!nicknameSavePending;
            GUILayout.BeginHorizontal();GUILayout.Label("방 이름",GUILayout.Width(60));GUI.SetNextControlName("Arena.RoomName");onlineRoomDraft=GUILayout.TextField(onlineRoomDraft,32);
            if(ArenaUiTheme.Button("방 만들기",GUILayout.Width(105)))_=CreateOnlineRoomAsync(onlineRoomDraft);GUILayout.EndHorizontal();
            if(ArenaUiTheme.Button("혼자 연습 · 인터넷 연결 불필요",GUILayout.Height(30)))StartSoloPractice();
            if(ArenaUiTheme.Button("튜토리얼 · 이동과 무기 익히기",GUILayout.Height(30)))StartTutorial();
            GUILayout.Label("개인전 · 2~4명 · 3분",ArenaUiTheme.Muted);GUILayout.Space(14);
            GUILayout.BeginHorizontal();GUILayout.Label("방 목록");if(ArenaUiTheme.Button("새로고침",GUILayout.Width(105)))_=RefreshOnlineRoomsAsync();GUILayout.EndHorizontal();
            roomScroll=GUILayout.BeginScrollView(roomScroll,GUILayout.Height(Mathf.Max(70,height-410)));
            if(onlineRooms.Count==0)GUILayout.Label(browserOpened?"검색된 방이 없습니다.":"새로고침을 누르면 인터넷 방 목록을 불러옵니다.");
            var roomStyle=new GUIStyle(GUI.skin.label){wordWrap=true,richText=false};
            foreach(var room in onlineRooms)
            {
                GUILayout.BeginVertical(GUI.skin.box);GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();GUILayout.Label(room.Name,roomStyle);
                string phase=RoomProperty(room,"phase","waiting");
                string state=phase=="results"?"결과 확인 중":phase!="waiting"||room.IsLocked?"경기 진행 중":room.AvailableSlots<=0?"인원 마감":"대기 중";

                GUILayout.Label((room.MaxPlayers-room.AvailableSlots)+" / "+room.MaxPlayers+"명 · "+state);GUILayout.EndVertical();
                GUI.enabled=enabled&&!OnlineBusy&&onlineListCurrent&&OnlineRoomJoinable(room);
                if(ArenaUiTheme.Button("입장",GUILayout.Width(75),GUILayout.Height(48)))_=JoinOnlineRoomAsync(room.Id);
                GUI.enabled=enabled&&!OnlineBusy&&!onlineLeaving&&!nicknameSavePending;GUILayout.EndHorizontal();GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();GUI.enabled=enabled;
            if(OnlineBusy)ArenaTransitionHud.Connection();
            if(!string.IsNullOrEmpty(ConnectionNotice))GUILayout.Label(ConnectionNotice,roomStyle);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            localTools=GUILayout.Toggle(localTools,"개발용 로컬 연결");
            if(localTools)
            {
                GUI.enabled=enabled&&!OnlineBusy&&!onlineLeaving&&!nicknameSavePending;
                GUILayout.BeginHorizontal();if(ArenaUiTheme.Button("로컬 방 만들기"))StartLocal(true);if(ArenaUiTheme.Button("로컬 방 참가"))StartLocal(false);GUILayout.EndHorizontal();GUI.enabled=enabled;
            }
#endif
            UpdateLobbyIme(GUI.enabled&&Application.isFocused&&RelayArenaPrototype.HasGameplayFocus&&(GUI.GetNameOfFocusedControl()=="Arena.Nickname"||GUI.GetNameOfFocusedControl()=="Arena.RoomName"));
            GUILayout.EndArea();
        }
    }
}
