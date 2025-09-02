using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public struct AbilityData
    {
        public string Id { get; private set; }
        public string AnimationTrigger { get; private set; }
        public System.Action<object, PartyData> Effect { get; private set; }
        public bool IsCommon { get; private set; }
        public bool CanDodge { get; private set; }
        public System.Func<CharacterStats, PartyData, List<ICombatUnit>, bool> UseCondition { get; private set; }

        public AbilityData(
            string id,
            string animationTrigger,
            System.Action<object, PartyData> effect,
            bool isCommon = false,
            bool canDodge = false,
            System.Func<CharacterStats, PartyData, List<ICombatUnit>, bool> useCondition = null)
        {
            Id = id;
            AnimationTrigger = animationTrigger;
            Effect = effect;
            IsCommon = isCommon;
            CanDodge = canDodge;
            UseCondition = useCondition ?? ((user, party, targets) => false);
        }
    }

    public static class AbilityDatabase
    {
        private static readonly Dictionary<string, AbilityData> heroAbilities = new Dictionary<string, AbilityData>();
        private static readonly Dictionary<string, AbilityData> monsterAbilities = new Dictionary<string, AbilityData>();

        static AbilityDatabase()
        {
            InitializeHeroAbilities();
            InitializeMonsterAbilities();
        }

        private static void InitializeHeroAbilities()
        {
            heroAbilities.Add("BasicAttack", new AbilityData(
                id: "BasicAttack",
                animationTrigger: "BasicAttack",
                effect: (target, partyData) => { /* Damage applied in CombatSceneController */ },
                isCommon: true,
                useCondition: (user, party, targets) => true // Always available
            ));

            heroAbilities.Add("FighterAttack", new AbilityData(
                id: "FighterAttack",
                animationTrigger: "FighterAttack",
                effect: (target, partyData) =>
                {
                    if (target is CharacterStats stats && stats.IsHero && stats.Health < stats.MaxHealth * 0.3f)
                    {
                        stats.Attack += 3;
                    }
                },
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            heroAbilities.Add("HealerHeal", new AbilityData(
                id: "HealerHeal",
                animationTrigger: "HealerHeal",
                effect: (target, partyData) =>
                {
                    if (target is CharacterStats stats && stats.IsHero && partyData.HeroStats.Any(h => h.Health < h.MaxHealth))
                    {
                        var lowestHealthAlly = partyData.FindLowestHealthAlly();
                        if (lowestHealthAlly != null)
                        {
                            lowestHealthAlly.Health = Mathf.Min(lowestHealthAlly.Health + 10, lowestHealthAlly.MaxHealth);
                        }
                    }
                },
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            heroAbilities.Add("ScoutDefend", new AbilityData(
                id: "ScoutDefend",
                animationTrigger: "ScoutDefend",
                effect: (target, partyData) =>
                {
                    if (target is CharacterStats stats && stats.IsHero)
                    {
                        stats.Defense += 2;
                    }
                },
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            heroAbilities.Add("TreasureFind", new AbilityData(
                id: "TreasureFind",
                animationTrigger: "TreasureFind",
                effect: (target, partyData) => { /* Placeholder effect */ },
                useCondition: (user, party, targets) => false // Disabled for now
            ));
        }

        private static void InitializeMonsterAbilities()
        {
            monsterAbilities.Add("BasicAttack", new AbilityData(
                id: "BasicAttack",
                animationTrigger: "BasicAttack",
                effect: (target, partyData) => { /* Damage applied in CombatSceneController */ },
                isCommon: true,
                useCondition: (user, party, targets) => true // Always available
            ));

            monsterAbilities.Add("DefaultMonsterAbility", new AbilityData(
                id: "DefaultMonsterAbility",
                animationTrigger: "DefaultMonsterAttack",
                effect: (target, partyData) => { /* No-op */ },
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            monsterAbilities.Add("GhoulClaw", new AbilityData(
                id: "GhoulClaw",
                animationTrigger: "GhoulClaw",
                effect: (target, partyData) => { /* Damage applied in CombatSceneController */ },
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            monsterAbilities.Add("GhoulRend", new AbilityData(
                id: "GhoulRend",
                animationTrigger: "GhoulRend",
                effect: (target, partyData) => { /* No-op */ },
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            monsterAbilities.Add("WraithStrike", new AbilityData(
                id: "WraithStrike",
                animationTrigger: "WraithStrike",
                effect: (target, partyData) => { /* Damage applied in CombatSceneController */ },
                canDodge: true,
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            monsterAbilities.Add("SkeletonSlash", new AbilityData(
                id: "SkeletonSlash",
                animationTrigger: "SkeletonSlash",
                effect: (target, partyData) => { /* Damage applied in CombatSceneController */ },
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            monsterAbilities.Add("VampireBite", new AbilityData(
                id: "VampireBite",
                animationTrigger: "VampireBite",
                effect: (target, partyData) => { /* Damage applied in CombatSceneController */ },
                useCondition: (user, party, targets) => false // Disabled for now
            ));
        }

        public static AbilityData? GetHeroAbility(string id)
        {
            return heroAbilities.TryGetValue(id, out var ability) ? ability : null;
        }

        public static AbilityData? GetMonsterAbility(string id)
        {
            return monsterAbilities.TryGetValue(id, out var ability) ? ability : null;
        }

        public static List<AbilityData> GetCommonAbilities()
        {
            var common = heroAbilities.Values.Where(a => a.IsCommon).ToList();
            common.AddRange(monsterAbilities.Values.Where(a => a.IsCommon));
            return common;
        }
    }
}