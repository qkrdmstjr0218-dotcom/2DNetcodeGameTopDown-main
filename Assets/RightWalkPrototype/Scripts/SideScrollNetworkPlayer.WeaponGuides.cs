using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        const int GuideSamples=24;
        readonly Vector2[] shotgunGuide=new Vector2[GuideSamples+1];
        public bool WeaponGuideVisible=>IsSpawned&&IsOwner&&NormalAmmoEnabled&&!IsDead&&SpecialHeld
            &&(SelectedAmmo==AmmoKind.Rifle||SelectedAmmo==AmmoKind.InstantShotgun||SelectedAmmo==AmmoKind.Shotgun)
            &&!RelayArenaPrototype.ControlsBlocked&&!ArenaObjectiveMatch.CombatBlocked&&!ArenaObjectiveMatch.RoomPointerOverUI;
        // Empty takes priority over the existing switch / per-weapon fire wait.
        public int WeaponGuideState=>SelectedAmmoRounds<=0?2:FireWaitRemaining>0?1:0;
        public Vector2 RifleGuideEnd=>(Vector2)aim.MuzzlePosition+aim.AimDirection*RifleRange;
        public Vector2 ShotgunGuidePoint(float degrees)=>ShotgunGuidePoint(degrees,FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None));
        Vector2 ShotgunGuidePoint(float degrees,SideScrollNetworkPlayer[] players)
        {
            Vector2 origin=aim.MuzzlePosition;
            Vector2 ray=Quaternion.Euler(0,0,Mathf.Clamp(degrees,-ShotgunSpread*.5f,ShotgunSpread*.5f))*aim.AimDirection;
            // Same barrel-back sweep and player bounds as FireSpreadServer.
            float barrel=Mathf.Max(0,Vector2.Dot(origin-(Vector2)ServerHitBounds().center,ray));
            Vector2 start=origin-ray*barrel;
            Vector2 end=aim.TraceShot(start,ray,barrel+ShotgunRange,out Collider2D _,true);
            float distance=Vector2.Distance(start,end);
            foreach(var p in players)
            {
                if(p==this||!p.IsSpawned||p.IsDead)continue;
                var bounds=p.ServerHitBounds();
                bool hit=bounds.IntersectRay(new Ray(start,ray),out float at);
                if(bounds.Contains(new Vector3(start.x,start.y,bounds.center.z))){hit=true;at=0;}
                if(hit&&at>=0&&at<distance)distance=at;
            }
            return start+ray*distance;
        }
        public void DrawWeaponGuide(Camera camera)
        {
            if(!WeaponGuideVisible||camera==null||Event.current.type!=EventType.Repaint)return;
            var old=GUI.color;int state=WeaponGuideState;
            Color color=state==2?new Color(1,.23f,.2f,.65f):state==1?new Color(1,.72f,.3f,.42f)
                :SelectedAmmo==AmmoKind.Rifle?new Color(.78f,.42f,1,.6f):new Color(1,.82f,.22f,.6f);
            Vector2 origin=aim.MuzzlePosition;GUI.color=color;
            if(SelectedAmmo==AmmoKind.Rifle)
            {
                // No raycast: the rifle continues through terrain and targets.
                if(state==0)GuideLine(camera,origin,RifleGuideEnd,1);
                else for(float d=0;d<RifleRange;d+=.5f)GuideLine(camera,origin+aim.AimDirection*d,origin+aim.AimDirection*Mathf.Min(d+.25f,RifleRange),1);
                var end=camera.WorldToScreenPoint(RifleGuideEnd);
                if(end.z>0)CursorArc(new Vector2(end.x,Screen.height-end.y),3,0,360,1);
            }
            else
            {
                var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
                for(int i=0;i<=GuideSamples;i++)shotgunGuide[i]=ShotgunGuidePoint(Mathf.Lerp(-ShotgunSpread*.5f,ShotgunSpread*.5f,i/(float)GuideSamples),players);
                // Envelope of all possible pellet angles, not a guaranteed filled hit area.
                for(int i=1;i<=GuideSamples;i++)if(state==0||i%2==1)GuideLine(camera,shotgunGuide[i-1],shotgunGuide[i],1.5f);
                color.a*=.55f;GUI.color=color;
                GuideLine(camera,origin,shotgunGuide[0],1);GuideLine(camera,origin,shotgunGuide[GuideSamples],1);
            }
            GUI.color=old;
        }
        static void GuideLine(Camera camera,Vector2 from,Vector2 to,float width)
        {
            var a=camera.WorldToScreenPoint(from);var b=camera.WorldToScreenPoint(to);
            if(a.z>0&&b.z>0&&Vector2.SqrMagnitude((Vector2)a-(Vector2)b)>.001f)
                CursorLine(new Vector2(a.x,Screen.height-a.y),new Vector2(b.x,Screen.height-b.y),width);
        }
    }
}
