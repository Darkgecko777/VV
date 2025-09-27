using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace VirulentVentures
{
    public static class AbilityDatabase
    {
        private static readonly Dictionary<string, IAbility> heroAbilities = new Dictionary<string, IAbility>();
        private static readonly Dictionary<string, IAbility> monsterAbilities = new Dictionary<string, IAbility>();
        private static readonly Dictionary<string, string[]> heroAbilityMap = new Dictionary<string, string[]>
        {
            { "Fighter", new[] { "FighterMeleeAttack", "FighterShieldBash", "FighterCoupDeGrace", "BasicAttack" } },
            { "Monk", new[] { "MonkBasicAttack", "MonkChiStrike", "MonkMeditate", "BasicAttack" } },
            { "Scout", new[] { "ScoutBasicAttack", "ScoutSniperShot", "ScoutEnhanceWeaponry", "BasicAttack" } },
            { "Healer", new[] { "HealerBasicAttack", "HealerHeal", "HealerSteelResolve", "BasicAttack" } }
        };
        private static readonly Dictionary<string, string[]> monsterAbilityMap = new Dictionary<string, string[]>
        {
            { "Mire Shambler", new[] { "ShamblerThornNeedle", "ShamblerSwampBrambles", "BasicAttack" } },
            { "Bog Fiend", new[] { "FiendMeleeAttack", "FiendSludgeSlam", "FiendDrainHealth", "BasicAttack" } },
            { "Umbral Corvax", new[] { "CorvaxBasicAttack", "CorvaxMortifyingShriek", "CorvaxWindsOfTerror", "BasicAttack" } },
            { "Wraith", new[] { "WraithStrike", "WraithHoaryGrasp", "BasicAttack" } }
        };

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

            heroAbilities.Clear();
            monsterAbilities.Clear();

            var abilityTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IAbility).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in abilityTypes)
            {
                try
                {
                    var ability = Activator.CreateInstance(type) as IAbility;
                    if (ability == null) continue;

                    bool isHero = heroAbilityMap.Values.Any(ids => ids.Contains(ability.Id));
                    bool isMonster = monsterAbilityMap.Values.Any(ids => ids.Contains(ability.Id));

                    if (isHero) heroAbilities[ability.Id] = ability;
                    if (isMonster) monsterAbilities[ability.Id] = ability;

                    Debug.Log($"AbilityDatabase: Registered {ability.Id} ({type.Name}).");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AbilityDatabase: Failed to instantiate {type.Name}: {ex.Message}");
                }
            }

            foreach (var entry in heroAbilityMap)
            {
                foreach (var id in entry.Value)
                {
                    if (!heroAbilities.ContainsKey(id))
                        Debug.LogWarning($"AbilityDatabase: Hero ability {id} for {entry.Key} not found.");
                }
            }
            foreach (var entry in monsterAbilityMap)
            {
                foreach (var id in entry.Value)
                {
                    if (!monsterAbilities.ContainsKey(id))
                        Debug.LogWarning($"AbilityDatabase: Monster ability {id} for {entry.Key} not found.");
                }
            }
        }

        public static string[] GetCharacterAbilityIds(string characterId, CharacterType type)
        {
            var map = type == CharacterType.Hero ? heroAbilityMap : monsterAbilityMap;
            if (map.TryGetValue(characterId, out var ids)) return ids;
            Debug.LogWarning($"AbilityDatabase: No abilities mapped for {characterId} ({type}). Returning empty.");
            return new string[0];
        }

        public static IAbility GetHeroAbility(string id)
        {
            heroAbilities.TryGetValue(id, out var ability);
            if (ability == null) Debug.LogWarning($"AbilityDatabase: Hero ability {id} not found.");
            return ability;
        }

        public static IAbility GetMonsterAbility(string id)
        {
            monsterAbilities.TryGetValue(id, out var ability);
            if (ability == null) Debug.LogWarning($"AbilityDatabase: Monster ability {id} not found.");
            return ability;
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
                string msg = $"No abilities assigned to {unit.Id}.";
                Debug.LogError(msg);
                return (null, msg);
            }

            IAbility selected = null;
            int lowestPriority = int.MaxValue;
            string failMessage = null;

            foreach (var id in unit.abilityIds)
            {
                var ability = unit.Type == CharacterType.Hero ? GetHeroAbility(id) : GetMonsterAbility(id);
                if (ability == null)
                {
                    Debug.LogWarning($"Invalid ability ID {id} for {unit.Id}. Skipping.");
                    continue;
                }

                if (state.AbilityCooldowns.TryGetValue(id, out int actionCd) && actionCd > 0) continue;
                if (state.RoundCooldowns.TryGetValue(id, out int roundCd) && roundCd > 0) continue;
                if (ability.Rank > 0 && unit.Rank < ability.Rank)
                {
                    failMessage = $"Ability {id} requires Rank {ability.Rank}, unit has {unit.Rank}.";
                    Debug.LogWarning(failMessage);
                    continue;
                }

                bool met = ability.Conditions.All(c => EvaluateCondition(c, unit, partyData, targets));
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
            }

            return (selectedId, failMessage);
        }

        private static bool EvaluateCondition(CombatTypes.AbilityCondition cond, CharacterStats unit, PartyData party, List<ICombatUnit> targets)
        {
            float value = GetStatValue(cond.Stat, unit);
            if (cond.TeamCondition != CombatTypes.TeamCondition.None)
            {
                var team = GetTeam(cond.TeamTarget, unit, party, targets);
                if (team.Count == 0) return false;
                value = cond.TeamCondition == CombatTypes.TeamCondition.AverageStat
                    ? team.Average(u => GetStatValue(cond.Stat, u as CharacterStats))
                    : team.Sum(u => GetStatValue(cond.Stat, u as CharacterStats));
            }

            float threshold = cond.IsPercentage ? cond.Threshold * GetStatValue(CombatTypes.Stat.MaxHealth, unit) : cond.Threshold;

            bool statMet = cond.Comparison switch
            {
                CombatTypes.Comparison.Greater => value > threshold,
                CombatTypes.Comparison.Lesser => value < threshold,
                CombatTypes.Comparison.Equal => Mathf.Approximately(value, threshold),
                _ => false
            };

            var filteredTargets = targets.Where(t => MeetsTargetCriteria(t, cond)).ToList();
            bool countMet = filteredTargets.Count >= cond.MinTargetCount && (cond.MaxTargetCount == 0 || filteredTargets.Count <= cond.MaxTargetCount);

            return statMet && countMet;
        }

        private static float GetStatValue(CombatTypes.Stat stat, CharacterStats unit)
        {
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
                CombatTypes.TeamTarget.Allies => unit.Type == CharacterType.Hero ? party.HeroStats.Cast<ICombatUnit>().ToList() : targets.Where(t => !t.IsHero).ToList(),
                CombatTypes.TeamTarget.Enemies => unit.Type == CharacterType.Hero ? targets.Where(t => !t.IsHero).ToList() : party.HeroStats.Cast<ICombatUnit>().ToList(),
                CombatTypes.TeamTarget.Both => party.HeroStats.Cast<ICombatUnit>().Concat(targets.Where(t => !t.IsHero)).ToList(),
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