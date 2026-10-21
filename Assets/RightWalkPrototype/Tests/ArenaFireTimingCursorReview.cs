#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
using RightWalkPrototype;
// Review-only rendering of the real cursor when virtual editors steal OS focus.
public sealed class ArenaFireTimingCursorReview:MonoBehaviour
{
    public SideScrollNetworkPlayer Player;
    void OnGUI()
    {
        if(Player==null||Player.AmmoCursorVisible||Event.current.type!=EventType.Repaint||ArenaObjectiveMatch.CombatBlocked)return;
        bool rifle=Player.RifleActive,shotgun=Player.SpecialShotActive;
        typeof(SideScrollNetworkPlayer).GetMethod("DrawAmmoCursor",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Player,new object[]{new Vector2(Screen.width*.57f,Screen.height*.48f),Player.GrenadeActive?(float)Player.GrenadeRounds:rifle?(float)Player.RifleRounds:shotgun?Player.ShotgunRounds:Player.NormalAmmo,Player.GrenadeActive?SideScrollNetworkPlayer.GrenadeCapacity:rifle?SideScrollNetworkPlayer.RifleCapacity:shotgun?SideScrollNetworkPlayer.ShotgunCapacity:Player.NormalCapacity,shotgun,Player.InstantShotgunActive,rifle});
    }
}

#endif
