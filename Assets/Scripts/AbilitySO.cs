using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "AbilitySO", menuName = "VirulentVentures/AbilitySO")]
    public class AbilitySO : ScriptableObject
    {
        [System.Serializable]
        public struct Attack
        {
            public int NumberOfTargets; // Clamped: 2 for Melee, 4 for Ranged
            public bool Enemy; // True for enemies, false for allies/self
            public bool Melee; // True for frontline (PartyPosition 1-2), false for Ranged (1-4)
            public DefenseCheck Defense; // Full, Partial, or No defense calculation
            public bool Dodgeable; // True if target.Evasion applies
            public float PartialDefenseMultiplier; // Multiplier for Partial defense (e.g., 0.025)
        }

        [System.Serializable]
        public struct Effect
        {
            public int NumberOfTargets; // Clamped: 2 for Melee, 4 for Ranged
            public bool Enemy; // True for enemies, false for allies/self
            public bool Melee; // True for frontline (PartyPosition 1-2), false for Ranged (1-4)
            public string[] Tags; // Effect IDs (e.g., "TrueStrike:10", "VirusSpread") for CombatEffectsComponent
        }

        [SerializeField] private string id;
        [SerializeField] private int priority; // Lower value = higher priority
        [SerializeField] private int cooldown; // Actions before reuse
        [SerializeField] private int rank; // Required hero rank (1-3), 0 for monsters
        [SerializeField] private List<AbilityCondition> conditions; // Existing struct: Target, Stat, Comparison, Threshold, IsPercentage
        [SerializeField] private List<Attack> attacks; // Array of attack actions
        [SerializeField] private List<Effect> effects; // Array of effect actions
        [SerializeField] private int costAmount; // Amount for costType

        public string Id => id;
        public int Priority => priority;
        public int Cooldown => cooldown;
        public int Rank => rank;
        public List<AbilityCondition> Conditions => conditions;
        public List<Attack> Attacks => attacks;
        public List<Effect> Effects => effects;
        public int CostAmount => costAmount;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"AbilitySO {name}: ID is empty.");
            }
            if (priority < 1)
            {
                Debug.LogWarning($"AbilitySO {id}: Priority must be >= 1.");
                priority = 1;
            }
            if (cooldown < 0)
            {
                Debug.LogWarning($"AbilitySO {id}: Cooldown must be >= 0.");
                cooldown = 0;
            }
            if (rank < 0 || rank > 3)
            {
                Debug.LogWarning($"AbilitySO {id}: Rank must be 0-3 (0 for monsters, 1-3 for heroes).");
                rank = Mathf.Clamp(rank, 0, 3);
            }
            foreach (var attack in attacks)
            {
                if (attack.NumberOfTargets < 1)
                {
                    Debug.LogWarning($"AbilitySO {id}: Attack NumberOfTargets must be >= 1.");
                }
                if (attack.Defense == DefenseCheck.Partial && attack.PartialDefenseMultiplier <= 0)
                {
                    Debug.LogWarning($"AbilitySO {id}: Partial Defense requires positive PartialDefenseMultiplier.");
                }
            }
            foreach (var effect in effects)
            {
                if (effect.NumberOfTargets < 1)
                {
                    Debug.LogWarning($"AbilitySO {id}: Effect NumberOfTargets must be >= 1.");
                }
                if (effect.Tags == null || effect.Tags.Length == 0)
                {
                    Debug.LogWarning($"AbilitySO {id}: Effect Tags array is empty.");
                }
            }
            foreach (var condition in conditions)
            {
                if (condition.IsPercentage && (condition.Threshold < 0 || condition.Threshold > 1))
                {
                    Debug.LogWarning($"AbilitySO {id}: Percentage Threshold must be 0-1.");
                }
            }
            if (costAmount < 0)
            {
                Debug.LogWarning($"AbilitySO {id}: CostAmount must be >= 0.");
                costAmount = 0;
            }
        }
    }
}