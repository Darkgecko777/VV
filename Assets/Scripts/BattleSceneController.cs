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
        private bool isEndingBattle; // Guard to prevent recursive calls

        void Awake()
        {
            combatModel = new CombatModel(); // Initialize early, as it's not reference-dependent

            // Disable other cameras
            var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in allCameras)
            {
                if (cam != battleCamera)
                {
                    cam.enabled = false;
                }
            }
        }

        void Start()
        {
            expeditionManager = ExpeditionManager.Instance;
            if (expeditionManager == null)
            {
                expeditionManager = FindObjectOfType<ExpeditionManager>();
                if (expeditionManager == null)
                {
                    Debug.LogError("BattleSceneController: Failed to find ExpeditionManager!");
                    return;
                }
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

            // Re-enable other cameras
            var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in allCameras)
            {
                if (cam != battleCamera)
                {
                    cam.enabled = true;
                }
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
            visualController.InitializeUnits(combatModel.Units);
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

                var unitList = combatModel.Units.Select(u => u.unit).ToList();
                var aliveUnits = unitList.Where(u => u.Health > 0).ToList();
                if (aliveUnits.Count == 0)
                {
                    combatModel.EndBattle();
                    yield break;
                }

                foreach (var attacker in aliveUnits)
                {
                    var targets = attacker is HeroStats
                        ? combatModel.Units.Select(u => u.unit).Where(u => u is MonsterStats).ToList()
                        : combatModel.Units.Select(u => u.unit).Where(u => u is HeroStats).ToList();
                    var target = GetRandomAliveTarget(targets);
                    if (target == null)
                    {
                        combatModel.EndBattle();
                        yield break;
                    }

                    List<string> abilityIds = null;
                    if (attacker is HeroStats hero && hero.SO is HeroSO heroSO)
                    {
                        abilityIds = heroSO.AbilityIds;
                    }
                    else if (attacker is MonsterStats monster && monster.SO is MonsterSO monsterSO)
                    {
                        abilityIds = monsterSO.AbilityIds;
                    }

                    if (abilityIds == null || abilityIds.Count == 0)
                    {
                        combatModel.LogMessage($"{attacker.Type.Id} has no abilities!", uiConfig.TextColor);
                        continue;
                    }

                    var abilityId = abilityIds[Random.Range(0, abilityIds.Count)];
                    AbilityData? ability = attacker is HeroStats
                        ? AbilityDatabase.GetHeroAbility(abilityId)
                        : AbilityDatabase.GetMonsterAbility(abilityId);

                    if (ability.HasValue)
                    {
                        ability.Value.Effect?.Invoke(attacker, expeditionManager.GetExpedition().Party);
                    }

                    int damage = attacker.Attack;
                    bool killed = false;
                    if (target is HeroStats targetHero && targetHero.SO is HeroSO targetHeroSO)
                    {
                        killed = targetHeroSO.TakeDamage(ref targetHero, damage);
                    }
                    else if (target is MonsterStats targetMonster && targetMonster.SO is MonsterSO targetMonsterSO)
                    {
                        killed = targetMonsterSO.TakeDamage(ref targetMonster, damage);
                    }

                    if (killed)
                    {
                        combatModel.LogMessage($"{attacker.Type.Id} kills {target.Type.Id}!", uiConfig.BogRotColor);
                        combatModel.UpdateUnit(target);
                    }
                    else
                    {
                        combatModel.LogMessage($"{attacker.Type.Id} hits {target.Type.Id} for {damage} damage!", uiConfig.TextColor);
                        combatModel.UpdateUnit(target, damage.ToString());
                    }

                    yield return new WaitForSeconds(0.5f / (combatConfig?.CombatSpeed ?? 1f));
                }
            }
        }

        private ICombatUnit GetRandomAliveTarget(List<ICombatUnit> targets)
        {
            var aliveTargets = targets.Where(t => t.Health > 0).ToList();
            return aliveTargets.Count > 0 ? aliveTargets[Random.Range(0, aliveTargets.Count)] : null;
        }

        private bool CheckRetreat(List<ICombatUnit> characters)
        {
            return characters.Where(c => c is HeroStats).Cast<HeroStats>().Any(h => h.Morale <= (combatConfig?.RetreatMoraleThreshold ?? 20));
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
            if (isEndingBattle) return; // Prevent recursive calls
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
                uiController.FadeToScene(() => {
                    expeditionManager.UnloadBattleScene();
                    expeditionManager.OnContinueClicked();
                });
            }

            isEndingBattle = false; // Reset guard
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