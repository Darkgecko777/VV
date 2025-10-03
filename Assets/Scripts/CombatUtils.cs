using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace VirulentVentures
{
    public static class CombatUtils
    {
        public static List<ICombatUnit> SelectTargets(CharacterStats user, List<ICombatUnit> targetPool, PartyData partyData, CombatTypes.TargetingRule rule, List<CharacterStats> heroPositions, List<CharacterStats> monsterPositions)
        {
            if (targetPool == null || targetPool.Count == 0)
            {
                Debug.LogWarning($"CombatUtils: Empty targetPool for {user.Id}. Returning empty list.");
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
            if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.LowestHealth)
            {
                selectedPool = selectedPool.OrderBy(t => (t as CharacterStats)?.Health / (float)(t as CharacterStats)?.MaxHealth ?? float.MaxValue).ToList();
            }
            // Default: No sorting, take first valid unit (frontmost in ordered list)

            return selectedPool.Take(1).ToList();
        }

        public static void ApplyEffect(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback)
        {
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
                    }
                }
                else if (ability.EffectId == "MinorHeal")
                {
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
                        eventBus.RaiseUnitDamaged(target, healMessage); // Reusing event for consistency
                        updateUnitCallback(target);
                    }
                }
                // Future effect types can be added here
            }
        }
    }
}