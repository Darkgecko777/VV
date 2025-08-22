using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public struct AbilityData
    {
        public string Id { get; private set; }
        public string AnimationTrigger { get; private set; }
        public System.Action<object, PartyData> Effect { get; private set; } // Supports HeroStats or MonsterStats
        public bool IsCommon { get; private set; } // Flags common attacks
        public bool CanDodge { get; private set; } // For monster dodge (ethereal)

        public AbilityData(string id, string animationTrigger, System.Action<object, PartyData> effect, bool isCommon = false, bool canDodge = false)
        {
            Id = id;
            AnimationTrigger = animationTrigger;
            Effect = effect;
            IsCommon = isCommon;
            CanDodge = canDodge;
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
                effect: (target, partyData) =>
                {
                    // Basic attack uses character's Attack stat; damage applied in auto-battler
                    string targetId = target is HeroStats hero ? hero.Type.Id : (target as MonsterStats).Type.Id;
                    Debug.Log($"{targetId} uses Basic Attack!");
                },
                isCommon: true
            ));

            heroAbilities.Add("FighterAttack", new AbilityData(
                id: "FighterAttack",
                animationTrigger: "FighterAttack",
                effect: (target, partyData) =>
                {
                    if (target is HeroStats hero && hero.Health < hero.MaxHealth * 0.3f)
                    {
                        hero.Attack += 3;
                        Debug.Log($"{hero.Type.Id} boosts attack by 3!");
                    }
                }
            ));

            heroAbilities.Add("HealerHeal", new AbilityData(
                id: "HealerHeal",
                animationTrigger: "HealerHeal",
                effect: (target, partyData) =>
                {
                    if (target is HeroStats hero && partyData != null)
                    {
                        HeroStats lowestAlly = partyData.FindLowestHealthAlly();
                        if (lowestAlly != null && lowestAlly.Health > 0)
                        {
                            lowestAlly.Health = Mathf.Min(lowestAlly.Health + 5, lowestAlly.MaxHealth);
                            Debug.Log($"{hero.Type.Id} heals {lowestAlly.Type.Id} for 5 HP!");
                        }
                    }
                }
            ));

            heroAbilities.Add("ScoutDefend", new AbilityData(
                id: "ScoutDefend",
                animationTrigger: "ScoutDefend",
                effect: (target, partyData) =>
                {
                    if (target is HeroStats hero)
                    {
                        hero.Defense += 2;
                        Debug.Log($"{hero.Type.Id} boosts defense by 2!");
                    }
                }
            ));

            heroAbilities.Add("TreasureHunterBoost", new AbilityData(
                id: "TreasureHunterBoost",
                animationTrigger: "TreasureHunterBoost",
                effect: (target, partyData) =>
                {
                    if (target is HeroStats hero)
                    {
                        hero.Morale = Mathf.Min(hero.Morale + 5, 100);
                        Debug.Log($"{hero.Type.Id} boosts morale by 5!");
                    }
                }
            ));
        }

        private static void InitializeMonsterAbilities()
        {
            monsterAbilities.Add("BasicAttack", new AbilityData(
                id: "BasicAttack",
                animationTrigger: "BasicAttack",
                effect: (target, partyData) =>
                {
                    // Shared basic attack; damage based on Attack stat (applied in auto-battler)
                    string targetId = target is MonsterStats monster ? monster.Type.Id : (target as HeroStats).Type.Id;
                    Debug.Log($"{targetId} uses Basic Attack!");
                },
                isCommon: true
            ));

            monsterAbilities.Add("DefaultMonsterAbility", new AbilityData(
                id: "DefaultMonsterAbility",
                animationTrigger: "DefaultMonsterAttack",
                effect: (target, partyData) =>
                {
                    // No-op for monsters with no special ability
                    if (target is MonsterStats monster)
                    {
                        Debug.Log($"{monster.Type.Id} uses default ability (no effect).");
                    }
                }
            ));

            monsterAbilities.Add("GhoulClaw", new AbilityData(
                id: "GhoulClaw",
                animationTrigger: "GhoulClaw",
                effect: (target, partyData) =>
                {
                    // Damage based on Attack stat (applied in auto-battler)
                    if (target is MonsterStats monster)
                    {
                        Debug.Log($"{monster.Type.Id} uses Claw Attack!");
                    }
                }
            ));

            monsterAbilities.Add("GhoulRend", new AbilityData(
                id: "GhoulRend",
                animationTrigger: "GhoulRend",
                effect: (target, partyData) =>
                {
                    if (target is MonsterStats monster)
                    {
                        Debug.Log($"{monster.Type.Id} uses Rend!"); // Removed morale reduction as monsters lack Morale
                    }
                }
            ));

            // Example ethereal monster ability with dodge
            monsterAbilities.Add("WraithStrike", new AbilityData(
                id: "WraithStrike",
                animationTrigger: "WraithStrike",
                effect: (target, partyData) =>
                {
                    if (target is MonsterStats monster)
                    {
                        Debug.Log($"{monster.Type.Id} uses Wraith Strike!");
                    }
                },
                canDodge: true // Ethereal dodge
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