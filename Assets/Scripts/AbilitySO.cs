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
        private GameTypes.TargetingRule rule = new GameTypes.TargetingRule
        {
            Type = GameTypes.TargetingRule.RuleType.Single,
            Target = GameTypes.ConditionTarget.Enemy,
            MeleeOnly = true,
            Criteria = GameTypes.TargetingRule.SelectionCriteria.Default,
            TargetSelf = false
        };
        [SerializeField] private bool dodgeable = true;
        [SerializeField]
        private GameTypes.CooldownParams cooldownParams = new GameTypes.CooldownParams
        {
            Type = GameTypes.CooldownType.None,
            Duration = 0
        };

        public string Id => id;
        public string AnimationTrigger => animationTrigger;
        public List<EffectSO> Effects => effects;
        public GameTypes.TargetingRule Rule => rule;
        public bool Dodgeable => dodgeable;
        public GameTypes.CooldownParams CooldownParams => cooldownParams;

        public List<ICombatUnit> GetTargets(CharacterStats user, PartyData party, List<ICombatUnit> allTargets)
        {
            if (rule.TargetSelf)
            {
                return new List<ICombatUnit> { user }; // Return user if TargetSelf is true
            }

            List<ICombatUnit> basePool;
            if (rule.Target == GameTypes.ConditionTarget.Ally)
            {
                basePool = user.Type == CharacterType.Hero
                    ? party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated).ToList()
                    : allTargets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated).ToList();
            }
            else if (rule.Target == GameTypes.ConditionTarget.Enemy)
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