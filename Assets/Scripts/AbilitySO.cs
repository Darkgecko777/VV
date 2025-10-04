using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "AbilitySO", menuName = "VirulentVentures/AbilitySO")]
    public class AbilitySO : ScriptableObject
    {
        [SerializeField] private string id = "MeleeStrike";
        [SerializeField] private string animationTrigger = "Attack";
        [SerializeField] private string effectId = "Damage";
        [SerializeField] private EffectParams effectParams = new EffectParams { Multiplier = 1f, HealthThresholdPercent = 80f };
        [SerializeField]
        private CombatTypes.TargetingRule rule = new CombatTypes.TargetingRule
        {
            Type = CombatTypes.TargetingRule.RuleType.Single,
            Target = CombatTypes.ConditionTarget.Enemy,
            MeleeOnly = true,
            Criteria = CombatTypes.TargetingRule.SelectionCriteria.Default
        };
        [SerializeField]
        private CombatTypes.AttackParams attackParams = new CombatTypes.AttackParams
        {
            Defense = CombatTypes.DefenseCheck.Standard,
            Dodgeable = true
        };
        [SerializeField]
        private CombatTypes.CooldownParams cooldownParams = new CombatTypes.CooldownParams
        {
            Type = CombatTypes.CooldownType.None, // Default: no cooldown
            Duration = 0
        };

        public string Id => id;
        public string AnimationTrigger => animationTrigger;
        public string EffectId => effectId;
        public EffectParams EffectParameters => effectParams;
        public CombatTypes.TargetingRule Rule => rule;
        public CombatTypes.AttackParams AttackParams => attackParams;
        public CombatTypes.CooldownParams CooldownParams => cooldownParams;

        [System.Serializable]
        public struct EffectParams
        {
            public float Multiplier;
            public float HealthThresholdPercent;
        }

        public List<ICombatUnit> GetTargets(CharacterStats user, PartyData party, List<ICombatUnit> allTargets)
        {
            List<ICombatUnit> basePool;
            if (rule.Target == CombatTypes.ConditionTarget.Ally)
            {
                basePool = user.Type == CharacterType.Hero
                    ? party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated).ToList()
                    : allTargets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated).ToList();
            }
            else if (rule.Target == CombatTypes.ConditionTarget.Enemy)
            {
                basePool = user.Type == CharacterType.Hero
                    ? allTargets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated).ToList()
                    : party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated).ToList();
            }
            else
            {
                basePool = new List<ICombatUnit> { user };
            }

            return basePool;
        }
    }
}