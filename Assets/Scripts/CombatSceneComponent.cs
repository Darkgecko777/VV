using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    public class CombatSceneComponent : MonoBehaviour
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
        private bool isPaused;
        private int roundNumber;
        private List<UnitAttackState> unitAttackStates = new List<UnitAttackState>();
        private List<string> allCombatLogs = new List<string>();
        public static CombatSceneComponent Instance { get; private set; }
        public bool IsPaused => isPaused;

        void Awake()
        {
            Instance = this;
            units.Clear();
            heroPositions.Clear();
            monsterPositions.Clear();
            unitAttackStates.Clear();
            isCombatActive = false;
            isPaused = false;
            roundNumber = 0;
            allCombatLogs.Clear();
        }

        void Start()
        {
            expeditionManager = ExpeditionManager.Instance;
            if (expeditionManager == null)
            {
                Debug.LogError("CombatSceneComponent: ExpeditionManager not found.");
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

        public static UnitAttackState GetUnitAttackState(ICombatUnit unit)
        {
            return Instance.unitAttackStates.Find(s => s.Unit == unit);
        }

        public static List<CharacterStats> GetMonsterUnits()
        {
            return Instance.monsterPositions;
        }

        public void PauseCombat()
        {
            isPaused = true;
        }

        public void PlayCombat()
        {
            isPaused = false;
        }

        private string SelectAbility(CharacterStats unit, PartyData partyData, List<ICombatUnit> targets)
        {
            // All units use BasicAttack, per CharacterLibrary.cs
            return "BasicAttack";
        }

        private bool CanAttackThisRound(ICombatUnit unit, UnitAttackState state)
        {
            if (unit is not CharacterStats stats) return false;
            if (state.SkipNextAttack)
            {
                state.SkipNextAttack = false;
                return false;
            }
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

        private IEnumerator ProcessAttack(ICombatUnit unit, PartyData partyData, List<ICombatUnit> targets)
        {
            if (unit is not CharacterStats stats) yield break;

            // Hard-coded BasicAttack: TargetEnemies, Melee, Damage=ATK*(1-0.05*DEF), Single, Dodgeable
            string abilityId = "BasicAttack";
            string abilityMessage = $"{stats.Id} uses BasicAttack!";
            allCombatLogs.Add(abilityMessage);
            eventBus.RaiseLogMessage(abilityMessage, Color.white);
            eventBus.RaiseUnitAttacking(unit, null, abilityId);

            // Select single enemy target
            List<ICombatUnit> selectedTargets = new List<ICombatUnit>();
            var enemyTargets = stats.Type == CharacterType.Hero
                ? monsterPositions.Cast<ICombatUnit>().Where(t => t.Health > 0 && !t.HasRetreated).ToList()
                : heroPositions.Cast<ICombatUnit>().Where(t => t.Health > 0 && !t.HasRetreated).ToList();
            ICombatUnit selectedTarget = GetRandomAliveTarget(enemyTargets);
            if (selectedTarget != null)
            {
                selectedTargets.Add(selectedTarget);
            }

            if (selectedTargets.Count == 0)
            {
                string message = $"{stats.Id} finds no valid targets for BasicAttack! <color=#FFFF00>[No Targets]</color>";
                allCombatLogs.Add(message);
                eventBus.RaiseLogMessage(message, Color.white);
                if (NoActiveHeroes() || NoActiveMonsters())
                {
                    EndCombat();
                    yield break;
                }
                yield break;
            }

            var state = unitAttackStates.Find(s => s.Unit == unit);
            int originalAttack = stats.Attack;
            int originalSpeed = stats.Speed;
            int originalEvasion = stats.Evasion;
            if (state != null)
            {
                if (state.TempStats.TryGetValue("Attack", out var attackMod)) stats.Attack += attackMod.value;
                if (state.TempStats.TryGetValue("Speed", out var speedMod)) stats.Speed += speedMod.value;
                if (state.TempStats.TryGetValue("Evasion", out var evaMod)) stats.Evasion += evaMod.value;
            }

            yield return new WaitUntil(() => !isPaused);
            yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));

            foreach (var target in selectedTargets)
            {
                var targetStats = target as CharacterStats;
                var targetState = unitAttackStates.Find(s => s.Unit == target);
                int originalDefense = targetStats != null ? targetStats.Defense : 0;
                int currentEvasion = targetStats != null ? targetStats.Evasion : 0;
                if (targetState != null && targetStats != null)
                {
                    if (targetState.TempStats.TryGetValue("Defense", out var defMod)) targetStats.Defense += defMod.value;
                    if (targetState.TempStats.TryGetValue("Evasion", out var evaMod)) currentEvasion += evaMod.value;
                }

                // Dodge check for BasicAttack
                float dodgeChance = Mathf.Clamp(currentEvasion, 0, 100) / 100f;
                if (Random.value <= dodgeChance)
                {
                    string dodgeMessage = $"{targetStats.Id} dodges the attack! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>";
                    allCombatLogs.Add(dodgeMessage);
                    eventBus.RaiseLogMessage(dodgeMessage, Color.white);
                    continue;
                }
                allCombatLogs.Add($"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>");
                eventBus.RaiseLogMessage($"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>", Color.white);

                // Calculate damage: ATK * (1 - 0.05 * DEF)
                int damage = Mathf.Max(0, Mathf.RoundToInt(stats.Attack * (1f - 0.05f * (targetStats != null ? targetStats.Defense : 0))));
                string damageFormula = $"[{stats.Attack} ATK - {targetStats?.Defense ?? 0} DEF * 5%]";
                if (damage > 0 && targetStats != null)
                {
                    targetStats.Health -= damage;
                    string damageMessage = $"{stats.Id} hits {targetStats.Id} for {damage} damage with BasicAttack <color=#FFFF00>{damageFormula}</color>";
                    allCombatLogs.Add(damageMessage);
                    eventBus.RaiseLogMessage(damageMessage, Color.white);
                    UpdateUnit(target, damageMessage);
                }

                // Handle Thorns (if any from previous buffs)
                if (targetState != null && targetState.TempStats.TryGetValue("ThornsFixed", out var thornsMod) && thornsMod.value > 0)
                {
                    int thornsDamage = thornsMod.value;
                    stats.Health -= thornsDamage;
                    string thornsMessage = $"{targetStats.Id} reflects {thornsDamage} damage to {stats.Id}! <color=#FFFF00>[ThornsFixed:{thornsDamage}]</color>";
                    allCombatLogs.Add(thornsMessage);
                    eventBus.RaiseLogMessage(thornsMessage, Color.white);
                    UpdateUnit(unit, thornsMessage);
                    if (targetState.TempStats.ContainsKey("ThornsInfection"))
                    {
                        float infectionChance = targetStats.Infectivity / 100f;
                        float resistanceChance = stats.Infectivity / 100f;
                        if (Random.value <= infectionChance && Random.value > resistanceChance && !stats.IsInfected)
                        {
                            stats.IsInfected = true;
                            string infectionMessage = $"{stats.Id} is infected by thorns! <color=#FFFF00>[{targetStats.Infectivity}% Infection vs {stats.Infectivity}% Resistance]</color>";
                            allCombatLogs.Add(infectionMessage);
                            eventBus.RaiseUnitInfected(stats, "Virus");
                            UpdateUnit(unit, infectionMessage);
                        }
                    }
                    if (stats.Health <= 0)
                    {
                        eventBus.RaiseUnitDied(unit);
                        string deathMessage = $"{stats.Id} dies!";
                        allCombatLogs.Add(deathMessage);
                        eventBus.RaiseLogMessage(deathMessage, Color.red);
                    }
                }

                if (targetStats != null && target.Health <= 0)
                {
                    eventBus.RaiseUnitDied(target);
                    string deathMessage = $"{targetStats.Id} dies!";
                    allCombatLogs.Add(deathMessage);
                    eventBus.RaiseLogMessage(deathMessage, Color.red);
                }

                if (targetStats != null) targetStats.Defense = originalDefense;
            }

            stats.Attack = originalAttack;
            stats.Speed = originalSpeed;
            stats.Evasion = originalEvasion;

            UpdateUnit(unit);
        }

        private IEnumerator RunCombat()
        {
            if (isCombatActive) yield break;
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
                yield return new WaitUntil(() => !isPaused);
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
                        state = new UnitAttackState { Unit = unit, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int, int)>() };
                        unitAttackStates.Add(state);
                    }
                    state.AttacksThisRound = 0;
                    state.RoundCounter++;
                    foreach (var abilityCd in state.AbilityCooldowns.ToList())
                    {
                        state.AbilityCooldowns[abilityCd.Key] = Mathf.Max(0, abilityCd.Value - 1);
                        if (state.AbilityCooldowns[abilityCd.Key] == 0)
                        {
                            state.AbilityCooldowns.Remove(abilityCd.Key);
                            string cooldownEndMessage = $"{unit.Id}'s {abilityCd.Key} is off cooldown!";
                            allCombatLogs.Add(cooldownEndMessage);
                            eventBus.RaiseLogMessage(cooldownEndMessage, Color.white);
                        }
                    }
                    foreach (var tempStat in state.TempStats.ToList())
                    {
                        state.TempStats[tempStat.Key] = (tempStat.Value.value, tempStat.Value.duration - 1);
                        if (state.TempStats[tempStat.Key].duration <= 0)
                        {
                            state.TempStats.Remove(tempStat.Key);
                            if (unit is CharacterStats stats)
                            {
                                var baseData = stats.Type == CharacterType.Hero
                                    ? CharacterLibrary.GetHeroData(stats.Id)
                                    : CharacterLibrary.GetMonsterData(stats.Id);
                                if (tempStat.Key == "Defense")
                                {
                                    stats.Defense = baseData.Defense;
                                    string message = $"{stats.Id}'s Defense buff expires! <color=#FFFF00>[Restored to {stats.Defense}]</color>";
                                    allCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, Color.white);
                                }
                                else if (tempStat.Key == "Speed")
                                {
                                    stats.Speed = baseData.Speed;
                                    string message = $"{stats.Id}'s Speed buff expires! <color=#FFFF00>[Restored to {stats.Speed}]</color>";
                                    allCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, Color.white);
                                }
                                else if (tempStat.Key == "Evasion")
                                {
                                    stats.Evasion = baseData.Evasion;
                                    string message = $"{stats.Id}'s Evasion buff expires! <color=#FFFF00>[Restored to {stats.Evasion}]</color>";
                                    allCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, Color.white);
                                }
                            }
                        }
                    }
                }
                foreach (var unit in unitList.ToList())
                {
                    var state = unitAttackStates.Find(s => s.Unit == unit);
                    if (state == null || unit.Health <= 0 || unit.HasRetreated) continue;
                    if (unit is CharacterStats stats && stats.Type == CharacterType.Hero && CheckRetreat(unit))
                    {
                        string retreatMessage = $"{stats.Id} flees! <color=#FFFF00>[Morale <= {combatConfig.RetreatMoraleThreshold}]</color>";
                        allCombatLogs.Add(retreatMessage);
                        ProcessRetreat(unit);
                        yield return new WaitUntil(() => !isPaused);
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
            allCombatLogs.Clear();
            string initMessage = "Combat begins!";
            allCombatLogs.Add(initMessage);
            eventBus.RaiseLogMessage(initMessage, Color.white);
            foreach (var hero in heroStats.Where(h => h.Type == CharacterType.Hero && h.Health > 0))
            {
                var stats = hero.GetDisplayStats();
                units.Add((hero, null, stats));
                heroPositions.Add(hero);
                unitAttackStates.Add(new UnitAttackState { Unit = hero, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int, int)>() });
                string heroMessage = $"{hero.Id} enters combat with {hero.Health}/{hero.MaxHealth} HP, {hero.Morale}/{hero.MaxMorale} Morale.";
                allCombatLogs.Add(heroMessage);
                eventBus.RaiseLogMessage(heroMessage, Color.white);
            }
            foreach (var monster in monsterStats.Where(m => m.Type == CharacterType.Monster && m.Health > 0 && !m.HasRetreated))
            {
                var stats = monster.GetDisplayStats();
                units.Add((monster, null, stats));
                monsterPositions.Add(monster);
                unitAttackStates.Add(new UnitAttackState { Unit = monster, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int, int)>() });
                string monsterMessage = $"{monster.Id} enters combat with {monster.Health}/{monster.MaxHealth} HP.";
                allCombatLogs.Add(monsterMessage);
                eventBus.RaiseLogMessage(monsterMessage, Color.white);
            }
            heroPositions = heroPositions.OrderBy(h => h.PartyPosition).ToList();
            monsterPositions = monsterPositions.OrderBy(m => m.PartyPosition).ToList();
            eventBus.RaiseCombatInitialized(units);
        }

        private void IncrementRound()
        {
            roundNumber++;
            string roundMessage = $"Round {roundNumber} begins!";
            allCombatLogs.Add(roundMessage);
            eventBus.RaiseLogMessage(roundMessage, Color.white);
        }

        private void LogMessage(string message, Color color)
        {
            allCombatLogs.Add(message);
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
                    allCombatLogs.Add(damageMessage);
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
                string retreatMessage = $"{stats.Id} flees! <color=#FFFF00>[Morale <= {combatConfig.RetreatMoraleThreshold}]</color>";
                allCombatLogs.Add(retreatMessage);
                LogMessage(retreatMessage, uiConfig.TextColor);
                eventBus.RaiseUnitRetreated(unit);
                int penalty = 10;
                var teammates = units
                    .Select(u => u.unit)
                    .Where(u => u is CharacterStats cs && cs.Type == stats.Type && u.Health > 0 && !u.HasRetreated && u != unit)
                    .ToList();
                foreach (var teammate in teammates)
                {
                    teammate.Morale = Mathf.Max(0, teammate.Morale - penalty);
                    string teammateMessage = $"{teammate.Id}'s morale drops by {penalty} due to {stats.Id}'s retreat! <color=#FFFF00>[-{penalty} Morale]</color>";
                    allCombatLogs.Add(teammateMessage);
                    UpdateUnit(teammate, teammateMessage);
                }
                UpdateUnit(unit);
            }
        }

        private void EndCombat()
        {
            if (isEndingCombat) return;
            isEndingCombat = true;
            isCombatActive = false;
            string endMessage = "Combat ends!";
            allCombatLogs.Add(endMessage);
            eventBus.RaiseLogMessage(endMessage, Color.white);
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
                string victoryMessage = "Party victorious!";
                allCombatLogs.Add(victoryMessage);
                eventBus.RaiseLogMessage(victoryMessage, Color.green);
                expeditionManager.TransitionToExpeditionScene();
            }
            else
            {
                string defeatMessage = "Party defeated!";
                allCombatLogs.Add(defeatMessage);
                eventBus.RaiseLogMessage(defeatMessage, Color.red);
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
                Debug.LogError("CombatSceneComponent: Missing required reference(s). Please assign in the Inspector.");
                return false;
            }
            return true;
        }

        public void SetCombatSpeed(float speed)
        {
            if (combatConfig != null)
            {
                float oldSpeed = combatConfig.CombatSpeed;
                combatConfig.CombatSpeed = Mathf.Clamp(speed, combatConfig.MinCombatSpeed, combatConfig.MaxCombatSpeed);
                if (oldSpeed != combatConfig.CombatSpeed)
                {
                    string speedMessage = $"Combat speed set to {combatConfig.CombatSpeed:F1}x!";
                    allCombatLogs.Add(speedMessage);
                    eventBus.RaiseLogMessage(speedMessage, Color.white);
                }
            }
        }
    }
}