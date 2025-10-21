using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "StrikeEffect", menuName = "VirulentVentures/Effects/Strike")]
    public class StrikeEffectSO : EffectSO
    {
        [SerializeField] private float multiplier = 1f;
        [SerializeField] private GameTypes.DefenseCheck defenseType = GameTypes.DefenseCheck.Standard;

        public float Multiplier => multiplier;
        public GameTypes.DefenseCheck DefenseType => defenseType;

        public override (TransmissionVector? changedVector, float delta) Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            bool applied = false;
            float totalDelta = 0f;
            foreach (var target in targets.ToList())
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null) continue;

                if (ability.Dodgeable && CombatUtils.CheckEvasion(targetStats))
                {
                    string dodgeMessage = $"{targetStats.Id} dodges {user.Id}'s {abilityId}!";
                    combatLogs.Add(dodgeMessage);
                    eventBus.RaiseLogMessage(dodgeMessage, Color.yellow);
                    applied = true;
                    continue;
                }

                int damage = DefenseType == GameTypes.DefenseCheck.Standard
                    ? (user.Attack * (100 - targetStats.Defense * 5)) / 100
                    : user.Attack;
                damage = Mathf.Max(0, Mathf.RoundToInt(damage * Multiplier));

                // Check for Thorns
                var targetState = combatScene.GetUnitAttackState(target);
                if (targetState != null && targetState.ActiveEffects.TryGetValue(GameTypes.StatusEffectType.Thorns, out var thorns) && thorns.stacks > 0)
                {
                    int reflectDamage = targetStats.Defense; // Reflect damage equal to target's Defense
                    user.Health -= Mathf.Max(0, reflectDamage);
                    totalDelta += reflectDamage; // Add to delta for virus transmission
                    string reflectMessage = $"{targetStats.Id}'s Thorns reflect {reflectDamage} damage to {user.Id}!";
                    combatLogs.Add(reflectMessage);
                    eventBus.RaiseLogMessage(reflectMessage, Color.red);
                    eventBus.RaiseUnitDamaged(user, reflectMessage);
                    updateUnitCallback(user);

                    // Consume one stack
                    targetState.ActiveEffects[GameTypes.StatusEffectType.Thorns] = (thorns.stacks - 1, thorns.duration);
                    if (targetState.ActiveEffects[GameTypes.StatusEffectType.Thorns].stacks <= 0)
                    {
                        targetState.ActiveEffects.Remove(GameTypes.StatusEffectType.Thorns);
                        string thornsEndMessage = $"{targetStats.Id}'s Thorns effect ends!";
                        combatLogs.Add(thornsEndMessage);
                        eventBus.RaiseLogMessage(thornsEndMessage, Color.yellow);
                    }
                }

                // Check for HealthShield
                if (targetState != null && targetState.ActiveEffects.TryGetValue(GameTypes.StatusEffectType.HealthShield, out var healthShield) && healthShield.stacks > 0)
                {
                    string shieldMessage = $"{targetStats.Id}'s HealthShield negates {damage} damage from {user.Id}'s {abilityId}!";
                    combatLogs.Add(shieldMessage);
                    eventBus.RaiseLogMessage(shieldMessage, Color.cyan);
                    targetState.ActiveEffects[GameTypes.StatusEffectType.HealthShield] = (healthShield.stacks - 1, healthShield.duration);
                    if (targetState.ActiveEffects[GameTypes.StatusEffectType.HealthShield].stacks <= 0)
                    {
                        targetState.ActiveEffects.Remove(GameTypes.StatusEffectType.HealthShield);
                        string shieldEndMessage = $"{targetStats.Id}'s HealthShield effect ends!";
                        combatLogs.Add(shieldEndMessage);
                        eventBus.RaiseLogMessage(shieldEndMessage, Color.yellow);
                    }
                    applied = true;
                    continue;
                }

                if (damage > 0)
                {
                    targetStats.Health -= damage;
                    totalDelta += damage; // Accumulate damage for delta
                    string damageMessage = $"{user.Id} hits {targetStats.Id} for {damage} {(DefenseType == GameTypes.DefenseCheck.None ? "true " : "")}damage with {abilityId} <color=#FFFF00>[{user.Attack} ATK{(DefenseType == GameTypes.DefenseCheck.Standard ? $" * (100 - {targetStats.Defense} DEF * 5) / 100" : "")} * {Multiplier}]</color>";
                    combatLogs.Add(damageMessage);
                    eventBus.RaiseLogMessage(damageMessage, uiConfig.TextColor);
                    eventBus.RaiseUnitDamaged(target, damageMessage);
                    updateUnitCallback(target);
                    applied = true;
                }
            }
            return applied ? (TransmissionVector.Health, totalDelta) : (null, 0f);
        }
    }
}