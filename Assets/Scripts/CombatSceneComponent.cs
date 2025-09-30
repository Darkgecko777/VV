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
                    Debug.LogError($"CombatSceneComponent: No abilities defined for hero {hero.Id}.");
                    hero.abilityIds = new string[] { "BasicAttack" }; // Fixed: hero to heroStats
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
                    Debug.LogError($"CombatSceneComponent: No abilities defined for monster {monster.Id}.");
                    monster.abilityIds = new string[] { "BasicAttack" }; // Fixed: hero to monster
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
            if (targetType == default)
            {
                Debug.LogWarning($"CombatSceneComponent: TargetType not set for {user.Id}. Defaulting to Enemy.");
                targetType = CombatTypes.ConditionTarget.Enemy;
            }
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
                    bool isInfected = stats != null && stats.IsInfected;
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
                    yield return ExecuteAbility(unit, expeditionData.Party, unitList);
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
                        yield return ExecuteAbility(unit, expeditionData.Party, unitList);
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

        private IEnumerator ExecuteAbility(ICombatUnit unit, PartyData partyData, List<ICombatUnit> targets)
        {
            var stats = unit as CharacterStats;
            var state = GetUnitAttackState(unit);
            if (stats == null || state == null)
            {
                Debug.LogWarning($"ExecuteAbility: Invalid stats or state for unit {unit?.Id}");
                yield break;
            }
            foreach (var abilityId in stats.abilityIds)
            {
                var ability = AbilityDatabase.GetAbility(abilityId) as AbilitySO;
                if (ability == null)
                {
                    Debug.LogWarning($"ExecuteAbility: Ability {abilityId} not found for {stats.Id}");
                    continue;
                }
                if (state.AbilityCooldowns.GetValueOrDefault(abilityId, 0) > 0 || state.RoundCooldowns.GetValueOrDefault(abilityId, 0) > 0)
                {
                    Debug.Log($"ExecuteAbility: {abilityId} on cooldown for {stats.Id}");
                    continue;
                }
                if (stats.Rank < ability.Rank)
                {
                    Debug.Log($"ExecuteAbility: {stats.Id} rank {stats.Rank} too low for {abilityId} (requires {ability.Rank})");
                    continue;
                }
                if (ability.Conditions.All(c => AbilityDatabase.EvaluateCondition(c, stats, partyData, targets)))
                {
                    var rule = ability.GetTargetingRule();
                    var selectedTargets = SelectTargets(stats, targets, partyData, rule, ability.Action.Melee, ability.Action.Target);
                    if (selectedTargets.Any())
                    {
                        string abilityMessage = $"{stats.Id} uses {abilityId}!";
                        allCombatLogs.Add(abilityMessage);
                        eventBus.RaiseLogMessage(abilityMessage, uiConfig.TextColor);
                        eventBus.RaiseUnitAttacking(unit, null, abilityId);
                        eventBus.RaiseAbilitySelected(new EventBusSO.AttackData { attacker = unit, target = null, abilityId = abilityId });
                        yield return new WaitUntil(() => !isPaused);
                        yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
                        if (ability.Action.Defense != CombatTypes.DefenseCheck.None)
                        {
                            ApplyAttackDamage(stats, selectedTargets, ability.Action, abilityId);
                        }
                        else
                        {
                            ProcessAction(stats, selectedTargets, ability.Action, abilityId);
                        }
                        if (ability.Cooldown > 0)
                        {
                            var cds = ability.CooldownType == CombatTypes.CooldownType.Actions ? state.AbilityCooldowns : state.RoundCooldowns;
                            cds[abilityId] = ability.Cooldown;
                            Debug.Log($"ExecuteAbility: Applied cooldown for {abilityId}: {ability.Cooldown} {ability.CooldownType}");
                        }
                        foreach (var target in selectedTargets.ToList())
                        {
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
                                partyData.ProcessRetreat(target, eventBus, uiConfig, allCombatLogs, combatConfig); // Fixed: Added allCombatLogs
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
                            partyData.ProcessRetreat(unit, eventBus, uiConfig, allCombatLogs, combatConfig); // Fixed: Added allCombatLogs
                            UpdateUnit(unit);
                        }
                        yield break;
                    }
                    else
                    {
                        string noTargetMessage = $"No legal targets for {abilityId} by {stats.Id}.";
                        allCombatLogs.Add(noTargetMessage);
                        eventBus.RaiseLogMessage(noTargetMessage, Color.red);
                    }
                }
            }
            // Fallback: Last ability (BasicAttack)
            var fallbackId = stats.abilityIds.Last();
            var fallback = AbilityDatabase.GetAbility(fallbackId) as AbilitySO;
            if (fallback != null)
            {
                var rule = fallback.GetTargetingRule();
                var selectedTargets = SelectTargets(stats, targets, partyData, rule, fallback.Action.Melee, fallback.Action.Target);
                if (selectedTargets.Any())
                {
                    string abilityMessage = $"{stats.Id} uses fallback {fallbackId}!";
                    allCombatLogs.Add(abilityMessage);
                    eventBus.RaiseLogMessage(abilityMessage, uiConfig.TextColor);
                    eventBus.RaiseUnitAttacking(unit, null, fallbackId);
                    eventBus.RaiseAbilitySelected(new EventBusSO.AttackData { attacker = unit, target = null, abilityId = fallbackId });
                    yield return new WaitUntil(() => !isPaused);
                    yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
                    if (fallback.Action.Defense != CombatTypes.DefenseCheck.None)
                    {
                        ApplyAttackDamage(stats, selectedTargets, fallback.Action, fallbackId);
                    }
                    else
                    {
                        ProcessAction(stats, selectedTargets, fallback.Action, fallbackId);
                    }
                    foreach (var target in selectedTargets.ToList())
                    {
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
                            partyData.ProcessRetreat(target, eventBus, uiConfig, allCombatLogs, combatConfig); // Fixed: Added allCombatLogs
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
                        partyData.ProcessRetreat(unit, eventBus, uiConfig, allCombatLogs, combatConfig); // Fixed: Added allCombatLogs
                        UpdateUnit(unit);
                    }
                }
                else
                {
                    string noTargetMessage = $"No legal targets for fallback {fallbackId} by {stats.Id}.";
                    allCombatLogs.Add(noTargetMessage);
                    eventBus.RaiseLogMessage(noTargetMessage, Color.red);
                }
            }
            yield return new WaitForSeconds(0.2f / (combatConfig?.CombatSpeed ?? 1f));
        }

        private void ApplyAttackDamage(CharacterStats user, List<ICombatUnit> targets, AbilitySO.AbilityAction action, string abilityId)
        {
            foreach (var target in targets.ToList())
            {
                var targetStats = target as CharacterStats;
                var targetState = GetUnitAttackState(target);
                if (targetStats == null || targetState == null) continue;
                int originalDefense = targetStats.Defense;
                int currentEvasion = targetStats.Evasion;
                if (targetState.TempStats.TryGetValue("Defense", out var defMod)) targetStats.Defense += defMod.value;
                if (targetState.TempStats.TryGetValue("Evasion", out var evaMod)) targetStats.Evasion += evaMod.value;
                bool attackDodged = false;
                if (action.Dodgeable)
                {
                    float dodgeChance = Mathf.Clamp(currentEvasion, 0, 100) / 100f;
                    float randomRoll = UnityEngine.Random.value;
                    if (randomRoll <= dodgeChance)
                    {
                        string dodgeMessage = $"{targetStats.Id} dodges the attack! <color=#FFFF00>[{currentEvasion}% Evasion Chance, Roll: {randomRoll:F2} <= {dodgeChance:F2}]</color>";
                        allCombatLogs.Add(dodgeMessage);
                        eventBus.RaiseLogMessage(dodgeMessage, Color.green);
                        attackDodged = true;
                    }
                    else
                    {
                        string failDodgeMessage = $"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance, Roll: {randomRoll:F2} > {dodgeChance:F2}]</color>";
                        allCombatLogs.Add(failDodgeMessage);
                        eventBus.RaiseLogMessage(failDodgeMessage, Color.red);
                    }
                }
                int damage = 0;
                if (!attackDodged)
                {
                    if (action.Defense == CombatTypes.DefenseCheck.Standard)
                        damage = Mathf.Max(0, Mathf.RoundToInt(user.Attack * (1f - 0.05f * targetStats.Defense)));
                    else if (action.Defense == CombatTypes.DefenseCheck.Partial)
                        damage = Mathf.Max(0, Mathf.RoundToInt(user.Attack * (1f - action.PartialDefenseMultiplier * targetStats.Defense)));
                    else
                        damage = Mathf.Max(0, user.Attack);
                    string damageFormula = $"[{user.Attack} ATK - {targetStats.Defense} DEF * {(action.Defense == CombatTypes.DefenseCheck.Partial ? action.PartialDefenseMultiplier : 0.05f) * 100}%]";
                    if (damage > 0)
                    {
                        targetStats.Health -= damage;
                        string damageMessage = $"{user.Id} hits {targetStats.Id} for {damage} damage with {abilityId} <color=#FFFF00>{damageFormula}</color>";
                        allCombatLogs.Add(damageMessage);
                        eventBus.RaiseLogMessage(damageMessage, uiConfig.TextColor);
                        eventBus.RaiseUnitDamaged(target, damageMessage);
                        UpdateUnit(target, damageMessage);
                    }
                    // Check for Reflect buff
                    if (targetState.TempStats.TryGetValue("Reflect", out var reflect) && !attackDodged && damage > 0)
                    {
                        int reflectDamage = Mathf.RoundToInt(damage * reflect.value / 100f);
                        user.Health = Mathf.Max(0, user.Health - reflectDamage);
                        string reflectMessage = $"{user.Id} takes {reflectDamage} reflected damage from {targetStats.Id}!";
                        allCombatLogs.Add(reflectMessage);
                        eventBus.RaiseLogMessage(reflectMessage, Color.red);
                        eventBus.RaiseUnitDamaged(user, reflectMessage);
                        UpdateUnit(user, reflectMessage);
                    }
                }
                targetStats.Defense = originalDefense;
                targetStats.Evasion = currentEvasion;
            }
        }

        private void ProcessAction(CharacterStats user, List<ICombatUnit> targets, AbilitySO.AbilityAction action, string abilityId)
        {
            foreach (var target in targets)
            {
                var targetStats = target as CharacterStats;
                var targetState = GetUnitAttackState(target);
                if (targetStats == null || targetState == null) continue;
                EffectReference.Apply(action.EffectId, user, targetStats, action.EffectValue, action.EffectDuration, targetState, eventBus, uiConfig);
                UpdateUnit(targetStats);
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
    }
}