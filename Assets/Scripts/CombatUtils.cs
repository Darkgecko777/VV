using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace VirulentVentures
{
    public static class CombatUtils
    {
        public static List<ICombatUnit> SelectTargets(CharacterStats user, List<ICombatUnit> targetPool, PartyData partyData, CombatTypes.TargetingRule rule, List<CharacterStats> heroPositions, List<CharacterStats> monsterPositions, AbilitySO ability)
        {
            if (targetPool == null || targetPool.Count == 0)
            {
                Debug.LogWarning($"CombatUtils: Empty targetPool for {user.Id}. Returning empty list.");
                return new List<ICombatUnit>();
            }

            if (ability == null)
            {
                Debug.LogWarning($"CombatUtils: Null AbilitySO for {user.Id}. Returning empty list.");
                return new List<ICombatUnit>();
            }

            var selectedPool = targetPool;
            if (rule.MeleeOnly)
            {
                var enemyList = user.Type == CharacterType.Hero ? monsterPositions : heroPositions;
                selectedPool = selectedPool.Where(t =>
                {
                    var stats = t as CharacterStats;
                    return stats != null && enemyList.IndexOf(stats) < 2; // Dynamic: First 2 in ordered living list are melee-eligible
                }).ToList();
            }

            // Apply selection criteria
            if (rule.Type == CombatTypes.TargetingRule.RuleType.Single)
            {
                if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.LowestHealth)
                {
                    selectedPool = selectedPool.OrderBy(t => (t as CharacterStats)?.Health / (float)(t as CharacterStats)?.MaxHealth ?? float.MaxValue).ToList();
                }
                else if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.Random)
                {
                    // Shuffle the pool for random selection
                    selectedPool = selectedPool.OrderBy(t => UnityEngine.Random.value).ToList();
                }
                // Default: No sorting, take first valid unit (frontmost in ordered list)
                return selectedPool.Take(1).ToList();
            }
            else if (rule.Type == CombatTypes.TargetingRule.RuleType.SingleConditional)
            {
                // Apply LowestHealth sorting if specified
                if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.LowestHealth)
                {
                    selectedPool = selectedPool.OrderBy(t => (t as CharacterStats)?.Health / (float)(t as CharacterStats)?.MaxHealth ?? float.MaxValue).ToList();
                }
                else if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.Random)
                {
                    selectedPool = selectedPool.OrderBy(t => UnityEngine.Random.value).ToList();
                }
                // Default: No sorting, scan in order (frontmost first)

                // Scan for first target meeting condition (e.g., health threshold)
                foreach (var target in selectedPool)
                {
                    var stats = target as CharacterStats;
                    if (stats == null) continue;

                    // Check health threshold for abilities that use it
                    if (ability.EffectParameters.HealthThresholdPercent > 0)
                    {
                        float threshold = ability.EffectParameters.HealthThresholdPercent;
                        if (stats.Health < threshold * stats.MaxHealth / 100f)
                        {
                            return new List<ICombatUnit> { target }; // Return first qualifying target
                        }
                    }
                    else
                    {
                        return new List<ICombatUnit> { target }; // No threshold, take first valid
                    }
                }
                return new List<ICombatUnit>(); // No qualifying targets
            }

            return selectedPool.Take(1).ToList();
        }

        public static bool ApplyEffect(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback)
        {
            bool applied = false;
            foreach (var target in targets.ToList())
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null) continue;

                if (ability.EffectId == "Damage")
                {
                    // Calculate damage: attack * (100 - defense * 5) / 100
                    int damage = (user.Attack * (100 - targetStats.Defense * 5)) / 100;
                    damage = Mathf.Max(0, Mathf.RoundToInt(damage * ability.EffectParameters.Multiplier));

                    if (damage > 0)
                    {
                        targetStats.Health -= damage;
                        string damageMessage = $"{user.Id} hits {targetStats.Id} for {damage} damage with {abilityId} <color=#FFFF00>[{user.Attack} ATK * (100 - {targetStats.Defense} DEF * 5) / 100]</color>";
                        combatLogs.Add(damageMessage);
                        eventBus.RaiseLogMessage(damageMessage, uiConfig.TextColor);
                        eventBus.RaiseUnitDamaged(target, damageMessage);
                        updateUnitCallback(target);
                        applied = true;
                    }
                }
                else if (ability.EffectId == "MinorHeal")
                {
                    float threshold = ability.EffectParameters.HealthThresholdPercent > 0 ? ability.EffectParameters.HealthThresholdPercent : 80f;
                    if (targetStats.Health >= (threshold / 100f) * targetStats.MaxHealth)
                    {
                        Debug.LogWarning($"{targetStats.Id} is too healthy for {abilityId} by {user.Id} (>= {threshold}% HP).");
                        continue;
                    }

                    // Apply fixed 15 health heal, capped at MaxHealth
                    int healAmount = Mathf.RoundToInt(15 * ability.EffectParameters.Multiplier);
                    int newHealth = Mathf.Min(targetStats.Health + healAmount, targetStats.MaxHealth);

                    if (newHealth > targetStats.Health)
                    {
                        int healed = newHealth - targetStats.Health;
                        targetStats.Health = newHealth;
                        string healMessage = $"{user.Id} heals {targetStats.Id} for {healed} health with {abilityId} <color=#00FF00>[+{healAmount} HP, capped at {targetStats.MaxHealth}]</color>";
                        combatLogs.Add(healMessage);
                        eventBus.RaiseLogMessage(healMessage, Color.green);
                        eventBus.RaiseUnitDamaged(target, healMessage);
                        updateUnitCallback(target);
                        applied = true;
                    }
                }
                else if (ability.EffectId == "CoupDeGrace")
                {
                    // Health threshold check moved to SelectTargets for SingleConditional
                    targetStats.Health = 0;
                    string killMessage = $"{user.Id} executes {targetStats.Id} with {abilityId} <color=#FF0000>[Instant Kill]</color>";
                    combatLogs.Add(killMessage);
                    eventBus.RaiseLogMessage(killMessage, Color.red);
                    eventBus.RaiseUnitDamaged(target, killMessage);
                    updateUnitCallback(target);
                    applied = true;
                }
                // Future effect types can be added here
            }
            return applied;
        }
    }
}