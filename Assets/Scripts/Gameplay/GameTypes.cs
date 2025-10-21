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
            StatEffect,
            ApplyStatusEffect // Added for new effect
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

        public enum StatusEffectType
        {
            Interrupt,
            Thorns,
            HealthShield,
            MoraleShield
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
                Random,
                HighestHealth
            }

            public RuleType Type;
            public ConditionTarget Target;
            public bool MeleeOnly;
            public SelectionCriteria Criteria;
            public bool TargetSelf; // Added to support self-targeting
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