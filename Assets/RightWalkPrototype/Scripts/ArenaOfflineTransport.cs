using System;
using Unity.Netcode;
using UnityEngine;
namespace RightWalkPrototype
{
    // A host's own client is internal to NGO. Solo mode needs no socket at all.
    // Never accepts or sends remote traffic; online games restore UnityTransport.
    public sealed class ArenaOfflineTransport:NetworkTransport
    {
        public override ulong ServerClientId=>0;
        public override void Initialize(NetworkManager networkManager=null){}
        public override bool StartServer()=>true;
        public override bool StartClient()=>false;
        public override void Send(ulong clientId,ArraySegment<byte> payload,NetworkDelivery delivery){}
        public override NetworkEvent PollEvent(out ulong clientId,out ArraySegment<byte> payload,out float receiveTime)
        {clientId=0;payload=default;receiveTime=Time.realtimeSinceStartup;return NetworkEvent.Nothing;}
        public override void DisconnectRemoteClient(ulong clientId){}
        public override void DisconnectLocalClient(){}
        public override ulong GetCurrentRtt(ulong clientId)=>0;
        public override void Shutdown(){}
    }
}
