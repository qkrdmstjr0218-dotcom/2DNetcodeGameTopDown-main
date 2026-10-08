using Unity.Netcode;
using UnityEngine;
namespace TopDownShooter.Networking
{
    public sealed class PracticeTarget : NetworkBehaviour
    {
        public readonly NetworkVariable<int> Hits = new(0);
        public readonly NetworkVariable<int> DamageTaken = new(0);
        public void ReceiveDamage(int damage)
        {
            if (!IsServer) return;
            Hits.Value++;
            DamageTaken.Value += damage;
        }
        public override void OnNetworkSpawn() { if (IsServer) { Hits.Value = 0; DamageTaken.Value = 0; } }
        private void OnGUI()
        {
            if (!IsSpawned || Camera.main == null) return;
            Vector3 point = Camera.main.WorldToScreenPoint(transform.position);
            if (point.z > 0) GUI.Label(new Rect(point.x-36,Screen.height-point.y-42,90,22),"Hits: "+Hits.Value);
        }
    }
}
