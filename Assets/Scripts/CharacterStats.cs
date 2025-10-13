using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class CharacterStats : ICombatUnit
    {
        public string Id { get; set; }
        public CharacterType Type { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public int Evasion { get; set; }
        public int Morale { get; set; }
        public int MaxMorale { get; set; }
        public int Immunity { get; set; }
        public List<VirusSO> Infections { get; set; }
        public bool HasRetreated { get; set; } = false;
        public bool IsInfected => Infections != null && Infections.Any();
        public int PartyPosition { get; set; }
        public string[] abilityIds { get; set; }
        public AbilitySO[] abilities { get; set; }
        public bool IsHero => Type == CharacterType.Hero;
        public int Rank { get; set; }
        public Sprite CombatSprite { get; set; }

        public CharacterStats(CharacterSO data, Vector3 position)
        {
            if (data == null)
            {
                Debug.LogError("CharacterStats: Null CharacterSO provided, using default values.");
                Id = "Default";
                Type = CharacterType.Hero;
                Health = 50;
                MaxHealth = 50;
                Attack = 10;
                Defense = 5;
                Speed = 3;
                Evasion = 10;
                Morale = 100;
                MaxMorale = 100;
                Immunity = 20;
                Infections = new List<VirusSO>();
                PartyPosition = 1;
                abilityIds = new string[] { "MeleeStrike" };
                abilities = new AbilitySO[0];
                Rank = 1;
                CombatSprite = null;
                return;
            }
            Id = data.Id;
            Type = data.Type;
            Health = Type == CharacterType.Hero ? data.Health : data.MaxHealth;
            MaxHealth = data.MaxHealth;
            Attack = data.Attack;
            Defense = data.Defense;
            Speed = data.Speed;
            Evasion = data.Evasion;
            Morale = Type == CharacterType.Hero ? data.Morale : 0;
            MaxMorale = Type == CharacterType.Hero ? data.MaxMorale : 0;
            Immunity = data.Immunity;
            Infections = new List<VirusSO>();
            PartyPosition = data.PartyPosition;
            abilityIds = data.Abilities != null && data.Abilities.Length > 0 ? data.Abilities.Select(a => a.Id).ToArray() : new string[] { "MeleeStrike" };
            abilities = data.Abilities != null ? data.Abilities : new AbilitySO[0];
            if (data.Abilities == null || data.Abilities.Length == 0)
            {
                Debug.LogWarning($"CharacterStats: No Abilities defined in CharacterSO for {data.Id}. Defaulting to MeleeStrike.");
            }
            Rank = data.Rank;
            CombatSprite = data.CombatSprite;
        }

        public struct DisplayStats
        {
            public string name;
            public int health;
            public int maxHealth;
            public int attack;
            public int defense;
            public int speed;
            public int evasion;
            public int morale;
            public int maxMorale;
            public int immunity;
            public bool isHero;
            public bool isInfected;
            public List<VirusSO> infections;
            public int rank;
            public Sprite combatSprite;

            public DisplayStats(string name, int health, int maxHealth, int attack, int defense, int speed, int evasion, int morale, int maxMorale, int immunity, bool isHero, bool isInfected, List<VirusSO> infections, int rank, Sprite combatSprite)
            {
                this.name = name;
                this.health = health;
                this.maxHealth = maxHealth;
                this.attack = attack;
                this.defense = defense;
                this.speed = speed;
                this.evasion = evasion;
                this.morale = morale;
                this.maxMorale = maxMorale;
                this.immunity = immunity;
                this.isHero = isHero;
                this.isInfected = isInfected;
                this.infections = infections;
                this.rank = rank;
                this.combatSprite = combatSprite;
            }
        }

        public DisplayStats GetDisplayStats()
        {
            return new DisplayStats(
                name: Id,
                health: Health,
                maxHealth: MaxHealth,
                attack: Attack,
                defense: Defense,
                speed: Speed,
                evasion: Evasion,
                morale: Morale,
                maxMorale: MaxMorale,
                immunity: Immunity,
                isHero: IsHero,
                isInfected: IsInfected,
                infections: Infections,
                rank: Rank,
                combatSprite: CombatSprite
            );
        }

        public void AddInfection(VirusSO virus)
        {
            if (virus != null && !Infections.Contains(virus))
            {
                Infections.Add(virus);
            }
        }

        public IEnumerator PerformAbility(UnitAttackState attackState, PartyData partyData, List<ICombatUnit> allTargets, EventBusSO eventBus, UIConfig uiConfig, CombatConfig combatConfig, List<string> combatLogs, List<CharacterStats> heroPositions, List<CharacterStats> monsterPositions, Action<ICombatUnit> updateUnitCallback, CombatSceneComponent combatScene)
        {
            bool abilityUsed = false;
            string noTargetMessage = null;

            for (int i = 0; i < abilities.Length; i++)
            {
                var ability = abilities[i];
                if (ability == null) continue;
                string abilityId = ability.Id;

                // Check cooldown silently
                if (ability.CooldownParams.Type != GameTypes.CooldownType.None)
                {
                    if ((ability.CooldownParams.Type == GameTypes.CooldownType.Actions && attackState.AbilityCooldowns.ContainsKey(abilityId) && attackState.AbilityCooldowns[abilityId] > 0) ||
                        (ability.CooldownParams.Type == GameTypes.CooldownType.Rounds && attackState.RoundCooldowns.ContainsKey(abilityId) && attackState.RoundCooldowns[abilityId] > 0))
                    {
                        continue; // Skip to next ability without logging
                    }
                }

                // Get potential targets
                var potentialTargets = ability.GetTargets(this, partyData, allTargets);

                // Silently check for valid targets based on rule
                var selectedTargets = CombatUtils.SelectTargets(this, potentialTargets, partyData, ability.Rule, heroPositions, monsterPositions, ability, combatLogs, eventBus, uiConfig);
                if (selectedTargets.Count == 0)
                {
                    noTargetMessage = $"No qualifying targets for {Id}'s {abilityId}.";
                    continue; // Skip to next ability
                }

                // Silently check effect-specific conditions (user-based)
                bool userConditionsPassed = true;
                foreach (var effect in ability.Effects)
                {
                    if (effect is SelfSacrificeEffectSO selfSac && selfSac.ThresholdPercent > 0)
                    {
                        if (Health <= (selfSac.ThresholdPercent / 100f) * MaxHealth)
                        {
                            userConditionsPassed = false;
                            break;
                        }
                    }
                }
                if (!userConditionsPassed)
                {
                    noTargetMessage = $"User conditions not met for {Id}'s {abilityId}.";
                    continue; // Skip to next ability
                }

                // Silently check effect-specific conditions (target-based)
                bool targetConditionsPassed = false;
                foreach (var effect in ability.Effects)
                {
                    if (effect is HealEffectSO healEffect && healEffect.ThresholdPercent > 0)
                    {
                        foreach (var target in selectedTargets.OfType<CharacterStats>())
                        {
                            int currentValue = healEffect.TargetStat == GameTypes.TargetStat.Health ? target.Health : target.Morale;
                            int maxValue = healEffect.TargetStat == GameTypes.TargetStat.Health ? target.MaxHealth : target.MaxMorale;
                            if (currentValue < (healEffect.ThresholdPercent / 100f) * maxValue)
                            {
                                targetConditionsPassed = true;
                                break;
                            }
                        }
                    }
                    else if (effect is InstantKillEffectSO instantKill && instantKill.ThresholdPercent > 0)
                    {
                        foreach (var target in selectedTargets.OfType<CharacterStats>())
                        {
                            if (target.Health < (instantKill.ThresholdPercent / 100f) * target.MaxHealth)
                            {
                                targetConditionsPassed = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Non-threshold effects (e.g., Strike, Interrupt) are always valid if targets exist
                        targetConditionsPassed = true;
                    }
                    if (targetConditionsPassed) break;
                }
                if (!targetConditionsPassed)
                {
                    noTargetMessage = $"Target conditions not met for {Id}'s {abilityId}.";
                    continue; // Skip to next ability
                }

                // Valid ability found: Trigger animation and wait for completion
                eventBus.RaiseUnitAttacking(this, selectedTargets.FirstOrDefault(), abilityId);
                // Find the SpriteAnimation component for this unit
                var unitEntry = combatScene.GetComponentsInChildren<SpriteAnimation>()
                    .FirstOrDefault(sa => combatScene.units.Any(u => u.unit == this && u.go == sa.gameObject));
                if (unitEntry != null)
                {
                    while (unitEntry.IsAnimating)
                    {
                        yield return null;
                    }
                }

                // Log the ability attempt
                string attackMessage = $"{Id} uses {abilityId} on {string.Join(", ", selectedTargets.Select(t => t.Id))}!";
                combatLogs.Add(attackMessage);
                eventBus.RaiseLogMessage(attackMessage, uiConfig.TextColor);

                // Execute the ability
                bool applied = CombatUtils.ExecuteAbility(this, selectedTargets, ability, abilityId, eventBus, uiConfig, combatLogs, updateUnitCallback, attackState, combatScene);

                if (applied)
                {
                    // Decrement action-based cooldowns for this unit's other abilities
                    foreach (var cd in attackState.AbilityCooldowns.ToList())
                    {
                        if (cd.Key != abilityId) // Skip the ability just used
                        {
                            attackState.AbilityCooldowns[cd.Key] = Mathf.Max(0, cd.Value - 1);
                            if (attackState.AbilityCooldowns[cd.Key] == 0)
                            {
                                attackState.AbilityCooldowns.Remove(cd.Key);
                                string cooldownEndMessage = $"{Id}'s {cd.Key} is off cooldown!";
                                combatLogs.Add(cooldownEndMessage);
                                eventBus.RaiseLogMessage(cooldownEndMessage, uiConfig.TextColor);
                            }
                        }
                    }
                }

                if (applied && ability.CooldownParams.Type != GameTypes.CooldownType.None)
                {
                    if (ability.CooldownParams.Type == GameTypes.CooldownType.Actions)
                    {
                        attackState.AbilityCooldowns[abilityId] = ability.CooldownParams.Duration;
                    }
                    else if (ability.CooldownParams.Type == GameTypes.CooldownType.Rounds)
                    {
                        attackState.RoundCooldowns[abilityId] = ability.CooldownParams.Duration;
                    }
                    string cooldownAppliedMessage = $"{Id}'s {abilityId} is now on cooldown for {ability.CooldownParams.Duration} {ability.CooldownParams.Type.ToString().ToLower()}.";
                    combatLogs.Add(cooldownAppliedMessage);
                    eventBus.RaiseLogMessage(cooldownAppliedMessage, Color.yellow);
                }

                abilityUsed = true;

                // Check targets for death or retreat
                foreach (var target in selectedTargets.ToList())
                {
                    if (target.Health <= 0)
                    {
                        if (!combatLogs.Contains($"{target.Id} dies!"))
                        {
                            eventBus.RaiseUnitDied(target);
                            string deathMessage = $"{target.Id} dies!";
                            combatLogs.Add(deathMessage);
                            eventBus.RaiseLogMessage(deathMessage, Color.red);
                            updateUnitCallback(target);
                            if (target is CharacterStats statsTarget)
                            {
                                if (statsTarget.Type == CharacterType.Hero)
                                    heroPositions.Remove(statsTarget);
                                else
                                    monsterPositions.Remove(statsTarget);
                            }
                        }
                    }
                    else if (partyData.CheckRetreat(target, eventBus, uiConfig, combatConfig))
                    {
                        partyData.ProcessRetreat(target, eventBus, uiConfig, combatLogs, combatConfig);
                        updateUnitCallback(target);
                    }
                }

                // Check user for death or retreat
                if (Health <= 0)
                {
                    if (!combatLogs.Contains($"{Id} dies!"))
                    {
                        eventBus.RaiseUnitDied(this);
                        string deathMessage = $"{Id} dies!";
                        combatLogs.Add(deathMessage);
                        eventBus.RaiseLogMessage(deathMessage, Color.red);
                        updateUnitCallback(this);
                        if (Type == CharacterType.Hero)
                            heroPositions.Remove(this);
                        else
                            monsterPositions.Remove(this);
                    }
                }
                else if (partyData.CheckRetreat(this, eventBus, uiConfig, combatConfig))
                {
                    partyData.ProcessRetreat(this, eventBus, uiConfig, combatLogs, combatConfig);
                    updateUnitCallback(this);
                }

                yield return new WaitForSeconds(0.3f / (combatConfig?.CombatSpeed ?? 1f));
                yield break; // Exit after first valid ability
            }

            // If no ability was used, log the issue for design review
            if (!abilityUsed)
            {
                noTargetMessage = noTargetMessage ?? $"No qualifying targets or conditions met for any abilities of {Id}.";
                combatLogs.Add(noTargetMessage);
                eventBus.RaiseLogMessage(noTargetMessage, Color.red);
                Debug.LogWarning($"CharacterStats: {noTargetMessage} Review ability design for {Id}.");
            }

            yield return new WaitForSeconds(0.3f / (combatConfig?.CombatSpeed ?? 1f));
        }
    }

    public enum CharacterType
    {
        Hero,
        Monster
    }
}