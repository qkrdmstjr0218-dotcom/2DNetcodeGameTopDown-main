using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public const float AmmoSelectionHintSeconds=1.4f;
        float ammoHintUntil;uint ammoHintLife;
        GUIStyle ammoHintStyle;
        Rect ammoHintRect;
        public int AmmoSelectionHintSlot=>SelectedAmmo==AmmoKind.Rifle?2:SelectedAmmo==AmmoKind.Grenade?3:1;
        ArenaObjectiveMatch.SupplyAmmo HintSupply=>SelectedAmmo==AmmoKind.Rifle?ArenaObjectiveMatch.SupplyAmmo.Rifle:SelectedAmmo==AmmoKind.Grenade?ArenaObjectiveMatch.SupplyAmmo.Grenade:ArenaObjectiveMatch.SupplyAmmo.Shotgun;
        public string AmmoSelectionHintText=>ArenaObjectiveMatch.SupplyInitial(HintSupply);
        public int SelectedAmmoRounds=>SelectedAmmo==AmmoKind.Rifle?RifleRounds:SelectedAmmo==AmmoKind.Grenade?GrenadeRounds:ShotgunRounds;
        public int SelectedAmmoCapacity=>SelectedAmmo==AmmoKind.Rifle?RifleCapacity:SelectedAmmo==AmmoKind.Grenade?GrenadeCapacity:ShotgunCapacity;
        public string AmmoSelectionHintCountText=>SelectedAmmoRounds.ToString();
        public bool AmmoSelectionHintVisible=>IsSpawned&&IsLocalHuman&&NormalAmmoEnabled&&!IsDead
            &&ammoHintLife==LifeVersion&&Time.unscaledTime<ammoHintUntil&&SelectedAmmo!=AmmoKind.Normal
            &&!RelayArenaPrototype.ControlsBlocked&&!ArenaObjectiveMatch.TransitionActive&&!ArenaObjectiveMatch.Ended;
        void ShowAmmoSelectionHint()
        {
            if(!NormalAmmoEnabled||SelectedAmmo==AmmoKind.Normal)return;
            ammoHintLife=LifeVersion;ammoHintUntil=Time.unscaledTime+AmmoSelectionHintSeconds;
        }
        void DrawAmmoSelectionHint()
        {
            if(!AmmoSelectionHintVisible)return;
            bool ready=ArenaObjectiveMatch.InReadyRoom&&!ArenaTutorial.Active;
            var at=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*(ready?ReadyIdentityHeight:crouch.IsCrouched?1.95f:2.45f));
            if(at.z<=0)return;
            float alpha=Mathf.Clamp01((ammoHintUntil-Time.unscaledTime)/.25f);
            // The compact row sits just above the actual world HUD / ready badge.
            float offset=ArenaCombatHud.Active?10*ArenaCombatHud.UiScale+34:62;
            ammoHintRect=new Rect(Mathf.Clamp(at.x-30,8,Mathf.Max(8,Screen.width-68)),Mathf.Clamp(Screen.height-at.y-offset,8,Mathf.Max(8,Screen.height-36)),60,28);
            var tint=SelectedAmmoRounds==0?new Color(1,.25f,.25f):ArenaObjectiveMatch.SupplyColor(HintSupply);
            ammoHintStyle??=ArenaUiTheme.Static(new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleLeft,fontSize=20,fontStyle=FontStyle.Bold,padding=new RectOffset()});
            var old=GUI.color;GUI.color=new Color(1,1,1,alpha);
            var fadedTint=tint;fadedTint.a*=alpha;
            var icon=new Rect(ammoHintRect.x,ammoHintRect.y+1,26,26);
            var count=new Rect(ammoHintRect.x+31,ammoHintRect.y,29,28);
            ArenaHudSkin.AmmoGlyph(new Rect(icon.x+1,icon.y+1,icon.width,icon.height),AmmoSelectionHintSlot,new Color(0,0,0,.8f*alpha));
            ammoHintStyle.normal.textColor=new Color(0,0,0,.8f);ArenaUiTheme.Static(ammoHintStyle);GUI.Label(new Rect(count.x+1,count.y+1,count.width,count.height),AmmoSelectionHintCountText,ammoHintStyle);
            ArenaHudSkin.AmmoGlyph(icon,AmmoSelectionHintSlot,fadedTint);
            ammoHintStyle.normal.textColor=tint;ArenaUiTheme.Static(ammoHintStyle);GUI.Label(count,AmmoSelectionHintCountText,ammoHintStyle);GUI.color=old;
        }
    }
}
