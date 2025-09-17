using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class CombatEffectsComponent : MonoBehaviour
    {
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private UIConfig uiConfig;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                Debug.LogError("CombatEffectsComponent: Missing required references. Disabling component.");
                enabled = false;
            }
        }

        public void ProcessEffect(CharacterStats user, CharacterStats target, string tag, string abilityId)
        {
            if (user == null || target == null)
            {
                Debug.LogWarning($"CombatEffectsComponent: Null user or target for effect {tag} in ability {abilityId}.");
                return;
            }

            string[] tagParts = tag.Split(':');
            string effectType = tagParts[0];
            int value = tagParts.Length > 1 && int.TryParse(tagParts[1], out int parsedValue) ? parsedValue : 0;
            float floatValue = tagParts.Length > 1 && float.TryParse(tagParts[1], out float parsedFloat) ? parsedFloat : 0f;
            var targetState = CombatSceneComponent.GetUnitAttackState(target); // Declare targetState at method scope

            string effectMessage = string.Empty;
            Color messageColor = uiConfig.TextColor;

            switch (effectType)
            {
                case "TrueStrike":
                    if (value > 0)
                    {
                        target.Health = Mathf.Max(0, target.Health - value);
                        effectMessage = $"{user.Id} deals {value} direct damage to {target.Id} with {abilityId}! <color=#FFFF00>[TrueStrike]</color>";
                        messageColor = Color.red;
                        eventBus.RaiseUnitDamaged(target, effectMessage);
                    }
                    break;

                case "VirusSpread":
                    if (!target.IsInfected)
                    {
                        float infectionChance = target.Infectivity / 100f;
                        float resistanceChance = user.Infectivity / 100f;
                        if (UnityEngine.Random.value <= infectionChance && UnityEngine.Random.value > resistanceChance)
                        {
                            target.IsInfected = true;
                            effectMessage = $"{target.Id} is infected by {abilityId}! <color=#FF0000>[VirusSpread]</color>";
                            messageColor = Color.red;
                            eventBus.RaiseUnitInfected(target, "Virus");
                        }
                    }
                    break;

                case "Heal":
                    if (floatValue > 0)
                    {
                        int healAmount = Mathf.RoundToInt(user.Attack * floatValue);
                        target.Health = Mathf.Min(target.MaxHealth, target.Health + healAmount);
                        effectMessage = $"{user.Id} heals {target.Id} for {healAmount} HP with {abilityId}! <color=#00FF00>[Heal]</color>";
                        messageColor = Color.green;
                        eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                    break;

                case "Morale":
                    if (value != 0)
                    {
                        target.Morale = Mathf.Clamp(target.Morale + value, 0, target.MaxMorale);
                        effectMessage = $"{user.Id} changes {target.Id}'s morale by {value} with {abilityId}! <color=#FFFF00>[Morale]</color>";
                        messageColor = Color.yellow;
                        eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                    break;

                case "Buff":
                    if (value != 0 && tagParts.Length > 2)
                    {
                        string stat = tagParts[2];
                        int duration = tagParts.Length > 3 && int.TryParse(tagParts[3], out int dur) ? dur : 2;
                        if (targetState != null)
                        {
                            targetState.TempStats[stat] = (value, duration);
                            effectMessage = $"{user.Id} buffs {target.Id}'s {stat} +{value} for {duration} actions with {abilityId}! <color=#00FF00>[Buff]</color>";
                            messageColor = Color.green;
                            eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                        }
                        else
                        {
                            Debug.LogWarning($"CombatEffectsComponent: No UnitAttackState for {target.Id} for Buff {tag}.");
                        }
                    }
                    break;

                case "Debuff":
                    if (value != 0 && tagParts.Length > 2)
                    {
                        string stat = tagParts[2];
                        int duration = tagParts.Length > 3 && int.TryParse(tagParts[3], out int dur) ? dur : 2;
                        if (targetState != null)
                        {
                            targetState.TempStats[stat] = (value, duration);
                            effectMessage = $"{user.Id} debuffs {target.Id}'s {stat} {value} for {duration} actions with {abilityId}! <color=#FF0000>[Debuff]</color>";
                            messageColor = Color.red;
                            eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                        }
                        else
                        {
                            Debug.LogWarning($"CombatEffectsComponent: No UnitAttackState for {target.Id} for Debuff {tag}.");
                        }
                    }
                    break;

                case "SelfDamage":
                    if (value > 0)
                    {
                        user.Health = Mathf.Max(0, user.Health - value);
                        effectMessage = $"{user.Id} takes {value} self-damage from {abilityId}! <color=#FF0000>[SelfDamage]</color>";
                        messageColor = Color.red;
                        eventBus.RaiseUnitDamaged(user, effectMessage);
                    }
                    break;

                case "SkipNextAttack":
                    var userState = CombatSceneComponent.GetUnitAttackState(user);
                    if (userState != null)
                    {
                        userState.SkipNextAttack = true;
                        effectMessage = $"{user.Id} will skip their next attack due to {abilityId}! <color=#FFFF00>[SkipNextAttack]</color>";
                        messageColor = Color.yellow;
                        eventBus.RaiseUnitUpdated(user, user.GetDisplayStats());
                    }
                    else
                    {
                        Debug.LogWarning($"CombatEffectsComponent: No UnitAttackState for {user.Id} for SkipNextAttack {tag}.");
                    }
                    break;

                case "Thorns":
                    if (value > 0 && targetState != null)
                    {
                        int duration = tagParts.Length > 2 && int.TryParse(tagParts[2], out int dur) ? dur : 2;
                        targetState.TempStats["ThornsReflect"] = (value, duration);
                        effectMessage = $"{user.Id} applies Thorns {value} to {target.Id} for {duration} rounds with {abilityId}! <color=#FFFF00>[Thorns]</color>";
                        messageColor = Color.yellow;
                        eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                    else
                    {
                        Debug.LogWarning($"CombatEffectsComponent: No UnitAttackState for {target.Id} for Thorns {tag}.");
                    }
                    break;

                case "ThornsInfection":
                    if (targetState != null)
                    {
                        int duration = tagParts.Length > 1 && int.TryParse(tagParts[1], out int dur) ? dur : 2;
                        targetState.TempStats["ThornsInfection"] = (0, duration);
                        effectMessage = $"{user.Id} applies ThornsInfection to {target.Id} for {duration} rounds with {abilityId}! <color=#FF0000>[ThornsInfection]</color>";
                        messageColor = Color.red;
                        eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                    else
                    {
                        Debug.LogWarning($"CombatEffectsComponent: No UnitAttackState for {target.Id} for ThornsInfection {tag}.");
                    }
                    break;

                default:
                    Debug.LogWarning($"CombatEffectsComponent: Unrecognized effect tag {tag} for {abilityId}.");
                    effectMessage = $"Unknown effect {tag} applied by {user.Id} on {target.Id} with {abilityId}!";
                    break;
            }

            if (!string.IsNullOrEmpty(effectMessage))
            {
                eventBus.RaiseLogMessage(effectMessage, messageColor);
            }
        }

        private bool ValidateReferences()
        {
            if (eventBus == null || combatConfig == null || uiConfig == null)
            {
                Debug.LogError($"CombatEffectsComponent: Missing references! EventBus: {eventBus != null}, CombatConfig: {combatConfig != null}, UIConfig: {uiConfig != null}");
                return false;
            }
            return true;
        }
    }
}