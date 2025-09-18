using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public struct TargetingRule
    {
        public enum RuleType
        {
            Random,
            LowestHealth,
            HighestHealth,
            LowestMorale,
            HighestMorale,
            LowestAttack,
            HighestAttack,
            AllAllies
        }

        public RuleType Type;
        public ConditionTarget Target;
        public bool MustBeInfected;
        public bool MustNotBeInfected;
        public bool MeleeOnly;

        public void Validate()
        {
            if (MustBeInfected && MustNotBeInfected)
            {
                Debug.LogWarning("TargetingRule: MustBeInfected and MustNotBeInfected cannot both be true.");
                MustBeInfected = false;
                MustNotBeInfected = false;
            }
            if (Type == RuleType.AllAllies && Target != ConditionTarget.Ally)
            {
                Debug.LogWarning("TargetingRule: AllAllies rule requires Target = Ally.");
                Target = ConditionTarget.Ally;
            }
        }

        public List<ICombatUnit> SelectTargets(CharacterStats user, List<ICombatUnit> targetPool, PartyData partyData, bool isMelee)
        {
            if (targetPool == null || targetPool.Count == 0)
            {
                Debug.LogWarning($"TargetingRule: Empty targetPool for {user.Id}. Returning empty list.");
                return new List<ICombatUnit>();
            }

            var orderedHeroes = CombatSceneComponent.Instance.setupComponent.HeroPositions
                .Where(h => h.Health > 0 && !h.HasRetreated)
                .OrderBy(h => h.PartyPosition)
                .Select((h, i) => new { Unit = (ICombatUnit)h, CombatPosition = i + 1 })
                .ToList();
            var orderedMonsters = CombatSceneComponent.Instance.setupComponent.MonsterPositions
                .Where(m => m.Health > 0 && !m.HasRetreated)
                .OrderBy(m => m.PartyPosition)
                .Select((m, i) => new { Unit = (ICombatUnit)m, CombatPosition = i + 1 })
                .ToList();

            if (isMelee || MeleeOnly)
            {
                targetPool = targetPool.Where(t =>
                {
                    var pos = user.Type == CharacterType.Hero
                        ? orderedMonsters.FirstOrDefault(m => m.Unit == t)?.CombatPosition
                        : orderedHeroes.FirstOrDefault(h => h.Unit == t)?.CombatPosition;
                    return pos.HasValue && pos.Value <= 2;
                }).ToList();
                if (targetPool.Count == 0)
                {
                    Debug.LogWarning($"TargetingRule: No frontline targets for {user.Id}'s melee attack.");
                    return new List<ICombatUnit>();
                }
            }

            if (MustBeInfected)
                targetPool = targetPool.Where(t => (t as CharacterStats)?.IsInfected == true).ToList();
            if (MustNotBeInfected)
                targetPool = targetPool.Where(t => (t as CharacterStats)?.IsInfected == false).ToList();

            if (targetPool.Count == 0)
            {
                Debug.LogWarning($"TargetingRule: No targets after infection filter for {user.Id}.");
                return new List<ICombatUnit>();
            }

            switch (Type)
            {
                case RuleType.LowestHealth:
                    targetPool = targetPool.OrderBy(t => (t as CharacterStats)?.Health ?? int.MaxValue).ToList();
                    break;
                case RuleType.HighestHealth:
                    targetPool = targetPool.OrderByDescending(t => (t as CharacterStats)?.Health ?? 0).ToList();
                    break;
                case RuleType.LowestMorale:
                    targetPool = targetPool.OrderBy(t => (t as CharacterStats)?.Morale ?? 0).ToList();
                    break;
                case RuleType.HighestMorale:
                    targetPool = targetPool.OrderByDescending(t => (t as CharacterStats)?.Morale ?? 0).ToList();
                    break;
                case RuleType.LowestAttack:
                    targetPool = targetPool.OrderBy(t => (t as CharacterStats)?.Attack ?? 0).ToList();
                    break;
                case RuleType.HighestAttack:
                    targetPool = targetPool.OrderByDescending(t => (t as CharacterStats)?.Attack ?? 0).ToList();
                    break;
                case RuleType.AllAllies:
                    if (Target != ConditionTarget.Ally)
                        targetPool = new List<ICombatUnit>();
                    break;
                default:
                    targetPool = targetPool.OrderBy(t => UnityEngine.Random.value).ToList();
                    break;
            }

            int maxTargets = isMelee ? Mathf.Min(2, targetPool.Count) : Mathf.Min(4, targetPool.Count);
            if (Type == RuleType.AllAllies && Target == ConditionTarget.Ally)
                maxTargets = targetPool.Count;

            var selected = targetPool.Take(maxTargets).ToList();
            Debug.Log($"TargetingRule: Selected {selected.Count} targets for {user.Id} from pool of {targetPool.Count}.");
            return selected;
        }
    }
}