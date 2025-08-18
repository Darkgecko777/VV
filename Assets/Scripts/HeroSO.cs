using UnityEngine;

[CreateAssetMenu(fileName = "HeroSO", menuName = "VirulentVentures/HeroSO", order = 1)]
public class HeroSO : ScriptableObject
{
    [SerializeField] protected CharacterStatsData stats;
    [SerializeField] protected Sprite sprite; // Aseprite placeholder
    protected const float lowMoraleThreshold = 50f;
    protected const float bogRotMoraleDrain = 5f;

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

        newStats.maxHealth = Random.Range(newStats.minHealth, newStats.maxHealth) * rankMultiplier;
        newStats.health = newStats.maxHealth;
        newStats.attack = Random.Range(newStats.minAttack, newStats.maxAttack) * rankMultiplier;
        newStats.defense = Random.Range(newStats.minDefense, newStats.maxDefense) * rankMultiplier;
        newStats.isInfected = false;
        newStats.slowTickDelay = 0;
        newStats.bogRotSpreadChance = newStats.isCultist ? 0.20f : 0.15f;
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
        stats.bogRotSpreadChance = stats.isCultist ? 0.20f : 0.15f;
    }

    public virtual bool TryInfect(ref CharacterStatsData stats, float currentMorale)
    {
        float spreadChance = GetBogRotSpreadChance(currentMorale);
        if (Random.value <= spreadChance)
        {
            stats.isInfected = true;
            stats.morale = Mathf.Max(stats.morale - bogRotMoraleDrain, 0f);
            return true;
        }
        return false;
    }

    public virtual bool CheckMurderCondition(ref CharacterStatsData stats, CharacterRuntimeStats other, int aliveCount)
    {
        if (!stats.isCultist || aliveCount != 2 || other == null || other.IsCultist)
        {
            return false;
        }
        float hpThreshold = other.Stats.maxHealth * 0.2f; // 20% HP
        if (other.Stats.health <= hpThreshold)
        {
            other.TakeDamage(other.Stats.health); // Instant kill
            return true; // Signals murder, ends battle with loot
        }
        return false;
    }

    public virtual float GetBogRotSpreadChance(float currentMorale)
    {
        if (stats.isCultist && currentMorale < lowMoraleThreshold)
        {
            return stats.bogRotSpreadChance * 1.5f; // 1.5x boost if morale < 50
        }
        return stats.bogRotSpreadChance; // 0.15f default, 0.20f for cultist
    }

    public virtual void ApplySpecialAbility(CharacterRuntimeStats target, PartyData partyData)
    {
        // Placeholder for derived classes (e.g., FighterSO, HealerSO)
    }
}