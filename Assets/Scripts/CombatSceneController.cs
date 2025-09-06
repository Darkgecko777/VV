using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    public class CombatSceneController : MonoBehaviour
    {
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private Camera combatCamera;

        private ExpeditionManager expeditionManager;
        private bool isEndingCombat;
        private List<(ICombatUnit unit, GameObject go, CharacterStats.DisplayStats displayStats)> units = new List<(ICombatUnit, GameObject, CharacterStats.DisplayStats)>();
        private List<CharacterStats> heroPositions = new List<CharacterStats>();
        private List<CharacterStats> monsterPositions = new List<CharacterStats>();
        private bool isCombatActive;
        private int roundNumber;
        private List<UnitAttackState> unitAttackStates = new List<UnitAttackState>();

        private class UnitAttackState
        {
            public ICombatUnit Unit { get; set; }
            public int AttacksThisRound { get; set; }
            public int RoundCounter { get; set; }
        }

        void Awake()
        {
            units.Clear();
            heroPositions.Clear();
            monsterPositions.Clear();
            unitAttackStates.Clear();
            isCombatActive = false;
            roundNumber = 0;
        }

        void Start()
        {
            expeditionManager = ExpeditionManager.Instance;
            if (expeditionManager == null)
            {
                Debug.LogError("CombatSceneController: ExpeditionManager not found.");
                return;
            }
            if (!ValidateReferences()) return;

            eventBus.OnCombatEnded += EndCombat;
            StartCoroutine(RunCombat());
        }

        void OnDestroy()
        {
            eventBus.OnCombatEnded -= EndCombat;
        }

        private string SelectAbility(CharacterStats unit, PartyData partyData, List<ICombatUnit> targets)
        {
            var data = unit.Type == CharacterType.Hero
                ? CharacterLibrary.GetHeroData(unit.Id)
                : CharacterLibrary.GetMonsterData(unit.Id);

            foreach (var abilityId in data.AbilityIds)
            {
                var ability = unit.Type == CharacterType.Hero
                    ? AbilityDatabase.GetHeroAbility(abilityId)
                    : AbilityDatabase.GetMonsterAbility(abilityId);

                if (ability.HasValue && ability.Value.UseCondition(unit, partyData, targets))
                {
                    return abilityId;
                }
            }

            return "BasicAttack";
        }

        private bool CanAttackThisRound(ICombatUnit unit, UnitAttackState state)
        {
            if (unit is not CharacterStats stats) return false;
            if (stats.Speed >= combatConfig.SpeedTwoAttacksThreshold)
                return state.AttacksThisRound < 1;
            else if (stats.Speed >= combatConfig.SpeedThreePerTwoThreshold)
                return state.RoundCounter % 2 == 1 ? state.AttacksThisRound < 2 : state.AttacksThisRound < 1;
            else if (stats.Speed >= combatConfig.SpeedOneAttackThreshold)
                return state.AttacksThisRound < 1;
            else if (stats.Speed >= combatConfig.SpeedOnePerTwoThreshold)
                return state.RoundCounter % 2 == 1 && state.AttacksThisRound < 1;
            return false;
        }

        private IEnumerator ProcessAttack(ICombatUnit unit, PartyData partyData, List<ICombatUnit> unitList)
        {
            if (unit is not CharacterStats stats) yield break;
            var targets = stats.Type == CharacterType.Hero
                ? monsterPositions.Cast<ICombatUnit>().ToList()
                : heroPositions.Cast<ICombatUnit>().ToList();
            var abilityId = SelectAbility(stats, partyData, targets);
            AbilityData? ability = stats.Type == CharacterType.Hero
                ? AbilityDatabase.GetHeroAbility(abilityId)
                : AbilityDatabase.GetMonsterAbility(abilityId);
            if (ability == null) yield break;
            if (ability.Value.IsMelee)
            {
                targets = targets.Take(2).ToList();
            }
            var target = GetRandomAliveTarget(targets);
            if (target == null)
            {
                if (NoActiveHeroes() || NoActiveMonsters())
                {
                    EndCombat();
                    yield break;
                }
                yield break;
            }
            eventBus.RaiseUnitAttacking(unit, target, abilityId);
            yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
            string abilityLog = ability.Value.Effect?.Invoke(target, partyData) ?? "";
            if (!string.IsNullOrEmpty(abilityLog))
            {
                eventBus.RaiseLogMessage(abilityLog, Color.white);
            }
            if (abilityId == "BasicAttack" || abilityId.EndsWith("Claw") || abilityId.EndsWith("Strike") || abilityId.EndsWith("Slash") || abilityId.EndsWith("Bite"))
            {
                int damage = Mathf.Max(1, unit.Attack - target.Defense);
                target.Health -= damage;
                UpdateUnit(target, $"{target.Id} takes {damage} damage!");
            }
            if (target.Health <= 0)
            {
                eventBus.RaiseUnitDied(target);
            }
            UpdateUnit(unit);
        }

        private IEnumerator RunCombat()
        {
            if (isCombatActive)
            {
                yield break;
            }

            var expeditionData = expeditionManager.GetExpedition();
            if (expeditionData == null || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                EndCombat();
                yield break;
            }

            var heroStats = expeditionData.Party.GetHeroes();
            var monsterStats = expeditionData.NodeData[expeditionData.CurrentNodeIndex].Monsters;

            if (heroStats == null || heroStats.Count == 0 || monsterStats == null || monsterStats.Count == 0)
            {
                EndCombat();
                yield break;
            }

            isCombatActive = true;
            InitializeUnits(heroStats, monsterStats);
            IncrementRound();
            while (isCombatActive)
            {
                var unitList = units.Select(u => u.unit).Where(u => u.Health > 0 && !u.HasRetreated).OrderByDescending(u => u.Speed).ToList();
                if (unitList.Count == 0 || NoActiveHeroes() || NoActiveMonsters())
                {
                    EndCombat();
                    yield break;
                }

                foreach (var unit in unitList.ToList())
                {
                    var state = unitAttackStates.Find(s => s.Unit == unit);
                    if (state == null)
                    {
                        state = new UnitAttackState { Unit = unit, AttacksThisRound = 0, RoundCounter = 0 };
                        unitAttackStates.Add(state);
                    }
                    state.AttacksThisRound = 0;
                    state.RoundCounter++;
                }

                foreach (var unit in unitList.ToList())
                {
                    var state = unitAttackStates.Find(s => s.Unit == unit);
                    if (state == null || unit.Health <= 0 || unit.HasRetreated) continue;

                    if (unit is CharacterStats stats && stats.Type == CharacterType.Hero && CheckRetreat(unit))
                    {
                        ProcessRetreat(unit);
                        yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
                        continue;
                    }

                    if (!CanAttackThisRound(unit, state)) continue;

                    state.AttacksThisRound++;
                    yield return ProcessAttack(unit, expeditionManager.GetExpedition().Party, unitList);

                    if (NoActiveHeroes() || NoActiveMonsters())
                    {
                        EndCombat();
                        yield break;
                    }
                }

                foreach (var unit in unitList.ToList())
                {
                    var state = unitAttackStates.Find(s => s.Unit == unit);
                    if (state == null || unit.Health <= 0 || unit.HasRetreated) continue;

                    if (unit is CharacterStats stats && stats.Type == CharacterType.Hero && stats.Speed >= combatConfig.SpeedTwoAttacksThreshold && state.AttacksThisRound < 2)
                    {
                        state.AttacksThisRound++;
                        yield return ProcessAttack(unit, expeditionManager.GetExpedition().Party, unitList);
                    }

                    if (NoActiveHeroes() || NoActiveMonsters())
                    {
                        EndCombat();
                        yield break;
                    }
                }

                IncrementRound();
            }
        }

        private void InitializeUnits(List<CharacterStats> heroStats, List<CharacterStats> monsterStats)
        {
            units.Clear();
            heroPositions.Clear();
            monsterPositions.Clear();
            unitAttackStates.Clear();

            foreach (var hero in heroStats.Where(h => h.Type == CharacterType.Hero && h.Health > 0 && !h.HasRetreated))
            {
                var stats = hero.GetDisplayStats();
                units.Add((hero, null, stats));
                heroPositions.Add(hero);
                unitAttackStates.Add(new UnitAttackState { Unit = hero, AttacksThisRound = 0, RoundCounter = 0 });
            }
            foreach (var monster in monsterStats.Where(m => m.Type == CharacterType.Monster && m.Health > 0 && !m.HasRetreated))
            {
                var stats = monster.GetDisplayStats();
                units.Add((monster, null, stats));
                monsterPositions.Add(monster);
                unitAttackStates.Add(new UnitAttackState { Unit = monster, AttacksThisRound = 0, RoundCounter = 0 });
            }

            heroPositions = heroPositions.OrderBy(h => h.PartyPosition).ToList();
            monsterPositions = monsterPositions.OrderBy(m => m.PartyPosition).ToList();

            eventBus.RaiseCombatInitialized(units);
        }

        private void IncrementRound()
        {
            roundNumber++;
            eventBus.RaiseLogMessage($"Round {roundNumber} begins!", Color.white);
        }

        private void LogMessage(string message, Color color)
        {
            eventBus.RaiseLogMessage(message, color);
        }

        private void UpdateUnit(ICombatUnit unit, string damageMessage = null)
        {
            if (unit == null) return;
            var unitEntry = units.Find(u => u.unit == unit);
            if (unitEntry.unit != null)
            {
                units.Remove(unitEntry);
                var newStats = unit.GetDisplayStats();
                units.Add((unit, unitEntry.go, newStats));
                eventBus.RaiseUnitUpdated(unit, newStats);
                if (damageMessage != null)
                {
                    eventBus.RaiseLogMessage(damageMessage, Color.white);
                    eventBus.RaiseUnitDamaged(unit, damageMessage);
                }

                if (unit is CharacterStats stats && (stats.Health <= 0 || stats.HasRetreated))
                {
                    if (stats.Type == CharacterType.Hero)
                    {
                        heroPositions.Remove(stats);
                    }
                    else
                    {
                        monsterPositions.Remove(stats);
                    }
                }
            }
        }

        private bool CheckRetreat(ICombatUnit unit)
        {
            return unit is CharacterStats stats && stats.Type == CharacterType.Hero && stats.Morale <= combatConfig.RetreatMoraleThreshold && !stats.HasRetreated;
        }

        private void ProcessRetreat(ICombatUnit unit)
        {
            if (unit == null || unit.HasRetreated) return;

            if (unit is CharacterStats stats && stats.Type == CharacterType.Hero)
            {
                stats.HasRetreated = true;
                stats.Morale = Mathf.Min(stats.Morale + 20, stats.MaxMorale);
                LogMessage($"{stats.Id} fled!", uiConfig.TextColor);
                eventBus.RaiseUnitRetreated(unit);

                int penalty = 10;
                var teammates = units
                    .Select(u => u.unit)
                    .Where(u => u is CharacterStats cs && cs.Type == stats.Type && u.Health > 0 && !u.HasRetreated && u != unit)
                    .ToList();
                foreach (var teammate in teammates)
                {
                    teammate.Morale = Mathf.Max(0, teammate.Morale - penalty);
                    UpdateUnit(teammate, $"{teammate.Id}'s morale drops by {penalty} due to {stats.Id}'s retreat!");
                }

                UpdateUnit(unit);
            }
        }

        private void EndCombat()
        {
            if (isEndingCombat) return;
            isEndingCombat = true;

            isCombatActive = false;
            eventBus.RaiseCombatEnded();
            expeditionManager.SaveProgress();
            bool partyDead = expeditionManager.GetExpedition().Party.CheckDeadStatus().Count == 0;
            if (!partyDead)
            {
                var expedition = expeditionManager.GetExpedition();
                if (expedition.CurrentNodeIndex < expedition.NodeData.Count)
                {
                    expedition.NodeData[expedition.CurrentNodeIndex].Completed = true;
                }
            }
            if (partyDead)
            {
                expeditionManager.EndExpedition();
            }
            else
            {
                expeditionManager.TransitionToExpeditionScene();
            }

            isEndingCombat = false;
        }

        private ICombatUnit GetRandomAliveTarget(List<ICombatUnit> targets)
        {
            var aliveTargets = targets.Where(t => t.Health > 0 && !t.HasRetreated).ToList();
            return aliveTargets.Count > 0 ? aliveTargets[Random.Range(0, aliveTargets.Count)] : null;
        }

        private bool NoActiveHeroes()
        {
            return heroPositions.Count == 0;
        }

        private bool NoActiveMonsters()
        {
            return monsterPositions.Count == 0;
        }

        private bool ValidateReferences()
        {
            if (combatConfig == null || eventBus == null || visualConfig == null || uiConfig == null || combatCamera == null)
            {
                Debug.LogError("CombatSceneController: Missing required reference(s). Please assign in the Inspector.");
                return false;
            }
            return true;
        }

        public void SetCombatSpeed(float speed)
        {
            if (combatConfig != null)
            {
                combatConfig.CombatSpeed = Mathf.Clamp(speed, combatConfig.MinCombatSpeed, combatConfig.MaxCombatSpeed);
            }
        }
    }
}