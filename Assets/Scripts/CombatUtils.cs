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
                return selectedPool.Take(1).ToList();
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
                var (changedVector, delta) = effect.Execute(user, targets, ability, abilityId, eventBus, uiConfig, combatLogs, updateUnitCallback, attackState, combatScene);
                applied |= (changedVector != null); // Consider applied if stat changed

                // Trigger virus transmission for each target if a stat was changed
                if (changedVector != null && targets.Any())
                {
                    foreach (var target in targets.OfType<CharacterStats>())
                    {
                        combatScene.TryInfectUnit(user, target, changedVector.Value, delta, combatLogs, eventBus, uiConfig);
                    }
                }
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