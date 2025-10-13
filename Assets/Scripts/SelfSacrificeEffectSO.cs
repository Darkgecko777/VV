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
        [SerializeField] private bool drain = false; // New field: true to add health, false to subtract

        public float AmountPercent => amountPercent;
        public float ThresholdPercent => thresholdPercent;
        public bool AllowSuicide => allowSuicide;
        public bool Drain => drain;

        public override (TransmissionVector? changedVector, float delta) Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            // Check health threshold for sacrifice (if draining) or gain (if not draining)
            if (ThresholdPercent > 0 && Drain == false && user.Health <= (ThresholdPercent / 100f) * user.MaxHealth)
            {
                string lowHealthMessage = $"{user.Id} needs more than {ThresholdPercent}% health to use {abilityId}!";
                combatLogs.Add(lowHealthMessage);
                eventBus.RaiseLogMessage(lowHealthMessage, Color.red);
                return (null, 0f);
            }

            int healthChange = Mathf.Max(1, Mathf.RoundToInt(user.MaxHealth * AmountPercent / 100f));
            int newHealth;

            if (Drain)
            {
                // Add health, capped at MaxHealth
                newHealth = Mathf.Min(user.Health + healthChange, user.MaxHealth);
                string gainMessage = $"{user.Id} gains {healthChange} health with {abilityId}!";
                combatLogs.Add(gainMessage);
                eventBus.RaiseLogMessage(gainMessage, Color.green);
                user.Health = newHealth;
                updateUnitCallback(user);
                return (TransmissionVector.Health, healthChange); // Positive delta for health gain
            }
            else
            {
                // Subtract health, respecting AllowSuicide
                newHealth = AllowSuicide ? Mathf.Max(0, user.Health - healthChange) : Mathf.Max(1, user.Health - healthChange);
                string costMessage = $"{user.Id} sacrifices {healthChange} health to use {abilityId}!";
                combatLogs.Add(costMessage);
                eventBus.RaiseLogMessage(costMessage, Color.magenta);
                user.Health = newHealth;
                updateUnitCallback(user);
                return (TransmissionVector.Health, -healthChange); // Negative delta for health loss
            }
        }
    }
}