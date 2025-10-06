using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "VirulentVentures/Effects/Heal")]
    public class HealEffectSO : EffectSO
    {
        [SerializeField] private int amount = 15;
        [SerializeField] private float thresholdPercent = 80f;
        [SerializeField] private CombatTypes.TargetStat targetStat = CombatTypes.TargetStat.Health;

        public int Amount => amount;
        public float ThresholdPercent => thresholdPercent;
        public CombatTypes.TargetStat TargetStat => targetStat;

        public override bool Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            bool applied = false;
            foreach (var target in targets.ToList())
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null) continue;

                int currentValue = TargetStat == CombatTypes.TargetStat.Health ? targetStats.Health : targetStats.Morale;
                int maxValue = TargetStat == CombatTypes.TargetStat.Health ? targetStats.MaxHealth : targetStats.MaxMorale;
                if (ThresholdPercent > 0 && currentValue >= (ThresholdPercent / 100f) * maxValue)
                {
                    Debug.LogWarning($"{targetStats.Id} is too healthy for {abilityId} by {user.Id} (>= {ThresholdPercent}% {TargetStat}).");
                    continue;
                }

                int healAmount = Amount;
                int newValue = Mathf.Min(currentValue + healAmount, maxValue);

                if (newValue > currentValue)
                {
                    int healed = newValue - currentValue;
                    if (TargetStat == CombatTypes.TargetStat.Health)
                        targetStats.Health = newValue;
                    else
                        targetStats.Morale = newValue;
                    string healMessage = $"{user.Id} {(TargetStat == CombatTypes.TargetStat.Health ? "heals" : "boosts")} {targetStats.Id} for {healed} {TargetStat.ToString().ToLower()} with {abilityId} <color=#00FF00>[+{healAmount} {TargetStat}, capped at {maxValue}]</color>";
                    combatLogs.Add(healMessage);
                    eventBus.RaiseLogMessage(healMessage, Color.green);
                    eventBus.RaiseUnitDamaged(target, healMessage);
                    updateUnitCallback(target);
                    applied = true;
                }
            }
            return applied;
        }
    }
}