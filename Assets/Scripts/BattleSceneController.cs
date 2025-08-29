using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    public class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private Camera battleCamera;

        private CombatModel combatModel;
        private ExpeditionManager expeditionManager;
        private bool isEndingBattle;

        void Awake()
        {
            combatModel = new CombatModel(eventBus);
        }

        void Start()
        {
            expeditionManager = ExpeditionManager.Instance;
            if (expeditionManager == null)
            {
                Debug.LogError("BattleSceneController: Failed to find ExpeditionManager!");
                return;
            }
            if (!ValidateReferences()) return;

            eventBus.OnBattleEnded += EndBattle;
            StartCoroutine(RunBattle());
        }

        void OnDestroy()
        {
            eventBus.OnBattleEnded -= EndBattle;
        }

        private IEnumerator RunBattle()
        {
            if (combatModel.IsBattleActive)
            {
                yield break;
            }

            var expeditionData = expeditionManager.GetExpedition();
            if (expeditionData == null || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                EndBattle();
                yield break;
            }

            var heroStats = expeditionData.Party.GetHeroes();
            var monsterStats = expeditionData.NodeData[expeditionData.CurrentNodeIndex].Monsters;

            if (heroStats == null || monsterStats == null || heroStats.Count == 0 || monsterStats.Count == 0)
            {
                EndBattle();
                yield break;
            }

            combatModel.IsBattleActive = true;
            combatModel.InitializeUnits(heroStats, monsterStats); // Triggers OnBattleInitialized via CombatModel
            combatModel.IncrementRound();
            while (combatModel.IsBattleActive)
            {
                if (CheckRetreat(combatModel.Units.Select(u => u.unit).ToList()))
                {
                    combatModel.LogMessage("Party morale too low, retreating!", uiConfig.BogRotColor);
                    eventBus.RaiseRetreatTriggered();
                    combatModel.EndBattle();
                    yield break;
                }

                var unitList = combatModel.Units.Select(u => u.unit).Where(u => u.Health > 0).OrderByDescending(u => u.Speed).ToList();
                if (unitList.Count == 0)
                {
                    combatModel.EndBattle();
                    yield break;
                }

                foreach (var unit in unitList)
                {
                    var abilityId = unit.AbilityId;
                    AbilityData? ability = unit is HeroStats ? AbilityDatabase.GetHeroAbility(abilityId) : AbilityDatabase.GetMonsterAbility(abilityId);
                    if (ability == null) continue;

                    var targets = unit is HeroStats ? combatModel.Units.Select(u => u.unit).Where(u => u is MonsterStats && u.Health > 0).ToList()
                                                  : combatModel.Units.Select(u => u.unit).Where(u => u is HeroStats && u.Health > 0).ToList();
                    var target = GetRandomAliveTarget(targets);
                    if (target == null) continue;

                    int damage = Mathf.Max(unit.Attack - target.Defense, 0);
                    bool killed = false;

                    if (ability.Value.Effect != null)
                    {
                        ability.Value.Effect(target, expeditionData.Party);
                    }

                    target.Health = Mathf.Max(target.Health - damage, 0);
                    killed = target.Health <= 0;
                    combatModel.UpdateUnit(target, damage.ToString());

                    if (killed)
                    {
                        combatModel.LogMessage($"{unit.GetDisplayStats().name} kills {target.GetDisplayStats().name}!", uiConfig.BogRotColor);
                    }
                    else
                    {
                        combatModel.LogMessage($"{unit.GetDisplayStats().name} hits {target.GetDisplayStats().name} for {damage} damage!", uiConfig.TextColor);
                    }

                    if (unit.Speed >= combatConfig.SpeedTwoAttacksThreshold)
                    {
                        var extraTarget = GetRandomAliveTarget(targets);
                        if (extraTarget != null)
                        {
                            if (Random.Range(0, 100) < extraTarget.Evasion)
                            {
                                combatModel.LogMessage($"{extraTarget.GetDisplayStats().name} dodges {unit.GetDisplayStats().name}'s extra attack!", uiConfig.TextColor);
                                continue;
                            }

                            damage = Mathf.Max(unit.Attack - extraTarget.Defense, 0);
                            extraTarget.Health = Mathf.Max(extraTarget.Health - damage, 0);
                            killed = extraTarget.Health <= 0;
                            combatModel.UpdateUnit(extraTarget, damage.ToString());
                            if (killed)
                            {
                                combatModel.LogMessage($"{unit.GetDisplayStats().name} kills {extraTarget.GetDisplayStats().name} with extra attack!", uiConfig.BogRotColor);
                            }
                            else
                            {
                                combatModel.LogMessage($"{unit.GetDisplayStats().name} hits {extraTarget.GetDisplayStats().name} for {damage} damage with extra attack!", uiConfig.TextColor);
                            }
                        }
                    }

                    yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
                }

                if (combatModel.RoundNumber >= combatConfig.MaxRounds)
                {
                    combatModel.EndBattle();
                    yield break;
                }

                combatModel.IncrementRound();
            }
        }

        private ICombatUnit GetRandomAliveTarget(List<ICombatUnit> targets)
        {
            var aliveTargets = targets.Where(t => t.Health > 0).ToList();
            return aliveTargets.Count > 0 ? aliveTargets[Random.Range(0, aliveTargets.Count)] : null;
        }

        private bool CheckRetreat(List<ICombatUnit> characters)
        {
            return characters.OfType<HeroStats>().Any(h => h.Morale <= combatConfig.RetreatMoraleThreshold);
        }

        public void SetCombatSpeed(float speed)
        {
            if (combatConfig != null)
            {
                combatConfig.CombatSpeed = Mathf.Clamp(speed, combatConfig.MinCombatSpeed, combatConfig.MaxCombatSpeed);
            }
        }

        private void EndBattle()
        {
            if (isEndingBattle) return;
            isEndingBattle = true;

            combatModel.EndBattle();
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

            isEndingBattle = false;
        }

        private bool ValidateReferences()
        {
            if (combatConfig == null || eventBus == null || visualConfig == null || uiConfig == null || battleCamera == null)
            {
                Debug.LogError($"BattleSceneController: Missing references! CombatConfig: {combatConfig != null}, EventBus: {eventBus != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, BattleCamera: {battleCamera != null}");
                return false;
            }
            return true;
        }
    }
}