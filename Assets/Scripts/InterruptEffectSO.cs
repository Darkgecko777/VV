using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "InterruptEffect", menuName = "VirulentVentures/Effects/Interrupt")]
    public class InterruptEffectSO : EffectSO
    {
        [SerializeField] private int duration = 1;

        public int Duration => duration;

        public override bool Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            bool applied = false;
            foreach (var target in targets.ToList())
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null) continue;

                if (ability.AttackParams.Dodgeable && CombatUtils.CheckEvasion(targetStats))
                {
                    string dodgeMessage = $"{targetStats.Id} dodges {user.Id}'s {abilityId}!";
                    combatLogs.Add(dodgeMessage);
                    eventBus.RaiseLogMessage(dodgeMessage, Color.yellow);
                    applied = true;
                    continue;
                }

                var targetState = combatScene.GetUnitAttackState(target);
                if (targetState != null)
                {
                    targetState.SkipNextAttack = true;
                    string interruptMessage = $"{user.Id}'s {abilityId} interrupts {targetStats.Id}'s next attack!";
                    combatLogs.Add(interruptMessage);
                    eventBus.RaiseLogMessage(interruptMessage, Color.cyan);
                    applied = true;
                }
                else
                {
                    Debug.LogWarning($"CombatUtils: No UnitAttackState found for {targetStats.Id} to apply interrupt.");
                }
            }
            return applied;
        }
    }
}