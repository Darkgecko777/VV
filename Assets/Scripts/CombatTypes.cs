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

        public enum ConditionTarget
        {
            User,
            Ally,
            Enemy
        }

        public enum CooldownType
        {
            None,        // No cooldown
            Actions,     // Cooldown based on number of actions (e.g., attacks)
            Rounds       // Cooldown based on combat rounds
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
                SingleConditional
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
            public CooldownType Type; // Actions or Rounds
            public int Duration;      // Number of actions/rounds for cooldown
        }
    }
}