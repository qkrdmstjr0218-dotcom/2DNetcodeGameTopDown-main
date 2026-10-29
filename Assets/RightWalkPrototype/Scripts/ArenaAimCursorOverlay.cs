using UnityEngine;
namespace RightWalkPrototype
{
    [DefaultExecutionOrder(12000)]
    public sealed class ArenaAimCursorOverlay:MonoBehaviour
    {
        SideScrollNetworkPlayer player;
        void Awake(){player=GetComponent<SideScrollNetworkPlayer>();}
        void OnGUI()
        {
            if(player==null||!player.IsLocalHuman||RelayArenaPrototype.Instance==null||RelayArenaPrototype.Instance.MenuOpen||ArenaObjectiveMatch.MarkerGuideOpen||ArenaCombatHud.TabVisible)return;
            GUI.depth=-100;
            player.DrawAimCursorOverlay();
        }
    }
}
