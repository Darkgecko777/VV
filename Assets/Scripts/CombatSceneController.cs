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
        private List<string> allCombatLogs = new List<string>(); // Tracks all logs per combat
        public static CombatSceneController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            units.Clear();
            heroPositions.Clear();
            monsterPositions.Clear();
            unitAttackStates.Clear();
            isCombatActive = false;
            roundNumber = 0;
            allCombatLogs.Clear(); // Clear logs at awake
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

        public static UnitAttackState GetUnitAttackState(ICombatUnit unit)
        {
            return Instance.unitAttackStates.Find(s => s.Unit == unit);
        }

        public static List<CharacterStats> GetMonsterUnits()
        {
            return Instance.monsterPositions;
        }

        private string SelectAbility(CharacterStats unit, PartyData partyData, List<ICombatUnit> targets)
        {
            var data = unit.Type == CharacterType.Hero
                ? CharacterLibrary.GetHeroData(unit.Id)
                : CharacterLibrary.GetMonsterData(unit.Id);
            var state = unitAttackStates.Find(s => s.Unit == unit);
            if (state == null) return "BasicAttack";
            foreach (var abilityId in data.AbilityIds)
            {
                var ability = unit.Type == CharacterType.Hero
                    ? AbilityDatabase.GetHeroAbility(abilityId)
                    : AbilityDatabase.GetMonsterAbility(abilityId);
                int cd = 0;
                if (ability.HasValue && (!state.AbilityCooldowns.TryGetValue(abilityId, out cd) || cd <= 0))
                {
                    if (ability.Value.UseCondition(unit, partyData, targets))
                    {
                        return abilityId;
                    }
                }
            }
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
            List<ICombatUnit> selectedTargets;
            if (ability.Value.IsMultiTarget && abilityId == "SludgeSlam")
            {
                selectedTargets = targets.Where(t => t is CharacterStats cs && (cs.PartyPosition == 1 || cs.PartyPosition == 2) && t.Health > 0 && !t.HasRetreated).ToList();
            }
            else if (!ability.Value.IsMelee)
            {
                selectedTargets = targets.Where(t => t.Health > 0 && !t.HasRetreated).ToList();
                var target = GetRandomAliveTarget(selectedTargets);
                selectedTargets = target != null ? new List<ICombatUnit> { target } : new List<ICombatUnit>();
            }
            else
            {
                selectedTargets = targets.Take(2).Where(t => t.Health > 0 && !t.HasRetreated).ToList();
                var target = GetRandomAliveTarget(selectedTargets);
                selectedTargets = target != null ? new List<ICombatUnit> { target } : new List<ICombatUnit>();
            }
            if (selectedTargets.Count == 0)
            {
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
                if (state.TempStats.TryGetValue("Evasion", out var evasionMod)) stats.Evasion += evasionMod.value;
            }
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
                eventBus.RaiseUnitAttacking(unit, target, abilityId);
                yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
                // Evasion check
                if (ability.Value.CanDodge && targetStats != null)
                {
                    float dodgeChance = Mathf.Clamp(currentEvasion, 0, 100) / 100f;
                    string dodgeMessage = $"{targetStats.Id} dodges the attack! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>";
                    if (Random.value <= dodgeChance)
                    {
                        allCombatLogs.Add(dodgeMessage);
                        eventBus.RaiseLogMessage(dodgeMessage, Color.white);
                        continue;
                    }
                    allCombatLogs.Add($"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>");
                    eventBus.RaiseLogMessage($"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>", Color.white);
                }
                // Damage calculation with new formula
                bool ignoreDEF = abilityId == "BasicAttack" && unit.Id == "Mire Shambler" || abilityId == "ThornNeedle" || abilityId == "Entangle" || abilityId == "ChiStrike";
                int damage = 0;
                string damageFormula = "";
                if (ignoreDEF)
                {
                    if (abilityId == "BasicAttack" && unit.Id == "Mire Shambler" || abilityId == "Entangle")
                    {
                        damage = 8;
                        damageFormula = "[Fixed 8 DMG]";
                    }
                    else if (abilityId == "ThornNeedle")
                    {
                        damage = 6;
                        damageFormula = "[Fixed 6 DMG]";
                    }
                    else if (abilityId == "ChiStrike")
                    {
                        damage = Mathf.Max(0, Mathf.RoundToInt(stats.Attack * (1f - 0.025f * (targetStats != null ? targetStats.Defense : 0))));
                        damageFormula = $"[{stats.Attack} ATK - {targetStats?.Defense ?? 0} DEF * 2.5%]";
                    }
                }
                else if (abilityId == "BasicAttack" || abilityId.EndsWith("Claw") || abilityId.EndsWith("Strike") || abilityId.EndsWith("Slash") || abilityId.EndsWith("Bite"))
                {
                    damage = Mathf.Max(0, Mathf.RoundToInt(stats.Attack * (1f - 0.05f * (targetStats != null ? targetStats.Defense : 0))));
                    damageFormula = $"[{stats.Attack} ATK - {targetStats?.Defense ?? 0} DEF * 5%]";
                }
                if (damage > 0)
                {
                    target.Health -= damage;
                    string damageMessage = $"{stats.Id} hits {targetStats?.Id} for {damage} damage with {abilityId} <color=#FFFF00>{damageFormula}</color>";
                    allCombatLogs.Add(damageMessage);
                    UpdateUnit(target, damageMessage);
                    // Infection check for monster attacks on heroes
                    if (stats.Type == CharacterType.Monster && targetStats != null)
                    {
                        float infectionCarryChance = stats.Infectivity / 100f;
                        float resistanceChance = targetStats.Infectivity / 100f;
                        string infectionMessage = "";
                        if (Random.value <= infectionCarryChance)
                        {
                            if (Random.value > resistanceChance && !targetStats.IsInfected)
                            {
                                targetStats.IsInfected = true;
                                infectionMessage = $"{targetStats.Id} is infected! <color=#FFFF00>[{stats.Infectivity}% Infection vs {targetStats.Infectivity}% Resistance]</color>";
                                allCombatLogs.Add(infectionMessage);
                                eventBus.RaiseUnitInfected(targetStats, "Virus");
                                UpdateUnit(targetStats, infectionMessage);
                            }
                            else
                            {
                                infectionMessage = $"{targetStats.Id} resists infection! <color=#FFFF00>[{stats.Infectivity}% Infection vs {targetStats.Infectivity}% Resistance]</color>";
                                allCombatLogs.Add(infectionMessage);
                                eventBus.RaiseLogMessage(infectionMessage, Color.white);
                            }
                        }
                    }
                }
                // Specific ability effects
                if (abilityId == "IronResolve")
                {
                    if (state != null)
                    {
                        state.SkipNextAttack = true;
                        state.TempStats["Defense"] = (5, 1);
                        stats.Morale = Mathf.Min(stats.Morale + 15, stats.MaxMorale);
                        string message = $"{unit.Id} bolsters defense and morale but will skip next attack! <color=#FFFF00>[+5 DEF, +15 Morale]</color>";
                        allCombatLogs.Add(message);
                        eventBus.RaiseLogMessage(message, Color.white);
                    }
                }
                else if (abilityId == "ChiStrike")
                {
                    if (state != null)
                    {
                        state.TempStats["Evasion"] = (5, 1);
                        string message = $"{unit.Id} gains +5 Evasion! <color=#FFFF00>[+5 EVA]</color>";
                        allCombatLogs.Add(message);
                        eventBus.RaiseLogMessage(message, Color.white);
                    }
                }
                else if (abilityId == "InnerFocus")
                {
                    if (state != null)
                    {
                        state.TempStats["Speed"] = (3, 1);
                        stats.Morale = Mathf.Min(stats.Morale + 10, stats.MaxMorale);
                        stats.Health -= 5;
                        string message = $"{unit.Id} boosts speed and morale but takes 5 damage! <color=#FFFF00>[+3 SPD, +10 Morale, -5 HP]</color>";
                        allCombatLogs.Add(message);
                        UpdateUnit(unit, message);
                        if (stats.Health <= 0)
                        {
                            eventBus.RaiseUnitDied(unit);
                        }
                    }
                }
                else if (abilityId == "SniperShot")
                {
                    if (targetState != null && targetStats != null)
                    {
                        int evasionReduction = targetStats.Evasion / 4;
                        targetState.TempStats["Evasion"] = (-evasionReduction, 1);
                        string message = $"{targetStats.Id}'s Evasion reduced by 25%! <color=#FFFF00>[-{evasionReduction} EVA]</color>";
                        allCombatLogs.Add(message);
                        eventBus.RaiseLogMessage(message, Color.white);
                    }
                }
                else if (abilityId == "HealerHeal")
                {
                    var lowestHealthAlly = partyData.FindLowestHealthAlly();
                    if (lowestHealthAlly != null)
                    {
                        int oldHealth = lowestHealthAlly.Health;
                        lowestHealthAlly.Health = Mathf.Min(lowestHealthAlly.Health + 10, lowestHealthAlly.MaxHealth);
                        lowestHealthAlly.Morale = Mathf.Min(lowestHealthAlly.Morale + 5, lowestHealthAlly.MaxMorale);
                        string message;
                        if (lowestHealthAlly.IsInfected)
                        {
                            lowestHealthAlly.IsInfected = false;
                            message = $"Healer heals {lowestHealthAlly.Id} for {lowestHealthAlly.Health - oldHealth} HP, +5 Morale, and cures infection! <color=#FFFF00>[+{lowestHealthAlly.Health - oldHealth} HP, +5 Morale]</color>";
                            allCombatLogs.Add(message);
                            eventBus.RaiseLogMessage(message, Color.white);
                        }
                        else
                        {
                            message = $"Healer heals {lowestHealthAlly.Id} for {lowestHealthAlly.Health - oldHealth} HP and +5 Morale! <color=#FFFF00>[+{lowestHealthAlly.Health - oldHealth} HP, +5 Morale]</color>";
                            allCombatLogs.Add(message);
                            eventBus.RaiseLogMessage(message, Color.white);
                        }
                        UpdateUnit(lowestHealthAlly);
                    }
                }
                else if (abilityId == "BasicAttack" && unit.Id == "Bog Fiend")
                {
                    if (targetState != null && targetStats != null)
                    {
                        targetState.TempStats["Evasion"] = (5, 1);
                    }
                    foreach (var hero in heroPositions)
                    {
                        hero.Morale = Mathf.Max(0, hero.Morale - 5);
                        string message = $"{hero.Id}'s morale drops by 5! <color=#FFFF00>[-5 Morale]</color>";
                        allCombatLogs.Add(message);
                        UpdateUnit(hero, message);
                    }
                }
                else if (abilityId == "MireGrasp")
                {
                    if (targetState != null && targetStats != null)
                    {
                        targetState.TempStats["Speed"] = (-3, 1);
                        targetStats.Morale = Mathf.Max(0, targetStats.Morale - 5);
                        string message = $"{targetStats.Id}'s Speed reduced by 3 and Morale by 5! <color=#FFFF00>[-3 SPD, -5 Morale]</color>";
                        allCombatLogs.Add(message);
                        UpdateUnit(targetStats, message);
                    }
                }
                else if (abilityId == "Entangle")
                {
                    if (targetState != null)
                    {
                        targetState.SkipNextAttack = true;
                        string message = $"{target.Id} is entangled and will skip their next attack! <color=#FFFF00>[Skip Next Attack]</color>";
                        allCombatLogs.Add(message);
                        eventBus.RaiseLogMessage(message, Color.white);
                    }
                }
                else if (abilityId == "ShriekOfDespair")
                {
                    foreach (var hero in heroPositions)
                    {
                        hero.Morale = Mathf.Max(0, hero.Morale - 10);
                        string message = $"{hero.Id}'s morale drops by 10! <color=#FFFF00>[-10 Morale]</color>";
                        allCombatLogs.Add(message);
                        UpdateUnit(hero, message);
                    }
                }
                else if (abilityId == "FlocksVigor")
                {
                    foreach (var monster in monsterPositions)
                    {
                        var monsterState = unitAttackStates.Find(s => s.Unit == monster);
                        if (monsterState != null)
                        {
                            monsterState.TempStats["Speed"] = (3, 1);
                            monsterState.TempStats["Evasion"] = (10, 1);
                        }
                    }
                    string message = "All monsters gain +3 Speed and +10 Evasion! <color=#FFFF00>[+3 SPD, +10 EVA]</color>";
                    allCombatLogs.Add(message);
                    eventBus.RaiseLogMessage(message, Color.white);
                }
                else if (abilityId == "TrueStrike")
                {
                    if (state != null)
                    {
                        state.TempStats["Evasion"] = (15, 1);
                    }
                    if (targetStats != null)
                    {
                        targetStats.Morale = Mathf.Max(0, targetStats.Morale - 5);
                        string message = $"{targetStats.Id}'s Morale drops by 5! <color=#FFFF00>[-5 Morale]</color>";
                        allCombatLogs.Add(message);
                        UpdateUnit(targetStats, message);
                    }
                }
                else if (abilityId == "SpectralDrain")
                {
                    if (state != null)
                    {
                        state.TempStats["Evasion"] = (15, 1);
                    }
                    if (targetState != null && targetStats != null)
                    {
                        targetState.TempStats["Defense"] = (-5, 1);
                        string message = $"{targetStats.Id}'s Defense reduced by 5! <color=#FFFF00>[-5 DEF]</color>";
                        allCombatLogs.Add(message);
                        UpdateUnit(targetStats, message);
                    }
                }
                else if (abilityId == "EtherealWail")
                {
                    if (state != null)
                    {
                        state.TempStats["Evasion"] = (15, 1);
                    }
                    foreach (var hero in heroPositions)
                    {
                        hero.Morale = Mathf.Max(0, hero.Morale - 8);
                        string message = $"{hero.Id}'s morale drops by 8! <color=#FFFF00>[-8 Morale]</color>";
                        allCombatLogs.Add(message);
                        UpdateUnit(hero, message);
                    }
                }
                // Self-Damage
                if (unit.Id == "Wraith" || (abilityId == "Entangle" && unit.Id == "Mire Shambler"))
                {
                    int selfDamage = 8;
                    if (abilityId == "Entangle") selfDamage = 10;
                    stats.Health -= selfDamage;
                    string message = $"{unit.Id} takes {selfDamage} self-damage! <color=#FFFF00>[-{selfDamage} HP]</color>";
                    allCombatLogs.Add(message);
                    UpdateUnit(unit, message);
                    if (stats.Health <= 0)
                    {
                        eventBus.RaiseUnitDied(unit);
                    }
                }
                if (targetStats != null) targetStats.Defense = originalDefense;
                if (target.Health <= 0)
                {
                    eventBus.RaiseUnitDied(target);
                }
            }
            stats.Attack = originalAttack;
            stats.Speed = originalSpeed;
            stats.Evasion = originalEvasion;
            if (abilityId != "BasicAttack")
            {
                if (state != null)
                {
                    state.AbilityCooldowns[abilityId] = 1;
                }
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
                        }
                    }
                    foreach (var tempStat in state.TempStats.ToList())
                    {
                        state.TempStats[tempStat.Key] = (tempStat.Value.value, tempStat.Value.duration - 1);
                        if (tempStat.Value.duration <= 0)
                        {
                            state.TempStats.Remove(tempStat.Key);
                            if (unit is CharacterStats stats)
                            {
                                var baseData = stats.Type == CharacterType.Hero
                                    ? CharacterLibrary.GetHeroData(stats.Id)
                                    : CharacterLibrary.GetMonsterData(stats.Id);
                                if (tempStat.Key == "Defense") stats.Defense = baseData.Defense;
                                else if (tempStat.Key == "Speed") stats.Speed = baseData.Speed;
                                else if (tempStat.Key == "Evasion") stats.Evasion = baseData.Evasion;
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
                        string retreatMessage = $"{stats.Id} fled! <color=#FFFF00>[Morale <= {combatConfig.RetreatMoraleThreshold}]</color>";
                        allCombatLogs.Add(retreatMessage);
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
            allCombatLogs.Clear(); // Clear logs at combat start
            foreach (var hero in heroStats.Where(h => h.Type == CharacterType.Hero && h.Health > 0 && !h.HasRetreated))
            {
                var stats = hero.GetDisplayStats();
                units.Add((hero, null, stats));
                heroPositions.Add(hero);
                unitAttackStates.Add(new UnitAttackState { Unit = hero, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int, int)>() });
            }
            foreach (var monster in monsterStats.Where(m => m.Type == CharacterType.Monster && m.Health > 0 && !m.HasRetreated))
            {
                var stats = monster.GetDisplayStats();
                units.Add((monster, null, stats));
                monsterPositions.Add(monster);
                unitAttackStates.Add(new UnitAttackState { Unit = monster, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int, int)>() });
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
                string retreatMessage = $"{stats.Id} fled! <color=#FFFF00>[Morale <= {combatConfig.RetreatMoraleThreshold}]</color>";
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