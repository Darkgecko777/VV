using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "SelfSacrificeEffect", menuName = "VirulentVentures/Effects/SelfSacrifice")]
    public class SelfSacrificeEffectSO : EffectSO
    {
        [SerializeField] private float amountPercent = 5f;
        [SerializeField] private float thresholdPercent = 25f;
        [SerializeField] private bool allowSuicide;

        public float AmountPercent => amountPercent;
        public float ThresholdPercent => thresholdPercent;
        public bool AllowSuicide => allowSuicide;

        public override (TransmissionVector? changedVector, float delta) Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            if (ThresholdPercent > 0 && user.Health <= (ThresholdPercent / 100f) * user.MaxHealth)
            {
                string lowHealthMessage = $"{user.Id} needs more than {ThresholdPercent}% health to use {abilityId}!";
                combatLogs.Add(lowHealthMessage);
                eventBus.RaiseLogMessage(lowHealthMessage, Color.red);
                return (null, 0f);
            }

            int healthCost = Mathf.Max(1, Mathf.RoundToInt(user.MaxHealth * AmountPercent / 100f));
            user.Health = AllowSuicide ? Mathf.Max(0, user.Health - healthCost) : Mathf.Max(1, user.Health - healthCost);
            string costMessage = $"{user.Id} sacrifices {healthCost} health to use {abilityId}!";
            combatLogs.Add(costMessage);
            eventBus.RaiseLogMessage(costMessage, Color.magenta);
            updateUnitCallback(user);
            return (TransmissionVector.Health, -healthCost); // Negative delta for health loss
        }
    }
}