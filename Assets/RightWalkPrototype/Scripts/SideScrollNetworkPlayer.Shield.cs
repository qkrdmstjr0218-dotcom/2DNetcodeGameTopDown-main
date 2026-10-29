using UnityEngine;
using Unity.Netcode;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        public const int ShieldCapacity=50;
        public const float ShieldDuration=15;
        public struct ShieldState:INetworkSerializable,System.IEquatable<ShieldState>
        {
            public int Amount;public uint Generation;public double Until;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Amount);s.SerializeValue(ref Generation);s.SerializeValue(ref Until);}
            public bool Equals(ShieldState o)=>Amount==o.Amount&&Generation==o.Generation&&Until==o.Until;
        }
        readonly NetworkVariable<ShieldState> shield=new();
        LineRenderer shieldOutline;Material shieldMaterial;
        SpriteRenderer shieldSurface;Sprite shieldSprite;int shieldLast;float shieldFlash;
        bool ShieldActive=>IsSpawned&&!IsDead&&shield.Value.Generation==LifeVersion&&ArenaClock.Now(NetworkManager)<shield.Value.Until
            &&ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.State.Value.Phase==ArenaObjectiveMatch.Stage.Playing;
        public int Shield=>ShieldActive?shield.Value.Amount:0;
        public float ShieldRemaining=>Shield>0?Mathf.Max(0,(float)(shield.Value.Until-ArenaClock.Now(NetworkManager))):0;
        public bool GrantShieldServer(uint generation)
        {
            var match=ArenaObjectiveMatch.Instance;
            if(!IsServer||!IsSpawned||IsDead||!HasCurrentLifePose||generation!=LifeVersion||match==null||!match.RoomFlow||match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Playing)return false;
            shield.Value=new ShieldState{Amount=ShieldCapacity,Generation=LifeVersion,Until=ArenaClock.Now(NetworkManager)+ShieldDuration};return true;
        }
        int AbsorbShield(int damage)
        {
            int absorbed=Mathf.Min(damage,Shield);if(absorbed<=0)return damage;
            var state=shield.Value;state.Amount-=absorbed;shield.Value=state;return damage-absorbed;
        }
        void UpdateShield()
        {
            if(IsServer&&shield.Value.Amount>0&&!ShieldActive)shield.Value=default;
            bool visible=Shield>0&&!ArenaObjectiveMatch.TransitionActive;
            if(!visible){if(shieldOutline!=null)shieldOutline.enabled=false;if(shieldSurface!=null)shieldSurface.enabled=false;shieldLast=0;return;}
            var effect=RelayArenaPrototype.Instance!=null?RelayArenaPrototype.Instance.shieldEffectShader:null;
            if(effect!=null&&effect.isSupported)
            {
                if(shieldSurface==null){var surface=new GameObject("Energy shield");surface.transform.SetParent(transform,false);shieldSurface=surface.AddComponent<SpriteRenderer>();var texture=Texture2D.whiteTexture;shieldSprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),texture.width);shieldSurface.sprite=shieldSprite;shieldMaterial=new Material(effect);shieldSurface.sharedMaterial=shieldMaterial;shieldSurface.sortingOrder=90;}
                float h=crouch!=null&&crouch.IsCrouched?1.55f:2.15f;shieldSurface.enabled=true;shieldSurface.transform.localPosition=new Vector3(0,h*.5f,0);shieldSurface.transform.localScale=new Vector3(1.68f,h+.48f,1);
                if(Shield!=shieldLast)shieldFlash=1;shieldLast=Shield;shieldFlash=Mathf.MoveTowards(shieldFlash,0,Time.deltaTime*4);
                shieldMaterial.SetFloat("_Strength",Shield/(float)ShieldCapacity);shieldMaterial.SetFloat("_Pulse",Time.time);shieldMaterial.SetFloat("_Flash",shieldFlash);shieldSurface.color=new Color(1,1,1,ShieldRemaining<3?.65f+.25f*Mathf.Sin(Time.time*10):1);return;
            }
            if(shieldOutline==null)
            {
                var go=new GameObject("Protective shield");go.transform.SetParent(transform,false);
                shieldOutline=go.AddComponent<LineRenderer>();shieldOutline.useWorldSpace=false;shieldOutline.loop=true;shieldOutline.positionCount=48;
                shieldMaterial=new Material(Shader.Find("Sprites/Default"));shieldOutline.sharedMaterial=shieldMaterial;shieldOutline.sortingOrder=90;
            }
            shieldOutline.enabled=true;shieldOutline.widthMultiplier=.035f;
            var color=new Color(.35f,.65f,1,ShieldRemaining<3?.45f+.3f*Mathf.Sin(Time.unscaledTime*12):.7f);
            shieldOutline.startColor=shieldOutline.endColor=color;
            float height=crouch!=null&&crouch.IsCrouched?1.55f:2.15f;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;shieldOutline.SetPosition(i,new Vector3(Mathf.Cos(a)*.72f,height*.5f+Mathf.Sin(a)*(height*.5f+.1f),0));}
        }
        void DrawShieldHud()
        {
            if(Shield<=0)return;
            var at=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*(crouch.IsCrouched?1.95f:2.45f));if(at.z<=0)return;
            var old=GUI.color;var bar=new Rect(at.x-38,Screen.height-at.y+20,76,5);
            GUI.color=new Color(.04f,.07f,.16f,.95f);GUI.DrawTexture(bar,Texture2D.whiteTexture);
            bar.width*=Shield/(float)ShieldCapacity;GUI.color=new Color(.35f,.65f,1);GUI.DrawTexture(bar,Texture2D.whiteTexture);GUI.color=old;
            if(IsLocalHuman)GUI.Label(new Rect(at.x-65,Screen.height-at.y-44,130,22),"보호막 "+Shield+" · "+ShieldRemaining.ToString("0")+"초",new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.55f,.8f,1)}});
        }
        void ClearShieldVisual(){if(shieldSurface!=null)Destroy(shieldSurface.gameObject);if(shieldSprite!=null)Destroy(shieldSprite);if(shieldOutline!=null)Destroy(shieldOutline.gameObject);if(shieldMaterial!=null)Destroy(shieldMaterial);}
    }
}
