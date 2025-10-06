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
        [SerializeField] private bool isDodgeable = true;
        [SerializeField] private CombatTypes.DefenseCheck defenseType = CombatTypes.DefenseCheck.Standard;

        public float Multiplier => multiplier;
        public bool IsDodgeable => isDodgeable;
        public CombatTypes.DefenseCheck DefenseType => defenseType;

        public override bool Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            bool applied = false;
            foreach (var target in targets.ToList())
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null) continue;

                if (IsDodgeable && CombatUtils.CheckEvasion(targetStats))
                {
                    string dodgeMessage = $"{targetStats.Id} dodges {user.Id}'s {abilityId}!";
                    combatLogs.Add(dodgeMessage);
                    eventBus.RaiseLogMessage(dodgeMessage, Color.yellow);
                    applied = true;
                    continue;
                }

                int damage = DefenseType == CombatTypes.DefenseCheck.Standard
                    ? (user.Attack * (100 - targetStats.Defense * 5)) / 100
                    : user.Attack;
                damage = Mathf.Max(0, Mathf.RoundToInt(damage * Multiplier));

                if (damage > 0)
                {
                    targetStats.Health -= damage;
                    string damageMessage = $"{user.Id} hits {targetStats.Id} for {damage} {(DefenseType == CombatTypes.DefenseCheck.None ? "true " : "")}damage with {abilityId} <color=#FFFF00>[{user.Attack} ATK{(DefenseType == CombatTypes.DefenseCheck.Standard ? $" * (100 - {targetStats.Defense} DEF * 5) / 100" : "")} * {Multiplier}]</color>";
                    combatLogs.Add(damageMessage);
                    eventBus.RaiseLogMessage(damageMessage, uiConfig.TextColor);
                    eventBus.RaiseUnitDamaged(target, damageMessage);
                    updateUnitCallback(target);
                    applied = true;
                }
            }
            return applied;
        }
    }
}