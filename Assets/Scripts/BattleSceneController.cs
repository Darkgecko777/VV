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
            if (combatConfig == null || uiController == null || visualController == null || expeditionManager == null)
            {
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
                    combatModel.LogMessage(AreAnyAlive(heroes) ? "Party retreats due to low morale!" : "Party defeated!");
                    EndBattle();
                    yield break;
                }

                if (!AreAnyAlive(monsters))
                {
                    combatModel.LogMessage("Monsters defeated!");
                    EndBattle();
                    yield break;
                }

                List<(ICombatUnit, int)> initiativeQueue = BuildInitiativeQueue(heroes.Concat(monsters).ToList());

                foreach (var (unit, init) in initiativeQueue)
                {
                    if (unit.Health <= 0) continue;
                    yield return ProcessUnitAction(unit, heroes, monsters);
                }

                yield return new WaitForSeconds(1f / (combatConfig?.CombatSpeed ?? 1f));
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

            AbilityData? ability = isHero
                ? AbilityDatabase.GetHeroAbility(attacker.AbilityId)
                : AbilityDatabase.GetMonsterAbility(attacker.AbilityId);

            if (ability == null) yield break;

            bool dodged = ability.Value.CanDodge && target is MonsterStats monsterStats && monsterStats.SO is MonsterSO monsterSO && monsterSO.CheckDodge();
            if (dodged)
            {
                combatModel.LogMessage($"{target.Type.Id} dodges the attack!");
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

            if (killed)
            {
                combatModel.LogMessage($"{attacker.Type.Id} kills {target.Type.Id}!");
                combatModel.UpdateUnit(target);
            }
            else
            {
                combatModel.LogMessage($"{attacker.Type.Id} hits {target.Type.Id} for {damage} damage!");
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
            return characters.Exists(c => c.Morale <= (combatConfig?.RetreatMoraleThreshold ?? 20));
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