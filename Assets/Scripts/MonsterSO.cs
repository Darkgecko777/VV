using UnityEngine;

[CreateAssetMenu(fileName = "MonsterSO", menuName = "VirulentVentures/MonsterSO", order = 7)]
public class MonsterSO : ScriptableObject
{
    [SerializeField] protected CharacterStatsData stats;
    [SerializeField] protected Sprite sprite;
    protected const int bogRotMoraleDrain = 5;

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

        newStats.maxHealth = Mathf.RoundToInt(Random.Range(newStats.minHealth, newStats.maxHealth) * rankMultiplier);
        newStats.health = newStats.maxHealth;
        newStats.attack = Mathf.RoundToInt(Random.Range(newStats.minAttack, newStats.maxAttack) * rankMultiplier);
        newStats.defense = Mathf.RoundToInt(Random.Range(newStats.minDefense, newStats.maxDefense) * rankMultiplier);
        newStats.isInfected = false;
        newStats.slowTickDelay = 0;
        newStats.bogRotSpreadChance = 0.25f;
        target.SetStats(newStats);
    }

    public virtual bool TakeDamage(ref CharacterStatsData stats, float damage)
    {
        int damageTaken = Mathf.Max(Mathf.RoundToInt(damage - stats.defense), 0);
        stats.health = Mathf.Max(stats.health - damageTaken, 0);
        return stats.health <= 0;
    }

    public virtual void ApplyMoraleDamage(ref CharacterStatsData stats, float amount)
    {
        stats.morale = Mathf.Max(stats.morale - Mathf.RoundToInt(amount), 0);
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
        stats.maxHealth = Mathf.RoundToInt(Random.Range(stats.minHealth, stats.maxHealth) * rankMultiplier);
        stats.health = stats.maxHealth;
        stats.attack = Mathf.RoundToInt(Random.Range(stats.minAttack, stats.maxAttack) * rankMultiplier);
        stats.defense = Mathf.RoundToInt(Random.Range(stats.minDefense, stats.maxDefense) * rankMultiplier);
        stats.isInfected = false;
        stats.slowTickDelay = 0;
        stats.bogRotSpreadChance = 0.25f;
    }

    public virtual bool TryInfect(ref CharacterStatsData stats, float currentMorale)
    {
        if (Random.value <= stats.bogRotSpreadChance)
        {
            stats.isInfected = true;
            stats.morale = Mathf.Max(stats.morale - bogRotMoraleDrain, 0);
            return true;
        }
        return false;
    }

    public virtual bool CheckDodge()
    {
        return false;
    }
}