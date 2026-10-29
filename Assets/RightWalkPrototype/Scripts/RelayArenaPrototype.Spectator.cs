using System;
using UnityEngine;

namespace RightWalkPrototype
{
    public sealed partial class RelayArenaPrototype
    {
        ulong observedClient=ulong.MaxValue;
        SideScrollNetworkPlayer LocalPlayer()
        {foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.IsLocalHuman)return p;return null;}
        public bool WantsSpectator=>ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.RoomFlow&&ArenaObjectiveMatch.Instance.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing&&LocalPlayer()!=null&&LocalPlayer().IsDead;
        SideScrollNetworkPlayer[] LivingPlayers()
        {
            var players=Array.FindAll(FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None),p=>p.IsSpawned&&!p.IsDead&&p.HasCurrentLifePose);
            Array.Sort(players,(a,b)=>a.PlayerNumber.CompareTo(b.PlayerNumber));return players;
        }
        public SideScrollNetworkPlayer SpectatorTarget()
        {
            var players=LivingPlayers();foreach(var p in players)if(p.ParticipantId==observedClient)return p;
            if(players.Length==0)return null;observedClient=players[0].ParticipantId;return players[0];
        }
        public void CycleSpectator(int direction)
        {
            if(!WantsSpectator)return;var players=LivingPlayers();if(players.Length==0)return;
            int index=Array.FindIndex(players,p=>p.ParticipantId==observedClient);index=(Mathf.Max(0,index)+direction+players.Length)%players.Length;observedClient=players[index].ParticipantId;
        }
        void DrawSpectator()
        {
            if(!WantsSpectator)return;
            using(ArenaUiTheme.Use())DrawStyledSpectator();
        }
        GUIStyle respawnTitleStyle,respawnSecondsStyle,respawnDetailStyle,respawnButtonStyle;
        public static Rect RespawnPanelRect=>new Rect((Screen.width-Mathf.Min(460,Screen.width-32))*.5f,Screen.height-212,Mathf.Min(460,Screen.width-32),192);
        void DrawStyledSpectator()
        {
            var own=LocalPlayer();var viewed=SpectatorTarget();
            if(respawnTitleStyle==null)
            {
                respawnTitleStyle=ArenaUiTheme.Static(new GUIStyle(GUI.skin.label){fontSize=16,alignment=TextAnchor.MiddleCenter,wordWrap=false});
                respawnSecondsStyle=ArenaUiTheme.Static(new GUIStyle(respawnTitleStyle){fontSize=42,fontStyle=FontStyle.Bold});
                respawnDetailStyle=ArenaUiTheme.Static(new GUIStyle(respawnTitleStyle){fontSize=13});
            }
            float remaining=own.RespawnRemaining;
            var accent=remaining<=2?new Color(1,.77f,.35f):new Color(.35f,.85f,.92f);
            var panel=RespawnPanelRect;ArenaHudSkin.Panel(panel,accent);
            GUI.Label(new Rect(panel.x+12,panel.y+9,panel.width-24,25),"자동 부활까지",respawnTitleStyle);
            respawnSecondsStyle.normal.textColor=accent;ArenaUiTheme.Static(respawnSecondsStyle);
            GUI.Label(new Rect(panel.x+12,panel.y+32,panel.width-24,58),remaining>.01f?Mathf.CeilToInt(remaining)+"초":"부활 중",respawnSecondsStyle);
            float duration=Mathf.Max(.1f,ArenaObjectiveMatch.Instance.RespawnSeconds);
            ArenaHudSkin.Meter(new Rect(panel.x+22,panel.y+96,panel.width-44,8),1-remaining/duration,accent);
            GUI.Label(new Rect(panel.x+12,panel.y+110,panel.width-24,22),viewed!=null?"P"+viewed.PlayerNumber+" 관전 중":"주변 전장을 확인하며 부활을 기다립니다",respawnDetailStyle);
            if(viewed!=null)
            {
                respawnButtonStyle??=ArenaUiTheme.Static(new GUIStyle(GUI.skin.button){fontSize=14,fixedHeight=0,alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset(6,6,2,2)});
                float half=(panel.width-36)*.5f;
                if(GUI.Button(new Rect(panel.x+12,panel.y+142,half,36),"◀ 이전 관전",respawnButtonStyle)){ArenaAudio.Ui(ArenaAudio.Cue.UiClick);CycleSpectator(-1);}
                if(GUI.Button(new Rect(panel.x+24+half,panel.y+142,half,36),"다음 관전 ▶",respawnButtonStyle)){ArenaAudio.Ui(ArenaAudio.Cue.UiClick);CycleSpectator(1);}
            }
        }
    }
}
