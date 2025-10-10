using System;
using UnityEngine;

namespace VirulentVentures
{
    public static class GameTypes
    {
        public enum DefenseCheck
        {
            Standard,
            None
        }

        public enum ConditionTarget
        {
            User,
            Ally,
            Enemy
        }

        public enum CooldownType
        {
            None,
            Actions,
            Rounds
        }

        public enum EffectType
        {
            Strike,
            Heal,
            Interrupt,
            SelfSacrifice,
            InstantKill,
            StatEffect
        }

        public enum TargetStat
        {
            Health,
            Morale,
            Speed,
            Attack,
            Defense,
            Evasion,
            Immunity
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
                SingleConditional,
                All
            }

            public enum SelectionCriteria
            {
                Default,
                LowestHealth,
                Random
            }

            public RuleType Type;
            public ConditionTarget Target;
            public bool MeleeOnly;
            public SelectionCriteria Criteria;
        }

        [Serializable]
        public struct CooldownParams
        {
            public CooldownType Type;
            public int Duration;
        }

        [Serializable]
        public struct EffectParams
        {
            public float Multiplier;
            public float HealthThresholdPercent;
        }
    }
}