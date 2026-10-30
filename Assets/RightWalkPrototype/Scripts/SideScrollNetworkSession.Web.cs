using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkSession
    {
        public static bool BrowserPlayer=>Application.platform==RuntimePlatform.WebGLPlayer;
        internal void ConfigureTransport(bool offline)
        {
            var manager=NetworkManager.Singleton;if(manager==null||manager.IsListening||manager.ShutdownInProgress)return;
            if(offline)
            {
                var transport=manager.GetComponent<ArenaOfflineTransport>();if(transport==null)transport=manager.gameObject.AddComponent<ArenaOfflineTransport>();
                manager.NetworkConfig.NetworkTransport=transport;
            }
            else
            {
                var transport=manager.GetComponent<UnityTransport>();
                if(BrowserPlayer&&transport!=null)transport.UseWebSockets=true;
                manager.NetworkConfig.NetworkTransport=transport;
            }
        }
        // Task.Delay relies on managed timers, which are restricted in Web builds.
        static async Task WaitForQueryDelay(float seconds)
        {
            float until=Time.realtimeSinceStartup+Mathf.Max(0,seconds);
            while(Application.isPlaying&&Time.realtimeSinceStartup<until)await Task.Yield();
        }
    }
}
