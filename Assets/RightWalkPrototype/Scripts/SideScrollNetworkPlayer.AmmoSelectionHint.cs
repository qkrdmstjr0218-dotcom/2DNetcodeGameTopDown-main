using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public const float AmmoSelectionHintSeconds=1.4f;
        float ammoHintUntil;uint ammoHintLife;
        GUIStyle ammoHintStyle;
        ArenaObjectiveMatch.SupplyAmmo HintSupply=>SelectedAmmo==AmmoKind.Rifle?ArenaObjectiveMatch.SupplyAmmo.Rifle:SelectedAmmo==AmmoKind.Grenade?ArenaObjectiveMatch.SupplyAmmo.Grenade:ArenaObjectiveMatch.SupplyAmmo.Shotgun;
        public string AmmoSelectionHintText=>ArenaObjectiveMatch.SupplyInitial(HintSupply);
        public int SelectedAmmoRounds=>SelectedAmmo==AmmoKind.Rifle?RifleRounds:SelectedAmmo==AmmoKind.Grenade?GrenadeRounds:ShotgunRounds;
        public int SelectedAmmoCapacity=>SelectedAmmo==AmmoKind.Rifle?RifleCapacity:SelectedAmmo==AmmoKind.Grenade?GrenadeCapacity:ShotgunCapacity;
        public string AmmoSelectionHintCountText=>SelectedAmmoRounds.ToString();
        public bool AmmoSelectionHintVisible=>IsSpawned&&IsOwner&&NormalAmmoEnabled&&!IsDead
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
            var at=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*(crouch.IsCrouched?1.95f:2.45f));
            if(at.z<=0)return;
            float alpha=Mathf.Clamp01((ammoHintUntil-Time.unscaledTime)/.25f);
            var rect=new Rect(Mathf.Clamp(at.x-40,8,Mathf.Max(8,Screen.width-88)),Mathf.Clamp(Screen.height-at.y-(ArenaCombatHud.Active?(Shield>0?102:80):(Shield>0?86:64)),8,Mathf.Max(8,Screen.height-44)),80,36);
            var tint=SelectedAmmoRounds==0?new Color(1,.25f,.25f):ArenaObjectiveMatch.SupplyColor(HintSupply);
            if(ammoHintStyle==null)ammoHintStyle=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=24,fontStyle=FontStyle.Bold};
            // Bare text with a small shadow for contrast; no box or capacity.
            var old=GUI.color;GUI.color=new Color(1,1,1,alpha);
            string text=AmmoSelectionHintText+" "+AmmoSelectionHintCountText;
            ammoHintStyle.normal.textColor=new Color(0,0,0,.8f);ArenaUiTheme.Static(ammoHintStyle);GUI.Label(new Rect(rect.x+1,rect.y+1,rect.width,rect.height),text,ammoHintStyle);
            ammoHintStyle.normal.textColor=tint;ArenaUiTheme.Static(ammoHintStyle);GUI.Label(rect,text,ammoHintStyle);GUI.color=old;
        }
    }
}
