using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private BattleUIController uiController;
        [SerializeField] private BattleVisualController visualController;
        [SerializeField] private VisualConfig visualConfig; // Added for visual effects
        [SerializeField] private UIConfig uiConfig; // Added for UI styling

        private CombatModel combatModel;
        private ExpeditionManager expeditionManager;

        void Awake()
        {
            expeditionManager = ExpeditionManager.Instance;
            combatModel = new CombatModel();
            if (!ValidateReferences()) return;
            uiController.OnContinueClicked += EndBattle;
            combatModel.OnBattleEnded += EndBattle;
            uiController.SubscribeToModel(combatModel);
            visualController.SubscribeToModel(combatModel);
        }

        void Start()
        {
            if (!ValidateReferences()) return;
            StartCoroutine(RunBattle());
        }

        private bool ValidateReferences()
        {
            if (combatConfig == null || uiController == null || visualController == null || expeditionManager == null || visualConfig == null || uiConfig == null)
            {
                Debug.LogError($"BattleSceneController: Missing references! CombatConfig: {combatConfig != null}, UIController: {uiController != null}, VisualController: {visualController != null}, ExpeditionManager: {expeditionManager != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}");
                return false;
            }
            return true;
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

            while (combatModel.IsBattleActive)
            {
                combatModel.IncrementRound();

                List<ICombatUnit> heroes = combatModel.Units.Where(u => u.unit is HeroStats).Select(u => u.unit).ToList();
                List<ICombatUnit> monsters = combatModel.Units.Where(u => u.unit is MonsterStats).Select(u => u.unit).ToList();

                if (!AreAnyAlive(heroes) || CheckRetreat(heroes))
                {
                    combatModel.LogMessage(AreAnyAlive(heroes) ? "Party retreats due to low morale!" : "Party defeated!", uiConfig.TextColor);
                    EndBattle();
                    yield break;
                }

                if (!AreAnyAlive(monsters))
                {
                    combatModel.LogMessage("Monsters defeated!", uiConfig.BogRotColor);
                    EndBattle();
                    yield break;
                }

                List<(ICombatUnit, int)> initiativeQueue = BuildInitiativeQueue(heroes, monsters);
                foreach (var (attacker, _) in initiativeQueue)
                {
                    if (attacker.Health <= 0) continue;
                    bool isHero = attacker is HeroStats;
                    yield return PerformAttack(attacker, isHero ? monsters : heroes);
                }
            }
        }

        private List<(ICombatUnit, int)> BuildInitiativeQueue(List<ICombatUnit> heroes, List<ICombatUnit> monsters)
        {
            List<(ICombatUnit, int)> queue = new List<(ICombatUnit, int)>();
            int GetSpeedValue(CharacterStatsData.Speed speed) => speed switch
            {
                CharacterStatsData.Speed.VeryFast => 4,
                CharacterStatsData.Speed.Fast => 3,
                CharacterStatsData.Speed.Normal => 2,
                CharacterStatsData.Speed.Slow => 1,
                _ => 0
            };

            queue.AddRange(heroes.Select(h => (h, GetSpeedValue(h.CharacterSpeed) + Random.Range(1, 10))));
            queue.AddRange(monsters.Select(m => (m, GetSpeedValue(m.CharacterSpeed) + Random.Range(1, 10))));
            queue.Sort((a, b) => b.Item2.CompareTo(a.Item2));

            return queue;
        }

        private IEnumerator PerformAttack(ICombatUnit attacker, List<ICombatUnit> targets)
        {
            ICombatUnit target = GetRandomAliveTarget(targets);
            if (target == null) yield break;

            AbilityData? ability = attacker is HeroStats
                ? AbilityDatabase.GetHeroAbility(attacker.AbilityId)
                : AbilityDatabase.GetMonsterAbility(attacker.AbilityId);

            if (ability == null) yield break;

            bool dodged = ability.Value.CanDodge && target is MonsterStats monsterStats && monsterStats.SO is MonsterSO monsterSO && monsterSO.CheckDodge();
            if (dodged)
            {
                combatModel.LogMessage($"{target.Type.Id} dodges the attack!", uiConfig.TextColor);
                yield break;
            }

            ability.Value.Effect?.Invoke(attacker, expeditionManager.GetExpedition().Party);

            int damage = attacker.Attack;
            bool killed = false;
            if (target is HeroStats heroStats && heroStats.SO is HeroSO heroSO)
            {
                killed = heroSO.TakeDamage(ref heroStats, damage);
            }
            else if (target is MonsterStats targetMonsterStats && targetMonsterStats.SO is MonsterSO targetMonsterSO)
            {
                killed = targetMonsterSO.TakeDamage(ref targetMonsterStats, damage);
            }

            // Placeholder for ability animation using VisualConfig
            // Sprite abilitySprite = visualConfig.GetCombatSprite(attacker.Type.Id);
            // visualController.TriggerAbilityAnimation(abilitySprite, attacker.Position);

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

        private ICombatUnit GetRandomAliveTarget(List<ICombatUnit> targets)
        {
            List<ICombatUnit> aliveTargets = targets.FindAll(t => t.Health > 0);
            return aliveTargets.Count > 0 ? aliveTargets[Random.Range(0, aliveTargets.Count)] : null;
        }

        private bool AreAnyAlive(List<ICombatUnit> characters)
        {
            return characters.Exists(c => c.Health > 0);
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
        }
    }
}