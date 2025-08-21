using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private PartyData partyData;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private ExpeditionManager expeditionManager;
        [SerializeField] private BattleUIController uiController;
        [SerializeField] private BattleVisualController visualController;

        private List<(ICombatUnit unit, GameObject go)> units;
        private bool isBattleActive = false;
        private const int retreatMoraleThreshold = 20;
        private float combatSpeed = 1f;
        private int roundNumber = 0;

        void Awake()
        {
            if (!ValidateReferences()) return;
            uiController.OnContinueClicked += EndBattle;
        }

        void Start()
        {
            if (!ValidateReferences()) return;
            StartCoroutine(RunBattle());
        }

        private bool ValidateReferences()
        {
            if (partyData == null || expeditionData == null || expeditionManager == null || uiController == null || visualController == null)
            {
                Debug.LogError($"BattleSceneController: Missing references! PartyData: {partyData != null}, ExpeditionData: {expeditionData != null}, ExpeditionManager: {expeditionManager != null}, UIController: {uiController != null}, VisualController: {visualController != null}");
                return false;
            }
            return true;
        }

        private IEnumerator RunBattle()
        {
            if (isBattleActive)
            {
                Debug.LogWarning("BattleSceneController: Battle already active!");
                yield break;
            }

            if (expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogError($"BattleSceneController: Invalid node index {expeditionData.CurrentNodeIndex}, node count: {expeditionData.NodeData.Count}");
                EndBattle();
                yield break;
            }

            var heroStats = partyData.GetHeroes();
            var monsterStats = expeditionData.NodeData[expeditionData.CurrentNodeIndex].Monsters;

            if (heroStats == null || monsterStats == null)
            {
                Debug.LogError($"BattleSceneController: Null data! Heroes: {heroStats != null}, Monsters: {monsterStats != null}");
                EndBattle();
                yield break;
            }

            if (heroStats.Count == 0 || monsterStats.Count == 0)
            {
                Debug.LogError($"BattleSceneController: Empty data! Heroes count: {heroStats.Count}, Monsters count: {monsterStats.Count}");
                EndBattle();
                yield break;
            }

            isBattleActive = true;
            units = new List<(ICombatUnit, GameObject)>();
            List<ICombatUnit> heroes = heroStats.Cast<ICombatUnit>().ToList();
            List<ICombatUnit> monsters = monsterStats.Cast<ICombatUnit>().ToList();

            // Initialize visuals
            visualController.InitializeUnits(heroStats, monsterStats);
            uiController.InitializeUI(units);

            while (isBattleActive)
            {
                roundNumber++;
                uiController.LogMessage($"Round {roundNumber} begins!");

                if (!AreAnyAlive(heroes) || CheckRetreat(heroes))
                {
                    uiController.LogMessage(AreAnyAlive(heroes) ? "Party retreats due to low morale!" : "Party defeated!");
                    EndBattle();
                    yield break;
                }

                if (!AreAnyAlive(monsters))
                {
                    uiController.LogMessage("Monsters defeated!");
                    EndBattle();
                    yield break;
                }

                List<(ICombatUnit, int)> initiativeQueue = BuildInitiativeQueue(heroes.Concat(monsters).ToList());

                foreach (var (unit, init) in initiativeQueue)
                {
                    if (unit.Health <= 0) continue;
                    yield return ProcessUnitAction(unit, heroes, monsters);
                }

                yield return new WaitForSeconds(1f / combatSpeed);
            }
        }

        private List<(ICombatUnit, int)> BuildInitiativeQueue(List<ICombatUnit> characters)
        {
            List<(ICombatUnit, int)> queue = new List<(ICombatUnit, int)>();
            foreach (var unit in characters)
            {
                int init = 20 - (int)unit.CharacterSpeed + Random.Range(1, 7);
                queue.Add((unit, init));
            }
            queue.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            return queue;
        }

        private IEnumerator ProcessUnitAction(ICombatUnit attacker, List<ICombatUnit> heroes, List<ICombatUnit> monsters)
        {
            bool isHero = attacker is HeroStats;
            List<ICombatUnit> targets = isHero ? monsters : heroes;
            ICombatUnit target = GetRandomAliveTarget(targets);
            if (target == null) yield break;

            bool dodged = target is MonsterStats monsterStats && monsterStats.SO is MonsterSO monsterSO && monsterSO.CheckDodge();
            if (dodged)
            {
                uiController.LogMessage($"{target.Type.Id} dodges the attack!");
                yield break;
            }

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

            if (killed)
            {
                uiController.LogMessage($"{attacker.Type.Id} kills {target.Type.Id}!");
                visualController.UpdateUnitVisual(target);
                uiController.UpdateUnitPanel(target);
            }
            else
            {
                uiController.LogMessage($"{attacker.Type.Id} hits {target.Type.Id} for {damage} damage!");
            }

            uiController.UpdateUnitPanel(target);
            uiController.ShowDamagePopup(target, damage.ToString());

            yield return new WaitForSeconds(0.5f / combatSpeed);
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
            return characters.Exists(c => c.Morale <= retreatMoraleThreshold);
        }

        public void SetCombatSpeed(float speed)
        {
            combatSpeed = Mathf.Clamp(speed, 0.5f, 4f);
        }

        private void EndBattle()
        {
            isBattleActive = false;
            expeditionManager.SaveProgress();
            bool partyDead = partyData.CheckDeadStatus().Count == 0;
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