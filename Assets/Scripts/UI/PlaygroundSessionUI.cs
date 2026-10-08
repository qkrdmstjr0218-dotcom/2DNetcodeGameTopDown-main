using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using TopDownShooter.Networking;

namespace TopDownShooter.Networking
{
    public sealed class PlaygroundSessionUI : MonoBehaviour
    {
        private string code = "", roomCode = "", message = "Choose Solo or a multiplayer connection.";
        private bool busy;
        private double timeoutAt;
        private NetworkManager manager;
        private GUIStyle text, title;
        private bool soloSession;
        private bool closeUp;
        private float regularZoom;
        public void StartSolo()
        {
            // Port zero lets the OS choose a free port, independent of local multiplayer hosts.
            StartLocal(true, 0);
            soloSession = true;
        }
        public static bool PointerOverPanel
        {
            get
            {
                if (Mouse.current == null) return false;
                Vector2 point = Mouse.current.position.ReadValue(); point.y = Screen.height-point.y;
                return new Rect(12,12,340,390).Contains(point) || new Rect(12,Screen.height-134,620,122).Contains(point);
            }
        }
        private void Start()
        {
            manager = NetworkManager.Singleton;
            if (manager == null) { message = "NetworkManager missing."; return; }
            manager.OnClientConnectedCallback += Connected;
            manager.OnClientDisconnectCallback += Disconnected;
            manager.OnTransportFailure += Failed;
        }
        private void Connected(ulong id)
        { if (id == manager.LocalClientId) { busy = false; message = manager.IsHost ? "Host ready. Practice freely or start waves." : "Connected to host."; } }
        private void Disconnected(ulong id)
        { if (id == manager.LocalClientId) { busy = false; message = "Disconnected. " + manager.DisconnectReason; } }
        private void Failed() { busy = false; message = "Transport failed. Check connection / host."; }
        private void Update()
        {
            if (busy && timeoutAt > 0 && Time.realtimeSinceStartupAsDouble > timeoutAt)
            { manager.Shutdown(); busy = false; message = "Connection timed out."; }
        }
        public void StartLocal(bool host, ushort port = 7777)
        {
            if (manager == null || manager.IsListening || busy) return;
            roomCode = "";
            soloSession = false;
            closeUp = false;
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1",port,"127.0.0.1");
            busy = true; timeoutAt = Time.realtimeSinceStartupAsDouble+20;
            message = host ? "Starting local host..." : "Connecting to local host...";
            bool started = host ? manager.StartHost() : manager.StartClient();
            if (!started) { busy = false; message = host ? "Local host failed. Port may be in use; use SOLO or close the other host." : "Could not start connection."; }
        }
        private async Task Relay(bool host)
        {
            if (busy || manager.IsListening) return;
            soloSession=false; closeUp=false;
            busy = true; timeoutAt = 0; message = "Preparing Relay...";
            try
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    AuthenticationService.Instance.SwitchProfile("sf-"+Guid.NewGuid().ToString("N").Substring(0,8));
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                if (this == null || !Application.isPlaying) return;
                var transport = manager.GetComponent<UnityTransport>();
                if (host)
                {
                    var allocation = await RelayService.Instance.CreateAllocationAsync(1);
                    roomCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation,"dtls"));
                }
                else
                {
                    var allocation = await RelayService.Instance.JoinAllocationAsync(code.Trim().ToUpperInvariant());
                    transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation,"dtls"));
                }
                if (this == null || !Application.isPlaying) return;
                timeoutAt = Time.realtimeSinceStartupAsDouble+20;
                message = "Connecting...";
                if (!(host ? manager.StartHost() : manager.StartClient())) throw new Exception("Network start failed.");
            }
            catch (Exception exception) { busy = false; roomCode = ""; message = "Relay: " + exception.Message; Debug.LogWarning(message); }
        }
        private void OnGUI()
        {
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                title = new GUIStyle(text) { fontSize = 19, fontStyle = FontStyle.Bold };
            }
            GUI.Box(new Rect(12,12,340,390),GUIContent.none);
            GUILayout.BeginArea(new Rect(24,22,316,370));
            GUILayout.Label("SF OPERATOR / PLAYGROUND",title);
            GUILayout.Label(message,text);
            GUILayout.Space(8);
            if (manager != null)
            {
                if (!manager.IsListening)
                {
                    GUI.enabled = !busy;
                    if (GUILayout.Button("SOLO / Offline test",GUILayout.Height(30))) StartSolo();
                    if (GUILayout.Button("LOCAL HOST / Multiplayer",GUILayout.Height(30))) StartLocal(true);
                    if (GUILayout.Button("LOCAL JOIN / Second Player",GUILayout.Height(30))) StartLocal(false);
                    if (GUILayout.Button("ONLINE HOST / Relay",GUILayout.Height(30))) _ = Relay(true);
                    code = GUILayout.TextField(code,12);
                    GUI.enabled = !busy && !string.IsNullOrWhiteSpace(code);
                    if (GUILayout.Button("ONLINE JOIN / Code",GUILayout.Height(30))) _ = Relay(false);
                    GUI.enabled = true;
                }
                else
                {
                    GUILayout.Label("Room code: " + (soloSession ? "Solo practice" : roomCode.Length > 0 ? roomCode : "Local / 127.0.0.1:7777"),text);
                    int count = NetworkPlayerController.ActivePlayers.Count;
                    GUILayout.Label("Players: " + count,text);
                    var game = NetworkGameManager.Instance;
                    if (game != null)
                    {
                        GUILayout.Label("Enemies alive: " + game.AliveEnemyCount + "  |  Yet to spawn: " + game.PendingSpawns,text);
                        GUILayout.Label(game.PracticeWaveActive ? "Practice wave active" : "Practice idle - one wave per press",text);
                    }
                    GUI.enabled = manager.IsServer && game != null && !game.PracticeWaveActive;
                    if (GUILayout.Button("START ONE WAVE",GUILayout.Height(30))) game.BeginPracticeWaves();
                    GUI.enabled = manager.IsServer && game != null;
                    if (GUILayout.Button("STOP WAVES / Clear enemies",GUILayout.Height(30))) game.StopPracticeWaves();
                    GUI.enabled = true;
                    if (GUILayout.Button(closeUp ? "NORMAL VIEW" : "CHARACTER CLOSE-UP",GUILayout.Height(24)))
                    {
                        foreach(var player in NetworkPlayerController.ActivePlayers)
                        {
                            if(!player.IsOwner) continue;
                            var camera=player.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>(true);
                            if(camera==null) continue;
                            if(!closeUp) regularZoom=camera.Lens.OrthographicSize;
                            camera.Lens.OrthographicSize=closeUp?regularZoom:2.5f;
                            closeUp=!closeUp;
                            break;
                        }
                    }
                    if (manager.IsServer && GUILayout.Button("RESTART ROUND",GUILayout.Height(30))) NetworkGameManager.Instance?.RestartGame();
                    if (GUILayout.Button("LEAVE / Reset connection",GUILayout.Height(30))) { manager.Shutdown(); roomCode=""; busy=false; message="Choose a connection."; }
                }
            }
            GUILayout.EndArea();
            GUI.Box(new Rect(12,Screen.height-134,620,122),GUIContent.none);
            GUILayout.BeginArea(new Rect(24,Screen.height-126,596,110));
            GUILayout.Label("WASD move   |   SHIFT sprint   |   Hold LMB fire   |   1-3 / Wheel switch",text);
            foreach (var player in NetworkPlayerController.ActivePlayers)
            {
                if (!player.IsOwner) continue;
                var hp = player.GetComponent<NetworkHealth>();
                GUILayout.Label("["+(player.SelectedWeapon+1)+"] " +player.CurrentWeapon.displayName+"   |   HP "+hp.CurrentHealth.Value+"   |   "+(player.IsSprinting?"SPRINT / wider spread":"WALK / steady aim")+"   |   Ammo: unlimited (test)",text);
                var stats=player.CurrentStats;
                GUILayout.Label("Damage "+stats.Damage+"  |  Interval "+stats.Interval.ToString("0.00")+"s  |  Penetration "+(stats.Targets==0?"Unlimited":stats.Targets.ToString()),text);
                if (!player.CanAct) GUILayout.Label("DOWNED - host can restart after round ends.",text);
            }
            GUILayout.EndArea();
        }
        private void OnDestroy()
        {
            if (manager == null) return;
            manager.OnClientConnectedCallback -= Connected;
            manager.OnClientDisconnectCallback -= Disconnected;
            manager.OnTransportFailure -= Failed;
        }
    }
}
