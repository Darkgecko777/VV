using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public struct AbilityData
    {
        public string Id { get; private set; }
        public string AnimationTrigger { get; private set; }
        public System.Func<object, PartyData, string> Effect { get; private set; } // Changed to Func for log return
        public bool IsCommon { get; private set; }
        public bool CanDodge { get; private set; }
        public System.Func<CharacterStats, PartyData, List<ICombatUnit>, bool> UseCondition { get; private set; }

        public AbilityData(
            string id,
            string animationTrigger,
            System.Func<object, PartyData, string> effect,
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
                effect: (target, partyData) => "", // No log, damage handled separately
                isCommon: true,
                useCondition: (user, party, targets) =>
                {
                    if (user.Id == "Healer")
                    {
                        bool shouldHeal = party.HeroStats.Any(h => h.Type == CharacterType.Hero && h.Health < h.MaxHealth * 0.75f);
                        return !shouldHeal;
                    }
                    return true;
                }
            ));

            heroAbilities.Add("FighterAttack", new AbilityData(
                id: "FighterAttack",
                animationTrigger: "FighterAttack",
                effect: (target, partyData) =>
                {
                    if (target is CharacterStats stats && stats.IsHero && stats.Health < stats.MaxHealth * 0.3f)
                    {
                        stats.Attack += 3;
                        return $"{stats.Id}'s Attack increased by 3!";
                    }
                    return "";
                },
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            heroAbilities.Add("HealerHeal", new AbilityData(
                id: "HealerHeal",
                animationTrigger: "HealerHeal",
                effect: (target, partyData) =>
                {
                    var lowestHealthAlly = partyData.FindLowestHealthAlly();
                    if (lowestHealthAlly != null)
                    {
                        int oldHealth = lowestHealthAlly.Health;
                        lowestHealthAlly.Health = Mathf.Min(lowestHealthAlly.Health + 10, lowestHealthAlly.MaxHealth);
                        return $"Healer heals {lowestHealthAlly.Id} for {lowestHealthAlly.Health - oldHealth} HP!";
                    }
                    return "";
                },
                useCondition: (user, party, targets) =>
                    party.HeroStats.Any(h => h.Type == CharacterType.Hero && h.Health < h.MaxHealth * 0.75f)
            ));

            heroAbilities.Add("ScoutDefend", new AbilityData(
                id: "ScoutDefend",
                animationTrigger: "ScoutDefend",
                effect: (target, partyData) =>
                {
                    if (target is CharacterStats stats && stats.IsHero)
                    {
                        stats.Defense += 2;
                        return $"{stats.Id}'s Defense increased by 2!";
                    }
                    return "";
                },
                useCondition: (user, party, targets) => false // Disabled for now
            ));

            heroAbilities.Add("TreasureFind", new AbilityData(
                id: "TreasureFind",
                animationTrigger: "TreasureFind",
                effect: (target, partyData) => "Treasure found!", // Example log
                useCondition: (user, party, targets) => false // Disabled for now
            ));
        }

        private static void InitializeMonsterAbilities()
        {
            monsterAbilities.Add("BasicAttack", new AbilityData(
                id: "BasicAttack",
                animationTrigger: "BasicAttack",
                effect: (target, partyData) => "", // No log, damage handled separately
                isCommon: true,
                useCondition: (user, party, targets) => true
            ));

            monsterAbilities.Add("DefaultMonsterAbility", new AbilityData(
                id: "DefaultMonsterAbility",
                animationTrigger: "DefaultMonsterAttack",
                effect: (target, partyData) => "",
                useCondition: (user, party, targets) => false
            ));

            monsterAbilities.Add("GhoulClaw", new AbilityData(
                id: "GhoulClaw",
                animationTrigger: "GhoulClaw",
                effect: (target, partyData) => "", // No log, damage handled separately
                useCondition: (user, party, targets) => false
            ));

            monsterAbilities.Add("GhoulRend", new AbilityData(
                id: "GhoulRend",
                animationTrigger: "GhoulRend",
                effect: (target, partyData) => "",
                useCondition: (user, party, targets) => false
            ));

            monsterAbilities.Add("WraithStrike", new AbilityData(
                id: "WraithStrike",
                animationTrigger: "WraithStrike",
                effect: (target, partyData) => "", // No log, damage handled separately
                canDodge: true,
                useCondition: (user, party, targets) => false
            ));

            monsterAbilities.Add("SkeletonSlash", new AbilityData(
                id: "SkeletonSlash",
                animationTrigger: "SkeletonSlash",
                effect: (target, partyData) => "", // No log, damage handled separately
                useCondition: (user, party, targets) => false
            ));

            monsterAbilities.Add("VampireBite", new AbilityData(
                id: "VampireBite",
                animationTrigger: "VampireBite",
                effect: (target, partyData) => "", // No log, damage handled separately
                useCondition: (user, party, targets) => false
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