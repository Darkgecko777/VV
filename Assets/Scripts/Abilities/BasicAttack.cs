using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    public class BasicAttack : IAbility
    {
        public string Id => "BasicAttack";
        public int Priority => 4;
        public int Cooldown => 0;
        public CombatTypes.CooldownType CooldownType => CombatTypes.CooldownType.Actions;
        public int Rank => 1;
        public string AnimationTrigger => "DefaultAttack";
        public string LogTemplate => "[{user.Attack} ATK - {target.Defense} DEF * 5%] {user.Id} attacks {target.Id} for {damage} damage!";
        public List<CombatTypes.AbilityCondition> Conditions => new List<CombatTypes.AbilityCondition>();

        public CombatTypes.TargetingRule GetTargetingRule()
        {
            return new CombatTypes.TargetingRule
            {
                Type = CombatTypes.TargetingRule.RuleType.Random,
                MeleeOnly = true
            };
        }

        public List<(string tag, int value, int duration)> GetEffects()
        {
            return new List<(string, int, int)>();
        }

        public void Execute(CharacterStats user, PartyData party, List<ICombatUnit> targets, CombatSceneComponent context)
        {
            var selectedTargets = context.SelectTargets(user, targets, party, GetTargetingRule(), isMelee: true, CombatTypes.ConditionTarget.Enemy);
            var target = selectedTargets.FirstOrDefault() as CharacterStats;
            if (target == null) return;

            context.ApplyAttackDamage(user, target, new CombatTypes.AttackParams
            {
                Defense = CombatTypes.DefenseCheck.Standard,
                Dodgeable = true
            }, Id);
        }
    }
}