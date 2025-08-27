using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    public class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private BattleUIController uiController;
        [SerializeField] private BattleVisualController visualController;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private Camera battleCamera;

        private CombatModel combatModel;
        private ExpeditionManager expeditionManager;
        private bool isEndingBattle;

        void Awake()
        {
            combatModel = new CombatModel();
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

            uiController.SubscribeToModel(combatModel);
            visualController.SubscribeToModel(combatModel);
            uiController.OnContinueClicked += EndBattle;
            combatModel.OnBattleEnded += EndBattle;

            StartCoroutine(RunBattle());
        }

        void OnDestroy()
        {
            if (uiController != null)
            {
                uiController.OnContinueClicked -= EndBattle;
            }
            if (combatModel != null)
            {
                combatModel.OnBattleEnded -= EndBattle;
            }
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

            var heroStats = expeditionManager.GetExpedition().Party.GetHeroes();
            var monsterStats = expeditionData.NodeData[expeditionData.CurrentNodeIndex].Monsters;

            if (heroStats == null || monsterStats == null || heroStats.Count == 0 || monsterStats.Count == 0)
            {
                EndBattle();
                yield break;
            }

            combatModel.IsBattleActive = true;
            combatModel.InitializeUnits(heroStats, monsterStats);
            visualController.InitializeUnits(combatModel.Units.Select(u => (u.unit, u.go)).ToList());
            uiController.InitializeUnitPanels(combatModel.Units.Select(u => (u.unit, u.go, u.unit.GetDisplayStats())).ToList());

            combatModel.IncrementRound();
            while (combatModel.IsBattleActive)
            {
                if (CheckRetreat(combatModel.Units.Select(u => u.unit).ToList()))
                {
                    combatModel.LogMessage("Party morale too low, retreating!", uiConfig.BogRotColor);
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
                    var ability = AbilityDatabase.GetHeroAbility(unit.AbilityId) ?? AbilityDatabase.GetMonsterAbility(unit.AbilityId);
                    if (ability == null || ability.Value.Effect == null) continue;

                    var targets = unit is HeroStats ? combatModel.Units.Select(u => u.unit).OfType<MonsterStats>().Where(m => m.Health > 0).Cast<ICombatUnit>().ToList()
                                                   : combatModel.Units.Select(u => u.unit).OfType<HeroStats>().Where(h => h.Health > 0).Cast<ICombatUnit>().ToList();

                    var target = GetRandomAliveTarget(targets);
                    if (target == null) continue;

                    ability.Value.Effect?.Invoke(target, expeditionManager.GetExpedition().Party);

                    int damage = unit.Attack;
                    bool killed = false;

                    if (target is HeroStats targetHero)
                    {
                        int evasionRoll = Random.Range(0, 100);
                        if (evasionRoll < targetHero.Evasion)
                        {
                            combatModel.LogMessage($"{targetHero.GetDisplayStats().name} dodges {unit.GetDisplayStats().name}'s attack!", uiConfig.TextColor);
                            continue;
                        }

                        targetHero.Health = Mathf.Max(targetHero.Health - Mathf.Max(damage - targetHero.Defense, 0), 0);
                        killed = targetHero.Health <= 0;
                        if (!killed && targetHero.Morale > 0)
                        {
                            targetHero.Morale -= 5;
                            combatModel.LogMessage($"{unit.GetDisplayStats().name} reduces {targetHero.GetDisplayStats().name}'s morale by 5!", uiConfig.BogRotColor);
                        }
                    }
                    else if (target is MonsterStats targetMonster)
                    {
                        int evasionRoll = Random.Range(0, 100);
                        if (ability.Value.CanDodge && evasionRoll < targetMonster.Evasion)
                        {
                            combatModel.LogMessage($"{targetMonster.GetDisplayStats().name} dodges {unit.GetDisplayStats().name}'s attack!", uiConfig.TextColor);
                            continue;
                        }

                        targetMonster.Health = Mathf.Max(targetMonster.Health - Mathf.Max(damage - targetMonster.Defense, 0), 0);
                        killed = targetMonster.Health <= 0;
                    }

                    if (killed)
                    {
                        combatModel.LogMessage($"{unit.GetDisplayStats().name} kills {target.GetDisplayStats().name}!", uiConfig.BogRotColor);
                        combatModel.UpdateUnit(target);
                    }
                    else
                    {
                        combatModel.LogMessage($"{unit.GetDisplayStats().name} hits {target.GetDisplayStats().name} for {damage} damage!", uiConfig.TextColor);
                        combatModel.UpdateUnit(target, damage.ToString());
                    }

                    if (unit.Speed >= combatConfig.SpeedTwoAttacksThreshold)
                    {
                        var extraTarget = GetRandomAliveTarget(targets);
                        if (extraTarget == null) continue;

                        ability.Value.Effect?.Invoke(extraTarget, expeditionManager.GetExpedition().Party);
                        damage = unit.Attack;
                        killed = false;

                        if (extraTarget is HeroStats targetHero2)
                        {
                            int evasionRoll = Random.Range(0, 100);
                            if (evasionRoll < targetHero2.Evasion)
                            {
                                combatModel.LogMessage($"{targetHero2.GetDisplayStats().name} dodges {unit.GetDisplayStats().name}'s extra attack!", uiConfig.TextColor);
                                continue;
                            }

                            targetHero2.Health = Mathf.Max(targetHero2.Health - Mathf.Max(damage - targetHero2.Defense, 0), 0);
                            killed = targetHero2.Health <= 0;
                            if (!killed && targetHero2.Morale > 0)
                            {
                                targetHero2.Morale -= 5;
                                combatModel.LogMessage($"{unit.GetDisplayStats().name} reduces {targetHero2.GetDisplayStats().name}'s morale by 5 with extra attack!", uiConfig.BogRotColor);
                            }
                        }
                        else if (extraTarget is MonsterStats targetMonster2)
                        {
                            int evasionRoll = Random.Range(0, 100);
                            if (ability.Value.CanDodge && evasionRoll < targetMonster2.Evasion)
                            {
                                combatModel.LogMessage($"{targetMonster2.GetDisplayStats().name} dodges {unit.GetDisplayStats().name}'s extra attack!", uiConfig.TextColor);
                                continue;
                            }

                            targetMonster2.Health = Mathf.Max(targetMonster2.Health - Mathf.Max(damage - targetMonster2.Defense, 0), 0);
                            killed = targetMonster2.Health <= 0;
                        }

                        if (killed)
                        {
                            combatModel.LogMessage($"{unit.GetDisplayStats().name} kills {extraTarget.GetDisplayStats().name} with extra attack!", uiConfig.BogRotColor);
                            combatModel.UpdateUnit(extraTarget);
                        }
                        else
                        {
                            combatModel.LogMessage($"{unit.GetDisplayStats().name} hits {extraTarget.GetDisplayStats().name} for {damage} damage with extra attack!", uiConfig.TextColor);
                            combatModel.UpdateUnit(extraTarget, damage.ToString());
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
                uiController.FadeToScene(() => expeditionManager.EndExpedition());
            }
            else
            {
                uiController.FadeToScene(() => expeditionManager.TransitionToExpeditionScene());
            }

            isEndingBattle = false;
        }

        private bool ValidateReferences()
        {
            if (combatConfig == null || uiController == null || visualController == null || expeditionManager == null || visualConfig == null || uiConfig == null || battleCamera == null)
            {
                Debug.LogError($"BattleSceneController: Missing references! CombatConfig: {combatConfig != null}, UIController: {uiController != null}, VisualController: {visualController != null}, ExpeditionManager: {expeditionManager != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, BattleCamera: {battleCamera != null}");
                return false;
            }
            return true;
        }
    }
}