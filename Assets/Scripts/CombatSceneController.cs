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
                Debug.LogError("CombatSceneController: Failed to find ExpeditionManager!");
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
                EndCombat();
                yield break;
            }

            var heroStats = expeditionData.Party.GetHeroes(); // Should return List<CharacterStats>
            var monsterStats = expeditionData.NodeData[expeditionData.CurrentNodeIndex].Monsters; // Now List<CharacterStats>

            if (heroStats == null || monsterStats == null || heroStats.Count == 0 || monsterStats.Count == 0)
            {
                EndCombat();
                yield break;
            }

            isCombatActive = true;
            InitializeUnits(heroStats, monsterStats);
            IncrementRound();
            while (isCombatActive)
            {
                if (CheckRetreat(units.Select(u => u.unit).ToList()))
                {
                    LogMessage("Party morale too low, retreating!", uiConfig.BogRotColor);
                    eventBus.RaiseRetreatTriggered();
                    EndCombat();
                    yield break;
                }

                var unitList = units.Select(u => u.unit).Where(u => u.Health > 0).OrderByDescending(u => u.Speed).ToList();
                if (unitList.Count == 0)
                {
                    EndCombat();
                    yield break;
                }

                foreach (var unit in unitList)
                {
                    var abilityId = unit.AbilityId;
                    AbilityData? ability = unit is CharacterStats charStats ? (charStats.Type == CharacterType.Hero ? AbilityDatabase.GetHeroAbility(abilityId) : AbilityDatabase.GetMonsterAbility(abilityId)) : null;
                    if (ability == null) continue;

                    var targets = unit is CharacterStats charStats2 && charStats2.Type == CharacterType.Hero
                        ? units.Select(u => u.unit).Where(u => u is CharacterStats cs && cs.Type == CharacterType.Monster && u.Health > 0).ToList()
                        : units.Select(u => u.unit).Where(u => u is CharacterStats cs && cs.Type == CharacterType.Hero && u.Health > 0).ToList();

                    var target = GetRandomAliveTarget(targets);
                    if (target == null) continue;

                    int damage = Mathf.Max(unit.Attack - target.Defense, 0);
                    bool killed = false;
                    if (ability.Value.Effect != null)
                    {
                        ability.Value.Effect(target, expeditionData.Party);
                    }

                    if (ability.Value.CanDodge && Random.Range(0, 100) < target.Evasion)
                    {
                        LogMessage($"{target.GetDisplayStats().name} dodges {unit.GetDisplayStats().name}!", uiConfig.TextColor);
                        continue;
                    }

                    target.Health = Mathf.Max(target.Health - damage, 0);
                    killed = target.Health <= 0;
                    UpdateUnit(target, damage.ToString());
                    if (killed)
                    {
                        LogMessage($"{unit.GetDisplayStats().name} kills {target.GetDisplayStats().name}!", uiConfig.BogRotColor);
                    }
                    else
                    {
                        LogMessage($"{unit.GetDisplayStats().name} hits {target.GetDisplayStats().name} for {damage} damage!", uiConfig.TextColor);
                    }

                    if (unit.Speed >= combatConfig.SpeedTwoAttacksThreshold)
                    {
                        var extraTarget = GetRandomAliveTarget(targets);
                        if (extraTarget != null)
                        {
                            if (ability.Value.CanDodge && Random.Range(0, 100) < extraTarget.Evasion)
                            {
                                LogMessage($"{extraTarget.GetDisplayStats().name} dodges {unit.GetDisplayStats().name}'s extra attack!", uiConfig.TextColor);
                                continue;
                            }
                            damage = Mathf.Max(unit.Attack - extraTarget.Defense, 0);
                            extraTarget.Health = Mathf.Max(extraTarget.Health - damage, 0);
                            killed = extraTarget.Health <= 0;
                            UpdateUnit(extraTarget, damage.ToString());
                            if (killed)
                            {
                                LogMessage($"{unit.GetDisplayStats().name} kills {extraTarget.GetDisplayStats().name} with extra attack!", uiConfig.BogRotColor);
                            }
                            else
                            {
                                LogMessage($"{unit.GetDisplayStats().name} hits {extraTarget.GetDisplayStats().name} for {damage} damage with extra attack!", uiConfig.TextColor);
                            }
                        }
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
            foreach (var hero in heroStats.Where(h => h.Type == CharacterType.Hero))
            {
                if (hero.Health > 0)
                {
                    var stats = hero.GetDisplayStats();
                    Debug.Log($"Initializing Hero {stats.name}: Health={stats.health}/{stats.maxHealth}, Morale={stats.morale}/{stats.maxMorale}");
                    units.Add((hero, null, stats));
                }
            }
            foreach (var monster in monsterStats.Where(m => m.Type == CharacterType.Monster))
            {
                if (monster.Health > 0)
                {
                    var stats = monster.GetDisplayStats();
                    Debug.Log($"Initializing Monster {stats.name}: Health={stats.health}/{stats.maxHealth}");
                    units.Add((monster, null, stats));
                }
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
            var unitEntry = units.Find(u => u.unit == unit);
            if (unitEntry.unit != null)
            {
                units.Remove(unitEntry);
                var newStats = unit.GetDisplayStats();
                units.Add((unit, unitEntry.go, newStats));
                eventBus.RaiseUnitUpdated(unit, newStats);
                if (damageMessage != null)
                {
                    eventBus.RaiseDamagePopup(unit, damageMessage);
                }
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
            var aliveTargets = targets.Where(t => t.Health > 0).ToList();
            return aliveTargets.Count > 0 ? aliveTargets[Random.Range(0, aliveTargets.Count)] : null;
        }

        private bool CheckRetreat(List<ICombatUnit> characters)
        {
            return characters.OfType<CharacterStats>().Any(h => h.Type == CharacterType.Hero && h.Morale <= combatConfig.RetreatMoraleThreshold);
        }

        public void SetCombatSpeed(float speed)
        {
            if (combatConfig != null)
            {
                combatConfig.CombatSpeed = Mathf.Clamp(speed, combatConfig.MinCombatSpeed, combatConfig.MaxCombatSpeed);
            }
        }

        private bool ValidateReferences()
        {
            if (combatConfig == null || eventBus == null || visualConfig == null || uiConfig == null || combatCamera == null)
            {
                Debug.LogError($"CombatSceneController: Missing references! CombatConfig: {combatConfig != null}, EventBus: {eventBus != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, CombatCamera: {combatCamera != null}");
                return false;
            }
            return true;
        }
    }
}