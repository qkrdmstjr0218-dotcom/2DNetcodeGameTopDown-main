using Unity.Services.Multiplayer;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkSession
    {
        string onlineRoomDraft="해킹 점령전";Vector2 roomScroll;bool localTools;
        static string RoomProperty(ISessionInfo room,string key,string fallback)
        {return room.Properties!=null&&room.Properties.TryGetValue(key,out var value)?value.Value:fallback;}
        public static bool OnlineRoomJoinable(ISessionInfo room)=>room!=null&&!room.IsLocked&&!room.HasPassword&&room.AvailableSlots>0&&RoomProperty(room,"phase","waiting")=="waiting";
        void DrawOnlineLobby()
        {
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            var old=GUI.color;GUI.color=new Color(.035f,.055f,.075f,.98f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            float width=Mathf.Min(690,Screen.width-24),height=Mathf.Min(540,Screen.height-24);
            GUILayout.BeginArea(new Rect((Screen.width-width)*.5f,(Screen.height-height)*.5f,width,height),GUI.skin.box);
            GUILayout.Label("RELAY / ARENA",new GUIStyle(GUI.skin.label){fontSize=24,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter});
            GUILayout.Label("인터넷 방에 입장하거나, 연결 없이 혼자 연습할 수 있습니다.");GUILayout.Space(8);
            var enabled=GUI.enabled;GUI.enabled=enabled&&!OnlineBusy&&!onlineLeaving;
            GUILayout.BeginHorizontal();GUILayout.Label("방 이름",GUILayout.Width(60));onlineRoomDraft=GUILayout.TextField(onlineRoomDraft,32);
            if(GUILayout.Button("방 만들기",GUILayout.Width(105)))_=CreateOnlineRoomAsync(onlineRoomDraft);GUILayout.EndHorizontal();
            if(GUILayout.Button("혼자 연습 · 인터넷 연결 불필요",GUILayout.Height(30)))StartSoloPractice();
            GUILayout.Label(OnlineModeName+" · "+OnlineMapName+" · 2~4명 · 3분");GUILayout.Space(10);
            GUILayout.BeginHorizontal();GUILayout.Label("방 목록");if(GUILayout.Button("새로고침",GUILayout.Width(105)))_=RefreshOnlineRoomsAsync();GUILayout.EndHorizontal();
            roomScroll=GUILayout.BeginScrollView(roomScroll,GUILayout.Height(Mathf.Max(100,height-280)));
            if(onlineRooms.Count==0)GUILayout.Label(browserOpened?"검색된 방이 없습니다.":"새로고침을 누르면 인터넷 방 목록을 불러옵니다.");
            var roomStyle=new GUIStyle(GUI.skin.label){wordWrap=true,richText=false};
            foreach(var room in onlineRooms)
            {
                GUILayout.BeginVertical(GUI.skin.box);GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();GUILayout.Label(room.Name,roomStyle);
                string phase=RoomProperty(room,"phase","waiting");
                string state=phase=="results"?"결과 확인 중":phase!="waiting"||room.IsLocked?"경기 진행 중":room.AvailableSlots<=0?"인원 마감":"대기 중";
                GUILayout.Label(RoomProperty(room,"mode",OnlineModeName)+" · "+RoomProperty(room,"map",OnlineMapName),roomStyle);
                GUILayout.Label((room.MaxPlayers-room.AvailableSlots)+" / "+room.MaxPlayers+"명 · "+state);GUILayout.EndVertical();
                GUI.enabled=enabled&&!OnlineBusy&&onlineListCurrent&&OnlineRoomJoinable(room);
                if(GUILayout.Button("입장",GUILayout.Width(75),GUILayout.Height(58)))_=JoinOnlineRoomAsync(room.Id);
                GUI.enabled=enabled&&!OnlineBusy&&!onlineLeaving;GUILayout.EndHorizontal();GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();GUI.enabled=enabled;
            if(OnlineBusy)GUILayout.Label("연결 중… 잠시 기다려 주세요.");
            if(!string.IsNullOrEmpty(ConnectionNotice))GUILayout.Label(ConnectionNotice,roomStyle);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            localTools=GUILayout.Toggle(localTools,"개발용 로컬 연결");
            if(localTools)
            {
                GUI.enabled=enabled&&!OnlineBusy&&!onlineLeaving;
                GUILayout.BeginHorizontal();if(GUILayout.Button("로컬 방 만들기"))StartLocal(true);if(GUILayout.Button("로컬 방 참가"))StartLocal(false);GUILayout.EndHorizontal();GUI.enabled=enabled;
            }
#endif
            GUILayout.EndArea();
        }
    }
}
