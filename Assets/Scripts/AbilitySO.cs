using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "AbilitySO", menuName = "VirulentVentures/AbilitySO")]
    public class AbilitySO : ScriptableObject
    {
        [System.Serializable]
        public struct AbilityEffect
        {
            public EffectType type;
            public Stat stat;
            public int value;
            public int duration;
        }

        [SerializeField] private string id;
        [SerializeField] private TargetType targetType;
        [SerializeField] private RangeType rangeType;
        [SerializeField] private EffectType effectTypes;
        [SerializeField] private DefenseCheck defenseCheck;
        [SerializeField] private float partialDefenseMultiplier;
        [SerializeField] private EvasionCheck evasionCheck;
        [SerializeField] private int fixedDamage;
        [SerializeField] private int selfDamage;
        [SerializeField] private int thornsFixed;
        [SerializeField] private bool thornsInfection;
        [SerializeField] private bool skipNextAttack;
        [SerializeField] private bool priorityLowHealth;
        [SerializeField] private float healMultiplier;
        [SerializeField] private int cooldown;
        [SerializeField] private int priority;
        [SerializeField] private CostType costType;
        [SerializeField] private int costAmount;
        [SerializeField] private List<AbilityCondition> conditions;
        [SerializeField] private List<AbilityEffect> effects; // Added for multiple effects

        public string Id => id;
        public TargetType TargetType => targetType;
        public RangeType RangeType => rangeType;
        public EffectType EffectTypes => effectTypes;
        public DefenseCheck DefenseCheck => defenseCheck;
        public float PartialDefenseMultiplier => partialDefenseMultiplier;
        public EvasionCheck EvasionCheck => evasionCheck;
        public int FixedDamage => fixedDamage;
        public int SelfDamage => selfDamage;
        public int ThornsFixed => thornsFixed;
        public bool ThornsInfection => thornsInfection;
        public bool SkipNextAttack => skipNextAttack;
        public bool PriorityLowHealth => priorityLowHealth;
        public float HealMultiplier => healMultiplier;
        public int Cooldown => cooldown;
        public int Priority => priority;
        public CostType CostType => costType;
        public int CostAmount => costAmount;
        public List<AbilityCondition> Conditions => conditions;
        public List<AbilityEffect> Effects => effects; // Added getter

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"AbilitySO {name}: ID is empty. This will appear in combat logs.");
            }
            if (defenseCheck == DefenseCheck.Partial && partialDefenseMultiplier <= 0)
            {
                Debug.LogWarning($"AbilitySO {id}: Partial DefenseCheck requires positive multiplier.");
            }
            if ((effectTypes & EffectType.Heal) != 0 && targetType == TargetType.Enemies)
            {
                Debug.LogWarning($"AbilitySO {id}: Heal effect cannot target Enemies.");
            }
            if (priority < 1)
            {
                Debug.LogWarning($"AbilitySO {id}: Priority must be >= 1.");
            }
            if (cooldown < 0)
            {
                Debug.LogWarning($"AbilitySO {id}: Cooldown must be >= 0.");
            }
            foreach (var condition in conditions)
            {
                if (condition.IsPercentage && (condition.Threshold < 0 || condition.Threshold > 1))
                {
                    Debug.LogWarning($"AbilitySO {id}: Percentage Threshold must be 0-1.");
                }
            }
            foreach (var effect in effects)
            {
                if (effect.duration < 0)
                {
                    Debug.LogWarning($"AbilitySO {id}: Effect {effect.type} duration must be >= 0.");
                }
                if ((effect.type == EffectType.Taunt || effect.type == EffectType.Thorns) && effect.duration <= 0)
                {
                    Debug.LogWarning($"AbilitySO {id}: Effect {effect.type} requires positive duration.");
                }
                if (effect.type != EffectType.None && effect.value == 0 && effect.type != EffectType.Infection)
                {
                    Debug.LogWarning($"AbilitySO {id}: Effect {effect.type} should have non-zero value (except Infection).");
                }
            }
        }
    }
}