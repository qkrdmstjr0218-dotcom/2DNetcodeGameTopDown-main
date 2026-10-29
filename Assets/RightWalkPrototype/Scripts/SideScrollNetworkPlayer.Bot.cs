using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        readonly NetworkVariable<bool> bot=new(false);
        public bool IsBot=>bot.Value;
        public bool IsLocalHuman=>IsOwner&&!IsBot;
        // Server ownership controls simulation; participant identity controls scores/targets.
        public ulong ParticipantId=>IsBot?ulong.MaxValue-(ulong)PlayerNumber:OwnerClientId;
        bool pendingBot;
        internal void PrepareBot(){pendingBot=true;}
        void InitializeBot()
        {
            if(!IsBot)return;
            movement.readKeyboard=false;walker.readKeyboard=false;crouch.readKeyboard=false;
            aim.mouseAim=false;aim.showAimPreview=false;shots.readMouse=false;
            if(IsServer){nickname.Value=new FixedString64Bytes("AI "+PlayerNumber);gameObject.AddComponent<ArenaPlayerBot>();}
        }
        internal void BotFire(Vector2 target,AmmoKind kind=AmmoKind.Normal,Vector2 grenadeVelocity=default)
        {
            if(!IsServer||!IsBot||IsDead||ArenaObjectiveMatch.CombatBlocked||RelayArenaPrototype.ControlsBlocked)return;
            SelectAmmo(kind==AmmoKind.Normal?SelectedAmmo:kind);SetSpecialHeld(kind!=AmmoKind.Normal);
            if(kind==AmmoKind.Grenade)aim.SetAimDirection(grenadeVelocity.normalized);else aim.SetAimTarget(target);
            // Same authoritative validation, switching delay, ammo, damage and effects as humans.
            if(kind==AmmoKind.Rifle)FireRifleServerRpc(aim.MuzzlePosition,aim.AimDirection,LifeVersion);
            else if(kind==AmmoKind.InstantShotgun)FireInstantShotgunServerRpc(aim.MuzzlePosition,aim.AimDirection,LifeVersion);
            else if(kind==AmmoKind.Grenade)
            {
                if(Vector2.Dot(aim.AimDirection,grenadeVelocity.normalized)>.995f)
                    FireGrenadeServerRpc(aim.MuzzlePosition,aim.AimDirection,grenadeVelocity.magnitude,LifeVersion);
            }
            else FireNormalServer(aim.MuzzlePosition,aim.AimDirection);
        }
        internal bool PlanBotGrenade(Bounds target,out Vector2 velocity)
        {
            velocity=default;var points=new System.Collections.Generic.List<Vector2>();
            var original=aim.AimPoint;
            bool found=false;
            // Solve low/high ballistic arcs, then verify with the production collision predictor.
            foreach(float speed in new[]{10f,14f,18f})
            {
                Vector2 origin=aim.MuzzlePosition,delta=(Vector2)target.center-origin;
                float x=Mathf.Abs(delta.x),g=ArenaGrenadeBallistics.Gravity,v2=speed*speed;
                if(x<.5f)continue;
                float discriminant=v2*v2-g*(g*x*x+2*delta.y*v2);if(discriminant<0)continue;
                foreach(float sign in new[]{-1f,1f})
                {
                    float angle=Mathf.Atan((v2+sign*Mathf.Sqrt(discriminant))/(g*x));
                    var direction=new Vector2(Mathf.Sign(delta.x)*Mathf.Cos(angle),Mathf.Sin(angle));
                    aim.SetAimDirection(direction);origin=aim.MuzzlePosition;
                    if(Vector2.Dot(direction,aim.AimDirection)<.995f)continue;
                    if(!ArenaGrenadeBallistics.Predict(this,origin,direction*speed,points,out var end))continue;
                    if(ArenaGrenadeBallistics.BlastDistance(end,target)>1.3f||ArenaGrenadeBallistics.BlastDistance(end,ArenaHitBounds)<ArenaGrenadeBallistics.BlastRadius+.5f)continue;
                    velocity=direction*speed;found=true;break;
                }
                if(found)break;
            }
            aim.SetAimTarget(original);return found;
        }
    }
}
