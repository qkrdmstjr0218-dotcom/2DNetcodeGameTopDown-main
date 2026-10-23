using UnityEngine;
using Unity.Netcode;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RightWalkPrototype
{
    // Scene-only prototype. Existing trial scenes do not contain this component.
    [DefaultExecutionOrder(50)]
    public sealed partial class RelayArenaPrototype : MonoBehaviour
    {
        public Transform bridgeLeft,bridgeRight;
        public BoxCollider2D bridgeCollider;
        private BoxCollider2D bridgeRightCollider;
        public SpriteRenderer warningLight;
        public Camera arenaCamera;
        public bool overview;
        public float pitLeft=12,pitRight=16;
        public float weaponRange=24f;
        public float walkSpeed=2.4f,runSpeed=3.6f,crouchSpeed=.9f,jumpSpeed=6.5f;
        public float staminaMaximum=100,staminaDrain=25,staminaRecovery=20,staminaRecoveryDelay=1,staminaResume=20;
        public static RelayArenaPrototype Instance {get;private set;}
        public bool MenuOpen {get;private set;}
        public static bool ControlsBlocked=>Instance!=null&&(Instance.MenuOpen||Instance.overview||ArenaObjectiveMatch.Ended||ArenaObjectiveMatch.CountingDown||ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.Transitioning);
        public static bool HasGameplayFocus
        {
            get
            {
                if(!Application.isFocused)return false;
#if UNITY_EDITOR
                var window=UnityEditor.EditorWindow.focusedWindow;
                return window!=null&&window.GetType().Name=="GameView";
#else
                return true;
#endif
            }
        }
        public static bool LiveInputBlocked=>ControlsBlocked||Instance!=null&&!HasGameplayFocus;
        public static bool FireInputBlocked=>LiveInputBlocked||ArenaObjectiveMatch.CombatBlocked||ArenaObjectiveMatch.RoomPointerOverUI||Instance!=null&&Instance.waitForMouseRelease;
        public float CycleTime {get;private set;}
        public bool BridgeOpen=>CycleTime>=10;
        private float desiredSize;
        private bool waitForMouseRelease;
        private Vector3 cameraPosition;
        private bool cameraReady;
        private ArenaMapSettings map;
        private ArenaRetractingBridge[] extraBridges;
        private void Awake()
        {
            map=GetComponent<ArenaMapSettings>();
            extraBridges=GetComponentsInChildren<ArenaRetractingBridge>();
            ArenaReadyRoom.Build(transform);
            // Keep the serialized collider reference, but let each visible leaf
            // carry its own collision instead of toggling the entire crossing.
            bridgeCollider.transform.SetParent(bridgeLeft,false);
            bridgeCollider.transform.localPosition=Vector3.zero;
            bridgeCollider.transform.localRotation=Quaternion.identity;
            bridgeCollider.transform.localScale=Vector3.one;
            bridgeCollider.size=Vector2.one;bridgeCollider.offset=Vector2.zero;
            bridgeRightCollider=bridgeRight.gameObject.AddComponent<BoxCollider2D>();
            bridgeRightCollider.sharedMaterial=bridgeCollider.sharedMaterial;
            bridgeRight.gameObject.layer=bridgeCollider.gameObject.layer;
        }
        private void SetBridgeOpening(float opened)
        {
            float width=(pitRight-pitLeft)*.5f*(1-Mathf.Clamp01(opened));
            bridgeLeft.localScale=new Vector3(width,.18f,1);
            bridgeRight.localScale=new Vector3(width,.18f,1);
            bridgeLeft.position=new Vector3(pitLeft+width*.5f,-.09f,0);
            bridgeRight.position=new Vector3(pitRight-width*.5f,-.09f,0);
            bridgeCollider.enabled=bridgeRightCollider.enabled=width>0;
        }
        private void OnEnable(){Instance=this;}
        private void OnDisable(){if(Instance==this)Instance=null;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        public void SetMenuOpen(bool value)
        {
            MenuOpen=value;if(!value)waitForMouseRelease=true;
            if(value){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        }
        public bool IsSafeLanding(Vector2 point)
        {
            if(point.x>pitLeft&&point.x<pitRight&&point.y<-2.3f)return false;
            if(extraBridges!=null)foreach(var bridge in extraBridges)if(bridge.UnsafeLanding(point))return false;
            return true;
        }
        private double Clock=>NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsListening?
            NetworkManager.Singleton.ServerTime.Time:Time.timeAsDouble;
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if(HasGameplayFocus&&Keyboard.current!=null)
            {
                if(Keyboard.current.escapeKey.wasPressedThisFrame)SetMenuOpen(!MenuOpen);
                if(!MenuOpen&&!ArenaObjectiveMatch.InReadyRoom&&!ArenaObjectiveMatch.CountingDown&&Keyboard.current.tabKey.wasPressedThisFrame)overview=!overview;
            }
            if(Mouse.current==null||!Mouse.current.leftButton.isPressed)waitForMouseRelease=false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if(HasGameplayFocus&&Input.GetKeyDown(KeyCode.Escape))SetMenuOpen(!MenuOpen);
            if(HasGameplayFocus&&!MenuOpen&&!ArenaObjectiveMatch.InReadyRoom&&!ArenaObjectiveMatch.CountingDown&&Input.GetKeyDown(KeyCode.Tab))overview=!overview;
            if(!Input.GetMouseButton(0))waitForMouseRelease=false;
#endif
            CycleTime=(float)(Clock%14);
            // Eight seconds safe, two seconds warning, four seconds retracted.
            float opened=CycleTime>=13.4f?(14-CycleTime)/.6f:CycleTime>=10?Mathf.Clamp01((CycleTime-10)/.6f):0;
            SetBridgeOpening(opened);
            if(warningLight!=null)warningLight.color=BridgeOpen?new Color(1,.25f,.20f):
                CycleTime>=8?Color.Lerp(new Color(1,.7f,.2f),new Color(.35f,.18f,.07f),Mathf.PingPong(Time.unscaledTime*3,1)):new Color(.25f,.9f,.75f);
            var manager=NetworkManager.Singleton;
            if(!HasGameplayFocus||manager==null||!manager.IsConnectedClient)waitForMouseRelease=true;
            bool capture=HasGameplayFocus&&manager!=null&&manager.IsConnectedClient&&!MenuOpen&&!overview&&!ArenaObjectiveMatch.CombatBlocked&&!ArenaObjectiveMatch.InReadyRoom&&!WantsSpectator;
            if(WantsSpectator)waitForMouseRelease=true;
            Cursor.lockState=capture?CursorLockMode.Confined:CursorLockMode.None;
            var local=LocalPlayer();Cursor.visible=local==null||!local.AmmoCursorVisible;
            if(manager==null||!manager.IsServer)return;
            foreach(var player in FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None))
            {
                Vector3 position=player.transform.position;
                if(player.IsSpawned&&player.HasCurrentLifePose&&!player.IsDead&&position.x>pitLeft&&position.x<pitRight&&position.y<-2.4f)
                    player.ReceiveServerDamage(player.MaxHealth,player.LifeVersion,null,true);
            }
        }
        private void LateUpdate()
        {
            if(arenaCamera==null)return;
            if(WantsSpectator)overview=false;
            if(ArenaObjectiveMatch.InReadyRoom)
            {arenaCamera.orthographicSize=Mathf.Max(6.5f,11/arenaCamera.aspect);cameraPosition=new Vector3(100,3.7f,-10);arenaCamera.transform.position=cameraPosition;cameraReady=false;return;}
            var target=map!=null?new Vector3(map.overviewCentre.x,map.overviewCentre.y,-10):new Vector3(0,3,-10);
            if(!overview)
            {
                var owner=LocalPlayer();var viewed=WantsSpectator?SpectatorTarget():owner;
                if(viewed!=null)target=viewed.transform.position+new Vector3(0,1.3f,-10);
                else if(owner!=null)target=owner.transform.position+new Vector3(0,1.3f,-10);
                float halfWidth=4.8f*arenaCamera.aspect;
                if(map==null){target.x=Mathf.Clamp(target.x,-24+halfWidth,24-halfWidth);target.y=Mathf.Clamp(target.y,2.7f,7.4f);}
                else
                {
                    var b=map.cameraBounds;
                    target.x=b.width<=halfWidth*2?b.center.x:Mathf.Clamp(target.x,b.xMin+halfWidth,b.xMax-halfWidth);
                    target.y=b.height<=9.6f?b.center.y:Mathf.Clamp(target.y,b.yMin+4.8f,b.yMax-4.8f);
                }
            }
            desiredSize=overview?(map!=null?Mathf.Max(map.cameraBounds.height*.5f,map.cameraBounds.width*.5f/arenaCamera.aspect):Mathf.Max(7,25/arenaCamera.aspect)):4.8f;
            // Sole writer, before aim (order 100). Never aim through an intermediate camera.
            arenaCamera.orthographicSize=desiredSize;
            if(!cameraReady||overview){cameraPosition=target;cameraReady=true;}
            else cameraPosition=Vector3.Lerp(cameraPosition,target,1-Mathf.Exp(-8*Time.deltaTime));
            arenaCamera.transform.position=cameraPosition;
        }
        private void OnGUI()
        {
            if(SideScrollNetworkSession.LobbyVisible||ArenaObjectiveMatch.TransitionActive||ArenaObjectiveMatch.InReadyRoom&&!MenuOpen)return;
            if(!ArenaObjectiveMatch.InReadyRoom)
            {
            GUILayout.BeginArea(new Rect(Screen.width*.5f-180,12,360,75),GUI.skin.box);
            GUILayout.Label(map!=null?map.displayName:"중계 시설 07 · 3층 순환 전장");
            string state=BridgeOpen?"통행 불가":CycleTime>=8?"곧 수납됩니다":"통행 가능";
            float remaining=BridgeOpen?14-CycleTime:10-CycleTime;
            bool extra=extraBridges!=null&&extraBridges.Length>0;
            GUILayout.Label((extra?"[디버그] 동쪽: ":"동쪽 다리: ")+state+" · "+Mathf.CeilToInt(remaining)+"초"+(extra?"":" / Tab: 전체 지도"));
            if(extra)
            {
                var bridge=extraBridges[0];
                string westState=bridge.Retracted?"통행 불가":bridge.CycleTime>=8?"곧 수납됩니다":"통행 가능";
                float westRemaining=bridge.Retracted?14-bridge.CycleTime:10-bridge.CycleTime;
                GUILayout.Label("서쪽: "+westState+" · "+Mathf.CeilToInt(westRemaining)+"초 / Tab: 전체 지도");
            }
            GUILayout.EndArea();
            }
            if(MenuOpen)
            {
                GUILayout.BeginArea(new Rect(Screen.width*.5f-160,Screen.height*.5f-95,320,190),GUI.skin.box);
                GUILayout.Label("게임 메뉴");
                GUILayout.Label("다른 플레이어의 게임은 계속 진행됩니다.");
                if(GUILayout.Button("계속하기 (ESC)"))SetMenuOpen(false);
                if(GUILayout.Button("연결 종료"))
                {FindFirstObjectByType<SideScrollNetworkSession>()?.DisconnectLocal();MenuOpen=false;}
                GUILayout.EndArea();
            }
            else DrawSpectator();
        }
    }
}
