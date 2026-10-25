using UnityEngine;
using Unity.Netcode;

namespace RightWalkPrototype
{
    public sealed class SideScrollDamageTarget : NetworkBehaviour
    {
        [Min(1)] public int maxHealth=3;
        public readonly NetworkVariable<int> Health=new(3);
        public readonly NetworkVariable<int> Hits=new(0);
        private int generation;
        public int Generation => generation;
        private float flash;
        public event System.Action<SideScrollNetworkPlayer,SideScrollNetworkPlayer.AmmoKind> Damaged;
        public override void OnNetworkSpawn()
        {
            Health.OnValueChanged+=Changed;
            if(IsServer)ResetTarget();
            ApplyState();
        }
        public void ResetTarget()
        {
            if(!IsServer)return;
            generation++;Hits.Value=0;Health.Value=Mathf.Max(1,maxHealth);
            ApplyState();
        }
        public void ReceiveDamage(int damage,int expectedGeneration,SideScrollNetworkPlayer source=null,SideScrollNetworkPlayer.AmmoKind kind=SideScrollNetworkPlayer.AmmoKind.Normal)
        {
            if(!IsServer||!IsSpawned||Health.Value<=0||damage<=0||generation!=expectedGeneration)return;
            Hits.Value++;
            Health.Value=Mathf.Max(0,Health.Value-damage);
            Damaged?.Invoke(source,kind);
        }
        private void Changed(int previous,int current)
        {
            if(current<previous)flash=.1f;
            ApplyState();
        }
        private void ApplyState()
        {
            GetComponent<Collider2D>().enabled=Health.Value>0;
            transform.localRotation=Quaternion.Euler(0,0,Health.Value>0 ? 0 : -75);
            foreach(var renderer in GetComponentsInChildren<SpriteRenderer>())
                renderer.color=Health.Value>0 ? Color.white : new Color(.4f,.4f,.4f,.65f);
        }
        private void Update()
        {
            if(flash<=0)return;
            flash-=Time.deltaTime;
            foreach(var renderer in GetComponentsInChildren<SpriteRenderer>())renderer.color=new Color(1f,.65f,.35f);
            if(flash<=0)ApplyState();
        }
        private void OnGUI()
        {
            if(!IsSpawned||Camera.main==null)return;
            var point=Camera.main.WorldToScreenPoint(transform.position+Vector3.up*.6f);
            if(point.z<=0)return;
            var rect=new Rect(point.x-35,Screen.height-point.y-15,70,6);
            var old=GUI.color;GUI.color=new Color(.1f,.1f,.1f,.85f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
            rect.width*=Health.Value/(float)Mathf.Max(1,maxHealth);
            GUI.color=new Color(.25f,.9f,.6f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;
            GUI.Label(new Rect(point.x-35,Screen.height-point.y-40,90,22),Health.Value+" / "+maxHealth);
        }
        public override void OnNetworkDespawn(){Health.OnValueChanged-=Changed;}
    }
}
