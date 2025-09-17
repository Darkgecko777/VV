using System;

namespace VirulentVentures
{
    public enum DefenseCheck
    {
        Standard,
        Partial,
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
        MaxHealth,
        Morale,
        MaxMorale,
        Speed,
        Attack,
        Defense,
        Rank,
        Infectivity,
        PartyPosition,
        IsInfected
    }

    public enum ConditionTarget
    {
        User,
        Ally,
        Enemy
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