using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "AbilitySO", menuName = "VirulentVentures/AbilitySO")]
    public class AbilitySO : ScriptableObject
    {
        [System.Serializable]
        public struct AbilityAction
        {
            [Tooltip("Target of the action (User, Ally, Enemy)")]
            public CombatTypes.ConditionTarget Target;
            [Tooltip("Effect ID from EffectReference (e.g., Heal, Reflect, Damage)")]
            public string EffectId;
            [Tooltip("Targeting rule (e.g., Single, All, MeleeSingle, MeleeAll)")]
            public CombatTypes.TargetingRule.RuleType RuleType;
            [Tooltip("Defense check for damage effects")]
            public CombatTypes.DefenseCheck Defense;
            [Tooltip("True if damage can be dodged")]
            public bool Dodgeable;
        }

        [SerializeField] private string id;
        [SerializeField] private int cooldown;
        [SerializeField] private CombatTypes.CooldownType cooldownType;
        [SerializeField] private List<CombatTypes.AbilityCondition> conditions;
        [SerializeField] private AbilityAction action;
        [SerializeField] private string animationTrigger;

        public string Id => id;
        public int Cooldown => cooldown;
        public CombatTypes.CooldownType CooldownType => cooldownType;
        public List<CombatTypes.AbilityCondition> Conditions => conditions;
        public AbilityAction Action => action;
        public string AnimationTrigger => animationTrigger;

        public CombatTypes.TargetingRule GetTargetingRule()
        {
            bool isMelee = action.RuleType == CombatTypes.TargetingRule.RuleType.MeleeSingle || action.RuleType == CombatTypes.TargetingRule.RuleType.MeleeAll;
            return new CombatTypes.TargetingRule
            {
                Type = action.RuleType,
                Target = action.Target,
                MeleeOnly = isMelee,
                MinPosition = isMelee ? 1 : 0,
                MaxPosition = isMelee ? 2 : 4
            };
        }

        public List<ICombatUnit> GetConditionFilteredTargets(CharacterStats user, PartyData party, List<ICombatUnit> allTargets, CombatSceneComponent combatScene)
        {
            List<ICombatUnit> basePool;
            if (action.Target == CombatTypes.ConditionTarget.Ally)
            {
                basePool = user.Type == CharacterType.Hero
                    ? party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated).ToList()
                    : allTargets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated).ToList();
            }
            else
            {
                basePool = user.Type == CharacterType.Hero
                    ? allTargets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated).ToList()
                    : party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated).ToList();
            }

            var filtered = new List<ICombatUnit>(basePool);
            foreach (var cond in conditions.Where(c => c.Target == action.Target))
            {
                filtered = filtered.Where(t => MeetsCondition(t as CharacterStats, cond, combatScene)).ToList();
                if (filtered.Count == 0) return new List<ICombatUnit>();
            }

            return filtered;
        }

        public bool EvaluateCondition(CombatTypes.AbilityCondition cond, CharacterStats unit, PartyData party, List<ICombatUnit> targets, CombatSceneComponent combatScene)
        {
            bool statMet = false;

            if (cond.Target == CombatTypes.ConditionTarget.User)
            {
                statMet = MeetsCondition(unit, cond, combatScene);
            }
            else
            {
                var filteredTargets = (cond.Target == CombatTypes.ConditionTarget.Ally
                    ? (unit.Type == CharacterType.Hero ? party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated) : targets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated))
                    : (unit.Type == CharacterType.Hero ? targets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated) : party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated))).ToList();
                statMet = filteredTargets.Any(t => MeetsCondition(t as CharacterStats, cond, combatScene));
            }

            return statMet;
        }

        private bool MeetsCondition(CharacterStats target, CombatTypes.AbilityCondition cond, CombatSceneComponent combatScene)
        {
            if (target == null) return false;
            float value = GetStatValue(cond.Stat, target, combatScene);
            if (cond.IsPercentage)
            {
                float maxValue = cond.Stat == CombatTypes.Stat.Health ? target.MaxHealth : cond.Stat == CombatTypes.Stat.Morale ? target.MaxMorale : 1f;
                value = maxValue > 0 ? value / maxValue : 0f;
            }
            bool met = cond.Comparison switch
            {
                CombatTypes.Comparison.Greater => value > cond.Threshold,
                CombatTypes.Comparison.Lesser => value < cond.Threshold,
                CombatTypes.Comparison.Equal => Mathf.Approximately(value, cond.Threshold),
                _ => false
            };
            return met;
        }

        private float GetStatValue(CombatTypes.Stat stat, CharacterStats unit, CombatSceneComponent combatScene)
        {
            if (unit == null) return 0f;
            return stat switch
            {
                CombatTypes.Stat.Health => unit.Health,
                CombatTypes.Stat.MaxHealth => unit.MaxHealth,
                CombatTypes.Stat.Morale => unit.Morale,
                CombatTypes.Stat.MaxMorale => unit.MaxMorale,
                CombatTypes.Stat.Speed => unit.Speed,
                CombatTypes.Stat.Attack => unit.Attack,
                CombatTypes.Stat.Defense => unit.Defense,
                CombatTypes.Stat.Evasion => unit.Evasion,
                CombatTypes.Stat.Rank => unit.Rank,
                CombatTypes.Stat.Infectivity => unit.Infectivity,
                CombatTypes.Stat.PartyPosition => unit.PartyPosition,
                CombatTypes.Stat.IsInfected => unit.IsInfected ? 1f : 0f,
                CombatTypes.Stat.HasBuff => unit is ICombatUnit u && combatScene.GetUnitAttackState(u)?.TempStats.Any(kv => kv.Value.duration > 0) == true ? 1f : 0f,
                CombatTypes.Stat.HasDebuff => unit is ICombatUnit u && combatScene.GetUnitAttackState(u)?.TempStats.Any(kv => kv.Value.duration > 0 && kv.Value.value < 0) == true ? 1f : 0f,
                CombatTypes.Stat.Role => 0f, // Placeholder, not implemented
                _ => 0f
            };
        }
    }
}