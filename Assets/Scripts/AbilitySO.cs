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
        [SerializeField] private List<EffectSO> effects = new List<EffectSO>();
        [SerializeField]
        private CombatTypes.TargetingRule rule = new CombatTypes.TargetingRule
        {
            Type = CombatTypes.TargetingRule.RuleType.Single,
            Target = CombatTypes.ConditionTarget.Enemy,
            MeleeOnly = true,
            Criteria = CombatTypes.TargetingRule.SelectionCriteria.Default
        };
        [SerializeField] private bool dodgeable = true;
        [SerializeField]
        private CombatTypes.CooldownParams cooldownParams = new CombatTypes.CooldownParams
        {
            Type = CombatTypes.CooldownType.None,
            Duration = 0
        };

        public string Id => id;
        public string AnimationTrigger => animationTrigger;
        public List<EffectSO> Effects => effects;
        public CombatTypes.TargetingRule Rule => rule;
        public bool Dodgeable => dodgeable;
        public CombatTypes.CooldownParams CooldownParams => cooldownParams;

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