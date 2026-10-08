using System;
using Unity.Netcode;
using UnityEngine;
namespace TopDownShooter.Core
{
    // Per-player, server-authored state. Shared definition assets remain unchanged.
    public struct WeaponUpgradeState : INetworkSerializable, IEquatable<WeaponUpgradeState>
    {
        public int DamageBonus;
        public float CooldownScale;
        public float RangeBonus;
        public int TargetBonus;
        public bool UnlimitedPiercing;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T:IReaderWriter
        {
            serializer.SerializeValue(ref DamageBonus); serializer.SerializeValue(ref CooldownScale);
            serializer.SerializeValue(ref RangeBonus); serializer.SerializeValue(ref TargetBonus);
            serializer.SerializeValue(ref UnlimitedPiercing);
        }
        public bool Equals(WeaponUpgradeState other) => DamageBonus==other.DamageBonus && CooldownScale.Equals(other.CooldownScale)
            && RangeBonus.Equals(other.RangeBonus) && TargetBonus==other.TargetBonus && UnlimitedPiercing==other.UnlimitedPiercing;
    }
    public readonly struct WeaponRuntimeStats
    {
        public readonly int Damage, Targets;
        public readonly float Interval, Range;
        public WeaponRuntimeStats(WeaponDefinitionSO definition, WeaponUpgradeState upgrade)
        {
            Damage=Mathf.Clamp(definition.damage+upgrade.DamageBonus,1,100000);
            Interval=Mathf.Max(0.05f,definition.interval*(upgrade.CooldownScale>0?upgrade.CooldownScale:1));
            Range=Mathf.Max(1,definition.range+upgrade.RangeBonus);
            Targets=upgrade.UnlimitedPiercing || definition.targets==0 ? 0 : Mathf.Max(1,definition.targets+upgrade.TargetBonus);
        }
    }
}
