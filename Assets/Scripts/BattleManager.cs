using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private List<CharacterStats> fighters;
    [SerializeField] private List<CharacterStats> ghouls;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private float attackDelay = 0.5f;
    [SerializeField] private float roundDelay = 1f;
    private bool isBattleActive = false;

    void Start()
    {
        if (fighters.Count != 4 || ghouls.Count != 4 || uiManager == null)
        {
            return;
        }
        foreach (var fighter in fighters)
        {
            if (fighter == null || fighter.Type != CharacterStats.CharacterType.Fighter)
            {
                return;
            }
        }
        foreach (var ghoul in ghouls)
        {
            if (ghoul == null || ghoul.Type != CharacterStats.CharacterType.Ghoul)
            {
                return;
            }
        }
        StartCoroutine(BattleLoop());
    }

    private IEnumerator BattleLoop()
    {
        isBattleActive = true;
        while (isBattleActive)
        {
            foreach (var fighter in fighters)
            {
                if (fighter != null && fighter.health > 0)
                {
                    CharacterStats target = GetRandomAliveTarget(ghouls);
                    if (target != null)
                    {
                        yield return StartCoroutine(PerformAttack(fighter, target, true));
                    }
                }
            }

            if (!AreAnyAlive(ghouls))
            {
                isBattleActive = false;
                yield break;
            }

            foreach (var ghoul in ghouls)
            {
                if (ghoul != null && ghoul.health > 0)
                {
                    CharacterStats target = GetRandomAliveTarget(fighters);
                    if (target != null)
                    {
                        yield return StartCoroutine(PerformAttack(ghoul, target, false));
                    }
                }
            }

            if (!AreAnyAlive(fighters))
            {
                isBattleActive = false;
                yield break;
            }

            yield return new WaitForSeconds(roundDelay);
        }
    }

    private IEnumerator PerformAttack(CharacterStats attacker, CharacterStats target, bool isAttackerFighter)
    {
        SpriteAnimation attackerAnim = attacker.GetComponent<SpriteAnimation>();
        SpriteAnimation targetAnim = target.GetComponent<SpriteAnimation>();
        if (attackerAnim != null) attackerAnim.Jiggle(true);
        if (targetAnim != null) targetAnim.Jiggle(false);

        float damage = Mathf.Max(attacker.attack - target.defense, 0f);
        bool isDead = target.TakeDamage(attacker.attack);
        if (uiManager != null)
        {
            uiManager.UpdateUnitUI(attacker, target);
        }

        if (attacker.TryInfect())
        {
            target.TryInfect();
        }

        yield return new WaitForSeconds(attackDelay);
    }

    private CharacterStats GetRandomAliveTarget(List<CharacterStats> targets)
    {
        List<CharacterStats> aliveTargets = targets.FindAll(t => t != null && t.health > 0);
        return aliveTargets.Count > 0 ? aliveTargets[Random.Range(0, aliveTargets.Count)] : null;
    }

    private bool AreAnyAlive(List<CharacterStats> characters)
    {
        return characters.Exists(c => c != null && c.health > 0);
    }
}