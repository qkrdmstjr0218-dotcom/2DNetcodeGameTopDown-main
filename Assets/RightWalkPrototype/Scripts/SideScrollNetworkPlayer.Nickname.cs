using Unity.Collections;
using Unity.Netcode;
namespace RightWalkPrototype
{
    public sealed partial class SideScrollNetworkPlayer
    {
        readonly NetworkVariable<FixedString64Bytes> nickname=new();
        public FixedString64Bytes Nickname=>nickname.Value;
        [Rpc(SendTo.Server,InvokePermission=RpcInvokePermission.Owner)]
        void SubmitNicknameRpc(FixedString64Bytes requested)
        {
            // Ownership is enforced by NGO; names are chosen before joining a match.
            var match=ArenaObjectiveMatch.Instance;
            if(match!=null&&match.IsSpawned&&match.State.Value.Phase!=ArenaObjectiveMatch.Stage.Waiting)return;
            nickname.Value=new FixedString64Bytes(ArenaPlayerNames.Normalize(requested.ToString()));
        }
    }
}
