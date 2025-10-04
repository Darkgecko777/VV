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
        }

        [Serializable]
        public struct TargetingRule
        {
            public enum RuleType
            {
                Single,
                SingleConditional // New: Scan for first target meeting condition
            }

            public enum SelectionCriteria
            {
                Default, // First valid unit (e.g., frontmost)
                LowestHealth // Unit with lowest health-to-max-health ratio
            }

            public RuleType Type;
            public ConditionTarget Target;
            public bool MeleeOnly;
            public SelectionCriteria Criteria;
        }
    }
}