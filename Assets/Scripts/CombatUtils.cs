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

        public static List<ICombatUnit> SelectTargets(CharacterStats user, List<ICombatUnit> targetPool, PartyData partyData, CombatTypes.TargetingRule rule, List<CharacterStats> heroPositions, List<CharacterStats> monsterPositions, AbilitySO ability, List<string> combatLogs, EventBusSO eventBus, UIConfig uiConfig)
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

            // Check user-based thresholds (e.g., SelfSacrifice for ChiStrike)
            foreach (var effect in ability.Effects)
            {
                if (effect is SelfSacrificeEffectSO selfSacrifice && selfSacrifice.ThresholdPercent > 0)
                {
                    float threshold = selfSacrifice.ThresholdPercent;
                    if (user.Health <= (threshold / 100f) * user.MaxHealth)
                    {
                        string lowHealthMessage = $"{user.Id} needs more than {threshold}% health to use {ability.Id}!";
                        combatLogs.Add(lowHealthMessage);
                        eventBus.RaiseLogMessage(lowHealthMessage, Color.red);
                        return new List<ICombatUnit>(); // Fail early to trigger fallback
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

            if (rule.Type == CombatTypes.TargetingRule.RuleType.Single)
            {
                if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.LowestHealth)
                {
                    selectedPool = selectedPool.OrderBy(t => (t as CharacterStats)?.Health / (float)(t as CharacterStats)?.MaxHealth ?? float.MaxValue).ToList();
                }
                else if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.Random)
                {
                    selectedPool = selectedPool.OrderBy(t => UnityEngine.Random.value).ToList();
                }
                return selectedPool.Take(1).ToList();
            }
            else if (rule.Type == CombatTypes.TargetingRule.RuleType.SingleConditional)
            {
                if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.LowestHealth)
                {
                    selectedPool = selectedPool.OrderBy(t => (t as CharacterStats)?.Health / (float)(t as CharacterStats)?.MaxHealth ?? float.MaxValue).ToList();
                }
                else if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.Random)
                {
                    selectedPool = selectedPool.OrderBy(t => UnityEngine.Random.value).ToList();
                }

                foreach (var target in selectedPool)
                {
                    var stats = target as CharacterStats;
                    if (stats == null) continue;

                    // Check target-based thresholds (e.g., Heal, InstantKill)
                    bool validTarget = false;
                    foreach (var effect in ability.Effects)
                    {
                        float threshold = 0f;
                        if (effect is HealEffectSO heal && heal.ThresholdPercent > 0)
                        {
                            threshold = heal.ThresholdPercent;
                            int currentValue = heal.TargetStat == CombatTypes.TargetStat.Health ? stats.Health : stats.Morale;
                            int maxValue = heal.TargetStat == CombatTypes.TargetStat.Health ? stats.MaxHealth : stats.MaxMorale;
                            if (currentValue < (threshold / 100f) * maxValue)
                            {
                                validTarget = true;
                                break;
                            }
                        }
                        else if (effect is InstantKillEffectSO instantKill && instantKill.ThresholdPercent > 0)
                        {
                            threshold = instantKill.ThresholdPercent;
                            if (stats.Health < (threshold / 100f) * stats.MaxHealth)
                            {
                                validTarget = true;
                                break;
                            }
                        }
                        else
                        {
                            validTarget = true; // No threshold, target is valid
                            break;
                        }
                    }

                    if (validTarget)
                    {
                        return new List<ICombatUnit> { target };
                    }
                }
                return new List<ICombatUnit>();
            }
            else if (rule.Type == CombatTypes.TargetingRule.RuleType.All)
            {
                if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.LowestHealth)
                {
                    selectedPool = selectedPool.OrderBy(t => (t as CharacterStats)?.Health / (float)(t as CharacterStats)?.MaxHealth ?? float.MaxValue).ToList();
                }
                else if (rule.Criteria == CombatTypes.TargetingRule.SelectionCriteria.Random)
                {
                    selectedPool = selectedPool.OrderBy(t => UnityEngine.Random.value).ToList();
                }
                return selectedPool; // Return all valid targets
            }

            return selectedPool.Take(1).ToList();
        }

        public static bool ApplyEffect(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            if (ability == null || attackState == null || combatScene == null || ability.Effects == null)
            {
                Debug.LogWarning($"CombatUtils: Null ability, attackState, combatScene, or effects for {user.Id}.");
                return false;
            }

            // Check cooldown
            bool isOnCooldown = false;
            if (ability.CooldownParams.Type != CombatTypes.CooldownType.None)
            {
                if (ability.CooldownParams.Type == CombatTypes.CooldownType.Actions && attackState.AbilityCooldowns.ContainsKey(abilityId) && attackState.AbilityCooldowns[abilityId] > 0 ||
                    ability.CooldownParams.Type == CombatTypes.CooldownType.Rounds && attackState.RoundCooldowns.ContainsKey(abilityId) && attackState.RoundCooldowns[abilityId] > 0)
                {
                    string cooldownMessage = $"{user.Id}'s {abilityId} is on cooldown ({attackState.AbilityCooldowns.GetValueOrDefault(abilityId, 0)} actions/{attackState.RoundCooldowns.GetValueOrDefault(abilityId, 0)} rounds remaining).";
                    combatLogs.Add(cooldownMessage);
                    eventBus.RaiseLogMessage(cooldownMessage, Color.yellow);
                    return false;
                }
            }

            bool applied = false;
            foreach (var effect in ability.Effects)
            {
                applied |= effect.Execute(user, targets, ability, abilityId, eventBus, uiConfig, combatLogs, updateUnitCallback, attackState, combatScene);
            }

            if (applied && ability.CooldownParams.Type != CombatTypes.CooldownType.None)
            {
                if (ability.CooldownParams.Type == CombatTypes.CooldownType.Actions)
                {
                    attackState.AbilityCooldowns[abilityId] = ability.CooldownParams.Duration;
                }
                else if (ability.CooldownParams.Type == CombatTypes.CooldownType.Rounds)
                {
                    attackState.RoundCooldowns[abilityId] = ability.CooldownParams.Duration;
                }
                string cooldownAppliedMessage = $"{user.Id}'s {abilityId} is now on cooldown for {ability.CooldownParams.Duration} {ability.CooldownParams.Type.ToString().ToLower()}.";
                combatLogs.Add(cooldownAppliedMessage);
                eventBus.RaiseLogMessage(cooldownAppliedMessage, Color.yellow);
            }

            return applied;
        }
    }
}