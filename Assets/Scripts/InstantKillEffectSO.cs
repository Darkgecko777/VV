using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "InstantKillEffect", menuName = "VirulentVentures/Effects/InstantKill")]
    public class InstantKillEffectSO : EffectSO
    {
        [SerializeField] private float thresholdPercent;

        public float ThresholdPercent => thresholdPercent;

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

                if (ThresholdPercent > 0 && targetStats.Health >= (ThresholdPercent / 100f) * targetStats.MaxHealth)
                {
                    Debug.LogWarning($"{targetStats.Id} is too healthy for {abilityId} by {user.Id} (>= {ThresholdPercent}% HP).");
                    continue;
                }

                int healthLost = targetStats.Health; // Record health lost as delta
                targetStats.Health = 0;
                totalDelta += healthLost;
                string killMessage = $"{user.Id} executes {targetStats.Id} with {abilityId} <color=#FF0000>[Instant Kill]</color>";
                combatLogs.Add(killMessage);
                eventBus.RaiseLogMessage(killMessage, Color.red);
                eventBus.RaiseUnitDamaged(target, killMessage);
                updateUnitCallback(target);
                applied = true;
            }
            return applied ? (TransmissionVector.Health, totalDelta) : (null, 0f);
        }
    }
}