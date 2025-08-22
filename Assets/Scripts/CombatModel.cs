using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class CombatModel
    {
        public List<(ICombatUnit unit, GameObject go)> Units { get; private set; }
        public bool IsBattleActive { get; set; }
        public int RoundNumber { get; private set; }

        public event Action<string, Color> OnLogMessage; // Modified to include Color
        public event Action<ICombatUnit> OnUnitUpdated;
        public event Action<ICombatUnit, string> OnDamagePopup;
        public event Action OnBattleEnded;

        public CombatModel()
        {
            Units = new List<(ICombatUnit, GameObject)>();
            IsBattleActive = false;
            RoundNumber = 0;
        }

        public void InitializeUnits(List<HeroStats> heroStats, List<MonsterStats> monsterStats)
        {
            Units.Clear();
            foreach (var hero in heroStats)
            {
                if (hero.Health > 0) Units.Add((hero, null));
            }
            foreach (var monster in monsterStats)
            {
                if (monster.Health > 0) Units.Add((monster, null));
            }
        }

        public void IncrementRound()
        {
            RoundNumber++;
            OnLogMessage?.Invoke($"Round {RoundNumber} begins!", Color.white); // Default to white for round messages
        }

        public void LogMessage(string message, Color color)
        {
            OnLogMessage?.Invoke(message, color);
        }

        public void UpdateUnit(ICombatUnit unit, string damageMessage = null)
        {
            OnUnitUpdated?.Invoke(unit);
            if (damageMessage != null)
            {
                OnDamagePopup?.Invoke(unit, damageMessage);
            }
        }

        public void EndBattle()
        {
            IsBattleActive = false;
            OnBattleEnded?.Invoke();
        }
    }
}