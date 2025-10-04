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

        [Serializable]
        public struct AttackParams
        {
            public DefenseCheck Defense;
            public bool Dodgeable; // Indicates if the attack can be dodged
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
                Default, // First valid unit (frontmost)
                LowestHealth, // Unit with lowest health-to-max-health ratio
                Random // Randomly select from valid targets
            }

            public RuleType Type;
            public ConditionTarget Target;
            public bool MeleeOnly;
            public SelectionCriteria Criteria;
        }
    }
}