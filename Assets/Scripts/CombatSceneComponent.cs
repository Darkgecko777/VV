using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        private List<(ICombatUnit unit, GameObject go, CharacterStats.DisplayStats displayStats)> units = new List<(ICombatUnit, GameObject, CharacterStats.DisplayStats)>();
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
                hasSubscribed = true;
            }
        }

        void Start()
        {
            AbilityDatabase.Reinitialize(this);
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
                if (hero.abilityIds == null || hero.abilityIds.Length == 0)
                {
                    hero.abilityIds = AbilityDatabase.GetCharacterAbilityIds(hero.Id, CharacterType.Hero);
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
                if (monster.abilityIds == null || monster.abilityIds.Length == 0)
                {
                    monster.abilityIds = AbilityDatabase.GetCharacterAbilityIds(monster.Id, CharacterType.Monster);
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

        public UnitAttackState GetUnitAttackState(ICombatUnit unit)
        {
            return unitAttackStates.Find(s => s.Unit == unit);
        }

        public List<CharacterStats> GetMonsterUnits()
        {
            return monsterPositions;
        }

        public List<ICombatUnit> SelectTargets(CharacterStats user, List<ICombatUnit> targetPool, PartyData partyData, CombatTypes.TargetingRule rule, bool isMelee, CombatTypes.ConditionTarget targetType)
        {
            rule.Validate();
            if (targetPool == null || targetPool.Count == 0)
            {
                Debug.LogWarning($"CombatSceneComponent: Empty targetPool for {user.Id}. Returning empty list.");
                return new List<ICombatUnit>();
            }
            var orderedHeroes = heroPositions
                .Where(h => h.Health > 0 && !h.HasRetreated)
                .OrderBy(h => h.PartyPosition)
                .Select((h, i) => new { Unit = (ICombatUnit)h, CombatPosition = i + 1 })
                .ToList();
            var orderedMonsters = monsterPositions
                .Where(m => m.Health > 0 && !m.HasRetreated)
                .OrderBy(m => m.PartyPosition)
                .Select((m, i) => new { Unit = (ICombatUnit)m, CombatPosition = i + 1 })
                .ToList();
            List<ICombatUnit> filteredPool = targetType == CombatTypes.ConditionTarget.Ally
                ? (user.Type == CharacterType.Hero ? orderedHeroes.Select(h => h.Unit).ToList() : orderedMonsters.Select(m => m.Unit).ToList())
                : (user.Type == CharacterType.Hero ? orderedMonsters.Select(m => m.Unit).ToList() : orderedHeroes.Select(h => h.Unit).ToList());
            targetPool = targetPool.Where(t => filteredPool.Contains(t)).ToList();
            if (rule.MinPosition > 0 || rule.MaxPosition > 0)
            {
                targetPool = targetPool.Where(t =>
                {
                    var pos = user.Type == CharacterType.Hero
                        ? orderedMonsters.FirstOrDefault(m => m.Unit == t)?.CombatPosition
                        : orderedHeroes.FirstOrDefault(h => h.Unit == t)?.CombatPosition;
                    return pos.HasValue && pos.Value >= rule.MinPosition && (rule.MaxPosition == 0 || pos.Value <= rule.MaxPosition);
                }).ToList();
            }
            if (isMelee || rule.MeleeOnly)
            {
                if (targetPool.Count > 1)
                {
                    targetPool = targetPool.Where(t =>
                    {
                        var pos = user.Type == CharacterType.Hero
                            ? orderedMonsters.FirstOrDefault(m => m.Unit == t)?.CombatPosition
                            : orderedHeroes.FirstOrDefault(h => h.Unit == t)?.CombatPosition;
                        return pos.HasValue && pos.Value <= 2;
                    }).ToList();
                    if (targetPool.Count == 0)
                    {
                        Debug.LogWarning($"CombatSceneComponent: No frontline targets for {user.Id}'s melee attack.");
                        return new List<ICombatUnit>();
                    }
                }
            }
            if (rule.MustBeInfected || rule.MustNotBeInfected)
            {
                targetPool = targetPool.Where(t =>
                {
                    var stats = t as CharacterStats;
                    bool isInfected = stats != null && stats.IsInfected; // Assumes CharacterStats has IsInfected
                    return rule.MustBeInfected ? isInfected : rule.MustNotBeInfected ? !isInfected : true;
                }).ToList();
            }
            switch (rule.Type)
            {
                case CombatTypes.TargetingRule.RuleType.LowestHealth:
                    targetPool = targetPool.OrderBy(t => (t as CharacterStats)?.Health ?? int.MaxValue).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.HighestHealth:
                    targetPool = targetPool.OrderByDescending(t => (t as CharacterStats)?.Health ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.LowestMorale:
                    targetPool = targetPool.OrderBy(t => (t as CharacterStats)?.Morale ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.HighestMorale:
                    targetPool = targetPool.OrderByDescending(t => (t as CharacterStats)?.Morale ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.LowestAttack:
                    targetPool = targetPool.OrderBy(t => (t as CharacterStats)?.Attack ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.HighestAttack:
                    targetPool = targetPool.OrderByDescending(t => (t as CharacterStats)?.Attack ?? 0).ToList();
                    break;
                case CombatTypes.TargetingRule.RuleType.AllAllies:
                    if (targetType != CombatTypes.ConditionTarget.Ally)
                    {
                        Debug.LogWarning($"CombatSceneComponent: AllAllies rule requires Ally target for {user.Id}. Returning empty list.");
                        targetPool = new List<ICombatUnit>();
                    }
                    break;
                case CombatTypes.TargetingRule.RuleType.WeightedRandom:
                    if (rule.WeightStat == default)
                    {
                        Debug.LogWarning($"CombatSceneComponent: WeightedRandom for {user.Id} has no WeightStat. Defaulting to Health.");
                        rule.WeightStat = CombatTypes.Stat.Health;
                    }
                    targetPool = targetPool.OrderBy(t => UnityEngine.Random.value * GetWeight(t as CharacterStats, rule.WeightStat, rule.WeightFactor)).ToList();
                    break;
                default:
                    targetPool = targetPool.OrderBy(t => UnityEngine.Random.value).ToList();
                    break;
            }
            int maxTargets = isMelee ? Mathf.Min(2, targetPool.Count) : Mathf.Min(4, targetPool.Count);
            if (rule.Type == CombatTypes.TargetingRule.RuleType.AllAllies && targetType == CombatTypes.ConditionTarget.Ally)
                maxTargets = targetPool.Count;
            var selected = targetPool.Take(maxTargets).ToList();
            return selected;
        }

        private float GetWeight(CharacterStats unit, CombatTypes.Stat stat, float factor)
        {
            if (unit == null) return 1f;
            float value = stat switch
            {
                CombatTypes.Stat.Health => unit.Health,
                CombatTypes.Stat.MaxHealth => unit.MaxHealth,
                CombatTypes.Stat.Morale => unit.Morale,
                CombatTypes.Stat.MaxMorale => unit.MaxMorale,
                CombatTypes.Stat.Speed => unit.Speed,
                CombatTypes.Stat.Attack => unit.Attack,
                CombatTypes.Stat.Defense => unit.Defense,
                CombatTypes.Stat.Evasion => unit.Evasion,
                CombatTypes.Stat.Rank => unit.Rank,
                CombatTypes.Stat.Infectivity => unit.Infectivity,
                CombatTypes.Stat.PartyPosition => unit.PartyPosition,
                _ => 1f
            };
            return value * factor;
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
                    foreach (var abilityCd in state.AbilityCooldowns.ToList())
                    {
                        state.AbilityCooldowns[abilityCd.Key] = Mathf.Max(0, abilityCd.Value - 1);
                        if (state.AbilityCooldowns[abilityCd.Key] == 0)
                        {
                            state.AbilityCooldowns.Remove(abilityCd.Key);
                            string cooldownEndMessage = $"{unit.Id}'s {abilityCd.Key} is off cooldown!";
                            allCombatLogs.Add(cooldownEndMessage);
                            eventBus.RaiseLogMessage(cooldownEndMessage, uiConfig.TextColor);
                        }
                    }
                    foreach (var tempStat in state.TempStats.ToList())
                    {
                        state.TempStats[tempStat.Key] = (tempStat.Value.value, tempStat.Value.duration - 1);
                        if (state.TempStats[tempStat.Key].duration <= 0)
                        {
                            state.TempStats.Remove(tempStat.Key);
                        }
                    }
                    if (!CanAttackThisRound(unit, state) || unit.Health <= 0 || unit.HasRetreated) continue;
                    state.AttacksThisRound++;
                    yield return ProcessAttack(unit, expeditionData.Party, unitList);
                    yield return new WaitForSeconds(0.2f / (combatConfig?.CombatSpeed ?? 1f));
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
                    if (unit is CharacterStats stats && stats.Type == CharacterType.Hero && stats.Speed >= combatConfig.SpeedTwoAttacksThreshold && state.AttacksThisRound < 2)
                    {
                        state.AttacksThisRound++;
                        yield return ProcessAttack(unit, expeditionData.Party, unitList);
                        yield return new WaitForSeconds(0.2f / (combatConfig?.CombatSpeed ?? 1f));
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

        public void ProcessEffect(CharacterStats user, CharacterStats target, string tag, string abilityId)
        {
            if (user == null || target == null)
            {
                Debug.LogWarning($"CombatSceneComponent: Null user or target for effect {tag} in ability {abilityId}.");
                return;
            }
            string[] tagParts = tag.Split(':');
            string effectType = tagParts[0];
            int value = tagParts.Length > 1 && int.TryParse(tagParts[1], out int parsedValue) ? parsedValue : 0;
            float floatValue = tagParts.Length > 1 && float.TryParse(tagParts[1], out float parsedFloat) ? parsedFloat : 0f;
            var targetState = GetUnitAttackState(target);
            string effectMessage = string.Empty;
            Color messageColor = uiConfig.TextColor;
            switch (effectType)
            {
                case "TrueStrike":
                    if (value > 0)
                    {
                        target.Health = Mathf.Max(0, target.Health - value);
                        effectMessage = $"{user.Id} deals {value} direct damage to {target.Id} with {abilityId}! <color=#FFFF00>[TrueStrike]</color>";
                        messageColor = Color.red;
                        eventBus.RaiseUnitDamaged(target, effectMessage);
                    }
                    break;
                case "Heal":
                    if (floatValue > 0)
                    {
                        int healAmount = Mathf.RoundToInt(user.Attack * floatValue);
                        target.Health = Mathf.Min(target.MaxHealth, target.Health + healAmount);
                        effectMessage = $"{user.Id} heals {target.Id} for {healAmount} HP with {abilityId}! <color=#00FF00>[Heal]</color>";
                        messageColor = Color.green;
                        eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                    break;
                case "SkipNextAttack":
                    if (targetState != null)
                    {
                        targetState.SkipNextAttack = true;
                        effectMessage = $"{target.Id} will skip their next attack due to {abilityId}! <color=#FFFF00>[SkipNextAttack]</color>";
                        messageColor = Color.yellow;
                        eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                    else
                    {
                        Debug.LogWarning($"CombatSceneComponent: No UnitAttackState for {target.Id} for SkipNextAttack {tag}.");
                    }
                    break;
                case "Thorns":
                    if (value > 0 && targetState != null)
                    {
                        int reflectDamage = Mathf.RoundToInt(value * 0.5f);
                        user.Health = Mathf.Max(0, user.Health - reflectDamage);
                        effectMessage = $"{user.Id} takes {reflectDamage} reflected damage from {target.Id}'s Thorns! <color=#FFFF00>[Thorns]</color>";
                        messageColor = Color.red;
                        eventBus.RaiseUnitDamaged(user, effectMessage);
                    }
                    else
                    {
                        Debug.LogWarning($"CombatSceneComponent: No UnitAttackState for {target.Id} for ThornsInfection {tag}.");
                    }
                    break;
                case "MoraleShield":
                    if (targetState != null && !targetState.TempStats.ContainsKey("MoraleShield"))
                    {
                        targetState.TempStats["MoraleShield"] = (0, -1);
                        effectMessage = $"{target.Id} gains MoraleShield from {abilityId}! <color=#00FF00>[MoraleShield]</color>";
                        messageColor = Color.green;
                        eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
                    }
                    break;
                default:
                    Debug.LogWarning($"CombatSceneComponent: Unrecognized effect tag {tag} for {abilityId}.");
                    effectMessage = $"Unknown effect {tag} applied by {user.Id} on {target.Id} with {abilityId}!";
                    break;
            }
            if (!string.IsNullOrEmpty(effectMessage))
            {
                allCombatLogs.Add(effectMessage);
                eventBus.RaiseLogMessage(effectMessage, messageColor);
            }
        }

        public void ApplyMoraleDamage(CharacterStats user, CharacterStats target, int moraleLoss, string abilityId)
        {
            if (target == null) return;
            var targetState = GetUnitAttackState(target);
            bool shielded = false;
            if (targetState != null && targetState.TempStats.TryGetValue("MoraleShield", out var shield))
            {
                targetState.TempStats.Remove("MoraleShield");
                shielded = true;
                string shieldMessage = $"{target.Id}’s MoraleShield absorbs {moraleLoss} Morale loss from {user.Id}’s {abilityId}!";
                allCombatLogs.Add(shieldMessage);
                eventBus.RaiseLogMessage(shieldMessage, uiConfig.TextColor);
                eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
            }
            if (!shielded)
            {
                target.Morale = Mathf.Max(0, target.Morale - moraleLoss);
                string moraleMessage = $"{user.Id} reduces {target.Id}'s Morale by {moraleLoss}! <color=#FFFF00>[Morale: {target.Morale}/{target.MaxMorale}]</color>";
                allCombatLogs.Add(moraleMessage);
                eventBus.RaiseLogMessage(moraleMessage, Color.yellow);
                eventBus.RaiseUnitUpdated(target, target.GetDisplayStats());
            }
        }

        public void ApplyAttackDamage(CharacterStats user, CharacterStats target, CombatTypes.AttackParams attackParams, string abilityId)
        {
            if (target == null) return;
            var targetState = GetUnitAttackState(target);
            int originalDefense = target.Defense;
            int currentEvasion = target.Evasion;
            if (targetState != null)
            {
                if (targetState.TempStats.TryGetValue("Defense", out var defMod)) target.Defense += defMod.value;
                if (targetState.TempStats.TryGetValue("Evasion", out var evaMod)) target.Evasion += evaMod.value;
            }
            bool attackDodged = false;
            if (attackParams.Dodgeable)
            {
                float dodgeChance = Mathf.Clamp(currentEvasion, 0, 100) / 100f;
                float randomRoll = UnityEngine.Random.value;
                if (randomRoll <= dodgeChance)
                {
                    string dodgeMessage = $"{target.Id} dodges the attack! <color=#FFFF00>[{currentEvasion}% Evasion Chance, Roll: {randomRoll:F2} <= {dodgeChance:F2}]</color>";
                    allCombatLogs.Add(dodgeMessage);
                    eventBus.RaiseLogMessage(dodgeMessage, Color.green);
                    attackDodged = true;
                }
                else
                {
                    string failDodgeMessage = $"{target.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance, Roll: {randomRoll:F2} > {dodgeChance:F2}]</color>";
                    allCombatLogs.Add(failDodgeMessage);
                    eventBus.RaiseLogMessage(failDodgeMessage, Color.red);
                }
            }
            if (!attackDodged)
            {
                int damage = 0;
                if (attackParams.Defense == CombatTypes.DefenseCheck.Standard)
                    damage = Mathf.Max(0, Mathf.RoundToInt(user.Attack * (1f - 0.05f * target.Defense)));
                else if (attackParams.Defense == CombatTypes.DefenseCheck.Partial)
                    damage = Mathf.Max(0, Mathf.RoundToInt(user.Attack * (1f - attackParams.PartialDefenseMultiplier * target.Defense)));
                else if (attackParams.Defense == CombatTypes.DefenseCheck.None)
                    damage = Mathf.Max(0, user.Attack);
                string damageFormula = $"[{user.Attack} ATK - {target.Defense} DEF * {(attackParams.Defense == CombatTypes.DefenseCheck.Partial ? attackParams.PartialDefenseMultiplier : 0.05f) * 100}%]";
                if (damage > 0)
                {
                    target.Health -= damage;
                    string damageMessage = $"{user.Id} hits {target.Id} for {damage} damage with {abilityId} <color=#FFFF00>{damageFormula}</color>";
                    allCombatLogs.Add(damageMessage);
                    eventBus.RaiseLogMessage(damageMessage, uiConfig.TextColor);
                    eventBus.RaiseUnitDamaged(target, damageMessage);
                    UpdateUnit(target, damageMessage);
                }
            }
            target.Defense = originalDefense;
        }

        private IEnumerator ProcessAttack(ICombatUnit unit, PartyData partyData, List<ICombatUnit> targets)
        {
            if (unit == null || unit.Health <= 0 || unit.HasRetreated) yield break;
            if (unit is not CharacterStats stats) yield break;
            var state = GetUnitAttackState(unit);
            if (state == null) yield break;

            var (abilityId, failMessage) = AbilityDatabase.SelectAbility(stats, partyData, targets, state);
            if (abilityId == null)
            {
                if (!string.IsNullOrEmpty(failMessage))
                {
                    allCombatLogs.Add(failMessage);
                    eventBus.RaiseLogMessage(failMessage, Color.red);
                }
                yield break;
            }

            var ability = stats.Type == CharacterType.Hero ? AbilityDatabase.GetHeroAbility(abilityId) : AbilityDatabase.GetMonsterAbility(abilityId);
            if (ability == null)
            {
                string noAbilityMessage = $"{stats.Id} cannot use {abilityId}: ability not found!";
                allCombatLogs.Add(noAbilityMessage);
                eventBus.RaiseLogMessage(noAbilityMessage, Color.red);
                yield break;
            }

            string abilityMessage = $"{stats.Id} uses {abilityId}!";
            allCombatLogs.Add(abilityMessage);
            eventBus.RaiseLogMessage(abilityMessage, uiConfig.TextColor);
            eventBus.RaiseUnitAttacking(unit, null, abilityId);
            eventBus.RaiseAbilitySelected(new EventBusSO.AttackData { attacker = unit, target = null, abilityId = abilityId });

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

            var rule = ability.GetTargetingRule();
            var isMelee = rule.MeleeOnly;
            // Determine target type from ability conditions (default to Enemy if none specified)
            var targetType = ability.Conditions.FirstOrDefault(c => c.Target != CombatTypes.ConditionTarget.User).Target;
            if (targetType == default) targetType = CombatTypes.ConditionTarget.Enemy; // Fallback for abilities like BasicAttack
            var selectedTargets = SelectTargets(stats, targets, partyData, rule, isMelee, targetType);
            if (selectedTargets.Count == 0)
            {
                string noTargetMessage = $"No legal targets for {abilityId} by {stats.Id}.";
                allCombatLogs.Add(noTargetMessage);
                eventBus.RaiseLogMessage(noTargetMessage, Color.red);
                yield break;
            }

            ability.Execute(stats, partyData, selectedTargets, this);

            var processedTargets = new HashSet<ICombatUnit>();
            foreach (var target in selectedTargets.ToList())
            {
                if (processedTargets.Contains(target)) continue;
                processedTargets.Add(target);
                if (target.Health <= 0)
                {
                    if (!allCombatLogs.Contains($"{target.Id} dies!"))
                    {
                        eventBus.RaiseUnitDied(target);
                        string deathMessage = $"{target.Id} dies!";
                        allCombatLogs.Add(deathMessage);
                        eventBus.RaiseLogMessage(deathMessage, Color.red);
                        UpdateUnit(target, deathMessage);
                        if (target is CharacterStats statsTarget)
                        {
                            if (statsTarget.Type == CharacterType.Hero)
                                heroPositions.Remove(statsTarget);
                            else
                                monsterPositions.Remove(statsTarget);
                        }
                    }
                }
                else if (partyData.CheckRetreat(target, eventBus, uiConfig, combatConfig))
                {
                    partyData.ProcessRetreat(target, eventBus, uiConfig, allCombatLogs, combatConfig);
                    UpdateUnit(target);
                }
            }

            if (stats.Health <= 0)
            {
                if (!allCombatLogs.Contains($"{stats.Id} dies!"))
                {
                    eventBus.RaiseUnitDied(unit);
                    string deathMessage = $"{stats.Id} dies!";
                    allCombatLogs.Add(deathMessage);
                    eventBus.RaiseLogMessage(deathMessage, Color.red);
                    UpdateUnit(unit, deathMessage);
                    if (stats.Type == CharacterType.Hero)
                        heroPositions.Remove(stats);
                    else
                        monsterPositions.Remove(stats);
                }
            }
            else if (partyData.CheckRetreat(unit, eventBus, uiConfig, combatConfig))
            {
                partyData.ProcessRetreat(unit, eventBus, uiConfig, allCombatLogs, combatConfig);
                UpdateUnit(unit);
            }

            stats.Attack = originalAttack;
            stats.Speed = originalSpeed;
            stats.Evasion = originalEvasion;
            UpdateUnit(unit);
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
    }
}