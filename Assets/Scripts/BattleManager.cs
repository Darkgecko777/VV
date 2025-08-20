using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace VirulentVentures
{
    public class BattleManager : MonoBehaviour
    {
        [SerializeField] private PartyData partyData;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private ExpeditionManager expeditionManager;
        [SerializeField] private BattleUIManager uiManager;
        private List<(ICombatUnit unit, GameObject go)> units;
        private bool isBattleActive = false;
        private const int retreatMoraleThreshold = 20;
        private float combatSpeed = 1f;
        private int roundNumber = 0;

        void Start()
        {
            if (partyData == null || expeditionData == null || expeditionManager == null || uiManager == null)
            {
                Debug.LogError($"BattleManager: Missing references! PartyData: {partyData != null}, ExpeditionData: {expeditionData != null}, ExpeditionManager: {expeditionManager != null}, UIManager: {uiManager != null}");
                return;
            }

            if (expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogError($"BattleManager: Invalid node index {expeditionData.CurrentNodeIndex}, node count: {expeditionData.NodeData.Count}");
                EndBattle();
                return;
            }

            var heroStats = partyData.GetHeroes();
            var monsterStats = expeditionData.NodeData[expeditionData.CurrentNodeIndex].Monsters;

            if (heroStats == null || monsterStats == null)
            {
                Debug.LogError($"BattleManager: Null data! Heroes: {heroStats != null}, Monsters: {monsterStats != null}");
                EndBattle();
                return;
            }

            if (heroStats.Count == 0 || monsterStats.Count == 0)
            {
                Debug.LogError($"BattleManager: Empty data! Heroes count: {heroStats.Count}, Monsters count: {monsterStats.Count}");
                EndBattle();
                return;
            }

            units = new List<(ICombatUnit, GameObject)>();

            // Create hero GameObjects dynamically
            for (int i = 0; i < heroStats.Count; i++)
            {
                if (heroStats[i].Health <= 0) continue;
                GameObject heroObj = new GameObject(heroStats[i].Type.Id);
                heroObj.transform.position = heroStats[i].Position;
                var renderer = heroObj.AddComponent<SpriteRenderer>();
                renderer.sprite = heroStats[i].SO as HeroSO ? (heroStats[i].SO as HeroSO).Sprite : null;
                renderer.sortingLayerName = "Characters";
                renderer.transform.localScale = new Vector3(2f, 2f, 1f);
                units.Add((heroStats[i], heroObj));
            }

            // Create monster GameObjects dynamically
            for (int i = 0; i < monsterStats.Count; i++)
            {
                if (monsterStats[i].Health <= 0) continue;
                GameObject monsterObj = new GameObject(monsterStats[i].Type.Id);
                monsterObj.transform.position = monsterStats[i].Position;
                var renderer = monsterObj.AddComponent<SpriteRenderer>();
                renderer.sprite = monsterStats[i].SO as MonsterSO ? (monsterStats[i].SO as MonsterSO).Sprite : null;
                renderer.sortingLayerName = "Characters";
                renderer.transform.localScale = new Vector3(2f, 2f, 1f);
                units.Add((monsterStats[i], monsterObj));
            }

            uiManager.InitializeUI(units);
            isBattleActive = true;
            StartCoroutine(CombatLoop());
        }

        private IEnumerator CombatLoop()
        {
            while (isBattleActive)
            {
                roundNumber++;
                uiManager.LogMessage($"Round {roundNumber} begins!");

                List<ICombatUnit> heroes = units.Where(u => u.unit is HeroStats && u.unit.Health > 0).Select(u => u.unit).ToList();
                List<ICombatUnit> monsters = units.Where(u => u.unit is MonsterStats && u.unit.Health > 0).Select(u => u.unit).ToList();

                if (!AreAnyAlive(heroes) || !AreAnyAlive(monsters) || CheckRetreat(heroes))
                {
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
                uiManager.LogMessage($"{target.Type.Id} dodges the attack!");
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
                uiManager.LogMessage($"{attacker.Type.Id} kills {target.Type.Id}!");
                uiManager.UpdateUnitPanel(target);
            }
            else
            {
                uiManager.LogMessage($"{attacker.Type.Id} hits {target.Type.Id} for {damage} damage!");
            }

            uiManager.UpdateUnitPanel(target);
            uiManager.ShowDamagePopup(target, damage.ToString());

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
            expeditionManager.SetTransitioning(true);
            StartCoroutine(uiManager.FadeToExpedition(() => SceneManager.LoadScene("Expedition")));
        }
    }
}