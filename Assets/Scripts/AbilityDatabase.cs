using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace VirulentVentures
{
    public static class AbilityDatabase
    {
        public enum CooldownType { Actions, Rounds }
        public class Ability
        {
            public string Id { get; }
            public string AnimationTrigger { get; }
            public Func<CharacterStats, PartyData, List<ICombatUnit>, bool> UseCondition { get; }
            public Action<CharacterStats, PartyData, List<ICombatUnit>> Effect { get; }
            public int Cooldown { get; }
            public CooldownType CooldownType { get; }
            public int Priority { get; }
            public int Rank { get; }
            public string LogTemplate { get; }
            private readonly CombatSceneComponent sceneComponent;
            public Ability(
                string id,
                string animationTrigger,
                Func<CharacterStats, PartyData, List<ICombatUnit>, bool> useCondition,
                Action<CharacterStats, PartyData, List<ICombatUnit>> effect,
                int cooldown,
                CooldownType cooldownType,
                int priority,
                int rank,
                string logTemplate,
                CombatSceneComponent sceneComponent)
            {
                Id = id;
                AnimationTrigger = animationTrigger;
                UseCondition = useCondition;
                Effect = effect;
                Cooldown = cooldown;
                CooldownType = cooldownType;
                Priority = priority;
                Rank = rank;
                LogTemplate = logTemplate;
                this.sceneComponent = sceneComponent ?? throw new ArgumentNullException(nameof(sceneComponent));
            }
        }
        private static readonly Dictionary<string, Ability> heroAbilities = new Dictionary<string, Ability>();
        private static readonly Dictionary<string, Ability> monsterAbilities = new Dictionary<string, Ability>();
        private static readonly Dictionary<string, string[]> heroAbilityMap = new Dictionary<string, string[]>
        {
            { "Fighter", new[] { "FighterMeleeAttack", "FighterShieldBash", "FighterCoupDeGrace", "BasicAttack" } },
            { "Monk", new[] { "MonkBasicAttack", "MonkChiStrike", "MonkMeditate", "BasicAttack" } },
            { "Scout", new[] { "ScoutBasicAttack", "ScoutSniperShot", "ScoutEnhanceWeaponry", "BasicAttack" } },
            { "Healer", new[] { "HealerBasicAttack", "HealerHeal", "HealerSteelResolve", "BasicAttack" } }
        };
        private static readonly Dictionary<string, string[]> monsterAbilityMap = new Dictionary<string, string[]>
        {
            { "Mire Shambler", new[] { "ShamblerThornNeedle", "ShamblerSwampBrambles" } },
            { "Bog Fiend", new[] { "FiendMeleeAttack", "FiendSludgeSlam", "FiendDrainHealth" } },
            { "Umbral Corvax", new[] { "CorvaxBasicAttack", "CorvaxMortifyingShriek", "CorvaxWindsOfTerror" } },
            { "Wraith", new[] { "WraithStrike", "WraithHoaryGrasp" } }
        };
        private static bool isInitialized = false;
        public static string[] GetCharacterAbilityIds(string characterId, CharacterType type)
        {
            var map = type == CharacterType.Hero ? heroAbilityMap : monsterAbilityMap;
            if (map.TryGetValue(characterId, out var abilityIds))
            {
                return abilityIds;
            }
            Debug.LogWarning($"AbilityDatabase: No abilities mapped for {characterId} ({type}). Returning BasicAttack.");
            return type == CharacterType.Hero ? new[] { "BasicAttack" } : new string[0];
        }
        public static void InitializeAbilities(CombatSceneComponent sceneComponent)
        {
            if (isInitialized) return;
            isInitialized = true;
            if (sceneComponent == null)
            {
                Debug.LogError("AbilityDatabase: sceneComponent is null in InitializeAbilities.");
                return;
            }
            // Generic BasicAttack (for fallback)
            heroAbilities["BasicAttack"] = new Ability(
                id: "BasicAttack",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = false
                    }, isMelee: false);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = true
                    }, "BasicAttack");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} attacks {target.Id} for {damage} damage!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 0,
                cooldownType: CooldownType.Actions,
                priority: 4,
                rank: 1,
                logTemplate: "{user.Id} attacks {target.Id} for {damage} damage!",
                sceneComponent: sceneComponent
            );
            // Fighter Abilities
            heroAbilities["FighterMeleeAttack"] = new Ability(
                id: "FighterMeleeAttack",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = true
                    }, isMelee: true);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = true
                    }, "FighterMeleeAttack");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} attacks {target.Id} for {damage} damage!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 0,
                cooldownType: CooldownType.Actions,
                priority: 3,
                rank: 1,
                logTemplate: "{user.Id} attacks {target.Id} for {damage} damage!",
                sceneComponent: sceneComponent
            );
            heroAbilities["FighterShieldBash"] = new Ability(
                id: "FighterShieldBash",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => targets.Any(t => t.PartyPosition <= 2 && t.Health > 0 && !t.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = true
                    }, isMelee: true);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = true
                    }, "FighterShieldBash");
                    var targetState = sceneComponent.GetUnitAttackState(target);
                    if (targetState != null && !targetState.SkipNextAttack)
                        sceneComponent.ProcessEffect(user, target as CharacterStats, "SkipNextAttack", "FighterShieldBash");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} bashes {target.Id} for {damage} damage, delaying their attack!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 1,
                cooldownType: CooldownType.Rounds,
                priority: 2,
                rank: 1,
                logTemplate: "{user.Id} bashes {target.Id} for {damage} damage, delaying their attack!",
                sceneComponent: sceneComponent
            );
            heroAbilities["FighterCoupDeGrace"] = new Ability(
                id: "FighterCoupDeGrace",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => targets.Any(t => t.PartyPosition <= 2 && t.Health > 0 && !t.HasRetreated && t.Health <= 0.25f * t.MaxHealth),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.LowestHealth,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = true
                    }, isMelee: true);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    float healthRatio = (float)target.Health / target.MaxHealth;
                    if (UnityEngine.Random.value > healthRatio)
                    {
                        target.Health = 0;
                        sceneComponent.EventBus.RaiseLogMessage(
                            "{user.Id} executes {target.Id} with CoupDeGrace!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id),
                            sceneComponent.UIConfig.TextColor);
                        sceneComponent.EventBus.RaiseUnitDied(target);
                    }
                    else
                    {
                        sceneComponent.EventBus.RaiseLogMessage(
                            "{target.Id} resists CoupDeGrace!".Replace("{target.Id}", (target as CharacterStats).Id),
                            sceneComponent.UIConfig.TextColor);
                    }
                },
                cooldown: 2,
                cooldownType: CooldownType.Actions,
                priority: 1,
                rank: 1,
                logTemplate: "{user.Id} executes {target.Id} with CoupDeGrace! | {target.Id} resists CoupDeGrace!",
                sceneComponent: sceneComponent
            );
            // Monk Abilities
            heroAbilities["MonkBasicAttack"] = new Ability(
                id: "MonkBasicAttack",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = true
                    }, isMelee: true);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = true
                    }, "MonkBasicAttack");
                    var userState = sceneComponent.GetUnitAttackState(user);
                    if (userState != null) userState.TempStats["Evasion"] = (5, 1);
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} attacks {target.Id} for {damage} damage, boosting Evasion!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                    sceneComponent.EventBus.RaiseUnitUpdated(user, user.GetDisplayStats());
                },
                cooldown: 0,
                cooldownType: CooldownType.Actions,
                priority: 3,
                rank: 1,
                logTemplate: "{user.Id} attacks {target.Id} for {damage} damage, boosting Evasion!",
                sceneComponent: sceneComponent
            );
            heroAbilities["MonkChiStrike"] = new Ability(
                id: "MonkChiStrike",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => user.Morale > 30 && targets.Any(t => t.PartyPosition <= 2 && t.Health > 0 && !t.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = true
                    }, isMelee: true);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    user.Morale = Mathf.Max(0, user.Morale - 10);
                    sceneComponent.ProcessEffect(user, target as CharacterStats, $"TrueStrike:{user.Attack}", "MonkChiStrike");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} unleashes ChiStrike on {target.Id} for {damage} true damage!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                    sceneComponent.EventBus.RaiseUnitUpdated(user, user.GetDisplayStats());
                },
                cooldown: 2,
                cooldownType: CooldownType.Actions,
                priority: 2,
                rank: 1,
                logTemplate: "{user.Id} unleashes ChiStrike on {target.Id} for {damage} true damage!",
                sceneComponent: sceneComponent
            );
            heroAbilities["MonkMeditate"] = new Ability(
                id: "MonkMeditate",
                animationTrigger: "DefaultBuff",
                useCondition: (user, party, targets) => user.Health < 0.5f * user.MaxHealth,
                effect: (user, party, targets) =>
                {
                    int healAmount = Mathf.Min((int)(0.1f * user.MaxHealth), user.MaxHealth - user.Health);
                    user.Health += healAmount;
                    user.Morale = Mathf.Min(user.Morale + 5, user.MaxMorale);
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} meditates, healing {amount} HP and gaining 5 Morale!".Replace("{user.Id}", user.Id).Replace("{amount}", healAmount.ToString()),
                        sceneComponent.UIConfig.TextColor);
                    sceneComponent.EventBus.RaiseUnitUpdated(user, user.GetDisplayStats());
                },
                cooldown: 1,
                cooldownType: CooldownType.Rounds,
                priority: 1,
                rank: 1,
                logTemplate: "{user.Id} meditates, healing {amount} HP and gaining 5 Morale!",
                sceneComponent: sceneComponent
            );
            // Scout Abilities
            heroAbilities["ScoutBasicAttack"] = new Ability(
                id: "ScoutBasicAttack",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = false
                    }, isMelee: false);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = true
                    }, "ScoutBasicAttack");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} attacks {target.Id} for {damage} damage!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 0,
                cooldownType: CooldownType.Actions,
                priority: 3,
                rank: 1,
                logTemplate: "{user.Id} attacks {target.Id} for {damage} damage!",
                sceneComponent: sceneComponent
            );
            heroAbilities["ScoutSniperShot"] = new Ability(
                id: "ScoutSniperShot",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => targets.Any(t => t.PartyPosition <= 4 && t.Health > 0 && !t.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.HighestHealth,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = false
                    }, isMelee: false);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = false
                    }, "ScoutSniperShot");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} snipes {target.Id} for {damage} damage!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 1,
                cooldownType: CooldownType.Actions,
                priority: 2,
                rank: 1,
                logTemplate: "{user.Id} snipes {target.Id} for {damage} damage!",
                sceneComponent: sceneComponent
            );
            heroAbilities["ScoutEnhanceWeaponry"] = new Ability(
                id: "ScoutEnhanceWeaponry",
                animationTrigger: "DefaultBuff",
                useCondition: (user, party, targets) => party.HeroStats.Any(h => h != user && h.Health > 0 && !h.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, party.HeroStats.Cast<ICombatUnit>().ToList(), party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Ally,
                        MeleeOnly = false
                    }, isMelee: false);
                    var targetAlly = selectedTargets.FirstOrDefault() as CharacterStats;
                    if (targetAlly == null) return;
                    var targetState = sceneComponent.GetUnitAttackState(targetAlly);
                    if (targetState != null) targetState.TempStats["Attack"] = (5, 1);
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} enhances {target.Id}’s weaponry, boosting Attack by 5!".Replace("{user.Id}", user.Id).Replace("{target.Id}", targetAlly.Id),
                        sceneComponent.UIConfig.TextColor);
                    sceneComponent.EventBus.RaiseUnitUpdated(targetAlly, targetAlly.GetDisplayStats());
                },
                cooldown: 1,
                cooldownType: CooldownType.Rounds,
                priority: 1,
                rank: 1,
                logTemplate: "{user.Id} enhances {target.Id}’s weaponry, boosting Attack by 5!",
                sceneComponent: sceneComponent
            );
            // Healer Abilities
            heroAbilities["HealerBasicAttack"] = new Ability(
                id: "HealerBasicAttack",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = false
                    }, isMelee: false);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = true
                    }, "HealerBasicAttack");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} attacks {target.Id} for {damage} damage!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 0,
                cooldownType: CooldownType.Actions,
                priority: 3,
                rank: 1,
                logTemplate: "{user.Id} attacks {target.Id} for {damage} damage!",
                sceneComponent: sceneComponent
            );
            heroAbilities["HealerHeal"] = new Ability(
                id: "HealerHeal",
                animationTrigger: "DefaultBuff",
                useCondition: (user, party, targets) => party.HeroStats.Any(h => h.Health < 0.75f * h.MaxHealth),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, party.HeroStats.Cast<ICombatUnit>().ToList(), party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.LowestHealth,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Ally,
                        MeleeOnly = false
                    }, isMelee: false);
                    var target = selectedTargets.FirstOrDefault() as CharacterStats;
                    if (target == null) return;
                    int healAmount = Mathf.Min((int)(0.15f * target.MaxHealth), target.MaxHealth - target.Health);
                    target.Health += healAmount;
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} heals {target.Id} for {amount} HP!".Replace("{user.Id}", user.Id).Replace("{target.Id}", target.Id).Replace("{amount}", healAmount.ToString()),
                        sceneComponent.UIConfig.TextColor);
                    sceneComponent.EventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                },
                cooldown: 1,
                cooldownType: CooldownType.Rounds,
                priority: 2,
                rank: 1,
                logTemplate: "{user.Id} heals {target.Id} for {amount} HP!",
                sceneComponent: sceneComponent
            );
            heroAbilities["HealerSteelResolve"] = new Ability(
                id: "HealerSteelResolve",
                animationTrigger: "DefaultBuff",
                useCondition: (user, party, targets) => party.HeroStats.Any(h => h.Health > 0 && !h.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, party.HeroStats.Cast<ICombatUnit>().ToList(), party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.AllAllies,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Ally,
                        MeleeOnly = false
                    }, isMelee: false);
                    foreach (var ally in selectedTargets.Cast<CharacterStats>())
                    {
                        var allyState = sceneComponent.GetUnitAttackState(ally);
                        if (allyState != null && !allyState.TempStats.ContainsKey("MoraleShield"))
                            allyState.TempStats["MoraleShield"] = (0, -1);
                        sceneComponent.EventBus.RaiseUnitUpdated(ally, ally.GetDisplayStats());
                    }
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} casts SteelResolve, shielding allies from Morale loss!".Replace("{user.Id}", user.Id),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 2,
                cooldownType: CooldownType.Actions,
                priority: 1,
                rank: 1,
                logTemplate: "{user.Id} casts SteelResolve, shielding allies from Morale loss!",
                sceneComponent: sceneComponent
            );
            // MireShambler Abilities
            monsterAbilities["ShamblerThornNeedle"] = new Ability(
                id: "ShamblerThornNeedle",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = false
                    }, isMelee: false);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.None,
                        Dodgeable = true
                    }, "ShamblerThornNeedle");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} fires ThornNeedle at {target.Id} for {damage} damage!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 0,
                cooldownType: CooldownType.Actions,
                priority: 2,
                rank: 0,
                logTemplate: "{user.Id} fires ThornNeedle at {target.Id} for {damage} damage!",
                sceneComponent: sceneComponent
            );
            monsterAbilities["ShamblerSwampBrambles"] = new Ability(
                id: "ShamblerSwampBrambles",
                animationTrigger: "DefaultBuff",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var userState = sceneComponent.GetUnitAttackState(user);
                    if (userState != null) userState.TempStats["ThornsReflect"] = (50, 1);
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} grows SwampBrambles, reflecting damage!".Replace("{user.Id}", user.Id),
                        sceneComponent.UIConfig.TextColor);
                    sceneComponent.EventBus.RaiseUnitUpdated(user, user.GetDisplayStats());
                },
                cooldown: 1,
                cooldownType: CooldownType.Actions,
                priority: 1,
                rank: 0,
                logTemplate: "{user.Id} grows SwampBrambles, reflecting damage!",
                sceneComponent: sceneComponent
            );
            // BogFiend Abilities
            monsterAbilities["FiendMeleeAttack"] = new Ability(
                id: "FiendMeleeAttack",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = true
                    }, isMelee: true);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = true
                    }, "FiendMeleeAttack");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} attacks {target.Id} for {damage} damage!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 0,
                cooldownType: CooldownType.Actions,
                priority: 3,
                rank: 0,
                logTemplate: "{user.Id} attacks {target.Id} for {damage} damage!",
                sceneComponent: sceneComponent
            );
            monsterAbilities["FiendSludgeSlam"] = new Ability(
                id: "FiendSludgeSlam",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => targets.Any(t => t.PartyPosition <= 2 && t.Health > 0 && !t.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = true
                    }, isMelee: true);
                    foreach (var target in selectedTargets.Cast<CharacterStats>())
                    {
                        sceneComponent.ApplyAttackDamage(user, target, new CombatSceneComponent.AttackParams
                        {
                            Defense = CombatSceneComponent.DefenseCheck.Standard,
                            Dodgeable = true
                        }, "FiendSludgeSlam");
                        var targetState = sceneComponent.GetUnitAttackState(target);
                        if (targetState != null) targetState.TempStats["Speed"] = (-1, 1);
                        sceneComponent.EventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} slams heroes for {damage} damage, slowing them!".Replace("{user.Id}", user.Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 1,
                cooldownType: CooldownType.Rounds,
                priority: 1,
                rank: 0,
                logTemplate: "{user.Id} slams heroes for {damage} damage, slowing them!",
                sceneComponent: sceneComponent
            );
            monsterAbilities["FiendDrainHealth"] = new Ability(
                id: "FiendDrainHealth",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => targets.Any(t => t.PartyPosition <= 4 && t.Health > 0 && !t.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = false
                    }, isMelee: false);
                    var target = selectedTargets.FirstOrDefault() as CharacterStats;
                    if (target == null) return;
                    int damage = (int)(0.1f * target.MaxHealth);
                    sceneComponent.ProcessEffect(user, target, $"TrueStrike:{damage}", "FiendDrainHealth");
                    int healAmount = Mathf.Min((int)(0.1f * user.MaxHealth), user.MaxHealth - user.Health);
                    user.Health += healAmount;
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} drains {target.Id} for {damage} HP, healing itself!".Replace("{user.Id}", user.Id).Replace("{target.Id}", target.Id).Replace("{damage}", damage.ToString()),
                        sceneComponent.UIConfig.TextColor);
                    sceneComponent.EventBus.RaiseUnitUpdated(user, user.GetDisplayStats());
                },
                cooldown: 2,
                cooldownType: CooldownType.Actions,
                priority: 2,
                rank: 0,
                logTemplate: "{user.Id} drains {target.Id} for {damage} HP, healing itself!",
                sceneComponent: sceneComponent
            );
            // UmbralCorvax Abilities
            monsterAbilities["CorvaxBasicAttack"] = new Ability(
                id: "CorvaxBasicAttack",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = false
                    }, isMelee: false);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = true
                    }, "CorvaxBasicAttack");
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} attacks {target.Id} for {damage} damage!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 0,
                cooldownType: CooldownType.Actions,
                priority: 3,
                rank: 0,
                logTemplate: "{user.Id} attacks {target.Id} for {damage} damage!",
                sceneComponent: sceneComponent
            );
            monsterAbilities["CorvaxMortifyingShriek"] = new Ability(
                id: "CorvaxMortifyingShriek",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => targets.Any(t => t.PartyPosition <= 4 && t.Health > 0 && !t.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = false
                    }, isMelee: false);
                    foreach (var target in selectedTargets.Cast<CharacterStats>())
                    {
                        sceneComponent.ApplyMoraleDamage(user, target, 10, "CorvaxMortifyingShriek");
                    }
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} shrieks, draining 10 Morale from heroes!".Replace("{user.Id}", user.Id),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 3,
                cooldownType: CooldownType.Rounds,
                priority: 1,
                rank: 0,
                logTemplate: "{user.Id} shrieks, draining 10 Morale from heroes!",
                sceneComponent: sceneComponent
            );
            monsterAbilities["CorvaxWindsOfTerror"] = new Ability(
                id: "CorvaxWindsOfTerror",
                animationTrigger: "DefaultBuff",
                useCondition: (user, party, targets) => sceneComponent.GetMonsterUnits().Any(m => m.Health > 0 && !m.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, sceneComponent.GetMonsterUnits().Cast<ICombatUnit>().ToList(), party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.AllAllies,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Ally,
                        MeleeOnly = false
                    }, isMelee: false);
                    foreach (var ally in selectedTargets.Cast<CharacterStats>())
                    {
                        var allyState = sceneComponent.GetUnitAttackState(ally);
                        if (allyState != null) allyState.TempStats["Speed"] = (2, 1);
                        sceneComponent.EventBus.RaiseUnitUpdated(ally, ally.GetDisplayStats());
                    }
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} summons WindsOfTerror, boosting ally Speed!".Replace("{user.Id}", user.Id),
                        sceneComponent.UIConfig.TextColor);
                },
                cooldown: 1,
                cooldownType: CooldownType.Rounds,
                priority: 2,
                rank: 0,
                logTemplate: "{user.Id} summons WindsOfTerror, boosting ally Speed!",
                sceneComponent: sceneComponent
            );
            // Wraith Abilities
            monsterAbilities["WraithStrike"] = new Ability(
                id: "WraithStrike",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => true,
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = false,
                    }, isMelee: false);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ProcessEffect(user, target as CharacterStats, $"TrueStrike:{user.Attack}", "WraithStrike");
                    user.Health = Mathf.Max(0, user.Health - 10);
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} strikes {target.Id} for {damage} true damage, losing 10 HP!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                    sceneComponent.EventBus.RaiseUnitUpdated(user, user.GetDisplayStats());
                    if (user.Health <= 0)
                        sceneComponent.EventBus.RaiseUnitDied(user);
                },
                cooldown: 0,
                cooldownType: CooldownType.Actions,
                priority: 2,
                rank: 0,
                logTemplate: "{user.Id} strikes {target.Id} for {damage} true damage, losing 10 HP!",
                sceneComponent: sceneComponent
            );
            monsterAbilities["WraithHoaryGrasp"] = new Ability(
                id: "WraithHoaryGrasp",
                animationTrigger: "DefaultAttack",
                useCondition: (user, party, targets) => targets.Any(t => t.PartyPosition <= 2 && t.Health > 0 && !t.HasRetreated),
                effect: (user, party, targets) =>
                {
                    var selectedTargets = sceneComponent.SelectTargets(user, targets, party, new CombatSceneComponent.TargetingRule
                    {
                        Type = CombatSceneComponent.TargetingRule.RuleType.Random,
                        Target = CombatSceneComponent.TargetingRule.ConditionTarget.Enemy,
                        MeleeOnly = true
                    }, isMelee: true);
                    var target = selectedTargets.FirstOrDefault();
                    if (target == null) return;
                    sceneComponent.ApplyAttackDamage(user, target as CharacterStats, new CombatSceneComponent.AttackParams
                    {
                        Defense = CombatSceneComponent.DefenseCheck.Standard,
                        Dodgeable = true
                    }, "WraithHoaryGrasp");
                    var targetState = sceneComponent.GetUnitAttackState(target);
                    if (targetState != null)
                    {
                        targetState.TempStats["Defense"] = (-3, 1);
                        sceneComponent.ApplyMoraleDamage(user, target as CharacterStats, 5, "WraithHoaryGrasp");
                    }
                    sceneComponent.EventBus.RaiseLogMessage(
                        "{user.Id} grasps {target.Id} for {damage} damage, reducing Morale and Defense!".Replace("{user.Id}", user.Id).Replace("{target.Id}", (target as CharacterStats).Id).Replace("{damage}", user.Attack.ToString()),
                        sceneComponent.UIConfig.TextColor);
                    sceneComponent.EventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                },
                cooldown: 1,
                cooldownType: CooldownType.Actions,
                priority: 1,
                rank: 0,
                logTemplate: "{user.Id} grasps {target.Id} for {damage} damage, reducing Morale and Defense!",
                sceneComponent: sceneComponent
            );
        }
        public static Ability GetHeroAbility(string id)
        {
            if (heroAbilities.TryGetValue(id, out var ability))
                return ability;
            Debug.LogWarning($"AbilityDatabase: Hero ability ID {id} not found, returning null.");
            return null;
        }
        public static Ability GetMonsterAbility(string id)
        {
            if (monsterAbilities.TryGetValue(id, out var ability))
                return ability;
            Debug.LogWarning($"AbilityDatabase: Monster ability ID {id} not found, returning null.");
            return null;
        }
        public static void Reinitialize(CombatSceneComponent sceneComponent)
        {
            heroAbilities.Clear();
            monsterAbilities.Clear();
            isInitialized = false;
            InitializeAbilities(sceneComponent);
        }
        public static (string abilityId, string failMessage) SelectAbility(CharacterStats unit, PartyData partyData, List<ICombatUnit> targets, UnitAttackState state)
        {
            if (state == null)
            {
                Debug.LogWarning($"No UnitAttackState for {unit.Id}. Falling back to BasicAttack.");
                return ("BasicAttack", $"No UnitAttackState for {unit.Id}. Falling back to BasicAttack.");
            }
            if (unit.abilityIds == null || unit.abilityIds.Length == 0)
            {
                Debug.LogWarning($"No abilities assigned to {unit.Id}. Falling back to BasicAttack.");
                return ("BasicAttack", $"No abilities assigned to {unit.Id}. Falling back to BasicAttack.");
            }
            Ability selectedAbility = null;
            int lowestPriority = int.MaxValue;
            string failMessage = null;
            foreach (var abilityId in unit.abilityIds)
            {
                var ability = unit.Type == CharacterType.Hero ? GetHeroAbility(abilityId) : GetMonsterAbility(abilityId);
                if (ability == null)
                {
                    Debug.LogWarning($"Invalid ability ID {abilityId} for {unit.Id}. Skipping.");
                    continue;
                }
                if (state.AbilityCooldowns.TryGetValue(abilityId, out int actionCd) && actionCd > 0)
                {
                    continue;
                }
                if (state.RoundCooldowns.TryGetValue(abilityId, out int roundCd) && roundCd > 0)
                {
                    continue;
                }
                if (ability.Rank > 0 && unit.Rank < ability.Rank)
                {
                    failMessage = $"Ability {abilityId} requires Rank {ability.Rank}, unit has Rank {unit.Rank}.";
                    Debug.LogWarning(failMessage);
                    continue;
                }
                if (ability.UseCondition(unit, partyData, targets) && ability.Priority < lowestPriority)
                {
                    selectedAbility = ability;
                    lowestPriority = ability.Priority;
                }
            }
            string selectedId = selectedAbility?.Id ?? "BasicAttack";
            if (selectedAbility != null && selectedAbility.Cooldown > 0)
            {
                if (selectedAbility.CooldownType == CooldownType.Actions)
                {
                    state.AbilityCooldowns[selectedId] = selectedAbility.Cooldown;
                }
                else
                {
                    state.RoundCooldowns[selectedId] = selectedAbility.Cooldown;
                }
            }
            return (selectedId, failMessage);
        }
    }
}