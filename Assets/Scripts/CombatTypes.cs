using System;
using UnityEngine;

namespace VirulentVentures
{
    public static class CombatTypes
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
            Evasion,
            Rank,
            Infectivity,
            PartyPosition,
            IsInfected,
            HasBuff,
            HasDebuff,
            Role
        }

        public enum ConditionTarget
        {
            User,
            Ally,
            Enemy
        }

        public enum TeamCondition
        {
            None,
            AverageStat,
            TotalStat
        }

        public enum TeamTarget
        {
            None,
            Allies,
            Enemies,
            Both
        }

        public enum CooldownType
        {
            Actions,
            Rounds
        }

        [Serializable]
        public struct AbilityCondition
        {
            public Comparison Comparison;
            public Stat Stat;
            public float Threshold;
            public bool IsPercentage;
            public ConditionTarget Target;
            public int MinTargetCount;
            public int MaxTargetCount;
            public int MinPosition;
            public int MaxPosition;
            public string StatusEffect;
            public TeamCondition TeamCondition;
            public TeamTarget TeamTarget;
        }

        [Serializable]
        public struct AttackParams
        {
            public DefenseCheck Defense;
            public bool Dodgeable;
            public float PartialDefenseMultiplier;
        }

        [Serializable]
        public struct TargetingRule
        {
            public enum RuleType
            {
                Random,
                LowestHealth,
                HighestHealth,
                LowestMorale,
                HighestMorale,
                LowestAttack,
                HighestAttack,
                AllAllies,
                WeightedRandom
            }

            public RuleType Type;
            public ConditionTarget Target;
            public bool MustBeInfected;
            public bool MustNotBeInfected;
            public bool MeleeOnly;
            public int MinPosition;
            public int MaxPosition;
            public Stat WeightStat;
            public float WeightFactor;

            public void Validate()
            {
                if (MustBeInfected && MustNotBeInfected)
                {
                    Debug.LogWarning("TargetingRule: MustBeInfected and MustNotBeInfected cannot both be true.");
                    MustBeInfected = false;
                    MustNotBeInfected = false;
                }
                if (Type == RuleType.AllAllies && Target != ConditionTarget.Ally)
                {
                    Debug.LogWarning("TargetingRule: AllAllies rule requires Target = Ally.");
                    Target = ConditionTarget.Ally;
                }
                if (MinPosition < 0 || MaxPosition < 0 || (MinPosition > MaxPosition && MaxPosition != 0))
                {
                    Debug.LogWarning("TargetingRule: Invalid position range, resetting.");
                    MinPosition = 0;
                    MaxPosition = 0;
                }
                if (Type == RuleType.WeightedRandom && WeightStat == default(Stat))
                {
                    Debug.LogWarning("TargetingRule: WeightedRandom requires a valid WeightStat, defaulting to Health.");
                    WeightStat = Stat.Health;
                }
            }
        }
    }
}