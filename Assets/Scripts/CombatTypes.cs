using System;
using UnityEngine;

namespace VirulentVentures
{
    public static class CombatTypes
    {
        public enum DefenseCheck
        {
            Standard,
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
        }

        [Serializable]
        public struct AttackParams
        {
            public DefenseCheck Defense;
            public bool Dodgeable;
        }

        [Serializable]
        public struct TargetingRule
        {
            public enum RuleType
            {
                Single,
                All,
                MeleeSingle,
                MeleeAll
            }

            public RuleType Type;
            public ConditionTarget Target;
            public bool MustBeInfected;
            public bool MustNotBeInfected;
            public bool MeleeOnly;
            public int MinPosition;
            public int MaxPosition;

            public void Validate()
            {
                if (MustBeInfected && MustNotBeInfected)
                {
                    Debug.LogWarning("TargetingRule: MustBeInfected and MustNotBeInfected cannot both be true.");
                    MustBeInfected = false;
                    MustNotBeInfected = false;
                }
                if ((Type == RuleType.All || Type == RuleType.MeleeAll) && Target != ConditionTarget.Ally)
                {
                    Debug.LogWarning("TargetingRule: All/MeleeAll rule requires Target = Ally.");
                    Target = ConditionTarget.Ally;
                }
                if (MinPosition < 0 || MaxPosition < 0 || (MinPosition > MaxPosition && MaxPosition != 0))
                {
                    Debug.LogWarning("TargetingRule: Invalid position range, resetting.");
                    MinPosition = 0;
                    MaxPosition = 0;
                }
            }
        }
    }
}