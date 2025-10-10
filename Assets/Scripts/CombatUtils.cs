using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace VirulentVentures
{
    public static class CombatUtils
    {
        public static bool CheckEvasion(CharacterStats target)
        {
            if (target == null) return false;
            float evasionChance = target.Evasion / 100f;
            return UnityEngine.Random.value < evasionChance;
        }

        public static List<ICombatUnit> SelectTargets(CharacterStats user, List<ICombatUnit> targetPool, PartyData partyData, GameTypes.TargetingRule rule, List<CharacterStats> heroPositions, List<CharacterStats> monsterPositions, AbilitySO ability, List<string> combatLogs, EventBusSO eventBus, UIConfig uiConfig)
        {
            if (targetPool == null || targetPool.Count == 0)
            {
                return new List<ICombatUnit>();
            }

            if (ability == null)
            {
                return new List<ICombatUnit>();
            }

            // Check user-based thresholds (e.g., SelfSacrifice for ChiStrike)
            foreach (var effect in ability.Effects)
            {
                if (effect is SelfSacrificeEffectSO selfSacrifice && selfSacrifice.ThresholdPercent > 0)
                {
                    float threshold = selfSacrifice.ThresholdPercent;
                    if (user.Health <= (threshold / 100f) * user.MaxHealth)
                    {
                        return new List<ICombatUnit>(); // Fail silently to allow PerformAbility to try next ability
                    }
                }
            }

            var selectedPool = targetPool;
            if (rule.MeleeOnly)
            {
                var enemyList = user.Type == CharacterType.Hero ? monsterPositions : heroPositions;
                selectedPool = selectedPool.Where(t =>
                {
                    var stats = t as CharacterStats;
                    return stats != null && enemyList.IndexOf(stats) < 2;
                }).ToList();
            }

            if (rule.Type == GameTypes.TargetingRule.RuleType.Single || rule.Type == GameTypes.TargetingRule.RuleType.SingleConditional)
            {
                if (rule.Criteria == GameTypes.TargetingRule.SelectionCriteria.LowestHealth)
                {
                    selectedPool = selectedPool
                        .Select(t => t as CharacterStats)
                        .Where(t => t != null)
                        .OrderBy(t => (float)t.Health / t.MaxHealth)
                        .Cast<ICombatUnit>()
                        .ToList();
                    if (selectedPool.Any())
                    {
                        Debug.Log($"CombatUtils: Selected {selectedPool[0].Id} with {((CharacterStats)selectedPool[0]).Health / (float)((CharacterStats)selectedPool[0]).MaxHealth:F2} health ratio for {user.Id}'s {ability.Id}");
                    }
                    return selectedPool.Take(1).ToList();
                }
                else if (rule.Criteria == GameTypes.TargetingRule.SelectionCriteria.HighestHealth)
                {
                    selectedPool = selectedPool
                        .Select(t => t as CharacterStats)
                        .Where(t => t != null)
                        .OrderByDescending(t => (float)t.Health / t.MaxHealth)
                        .Cast<ICombatUnit>()
                        .ToList();
                    if (selectedPool.Any())
                    {
                        Debug.Log($"CombatUtils: Selected {selectedPool[0].Id} with {((CharacterStats)selectedPool[0]).Health / (float)((CharacterStats)selectedPool[0]).MaxHealth:F2} health ratio for {user.Id}'s {ability.Id}");
                    }
                    return selectedPool.Take(1).ToList();
                }
                else if (rule.Criteria == GameTypes.TargetingRule.SelectionCriteria.Random)
                {
                    selectedPool = selectedPool.OrderBy(t => UnityEngine.Random.value).ToList();
                    return selectedPool.Take(1).ToList();
                }
                return selectedPool.Take(1).ToList();
            }
            else if (rule.Type == GameTypes.TargetingRule.RuleType.All)
            {
                return selectedPool;
            }

            return new List<ICombatUnit>();
        }

        public static bool ExecuteAbility(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            if (ability == null || targets == null || attackState == null || combatScene == null || ability.Effects == null)
            {
                Debug.LogWarning($"CombatUtils: Invalid parameters for {user.Id}'s {abilityId}. Skipping execution.");
                return false;
            }

            // Check cooldown
            if (ability.CooldownParams.Type != GameTypes.CooldownType.None)
            {
                if (ability.CooldownParams.Type == GameTypes.CooldownType.Actions && attackState.AbilityCooldowns.ContainsKey(abilityId) && attackState.AbilityCooldowns[abilityId] > 0 ||
                    ability.CooldownParams.Type == GameTypes.CooldownType.Rounds && attackState.RoundCooldowns.ContainsKey(abilityId) && attackState.RoundCooldowns[abilityId] > 0)
                {
                    return false; // Skip silently
                }
            }

            bool applied = false;
            foreach (var effect in ability.Effects)
            {
                var (changedVector, delta) = effect.Execute(user, targets, ability, abilityId, eventBus, uiConfig, combatLogs, updateUnitCallback, attackState, combatScene);
                applied |= (changedVector != null); // Consider applied if stat changed

                // Trigger virus transmission for each target if a stat was changed
                if (changedVector != null && delta != 0f && targets.Any())
                {
                    foreach (var target in targets.OfType<CharacterStats>())
                    {
                        combatScene.TryInfectUnit(user, target, changedVector.Value, delta, combatLogs, eventBus, uiConfig);
                    }
                }
            }

            if (applied && ability.CooldownParams.Type != GameTypes.CooldownType.None)
            {
                if (ability.CooldownParams.Type == GameTypes.CooldownType.Actions)
                {
                    attackState.AbilityCooldowns[abilityId] = ability.CooldownParams.Duration;
                }
                else if (ability.CooldownParams.Type == GameTypes.CooldownType.Rounds)
                {
                    attackState.RoundCooldowns[abilityId] = ability.CooldownParams.Duration;
                }
            }

            return applied;
        }
    }
}