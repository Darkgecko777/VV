using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "VirulentVentures/Effects/ApplyStatusEffect")]
    public class ApplyStatusEffectSO : EffectSO
    {
        [SerializeField] private GameTypes.StatusEffectType effectType;
        [SerializeField] private int stacksToAdd = 1;
        [SerializeField] private int maxStacks = 1;
        [SerializeField] private int duration = 1; // Default to 1 round; -1 for permanent

        public GameTypes.StatusEffectType EffectType => effectType;
        public int StacksToAdd => stacksToAdd;
        public int MaxStacks => maxStacks;
        public int Duration => duration;

        public override (TransmissionVector? changedVector, float delta) Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            bool applied = false;

            // Determine targets: if TargetSelf, apply to user; else to provided targets
            var effectTargets = ability.Rule.TargetSelf ? new List<ICombatUnit> { user } : targets;

            foreach (var target in effectTargets.ToList())
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null || targetStats.Health <= 0 || targetStats.HasRetreated) continue;

                var targetState = combatScene.GetUnitAttackState(target);
                if (targetState == null)
                {
                    Debug.LogWarning($"ApplyStatusEffectSO: No UnitAttackState found for {targetStats.Id}. Skipping.");
                    continue;
                }

                // Apply or update the status effect
                if (targetState.ActiveEffects.TryGetValue(EffectType, out var current))
                {
                    int newStacks = Mathf.Min(current.stacks + StacksToAdd, MaxStacks);
                    int newDuration = Mathf.Max(current.duration, Duration); // Refresh to max duration
                    targetState.ActiveEffects[EffectType] = (newStacks, newDuration);
                }
                else
                {
                    targetState.ActiveEffects[EffectType] = (StacksToAdd, Duration);
                }

                string applyMessage = $"{user.Id} applies {EffectType} ({StacksToAdd} stacks, {Duration} rounds) to {targetStats.Id} with {abilityId}!";
                combatLogs.Add(applyMessage);
                eventBus.RaiseLogMessage(applyMessage, Color.cyan);
                eventBus.RaiseUnitUpdated(target, targetStats.GetDisplayStats());
                updateUnitCallback(target);
                applied = true;
            }

            return applied ? (TransmissionVector.Buff, 0f) : (null, 0f); // Buff vector, no delta since no direct stat change
        }
    }
}