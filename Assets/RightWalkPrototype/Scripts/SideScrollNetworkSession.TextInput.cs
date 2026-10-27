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
