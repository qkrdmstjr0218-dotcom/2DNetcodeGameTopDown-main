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
            var rect=new Rect(Mathf.Clamp(at.x-18,8,Mathf.Max(8,Screen.width-44)),Mathf.Clamp(Screen.height-at.y-(Shield>0?86:64),8,Mathf.Max(8,Screen.height-44)),36,36);
            var tint=ArenaObjectiveMatch.SupplyColor(HintSupply);
            var old=GUI.color;GUI.color=new Color(tint.r,tint.g,tint.b,alpha);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=new Color(.04f,.13f,.17f,.96f*alpha);GUI.DrawTexture(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),Texture2D.whiteTexture);
            if(ammoHintStyle==null)ammoHintStyle=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=24,fontStyle=FontStyle.Bold};
            ammoHintStyle.normal.textColor=tint;GUI.color=new Color(1,1,1,alpha);GUI.Label(rect,AmmoSelectionHintText,ammoHintStyle);GUI.color=old;
        }
    }
}
