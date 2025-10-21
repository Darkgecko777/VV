using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "StatEffect", menuName = "VirulentVentures/Effects/StatEffect")]
    public class StatEffectSO : EffectSO
    {
        [SerializeField] private GameTypes.TargetStat targetStat = GameTypes.TargetStat.Speed;
        [SerializeField] private int amount = 0;
        [SerializeField] private bool targetSelf = false; // New field to target user
        [SerializeField] private int duration = -1; // New field for duration (default: combat-end)

        public GameTypes.TargetStat TargetStat => targetStat;
        public int Amount => amount;
        public bool TargetSelf => targetSelf;
        public int Duration => duration;

        public override (TransmissionVector? changedVector, float delta) Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            bool applied = false;
            float totalDelta = 0f;
            TransmissionVector? vector = TransmissionVector.Buff;

            if (TargetSelf)
            {
                // Apply to user
                if (user == null || user.Health <= 0 || user.HasRetreated) return (null, 0f);

                int currentValue = GetStatValue(user, targetStat);
                int newValue = Mathf.Max(0, currentValue + Amount);
                int change = newValue - currentValue;

                if (change != 0)
                {
                    var userState = combatScene.GetUnitAttackState(user);
                    if (userState != null)
                    {
                        string statKey = targetStat.ToString().ToLower();
                        userState.TempStats[statKey] = (Amount, Duration); // Use configurable duration
                    }

                    SetStatValue(user, targetStat, newValue);
                    totalDelta += change;

                    string action = change > 0 ? "increases" : "reduces";
                    string statName = targetStat.ToString().ToLower();
                    string colorCode = change > 0 ? "#00FF00" : "#FF0000";
                    string changeMessage = $"{user.Id} {action} their own {statName} by {Mathf.Abs(change)} with {abilityId} <color={colorCode}>[{Amount:+#;-#} {statName}]</color>";
                    combatLogs.Add(changeMessage);
                    eventBus.RaiseLogMessage(changeMessage, change > 0 ? Color.green : Color.red);
                    eventBus.RaiseUnitUpdated(user, user.GetDisplayStats());
                    updateUnitCallback(user);
                    applied = true;
                }
            }
            else
            {
                // Apply to targets (existing behavior)
                foreach (var target in targets.ToList())
                {
                    var targetStats = target as CharacterStats;
                    if (targetStats == null || targetStats.Health <= 0 || targetStats.HasRetreated) continue;

                    int currentValue = GetStatValue(targetStats, targetStat);
                    int newValue = Mathf.Max(0, currentValue + Amount);
                    int change = newValue - currentValue;

                    if (change != 0)
                    {
                        var targetState = combatScene.GetUnitAttackState(target);
                        if (targetState != null)
                        {
                            string statKey = targetStat.ToString().ToLower();
                            targetState.TempStats[statKey] = (Amount, Duration); // Use configurable duration
                        }

                        SetStatValue(targetStats, targetStat, newValue);
                        totalDelta += change;

                        string action = change > 0 ? "increases" : "reduces";
                        string statName = targetStat.ToString().ToLower();
                        string colorCode = change > 0 ? "#00FF00" : "#FF0000";
                        string changeMessage = $"{user.Id} {action} {targetStats.Id}'s {statName} by {Mathf.Abs(change)} with {abilityId} <color={colorCode}>[{Amount:+#;-#} {statName}]</color>";
                        combatLogs.Add(changeMessage);
                        eventBus.RaiseLogMessage(changeMessage, change > 0 ? Color.green : Color.red);
                        eventBus.RaiseUnitUpdated(target, targetStats.GetDisplayStats());
                        updateUnitCallback(target);
                        applied = true;
                    }
                }
            }

            return applied ? (vector, totalDelta) : (null, 0f);
        }

        private int GetStatValue(CharacterStats stats, GameTypes.TargetStat stat)
        {
            return stat switch
            {
                GameTypes.TargetStat.Health => stats.Health,
                GameTypes.TargetStat.Morale => stats.Morale,
                GameTypes.TargetStat.Speed => stats.Speed,
                GameTypes.TargetStat.Attack => stats.Attack,
                GameTypes.TargetStat.Defense => stats.Defense,
                GameTypes.TargetStat.Evasion => stats.Evasion,
                GameTypes.TargetStat.Immunity => stats.Immunity,
                _ => 0
            };
        }

        private void SetStatValue(CharacterStats stats, GameTypes.TargetStat stat, int value)
        {
            switch (stat)
            {
                case GameTypes.TargetStat.Health:
                    stats.Health = value;
                    break;
                case GameTypes.TargetStat.Morale:
                    stats.Morale = value;
                    break;
                case GameTypes.TargetStat.Speed:
                    stats.Speed = value;
                    break;
                case GameTypes.TargetStat.Attack:
                    stats.Attack = value;
                    break;
                case GameTypes.TargetStat.Defense:
                    stats.Defense = value;
                    break;
                case GameTypes.TargetStat.Evasion:
                    stats.Evasion = value;
                    break;
                case GameTypes.TargetStat.Immunity:
                    stats.Immunity = value;
                    break;
            }
        }
    }
}