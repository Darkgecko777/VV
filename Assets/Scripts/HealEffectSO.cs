using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "HealEffect", menuName = "VirulentVentures/Effects/Heal")]
    public class HealEffectSO : EffectSO
    {
        [SerializeField] private int flatAmount = 15;
        [SerializeField] private float percentAmount = 0f;
        [SerializeField] private bool usePercentage = false;
        [SerializeField] private float thresholdPercent = 80f;
        [SerializeField] private GameTypes.TargetStat targetStat = GameTypes.TargetStat.Health;

        public int FlatAmount => flatAmount;
        public float PercentAmount => percentAmount;
        public bool UsePercentage => usePercentage;
        public float ThresholdPercent => thresholdPercent;
        public GameTypes.TargetStat TargetStat => targetStat;

        public override (TransmissionVector? changedVector, float delta) Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            bool applied = false;
            float totalDelta = 0f;
            TransmissionVector? vector = TargetStat == GameTypes.TargetStat.Health ? TransmissionVector.Health : TransmissionVector.Morale;

            foreach (var target in targets.ToList())
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null) continue;

                int currentValue = TargetStat == GameTypes.TargetStat.Health ? targetStats.Health : targetStats.Morale;
                int maxValue = TargetStat == GameTypes.TargetStat.Health ? targetStats.MaxHealth : targetStats.MaxMorale;
                if (ThresholdPercent > 0 && currentValue >= (ThresholdPercent / 100f) * maxValue)
                {
                    Debug.LogWarning($"{targetStats.Id} is too healthy for {abilityId} by {user.Id} (>= {ThresholdPercent}% {TargetStat}).");
                    continue;
                }

                int effectAmount;
                if (UsePercentage)
                {
                    effectAmount = Mathf.RoundToInt(maxValue * (PercentAmount / 100f));
                }
                else
                {
                    effectAmount = FlatAmount;
                }

                int newValue = Mathf.Clamp(currentValue + effectAmount, 0, maxValue);
                int change = newValue - currentValue;

                if (change != 0)
                {
                    if (TargetStat == GameTypes.TargetStat.Health)
                        targetStats.Health = newValue;
                    else
                        targetStats.Morale = newValue;

                    totalDelta += change; // Accumulate change for delta
                    string action = change > 0 ? (TargetStat == GameTypes.TargetStat.Health ? "heals" : "boosts") : "damages";
                    string statName = TargetStat.ToString().ToLower();
                    string colorCode = change > 0 ? "#00FF00" : "#FF0000";
                    string changeMessage = $"{user.Id} {action} {targetStats.Id} for {Mathf.Abs(change)} {statName} with {abilityId} <color={colorCode}>[{effectAmount:+#;-#} {statName}{(UsePercentage ? $" ({PercentAmount}% of {maxValue})" : "")}, capped at {maxValue}]</color>";
                    combatLogs.Add(changeMessage);
                    eventBus.RaiseLogMessage(changeMessage, change > 0 ? Color.green : Color.red);
                    eventBus.RaiseUnitDamaged(target, changeMessage);
                    updateUnitCallback(target);
                    applied = true;
                }
            }
            return applied ? (vector, totalDelta) : (null, 0f);
        }
    }
}