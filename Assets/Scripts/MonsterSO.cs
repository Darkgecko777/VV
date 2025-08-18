using UnityEngine;

[CreateAssetMenu(fileName = "MonsterSO", menuName = "VirulentVentures/MonsterSO", order = 7)]
public class MonsterSO : ScriptableObject
{
    [SerializeField] protected CharacterStatsData stats;
    [SerializeField] protected Sprite sprite;
    protected const float bogRotMoraleDrain = 5f;

    public CharacterStatsData Stats => stats;
    public Sprite Sprite => sprite;

    public virtual void ApplyStats(CharacterRuntimeStats target)
    {
        CharacterStatsData newStats = stats;
        float rankMultiplier = newStats.rank switch
        {
            1 => 0.8f,
            3 => 1.2f,
            _ => 1.0f
        };

        newStats.maxHealth = Random.Range(newStats.minHealth, newStats.maxHealth) * rankMultiplier;
        newStats.health = newStats.maxHealth;
        newStats.attack = Random.Range(newStats.minAttack, newStats.maxAttack) * rankMultiplier;
        newStats.defense = Random.Range(newStats.minDefense, newStats.maxDefense) * rankMultiplier;
        newStats.isInfected = false;
        newStats.slowTickDelay = 0;
        newStats.bogRotSpreadChance = 0.25f;
        target.SetStats(newStats);
    }

    public virtual bool TakeDamage(ref CharacterStatsData stats, float damage)
    {
        float damageTaken = Mathf.Max(damage - stats.defense, 0f);
        stats.health = Mathf.Max(stats.health - damageTaken, 0f);
        return stats.health <= 0f;
    }

    public virtual void ApplyMoraleDamage(ref CharacterStatsData stats, float amount)
    {
        stats.morale = Mathf.Max(stats.morale - amount, 0f);
    }

    public virtual void ApplySlowEffect(ref CharacterStatsData stats, int tickDelay)
    {
        stats.slowTickDelay += tickDelay;
    }

    public virtual void ResetStats(ref CharacterStatsData stats)
    {
        float rankMultiplier = stats.rank switch
        {
            1 => 0.8f,
            3 => 1.2f,
            _ => 1.0f
        };
        stats.maxHealth = Random.Range(stats.minHealth, stats.maxHealth) * rankMultiplier;
        stats.health = stats.maxHealth;
        stats.attack = Random.Range(stats.minAttack, stats.maxAttack) * rankMultiplier;
        stats.defense = Random.Range(stats.minDefense, stats.maxDefense) * rankMultiplier;
        stats.isInfected = false;
        stats.slowTickDelay = 0;
        stats.bogRotSpreadChance = 0.25f;
    }

    public virtual bool TryInfect(ref CharacterStatsData stats, float currentMorale)
    {
        if (Random.value <= stats.bogRotSpreadChance)
        {
            stats.isInfected = true;
            stats.morale = Mathf.Max(stats.morale - bogRotMoraleDrain, 0f);
            return true;
        }
        return false;
    }

    public virtual bool CheckDodge()
    {
        return false;
    }
}