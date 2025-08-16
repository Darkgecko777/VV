using UnityEngine;
using UnityEngine.Events;

public class CharacterStats : MonoBehaviour
{
    public enum CharacterType { Fighter, Ghoul }

    [SerializeField] private CharacterType characterType = CharacterType.Fighter;
    public float maxHealth = 100f;
    public float health = 100f;
    public float attack = 10f;
    public float defense = 5f;
    public float morale = 100f;
    public bool isInfected = false;
    public UnityEvent<CharacterStats> OnInfected = new UnityEvent<CharacterStats>();
    private const float bogRotMoraleDrain = 5f;
    private const float bogRotSpreadChance = 0.2f; // 20% chance on melee hit

    public CharacterType Type => characterType;

    // Apply damage and check for death
    public bool TakeDamage(float damage)
    {
        float damageTaken = Mathf.Max(damage - defense, 0f);
        health = Mathf.Max(health - damageTaken, 0f);
        return health <= 0f;
    }

    // Attempt to spread Bog Rot on melee hit
    public bool TryInfect()
    {
        if (Random.value <= bogRotSpreadChance)
        {
            isInfected = true;
            morale = Mathf.Max(morale - bogRotMoraleDrain, 0f);
            OnInfected.Invoke(this);
            return true;
        }
        return false;
    }

    // Apply morale damage (e.g., from Bog Rot)
    public void ApplyMoraleDamage(float amount)
    {
        morale = Mathf.Max(morale - amount, 0f);
    }

    // Reset stats for a new run
    public void ResetStats()
    {
        health = maxHealth;
        morale = 100f;
        isInfected = false;
    }
}