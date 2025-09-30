using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public static class EffectReference
    {
        public struct EffectData
        {
            public CombatTypes.Stat TargetStat; // e.g., Health, IsInfected
            public bool IsBuff; // True for persistent effects (e.g., Reflect)
            public bool ApplyToUser; // True for effects like Reflect (affects attacker)
            public float ValueMultiplier; // Scales effectValue from AbilitySO
            public string LogTemplate; // e.g., "{user.Id} takes {amount} reflected damage!"
        }

        private static readonly Dictionary<string, EffectData> effects = new Dictionary<string, EffectData>
        {
            {
                "Damage", new EffectData
                {
                    TargetStat = CombatTypes.Stat.Health,
                    IsBuff = false,
                    ApplyToUser = false,
                    ValueMultiplier = 1.0f,
                    LogTemplate = "{user.Id} deals {amount} damage to {target.Id}!"
                }
            },
            {
                "Heal", new EffectData
                {
                    TargetStat = CombatTypes.Stat.Health,
                    IsBuff = false,
                    ApplyToUser = false,
                    ValueMultiplier = 1.0f,
                    LogTemplate = "{user.Id} heals {target.Id} for {amount} HP!"
                }
            },
            {
                "Reflect", new EffectData
                {
                    TargetStat = CombatTypes.Stat.Health,
                    IsBuff = true,
                    ApplyToUser = true,
                    ValueMultiplier = 1.0f,
                    LogTemplate = "{user.Id} takes {amount} reflected damage from {target.Id}!"
                }
            },
            {
                "VirusSpread", new EffectData
                {
                    TargetStat = CombatTypes.Stat.IsInfected,
                    IsBuff = false,
                    ApplyToUser = false,
                    ValueMultiplier = 1.0f,
                    LogTemplate = "{user.Id} spreads virus to {target.Id}!"
                }
            }
        };

        public static void Apply(string effectId, CharacterStats user, CharacterStats target, float value, int duration, UnitAttackState targetState, EventBusSO eventBus, UIConfig uiConfig)
        {
            if (!effects.TryGetValue(effectId, out var effect))
            {
                Debug.LogWarning($"EffectReference: Unknown effectId {effectId}");
                return;
            }
            if (target == null || targetState == null || user == null)
            {
                Debug.LogWarning($"EffectReference: Null user, target, or targetState for {effectId}");
                return;
            }

            float finalValue = value * effect.ValueMultiplier;
            string message;
            if (effect.IsBuff)
            {
                targetState.TempStats[effectId] = ((int)(finalValue * 100), duration);
                message = $"{target.Id} gains {effectId} ({finalValue * 100}%) for {duration} rounds!";
                eventBus.RaiseLogMessage(message, uiConfig.TextColor);
            }
            else
            {
                if (effect.TargetStat == CombatTypes.Stat.Health)
                {
                    int amount = Mathf.RoundToInt(finalValue * target.MaxHealth);
                    if (effect.ApplyToUser)
                    {
                        user.Health = Mathf.Max(0, user.Health - amount);
                        message = effect.LogTemplate.Replace("{user.Id}", user.Id).Replace("{target.Id}", target.Id).Replace("{amount}", amount.ToString());
                        eventBus.RaiseUnitDamaged(user, message);
                    }
                    else
                    {
                        target.Health = effectId == "Heal" ? Mathf.Min(target.MaxHealth, target.Health + amount) : Mathf.Max(0, target.Health - amount);
                        message = effect.LogTemplate.Replace("{user.Id}", user.Id).Replace("{target.Id}", target.Id).Replace("{amount}", amount.ToString());
                        eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                }
                else if (effect.TargetStat == CombatTypes.Stat.IsInfected)
                {
                    target.IsInfected = true;
                    message = effect.LogTemplate.Replace("{user.Id}", user.Id).Replace("{target.Id}", target.Id).Replace("{amount}", "virus");
                    eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                }
                else
                {
                    Debug.LogWarning($"EffectReference: Unsupported TargetStat {effect.TargetStat} for {effectId}");
                    return;
                }
                eventBus.RaiseLogMessage(message, effect.TargetStat == CombatTypes.Stat.Health ? Color.red : Color.green);
            }
        }
    }
}