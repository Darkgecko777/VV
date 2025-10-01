using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace VirulentVentures
{
    public static class CombatUtils
    {
        public static List<ICombatUnit> SelectTargets(CharacterStats user, List<ICombatUnit> targetPool, PartyData partyData, CombatTypes.TargetingRule rule, bool isMelee, CombatTypes.ConditionTarget targetType, int numberOfTargets, List<CharacterStats> heroPositions, List<CharacterStats> monsterPositions)
        {
            Debug.Log($"SelectTargets for {user.Id}: Initial targetPool count = {targetPool.Count}");
            if (targetPool == null || targetPool.Count == 0)
            {
                Debug.LogWarning($"CombatUtils: Empty targetPool for {user.Id}. Returning empty list.");
                return new List<ICombatUnit>();
            }

            var filteredPool = targetPool;
            if (rule.MinPosition > 0 || rule.MaxPosition > 0)
            {
                filteredPool = filteredPool.Where(t =>
                {
                    var pos = user.Type == CharacterType.Hero
                        ? monsterPositions.FirstOrDefault(m => m == t as CharacterStats)?.PartyPosition
                        : heroPositions.FirstOrDefault(h => h == t as CharacterStats)?.PartyPosition;
                    return pos.HasValue && pos.Value >= rule.MinPosition && (rule.MaxPosition == 0 || pos.Value <= rule.MaxPosition);
                }).ToList();
                Debug.Log($"TargetPool after position filter count = {filteredPool.Count}");
            }
            if (isMelee || rule.MeleeOnly)
            {
                if (filteredPool.Count > 0)
                {
                    filteredPool = filteredPool.Where(t =>
                    {
                        var pos = user.Type == CharacterType.Hero
                            ? monsterPositions.FirstOrDefault(m => m == t as CharacterStats)?.PartyPosition
                            : heroPositions.FirstOrDefault(h => h == t as CharacterStats)?.PartyPosition;
                        return pos.HasValue && pos.Value <= 2;
                    }).ToList();
                    if (filteredPool.Count == 0)
                    {
                        Debug.LogWarning($"CombatUtils: No frontline targets for {user.Id}'s melee attack.");
                        return new List<ICombatUnit>();
                    }
                    Debug.Log($"TargetPool after melee filter count = {filteredPool.Count}");
                }
            }
            if (rule.MustBeInfected || rule.MustNotBeInfected)
            {
                filteredPool = filteredPool.Where(t =>
                {
                    var stats = t as CharacterStats;
                    bool isInfected = stats != null && stats.IsInfected;
                    return rule.MustBeInfected ? isInfected : rule.MustNotBeInfected ? !isInfected : true;
                }).ToList();
                Debug.Log($"TargetPool after infection filter count = {filteredPool.Count}");
            }

            switch (rule.Type)
            {
                case CombatTypes.TargetingRule.RuleType.LowestHealth:
                    filteredPool = filteredPool.OrderBy(t => (t as CharacterStats)?.Health ?? int.MaxValue).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.HighestHealth:
                    filteredPool = filteredPool.OrderByDescending(t => (t as CharacterStats)?.Health ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.LowestMorale:
                    filteredPool = filteredPool.OrderBy(t => (t as CharacterStats)?.Morale ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.HighestMorale:
                    filteredPool = filteredPool.OrderByDescending(t => (t as CharacterStats)?.Morale ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.LowestAttack:
                    filteredPool = filteredPool.OrderBy(t => (t as CharacterStats)?.Attack ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.HighestAttack:
                    filteredPool = filteredPool.OrderByDescending(t => (t as CharacterStats)?.Attack ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.AllAllies:
                    if (targetType != CombatTypes.ConditionTarget.Ally)
                    {
                        Debug.LogWarning($"CombatUtils: AllAllies rule requires Ally target for {user.Id}. Returning empty list.");
                        filteredPool = new List<ICombatUnit>();
                    }
                    break;
                case CombatTypes.TargetingRule.RuleType.WeightedRandom:
                    if (rule.WeightStat == default)
                    {
                        Debug.LogWarning($"CombatUtils: WeightedRandom for {user.Id} has no WeightStat. Defaulting to Health.");
                        rule.WeightStat = CombatTypes.Stat.Health;
                    }
                    filteredPool = filteredPool.OrderBy(t => UnityEngine.Random.value * GetWeight(t as CharacterStats, rule.WeightStat, rule.WeightFactor)).ToList();
                    break;
                default:
                    filteredPool = filteredPool.OrderBy(t => UnityEngine.Random.value).ToList();
                    break;
            }

            int maxTargets = Mathf.Min(numberOfTargets, filteredPool.Count);
            if (rule.Type == CombatTypes.TargetingRule.RuleType.AllAllies && targetType == CombatTypes.ConditionTarget.Ally)
                maxTargets = filteredPool.Count;
            var selected = filteredPool.Take(maxTargets).ToList();
            Debug.Log($"Selected targets count = {selected.Count}");
            return selected;
        }

        private static float GetWeight(CharacterStats unit, CombatTypes.Stat stat, float factor)
        {
            if (unit == null) return 1f;
            float value = stat switch
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
                _ => 1f
            };
            return value * factor;
        }

        public static void ApplyAttackDamage(CharacterStats user, List<ICombatUnit> targets, AbilitySO.AbilityAction action, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, UnitAttackState targetState, Action<ICombatUnit> updateUnitCallback)
        {
            foreach (var target in targets.ToList())
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null || targetState == null) continue;

                int originalDefense = targetStats.Defense;
                int currentEvasion = targetStats.Evasion;
                if (targetState.TempStats.TryGetValue("Defense", out var defMod)) targetStats.Defense += defMod.value;
                if (targetState.TempStats.TryGetValue("Evasion", out var evaMod)) targetStats.Evasion += evaMod.value;

                bool attackDodged = false;
                if (action.Dodgeable)
                {
                    float dodgeChance = Mathf.Clamp(currentEvasion, 0, 100) / 100f;
                    float randomRoll = UnityEngine.Random.value;
                    if (randomRoll <= dodgeChance)
                    {
                        string dodgeMessage = $"{targetStats.Id} dodges the attack! <color=#FFFF00>[{currentEvasion}% Evasion Chance, Roll: {randomRoll:F2} <= {dodgeChance:F2}]</color>";
                        combatLogs.Add(dodgeMessage);
                        eventBus.RaiseLogMessage(dodgeMessage, Color.green);
                        attackDodged = true;
                    }
                    else
                    {
                        string failDodgeMessage = $"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance, Roll: {randomRoll:F2} > {dodgeChance:F2}]</color>";
                        combatLogs.Add(failDodgeMessage);
                        eventBus.RaiseLogMessage(failDodgeMessage, Color.red);
                    }
                }

                int damage = 0;
                if (!attackDodged)
                {
                    if (action.Defense == CombatTypes.DefenseCheck.Standard)
                        damage = Mathf.Max(0, Mathf.RoundToInt(user.Attack * (1f - 0.05f * targetStats.Defense)));
                    else if (action.Defense == CombatTypes.DefenseCheck.Partial)
                        damage = Mathf.Max(0, Mathf.RoundToInt(user.Attack * (1f - action.PartialDefenseMultiplier * targetStats.Defense)));
                    else
                        damage = Mathf.Max(0, user.Attack);

                    string damageFormula = $"[{user.Attack} ATK - {targetStats.Defense} DEF * {(action.Defense == CombatTypes.DefenseCheck.Partial ? action.PartialDefenseMultiplier : 0.05f) * 100}%]";
                    if (damage > 0)
                    {
                        targetStats.Health -= damage;
                        string damageMessage = $"{user.Id} hits {targetStats.Id} for {damage} damage with {abilityId} <color=#FFFF00>{damageFormula}</color>";
                        combatLogs.Add(damageMessage);
                        eventBus.RaiseLogMessage(damageMessage, uiConfig.TextColor);
                        eventBus.RaiseUnitDamaged(target, damageMessage);
                        updateUnitCallback(target);
                    }

                    if (targetState.TempStats.TryGetValue("Reflect", out var reflect) && !attackDodged && damage > 0)
                    {
                        int reflectDamage = Mathf.RoundToInt(damage * reflect.value / 100f);
                        user.Health = Mathf.Max(0, user.Health - reflectDamage);
                        string reflectMessage = $"{user.Id} takes {reflectDamage} reflected damage from {targetStats.Id}!";
                        combatLogs.Add(reflectMessage);
                        eventBus.RaiseLogMessage(reflectMessage, Color.red);
                        eventBus.RaiseUnitDamaged(user, reflectMessage);
                        updateUnitCallback(user);
                    }
                }

                targetStats.Defense = originalDefense;
                targetStats.Evasion = currentEvasion;
            }
        }

        public static void ProcessAction(CharacterStats user, List<ICombatUnit> targets, AbilitySO.AbilityAction action, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, UnitAttackState targetState)
        {
            foreach (var target in targets)
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null || targetState == null) continue;
                EffectReference.Apply(action.EffectId, user, targetStats, action.EffectValue, action.EffectDuration, targetState, eventBus, uiConfig);
            }
        }
    }
}