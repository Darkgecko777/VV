using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private List<CharacterStats> fighters;
    [SerializeField] private List<CharacterStats> ghouls;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private float attackDelay = 0.5f; // Delay between attacks for pacing
    private bool isBattleActive = false;

    void Start()
    {
        if (fighters.Count != 4 || ghouls.Count != 4)
        {
            Debug.LogError("BattleManager requires exactly 4 fighters and 4 ghouls assigned in Inspector!");
            return;
        }
        foreach (var fighter in fighters)
        {
            if (fighter.Type != CharacterStats.CharacterType.Fighter)
            {
                Debug.LogError($"Invalid fighter type: {fighter.Type} on {fighter.name}");
                return;
            }
        }
        foreach (var ghoul in ghouls)
        {
            if (ghoul.Type != CharacterStats.CharacterType.Ghoul)
            {
                Debug.LogError($"Invalid ghoul type: {ghoul.Type} on {ghoul.name}");
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
            // Fighters attack ghouls
            foreach (var fighter in fighters)
            {
                if (fighter.health > 0)
                {
                    CharacterStats target = GetRandomAliveTarget(ghouls);
                    if (target != null)
                    {
                        yield return StartCoroutine(PerformAttack(fighter, target));
                    }
                }
            }

            // Check if ghouls are defeated
            if (!AreAnyAlive(ghouls))
            {
                isBattleActive = false;
                yield break;
            }

            // Ghouls attack fighters
            foreach (var ghoul in ghouls)
            {
                if (ghoul.health > 0)
                {
                    CharacterStats target = GetRandomAliveTarget(fighters);
                    if (target != null)
                    {
                        yield return StartCoroutine(PerformAttack(ghoul, target));
                    }
                }
            }

            // Check if fighters are defeated
            if (!AreAnyAlive(fighters))
            {
                isBattleActive = false;
                yield break;
            }

            yield return new WaitForSeconds(attackDelay);
        }
    }

    private IEnumerator PerformAttack(CharacterStats attacker, CharacterStats target)
    {
        SpriteAnimation attackerAnim = attacker.GetComponent<SpriteAnimation>();
        SpriteAnimation targetAnim = target.GetComponent<SpriteAnimation>();
        if (attackerAnim != null) attackerAnim.Jiggle();
        if (targetAnim != null) targetAnim.Jiggle();

        bool isDead = target.TakeDamage(attacker.attack);
        uiManager.UpdateUnitUI(attacker, target); // Update health bars/labels

        if (attacker.TryInfect())
        {
            target.TryInfect(); // Trigger infection popup via CharacterStats.OnInfected
        }

        yield return new WaitForSeconds(attackDelay / 2);
    }

    private CharacterStats GetRandomAliveTarget(List<CharacterStats> targets)
    {
        List<CharacterStats> aliveTargets = targets.FindAll(t => t.health > 0);
        return aliveTargets.Count > 0 ? aliveTargets[Random.Range(0, aliveTargets.Count)] : null;
    }

    private bool AreAnyAlive(List<CharacterStats> characters)
    {
        return characters.Exists(c => c.health > 0);
    }
}