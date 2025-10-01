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
            [Tooltip("Number of targets (1-4, clamped by Melee)")]
            public int NumberOfTargets;
            [Tooltip("True: targets positions 1-2, False: 1-4")]
            public bool Melee;
            [Tooltip("Effect ID from EffectReference (e.g., Heal, Reflect, Damage)")]
            public string EffectId;
            [Tooltip("Value to scale effect (e.g., 0.15 for 15% heal)")]
            public float EffectValue;
            [Tooltip("Duration in rounds for buffs (0 for instant effects)")]
            public int EffectDuration;
            [Tooltip("Targeting rule (e.g., LowestHealth, Random)")]
            public CombatTypes.TargetingRule.RuleType RuleType;
            [Tooltip("Defense check for damage effects")]
            public CombatTypes.DefenseCheck Defense;
            [Tooltip("True if damage can be dodged")]
            public bool Dodgeable;
            [Tooltip("Multiplier for partial defense (e.g., 0.025)")]
            public float PartialDefenseMultiplier;
        }

        [SerializeField] private string id;
        [SerializeField] private int cooldown;
        [SerializeField] private CombatTypes.CooldownType cooldownType;
        [SerializeField] private int rank;
        [SerializeField] private List<CombatTypes.AbilityCondition> conditions;
        [SerializeField] private AbilityAction action;
        [SerializeField] private string animationTrigger;

        public string Id => id;
        public int Cooldown => cooldown;
        public CombatTypes.CooldownType CooldownType => cooldownType;
        public int Rank => rank;
        public List<CombatTypes.AbilityCondition> Conditions => conditions;
        public AbilityAction Action => action;
        public string AnimationTrigger => animationTrigger;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) Debug.LogWarning($"AbilitySO {name}: ID is empty.");
            if (cooldown < 0)
            {
                Debug.LogWarning($"AbilitySO {id}: Cooldown must be >= 0.");
                cooldown = 0;
            }
            if (rank < 0 || rank > 3)
            {
                Debug.LogWarning($"AbilitySO {id}: Rank must be 0-3.");
                rank = Mathf.Clamp(rank, 0, 3);
            }
            if (action.NumberOfTargets < 1)
            {
                Debug.LogWarning($"AbilitySO {id}: NumberOfTargets must be >= 1.");
                action.NumberOfTargets = 1;
            }
            if (string.IsNullOrEmpty(action.EffectId))
            {
                Debug.LogWarning($"AbilitySO {id}: EffectId is empty.");
            }
            if (action.Defense == CombatTypes.DefenseCheck.Partial && action.PartialDefenseMultiplier <= 0)
            {
                Debug.LogWarning($"AbilitySO {id}: Partial Defense requires positive PartialDefenseMultiplier.");
                action.PartialDefenseMultiplier = 0.025f;
            }
            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                if (condition.IsPercentage && (condition.Threshold < 0 || condition.Threshold > 1))
                {
                    Debug.LogWarning($"AbilitySO {id}: Percentage Threshold must be 0-1 for condition {i}.");
                    condition.Threshold = Mathf.Clamp(condition.Threshold, 0f, 1f);
                    conditions[i] = condition;
                }
            }
            if (string.IsNullOrEmpty(animationTrigger))
            {
                Debug.LogWarning($"AbilitySO {id}: AnimationTrigger is empty.");
            }
        }

        public CombatTypes.TargetingRule GetTargetingRule()
        {
            return new CombatTypes.TargetingRule
            {
                Type = action.RuleType,
                Target = action.Target,
                MeleeOnly = action.Melee,
                MinPosition = action.Melee ? 1 : 0,
                MaxPosition = action.Melee ? 2 : 4
            };
        }

        public List<ICombatUnit> GetConditionFilteredTargets(CharacterStats user, PartyData party, List<ICombatUnit> allTargets)
        {
            List<ICombatUnit> basePool;
            if (Action.Target == CombatTypes.ConditionTarget.Ally)
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

            var filtered = new HashSet<ICombatUnit>(basePool);
            foreach (var cond in Conditions.Where(c => c.Target == Action.Target && c.TeamCondition == CombatTypes.TeamCondition.None))
            {
                var team = GetTeam(cond.TeamTarget, user, party, allTargets);
                var condFiltered = team.Where(t => MeetsTargetCriteria(t, cond) && MeetsCondition(t as CharacterStats, cond)).ToHashSet();
                filtered.IntersectWith(condFiltered);
                if (filtered.Count == 0) return new List<ICombatUnit>();
            }

            return filtered.ToList();
        }

        public bool EvaluateCondition(CombatTypes.AbilityCondition cond, CharacterStats unit, PartyData party, List<ICombatUnit> targets)
        {
            float value = 0f;
            bool statMet = false;
            bool countMet = true;

            if (cond.Target == CombatTypes.ConditionTarget.User)
            {
                statMet = MeetsCondition(unit, cond);
            }
            else
            {
                var team = GetTeam(cond.TeamTarget, unit, party, targets);
                var filteredTargets = team.Where(t => MeetsTargetCriteria(t, cond)).ToList();

                if (cond.MinTargetCount > 0 && filteredTargets.Count < cond.MinTargetCount)
                {
                    countMet = false;
                }
                if (cond.MaxTargetCount > 0 && filteredTargets.Count > cond.MaxTargetCount)
                {
                    countMet = false;
                }

                if (cond.TeamCondition == CombatTypes.TeamCondition.None)
                {
                    statMet = filteredTargets.Any(t => MeetsCondition(t as CharacterStats, cond));
                }
                else
                {
                    float totalValue = 0f;
                    int count = filteredTargets.Count;
                    foreach (var target in filteredTargets)
                    {
                        if (target is CharacterStats stats)
                        {
                            totalValue += GetStatValue(cond.Stat, stats);
                        }
                    }
                    value = cond.TeamCondition == CombatTypes.TeamCondition.AverageStat && count > 0 ? totalValue / count : totalValue;

                    if (cond.IsPercentage)
                    {
                        float maxValue = cond.Stat == CombatTypes.Stat.Health ? unit.MaxHealth : cond.Stat == CombatTypes.Stat.Morale ? unit.MaxMorale : 1f;
                        value = maxValue > 0 ? value / maxValue : 0f;
                    }

                    statMet = cond.Comparison switch
                    {
                        CombatTypes.Comparison.Greater => value > cond.Threshold,
                        CombatTypes.Comparison.Lesser => value < cond.Threshold,
                        CombatTypes.Comparison.Equal => Mathf.Approximately(value, cond.Threshold),
                        _ => false
                    };
                }
            }

            bool result = statMet && countMet;
            Debug.Log($"EvaluateCondition for {unit.Id}: Stat={cond.Stat}, Target={cond.Target}, Value={value}, Threshold={cond.Threshold}, StatMet={statMet}, CountMet={countMet}, Result={result}");
            return result;
        }

        private bool MeetsCondition(CharacterStats target, CombatTypes.AbilityCondition cond)
        {
            if (target == null) return false;
            float value = GetStatValue(cond.Stat, target);
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

        private float GetStatValue(CombatTypes.Stat stat, CharacterStats unit)
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
                _ => 0f
            };
        }

        private List<ICombatUnit> GetTeam(CombatTypes.TeamTarget team, CharacterStats unit, PartyData party, List<ICombatUnit> targets)
        {
            return team switch
            {
                CombatTypes.TeamTarget.Allies => unit.Type == CharacterType.Hero ? party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated).ToList() : targets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated).ToList(),
                CombatTypes.TeamTarget.Enemies => unit.Type == CharacterType.Hero ? targets.Where(t => !t.IsHero && t.Health > 0 && !t.HasRetreated).ToList() : party.HeroStats.Cast<ICombatUnit>().Where(h => h.Health > 0 && !h.HasRetreated).ToList(),
                CombatTypes.TeamTarget.Both => party.HeroStats.Cast<ICombatUnit>().Concat(targets.Where(t => !t.IsHero)).Where(u => u.Health > 0 && !u.HasRetreated).ToList(),
                _ => new List<ICombatUnit>()
            };
        }

        private bool MeetsTargetCriteria(ICombatUnit target, CombatTypes.AbilityCondition cond)
        {
            if (target.PartyPosition < cond.MinPosition || (cond.MaxPosition > 0 && target.PartyPosition > cond.MaxPosition)) return false;
            return true;
        }
    }
}