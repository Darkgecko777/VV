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
            var state = unitAttackStates.Find(s => s.Unit == unit);
            if (state == null)
            {
                Debug.LogWarning($"No UnitAttackState for {unit.Id}. Falling back to BasicAttack.");
                return "BasicAttack";
            }
            if (unit.Abilities == null || unit.Abilities.Length == 0)
            {
                Debug.LogWarning($"No abilities assigned to {unit.Id}. Falling back to BasicAttack.");
                return "BasicAttack";
            }
            AbilitySO selectedAbility = null;
            int lowestPriority = int.MaxValue;
            foreach (var abilityObj in unit.Abilities)
            {
                if (!(abilityObj is AbilitySO ability))
                {
                    Debug.LogWarning($"Invalid AbilitySO for {unit.Id}: {abilityObj?.name}. Skipping.");
                    continue;
                }
                // Check cooldown
                if (state.AbilityCooldowns.TryGetValue(ability.Id, out int cd) && cd > 0)
                {
                    Debug.Log($"Ability {ability.Id} for {unit.Id} on cooldown: {cd} actions remaining.");
                    continue;
                }
                // Evaluate conditions
                bool conditionsMet = true;
                foreach (var condition in ability.Conditions)
                {
                    CharacterStats targetUnit = null;
                    float statValue = 0;
                    if (condition.Target == ConditionTarget.User)
                        targetUnit = unit;
                    else if (condition.Target == ConditionTarget.Enemy)
                        targetUnit = targets.FirstOrDefault(t => t is CharacterStats cs && cs.Health > 0 && !cs.HasRetreated) as CharacterStats;
                    else if (condition.Target == ConditionTarget.Ally)
                        targetUnit = partyData.HeroStats.FirstOrDefault(h => h != unit && h.Health > 0 && !h.HasRetreated);
                    if (targetUnit == null)
                    {
                        Debug.Log($"Condition failed for {ability.Id}: No valid {condition.Target} target.");
                        conditionsMet = false;
                        break;
                    }
                    switch (condition.Stat)
                    {
                        case Stat.Health: statValue = condition.IsPercentage ? targetUnit.Health / (float)targetUnit.MaxHealth : targetUnit.Health; break;
                        case Stat.Morale: statValue = condition.IsPercentage ? targetUnit.Morale / (float)targetUnit.MaxMorale : targetUnit.Morale; break;
                        case Stat.Speed: statValue = targetUnit.Speed; break;
                        case Stat.Attack: statValue = targetUnit.Attack; break;
                        case Stat.Defense: statValue = targetUnit.Defense; break;
                    }
                    bool conditionResult = condition.Comparison == Comparison.Greater ? statValue > condition.Threshold :
                                          condition.Comparison == Comparison.Lesser ? statValue < condition.Threshold :
                                          Mathf.Abs(statValue - condition.Threshold) < 0.001f;
                    if (!conditionResult)
                    {
                        Debug.Log($"Condition failed for {ability.Id}: {condition.Stat} {condition.Comparison} {condition.Threshold} (Value: {statValue}).");
                        conditionsMet = false;
                        break;
                    }
                }
                if (conditionsMet && ability.Priority < lowestPriority)
                {
                    selectedAbility = ability;
                    lowestPriority = ability.Priority;
                }
            }
            string selectedId = selectedAbility != null ? selectedAbility.Id : "BasicAttack";
            Debug.Log($"Selected ability for {unit.Id}: {selectedId}");
            if (selectedAbility != null && selectedAbility.Cooldown > 0)
            {
                state.AbilityCooldowns[selectedId] = selectedAbility.Cooldown;
                Debug.Log($"Set cooldown for {selectedId}: {selectedAbility.Cooldown} actions.");
            }
            return selectedId;
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

            // Use SelectAbility to pick ability dynamically
            string abilityId = SelectAbility(stats, partyData, targets);
            AbilitySO ability = stats.Abilities.FirstOrDefault(a => a is AbilitySO so && so.Id == abilityId) as AbilitySO;
            string abilityMessage = $"{stats.Id} uses {abilityId}!";
            allCombatLogs.Add(abilityMessage);
            eventBus.RaiseLogMessage(abilityMessage, Color.white);
            eventBus.RaiseUnitAttacking(unit, null, abilityId);

            // Select targets based on AbilitySO
            List<ICombatUnit> selectedTargets = new List<ICombatUnit>();
            if (ability != null)
            {
                if (ability.TargetType == TargetType.Self)
                {
                    selectedTargets.Add(unit);
                }
                else if (ability.TargetType == TargetType.Enemies)
                {
                    var enemyTargets = stats.Type == CharacterType.Hero
                        ? monsterPositions.Cast<ICombatUnit>().Where(t => t.Health > 0 && !t.HasRetreated).ToList()
                        : heroPositions.Cast<ICombatUnit>().Where(t => t.Health > 0 && !t.HasRetreated).ToList();
                    if (ability.PriorityLowHealth)
                    {
                        selectedTargets.Add(enemyTargets.OrderBy(t => t.Health).FirstOrDefault());
                    }
                    else if (ability.TargetType == TargetType.AOE)
                    {
                        selectedTargets.AddRange(enemyTargets);
                    }
                    else
                    {
                        selectedTargets.Add(GetRandomAliveTarget(enemyTargets));
                    }
                }
                else if (ability.TargetType == TargetType.Allies)
                {
                    var allyTargets = stats.Type == CharacterType.Hero
                        ? heroPositions.Cast<ICombatUnit>().Where(t => t != unit && t.Health > 0 && !t.HasRetreated).ToList()
                        : monsterPositions.Cast<ICombatUnit>().Where(t => t != unit && t.Health > 0 && !t.HasRetreated).ToList();
                    if (ability.TargetType == TargetType.AOE)
                    {
                        selectedTargets.AddRange(allyTargets);
                    }
                    else
                    {
                        selectedTargets.Add(GetRandomAliveTarget(allyTargets));
                    }
                }
            }
            else
            {
                // Fallback to BasicAttack targets
                var enemyTargets = stats.Type == CharacterType.Hero
                    ? monsterPositions.Cast<ICombatUnit>().Where(t => t.Health > 0 && !t.HasRetreated).ToList()
                    : heroPositions.Cast<ICombatUnit>().Where(t => t.Health > 0 && !t.HasRetreated).ToList();
                ICombatUnit selectedTarget = GetRandomAliveTarget(enemyTargets);
                if (selectedTarget != null)
                {
                    selectedTargets.Add(selectedTarget);
                }
            }

            if (selectedTargets.Count == 0)
            {
                string message = $"{stats.Id} finds no valid targets for {abilityId}! <color=#FFFF00>[No Targets]</color>";
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

                // Dodge check based on AbilitySO
                bool canDodge = ability != null && ability.EvasionCheck == EvasionCheck.Dodgeable;
                float dodgeChance = canDodge ? Mathf.Clamp(currentEvasion, 0, 100) / 100f : 0;
                if (canDodge && Random.value <= dodgeChance)
                {
                    string dodgeMessage = $"{targetStats.Id} dodges the attack! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>";
                    allCombatLogs.Add(dodgeMessage);
                    eventBus.RaiseLogMessage(dodgeMessage, Color.white);
                    continue;
                }
                if (canDodge)
                {
                    allCombatLogs.Add($"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>");
                    eventBus.RaiseLogMessage($"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>", Color.white);
                }

                // Resolve effects based on AbilitySO
                if (ability != null)
                {
                    if ((ability.EffectTypes & EffectType.Damage) != 0)
                    {
                        int damage = 0;
                        if (ability.FixedDamage > 0)
                            damage = ability.FixedDamage;
                        else if (ability.DefenseCheck == DefenseCheck.Standard)
                            damage = Mathf.Max(0, Mathf.RoundToInt(stats.Attack * (1f - 0.05f * (targetStats?.Defense ?? 0))));
                        else if (ability.DefenseCheck == DefenseCheck.Partial)
                            damage = Mathf.Max(0, Mathf.RoundToInt(stats.Attack * (1f - ability.PartialDefenseMultiplier * (targetStats?.Defense ?? 0))));
                        else if (ability.DefenseCheck == DefenseCheck.Ignore)
                            damage = Mathf.Max(0, Mathf.RoundToInt(stats.Attack));

                        string damageFormula = ability.FixedDamage > 0
                            ? $"[Fixed {ability.FixedDamage}]"
                            : $"[{stats.Attack} ATK - {targetStats?.Defense ?? 0} DEF * {(ability.DefenseCheck == DefenseCheck.Partial ? ability.PartialDefenseMultiplier : 0.05f) * 100}%]";
                        if (damage > 0 && targetStats != null)
                        {
                            targetStats.Health -= damage;
                            string damageMessage = $"{stats.Id} hits {targetStats.Id} for {damage} damage with {abilityId} <color=#FFFF00>{damageFormula}</color>";
                            allCombatLogs.Add(damageMessage);
                            eventBus.RaiseLogMessage(damageMessage, Color.white);
                            UpdateUnit(target, damageMessage);
                        }
                    }
                    if ((ability.EffectTypes & EffectType.Heal) != 0 && targetStats != null)
                    {
                        int healAmount = Mathf.RoundToInt(stats.Attack * ability.HealMultiplier);
                        targetStats.Health = Mathf.Min(targetStats.MaxHealth, targetStats.Health + healAmount);
                        string healMessage = $"{stats.Id} heals {targetStats.Id} for {healAmount} HP with {abilityId}!";
                        allCombatLogs.Add(healMessage);
                        eventBus.RaiseLogMessage(healMessage, Color.green);
                        UpdateUnit(target, healMessage);
                    }
                    if ((ability.EffectTypes & EffectType.Morale) != 0 && targetStats != null)
                    {
                        int moraleChange = ability.SelfDamage > 0 ? -ability.SelfDamage : 10; // Placeholder: +10 or -SelfDamage
                        targetStats.Morale = Mathf.Clamp(targetStats.Morale + moraleChange, 0, targetStats.MaxMorale);
                        string moraleMessage = $"{stats.Id} changes {targetStats.Id}'s morale by {moraleChange} with {abilityId}!";
                        allCombatLogs.Add(moraleMessage);
                        eventBus.RaiseLogMessage(moraleMessage, Color.yellow);
                        UpdateUnit(target, moraleMessage);
                    }
                    if ((ability.EffectTypes & EffectType.Buff) != 0 && targetStats != null)
                    {
                        // Placeholder: +10 ATK for 2 actions
                        var buffState = targetState ?? unitAttackStates.Find(s => s.Unit == target);
                        if (buffState != null)
                        {
                            buffState.TempStats["Attack"] = (10, 2); // (value, duration)
                            string buffMessage = $"{stats.Id} buffs {targetStats.Id}'s Attack +10 with {abilityId}!";
                            allCombatLogs.Add(buffMessage);
                            eventBus.RaiseLogMessage(buffMessage, Color.green);
                            UpdateUnit(target, buffMessage);
                        }
                    }
                    if ((ability.EffectTypes & EffectType.Debuff) != 0 && targetStats != null)
                    {
                        // Placeholder: -10 DEF for 2 actions
                        var debuffState = targetState ?? unitAttackStates.Find(s => s.Unit == target);
                        if (debuffState != null)
                        {
                            debuffState.TempStats["Defense"] = (-10, 2); // (value, duration)
                            string debuffMessage = $"{stats.Id} debuffs {targetStats.Id}'s Defense -10 with {abilityId}!";
                            allCombatLogs.Add(debuffMessage);
                            eventBus.RaiseLogMessage(debuffMessage, Color.red);
                            UpdateUnit(target, debuffMessage);
                        }
                    }
                    if ((ability.EffectTypes & EffectType.Infection) != 0 && targetStats != null)
                    {
                        if (!targetStats.IsInfected)
                        {
                            targetStats.IsInfected = true;
                            string infectionMessage = $"{targetStats.Id} is infected by {abilityId}!";
                            allCombatLogs.Add(infectionMessage);
                            eventBus.RaiseUnitInfected(targetStats, "Virus");
                            UpdateUnit(target, infectionMessage);
                        }
                    }
                    if (ability.SkipNextAttack)
                    {
                        state.SkipNextAttack = true;
                        string skipMessage = $"{stats.Id} skips next attack due to {abilityId}!";
                        allCombatLogs.Add(skipMessage);
                        eventBus.RaiseLogMessage(skipMessage, Color.yellow);
                    }
                }
                else
                {
                    // Fallback BasicAttack damage
                    int damage = Mathf.Max(0, Mathf.RoundToInt(stats.Attack * (1f - 0.05f * (targetStats?.Defense ?? 0))));
                    string damageFormula = $"[{stats.Attack} ATK - {targetStats?.Defense ?? 0} DEF * 5%]";
                    if (damage > 0 && targetStats != null)
                    {
                        targetStats.Health -= damage;
                        string damageMessage = $"{stats.Id} hits {targetStats.Id} for {damage} damage with {abilityId} <color=#FFFF00>{damageFormula}</color>";
                        allCombatLogs.Add(damageMessage);
                        eventBus.RaiseLogMessage(damageMessage, Color.white);
                        UpdateUnit(target, damageMessage);
                    }
                }

                // Handle Thorns (unchanged)
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

            // Apply SelfDamage if any
            if (ability != null && ability.SelfDamage > 0)
            {
                stats.Health = Mathf.Max(0, stats.Health - ability.SelfDamage);
                string selfDamageMessage = $"{stats.Id} takes {ability.SelfDamage} self-damage from {abilityId}!";
                allCombatLogs.Add(selfDamageMessage);
                eventBus.RaiseLogMessage(selfDamageMessage, Color.red);
                UpdateUnit(unit, selfDamageMessage);
            }

            // Apply Cost
            if (ability != null && ability.CostType != CostType.None)
            {
                int cost = ability.CostAmount;
                if (ability.CostType == CostType.Health)
                    stats.Health = Mathf.Max(0, stats.Health - cost);
                else if (ability.CostType == CostType.Morale)
                    stats.Morale = Mathf.Max(0, stats.Morale - cost);
                string costMessage = $"{stats.Id} pays {cost} {ability.CostType} for {abilityId}!";
                allCombatLogs.Add(costMessage);
                eventBus.RaiseLogMessage(costMessage, Color.white);
                UpdateUnit(unit, costMessage);
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