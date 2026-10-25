using UnityEngine;
using Unity.Netcode;
namespace RightWalkPrototype
{
    public static class ArenaClock
    {
#if UNITY_EDITOR
        static NetworkManager local;static double offset;static SideScrollNetworkSession session;
#endif
        public static void Reset()
        {
#if UNITY_EDITOR
            local=null;session=null;
#endif
        }
        public static double Now(NetworkManager manager)
        {
            if(manager==null||!manager.IsListening)return Time.timeAsDouble;
#if UNITY_EDITOR
            if(session==null)session=Object.FindFirstObjectByType<SideScrollNetworkSession>();
            // Editor Pause is a debugging convenience for a single offline host.
            // Online/ordinary local sessions and every build keep NGO's server clock.
            if(manager.IsHost&&session!=null&&session.SoloPractice&&manager.ConnectedClients.Count<=1)
            {
                if(local!=manager){local=manager;offset=manager.ServerTime.Time-Time.timeAsDouble;}
                return offset+Time.timeAsDouble;
            }
#endif
            return manager.ServerTime.Time;
        }
    }
}
