using UnityEngine;
using System;

namespace VirulentVentures
{
    [Flags]
    public enum EffectType
    {
        None = 0,
        Damage = 1 << 0,
        Heal = 1 << 1,
        Buff = 1 << 2,
        Debuff = 1 << 3,
        Morale = 1 << 4,
        Infection = 1 << 5,
        Taunt = 1 << 6, // Added for Taunt effect
        Thorns = 1 << 7 // Added for Thorns effect
    }

    public enum TargetType
    {
        Self,
        Enemies,
        Allies,
        AOE
    }

    public enum RangeType
    {
        Melee,
        Ranged,
        None
    }

    public enum DefenseCheck
    {
        Standard,
        Ignore,
        Partial,
        None
    }

    public enum EvasionCheck
    {
        Dodgeable,
        Undodgeable,
        None
    }

    public enum Comparison
    {
        Greater,
        Lesser,
        Equal
    }

    public enum Stat
    {
        Health,
        Morale,
        Speed,
        Attack,
        Defense,
        Rank // Added previously in CombatSceneComponent.cs
    }

    public enum ConditionTarget
    {
        User,
        Ally,
        Enemy
    }

    public enum CostType
    {
        None,
        Health,
        Morale
    }

    [Serializable]
    public struct AbilityCondition
    {
        public Comparison Comparison;
        public Stat Stat;
        public float Threshold;
        public bool IsPercentage;
        public ConditionTarget Target;
    }
}