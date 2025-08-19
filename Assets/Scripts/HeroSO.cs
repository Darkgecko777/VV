using UnityEngine;

[CreateAssetMenu(fileName = "HeroSO", menuName = "VirulentVentures/HeroSO", order = 1)]
public class HeroSO : ScriptableObject
{
    [SerializeField] protected CharacterStatsData stats;
    [SerializeField] protected Sprite sprite; // Aseprite placeholder
    protected const int lowMoraleThreshold = 50;

    public CharacterStatsData Stats => stats;
    public Sprite Sprite => sprite;

    public virtual void ApplyStats(CharacterRuntimeStats target)
    {
        CharacterStatsData newStats = stats;
        float rankMultiplier = newStats.rank switch
        {
            1 => 0.8f, // Rank 1: 80% base stats
            3 => 1.2f, // Rank 3: 120% base stats
            _ => 1.0f  // Rank 2: 100% base stats
        };

        newStats.maxHealth = Mathf.RoundToInt(Random.Range(newStats.minHealth, newStats.maxHealth) * rankMultiplier);
        newStats.health = newStats.maxHealth;
        newStats.attack = Mathf.RoundToInt(Random.Range(newStats.minAttack, newStats.maxAttack) * rankMultiplier);
        newStats.defense = Mathf.RoundToInt(Random.Range(newStats.minDefense, newStats.maxDefense) * rankMultiplier);
        newStats.isInfected = false;
        newStats.slowTickDelay = 0;
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
    }

    public virtual bool CheckMurderCondition(ref CharacterStatsData stats, CharacterRuntimeStats other, int aliveCount)
    {
        if (!stats.isCultist || aliveCount != 2 || other == null || other.IsCultist)
        {
            return false;
        }
        int hpThreshold = Mathf.RoundToInt(other.Stats.maxHealth * 0.2f); // 20% HP
        if (other.Stats.health <= hpThreshold)
        {
            other.TakeDamage(other.Stats.health); // Instant kill
            return true; // Signals murder, ends battle with loot
        }
        return false;
    }

    public virtual void ApplySpecialAbility(CharacterRuntimeStats target, PartyData partyData)
    {
        // Placeholder for derived classes (e.g., FighterSO, HealerSO)
    }
}