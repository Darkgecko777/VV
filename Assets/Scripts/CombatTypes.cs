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
                Single
            }

            public enum SelectionCriteria // New: Controls how final target is chosen
            {
                Default, // First valid unit (e.g., frontmost)
                LowestHealth // Unit with lowest health-to-max-health ratio
            }

            public RuleType Type;
            public ConditionTarget Target;
            public bool MeleeOnly;
            public SelectionCriteria Criteria; // New: Selection criteria field
        }
    }
}