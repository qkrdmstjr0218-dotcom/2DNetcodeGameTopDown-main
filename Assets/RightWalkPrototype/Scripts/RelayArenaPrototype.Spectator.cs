using System;
using UnityEngine;

namespace RightWalkPrototype
{
    public sealed partial class RelayArenaPrototype
    {
        ulong observedClient=ulong.MaxValue;
        SideScrollNetworkPlayer LocalPlayer()
        {foreach(var p in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))if(p.IsSpawned&&p.IsOwner)return p;return null;}
        public bool WantsSpectator=>ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.RoomFlow&&ArenaObjectiveMatch.Instance.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing&&LocalPlayer()!=null&&LocalPlayer().IsDead;
        SideScrollNetworkPlayer[] LivingPlayers()
        {
            var players=Array.FindAll(FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None),p=>p.IsSpawned&&!p.IsDead&&p.HasCurrentLifePose);
            Array.Sort(players,(a,b)=>a.PlayerNumber.CompareTo(b.PlayerNumber));return players;
        }
        public SideScrollNetworkPlayer SpectatorTarget()
        {
            var players=LivingPlayers();foreach(var p in players)if(p.OwnerClientId==observedClient)return p;
            if(players.Length==0)return null;observedClient=players[0].OwnerClientId;return players[0];
        }
        public void CycleSpectator(int direction)
        {
            if(!WantsSpectator)return;var players=LivingPlayers();if(players.Length==0)return;
            int index=Array.FindIndex(players,p=>p.OwnerClientId==observedClient);index=(Mathf.Max(0,index)+direction+players.Length)%players.Length;observedClient=players[index].OwnerClientId;
        }
        void DrawSpectator()
        {
            if(!WantsSpectator)return;
            using(ArenaUiTheme.Use())DrawStyledSpectator();
        }
        void DrawStyledSpectator()
        {
            var own=LocalPlayer();var viewed=SpectatorTarget();
            GUILayout.BeginArea(new Rect(Screen.width*.5f-190,Screen.height-130,380,110),GUI.skin.box);
            GUILayout.Label("부활 대기 · "+own.RespawnRemaining.ToString("0.0")+"초");
            GUILayout.Label(viewed!=null?"P"+viewed.PlayerNumber+" 관전 중":"생존 플레이어가 없습니다.");
            if(viewed!=null){GUILayout.BeginHorizontal();if(ArenaUiTheme.Button("◀ 이전"))CycleSpectator(-1);if(ArenaUiTheme.Button("다음 ▶"))CycleSpectator(1);GUILayout.EndHorizontal();}
            GUILayout.EndArea();
        }
    }
}
