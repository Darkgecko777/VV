using UnityEngine;
using UnityEngine.Events;

public abstract class BaseCharacterStats : MonoBehaviour
{
    public enum CharacterType { Fighter, Ghoul }
    public enum Speed { VerySlow = 5, Slow = 4, Normal = 3, Fast = 2, VeryFast = 1 }

    public CharacterType characterType;
    public Speed speed = Speed.Normal;
    public float maxHealth = 100f;
    public float health = 100f;
    public float attack = 10f;
    public float defense = 5f;
    public float morale = 100f;
    public bool isInfected = false;
    public UnityEvent<BaseCharacterStats> OnInfected = new UnityEvent<BaseCharacterStats>();
    private int slowTickDelay = 0;

    public int EffectiveSpeed => (int)speed + slowTickDelay;

    // Apply damage and check for death
    public bool TakeDamage(float damage)
    {
        float damageTaken = Mathf.Max(damage - defense, 0f);
        health = Mathf.Max(health - damageTaken, 0f);
        return health <= 0f;
    }

    // Apply morale damage
    public void ApplyMoraleDamage(float amount)
    {
        morale = Mathf.Max(morale - amount, 0f);
    }

    // Apply slow effect (increases ticks per attack)
    public void ApplySlowEffect(int tickDelay)
    {
        slowTickDelay += tickDelay;
    }

    // Reset stats for a new run
    public void ResetStats()
    {
        health = maxHealth;
        morale = 100f;
        isInfected = false;
        slowTickDelay = 0;
    }

    // Abstract method for infection logic
    public abstract bool TryInfect();
}