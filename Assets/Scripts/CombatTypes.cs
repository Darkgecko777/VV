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

            public RuleType Type;
            public ConditionTarget Target;
            public bool MeleeOnly;
        }
    }
}