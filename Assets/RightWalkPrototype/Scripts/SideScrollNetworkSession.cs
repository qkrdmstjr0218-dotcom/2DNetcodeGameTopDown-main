using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkSession : MonoBehaviour
    {
        public GameObject targetPrefab;
        public GameObject tutorialTargetPrefab;
        public GameObject arenaObjectivePrefab;
        public Transform[] playerSpawns;
        public bool useReadyRoom=true;
        public string ConnectionNotice {get;private set;}="";
        private NetworkManager sessionManager;
        private bool connectedToHost, intentionalDisconnect;
        private void ClientConnected(ulong clientId)
        {
            if(sessionManager!=null&&clientId==sessionManager.LocalClientId)
            {connectedToHost=true;ConnectionNotice="";}
        }
        private void ClientStopped(bool wasHost)
        {
            StopTutorial();
            ConnectionNotice=wasHost||intentionalDisconnect?"연결을 종료했습니다.":
                connectedToHost?"호스트와의 연결이 종료되었습니다.\n다시 접속할 수 있습니다.":"호스트에 연결하지 못했습니다.\n호스트 실행 여부를 확인해 주세요.";
            if(!wasHost&&!intentionalDisconnect&&!connectedToHost&&sessionManager!=null&&!string.IsNullOrEmpty(sessionManager.DisconnectReason))ConnectionNotice=sessionManager.DisconnectReason;
            connectedToHost=false;
            if(onlineConnection&&!intentionalDisconnect&&!onlineLeaving&&isActiveAndEnabled)StartCoroutine(WaitForOnlineClosure());
        }
        public void DisconnectLocal()
        {
            StopTutorial();
            if(onlineConnection||onlineSession!=null){_=LeaveOnlineRoomAsync();return;}
            intentionalDisconnect=true;
            NetworkManager.Singleton?.Shutdown();
        }
        private void SpawnTargets()
        {
            if(arenaObjectivePrefab!=null)Instantiate(arenaObjectivePrefab).GetComponent<NetworkObject>().Spawn();
            if(TutorialMode)return;
            if(targetPrefab==null)return;
            foreach(float x in new[]{-3f,3f})
                Instantiate(targetPrefab,new Vector3(x,1.2f,0),Quaternion.identity).GetComponent<NetworkObject>().Spawn();
        }
        public bool SoloPractice {get;private set;}
        public void StartSoloPractice(){StopTutorial();StartLocal(true,true);}
        public void StartLocal(bool host){StopTutorial();StartLocal(host,false);}
        public bool TutorialMode=>ArenaTutorial.Active&&ArenaTutorial.Instance.Session==this;
        public void StartTutorial()
        {
            var n=NetworkManager.Singleton;if(n==null||n.IsListening||n.ShutdownInProgress||OnlineBusy||onlineSession!=null)return;
            ArenaTutorial.Begin(this);StartLocal(true,true);
        }
        void StopTutorial(){if(TutorialMode)ArenaTutorial.Instance.End();}
        public void TutorialToPractice()=>StartCoroutine(SwitchTutorialToPractice());
        System.Collections.IEnumerator SwitchTutorialToPractice()
        {
            DisconnectLocal();while(NetworkManager.Singleton!=null&&(NetworkManager.Singleton.IsListening||NetworkManager.Singleton.ShutdownInProgress))yield return null;
            StartSoloPractice();
        }
        void StartLocal(bool host,bool solo)
        {
            var manager=NetworkManager.Singleton;
            if(manager==null||manager.IsListening||manager.ShutdownInProgress)return;
            if(OnlineBusy||onlineSession!=null)return;
            ArenaClock.Reset();SoloPractice=solo;PrepareConnection(false);
            // Solo has no remote clients: let the OS choose a free loopback port.
            // Keep the known port only for the explicit local host/client test mode.
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1",solo?(ushort)0:(ushort)7783,"127.0.0.1");
            bool started=host?manager.StartHost():manager.StartClient();
            if(!started){StopTutorial();SoloPractice=false;ConnectionNotice="연결을 시작하지 못했습니다. 다시 시도해 주세요.";}
        }
        private void ApproveRoomConnection(NetworkManager.ConnectionApprovalRequest request,NetworkManager.ConnectionApprovalResponse response)
        {
            var match=ArenaObjectiveMatch.Instance;
            bool busy=match!=null&&match.IsSpawned&&match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Waiting;
            bool full=NetworkManager.Singleton.ConnectedClients.Count>=4;
            bool compatible=!onlineConnection||System.Text.Encoding.UTF8.GetString(request.Payload??System.Array.Empty<byte>())==OnlineRoomKind;
            bool soloGuest=SoloPractice&&request.ClientNetworkId!=Unity.Netcode.NetworkManager.ServerClientId;
            response.Approved=!soloGuest&&!busy&&!full&&compatible;response.CreatePlayerObject=response.Approved;response.Pending=false;
            response.Reason=soloGuest?"혼자 연습 중인 방입니다.":!compatible?"게임 버전이 다릅니다. 같은 빌드로 접속해 주세요.":busy?"경기가 진행 중입니다. 대기방으로 돌아온 뒤 참가해 주세요.":full?"방이 가득 찼습니다. 최대 4명까지 참가할 수 있습니다.":"";
        }
        private void Start()
        {
            var args=System.Environment.GetCommandLineArgs();
            // NetworkConfig.Prefabs.Add alone is not a persisted prefab-list asset.
            if(targetPrefab!=null && !NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(targetPrefab))
                NetworkManager.Singleton.AddNetworkPrefab(targetPrefab);
            if(tutorialTargetPrefab!=null&&!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(tutorialTargetPrefab))
                NetworkManager.Singleton.AddNetworkPrefab(tutorialTargetPrefab);
            if(arenaObjectivePrefab!=null&&!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(arenaObjectivePrefab))NetworkManager.Singleton.AddNetworkPrefab(arenaObjectivePrefab);
            NetworkManager.Singleton.OnServerStarted+=SpawnTargets;
            sessionManager=NetworkManager.Singleton;
            sessionManager.OnClientConnectedCallback+=ClientConnected;
            sessionManager.OnClientStopped+=ClientStopped;
            if(System.Array.IndexOf(args,"--side-host")>=0)StartLocal(true);
            else if(System.Array.IndexOf(args,"--side-client")>=0)StartLocal(false);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(System.Array.IndexOf(args,"--side-motion-test")>=0)StartCoroutine(SideScrollNetworkMotionVerification.Run(this));
#endif
            if(System.Array.IndexOf(args,"--side-test")>=0||System.Array.IndexOf(args,"--side-damage-test")>=0||System.Array.IndexOf(args,"--side-air-test")>=0)StartCoroutine(CheckTwoPlayers());
        }
        private System.Collections.IEnumerator CheckTwoPlayers()
        {
            float deadline=Time.realtimeSinceStartup+35f;
            int sent=0;
            bool damageTest=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--side-damage-test")>=0;
            bool airTest=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--side-air-test")>=0;
            float next=0f;
            while(Time.realtimeSinceStartup<deadline)
            {
                var players=FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None);
                if(players.Length==2)
                {
                    foreach(var player in players)
                    {
                        if(player.IsOwner && sent<5 && Time.realtimeSinceStartup>=next)
                        {
                            var aim=player.GetComponentInChildren<RightFacingAimRig>();
                            if(airTest)
                            {
                                var movement=player.GetComponent<SideScrollMapPreview>();movement.enabled=false;movement.SetRemoteGrounded(false);
                                player.GetComponent<Rigidbody2D>().simulated=false;
                                player.transform.position=new Vector3(player.OwnerClientId==0 ? -1.5f : 1.5f,1.1f,0);
                                aim.SetAimDirection(Vector2.down);
                                player.GetComponent<AirDownShotPose>().ApplyPose();
                            }
                            else aim.SetAimTarget(damageTest ? new Vector2(player.OwnerClientId==0 ? -3f : 3f,1.2f) : (Vector2)player.transform.position+new Vector2(player.OwnerClientId==0 ? 5f : -5f,1.5f));
                            aim.GetComponent<RightFacingShotPreview>().Fire();sent++;next=Time.realtimeSinceStartup+.3f;
                        }
                        var targets=FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None);
                        bool damagePassed=targets.Length==2&&System.Array.TrueForAll(targets,t=>t.Health.Value==0&&t.Hits.Value==3&&!t.GetComponent<Collider2D>().enabled);
                        var remoteAim=player.GetComponentInChildren<RightFacingAimRig>();
                        bool airPassed=remoteAim.AirborneDownAim&&Vector2.Dot(remoteAim.AimDirection,Vector2.down)>.9999f&&player.GetComponent<SpriteRenderer>().sprite==player.GetComponent<AirDownShotPose>().tuckedLegs;
                        if(!player.IsOwner && player.ReceivedShots>=3 && sent>=3 && (!damageTest||damagePassed) && (!airTest||airPassed))
                        {
                            System.IO.Directory.CreateDirectory("Temp");
                            System.IO.File.WriteAllText("Temp/side-network-"+(NetworkManager.Singleton.IsHost ? "host" : "client")+".txt","PASS: players=2; remote shots="+player.ReceivedShots+"; local requests="+sent+(damageTest ? "; both targets HP=0 hits=3 collision disabled" : "")+(airTest ? "; remote vertical barrel + tucked legs verified" : ""));
                            yield return new WaitForSecondsRealtime(3f);
                            Application.Quit(0);yield break;
                        }
                    }
                }
                yield return null;
            }
            System.IO.Directory.CreateDirectory("Temp");
            System.IO.File.WriteAllText("Temp/side-network-"+(NetworkManager.Singleton.IsHost ? "host" : "client")+".txt","FAIL: two-client shot replication timed out");
            Application.Quit(2);
        }
        private void OnGUI()
        {
            if(TutorialMode||ArenaCombatHud.Active||ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.IsSpawned&&ArenaObjectiveMatch.Instance.RoomFlow)return;
            if(ArenaObjectiveMatch.TransitionActive)return;
            var manager=NetworkManager.Singleton;
            if(manager==null)return;
            if(!manager.IsListening&&useReadyRoom&&RelayArenaPrototype.Instance!=null){DrawOnlineLobby();return;}
            if(ArenaCombatHud.UsesGameUi&&manager.IsListening)return;
            GUILayout.BeginArea(new Rect(Screen.width-230,15,215,manager.IsListening&&ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.RoomFlow?100:160),GUI.skin.box);
            if(!manager.IsListening)
            {
                if(!string.IsNullOrEmpty(ConnectionNotice))
                    GUILayout.Label(ConnectionNotice,new GUIStyle(GUI.skin.label){wordWrap=true});
                if(GUILayout.Button("Local Host"))StartLocal(true);
                if(GUILayout.Button("Local Client"))StartLocal(false);
            }
            else
            {
                if(ArenaObjectiveMatch.Instance!=null&&ArenaObjectiveMatch.Instance.RoomFlow)
                {GUILayout.Label(manager.IsHost?"내가 방장":"참가 중");GUILayout.Label(TutorialMode?"튜토리얼 · 오프라인":onlineConnection?"인터넷 · "+OnlineRoomName:SoloPractice?"혼자 연습 · 로컬":"로컬 테스트 연결");if(GUILayout.Button(manager.IsHost?"방 닫고 나가기":"방 나가기"))DisconnectLocal();GUILayout.EndArea();return;}
                GUILayout.Label(manager.IsHost ? "Host : 7783" : "Client : 7783");
                GUILayout.Label("Players: "+FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None).Length);
                if(manager.IsServer && GUILayout.Button("Reset Targets"))
                    foreach(var target in FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None))target.ResetTarget();
                if(GUILayout.Button("Disconnect"))DisconnectLocal();
            }
            GUILayout.EndArea();
        }
        private void OnDestroy()
        {
            StopTutorial();
            CleanupOnlineOnDestroy();
            if(sessionManager!=null)
            {
                sessionManager.OnServerStarted-=SpawnTargets;
                sessionManager.OnClientConnectedCallback-=ClientConnected;
                sessionManager.OnClientStopped-=ClientStopped;
            }
        }
    }
}
