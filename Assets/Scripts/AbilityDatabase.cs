using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace VirulentVentures
{
    public static class AbilityDatabase
    {
        private static readonly Dictionary<string, IAbility> abilities = new Dictionary<string, IAbility>();
        private static bool isInitialized = false;

        public static void InitializeAbilities(CombatSceneComponent sceneComponent)
        {
            if (isInitialized) return;
            isInitialized = true;

            if (sceneComponent == null)
            {
                Debug.LogError("AbilityDatabase: sceneComponent is null in InitializeAbilities.");
                return;
            }

            abilities.Clear();

            var abilityTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IAbility).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in abilityTypes)
            {
                try
                {
                    var ability = Activator.CreateInstance(type) as IAbility;
                    if (ability == null) continue;

                    abilities[ability.Id] = ability;
                    Debug.Log($"AbilityDatabase: Registered {ability.Id} ({type.Name}).");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AbilityDatabase: Failed to instantiate {type.Name}: {ex.Message}");
                }
            }
        }

        public static IAbility GetAbility(string id)
        {
            if (abilities.TryGetValue(id, out var ability))
                return ability;
            Debug.LogWarning($"AbilityDatabase: Ability {id} not found.");
            return null;
        }

        public static void Reinitialize(CombatSceneComponent sceneComponent)
        {
            isInitialized = false;
            InitializeAbilities(sceneComponent);
        }

        public static (string abilityId, string failMessage) SelectAbility(CharacterStats unit, PartyData partyData, List<ICombatUnit> targets, UnitAttackState state)
        {
            if (state == null)
            {
                string msg = $"No UnitAttackState for {unit.Id}.";
                Debug.LogError(msg);
                return (null, msg);
            }

            if (unit.abilityIds == null || unit.abilityIds.Length == 0)
            {
                string msg = $"No abilities assigned to {unit.Id} in CharacterSO.";
                Debug.LogError(msg);
                return (null, msg);
            }

            IAbility selected = null;
            int lowestPriority = int.MaxValue;
            string failMessage = null;

            foreach (var id in unit.abilityIds)
            {
                var ability = GetAbility(id);
                if (ability == null)
                {
                    Debug.LogWarning($"Invalid ability ID {id} for {unit.Id}. Skipping.");
                    continue;
                }

                if (state.AbilityCooldowns.TryGetValue(id, out int actionCd) && actionCd > 0)
                {
                    Debug.Log($"Ability {id} for {unit.Id} on cooldown ({actionCd} actions remaining).");
                    continue;
                }
                if (state.RoundCooldowns.TryGetValue(id, out int roundCd) && roundCd > 0)
                {
                    Debug.Log($"Ability {id} for {unit.Id} on cooldown ({roundCd} rounds remaining).");
                    continue;
                }
                if (ability.Rank > 0 && unit.Rank < ability.Rank)
                {
                    failMessage = $"Ability {id} requires Rank {ability.Rank}, unit has {unit.Rank}.";
                    Debug.LogWarning(failMessage);
                    continue;
                }

                bool met = ability.Conditions.All(c => EvaluateCondition(c, unit, partyData, targets));
                if (!met)
                {
                    Debug.Log($"Conditions not met for {id} by {unit.Id}.");
                    continue;
                }
                if (met && ability.Priority < lowestPriority)
                {
                    selected = ability;
                    lowestPriority = ability.Priority;
                }
            }

            if (selected == null)
            {
                string msg = $"No valid ability for {unit.Id}: all on cooldown or conditions not met.";
                Debug.LogError(msg);
                return (null, msg);
            }

            string selectedId = selected.Id;
            if (selected.Cooldown > 0)
            {
                var cds = selected.CooldownType == CombatTypes.CooldownType.Actions ? state.AbilityCooldowns : state.RoundCooldowns;
                cds[selectedId] = selected.Cooldown;
                Debug.Log($"Applied cooldown for {selectedId}: {selected.Cooldown} {selected.CooldownType}.");
            }

            return (selectedId, failMessage);
        }

        public static bool EvaluateCondition(CombatTypes.AbilityCondition cond, CharacterStats unit, PartyData party, List<ICombatUnit> targets)
        {
            float value;
            if (cond.Target == CombatTypes.ConditionTarget.User)
            {
                value = GetStatValue(cond.Stat, unit);
                if (cond.IsPercentage)
                {
                    float maxValue = cond.Stat == CombatTypes.Stat.Health ? unit.MaxHealth : cond.Stat == CombatTypes.Stat.Morale ? unit.MaxMorale : 1f;
                    value = maxValue > 0 ? value / maxValue : 0f;
                }
            }
            else
            {
                var team = GetTeam(cond.Target == CombatTypes.ConditionTarget.Ally ? CombatTypes.TeamTarget.Allies : CombatTypes.TeamTarget.Enemies, unit, party, targets);
                if (team.Count == 0)
                {
                    Debug.Log($"No valid targets for condition {cond.Stat} (Target={cond.Target}) for {unit.Id}.");
                    return false;
                }
                value = team.Any(t => MeetsCondition(t as CharacterStats, cond)) ? 1f : 0f; // 1 if any target meets condition, 0 otherwise
            }

            float threshold = cond.IsPercentage && cond.Target == CombatTypes.ConditionTarget.User ? cond.Threshold : cond.Threshold;

            bool statMet = cond.Comparison switch
            {
                CombatTypes.Comparison.Greater => value > threshold,
                CombatTypes.Comparison.Lesser => value < threshold,
                CombatTypes.Comparison.Equal => Mathf.Approximately(value, threshold),
                _ => false
            };

            var filteredTargets = targets.Where(t => MeetsTargetCriteria(t, cond)).ToList();
            bool countMet = filteredTargets.Count >= cond.MinTargetCount && (cond.MaxTargetCount == 0 || filteredTargets.Count <= cond.MaxTargetCount);

            bool result = statMet && countMet;
            Debug.Log($"EvaluateCondition for {unit.Id}: Stat={cond.Stat}, Target={cond.Target}, Value={value}, Threshold={threshold}, StatMet={statMet}, CountMet={countMet}, Result={result}");
            return result;
        }

        private static bool MeetsCondition(CharacterStats target, CombatTypes.AbilityCondition cond)
        {
            if (target == null) return false;
            float value = GetStatValue(cond.Stat, target);
            if (cond.IsPercentage)
            {
                float maxValue = cond.Stat == CombatTypes.Stat.Health ? target.MaxHealth : cond.Stat == CombatTypes.Stat.Morale ? target.MaxMorale : 1f;
                value = maxValue > 0 ? value / maxValue : 0f;
            }
            bool met = cond.Comparison switch
            {
                CombatTypes.Comparison.Greater => value > cond.Threshold,
                CombatTypes.Comparison.Lesser => value < cond.Threshold,
                CombatTypes.Comparison.Equal => Mathf.Approximately(value, cond.Threshold),
                _ => false
            };
            return met;
        }

        private static float GetStatValue(CombatTypes.Stat stat, CharacterStats unit)
        {
            if (unit == null) return 0f;
            return stat switch
            {
                CombatTypes.Stat.Health => unit.Health,
                CombatTypes.Stat.MaxHealth => unit.MaxHealth,
                CombatTypes.Stat.Morale => unit.Morale,
                CombatTypes.Stat.MaxMorale => unit.MaxMorale,
                CombatTypes.Stat.Speed => unit.Speed,
                CombatTypes.Stat.Attack => unit.Attack,
                CombatTypes.Stat.Defense => unit.Defense,
                CombatTypes.Stat.Evasion => unit.Evasion,
                CombatTypes.Stat.Rank => unit.Rank,
                CombatTypes.Stat.Infectivity => unit.Infectivity,
                CombatTypes.Stat.PartyPosition => unit.PartyPosition,
                _ => 0f
            };
        }

        private static List<ICombatUnit> GetTeam(CombatTypes.TeamTarget team, CharacterStats unit, PartyData party, List<ICombatUnit> targets)
        {
            return team switch
            {
                CombatTypes.TeamTarget.Allies => unit.Type == CharacterType.Hero ? party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated).ToList() : targets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated).ToList(),
                CombatTypes.TeamTarget.Enemies => unit.Type == CharacterType.Hero ? targets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated).ToList() : party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated).ToList(),
                CombatTypes.TeamTarget.Both => party.HeroStats.Cast<ICombatUnit>().Concat(targets.Where(t => !t.IsHero)).Where(u => u.Health > 0 && !u.HasRetreated).ToList(),
                _ => new List<ICombatUnit>()
            };
        }

        private static bool MeetsTargetCriteria(ICombatUnit target, CombatTypes.AbilityCondition cond)
        {
            if (target.PartyPosition < cond.MinPosition || (cond.MaxPosition > 0 && target.PartyPosition > cond.MaxPosition)) return false;
            return true;
        }
    }
}