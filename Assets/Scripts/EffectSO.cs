using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "EffectSO", menuName = "VirulentVentures/EffectSO")]
    public abstract class EffectSO : ScriptableObject
    {
        [SerializeField] private string effectId; // Unique ID, e.g., "Strike1", "Heal1"
        public string EffectId => effectId;

        public abstract bool Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene);
    }

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

    [CreateAssetMenu(fileName = "SelfSacrificeEffect", menuName = "VirulentVentures/Effects/SelfSacrifice")]
    public class SelfSacrificeEffectSO : EffectSO
    {
        [SerializeField] private float amountPercent = 5f;
        [SerializeField] private float thresholdPercent = 25f;
        [SerializeField] private bool allowSuicide;

        public float AmountPercent => amountPercent;
        public float ThresholdPercent => thresholdPercent;
        public bool AllowSuicide => allowSuicide;

        public override bool Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            if (ThresholdPercent > 0 && user.Health <= (ThresholdPercent / 100f) * user.MaxHealth)
            {
                string lowHealthMessage = $"{user.Id} needs more than {ThresholdPercent}% health to use {abilityId}!";
                combatLogs.Add(lowHealthMessage);
                eventBus.RaiseLogMessage(lowHealthMessage, Color.red);
                return false;
            }

            int healthCost = Mathf.Max(1, Mathf.RoundToInt(user.MaxHealth * AmountPercent / 100f));
            user.Health = AllowSuicide ? Mathf.Max(0, user.Health - healthCost) : Mathf.Max(1, user.Health - healthCost);
            string costMessage = $"{user.Id} sacrifices {healthCost} health to use {abilityId}!";
            combatLogs.Add(costMessage);
            eventBus.RaiseLogMessage(costMessage, Color.magenta);
            updateUnitCallback(user);
            return true;
        }
    }

    [CreateAssetMenu(fileName = "InstantKillEffect", menuName = "VirulentVentures/Effects/InstantKill")]
    public class InstantKillEffectSO : EffectSO
    {
        [SerializeField] private float thresholdPercent;

        public float ThresholdPercent => thresholdPercent;

        public override bool Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            bool applied = false;
            foreach (var target in targets.ToList())
            {
                var targetStats = target as CharacterStats;
                if (targetStats == null) continue;

                if (ThresholdPercent > 0 && targetStats.Health >= (ThresholdPercent / 100f) * targetStats.MaxHealth)
                {
                    Debug.LogWarning($"{targetStats.Id} is too healthy for {abilityId} by {user.Id} (>= {ThresholdPercent}% HP).");
                    continue;
                }

                targetStats.Health = 0;
                string killMessage = $"{user.Id} executes {targetStats.Id} with {abilityId} <color=#FF0000>[Instant Kill]</color>";
                combatLogs.Add(killMessage);
                eventBus.RaiseLogMessage(killMessage, Color.red);
                eventBus.RaiseUnitDamaged(target, killMessage);
                updateUnitCallback(target);
                applied = true;
            }
            return applied;
        }
    }
}