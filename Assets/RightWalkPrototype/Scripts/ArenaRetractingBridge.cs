using UnityEngine;
using Unity.Netcode;

namespace RightWalkPrototype
{
    // Optional extra crossing. Shared server time keeps collision and visuals together.
    [DefaultExecutionOrder(51)]
    public sealed class ArenaRetractingBridge : MonoBehaviour
    {
        public float left=-24,right=-20,phaseOffset=7;
        public float floor;
        public bool lethalPit=true;
        public string detour="UPPER ROUTE";
        public Transform westLeaf,eastLeaf;
        public SpriteRenderer warning;
        public TextMesh status;
        BoxCollider2D westCollider,eastCollider;
        public float CycleTime {get;private set;}
        public bool Retracted=>CycleTime>=10;
        public bool UnsafeLanding(Vector2 point)=>lethalPit&&point.x>left&&point.x<right&&point.y<floor-2.3f;
        void Awake()
        {
            westCollider=westLeaf.GetComponent<BoxCollider2D>();
            eastCollider=eastLeaf.GetComponent<BoxCollider2D>();
        }
        void Update()
        {
            var manager=NetworkManager.Singleton;
            double clock=manager!=null&&manager.IsListening?ArenaClock.Now(manager):Time.timeAsDouble;
            CycleTime=(float)((clock+phaseOffset)%14);
            float opened=CycleTime>=13.4f?(14-CycleTime)/.6f:CycleTime>=10?Mathf.Clamp01((CycleTime-10)/.6f):0;
            float width=(right-left)*.5f*(1-Mathf.Clamp01(opened));
            westLeaf.localScale=eastLeaf.localScale=new Vector3(width,.18f,1);
            westLeaf.position=new Vector3(left+width*.5f,floor-.09f,0);
            eastLeaf.position=new Vector3(right-width*.5f,floor-.09f,0);
            westCollider.enabled=eastCollider.enabled=width>0;
            var color=Retracted?new Color(1,.25f,.2f):CycleTime>=8?
                Color.Lerp(new Color(1,.7f,.2f),new Color(.35f,.18f,.07f),Mathf.PingPong(Time.unscaledTime*3,1)):new Color(.25f,.9f,.75f);
            warning.color=color;status.color=color;
            status.text=CycleTime>=13.4f?"CLOSING":Retracted?"OPEN / USE "+detour:CycleTime>=8?"WARNING / RETRACTING":"CROSSING SAFE";
            if(!lethalPit||manager==null||!manager.IsServer)return;
            foreach(var player in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
                if(player.IsSpawned&&player.HasCurrentLifePose&&!player.IsDead&&player.transform.position.x>left&&player.transform.position.x<right&&player.transform.position.y<floor-2.4f)
                    player.ReceiveServerDamage(player.MaxHealth,player.LifeVersion,null,true);
        }
    }
}
