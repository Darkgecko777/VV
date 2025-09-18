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
            var allAbilities = Resources.LoadAll<AbilitySO>("Abilities");
            foreach (var ability in allAbilities)
            {
                if (ability == null || string.IsNullOrEmpty(ability.Id))
                {
                    Debug.LogWarning($"AbilityDatabase: Skipping invalid AbilitySO (null or empty ID)");
                    continue;
                }
                if (ability.Rank > 0)
                    heroAbilities[ability.Id] = ability;
                else
                    monsterAbilities[ability.Id] = ability;
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
                return ability;
            Debug.LogWarning($"AbilityDatabase: Hero ability ID {id} not found, returning null.");
            return null;
        }

        public static AbilitySO GetMonsterAbility(string id)
        {
            if (monsterAbilities.TryGetValue(id, out var ability))
                return ability;
            Debug.LogWarning($"AbilityDatabase: Monster ability ID {id} not found, returning null.");
            return null;
        }

        public static List<AbilitySO> GetCommonAbilities()
        {
            var common = heroAbilities.Values.Where(a => a.Id == "BasicAttack").ToList();
            common.AddRange(monsterAbilities.Values.Where(a => a.Id == "BasicAttack"));
            return common;
        }

        public static void Reinitialize()
        {
            heroAbilities.Clear();
            monsterAbilities.Clear();
            isInitialized = false;
            InitializeAbilities();
        }

        public static string SelectAbility(CharacterStats unit, PartyData partyData, List<ICombatUnit> targets, UnitAttackState state)
        {
            if (state == null)
            {
                Debug.LogWarning($"No UnitAttackState for {unit.Id}. Falling back to BasicAttack.");
                return "BasicAttack";
            }
            if (unit.Abilities == null || unit.Abilities.Length == 0)
            {
                Debug.LogWarning($"No abilities assigned to {unit.Id}. Falling back to BasicAttack.");
                return "BasicAttack";
            }
            AbilitySO selectedAbility = null;
            int lowestPriority = int.MaxValue;
            foreach (var abilityObj in unit.Abilities)
            {
                if (!(abilityObj is AbilitySO ability))
                {
                    Debug.LogWarning($"Invalid AbilitySO for {unit.Id}: {abilityObj?.name}. Skipping.");
                    continue;
                }
                if (state.AbilityCooldowns.TryGetValue(ability.Id, out int cd) && cd > 0)
                {
                    Debug.Log($"Ability {ability.Id} for {unit.Id} on cooldown: {cd} actions remaining.");
                    continue;
                }
                if (ability.Rank > 0 && unit.Rank < ability.Rank)
                {
                    string failMessage = $"Ability {ability.Id} requires Rank {ability.Rank}, unit has Rank {unit.Rank}.";
                    CombatSceneComponent.Instance.AllCombatLogs.Add(failMessage);
                    CombatSceneComponent.Instance.EventBus.RaiseLogMessage(failMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
                    continue;
                }
                bool conditionsMet = true;
                foreach (var condition in ability.Conditions)
                {
                    CharacterStats targetUnit = null;
                    float statValue = 0;
                    if (condition.Target == ConditionTarget.User)
                        targetUnit = unit;
                    else if (condition.Target == ConditionTarget.Enemy)
                        targetUnit = targets.FirstOrDefault(t => t is CharacterStats cs && cs.Health > 0 && !cs.HasRetreated) as CharacterStats;
                    else if (condition.Target == ConditionTarget.Ally)
                        targetUnit = partyData.HeroStats.FirstOrDefault(h => h != unit && h.Health > 0 && !h.HasRetreated);
                    if (targetUnit == null)
                    {
                        string failMessage = $"No valid {condition.Target} target for {ability.Id}.";
                        CombatSceneComponent.Instance.AllCombatLogs.Add(failMessage);
                        CombatSceneComponent.Instance.EventBus.RaiseLogMessage(failMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
                        conditionsMet = false;
                        break;
                    }
                    switch (condition.Stat)
                    {
                        case Stat.Health: statValue = condition.IsPercentage ? targetUnit.Health / (float)targetUnit.MaxHealth : targetUnit.Health; break;
                        case Stat.Morale: statValue = condition.IsPercentage ? targetUnit.Morale / (float)targetUnit.MaxMorale : targetUnit.Morale; break;
                        case Stat.Speed: statValue = targetUnit.Speed; break;
                        case Stat.Attack: statValue = targetUnit.Attack; break;
                        case Stat.Defense: statValue = targetUnit.Defense; break;
                        case Stat.Rank: statValue = targetUnit.Rank; break;
                        case Stat.MaxHealth: statValue = targetUnit.MaxHealth; break;
                        case Stat.MaxMorale: statValue = targetUnit.MaxMorale; break;
                        case Stat.Infectivity: statValue = targetUnit.Infectivity; break;
                        case Stat.PartyPosition: statValue = targetUnit.PartyPosition; break;
                        case Stat.IsInfected: statValue = targetUnit.IsInfected ? 1 : 0; break;
                    }
                    bool conditionResult = condition.Comparison == Comparison.Greater ? statValue > condition.Threshold :
                                          condition.Comparison == Comparison.Lesser ? statValue < condition.Threshold :
                                          Mathf.Abs(statValue - condition.Threshold) < 0.001f;
                    if (!conditionResult)
                    {
                        string failMessage = $"Condition failed for {ability.Id}: {condition.Stat} {condition.Comparison} {condition.Threshold} (Value: {statValue}).";
                        CombatSceneComponent.Instance.AllCombatLogs.Add(failMessage);
                        CombatSceneComponent.Instance.EventBus.RaiseLogMessage(failMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
                        conditionsMet = false;
                        break;
                    }
                }
                if (conditionsMet && ability.Priority < lowestPriority)
                {
                    selectedAbility = ability;
                    lowestPriority = ability.Priority;
                }
            }
            string selectedId = selectedAbility != null ? selectedAbility.Id : "BasicAttack";
            if (selectedAbility != null && selectedAbility.Cooldown > 0)
            {
                state.AbilityCooldowns[selectedId] = selectedAbility.Cooldown;
                Debug.Log($"Set cooldown for {selectedId}: {selectedAbility.Cooldown} actions.");
            }
            return selectedId;
        }
    }
}