using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    public class CombatLoopComponent : MonoBehaviour
    {
        [SerializeField] public CombatConfig combatConfig;
        [SerializeField] public EventBusSO eventBus;
        [SerializeField] public CombatTurnComponent turnComponent;
        [SerializeField] public CombatSetupComponent setupComponent;
        [SerializeField] public CombatEffectsComponent effectsComponent;
        [SerializeField] public PartyData partyData;
        private bool isCombatActive;
        private bool isPaused;
        private Dictionary<string, float> noTargetLogCooldowns = new Dictionary<string, float>();

        void Awake()
        {
            isCombatActive = false;
            isPaused = false;
            noTargetLogCooldowns.Clear();
            Debug.Log("CombatLoopComponent: Awake completed.");
        }

        void Start()
        {
            if (partyData == null)
            {
                Debug.LogError("CombatLoopComponent: partyData is null.");
            }
            Debug.Log("CombatLoopComponent: Start completed.");
        }

        public void StartCombatLoop(PartyData party)
        {
            if (partyData == null)
            {
                Debug.LogError("CombatLoopComponent: partyData is null, cannot start combat loop.");
                return;
            }
            if (isCombatActive)
            {
                Debug.LogWarning("CombatLoopComponent: Combat already active, ignoring StartCombatLoop.");
                return;
            }
            partyData = party;
            isCombatActive = true;
            Debug.Log("CombatLoopComponent: Starting RunCombat coroutine...");
            StartCoroutine(RunCombat());
        }

        private IEnumerator RunCombat()
        {
            if (!isCombatActive)
            {
                Debug.LogWarning("CombatLoopComponent: RunCombat called while not active, exiting.");
                yield break;
            }
            var expeditionData = CombatSceneComponent.Instance.ExpeditionManager.GetExpedition();
            if (expeditionData == null || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogWarning("CombatLoopComponent: Invalid expedition data, ending combat.");
                isCombatActive = false;
                eventBus.RaiseCombatEnded();
                yield break;
            }
            var heroStats = expeditionData.Party.GetHeroes();
            var monsterStats = expeditionData.NodeData[expeditionData.CurrentNodeIndex].Monsters;
            if (heroStats == null || heroStats.Count == 0 || monsterStats == null || monsterStats.Count == 0)
            {
                Debug.LogWarning("CombatLoopComponent: No valid units for combat, ending.");
                isCombatActive = false;
                eventBus.RaiseCombatEnded();
                yield break;
            }
            isCombatActive = true;
            turnComponent.IncrementRound();
            while (isCombatActive)
            {
                yield return new WaitUntil(() => !isPaused);
                var unitList = setupComponent.Units.Select(u => u.unit).Where(u => u.Health > 0 && !u.HasRetreated).OrderByDescending(u => u.Speed).ToList();
                if (unitList.Count == 0 || setupComponent.HeroPositions.Count == 0 || setupComponent.MonsterPositions.Count == 0)
                {
                    Debug.Log("CombatLoopComponent: No active units, ending combat.");
                    isCombatActive = false;
                    eventBus.RaiseCombatEnded();
                    yield break;
                }
                foreach (var unit in unitList.ToList())
                {
                    var state = setupComponent.UnitAttackStates.Find(s => s.Unit == unit);
                    if (state == null)
                    {
                        state = new UnitAttackState { Unit = unit, AttacksThisRound = 0, RoundCounter = 0, AbilityCooldowns = new Dictionary<string, int>(), SkipNextAttack = false, TempStats = new Dictionary<string, (int value, int duration)>() };
                        setupComponent.UnitAttackStates.Add(state);
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
                            setupComponent.AllCombatLogs.Add(cooldownEndMessage);
                            eventBus.RaiseLogMessage(cooldownEndMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
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
                                    setupComponent.AllCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, CombatSceneComponent.Instance.UIConfig.TextColor);
                                }
                                else if (tempStat.Key == "Speed")
                                {
                                    stats.Speed = baseData.Speed;
                                    string message = $"{stats.Id}'s Speed buff expires! <color=#FFFF00>[Restored to {stats.Speed}]</color>";
                                    setupComponent.AllCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, CombatSceneComponent.Instance.UIConfig.TextColor);
                                }
                                else if (tempStat.Key == "Evasion")
                                {
                                    stats.Evasion = baseData.Evasion;
                                    string message = $"{stats.Id}'s Evasion buff expires! <color=#FFFF00>[Restored to {stats.Evasion}]</color>";
                                    setupComponent.AllCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, CombatSceneComponent.Instance.UIConfig.TextColor);
                                }
                                else if (tempStat.Key == "TauntedBy" || tempStat.Key == "ThornsReflect")
                                {
                                    string message = $"{stats.Id}'s {tempStat.Key} effect expires!";
                                    setupComponent.AllCombatLogs.Add(message);
                                    eventBus.RaiseLogMessage(message, CombatSceneComponent.Instance.UIConfig.TextColor);
                                }
                            }
                        }
                    }
                }
                foreach (var unit in unitList.ToList())
                {
                    var state = setupComponent.UnitAttackStates.Find(s => s.Unit == unit);
                    if (state == null || unit.Health <= 0 || unit.HasRetreated) continue;
                    if (unit is CharacterStats stats && stats.Type == CharacterType.Hero && CombatSceneComponent.Instance.ExpeditionManager.GetExpedition().Party.CheckRetreat(unit, eventBus, CombatSceneComponent.Instance.UIConfig))
                    {
                        CombatSceneComponent.Instance.ExpeditionManager.GetExpedition().Party.ProcessRetreat(unit, eventBus, CombatSceneComponent.Instance.UIConfig);
                        yield return new WaitUntil(() => !isPaused);
                        yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
                        if (setupComponent.HeroPositions.Count == 0 || setupComponent.MonsterPositions.Count == 0)
                        {
                            Debug.Log("CombatLoopComponent: No active heroes or monsters after retreat, ending combat.");
                            isCombatActive = false;
                            eventBus.RaiseCombatEnded();
                            yield break;
                        }
                        continue;
                    }
                    if (!turnComponent.CanAttackThisRound(unit, state)) continue;
                    state.AttacksThisRound++;
                    yield return ProcessAttack(unit, CombatSceneComponent.Instance.ExpeditionManager.GetExpedition().Party, unitList);
                    yield return new WaitForSeconds(0.2f / (combatConfig?.CombatSpeed ?? 1f));
                    if (setupComponent.HeroPositions.Count == 0 || setupComponent.MonsterPositions.Count == 0)
                    {
                        Debug.Log("CombatLoopComponent: No active heroes or monsters after attack, ending combat.");
                        isCombatActive = false;
                        eventBus.RaiseCombatEnded();
                        yield break;
                    }
                }
                foreach (var unit in unitList.ToList())
                {
                    var state = setupComponent.UnitAttackStates.Find(s => s.Unit == unit);
                    if (state == null || unit.Health <= 0 || unit.HasRetreated) continue;
                    if (unit is CharacterStats stats && stats.Type == CharacterType.Hero && stats.Speed >= combatConfig.SpeedTwoAttacksThreshold && state.AttacksThisRound < 2)
                    {
                        state.AttacksThisRound++;
                        yield return ProcessAttack(unit, CombatSceneComponent.Instance.ExpeditionManager.GetExpedition().Party, unitList);
                        yield return new WaitForSeconds(0.2f / (combatConfig?.CombatSpeed ?? 1f));
                        if (setupComponent.HeroPositions.Count == 0 || setupComponent.MonsterPositions.Count == 0)
                        {
                            Debug.Log("CombatLoopComponent: No active heroes or monsters after second attack, ending combat.");
                            isCombatActive = false;
                            eventBus.RaiseCombatEnded();
                            yield break;
                        }
                    }
                }
                turnComponent.IncrementRound();
            }
        }

        private IEnumerator ProcessAttack(ICombatUnit unit, PartyData partyData, List<ICombatUnit> targets)
        {
            if (unit is not CharacterStats stats) yield break;
            var state = setupComponent.UnitAttackStates.Find(s => s.Unit == unit);
            if (state == null) yield break;
            string abilityId = AbilityDatabase.SelectAbility(stats, partyData, targets, state);
            AbilitySO ability = stats.Abilities.FirstOrDefault(a => a is AbilitySO so && so.Id == abilityId) as AbilitySO;
            string abilityMessage = $"{stats.Id} uses {abilityId}!";
            setupComponent.AllCombatLogs.Add(abilityMessage);
            eventBus.RaiseLogMessage(abilityMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
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

                var enemyTargets = stats.Type == CharacterType.Hero
                    ? setupComponent.MonsterPositions.Where(m => m.Health > 0 && !m.HasRetreated).OrderBy(m => m.PartyPosition).Select(m => (ICombatUnit)m).ToList()
                    : setupComponent.HeroPositions.Where(h => h.Health > 0 && !h.HasRetreated).OrderBy(h => h.PartyPosition).Select(h => (ICombatUnit)h).ToList();
                var allyTargets = stats.Type == CharacterType.Hero
                    ? setupComponent.HeroPositions.Where(h => h.Health > 0 && !h.HasRetreated).OrderBy(h => h.PartyPosition).Select(h => (ICombatUnit)h).ToList()
                    : setupComponent.MonsterPositions.Where(m => m.Health > 0 && !m.HasRetreated).OrderBy(m => m.PartyPosition).Select(m => (ICombatUnit)m).ToList();

                foreach (var attack in ability.Attacks)
                {
                    selectedTargets.AddRange(attack.TargetingRule.SelectTargets(stats, attack.Enemy ? enemyTargets : allyTargets, partyData, attack.Melee));
                    if (selectedTargets.Any())
                    {
                        string targetMessage = $"Attack {abilityId} targets {string.Join(", ", selectedTargets.Select(t => (t as CharacterStats).Id))}.";
                        setupComponent.AllCombatLogs.Add(targetMessage);
                        eventBus.RaiseLogMessage(targetMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
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
                        setupComponent.AllCombatLogs.Add(targetMessage);
                        eventBus.RaiseLogMessage(targetMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
                    }
                }

                selectedTargets = selectedTargets.Distinct().Take(maxTargets).ToList();
            }
            else
            {
                var enemyTargets = stats.Type == CharacterType.Hero
                    ? setupComponent.MonsterPositions.Where(m => m.Health > 0 && !m.HasRetreated).OrderBy(m => m.PartyPosition).Select(m => (ICombatUnit)m).ToList()
                    : setupComponent.HeroPositions.Where(h => h.Health > 0 && !h.HasRetreated).OrderBy(h => h.PartyPosition).Select(h => (ICombatUnit)h).ToList();
                AbilitySO basicAttack = AbilityDatabase.GetHeroAbility("BasicAttack") ?? AbilityDatabase.GetMonsterAbility("BasicAttack");
                if (basicAttack != null && (basicAttack.Attacks.Any(a => a.Melee || a.TargetingRule.MeleeOnly)))
                    enemyTargets = enemyTargets.Where(t => (stats.Type == CharacterType.Hero
                        ? setupComponent.MonsterPositions.IndexOf(t as CharacterStats) + 1
                        : setupComponent.HeroPositions.IndexOf(t as CharacterStats) + 1) <= 2).ToList();
                selectedTargets = basicAttack != null ? basicAttack.Attacks.First().TargetingRule.SelectTargets(stats, enemyTargets, partyData, true) : new List<ICombatUnit>();
            }

            if (!selectedTargets.Any())
            {
                string key = stats.Id + "_no_target";
                if (!noTargetLogCooldowns.ContainsKey(key) || Time.time - noTargetLogCooldowns[key] > 2f)
                {
                    string message = $"{stats.Id} finds no valid targets for {abilityId}! <color=#FFFF00>[No Targets]</color>";
                    setupComponent.AllCombatLogs.Add(message);
                    eventBus.RaiseLogMessage(message, CombatSceneComponent.Instance.UIConfig.TextColor);
                    noTargetLogCooldowns[key] = Time.time;
                }
                if (setupComponent.HeroPositions.Count == 0 || setupComponent.MonsterPositions.Count == 0)
                {
                    Debug.Log("CombatLoopComponent: No active heroes or monsters, ending combat.");
                    isCombatActive = false;
                    eventBus.RaiseCombatEnded();
                    yield break;
                }
                yield break;
            }

            var unitState = setupComponent.UnitAttackStates.Find(s => s.Unit == unit);
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
                var targetState = setupComponent.UnitAttackStates.Find(s => s.Unit == target);
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
                            if (UnityEngine.Random.value <= dodgeChance)
                            {
                                string dodgeMessage = $"{targetStats.Id} dodges the attack! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>";
                                setupComponent.AllCombatLogs.Add(dodgeMessage);
                                eventBus.RaiseLogMessage(dodgeMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
                                attackDodged = true;
                                continue;
                            }
                            string failDodgeMessage = $"{targetStats.Id} fails to dodge! <color=#FFFF00>[{currentEvasion}% Evasion Chance]</color>";
                            setupComponent.AllCombatLogs.Add(failDodgeMessage);
                            eventBus.RaiseLogMessage(failDodgeMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
                        }
                        if (!attackDodged)
                            effectsComponent.ApplyAttackDamage(stats, targetStats, attack, abilityId);
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
                    setupComponent.AllCombatLogs.Add(deathMessage);
                    eventBus.RaiseLogMessage(deathMessage, Color.red);
                    setupComponent.UpdateUnit(target, deathMessage);
                }
                if (stats.Health <= 0)
                {
                    eventBus.RaiseUnitDied(unit);
                    string deathMessage = $"{stats.Id} dies!";
                    setupComponent.AllCombatLogs.Add(deathMessage);
                    eventBus.RaiseLogMessage(deathMessage, Color.red);
                    setupComponent.UpdateUnit(unit, deathMessage);
                }
                if (targetStats != null) targetStats.Defense = originalDefense;
            }

            stats.Attack = originalAttack;
            stats.Speed = originalSpeed;
            stats.Evasion = originalEvasion;
            setupComponent.UpdateUnit(unit);
        }
    }
}