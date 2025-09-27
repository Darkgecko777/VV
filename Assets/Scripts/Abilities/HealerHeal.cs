using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    public class HealerHeal : IAbility
    {
        public string Id => "HealerHeal";
        public int Priority => 2;
        public int Cooldown => 1;
        public CombatTypes.CooldownType CooldownType => CombatTypes.CooldownType.Actions;
        public int Rank => 1;
        public string AnimationTrigger => "DefaultBuff";
        public string LogTemplate => "{user.Id} heals {target.Id} for {amount} HP!";
        public List<CombatTypes.AbilityCondition> Conditions => new List<CombatTypes.AbilityCondition>
        {
            new CombatTypes.AbilityCondition
            {
                Comparison = CombatTypes.Comparison.Lesser,
                Stat = CombatTypes.Stat.Health,
                Threshold = 0.75f,
                IsPercentage = true,
                Target = CombatTypes.ConditionTarget.Ally,
                MinTargetCount = 1
            }
        };

        public CombatTypes.TargetingRule GetTargetingRule()
        {
            return new CombatTypes.TargetingRule
            {
                Type = CombatTypes.TargetingRule.RuleType.LowestHealth,
                MinPosition = 1,
                MaxPosition = 4 // Can target any party position
            };
        }

        public List<(string tag, int value, int duration)> GetEffects()
        {
            return new List<(string, int, int)>();
        }

        public void Execute(CharacterStats user, PartyData party, List<ICombatUnit> targets, CombatSceneComponent context)
        {
            var selectedTargets = context.SelectTargets(user, party.HeroStats.Cast<ICombatUnit>().ToList(), party, GetTargetingRule(), isMelee: false, CombatTypes.ConditionTarget.Ally);
            var target = selectedTargets.FirstOrDefault() as CharacterStats;
            if (target == null)
            {
                Debug.LogWarning($"HealerHeal: No valid target found for {user.Id}.");
                return;
            }

            int healAmount = Mathf.Min(Mathf.RoundToInt(0.15f * target.MaxHealth), target.MaxHealth - target.Health);
            target.Health += healAmount;

            string logMessage = LogTemplate
                .Replace("{user.Id}", user.Id)
                .Replace("{target.Id}", target.Id)
                .Replace("{amount}", healAmount.ToString());
            context.EventBus.RaiseLogMessage(logMessage, context.UIConfig.TextColor);
            context.EventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
        }
    }
}