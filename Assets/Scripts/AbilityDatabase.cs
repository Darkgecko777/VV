using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public static class AbilityDatabase
    {
        private static readonly Dictionary<string, AbilitySO> heroAbilities = new Dictionary<string, AbilitySO>();
        private static readonly Dictionary<string, AbilitySO> monsterAbilities = new Dictionary<string, AbilitySO>();
        private static bool isInitialized = false;

        static AbilityDatabase()
        {
            InitializeAbilities();
        }

        private static void InitializeAbilities()
        {
            if (isInitialized) return;
            isInitialized = true;

            // Load all AbilitySO assets from Resources/Abilities
            var allAbilities = Resources.LoadAll<AbilitySO>("Abilities");
            foreach (var ability in allAbilities)
            {
                if (ability == null || string.IsNullOrEmpty(ability.Id))
                {
                    Debug.LogWarning($"AbilityDatabase: Skipping invalid AbilitySO (null or empty ID)");
                    continue;
                }

                // Categorize based on Rank (hero if Rank > 0, else monster or common)
                if (ability.Rank > 0)
                {
                    heroAbilities[ability.Id] = ability;
                }
                else
                {
                    monsterAbilities[ability.Id] = ability;
                }
            }

            if (heroAbilities.Count == 0)
                Debug.LogWarning("AbilityDatabase: No hero AbilitySOs found in Resources/Abilities.");
            if (monsterAbilities.Count == 0)
                Debug.LogWarning("AbilityDatabase: No monster AbilitySOs found in Resources/Abilities.");
            if (!heroAbilities.ContainsKey("BasicAttack") && !monsterAbilities.ContainsKey("BasicAttack"))
                Debug.LogWarning("AbilityDatabase: BasicAttack not found in Resources/Abilities. Ensure a BasicAttack AbilitySO exists.");
        }

        public static AbilitySO GetHeroAbility(string id)
        {
            if (heroAbilities.TryGetValue(id, out var ability))
            {
                return ability;
            }
            Debug.LogWarning($"AbilityDatabase: Hero ability ID {id} not found, returning null.");
            return null;
        }

        public static AbilitySO GetMonsterAbility(string id)
        {
            if (monsterAbilities.TryGetValue(id, out var ability))
            {
                return ability;
            }
            Debug.LogWarning($"AbilityDatabase: Monster ability ID {id} not found, returning null.");
            return null;
        }

        public static List<AbilitySO> GetCommonAbilities()
        {
            var common = heroAbilities.Values.Where(a => a.Id == "BasicAttack").ToList();
            common.AddRange(monsterAbilities.Values.Where(a => a.Id == "BasicAttack"));
            return common;
        }

        // Utility to force reinitialization (e.g., for hot-reloading in Editor)
        public static void Reinitialize()
        {
            heroAbilities.Clear();
            monsterAbilities.Clear();
            isInitialized = false;
            InitializeAbilities();
        }
    }
}