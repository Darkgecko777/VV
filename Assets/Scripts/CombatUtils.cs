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
            if (rule.MeleeOnly)
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
                case CombatTypes.TargetingRule.RuleType.Single:
                    filteredPool = filteredPool.OrderBy(t => (t as CharacterStats)?.Health / (float)(t as CharacterStats)?.MaxHealth ?? float.MaxValue).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.MeleeSingle:
                    filteredPool = filteredPool.OrderBy(t => (t as CharacterStats)?.Health / (float)(t as CharacterStats)?.MaxHealth ?? float.MaxValue).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.All:
                    break;
                case CombatTypes.TargetingRule.RuleType.MeleeAll:
                    break;
                default:
                    filteredPool = filteredPool.OrderBy(t => UnityEngine.Random.value).ToList();
                    break;
            }

            int maxTargets = rule.Type == CombatTypes.TargetingRule.RuleType.Single || rule.Type == CombatTypes.TargetingRule.RuleType.MeleeSingle ? 1 : filteredPool.Count;
            var selected = filteredPool.Take(maxTargets).ToList();
            Debug.Log($"Selected targets count = {selected.Count}");
            return selected;
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
                    damage = action.Defense == CombatTypes.DefenseCheck.Standard
                        ? Mathf.Max(0, Mathf.RoundToInt(user.Attack * (1f - 0.05f * targetStats.Defense)))
                        : Mathf.Max(0, user.Attack);

                    string damageFormula = $"[{user.Attack} ATK - {targetStats.Defense} DEF * 0.05]";
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
                EffectReference.Apply(action.EffectId, user, targetStats, 0f, 0, targetState, eventBus, uiConfig);
            }
        }
    }
}