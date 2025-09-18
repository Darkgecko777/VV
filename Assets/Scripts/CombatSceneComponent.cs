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
        [SerializeField] public UIConfig uiConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private CombatEffectsComponent effectsComponent;
        [SerializeField] private Camera combatCamera;
        [SerializeField] private CombatTurnComponent turnComponent;
        private ExpeditionManager expeditionManager;
        private bool isEndingCombat;
        private List<(ICombatUnit unit, GameObject go, CharacterStats.DisplayStats displayStats)> units = new List<(ICombatUnit, GameObject, CharacterStats.DisplayStats)>();
        public List<CharacterStats> heroPositions = new List<CharacterStats>();
        public List<CharacterStats> monsterPositions = new List<CharacterStats>();
        private bool isCombatActive;
        private bool isPaused;
        private List<UnitAttackState> unitAttackStates = new List<UnitAttackState>();
        public List<string> allCombatLogs = new List<string>();
        public static CombatSceneComponent Instance { get; private set; }
        public bool IsPaused => isPaused;
        public List<string> AllCombatLogs => allCombatLogs;
        public EventBusSO EventBus => eventBus;
        public UIConfig UIConfig => uiConfig;

        void Awake()
        {
            Instance = this;
            units.Clear();
            heroPositions.Clear();
            monsterPositions.Clear();
            unitAttackStates.Clear();
            isCombatActive = false;
            isPaused = false;
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
            eventBus.OnCombatPaused += () => { isPaused = true; };
            eventBus.OnCombatPlayed += () => { isPaused = false; };
            StartCoroutine(RunCombat());
        }

        void OnDestroy()
        {
            eventBus.OnCombatEnded -= EndCombat;
            eventBus.OnCombatPaused -= () => { isPaused = true; };
            eventBus.OnCombatPlayed -= () => { isPaused = false; };
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
            eventBus.RaiseCombatPaused();
        }

        public void PlayCombat()
        {
            isPaused = false;
            eventBus.RaiseCombatPlayed();
        }

        private IEnumerator ProcessAttack(ICombatUnit unit, PartyData partyData, List<ICombatUnit> targets)
        {
            if (unit is not CharacterStats stats) yield break;
            var state = unitAttackStates.Find(s => s.Unit == unit);
            if (state == null) yield break;
            string abilityId = AbilityDatabase.SelectAbility(stats, partyData, targets, state);
            AbilitySO ability = stats.Abilities.FirstOrDefault(a => a is AbilitySO so && so.Id == abilityId) as AbilitySO;
            string abilityMessage = $"{stats.Id} uses {abilityId}!";
            allCombatLogs.Add(abilityMessage);
            eventBus.RaiseLogMessage(abilityMessage, uiConfig.TextColor);
            eventBus.RaiseUnitAttacking(unit, null, abilityId);
            eventBus.RaiseAbilitySelected(new EventBusSO.AttackData { attacker = unit, target = null, abilityId = abilityId });

            List<ICombatUnit> selectedTargets = new List<ICombatUnit>();
            if (ability != null)
            {
                int maxTargets = 1;
                foreach (var attack in ability.Attacks)
                    maxTargets = Mathf.Max(maxTargets, attack.Melee ? Mathf.Min(attack.NumberOfTargets, 2) : Mathf.Min(attack.NumberOfTargets, 4));
                foreach (var effect in ability.Effects)
                    maxTargets = Mathf.Max(maxTargets, effect.Melee ? Mathf.Min(effect.NumberOfTargets, 2) : Mathf.Min(effect.NumberOfTargets, 4));

                var orderedHeroes = heroPositions.Where(h => h.Health > 0 && !h.HasRetreated)
                    .OrderBy(h => h.PartyPosition).Select((h, i) => new { Unit = (ICombatUnit)h, CombatPosition = i + 1 }).ToList();
                var orderedMonsters = monsterPositions.Where(m => m.Health > 0 && !m.HasRetreated)
                    .OrderBy(m => m.PartyPosition).Select((m, i) => new { Unit = (ICombatUnit)m, CombatPosition = i + 1 }).ToList();

                var enemyTargets = stats.Type == CharacterType.Hero ? orderedMonsters.Select(m => m.Unit).ToList() : orderedHeroes.Select(h => h.Unit).ToList();
                var allyTargets = stats.Type == CharacterType.Hero ? orderedHeroes.Select(h => h.Unit).ToList() : orderedMonsters.Select(m => m.Unit).ToList();

                foreach (var attack in ability.Attacks)
                {
                    selectedTargets.AddRange(attack.TargetingRule.SelectTargets(stats, attack.Enemy ? enemyTargets : allyTargets, partyData, attack.Melee));
                    if (selectedTargets.Any())
                    {
                        string targetMessage = $"Attack {abilityId} targets {string.Join(", ", selectedTargets.Select(t => (t as CharacterStats).Id))}.";
                        allCombatLogs.Add(targetMessage);
                        eventBus.RaiseLogMessage(targetMessage, uiConfig.TextColor);
                    }
                }

                foreach (var effect in ability.Effects)
                {
                    var newTargets = effect.TargetingRule.SelectTargets(stats, effect.Enemy ? enemyTargets : allyTargets, partyData, effect.Melee);
                    if (effect.TargetingRule.Type == TargetingRule.RuleType.LowestHealth && effect.TargetingRule.Target == ConditionTarget.Ally && stats.Type == CharacterType.Hero)
                    {
                        var lowestHealthHero = partyData.FindLowestHealthAlly();
                        if (lowestHealthHero != null && lowestHealthHero.Health > 0 && !lowestHealthHero.HasRetreated)
                            newTargets = new List<ICombatUnit> { lowestHealthHero };
                    }
                    selectedTargets.AddRange(newTargets);
                    if (newTargets.Any())
                    {
                        string targetMessage = $"Effect {abilityId} targets {string.Join(", ", newTargets.Select(t => (t as CharacterStats).Id))}.";
                        allCombatLogs.Add(targetMessage);
                        eventBus.RaiseLogMessage(targetMessage, uiConfig.TextColor);
                    }
                }

                selectedTargets = selectedTargets.Distinct().Take(maxTargets).ToList();
            }
            else
            {
                var orderedHeroes = heroPositions.Where(h => h.Health > 0 && !h.HasRetreated)
                    .OrderBy(h => h.PartyPosition).Select((h, i) => new { Unit = (ICombatUnit)h, CombatPosition = i + 1 }).ToList();
                var orderedMonsters = monsterPositions.Where(m => m.Health > 0 && !m.HasRetreated)
                    .OrderBy(m => m.PartyPosition).Select((m, i) => new { Unit = (ICombatUnit)m, CombatPosition = i + 1 }).ToList();
                var enemyTargets = stats.Type == CharacterType.Hero ? orderedMonsters.Select(m => m.Unit).ToList() : orderedHeroes.Select(h => h.Unit).ToList();
                AbilitySO basicAttack = AbilityDatabase.GetHeroAbility("BasicAttack") ?? AbilityDatabase.GetMonsterAbility("BasicAttack");
                if (basicAttack != null && (basicAttack.Attacks.Any(a => a.Melee || a.TargetingRule.MeleeOnly)))
                    enemyTargets = enemyTargets.Where(t => (stats.Type == CharacterType.Hero
                        ? orderedMonsters.FirstOrDefault(m => m.Unit == t)?.CombatPosition
                        : orderedHeroes.FirstOrDefault(h => h.Unit == t)?.CombatPosition) <= 2).ToList();
                selectedTargets = basicAttack != null ? basicAttack.Attacks.First().TargetingRule.SelectTargets(stats, enemyTargets, partyData, true) : new List<ICombatUnit>();
            }

            if (!selectedTargets.Any())
            {
                string message = $"{stats.Id} finds no valid targets for {abilityId}! <color=#FFFF00>[No Targets]</color>";
                allCombatLogs.Add(message);
                eventBus.RaiseLogMessage(message, uiConfig.TextColor);
                if (NoActiveHeroes() || NoActiveMonsters())
                {
                    EndCombat();
                    yield break;
                }
                yield break;
            }

            var unitState = unitAttackStates.Find(s => s.Unit == unit);
            int originalAttack = stats.Attack;
            int originalSpeed = stats.Speed;
            int originalEvasion = stats.Evasion;
            if (unitState != null)
            {
                if (unitState.TempStats.TryGetValue("Attack", out var attackMod)) stats.Attack += attackMod.value;
                if (unitState.TempStats.TryGetValue("Speed", out var speedMod)) stats.Speed += speedMod.value;
                if (unitState.TempStats.TryGetValue("Evasion", out var evaMod)) stats.Evasion += evaMod.value;
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

                bool attackDodged = false;
                if (ability != null)
                {
                    foreach (var attack in ability.Attacks)
                    {
                        if ((attack.Enemy && targetStats.Type == stats.Type) || (!attack.Enemy && targetStats.Type != stats.Type && target != unit))
                            continue;
                        if (attack.Dodgeable)
                        {
                            float dodgeChance = Mathf.Clamp(currentEvasion, 0, 100) / 100f;
                            if (Random.value <= dodgeChance)
                            {
                                string dodgeMessage = $"{targetStats.Id} dodges the attack! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>";
                                allCombatLogs.Add(dodgeMessage);
                                eventBus.RaiseLogMessage(dodgeMessage, uiConfig.TextColor);
                                attackDodged = true;
                                continue;
                            }
                            string failDodgeMessage = $"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>";
                            allCombatLogs.Add(failDodgeMessage);
                            eventBus.RaiseLogMessage(failDodgeMessage, uiConfig.TextColor);
                        }
                        if (!attackDodged)
                        {
                            int damage = 0;
                            if (attack.Defense == DefenseCheck.Standard)
                                damage = Mathf.Max(0, Mathf.RoundToInt(stats.Attack * (1f - 0.05f * targetStats.Defense)));
                            else if (attack.Defense == DefenseCheck.Partial)
                                damage = Mathf.Max(0, Mathf.RoundToInt(stats.Attack * (1f - attack.PartialDefenseMultiplier * targetStats.Defense)));
                            else if (attack.Defense == DefenseCheck.None)
                                damage = Mathf.Max(0, stats.Attack);
                            string damageFormula = $"[{stats.Attack} ATK - {targetStats.Defense} DEF * {(attack.Defense == DefenseCheck.Partial ? attack.PartialDefenseMultiplier : 0.05f) * 100}%]";
                            if (damage > 0 && targetStats != null)
                            {
                                targetStats.Health -= damage;
                                string damageMessage = $"{stats.Id} hits {targetStats.Id} for {damage} damage with {abilityId} <color=#FFFF00>{damageFormula}</color>";
                                allCombatLogs.Add(damageMessage);
                                eventBus.RaiseLogMessage(damageMessage, uiConfig.TextColor);
                                eventBus.RaiseUnitDamaged(target, damageMessage);
                                UpdateUnit(target, damageMessage);
                            }
                        }
                    }
                    foreach (var effect in ability.Effects)
                    {
                        if ((effect.Enemy && targetStats.Type == stats.Type) || (!effect.Enemy && targetStats.Type != stats.Type && target != unit))
                            continue;
                        foreach (var tag in effect.Tags)
                            effectsComponent.ProcessEffect(stats, targetStats, tag, abilityId);
                    }
                }

                if (targetStats != null && target.Health <= 0)
                {
                    eventBus.RaiseUnitDied(target);
                    string deathMessage = $"{targetStats.Id} dies!";
                    allCombatLogs.Add(deathMessage);
                    eventBus.RaiseLogMessage(deathMessage, Color.red);
                    UpdateUnit(target, deathMessage);
                }
                if (stats.Health <= 0)
                {
                    eventBus.RaiseUnitDied(unit);
                    string deathMessage = $"{stats.Id} dies!";
                    allCombatLogs.Add(deathMessage);
                    eventBus.RaiseLogMessage(deathMessage, Color.red);
                    UpdateUnit(unit, deathMessage);
                }
                if (targetStats != null) targetStats.Defense = originalDefense;
            }

            stats.Attack = originalAttack;
            stats.Speed = originalSpeed;
            stats.Evasion = originalEvasion;
            UpdateUnit(unit);
            if (NoActiveHeroes() || NoActiveMonsters())
            {
                EndCombat();
                yield break;
            }
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
            turnComponent.IncrementRound();
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
                        state = new UnitAttackState { Unit = unit, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int value, int duration)>() };
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
                            if (unit is CharacterStats stats)
                            {
                                var baseData = stats.Type == CharacterType.Hero ? CharacterLibrary.GetHeroData(stats.Id) : CharacterLibrary.GetMonsterData(stats.Id);
                                if (tempStat.Key == "Defense")
                                {
                                    stats.Defense = baseData.Defense;
                                    string message = $"{stats.Id}'s Defense buff expires! <color=#FFFF00>[Restored to {stats.Defense}]</color>";
                                    allCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, uiConfig.TextColor);
                                }
                                else if (tempStat.Key == "Speed")
                                {
                                    stats.Speed = baseData.Speed;
                                    string message = $"{stats.Id}'s Speed buff expires! <color=#FFFF00>[Restored to {stats.Speed}]</color>";
                                    allCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, uiConfig.TextColor);
                                }
                                else if (tempStat.Key == "Evasion")
                                {
                                    stats.Evasion = baseData.Evasion;
                                    string message = $"{stats.Id}'s Evasion buff expires! <color=#FFFF00>[Restored to {stats.Evasion}]</color>";
                                    allCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, uiConfig.TextColor);
                                }
                                else if (tempStat.Key == "TauntedBy" || tempStat.Key == "ThornsReflect")
                                {
                                    string message = $"{stats.Id}'s {tempStat.Key} effect expires!";
                                    allCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, uiConfig.TextColor);
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
                    if (!turnComponent.CanAttackThisRound(unit, state)) continue;
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
                turnComponent.IncrementRound();
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
            eventBus.RaiseLogMessage(initMessage, uiConfig.TextColor);
            foreach (var hero in heroStats.Where(h => h.Type == CharacterType.Hero && h.Health > 0))
            {
                var stats = hero.GetDisplayStats();
                units.Add((hero, null, stats));
                heroPositions.Add(hero);
                unitAttackStates.Add(new UnitAttackState { Unit = hero, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int value, int duration)>() });
                string heroMessage = $"{hero.Id} enters combat with {hero.Health}/{hero.MaxHealth} HP, {hero.Morale}/{hero.MaxMorale} Morale.";
                allCombatLogs.Add(heroMessage);
                eventBus.RaiseLogMessage(heroMessage, uiConfig.TextColor);
            }
            foreach (var monster in monsterStats.Where(m => m.Type == CharacterType.Monster && m.Health > 0 && !m.HasRetreated))
            {
                var stats = monster.GetDisplayStats();
                units.Add((monster, null, stats));
                monsterPositions.Add(monster);
                unitAttackStates.Add(new UnitAttackState { Unit = monster, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int value, int duration)>() });
                string monsterMessage = $"{monster.Id} enters combat with {monster.Health}/{monster.MaxHealth} HP.";
                allCombatLogs.Add(monsterMessage);
                eventBus.RaiseLogMessage(monsterMessage, uiConfig.TextColor);
            }
            heroPositions = heroPositions.OrderBy(h => h.PartyPosition).ToList();
            monsterPositions = monsterPositions.OrderBy(m => m.PartyPosition).ToList();
            eventBus.RaiseCombatInitialized(units);
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
                eventBus.RaiseLogMessage(retreatMessage, uiConfig.TextColor);
                eventBus.RaiseUnitRetreated(unit);
                int penalty = 10;
                var teammates = units.Select(u => u.unit).Where(u => u is CharacterStats cs && cs.Type == stats.Type && u.Health > 0 && !u.HasRetreated && u != unit).ToList();
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
            eventBus.RaiseLogMessage(endMessage, uiConfig.TextColor);
            eventBus.RaiseCombatEnded();
            expeditionManager.SaveProgress();
            bool partyDead = expeditionManager.GetExpedition().Party.CheckDeadStatus().Count == 0;
            if (!partyDead)
            {
                var expedition = expeditionManager.GetExpedition();
                if (expedition.CurrentNodeIndex < expedition.NodeData.Count)
                    expedition.NodeData[expedition.CurrentNodeIndex].Completed = true;
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
            if (combatConfig == null || eventBus == null || visualConfig == null || uiConfig == null || combatCamera == null || effectsComponent == null || turnComponent == null)
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
                    eventBus.RaiseLogMessage(speedMessage, uiConfig.TextColor);
                    eventBus.RaiseCombatSpeedChanged(combatConfig.CombatSpeed);
                }
            }
        }
    }
}