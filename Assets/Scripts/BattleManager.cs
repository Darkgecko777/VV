using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private PartyData partyData;
    [SerializeField] private EncounterData encounterData;
    [SerializeField] private UIManager uiManager;
    private List<CharacterRuntimeStats> heroes;
    private List<CharacterRuntimeStats> monsters;
    private bool isBattleActive = false;
    private const int retreatMoraleThreshold = 20;
    private float combatSpeed = 1f; // Placeholder for animation speed multiplier
    private int roundNumber = 0;

    void Start()
    {
        if (partyData == null || encounterData == null || uiManager == null)
        {
            return;
        }

        heroes = partyData.GetHeroes();
        monsters = encounterData.SpawnMonsters();

        if (heroes == null || monsters == null)
        {
            return;
        }

        if (heroes.Count != 4 || monsters.Count != 4)
        {
            return;
        }

        foreach (var hero in heroes)
        {
            if (hero == null || hero.Stats.characterType == CharacterStatsData.CharacterType.Ghoul || hero.Stats.characterType == CharacterStatsData.CharacterType.Wraith)
            {
                return;
            }
        }
        foreach (var monster in monsters)
        {
            if (monster == null || (monster.Stats.characterType != CharacterStatsData.CharacterType.Ghoul && monster.Stats.characterType != CharacterStatsData.CharacterType.Wraith))
            {
                return;
            }
        }

        StartCoroutine(ProcessCombat());
    }

    private IEnumerator ProcessCombat()
    {
        isBattleActive = true;
        while (isBattleActive)
        {
            roundNumber++;
            List<(CharacterRuntimeStats unit, int init)> queue = BuildInitiativeQueue();
            foreach (var (unit, initValue) in queue)
            {
                if (!isBattleActive) break;
                if (unit != null && unit.Stats.health > 0)
                {
                    bool isHero = heroes.Contains(unit);
                    yield return StartCoroutine(ProcessUnitAction(unit, isHero));
                    if (isHero && unit.Stats.speed == CharacterStatsData.Speed.VeryFast && roundNumber % 2 == 0)
                    {
                        yield return StartCoroutine(ProcessUnitAction(unit, isHero)); // Bonus action for VeryFast
                    }
                    if (!AreAnyAlive(heroes) || CheckRetreat(heroes) || !AreAnyAlive(monsters) || CheckRetreat(monsters))
                    {
                        isBattleActive = false;
                        break;
                    }
                }
                yield return new WaitForSeconds(0.5f / combatSpeed); // Base delay, adjustable later
            }
        }
    }

    private List<(CharacterRuntimeStats, int)> BuildInitiativeQueue()
    {
        List<(CharacterRuntimeStats, int)> queue = new List<(CharacterRuntimeStats, int)>();
        foreach (var hero in heroes)
        {
            if (hero != null && hero.Stats.health > 0)
            {
                int init = 20 - (int)hero.Stats.speed + Random.Range(1, 7);
                queue.Add((hero, init));
            }
        }
        foreach (var monster in monsters)
        {
            if (monster != null && monster.Stats.health > 0)
            {
                int init = 20 - (int)monster.Stats.speed + Random.Range(1, 7);
                queue.Add((monster, init));
            }
        }
        return queue.OrderByDescending(x => x.Item2).ToList();
    }

    private IEnumerator ProcessUnitAction(CharacterRuntimeStats attacker, bool isHero)
    {
        if (attacker == null || attacker.Stats.health <= 0) yield break;

        List<CharacterRuntimeStats> targets = isHero ? monsters : heroes;
        if (isHero && attacker.CharacterSO is HeroSO heroSO)
        {
            heroSO.ApplySpecialAbility(attacker, partyData);
        }

        if (isHero && attacker.IsCultist)
        {
            var aliveHeroes = partyData.CheckDeadStatus();
            if (aliveHeroes.Count == 2)
            {
                var otherHero = aliveHeroes.Find(h => h != attacker);
                if (otherHero != null && attacker.CheckMurderCondition(otherHero, aliveHeroes.Count))
                {
                    isBattleActive = false;
                    yield break;
                }
            }
        }

        CharacterRuntimeStats target = GetRandomAliveTarget(targets);
        if (target != null)
        {
            yield return StartCoroutine(PerformAttack(attacker, target, isHero));
        }
    }

    private IEnumerator PerformAttack(CharacterRuntimeStats attacker, CharacterRuntimeStats target, bool isHero)
    {
        SpriteAnimation attackerAnim = attacker.GetComponent<SpriteAnimation>();
        SpriteAnimation targetAnim = target.GetComponent<SpriteAnimation>();
        if (attackerAnim != null) attackerAnim.Jiggle(true);
        if (targetAnim != null) targetAnim.Jiggle(false);

        int damage = Mathf.Max(attacker.Stats.attack - target.Stats.defense, 0);
        bool isDead = target.TakeDamage(damage);
        uiManager.UpdateUnitUI(attacker, target);
        uiManager.ShowPopup(target, isDead ? "Unit Defeated!" : $"{attacker.Stats.characterType} hits {target.Stats.characterType} for {damage} damage!");
        uiManager.LogMessage(isDead ? $"{target.Stats.characterType} Defeated!" : $"{attacker.Stats.characterType} hits {target.Stats.characterType} for {damage} damage!");

        yield return new WaitForSeconds(0.5f / combatSpeed); // Adjustable delay
    }

    private CharacterRuntimeStats GetRandomAliveTarget(List<CharacterRuntimeStats> targets)
    {
        List<CharacterRuntimeStats> aliveTargets = targets.FindAll(t => t != null && t.Stats.health > 0);
        return aliveTargets.Count > 0 ? aliveTargets[Random.Range(0, aliveTargets.Count)] : null;
    }

    private bool AreAnyAlive(List<CharacterRuntimeStats> characters)
    {
        return characters.Exists(c => c != null && c.Stats.health > 0);
    }

    private bool CheckRetreat(List<CharacterRuntimeStats> characters)
    {
        return characters.Exists(c => c != null && c.Stats.morale <= retreatMoraleThreshold);
    }

    public void SetCombatSpeed(float speed)
    {
        combatSpeed = Mathf.Clamp(speed, 0.5f, 4f); // Placeholder: 0.5x to 4x speed
    }
}