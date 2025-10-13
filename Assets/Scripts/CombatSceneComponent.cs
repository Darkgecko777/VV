using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public class CombatSceneComponent : MonoBehaviour
    {
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private Camera combatCamera;
        [SerializeField] private PartyData partyData;
        private List<UnitAttackState> unitAttackStates = new List<UnitAttackState>();
        private List<string> allCombatLogs = new List<string>();
        private List<CharacterStats> heroPositions = new List<CharacterStats>();
        private List<CharacterStats> monsterPositions = new List<CharacterStats>();
        public List<(ICombatUnit unit, GameObject go, CharacterStats.DisplayStats displayStats)> units = new List<(ICombatUnit, GameObject, CharacterStats.DisplayStats)>();
        private bool isCombatActive;
        private bool isPaused;
        private int roundNumber;
        private Dictionary<string, float> noTargetLogCooldowns = new Dictionary<string, float>();
        private Coroutine activeCombatCoroutine;
        private float lastEndCombatTime;
        private const float logDuplicateWindow = 1f;
        private static bool hasSubscribed;
        public ExpeditionManager ExpeditionManager => ExpeditionManager.Instance;
        public List<string> AllCombatLogs => allCombatLogs;
        public bool IsPaused => isPaused;
        public EventBusSO EventBus => eventBus;
        public UIConfig UIConfig => uiConfig;

        public UnitAttackState GetUnitAttackState(ICombatUnit unit)
        {
            return unitAttackStates.Find(s => s.Unit == unit);
        }

        void Awake()
        {
            isCombatActive = false;
            isPaused = false;
            roundNumber = 0;
            noTargetLogCooldowns.Clear();
            unitAttackStates.Clear();
            allCombatLogs.Clear();
            heroPositions.Clear();
            monsterPositions.Clear();
            units.Clear();
            lastEndCombatTime = -logDuplicateWindow;
            if (!hasSubscribed)
            {
                eventBus.OnCombatPaused += () => { isPaused = true; };
                eventBus.OnCombatPlayed += () => { isPaused = false; };
                eventBus.OnCombatEnded += (isVictory) => EndCombat(ExpeditionManager, isVictory);
                eventBus.OnRequestSetCombatSpeed += SetCombatSpeed;
                hasSubscribed = true;
            }
        }

        void Start()
        {
            if (!ValidateReferences())
            {
                Debug.LogError("CombatSceneComponent: Validation failed, aborting Start.");
                return;
            }
            var expedition = ExpeditionManager?.GetExpedition();
            if (expedition == null || expedition.Party == null || expedition.NodeData == null || expedition.CurrentNodeIndex >= expedition.NodeData.Count)
            {
                Debug.LogError("CombatSceneComponent: Invalid expedition data, cannot initialize units.");
                return;
            }
            InitializeUnits(expedition.Party.GetHeroes(), expedition.NodeData[expedition.CurrentNodeIndex].Monsters);
            StartCombatLoop(expedition.Party);
        }

        void OnDestroy()
        {
            if (hasSubscribed)
            {
                eventBus.OnCombatPaused -= () => { isPaused = true; };
                eventBus.OnCombatPlayed -= () => { isPaused = false; };
                eventBus.OnCombatEnded -= (isVictory) => EndCombat(ExpeditionManager, isVictory);
                eventBus.OnRequestSetCombatSpeed -= SetCombatSpeed;
                hasSubscribed = false;
            }
            if (activeCombatCoroutine != null)
            {
                StopCoroutine(activeCombatCoroutine);
                activeCombatCoroutine = null;
            }
        }

        public void PauseCombat()
        {
            isPaused = true;
            eventBus.RaiseCombatPaused();
        }

        public void PlayCombat()
        {
            isPaused = false;
            eventBus.RaiseCombatPlayed();
        }

        public void SetCombatSpeed(float speed)
        {
            if (combatConfig != null)
            {
                float oldSpeed = combatConfig.CombatSpeed;
                combatConfig.CombatSpeed = Mathf.Clamp(speed, combatConfig.MinCombatSpeed, combatConfig.MaxCombatSpeed);
                Debug.Log($"CombatSceneComponent: CombatSpeed set to {combatConfig.CombatSpeed:F1}x (was {oldSpeed:F1}x)");
                if (oldSpeed != combatConfig.CombatSpeed)
                {
                    string speedMessage = $"Combat speed set to {combatConfig.CombatSpeed:F1}x!";
                    allCombatLogs.Add(speedMessage);
                    eventBus.RaiseLogMessage(speedMessage, uiConfig.TextColor);
                    eventBus.RaiseCombatSpeedChanged(combatConfig.CombatSpeed);
                }
            }
        }

        public void InitializeUnits(List<CharacterStats> heroStats, List<CharacterStats> monsterStats)
        {
            string initMessage = "Combat begins!";
            allCombatLogs.Add(initMessage);
            eventBus.RaiseLogMessage(initMessage, uiConfig.TextColor);
            foreach (var hero in heroStats.Where(h => h.Type == CharacterType.Hero && h.Health > 0))
            {
                if (hero.abilities == null || hero.abilities.Length == 0)
                {
                    Debug.LogError($"CombatSceneComponent: No abilities defined for hero {hero.Id}.");
                    hero.abilityIds = new string[] { "MeleeStrike" };
                    hero.abilities = new AbilitySO[0];
                }
                var stats = hero.GetDisplayStats();
                units.Add((hero, null, stats));
                heroPositions.Add(hero);
                unitAttackStates.Add(new UnitAttackState
                {
                    Unit = hero,
                    AttacksThisRound = 0,
                    RoundCounter = 0,
                    AbilityCooldowns = new Dictionary<string, int>(),
                    RoundCooldowns = new Dictionary<string, int>(),
                    SkipNextAttack = false,
                    TempStats = new Dictionary<string, (int value, int duration)>()
                });
                string heroMessage = $"{hero.Id} enters combat with {hero.Health}/{hero.MaxHealth} HP, {hero.Morale}/{hero.MaxMorale} Morale.";
                allCombatLogs.Add(heroMessage);
                eventBus.RaiseLogMessage(heroMessage, uiConfig.TextColor);
            }
            foreach (var monster in monsterStats.Where(m => m.Type == CharacterType.Monster && m.Health > 0 && !m.HasRetreated))
            {
                if (monster.abilities == null || monster.abilities.Length == 0)
                {
                    Debug.LogError($"CombatSceneComponent: No abilities defined for monster {monster.Id}.");
                    monster.abilityIds = new string[] { "MeleeStrike" };
                    monster.abilities = new AbilitySO[0];
                }
                var stats = monster.GetDisplayStats();
                units.Add((monster, null, stats));
                monsterPositions.Add(monster);
                unitAttackStates.Add(new UnitAttackState
                {
                    Unit = monster,
                    AttacksThisRound = 0,
                    RoundCounter = 0,
                    AbilityCooldowns = new Dictionary<string, int>(),
                    RoundCooldowns = new Dictionary<string, int>(),
                    SkipNextAttack = false,
                    TempStats = new Dictionary<string, (int value, int duration)>()
                });
                string monsterMessage = $"{monster.Id} enters combat with {monster.Health}/{monster.MaxHealth} HP.";
                allCombatLogs.Add(monsterMessage);
                eventBus.RaiseLogMessage(monsterMessage, uiConfig.TextColor);
            }
            heroPositions = heroPositions.OrderBy(h => h.PartyPosition).ToList();
            monsterPositions = monsterPositions.OrderBy(m => m.PartyPosition).ToList();
            eventBus.RaiseCombatInitialized(units);
        }

        public void UpdateUnit(ICombatUnit unit, string damageMessage = null)
        {
            if (unit == null) return;
            var unitEntry = units.Find(u => u.unit == unit);
            if (unitEntry.unit != null)
            {
                units.Remove(unitEntry);
                var newStats = unit.GetDisplayStats();
                units.Add((unit, null, newStats));
                eventBus.RaiseUnitUpdated(unit, newStats);
                if (damageMessage != null)
                {
                    allCombatLogs.Add(damageMessage);
                    eventBus.RaiseLogMessage(damageMessage, uiConfig.TextColor);
                    eventBus.RaiseUnitDamaged(unit, damageMessage);
                }
                if (unit is CharacterStats stats && (stats.Health <= 0 || stats.HasRetreated))
                {
                    if (stats.Type == CharacterType.Hero)
                        heroPositions.Remove(stats);
                    else
                        monsterPositions.Remove(stats);
                }
            }
        }

        public void TryInfectUnit(CharacterStats source, CharacterStats target, TransmissionVector changedVector, float delta, List<string> combatLogs, EventBusSO eventBus, UIConfig uiConfig)
        {
            if (source == null || target == null || target.Health <= 0 || target.HasRetreated) return;

            // Standard infection (source to target if source infected)
            if (source.Infections.Any(v => v.TransmissionVector == changedVector))
            {
                foreach (var virus in source.Infections.Where(v => v.TransmissionVector == changedVector))
                {
                    float infectionChance = Mathf.Clamp01(1f - (target.Immunity / 100f + virus.InfectivityModifier));
                    string chanceMessage = $"{source.Id} attempts to infect {target.Id} with {virus.VirusID} after {(delta > 0 ? "+" : "")}{delta} {changedVector}: {(infectionChance * 100):F0}% chance";
                    combatLogs.Add(chanceMessage);
                    eventBus.RaiseLogMessage(chanceMessage, uiConfig.TextColor);
                    if (Random.value <= infectionChance)
                    {
                        target.AddInfection(virus);
                        string infectMessage = $"{target.Id} infected with {virus.VirusID}!";
                        combatLogs.Add(infectMessage);
                        eventBus.RaiseLogMessage(infectMessage, Color.red);
                        eventBus.RaiseUnitInfected(target, virus.VirusID);
                        eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                }
            }

            // Counter-infection (target to source if target infected, with 0.3 multiplier)
            if (target.Infections.Any(v => v.TransmissionVector == changedVector))
            {
                foreach (var virus in target.Infections.Where(v => v.TransmissionVector == changedVector))
                {
                    float baseChance = 1f - (source.Immunity / 100f + virus.InfectivityModifier);
                    float infectionChance = Mathf.Clamp01(baseChance * 0.3f);
                    string chanceMessage = $"{target.Id} attempts counter-infection on {source.Id} with {virus.VirusID} after {(delta > 0 ? "+" : "")}{delta} {changedVector}: {(infectionChance * 100):F0}% chance";
                    combatLogs.Add(chanceMessage);
                    eventBus.RaiseLogMessage(chanceMessage, uiConfig.TextColor);
                    if (Random.value <= infectionChance)
                    {
                        source.AddInfection(virus);
                        string infectMessage = $"{source.Id} infected with {virus.VirusID} via counter-attack!";
                        combatLogs.Add(infectMessage);
                        eventBus.RaiseLogMessage(infectMessage, Color.red);
                        eventBus.RaiseUnitInfected(source, virus.VirusID);
                        eventBus.RaiseUnitUpdated(source, source.GetDisplayStats());
                    }
                }
            }
        }

        public void StartCombatLoop(PartyData party)
        {
            if (partyData == null)
            {
                Debug.LogError("CombatSceneComponent: partyData is null, cannot start combat loop.");
                return;
            }
            if (isCombatActive)
            {
                Debug.LogWarning("CombatSceneComponent: Combat already active, stopping existing coroutine.");
                if (activeCombatCoroutine != null)
                {
                    StopCoroutine(activeCombatCoroutine);
                    activeCombatCoroutine = null;
                }
            }
            partyData = party;
            isCombatActive = true;
            activeCombatCoroutine = StartCoroutine(RunCombat());
        }

        private IEnumerator RunCombat()
        {
            if (!isCombatActive)
            {
                Debug.LogWarning("CombatSceneComponent: RunCombat called while not active, exiting.");
                yield break;
            }
            var expeditionData = ExpeditionManager.GetExpedition();
            if (expeditionData == null || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogWarning("CombatSceneComponent: Invalid expedition data, ending combat.");
                isCombatActive = false;
                eventBus.RaiseCombatEnded(false);
                yield break;
            }
            var heroStats = expeditionData.Party.GetHeroes();
            var monsterStats = expeditionData.NodeData[expeditionData.CurrentNodeIndex].Monsters;
            if (heroStats == null || heroStats.Count == 0 || monsterStats == null || monsterStats.Count == 0)
            {
                Debug.LogWarning("CombatSceneComponent: No valid units for combat, ending.");
                isCombatActive = false;
                eventBus.RaiseCombatEnded(false);
                yield break;
            }
            isCombatActive = true;
            IncrementRound();
            while (isCombatActive)
            {
                yield return new WaitUntil(() => !isPaused);
                partyData.ApplyVirusEffects(eventBus, uiConfig, allCombatLogs);
                var unitList = heroPositions.Cast<ICombatUnit>().Concat(monsterPositions.Cast<ICombatUnit>()).Where(u => u.Health > 0 && !u.HasRetreated).OrderByDescending(u => u.Speed).ToList();
                if (unitList.Count == 0 || monsterPositions.Count == 0)
                {
                    isCombatActive = false;
                    eventBus.RaiseCombatEnded(true);
                    yield break;
                }
                if (heroPositions.Count == 0)
                {
                    isCombatActive = false;
                    eventBus.RaiseCombatEnded(false);
                    yield break;
                }
                foreach (var unit in unitList.ToList())
                {
                    var state = unitAttackStates.Find(s => s.Unit == unit);
                    if (state == null)
                    {
                        state = new UnitAttackState
                        {
                            Unit = unit,
                            AttacksThisRound = 0,
                            RoundCounter = 0,
                            AbilityCooldowns = new Dictionary<string, int>(),
                            RoundCooldowns = new Dictionary<string, int>(),
                            SkipNextAttack = false,
                            TempStats = new Dictionary<string, (int value, int duration)>()
                        };
                        unitAttackStates.Add(state);
                    }
                    state.AttacksThisRound = 0;
                    state.RoundCounter++;
                    foreach (var tempStat in state.TempStats.ToList())
                    {
                        if (tempStat.Value.duration == -1) continue; // Combat-end effects persist
                        state.TempStats[tempStat.Key] = (tempStat.Value.value, tempStat.Value.duration - 1);
                        if (state.TempStats[tempStat.Key].duration <= 0)
                        {
                            state.TempStats.Remove(tempStat.Key);
                        }
                    }
                    if (!CanAttackThisRound(unit, state) || unit.Health <= 0 || unit.HasRetreated) continue;
                    state.AttacksThisRound++;
                    if (unit is CharacterStats stats)
                    {
                        yield return stats.PerformAbility(state, partyData, unitList, eventBus, uiConfig, combatConfig, allCombatLogs, heroPositions, monsterPositions, u => UpdateUnit(u), this);
                        yield return ScaledWait(0.95f); // Sync to TiltForward duration
                    }
                    if (heroPositions.Count == 0 || monsterPositions.Count == 0)
                    {
                        isCombatActive = false;
                        eventBus.RaiseCombatEnded(monsterPositions.Count == 0);
                        yield break;
                    }
                }
                foreach (var unit in unitList.ToList())
                {
                    var state = unitAttackStates.Find(s => s.Unit == unit);
                    if (state == null || unit.Health <= 0 || unit.HasRetreated) continue;
                    if (unit is CharacterStats stats && stats.Speed >= combatConfig.SpeedTwoAttacksThreshold && state.AttacksThisRound < 2)
                    {
                        state.AttacksThisRound++;
                        Debug.Log($"CombatSceneComponent: Starting {unit.Id}'s second PerformAbility");
                        yield return stats.PerformAbility(state, partyData, unitList, eventBus, uiConfig, combatConfig, allCombatLogs, heroPositions, monsterPositions, u => UpdateUnit(u), this);
                        Debug.Log($"CombatSceneComponent: Finished {unit.Id}'s second PerformAbility, waiting for animation tempo (~0.95s base)");
                        yield return ScaledWait(0.95f); // Sync to TiltForward duration
                        if (heroPositions.Count == 0 || monsterPositions.Count == 0)
                        {
                            isCombatActive = false;
                            eventBus.RaiseCombatEnded(monsterPositions.Count == 0);
                            yield break;
                        }
                    }
                }
                foreach (var state in unitAttackStates)
                {
                    if (state.Unit.Health <= 0 || state.Unit.HasRetreated) continue;
                    foreach (var roundCd in state.RoundCooldowns.ToList())
                    {
                        state.RoundCooldowns[roundCd.Key] = Mathf.Max(0, roundCd.Value - 1);
                        if (state.RoundCooldowns[roundCd.Key] == 0)
                        {
                            state.RoundCooldowns.Remove(roundCd.Key);
                            string cooldownEndMessage = $"{state.Unit.Id}'s {roundCd.Key} is off cooldown!";
                            allCombatLogs.Add(cooldownEndMessage);
                            eventBus.RaiseLogMessage(cooldownEndMessage, uiConfig.TextColor);
                        }
                    }
                }
                IncrementRound();
            }
        }

        private IEnumerator ScaledWait(float baseDuration)
        {
            float elapsed = 0f;
            while (elapsed < baseDuration)
            {
                if (isPaused) yield return null;
                else elapsed += Time.deltaTime * combatConfig.CombatSpeed;
                yield return null;
            }
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

        private void IncrementRound()
        {
            roundNumber++;
            string roundMessage = $"Round {roundNumber} begins!";
            allCombatLogs.Add(roundMessage);
            eventBus.RaiseLogMessage(roundMessage, uiConfig.TextColor);
        }

        private void EndCombat(ExpeditionManager expeditionManager, bool isVictory)
        {
            float currentTime = Time.time;
            if (currentTime - lastEndCombatTime < logDuplicateWindow)
            {
                Debug.LogWarning($"CombatSceneComponent: EndCombat called too soon after previous call (time: {currentTime}, last: {lastEndCombatTime}). Skipping duplicate logs.");
                return;
            }
            lastEndCombatTime = currentTime;
            string endMessage = "Combat ends!";
            if (!allCombatLogs.Contains(endMessage) || allCombatLogs.LastIndexOf(endMessage) < allCombatLogs.Count - 5)
            {
                allCombatLogs.Add(endMessage);
                eventBus?.RaiseLogMessage(endMessage, uiConfig?.TextColor ?? Color.white);
            }

            // Reset temporary stat changes (except Health and Morale)
            foreach (var state in unitAttackStates)
            {
                if (state.Unit is CharacterStats stats)
                {
                    foreach (var tempStat in state.TempStats.ToList())
                    {
                        string statKey = tempStat.Key;
                        if (statKey == "health" || statKey == "morale") continue;
                        int originalValue = GetOriginalStatValue(stats, statKey);
                        SetStatValue(stats, statKey, originalValue);
                        state.TempStats.Remove(statKey);
                        string resetMessage = $"{stats.Id}'s {statKey} reset to {originalValue} at combat end.";
                        allCombatLogs.Add(resetMessage);
                        eventBus?.RaiseLogMessage(resetMessage, uiConfig?.TextColor ?? Color.white);
                        eventBus?.RaiseUnitUpdated(stats, stats.GetDisplayStats());
                    }
                }
            }

            expeditionManager.SaveProgress();
            unitAttackStates.Clear();
            heroPositions.Clear();
            monsterPositions.Clear();
            units.Clear();
            noTargetLogCooldowns.Clear();
            isCombatActive = false;
            roundNumber = 0;
            if (isVictory)
            {
                var expedition = expeditionManager.GetExpedition();
                if (expedition != null && expedition.CurrentNodeIndex < expedition.NodeData.Count)
                {
                    expedition.NodeData[expedition.CurrentNodeIndex].Completed = true;
                    string victoryMessage = "Party victorious!";
                    if (!allCombatLogs.Contains(victoryMessage) || allCombatLogs.LastIndexOf(victoryMessage) < allCombatLogs.Count - 5)
                    {
                        allCombatLogs.Add(victoryMessage);
                        eventBus?.RaiseLogMessage(victoryMessage, Color.green);
                    }
                }
                else
                {
                    Debug.LogWarning("CombatSceneComponent: Invalid expedition data, cannot mark node as completed.");
                }
            }
            else
            {
                string defeatMessage = "Party defeated!";
                if (!allCombatLogs.Contains(defeatMessage) || allCombatLogs.LastIndexOf(defeatMessage) < allCombatLogs.Count - 5)
                {
                    allCombatLogs.Add(defeatMessage);
                    eventBus?.RaiseLogMessage(defeatMessage, Color.red);
                }
            }
        }

        private bool ValidateReferences()
        {
            if (combatConfig == null)
                Debug.LogError("CombatSceneComponent: combatConfig is null.");
            if (eventBus == null)
                Debug.LogError("CombatSceneComponent: eventBus is null.");
            if (uiConfig == null)
                Debug.LogError("CombatSceneComponent: uiConfig is null.");
            if (combatCamera == null)
                Debug.LogError("CombatSceneComponent: combatCamera is null.");
            if (partyData == null)
                Debug.LogError("CombatSceneComponent: partyData is null.");
            if (combatConfig == null || eventBus == null || uiConfig == null || combatCamera == null || partyData == null)
                return false;
            return true;
        }

        private int GetOriginalStatValue(CharacterStats stats, string statKey)
        {
            return statKey switch
            {
                "speed" => stats.Speed,
                "attack" => stats.Attack,
                "defense" => stats.Defense,
                "evasion" => stats.Evasion,
                "immunity" => stats.Immunity,
                _ => 0
            };
        }

        private void SetStatValue(CharacterStats stats, string statKey, int value)
        {
            switch (statKey)
            {
                case "speed":
                    stats.Speed = value;
                    break;
                case "attack":
                    stats.Attack = value;
                    break;
                case "defense":
                    stats.Defense = value;
                    break;
                case "evasion":
                    stats.Evasion = value;
                    break;
                case "immunity":
                    stats.Immunity = value;
                    break;
            }
        }
    }
}