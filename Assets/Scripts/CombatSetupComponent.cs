using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    public class CombatSetupComponent : MonoBehaviour
    {
        [SerializeField] public EventBusSO eventBus;
        [SerializeField] public UIConfig uiConfig;
        public List<string> AllCombatLogs = new List<string>();
        public List<(ICombatUnit unit, GameObject go, CharacterStats.DisplayStats displayStats)> Units = new List<(ICombatUnit, GameObject, CharacterStats.DisplayStats)>();
        public List<CharacterStats> HeroPositions = new List<CharacterStats>();
        public List<CharacterStats> MonsterPositions = new List<CharacterStats>();
        public List<UnitAttackState> UnitAttackStates = new List<UnitAttackState>();

        public void InitializeUnits(List<CharacterStats> heroStats, List<CharacterStats> monsterStats)
        {
            Units.Clear();
            HeroPositions.Clear();
            MonsterPositions.Clear();
            UnitAttackStates.Clear();
            AllCombatLogs.Clear();
            string initMessage = "Combat begins!";
            AllCombatLogs.Add(initMessage);
            eventBus.RaiseLogMessage(initMessage, uiConfig.TextColor);
            foreach (var hero in heroStats.Where(h => h.Type == CharacterType.Hero && h.Health > 0))
            {
                var stats = hero.GetDisplayStats();
                Units.Add((hero, null, stats));
                HeroPositions.Add(hero);
                UnitAttackStates.Add(new UnitAttackState { Unit = hero, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int value, int duration)>() });
                string heroMessage = $"{hero.Id} enters combat with {hero.Health}/{hero.MaxHealth} HP, {hero.Morale}/{hero.MaxMorale} Morale.";
                AllCombatLogs.Add(heroMessage);
                eventBus.RaiseLogMessage(heroMessage, uiConfig.TextColor);
            }
            foreach (var monster in monsterStats.Where(m => m.Type == CharacterType.Monster && m.Health > 0 && !m.HasRetreated))
            {
                var stats = monster.GetDisplayStats();
                Units.Add((monster, null, stats));
                MonsterPositions.Add(monster);
                UnitAttackStates.Add(new UnitAttackState { Unit = monster, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int value, int duration)>() });
                string monsterMessage = $"{monster.Id} enters combat with {monster.Health}/{monster.MaxHealth} HP.";
                AllCombatLogs.Add(monsterMessage);
                eventBus.RaiseLogMessage(monsterMessage, uiConfig.TextColor);
            }
            HeroPositions = HeroPositions.OrderBy(h => h.PartyPosition).ToList();
            MonsterPositions = MonsterPositions.OrderBy(m => m.PartyPosition).ToList();
            eventBus.RaiseCombatInitialized(Units);
        }

        public void UpdateUnit(ICombatUnit unit, string damageMessage = null)
        {
            if (unit == null) return;
            var unitEntry = Units.Find(u => u.unit == unit);
            if (unitEntry.unit != null)
            {
                Units.Remove(unitEntry);
                var newStats = unit.GetDisplayStats();
                Units.Add((unit, unitEntry.go, newStats));
                eventBus.RaiseUnitUpdated(unit, newStats);
                if (damageMessage != null)
                {
                    AllCombatLogs.Add(damageMessage);
                    eventBus.RaiseLogMessage(damageMessage, uiConfig.TextColor);
                    eventBus.RaiseUnitDamaged(unit, damageMessage);
                }
                if (unit is CharacterStats stats && (stats.Health <= 0 || stats.HasRetreated))
                {
                    if (stats.Type == CharacterType.Hero)
                        HeroPositions.Remove(stats);
                    else
                        MonsterPositions.Remove(stats);
                }
            }
        }

        public static UnitAttackState GetUnitAttackState(ICombatUnit unit)
        {
            return CombatSceneComponent.Instance.setupComponent.UnitAttackStates.Find(s => s.Unit == unit);
        }

        public static List<CharacterStats> GetMonsterUnits()
        {
            return CombatSceneComponent.Instance.setupComponent.MonsterPositions;
        }
    }
}