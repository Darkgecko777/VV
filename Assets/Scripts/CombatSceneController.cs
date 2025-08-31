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
        private bool isCombatActive;
        private int roundNumber;

        void Awake()
        {
            units.Clear();
            isCombatActive = false;
            roundNumber = 0;
        }

        void Start()
        {
            expeditionManager = ExpeditionManager.Instance;
            if (expeditionManager == null)
            {
                Debug.LogError("CombatSceneController: ExpeditionManager is null!");
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

        private IEnumerator RunCombat()
        {
            if (isCombatActive)
            {
                yield break;
            }

            var expeditionData = expeditionManager.GetExpedition();
            if (expeditionData == null || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogError("CombatSceneController: Invalid expedition data or node index!");
                EndCombat();
                yield break;
            }

            var heroStats = expeditionData.Party.GetHeroes();
            var monsterStats = expeditionData.NodeData[expeditionData.CurrentNodeIndex].Monsters;
            Debug.Log($"CombatSceneController: monsterStats type: {monsterStats.GetType()}, count: {monsterStats.Count}");

            if (heroStats == null || heroStats.Count == 0 || monsterStats == null || monsterStats.Count == 0)
            {
                Debug.LogError($"CombatSceneController: Invalid stats - Heroes: {(heroStats != null ? heroStats.Count : 0)}, Monsters: {(monsterStats != null ? monsterStats.Count : 0)}");
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

                // Check for retreats before actions
                foreach (var unit in unitList.ToList()) // ToList to avoid modification issues
                {
                    if (CheckRetreat(unit))
                    {
                        ProcessRetreat(unit);
                        yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
                    }
                }

                // Process actions for remaining units
                foreach (var unit in unitList)
                {
                    if (unit.Health <= 0 || unit.HasRetreated) continue;

                    var abilityId = unit.AbilityId;
                    AbilityData? ability = unit is CharacterStats charStats ? (charStats.Type == CharacterType.Hero ? AbilityDatabase.GetHeroAbility(abilityId) : AbilityDatabase.GetMonsterAbility(abilityId)) : null;
                    if (ability == null) continue;

                    var targets = unit is CharacterStats charStats2 && charStats2.Type == CharacterType.Hero
                        ? units.Select(u => u.unit).Where(u => u is CharacterStats cs && cs.Type == CharacterType.Monster && u.Health > 0 && !u.HasRetreated).ToList()
                        : units.Select(u => u.unit).Where(u => u is CharacterStats cs && cs.Type == CharacterType.Hero && u.Health > 0 && !u.HasRetreated).ToList();

                    var target = GetRandomAliveTarget(targets);
                    if (target == null)
                    {
                        if (NoActiveHeroes() || NoActiveMonsters())
                        {
                            EndCombat();
                            yield break;
                        }
                        continue;
                    }

                    eventBus.RaiseUnitAttacking(unit, target);
                    yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));

                    // Simplified damage calculation for prototype
                    int damage = Mathf.Max(1, unit.Attack - target.Defense);
                    target.Health -= damage;
                    UpdateUnit(target, $"{target.Id} takes {damage} damage!");

                    if (target.Health <= 0)
                    {
                        eventBus.RaiseUnitDied(target);
                    }

                    UpdateUnit(unit);
                    if (NoActiveHeroes() || NoActiveMonsters())
                    {
                        EndCombat();
                        yield break;
                    }

                    yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
                }

                if (roundNumber >= combatConfig.MaxRounds)
                {
                    EndCombat();
                    yield break;
                }

                IncrementRound();
            }
        }

        private void InitializeUnits(List<CharacterStats> heroStats, List<CharacterStats> monsterStats)
        {
            units.Clear();
            foreach (var hero in heroStats.Where(h => h.Type == CharacterType.Hero && h.Health > 0))
            {
                var stats = hero.GetDisplayStats();
                units.Add((hero, null, stats));
            }
            foreach (var monster in monsterStats.Where(m => m.Type == CharacterType.Monster && m.Health > 0))
            {
                var stats = monster.GetDisplayStats();
                units.Add((monster, null, stats));
            }
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
                    eventBus.RaiseUnitDamaged(unit, damageMessage);
                }
            }
        }

        private bool CheckRetreat(ICombatUnit unit)
        {
            return unit is CharacterStats stats && stats.Morale <= combatConfig.RetreatMoraleThreshold && !stats.HasRetreated;
        }

        private void ProcessRetreat(ICombatUnit unit)
        {
            if (unit == null || unit.HasRetreated) return;

            // Mark as retreated
            if (unit is CharacterStats stats)
            {
                stats.HasRetreated = true;

                // Apply +20 morale recovery for heroes
                if (stats.IsHero)
                {
                    stats.Morale = Mathf.Min(stats.Morale + 20, stats.MaxMorale);
                }

                // Log retreat
                LogMessage($"{stats.Id} fled!", uiConfig.TextColor);
                eventBus.RaiseUnitRetreated(unit);

                // Apply cascading morale penalty to teammates
                int penalty = 10; // To be moved to CombatConfig later
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
            return units.Count(u => u.displayStats.isHero && u.unit.Health > 0 && !u.unit.HasRetreated) == 0;
        }

        private bool NoActiveMonsters()
        {
            return units.Count(u => !u.displayStats.isHero && u.unit.Health > 0 && !u.unit.HasRetreated) == 0;
        }

        private bool ValidateReferences()
        {
            if (combatConfig == null || eventBus == null || visualConfig == null || uiConfig == null || combatCamera == null)
            {
                Debug.LogError($"CombatSceneController: Missing references! CombatConfig: {combatConfig != null}, EventBus: {eventBus != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, Camera: {combatCamera != null}");
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