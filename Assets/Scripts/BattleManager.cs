using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private PartyData partyData;
    [SerializeField] private EncounterData encounterData;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TimeKeeper timeKeeper;
    private List<CharacterRuntimeStats> heroes;
    private List<CharacterRuntimeStats> monsters;
    private bool isBattleActive = false;
    private const float retreatMoraleThreshold = 20f;

    void Start()
    {
        if (partyData == null || encounterData == null || uiManager == null || timeKeeper == null)
        {
            Debug.LogError("BattleManager: Missing required references!");
            return;
        }

        heroes = partyData.GetHeroes();
        monsters = encounterData.SpawnMonsters();
        if (heroes.Count != 4 || monsters.Count == 0)
        {
            Debug.LogError("BattleManager: Invalid hero or monster count!");
            return;
        }

        foreach (var hero in heroes)
        {
            if (hero == null || hero.Stats.characterType == CharacterStatsData.CharacterType.Ghoul || hero.Stats.characterType == CharacterStatsData.CharacterType.Wraith)
            {
                Debug.LogError("BattleManager: Invalid hero type!");
                return;
            }
        }
        foreach (var monster in monsters)
        {
            if (monster == null || (monster.Stats.characterType != CharacterStatsData.CharacterType.Ghoul && monster.Stats.characterType != CharacterStatsData.CharacterType.Wraith))
            {
                Debug.LogError("BattleManager: Invalid monster type!");
                return;
            }
        }

        timeKeeper.OnTick.AddListener(ProcessTick);
        isBattleActive = true;
    }

    private void ProcessTick(int currentTick)
    {
        if (!isBattleActive) return;

        // Process monsters first
        ProcessGroupActions(monsters, heroes, currentTick, true);
        if (!AreAnyAlive(heroes) || CheckRetreat(heroes))
        {
            isBattleActive = false;
            return;
        }

        // Process heroes
        ProcessGroupActions(heroes, monsters, currentTick, false);
        if (!AreAnyAlive(monsters) || CheckRetreat(monsters))
        {
            isBattleActive = false;
            return;
        }
    }

    private void ProcessGroupActions(List<CharacterRuntimeStats> attackers, List<CharacterRuntimeStats> targets, int currentTick, bool isMonsterGroup)
    {
        for (int i = 0; i < attackers.Count; i++)
        {
            var attacker = attackers[i];
            if (attacker != null && attacker.Stats.health > 0 && currentTick % (attacker.Stats.slowTickDelay + (int)attacker.Stats.speed) == 0)
            {
                // Apply special abilities for heroes only
                if (!isMonsterGroup && attacker.CharacterSO is HeroSO heroSO)
                {
                    heroSO.ApplySpecialAbility(attacker, partyData); // Call HeroSO's ApplySpecialAbility
                }

                // Check for cultist murder condition
                if (!isMonsterGroup && attacker.IsCultist)
                {
                    var aliveHeroes = partyData.CheckDeadStatus();
                    if (aliveHeroes.Count == 2)
                    {
                        var otherHero = aliveHeroes.Find(h => h != attacker);
                        if (otherHero != null && attacker.CheckMurderCondition(otherHero, aliveHeroes.Count))
                        {
                            uiManager.ShowPopup(otherHero, "Cultist Murdered Ally!");
                            uiManager.LogMessage("Cultist Murdered Ally!");
                            isBattleActive = false; // End battle with loot
                            return;
                        }
                    }
                }

                // Perform attack
                CharacterRuntimeStats target = GetRandomAliveTarget(targets);
                if (target != null)
                {
                    StartCoroutine(PerformAttack(attacker, target, !isMonsterGroup));
                }
            }
        }
    }

    private IEnumerator PerformAttack(CharacterRuntimeStats attacker, CharacterRuntimeStats target, bool isAttackerHero)
    {
        SpriteAnimation attackerAnim = attacker.GetComponent<SpriteAnimation>();
        SpriteAnimation targetAnim = target.GetComponent<SpriteAnimation>();
        if (attackerAnim != null) attackerAnim.Jiggle(true);
        if (targetAnim != null) targetAnim.Jiggle(false);

        bool isDead = target.TakeDamage(attacker.Stats.attack); // Includes dodge check for Wraith
        uiManager.UpdateUnitUI(attacker, target);
        uiManager.ShowPopup(target, isDead ? "Unit Defeated!" : "Attack Landed!");
        uiManager.LogMessage(isDead ? $"{target.Stats.characterType} Defeated!" : $"{attacker.Stats.characterType} attacked {target.Stats.characterType}!");

        if (attacker.TryInfect())
        {
            if (target.TryInfect())
            {
                target.ApplySlowEffect(1);
                uiManager.ShowPopup(target, "Infected: Slowed!");
                uiManager.LogMessage($"{target.Stats.characterType} Infected: Slowed!");
            }
        }

        yield return new WaitForSeconds(timeKeeper.GetTickRate());
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
}