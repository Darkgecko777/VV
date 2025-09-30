using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "AbilitySO", menuName = "VirulentVentures/AbilitySO")]
    public class AbilitySO : ScriptableObject
    {
        [System.Serializable]
        public struct AbilityAction
        {
            [Tooltip("Target of the action (User, Ally, Enemy)")]
            public CombatTypes.ConditionTarget Target;
            [Tooltip("Number of targets (1-4, clamped by Melee)")]
            public int NumberOfTargets;
            [Tooltip("True: targets positions 1-2, False: 1-4")]
            public bool Melee;
            [Tooltip("Effect ID from EffectReference (e.g., Heal, Reflect, Damage)")]
            public string EffectId;
            [Tooltip("Value to scale effect (e.g., 0.15 for 15% heal)")]
            public float EffectValue;
            [Tooltip("Duration in rounds for buffs (0 for instant effects)")]
            public int EffectDuration;
            [Tooltip("Targeting rule (e.g., LowestHealth, Random)")]
            public CombatTypes.TargetingRule.RuleType RuleType;
            [Tooltip("Defense check for damage effects")]
            public CombatTypes.DefenseCheck Defense;
            [Tooltip("True if damage can be dodged")]
            public bool Dodgeable;
            [Tooltip("Multiplier for partial defense (e.g., 0.025)")]
            public float PartialDefenseMultiplier;
        }

        [SerializeField] private string id;
        [SerializeField] private int cooldown;
        [SerializeField] private CombatTypes.CooldownType cooldownType;
        [SerializeField] private int rank;
        [SerializeField] private List<CombatTypes.AbilityCondition> conditions;
        [SerializeField] private AbilityAction action;
        [SerializeField] private string animationTrigger;

        public string Id => id;
        public int Cooldown => cooldown;
        public CombatTypes.CooldownType CooldownType => cooldownType;
        public int Rank => rank;
        public List<CombatTypes.AbilityCondition> Conditions => conditions;
        public AbilityAction Action => action;
        public string AnimationTrigger => animationTrigger;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) Debug.LogWarning($"AbilitySO {name}: ID is empty.");
            if (cooldown < 0)
            {
                Debug.LogWarning($"AbilitySO {id}: Cooldown must be >= 0.");
                cooldown = 0;
            }
            if (rank < 0 || rank > 3)
            {
                Debug.LogWarning($"AbilitySO {id}: Rank must be 0-3.");
                rank = Mathf.Clamp(rank, 0, 3);
            }
            if (action.NumberOfTargets < 1)
            {
                Debug.LogWarning($"AbilitySO {id}: NumberOfTargets must be >= 1.");
                action.NumberOfTargets = 1;
            }
            if (string.IsNullOrEmpty(action.EffectId))
            {
                Debug.LogWarning($"AbilitySO {id}: EffectId is empty.");
            }
            if (action.Defense == CombatTypes.DefenseCheck.Partial && action.PartialDefenseMultiplier <= 0)
            {
                Debug.LogWarning($"AbilitySO {id}: Partial Defense requires positive PartialDefenseMultiplier.");
                action.PartialDefenseMultiplier = 0.025f;
            }
            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                if (condition.IsPercentage && (condition.Threshold < 0 || condition.Threshold > 1))
                {
                    Debug.LogWarning($"AbilitySO {id}: Percentage Threshold must be 0-1 for condition {i}.");
                    condition.Threshold = Mathf.Clamp(condition.Threshold, 0f, 1f);
                    conditions[i] = condition;
                }
            }
            if (string.IsNullOrEmpty(animationTrigger))
            {
                Debug.LogWarning($"AbilitySO {id}: AnimationTrigger is empty.");
            }
        }

        public CombatTypes.TargetingRule GetTargetingRule()
        {
            return new CombatTypes.TargetingRule
            {
                Type = action.RuleType,
                Target = action.Target,
                MeleeOnly = action.Melee,
                MinPosition = action.Melee ? 1 : 0,
                MaxPosition = action.Melee ? 2 : 4
            };
        }
    }
}