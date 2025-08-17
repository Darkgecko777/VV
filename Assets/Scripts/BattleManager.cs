using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private List<BaseCharacterStats> fighters;
    [SerializeField] private List<BaseCharacterStats> ghouls;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TimeKeeper timeKeeper;
    private bool isBattleActive = false;
    private const float retreatMoraleThreshold = 20f;

    void Start()
    {
        if (fighters.Count != 4 || ghouls.Count != 4 || uiManager == null || timeKeeper == null)
        {
            return;
        }
        foreach (var fighter in fighters)
        {
            if (fighter == null || fighter.characterType != BaseCharacterStats.CharacterType.Fighter)
            {
                return;
            }
        }
        foreach (var ghoul in ghouls)
        {
            if (ghoul == null || ghoul.characterType != BaseCharacterStats.CharacterType.Ghoul)
            {
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
        ProcessGroupActions(ghouls, fighters, currentTick, true);
        if (!AreAnyAlive(fighters) || CheckRetreat(fighters))
        {
            isBattleActive = false;
            return;
        }

        // Process fighters
        ProcessGroupActions(fighters, ghouls, currentTick, false);
        if (!AreAnyAlive(ghouls) || CheckRetreat(ghouls))
        {
            isBattleActive = false;
            return;
        }
    }

    private void ProcessGroupActions(List<BaseCharacterStats> attackers, List<BaseCharacterStats> targets, int currentTick, bool isMonsterGroup)
    {
        for (int i = 0; i < attackers.Count; i++)
        {
            var attacker = attackers[i];
            if (attacker != null && attacker.health > 0 && currentTick % attacker.EffectiveSpeed == 0)
            {
                BaseCharacterStats target = GetRandomAliveTarget(targets);
                if (target != null)
                {
                    StartCoroutine(PerformAttack(attacker, target, !isMonsterGroup));
                }
            }
        }
    }

    private IEnumerator PerformAttack(BaseCharacterStats attacker, BaseCharacterStats target, bool isAttackerFighter)
    {
        SpriteAnimation attackerAnim = attacker.GetComponent<SpriteAnimation>();
        SpriteAnimation targetAnim = target.GetComponent<SpriteAnimation>();
        if (attackerAnim != null) attackerAnim.Jiggle(true);
        if (targetAnim != null) targetAnim.Jiggle(false);

        float damage = Mathf.Max(attacker.attack - target.defense, 0f);
        bool isDead = target.TakeDamage(damage);

        if (uiManager != null)
        {
            uiManager.UpdateUnitUI(attacker, target);
            uiManager.ShowPopup(target, isDead ? "Unit Defeated!" : "Attack Landed!");
        }

        if (attacker.TryInfect())
        {
            if (target.TryInfect())
            {
                target.ApplySlowEffect(1);
                if (uiManager != null)
                {
                    uiManager.ShowPopup(target, "Infected: Slowed!");
                }
            }
        }

        yield return new WaitForSeconds(timeKeeper.GetTickRate());
    }

    private BaseCharacterStats GetRandomAliveTarget(List<BaseCharacterStats> targets)
    {
        List<BaseCharacterStats> aliveTargets = targets.FindAll(t => t != null && t.health > 0);
        return aliveTargets.Count > 0 ? aliveTargets[Random.Range(0, aliveTargets.Count)] : null;
    }

    private bool AreAnyAlive(List<BaseCharacterStats> characters)
    {
        return characters.Exists(c => c != null && c.health > 0);
    }

    private bool CheckRetreat(List<BaseCharacterStats> characters)
    {
        return characters.Exists(c => c != null && c.morale <= retreatMoraleThreshold);
    }
}