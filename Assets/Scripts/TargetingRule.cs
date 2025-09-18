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
                string noTargetMessage = $"No valid targets for {user.Id}'s attack.";
                CombatSceneComponent.Instance.AllCombatLogs.Add(noTargetMessage);
                CombatSceneComponent.Instance.EventBus.RaiseLogMessage(noTargetMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
                return new List<ICombatUnit>();
            }

            var orderedHeroes = CombatSceneComponent.Instance.heroPositions
                .Where(h => h.Health > 0 && !h.HasRetreated)
                .OrderBy(h => h.PartyPosition)
                .Select((h, i) => new { Unit = (ICombatUnit)h, CombatPosition = i + 1 })
                .ToList();
            var orderedMonsters = CombatSceneComponent.Instance.monsterPositions
                .Where(m => m.Health > 0 && !m.HasRetreated)
                .OrderBy(m => m.PartyPosition)
                .Select((m, i) => new { Unit = (ICombatUnit)m, CombatPosition = i + 1 })
                .ToList();

            if (isMelee || MeleeOnly)
            {
                targetPool = targetPool.Where(t => (user.Type == CharacterType.Hero
                    ? orderedMonsters.FirstOrDefault(m => m.Unit == t)?.CombatPosition
                    : orderedHeroes.FirstOrDefault(h => h.Unit == t)?.CombatPosition) <= 2).ToList();
                if (targetPool.Count == 0)
                {
                    string noTargetMessage = $"No frontline targets (CombatPosition 1-2) for {user.Id}'s melee attack.";
                    CombatSceneComponent.Instance.AllCombatLogs.Add(noTargetMessage);
                    CombatSceneComponent.Instance.EventBus.RaiseLogMessage(noTargetMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
                    return new List<ICombatUnit>();
                }
            }

            if (MustBeInfected)
                targetPool = targetPool.Where(t => (t as CharacterStats).IsInfected).ToList();
            if (MustNotBeInfected)
                targetPool = targetPool.Where(t => !(t as CharacterStats).IsInfected).ToList();

            if (targetPool.Count == 0)
                return new List<ICombatUnit>();

            switch (Type)
            {
                case RuleType.LowestHealth:
                    targetPool = targetPool.OrderBy(t => (t as CharacterStats).Health).ToList();
                    break;
                case RuleType.HighestHealth:
                    targetPool = targetPool.OrderByDescending(t => (t as CharacterStats).Health).ToList();
                    break;
                case RuleType.LowestMorale:
                    targetPool = targetPool.OrderBy(t => (t as CharacterStats).Morale).ToList();
                    break;
                case RuleType.HighestMorale:
                    targetPool = targetPool.OrderByDescending(t => (t as CharacterStats).Morale).ToList();
                    break;
                case RuleType.LowestAttack:
                    targetPool = targetPool.OrderBy(t => (t as CharacterStats).Attack).ToList();
                    break;
                case RuleType.HighestAttack:
                    targetPool = targetPool.OrderByDescending(t => (t as CharacterStats).Attack).ToList();
                    break;
                case RuleType.AllAllies:
                    if (Target == ConditionTarget.Ally)
                        break;
                    targetPool = new List<ICombatUnit>();
                    break;
                default:
                    targetPool = targetPool.OrderBy(t => UnityEngine.Random.value).ToList();
                    break;
            }

            int maxTargets = isMelee ? Mathf.Min(2, targetPool.Count) : Mathf.Min(4, targetPool.Count);
            if (Type == RuleType.AllAllies && Target == ConditionTarget.Ally)
                maxTargets = targetPool.Count;

            return targetPool.Take(maxTargets).ToList();
        }
    }
}