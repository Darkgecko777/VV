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

        public GameTypes.TargetStat TargetStat => targetStat;
        public int Amount => amount;

        public override (TransmissionVector? changedVector, float delta) Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene)
        {
            bool applied = false;
            float totalDelta = 0f;
            TransmissionVector? vector = TransmissionVector.Buff;

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
                        targetState.TempStats[statKey] = (Amount, -1); // -1 indicates combat-end duration
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