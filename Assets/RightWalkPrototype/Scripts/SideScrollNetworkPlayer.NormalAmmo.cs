using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        readonly NetworkVariable<float> normalAmmo=new(0);
        double lastNormalShot=double.NegativeInfinity;
        bool normalAmmoInitialized;
        public bool NormalAmmoEnabled=>ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.IsSpawned&&ArenaObjectiveMatch.Instance.RoomFlow&&ArenaObjectiveMatch.Instance.NormalAmmoRules.Capacity>0;
        public int NormalCapacity=>NormalAmmoEnabled?ArenaObjectiveMatch.Instance.NormalAmmoRules.Capacity:0;
        public float NormalShotInterval=>NormalAmmoEnabled?ArenaObjectiveMatch.Instance.NormalAmmoRules.ShotInterval:shots.shotInterval;
        public float NormalAmmo=>normalAmmo.Value;
        public int NormalRounds=>Mathf.FloorToInt(NormalAmmo+.0001f);
        void ResetNormalAmmo()
        {
            if(!IsServer)return;
            normalAmmoInitialized=NormalAmmoEnabled;
            normalAmmo.Value=NormalCapacity;lastNormalShot=double.NegativeInfinity;
        }
        bool SpendNormalAmmo(double now)
        {
            if(!NormalAmmoEnabled)return true;
            if(!normalAmmoInitialized)ResetNormalAmmo();
            if(normalAmmo.Value<1)return false;
            normalAmmo.Value=Mathf.Max(0,normalAmmo.Value-1);lastNormalShot=now;return true;
        }
        void UpdateNormalAmmo()
        {
            if(!IsServer||!NormalAmmoEnabled)return;
            if(!normalAmmoInitialized)ResetNormalAmmo();
            if(IsDead||ArenaObjectiveMatch.CombatBlocked)return;
            var rules=ArenaObjectiveMatch.Instance.NormalAmmoRules;
            if(NetworkManager.ServerTime.Time-lastNormalShot<rules.Delay)return;
            normalAmmo.Value=Mathf.Min(rules.Capacity,normalAmmo.Value+rules.Rate*Time.deltaTime);
        }
        public bool WorldStaminaVisible=>IsSpawned&&IsOwner&&!IsDead&&RelayArenaPrototype.Instance!=null
            &&!ArenaObjectiveMatch.TransitionActive&&!ArenaObjectiveMatch.Ended&&Stamina<MaximumStamina-.01f;
        void DrawWorldStamina()
        {
            if(!WorldStaminaVisible)return;
            var point=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*(crouch.IsCrouched?1.95f:2.45f));
            if(point.z<=0)return;
            var bar=new Rect(point.x-29,Screen.height-point.y+(ArenaObjectiveMatch.PracticeRoom?28:11),58,5);var old=GUI.color;
            GUI.color=new Color(.025f,.04f,.055f,.9f);GUI.DrawTexture(new Rect(bar.x-1,bar.y-1,bar.width+2,bar.height+2),Texture2D.whiteTexture);
            bar.width*=Mathf.Clamp01(Stamina/MaximumStamina);
            GUI.color=SprintExhausted?new Color(1,.65f,.25f):new Color(.25f,.8f,.9f);GUI.DrawTexture(bar,Texture2D.whiteTexture);GUI.color=old;
        }
        Vector2 MouseScreenPoint()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current!=null?Mouse.current.position.ReadValue():new Vector2(-1,-1);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return new Vector2(-1,-1);
#endif
        }
        public bool AmmoCursorVisible
        {
            get
            {
                if(!IsSpawned||!IsOwner||!NormalAmmoEnabled||IsDead||RelayArenaPrototype.FireInputBlocked||!RelayArenaPrototype.HasGameplayFocus)return false;
                var mouse=MouseScreenPoint();return mouse.x>=0&&mouse.y>=0&&mouse.x<Screen.width&&mouse.y<Screen.height;
            }
        }
        void DrawNormalAmmoHud()
        {
            GUILayout.BeginArea(new Rect(Screen.width-255,Screen.height-189,240,174),GUI.skin.box);
            GUILayout.Label("현재: "+ActiveAmmoName+(ActiveAmmoEmpty?" · 탄약 없음":""));
            GUILayout.Label("일반탄  "+NormalRounds+" / "+NormalCapacity+"     S 샷건  "+ShotgunRounds+" / "+ShotgunCapacity);
            GUILayout.Label("R 라이플  "+RifleRounds+" / "+RifleCapacity+"     G 유탄  "+GrenadeRounds+" / "+GrenadeCapacity);
            GUILayout.Label("Q/E 특수탄 선택 · 우클릭 동안 사용");
            GUILayout.Label("특수탄 선택: "+SelectedAmmoName);
            GUILayout.EndArea();
            if(!AmmoCursorVisible||Event.current.type!=EventType.Repaint)return;
            var point=MouseScreenPoint();point.y=Screen.height-point.y;
            DrawAmmoCursor(point,GrenadeActive?GrenadeRounds:RifleActive?RifleRounds:InstantShotgunActive?InstantShotgunRounds:ShotgunActive?ShotgunRounds:NormalAmmo,GrenadeActive?GrenadeCapacity:RifleActive?RifleCapacity:SpecialShotActive?ShotgunCapacity:NormalCapacity,SpecialShotActive,InstantShotgunActive,RifleActive);
        }
        // Screen y grows downwards: -90 degrees is twelve o'clock and positive
        // sweep is clockwise. Fractional ammo fills the next segment gradually.
        void DrawAmmoCursor(Vector2 centre,float ammo,int capacity,bool shotgun,bool instant,bool rifle)
        {
            var previous=GUI.color;float alpha=ammo>=capacity-.001f?.45f:1;
            Color ink=GrenadeActive?new Color(.65f,1,.3f,alpha):rifle?new Color(.8f,.4f,1,alpha):instant?new Color(1,.82f,.18f,alpha):shotgun?new Color(1,.76f,.27f,alpha):new Color(.3f,.9f,1,alpha);
            int segments=Mathf.Clamp(capacity,1,32);float units=capacity/(float)segments,span=360f/segments,gap=Mathf.Min(5,span*.22f);
            for(int i=0;i<segments;i++)
            {
                float start=-90+i*span+gap*.5f,length=span-gap;
                GUI.color=ammo<1?new Color(1,.18f,.12f,.75f+.2f*Mathf.Sin(Time.unscaledTime*6)):new Color(.02f,.04f,.06f,.85f);
                CursorArc(centre,22,start,length,4);
                float fill=Mathf.Clamp01((ammo-i*units)/units);
                if(fill>0){GUI.color=ink;CursorArc(centre,22,start,length*fill,2.5f);}
            }
            bool blocked=FireCursorBlocked;
            float scale=blocked?.65f:1;
            if(blocked&&!ActiveAmmoEmpty&&FireWaitRemaining>0)
            {
                GUI.color=new Color(.12f,.12f,.15f,.85f);CursorArc(centre,12,-90,360,2);
                GUI.color=new Color(.85f,.85f,.9f,.8f);CursorArc(centre,12,-90,360*FireWaitProgress,2);
            }
            GUI.color=ActiveAmmoEmpty?new Color(1,.2f,.15f,.95f):blocked?new Color(.6f,.6f,.65f,.5f):new Color(1,1,1,.95f);
            CursorLine(centre+new Vector2(-8,0)*scale,centre+new Vector2(-4,0)*scale,1.5f);CursorLine(centre+new Vector2(4,0)*scale,centre+new Vector2(8,0)*scale,1.5f);
            CursorLine(centre+new Vector2(0,-8)*scale,centre+new Vector2(0,-4)*scale,1.5f);CursorLine(centre+new Vector2(0,4)*scale,centre+new Vector2(0,8)*scale,1.5f);
            GUI.color=previous;
        }
        static void CursorArc(Vector2 centre,float radius,float start,float sweep,float width)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(sweep/7));
            Vector2 At(float angle){float r=angle*Mathf.Deg2Rad;return centre+new Vector2(Mathf.Cos(r),Mathf.Sin(r))*radius;}
            for(int i=0;i<steps;i++)CursorLine(At(start+sweep*i/steps),At(start+sweep*(i+1)/steps),width);
        }
        static void CursorLine(Vector2 from,Vector2 to,float width)
        {
            var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg,from);
            GUI.DrawTexture(new Rect(from.x,from.y-width*.5f,Vector2.Distance(from,to),width),Texture2D.whiteTexture);GUI.matrix=matrix;
        }
    }
}
