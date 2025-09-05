using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public struct AbilityData
    {
        public string Id { get; private set; }
        public string AnimationTrigger { get; private set; }
        public System.Func<object, PartyData, string> Effect { get; private set; }
        public bool IsCommon { get; private set; }
        public bool CanDodge { get; private set; }
        public bool IsMelee { get; private set; } // Added melee flag
        public System.Func<CharacterStats, PartyData, List<ICombatUnit>, bool> UseCondition { get; private set; }

        public AbilityData(
            string id,
            string animationTrigger,
            System.Func<object, PartyData, string> effect,
            bool isCommon = false,
            bool canDodge = false,
            bool isMelee = false, // Default to false
            System.Func<CharacterStats, PartyData, List<ICombatUnit>, bool> useCondition = null)
        {
            Id = id;
            AnimationTrigger = animationTrigger;
            Effect = effect;
            IsCommon = isCommon;
            CanDodge = canDodge;
            IsMelee = isMelee;
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
                effect: (target, partyData) => "",
                isCommon: true,
                isMelee: true, // Melee attack
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
                isMelee: true, // Melee attack
                useCondition: (user, party, targets) => false
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
                isMelee: true, // Melee-based defense
                useCondition: (user, party, targets) => false
            ));

            heroAbilities.Add("TreasureFind", new AbilityData(
                id: "TreasureFind",
                animationTrigger: "TreasureFind",
                effect: (target, partyData) => "Treasure found!",
                useCondition: (user, party, targets) => false
            ));
        }

        private static void InitializeMonsterAbilities()
        {
            monsterAbilities.Add("BasicAttack", new AbilityData(
                id: "BasicAttack",
                animationTrigger: "BasicAttack",
                effect: (target, partyData) => "",
                isCommon: true,
                isMelee: true // Melee attack
            ));

            monsterAbilities.Add("DefaultMonsterAbility", new AbilityData(
                id: "DefaultMonsterAbility",
                animationTrigger: "DefaultMonsterAttack",
                effect: (target, partyData) => "",
                useCondition: (user, party, targets) => false
            ));

            monsterAbilities.Add("Bog FiendClaw", new AbilityData(
                id: "Bog FiendClaw",
                animationTrigger: "Bog FiendClaw",
                effect: (target, partyData) => "",
                isMelee: true // Melee attack
            ));

            monsterAbilities.Add("Bog FiendRend", new AbilityData(
                id: "Bog FiendRend",
                animationTrigger: "Bog FiendRend",
                effect: (target, partyData) => "",
                isMelee: true // Melee attack
            ));

            monsterAbilities.Add("WraithStrike", new AbilityData(
                id: "WraithStrike",
                animationTrigger: "WraithStrike",
                effect: (target, partyData) => "",
                canDodge: true
            ));

            monsterAbilities.Add("SkeletonSlash", new AbilityData(
                id: "SkeletonSlash",
                animationTrigger: "SkeletonSlash",
                effect: (target, partyData) => "",
                isMelee: true // Melee attack
            ));

            monsterAbilities.Add("VampireBite", new AbilityData(
                id: "VampireBite",
                animationTrigger: "VampireBite",
                effect: (target, partyData) => "",
                isMelee: true // Melee attack
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