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
            uiController.InitializeUI(combatModel.Units);

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

                foreach (var attacker in unitList)
                {
                    var targets = attacker is HeroStats
                        ? unitList.Where(u => u is MonsterStats && u.Health > 0).ToList()
                        : unitList.Where(u => u is HeroStats && u.Health > 0).ToList();

                    // Get ability IDs from CharacterLibrary
                    List<string> abilityIds;
                    if (attacker is HeroStats hero)
                    {
                        abilityIds = CharacterLibrary.GetHeroData(hero.GetDisplayStats().name).AbilityIds;
                    }
                    else if (attacker is MonsterStats monster)
                    {
                        abilityIds = CharacterLibrary.GetMonsterData(monster.GetDisplayStats().name).AbilityIds;
                    }
                    else
                    {
                        abilityIds = new List<string> { "BasicAttack" };
                    }

                    var abilityId = abilityIds.Count > 0 ? abilityIds[Random.Range(0, abilityIds.Count)] : "BasicAttack";
                    AbilityData? ability = attacker is HeroStats
                        ? AbilityDatabase.GetHeroAbility(abilityId)
                        : AbilityDatabase.GetMonsterAbility(abilityId);

                    if (ability.HasValue)
                    {
                        ability.Value.Effect?.Invoke(attacker, expeditionManager.GetExpedition().Party);
                    }

                    var target = GetRandomAliveTarget(targets);
                    if (target == null) continue;

                    // Evasion check
                    if (Random.value <= target.Evasion / 100f)
                    {
                        combatModel.LogMessage($"{target.GetDisplayStats().name} dodges {attacker.GetDisplayStats().name}'s attack!", uiConfig.TextColor);
                        continue;
                    }

                    int damage = attacker.Attack;
                    bool killed = false;
                    if (target is HeroStats targetHero)
                    {
                        targetHero.Health = Mathf.Max(targetHero.Health - Mathf.Max(damage - targetHero.Defense, 0), 0);
                        killed = targetHero.Health <= 0;
                    }
                    else if (target is MonsterStats targetMonster)
                    {
                        targetMonster.Health = Mathf.Max(targetMonster.Health - Mathf.Max(damage - targetMonster.Defense, 0), 0);
                        killed = targetMonster.Health <= 0;
                    }

                    if (killed)
                    {
                        combatModel.LogMessage($"{attacker.GetDisplayStats().name} kills {target.GetDisplayStats().name}!", uiConfig.BogRotColor);
                        combatModel.UpdateUnit(target);
                    }
                    else
                    {
                        combatModel.LogMessage($"{attacker.GetDisplayStats().name} hits {target.GetDisplayStats().name} for {damage} damage!", uiConfig.TextColor);
                        combatModel.UpdateUnit(target, damage.ToString());
                    }

                    // Additional attacks based on Speed
                    bool extraAttack = false;
                    if (attacker.Speed >= combatConfig.SpeedTwoAttacksThreshold) // Speed 7-8: 2 attacks/round
                    {
                        extraAttack = true;
                    }
                    else if (attacker.Speed >= combatConfig.SpeedThreePerTwoThreshold && combatModel.RoundNumber % 2 == 0) // Speed 5-6: 3 attacks/2 rounds
                    {
                        extraAttack = true;
                    }
                    else if (attacker.Speed <= combatConfig.SpeedOnePerTwoThreshold && combatModel.RoundNumber % 2 == 1) // Speed 1-2: 1 attack/every other round
                    {
                        continue; // Skip attack in even rounds
                    }

                    if (extraAttack)
                    {
                        target = GetRandomAliveTarget(targets);
                        if (target == null) continue;

                        if (Random.value <= target.Evasion / 100f)
                        {
                            combatModel.LogMessage($"{target.GetDisplayStats().name} dodges {attacker.GetDisplayStats().name}'s extra attack!", uiConfig.TextColor);
                            continue;
                        }

                        damage = attacker.Attack;
                        killed = false;
                        if (target is HeroStats targetHero2)
                        {
                            targetHero2.Health = Mathf.Max(targetHero2.Health - Mathf.Max(damage - targetHero2.Defense, 0), 0);
                            killed = targetHero2.Health <= 0;
                        }
                        else if (target is MonsterStats targetMonster2)
                        {
                            targetMonster2.Health = Mathf.Max(targetMonster2.Health - Mathf.Max(damage - targetMonster2.Defense, 0), 0);
                            killed = targetMonster2.Health <= 0;
                        }

                        if (killed)
                        {
                            combatModel.LogMessage($"{attacker.GetDisplayStats().name} kills {target.GetDisplayStats().name} with extra attack!", uiConfig.BogRotColor);
                            combatModel.UpdateUnit(target);
                        }
                        else
                        {
                            combatModel.LogMessage($"{attacker.GetDisplayStats().name} hits {target.GetDisplayStats().name} for {damage} damage with extra attack!", uiConfig.TextColor);
                            combatModel.UpdateUnit(target, damage.ToString());
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