using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkSession
    {
        bool lobbyIme,nicknameSavePending;int nicknameSaveFrame;float nicknameSavedUntil;
#if ENABLE_LEGACY_INPUT_MANAGER
        IMECompositionMode previousImeMode;
#endif
        bool ComposingNickname
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                return !string.IsNullOrEmpty(Input.compositionString);
#else
                return false;
#endif
            }
        }
        public string NicknameSaveNotice=>nicknameSavePending?"입력 중인 글자를 확정해 주세요":Time.unscaledTime<nicknameSavedUntil?"닉네임이 저장되었습니다.":"최대 12자";
        string LobbyTextField(string value,int length,bool nickname,params GUILayoutOption[] options)
        {
            if(!BrowserPlayer)return GUILayout.TextField(value,length,options);
            if(GUILayout.Button(value,GUI.skin.textField,options))
                ArenaBrowserTextInput.Open(this,nickname?"닉네임":"방 이름",value,nickname?"한글·영문 최대 12자 · 조합을 마친 뒤 저장하세요.":"최대 32자 · 입력을 마친 뒤 적용하세요.",text=>
                {
                    if(nickname){nicknameDraft=text;nicknameSavedUntil=0;RequestNicknameSave();}
                    else
                    {
                        onlineRoomDraft=text.Length>32?text.Substring(0,32):text;
                        if(onlineRoomDraft.Length>0&&char.IsHighSurrogate(onlineRoomDraft[onlineRoomDraft.Length-1]))onlineRoomDraft=onlineRoomDraft.Substring(0,onlineRoomDraft.Length-1);
                    }
                });
            return value;
        }
        void RequestNicknameSave(){nicknameSavePending=true;nicknameSaveFrame=Time.frameCount;}
        void ProcessNicknameSave()
        {
            // Keep the field focused while Hangul is composing. Normalize only
            // after the committed text has reached IMGUI on a later frame.
            if(!nicknameSavePending||ComposingNickname||Time.frameCount<=nicknameSaveFrame)return;
            ArenaPlayerNames.Save(nicknameDraft);nicknameDraft=ArenaPlayerNames.Local;
            nicknameSavePending=false;nicknameSavedUntil=Time.unscaledTime+3;
        }
        void UpdateLobbyIme(bool enabled)
        {
            if(BrowserPlayer)return;
            if(lobbyIme==enabled)return;
#if ENABLE_LEGACY_INPUT_MANAGER
            if(enabled){previousImeMode=Input.imeCompositionMode;Input.imeCompositionMode=IMECompositionMode.On;}
            else Input.imeCompositionMode=previousImeMode;
#endif
#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Keyboard.current?.SetIMEEnabled(enabled);
#endif
            lobbyIme=enabled;
        }
        void OnDisable(){UpdateLobbyIme(false);nicknameSavePending=false;}
    }
}
