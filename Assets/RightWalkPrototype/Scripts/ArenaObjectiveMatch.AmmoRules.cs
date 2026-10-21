using System;
using Unity.Netcode;
using UnityEngine;

namespace RightWalkPrototype
{
    public sealed partial class ArenaObjectiveMatch
    {
        [SerializeField,Min(1)] int normalAmmoCapacity;
        [SerializeField,Min(0)] float normalAmmoRechargeDelay;
        [SerializeField,Min(.01f)] float normalAmmoRechargeRate;
        [SerializeField,Min(.01f)] float normalShotInterval;
        public struct AmmoRules:INetworkSerializable,IEquatable<AmmoRules>
        {
            public int Capacity;public float Delay,Rate,ShotInterval;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref Capacity);s.SerializeValue(ref Delay);s.SerializeValue(ref Rate);s.SerializeValue(ref ShotInterval);}
            public bool Equals(AmmoRules r)=>Capacity==r.Capacity&&Delay==r.Delay&&Rate==r.Rate&&ShotInterval==r.ShotInterval;
        }
        readonly NetworkVariable<AmmoRules> ammoRules=new();
        public AmmoRules NormalAmmoRules=>ammoRules.Value;
        void InitializeAmmoRules()
        {
            if(IsServer)ammoRules.Value=new AmmoRules{Capacity=Mathf.Max(1,normalAmmoCapacity),Delay=Mathf.Max(0,normalAmmoRechargeDelay),Rate=Mathf.Max(.01f,normalAmmoRechargeRate),ShotInterval=Mathf.Max(.01f,normalShotInterval)};
        }
    }
}
