using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class RelayArenaPrototype
    {
        public bool LeaveConfirmationOpen {get;private set;}
        public bool QuitConfirmationOpen {get;private set;}
        bool leaveReturnMenu,quitReturnMenu;
        float masterVolume=1;
        void LoadAudioSettings(){masterVolume=Mathf.Clamp01(PlayerPrefs.GetFloat("RightWalk.MasterVolume",1));AudioListener.volume=masterVolume;}
        public void SetMasterVolume(float value){masterVolume=Mathf.Clamp01(value);AudioListener.volume=masterVolume;PlayerPrefs.SetFloat("RightWalk.MasterVolume",masterVolume);PlayerPrefs.Save();}
        public void RequestLeave(){if(!LeaveConfirmationOpen)leaveReturnMenu=MenuOpen;LeaveConfirmationOpen=true;SetMenuOpen(true);}
        public void CancelLeave(){LeaveConfirmationOpen=false;SetMenuOpen(leaveReturnMenu);}
        public void ConfirmLeave(){LeaveConfirmationOpen=false;FindFirstObjectByType<SideScrollNetworkSession>()?.DisconnectLocal();SetMenuOpen(false);}
        public void RequestQuit()
        {
            if(!QuitConfirmationOpen)quitReturnMenu=MenuOpen;
            LeaveConfirmationOpen=false;QuitConfirmationOpen=true;SetMenuOpen(true);
        }
        public void CancelQuit(){QuitConfirmationOpen=false;SetMenuOpen(quitReturnMenu);}
        public void ConfirmQuit()
        {
            if(!QuitConfirmationOpen)return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        void HandleMenuEscape()
        {
            if(ArenaObjectiveMatch.MarkerGuideOpen){ArenaObjectiveMatch.Instance.CloseRules();return;}
            if(QuitConfirmationOpen)CancelQuit();
            else if(LeaveConfirmationOpen)CancelLeave();
            else SetMenuOpen(!MenuOpen);
        }
        void PlayVolumePreview()
        {
            ArenaAudio.Ui(ArenaAudio.Cue.RoundStart);
        }
        internal void DrawGameMenu()
        {
            int depth=GUI.depth;GUI.depth=-200;var old=GUI.color;GUI.color=new Color(.008f,.02f,.025f,.82f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=old;
            using(ArenaUiTheme.Use())
            {
                bool lobby=SideScrollNetworkSession.LobbyVisible;
                float width=Mathf.Min(480,Screen.width-32),height=Mathf.Min(LeaveConfirmationOpen||QuitConfirmationOpen?260:lobby?350:420,Screen.height-32);
                GUILayout.BeginArea(new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height),GUI.skin.box);
                if(QuitConfirmationOpen)
                {
                    GUILayout.Label("게임을 종료할까요?",ArenaUiTheme.Title);
                    GUILayout.Label("게임을 종료하고 바탕화면으로 돌아갑니다.",ArenaUiTheme.Muted);
                    GUILayout.FlexibleSpace();GUILayout.BeginHorizontal();
                    if(ArenaUiTheme.Button("게임 종료",GUILayout.Height(42)))ConfirmQuit();
                    if(ArenaUiTheme.Button("취소",GUILayout.Height(42)))CancelQuit();
                    GUILayout.EndHorizontal();
                }
                else if(LeaveConfirmationOpen)
                {
                    GUILayout.Label("로비로 나갈까요?",ArenaUiTheme.Title);
                    var n=Unity.Netcode.NetworkManager.Singleton;
                    GUILayout.Label(n!=null&&n.IsHost&&FindFirstObjectByType<SideScrollNetworkSession>()!=null&&!FindFirstObjectByType<SideScrollNetworkSession>().SoloPractice?"방장이 나가면 방이 닫히고 모든 참가자가 로비로 돌아갑니다.":"현재 방에서 나가 로비로 돌아갑니다.",ArenaUiTheme.Muted);
                    GUILayout.FlexibleSpace();GUILayout.BeginHorizontal();if(ArenaUiTheme.Button("로비로 나가기",GUILayout.Height(42)))ConfirmLeave();if(ArenaUiTheme.Button("취소",GUILayout.Height(42)))CancelLeave();GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("설정",ArenaUiTheme.Title);GUILayout.Label(lobby?"음량을 조절하고 소리를 확인할 수 있습니다.":"메뉴를 열어도 경기는 계속 진행됩니다.",ArenaUiTheme.Muted);GUILayout.Space(22);
                    GUILayout.Label("전체 음량   "+Mathf.RoundToInt(masterVolume*100)+"%");float next=GUILayout.HorizontalSlider(masterVolume,0,1);
                    if(Mathf.Abs(next-masterVolume)>.001f)SetMasterVolume(next);
                    GUILayout.Label("효과음   "+Mathf.RoundToInt(ArenaAudio.EffectsVolume*100)+"%");float effects=GUILayout.HorizontalSlider(ArenaAudio.EffectsVolume,0,1);
                    if(Mathf.Abs(effects-ArenaAudio.EffectsVolume)>.001f)ArenaAudio.SetEffectsVolume(effects);
                    GUILayout.Space(12);
                    if(GUILayout.Button("소리 테스트"))PlayVolumePreview();GUILayout.FlexibleSpace();
                    if(ArenaUiTheme.Button(lobby?"닫기 · ESC":"계속하기 · ESC",GUILayout.Height(40)))SetMenuOpen(false);
                    if(!lobby&&ArenaUiTheme.Button("로비로 나가기",GUILayout.Height(40)))RequestLeave();
                }
                GUILayout.EndArea();
            }
            GUI.depth=depth;
        }
    }
}
