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
        public int Infectivity { get; set; }
        public bool HasRetreated { get; set; } = false;
        public bool IsInfected { get; set; } = false;
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
                Infectivity = 20;
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
            Infectivity = data.Infectivity;
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
            public int infectivity;
            public bool isHero;
            public bool isInfected;
            public int rank;
            public Sprite combatSprite;

            public DisplayStats(string name, int health, int maxHealth, int attack, int defense, int speed, int evasion, int morale, int maxMorale, int infectivity, bool isHero, bool isInfected, int rank, Sprite combatSprite)
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
                this.infectivity = infectivity;
                this.isHero = isHero;
                this.isInfected = isInfected;
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
                speed: Mathf.Clamp(Speed, 1, 8),
                evasion: Mathf.Clamp(Evasion, 0, 100),
                morale: Morale,
                maxMorale: MaxMorale,
                infectivity: Infectivity,
                isHero: IsHero,
                isInfected: IsInfected,
                rank: Rank,
                combatSprite: CombatSprite
            );
        }

        public IEnumerator PerformAbility(UnitAttackState state, PartyData partyData, List<ICombatUnit> allTargets, EventBusSO eventBus, UIConfig uiConfig, CombatConfig combatConfig, List<string> combatLogs, List<CharacterStats> heroPositions, List<CharacterStats> monsterPositions, Action<ICombatUnit> updateUnitCallback, CombatSceneComponent combatScene)
        {
            if (state == null || Health <= 0 || HasRetreated)
            {
                Debug.LogWarning($"PerformAbility: Invalid state or unit {Id} is dead/retreated");
                yield break;
            }

            foreach (var ability in abilities)
            {
                if (ability == null)
                {
                    Debug.LogWarning($"PerformAbility: Null AbilitySO for {Id}");
                    continue;
                }

                var abilityId = ability.Id;
                if (state.AbilityCooldowns.ContainsKey(abilityId) && state.AbilityCooldowns[abilityId] > 0)
                {
                    continue; // Skip if ability is on cooldown
                }

                var filteredPool = ability.GetTargets(this, partyData, allTargets);
                if (filteredPool.Count == 0)
                {
                    string noTargetMessage = $"No qualifying targets for {abilityId} by {Id}.";
                    combatLogs.Add(noTargetMessage);
                    eventBus.RaiseLogMessage(noTargetMessage, Color.red);
                    continue;
                }

                var selectedTargets = CombatUtils.SelectTargets(this, filteredPool, partyData, ability.Rule, heroPositions, monsterPositions);
                if (selectedTargets.Any())
                {
                    string abilityMessage = $"{Id} uses {abilityId}!";
                    combatLogs.Add(abilityMessage);
                    eventBus.RaiseLogMessage(abilityMessage, uiConfig.TextColor);
                    eventBus.RaiseUnitAttacking(this, null, abilityId);
                    eventBus.RaiseAbilitySelected(new EventBusSO.AttackData { attacker = this, target = null, abilityId = abilityId });
                    yield return new WaitUntil(() => !combatScene.IsPaused);
                    yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));

                    CombatUtils.ApplyEffect(this, selectedTargets, ability, abilityId, eventBus, uiConfig, combatLogs, updateUnitCallback);

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

                    yield return new WaitForSeconds(0.2f / (combatConfig?.CombatSpeed ?? 1f));
                    yield break; // Exit after successful ability use
                }
            }

            // Fallback if no ability was used
            string noAbilityMessage = $"No usable abilities for {Id}.";
            combatLogs.Add(noAbilityMessage);
            eventBus.RaiseLogMessage(noAbilityMessage, Color.red);
            yield return new WaitForSeconds(0.2f / (combatConfig?.CombatSpeed ?? 1f));
        }
    }

    public enum CharacterType
    {
        Hero,
        Monster
    }
}