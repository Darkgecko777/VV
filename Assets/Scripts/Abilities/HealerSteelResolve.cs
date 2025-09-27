using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    public class HealerSteelResolve : IAbility
    {
        public string Id => "HealerSteelResolve";
        public int Priority => 2;
        public int Cooldown => 3;
        public CombatTypes.CooldownType CooldownType => CombatTypes.CooldownType.Actions;
        public int Rank => 1;
        public string AnimationTrigger => "DefaultBuff";
        public string LogTemplate => "{user.Id} bolsters {target.Id}'s resolve with a MoraleShield!";
        public List<CombatTypes.AbilityCondition> Conditions => new List<CombatTypes.AbilityCondition>
        {
            new CombatTypes.AbilityCondition
            {
                Comparison = CombatTypes.Comparison.Lesser,
                Stat = CombatTypes.Stat.Morale,
                Threshold = 50,
                IsPercentage = false,
                Target = CombatTypes.ConditionTarget.Ally,
                MinTargetCount = 1
            }
        };

        public CombatTypes.TargetingRule GetTargetingRule()
        {
            return new CombatTypes.TargetingRule
            {
                Type = CombatTypes.TargetingRule.RuleType.LowestMorale,
                MinPosition = 1,
                MaxPosition = 4
            };
        }

        public List<(string tag, int value, int duration)> GetEffects()
        {
            return new List<(string, int, int)>
            {
                ("MoraleShield", 0, -1) // Duration -1 for one round, as per CombatSceneComponent.cs
            };
        }

        public void Execute(CharacterStats user, PartyData party, List<ICombatUnit> targets, CombatSceneComponent context)
        {
            var selectedTargets = context.SelectTargets(user, party.HeroStats.Cast<ICombatUnit>().ToList(), party, GetTargetingRule(), isMelee: false, CombatTypes.ConditionTarget.Ally);
            var target = selectedTargets.FirstOrDefault() as CharacterStats;
            if (target == null)
            {
                Debug.LogWarning($"HealerSteelResolve: No valid target found for {user.Id}.");
                return;
            }

            foreach (var effect in GetEffects())
            {
                context.ProcessEffect(user, target, effect.tag, Id);
            }
        }
    }
}