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
        private readonly EventBusSO eventBus;

        public CombatModel(EventBusSO bus)
        {
            eventBus = bus ?? throw new ArgumentNullException(nameof(bus), "EventBusSO cannot be null");
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
                    var stats = hero.GetDisplayStats();
                    Debug.Log($"Initializing Hero {stats.name}: Health={stats.health}/{stats.maxHealth}, Morale={stats.morale}/{stats.maxMorale}");
                    Units.Add((hero, null, stats));
                }
            }
            foreach (var monster in monsterStats)
            {
                if (monster.Health > 0)
                {
                    var stats = monster.GetDisplayStats();
                    Debug.Log($"Initializing Monster {stats.name}: Health={stats.health}/{stats.maxHealth}");
                    Units.Add((monster, null, stats));
                }
            }
            eventBus.RaiseBattleInitialized(Units);
        }

        public void IncrementRound()
        {
            RoundNumber++;
            eventBus.RaiseLogMessage($"Round {RoundNumber} begins!", Color.white);
        }

        public void LogMessage(string message, Color color)
        {
            eventBus.RaiseLogMessage(message, color);
        }

        public void UpdateUnit(ICombatUnit unit, string damageMessage = null)
        {
            var unitEntry = Units.Find(u => u.unit == unit);
            if (unitEntry.unit != null)
            {
                Units.Remove(unitEntry);
                var newStats = unit.GetDisplayStats();
                Units.Add((unit, unitEntry.go, newStats));
                eventBus.RaiseUnitUpdated(unit, newStats);
                if (damageMessage != null)
                {
                    eventBus.RaiseDamagePopup(unit, damageMessage);
                }
            }
        }

        public void EndBattle()
        {
            IsBattleActive = false;
            eventBus.RaiseBattleEnded();
        }
    }
}