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
        public List<string> Tags { get; private set; }
        public System.Func<CharacterStats, PartyData, List<ICombatUnit>, bool> UseCondition { get; private set; }

        public AbilityData(
            string id,
            string animationTrigger,
            System.Func<object, PartyData, string> effect,
            bool isCommon = false,
            List<string> tags = null,
            System.Func<CharacterStats, PartyData, List<ICombatUnit>, bool> useCondition = null)
        {
            Id = id;
            AnimationTrigger = animationTrigger;
            Effect = effect;
            IsCommon = isCommon;
            Tags = tags ?? new List<string>();
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
                tags: new List<string> { "TargetEnemies", "Melee", "Damage", "StandardDefense", "Dodgeable" },
                useCondition: (user, party, targets) =>
                {
                    if (user.Id == "Healer")
                    {
                        return !party.HeroStats.Any(h => h.Type == CharacterType.Hero && h.Health < h.MaxHealth * 0.75f);
                    }
                    return true;
                }
            ));

            heroAbilities.Add("ShieldBash", new AbilityData(
                id: "ShieldBash",
                animationTrigger: "ShieldBash",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "Melee", "Damage", "StandardDefense", "Dodgeable" },
                useCondition: (user, party, targets) =>
                    user.Health > user.MaxHealth * 0.5f && targets.Any(t => t.Attack > user.Defense)
            ));

            heroAbilities.Add("IronResolve", new AbilityData(
                id: "IronResolve",
                animationTrigger: "IronResolve",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetSelf", "Buff", "Morale", "NoDefenseCheck", "NoEvasionCheck", "SkipNextAttack" },
                useCondition: (user, party, targets) =>
                    user.Morale < user.MaxMorale * 0.7f && user.Health > user.MaxHealth * 0.3f
            ));

            heroAbilities.Add("ChiStrike", new AbilityData(
                id: "ChiStrike",
                animationTrigger: "ChiStrike",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "Melee", "Damage", "PartialIgnoreDefense:0.025", "Dodgeable", "Buff" },
                useCondition: (user, party, targets) =>
                    targets.Any(t => t is CharacterStats ts && ts.Defense > user.Attack / 2)
            ));

            heroAbilities.Add("InnerFocus", new AbilityData(
                id: "InnerFocus",
                animationTrigger: "InnerFocus",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetSelf", "Buff", "Morale", "SelfDamage:5", "NoDefenseCheck", "NoEvasionCheck" },
                useCondition: (user, party, targets) =>
                    user.Health < user.MaxHealth * 0.7f && user.Morale < user.MaxMorale * 0.6f
            ));

            heroAbilities.Add("SniperShot", new AbilityData(
                id: "SniperShot",
                animationTrigger: "SniperShot",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "Ranged", "Damage", "StandardDefense", "Dodgeable", "Debuff", "PriorityLowHealth" },
                useCondition: (user, party, targets) =>
                    targets.Any(t => t.Health < t.MaxHealth * 0.3f)
            ));

            heroAbilities.Add("HealerHeal", new AbilityData(
                id: "HealerHeal",
                animationTrigger: "HealerHeal",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetAllies", "Ranged", "Heal", "Morale", "NoDefenseCheck", "NoEvasionCheck" },
                useCondition: (user, party, targets) =>
                    party.HeroStats.Any(h => h.Type == CharacterType.Hero && h.Health < h.MaxHealth * 0.75f)
            ));
        }

        private static void InitializeMonsterAbilities()
        {
            monsterAbilities.Add("BasicAttack", new AbilityData(
                id: "BasicAttack",
                animationTrigger: "BasicAttack",
                effect: (target, partyData) => "",
                isCommon: true,
                tags: new List<string> { "TargetEnemies", "Melee", "Damage", "StandardDefense", "Dodgeable" }
            ));

            monsterAbilities.Add("SludgeSlam", new AbilityData(
                id: "SludgeSlam",
                animationTrigger: "SludgeSlam",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "AOE", "Melee", "Damage", "FixedDamage:10", "Dodgeable", "Infection" },
                useCondition: (user, party, targets) =>
                    targets.Count(t => t is CharacterStats cs && (cs.PartyPosition == 1 || cs.PartyPosition == 2) && t.Health > 0 && !t.HasRetreated) >= 2
            ));

            monsterAbilities.Add("MireGrasp", new AbilityData(
                id: "MireGrasp",
                animationTrigger: "MireGrasp",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "Melee", "Damage", "StandardDefense", "Dodgeable", "Debuff", "Morale" },
                useCondition: (user, party, targets) =>
                    targets.Any(t => t.Speed > user.Speed)
            ));

            monsterAbilities.Add("ThornNeedle", new AbilityData(
                id: "ThornNeedle",
                animationTrigger: "ThornNeedle",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "Ranged", "Damage", "IgnoreDefense", "FixedDamage:6", "Dodgeable" },
                useCondition: (user, party, targets) =>
                    targets.Any(t => t is CharacterStats cs && (cs.PartyPosition == 3 || cs.PartyPosition == 4) && t.Health > 0 && !t.HasRetreated)
            ));

            monsterAbilities.Add("Entangle", new AbilityData(
                id: "Entangle",
                animationTrigger: "Entangle",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "Melee", "Damage", "IgnoreDefense", "FixedDamage:8", "Dodgeable", "SelfDamage:10", "SkipNextAttack" },
                useCondition: (user, party, targets) =>
                    targets.Any(t => t.Speed > user.Speed)
            ));

            monsterAbilities.Add("ShriekOfDespair", new AbilityData(
                id: "ShriekOfDespair",
                animationTrigger: "ShriekOfDespair",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "AOE", "Ranged", "Morale", "NoDefenseCheck", "NoEvasionCheck" },
                useCondition: (user, party, targets) =>
                    party.HeroStats.Any(h => h.Morale > h.MaxMorale * 0.6f)
            ));

            monsterAbilities.Add("FlocksVigor", new AbilityData(
                id: "FlocksVigor",
                animationTrigger: "FlocksVigor",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetAllies", "AOE", "Ranged", "Buff", "NoDefenseCheck", "NoEvasionCheck" },
                useCondition: (user, party, targets) =>
                    targets.Any(t => t is CharacterStats cs && cs.Type == CharacterType.Monster && t.Health < t.MaxHealth * 0.5f)
            ));

            monsterAbilities.Add("TrueStrike", new AbilityData(
                id: "TrueStrike",
                animationTrigger: "TrueStrike",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "Melee", "Damage", "StandardDefense", "Undodgeable", "Morale", "Buff" },
                useCondition: (user, party, targets) => true
            ));

            monsterAbilities.Add("SpectralDrain", new AbilityData(
                id: "SpectralDrain",
                animationTrigger: "SpectralDrain",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "Melee", "Damage", "StandardDefense", "Dodgeable", "Debuff", "Infection", "SelfDamage:5", "Buff" },
                useCondition: (user, party, targets) =>
                    targets.Any(t => t is CharacterStats ts && ts.Defense > 5)
            ));

            monsterAbilities.Add("EtherealWail", new AbilityData(
                id: "EtherealWail",
                animationTrigger: "EtherealWail",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetEnemies", "AOE", "Ranged", "Morale", "Dodgeable", "NoDefenseCheck", "Buff" },
                useCondition: (user, party, targets) =>
                    party.HeroStats.Any(h => h.Morale > h.MaxMorale * 0.5f)
            ));

            monsterAbilities.Add("ViralSpikes", new AbilityData(
                id: "ViralSpikes",
                animationTrigger: "ViralSpikes",
                effect: (target, partyData) => "",
                tags: new List<string> { "TargetSelf", "Buff", "ThornsFixed:5", "ThornsInfection", "NoDefenseCheck", "NoEvasionCheck" },
                useCondition: (user, party, targets) => user.Health < user.MaxHealth * 0.5f
            ));
        }

        public static AbilityData? GetHeroAbility(string id) => heroAbilities.TryGetValue(id, out var ability) ? ability : null;
        public static AbilityData? GetMonsterAbility(string id) => monsterAbilities.TryGetValue(id, out var ability) ? ability : null;

        public static List<AbilityData> GetCommonAbilities()
        {
            var common = heroAbilities.Values.Where(a => a.IsCommon).ToList();
            common.AddRange(monsterAbilities.Values.Where(a => a.IsCommon));
            return common;
        }
    }
}