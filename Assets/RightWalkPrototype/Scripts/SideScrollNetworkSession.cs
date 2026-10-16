using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace RightWalkPrototype
{
    public sealed class SideScrollNetworkSession : MonoBehaviour
    {
        public GameObject targetPrefab;
        private void SpawnTargets()
        {
            if(targetPrefab==null)return;
            foreach(float x in new[]{-3f,3f})
                Instantiate(targetPrefab,new Vector3(x,1.2f,0),Quaternion.identity).GetComponent<NetworkObject>().Spawn();
        }
        public void StartLocal(bool host)
        {
            var manager=NetworkManager.Singleton;
            if(manager==null||manager.IsListening)return;
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1",7783,"127.0.0.1");
            if(host)manager.StartHost();else manager.StartClient();
        }
        private void Start()
        {
            var args=System.Environment.GetCommandLineArgs();
            // NetworkConfig.Prefabs.Add alone is not a persisted prefab-list asset.
            if(targetPrefab!=null && !NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(targetPrefab))
                NetworkManager.Singleton.AddNetworkPrefab(targetPrefab);
            NetworkManager.Singleton.OnServerStarted+=SpawnTargets;
            if(System.Array.IndexOf(args,"--side-host")>=0)StartLocal(true);
            else if(System.Array.IndexOf(args,"--side-client")>=0)StartLocal(false);
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
            var manager=NetworkManager.Singleton;
            GUILayout.BeginArea(new Rect(Screen.width-230,15,215,160),GUI.skin.box);
            if(!manager.IsListening)
            {
                if(GUILayout.Button("Local Host"))StartLocal(true);
                if(GUILayout.Button("Local Client"))StartLocal(false);
            }
            else
            {
                GUILayout.Label(manager.IsHost ? "Host : 7783" : "Client : 7783");
                GUILayout.Label("Players: "+FindObjectsByType<SideScrollNetworkPlayer>(FindObjectsSortMode.None).Length);
                if(manager.IsServer && GUILayout.Button("Reset Targets"))
                    foreach(var target in FindObjectsByType<SideScrollDamageTarget>(FindObjectsSortMode.None))target.ResetTarget();
                if(GUILayout.Button("Disconnect"))manager.Shutdown();
            }
            GUILayout.EndArea();
        }
        private void OnDestroy()
        {
            if(NetworkManager.Singleton!=null)NetworkManager.Singleton.OnServerStarted-=SpawnTargets;
        }
    }
}
