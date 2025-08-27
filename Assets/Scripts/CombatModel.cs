using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class CombatModel
    {
        public List<(ICombatUnit unit, GameObject go, DisplayStats displayStats)> Units { get; private set; }
        public bool IsBattleActive { get; set; }
        public int RoundNumber { get; private set; }

        public event Action<string, Color> OnLogMessage;
        public event Action<ICombatUnit, DisplayStats> OnUnitUpdated;
        public event Action<ICombatUnit, string> OnDamagePopup;
        public event Action OnBattleEnded;

        public CombatModel()
        {
            Units = new List<(ICombatUnit, GameObject, DisplayStats)>();
            IsBattleActive = false;
            RoundNumber = 0;
        }

        public void InitializeUnits(List<HeroStats> heroStats, List<MonsterStats> monsterStats)
        {
            Units.Clear();
            foreach (var hero in heroStats)
            {
                if (hero.Health > 0)
                {
                    Units.Add((hero, null, hero.GetDisplayStats()));
                }
            }
            foreach (var monster in monsterStats)
            {
                if (monster.Health > 0)
                {
                    Units.Add((monster, null, monster.GetDisplayStats()));
                }
            }
        }

        public void IncrementRound()
        {
            RoundNumber++;
            OnLogMessage?.Invoke($"Round {RoundNumber} begins!", Color.white);
        }

        public void LogMessage(string message, Color color)
        {
            OnLogMessage?.Invoke(message, color);
        }

        public void UpdateUnit(ICombatUnit unit, string damageMessage = null)
        {
            var unitEntry = Units.Find(u => u.unit == unit);
            if (unitEntry.unit != null)
            {
                Units.Remove(unitEntry);
                Units.Add((unit, unitEntry.go, unit.GetDisplayStats())); // Refresh with new DisplayStats
                OnUnitUpdated?.Invoke(unit, unit.GetDisplayStats());
                if (damageMessage != null)
                {
                    OnDamagePopup?.Invoke(unit, damageMessage);
                }
            }
        }

        public void EndBattle()
        {
            IsBattleActive = false;
            OnBattleEnded?.Invoke();
        }
    }
}