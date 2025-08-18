using UnityEngine;

[CreateAssetMenu(fileName = "ScoutSO", menuName = "VirulentVentures/ScoutSO", order = 5)]
public class ScoutSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Scout,
        minHealth = 50f,
        maxHealth = 70f,
        minAttack = 12f,
        maxAttack = 18f,
        minDefense = 3f,
        maxDefense = 6f,
        morale = 100f,
        sanity = 100f,
        speed = CharacterStatsData.Speed.Fast,
        isInfected = false,
        isCultist = false,
        rank = 2, // Default Rank 2 (100% stats)
        bogRotSpreadChance = 0.15f // 0.20f if isCultist
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.Scout;
        defaultStats.minHealth = 50f;
        defaultStats.maxHealth = 70f;
        defaultStats.minAttack = 12f;
        defaultStats.maxAttack = 18f;
        defaultStats.minDefense = 3f;
        defaultStats.maxDefense = 6f;
        defaultStats.morale = 100f;
        defaultStats.sanity = 100f;
        defaultStats.speed = CharacterStatsData.Speed.Fast;
        defaultStats.isInfected = false;
        defaultStats.isCultist = false;
        defaultStats.rank = 2;
        defaultStats.bogRotSpreadChance = 0.15f;
        stats = defaultStats;
    }

    public override void ApplyStats(CharacterRuntimeStats target)
    {
        CharacterStatsData newStats = defaultStats;
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

    public override void ApplySpecialAbility(CharacterRuntimeStats target, PartyData partyData)
    {
        // Placeholder: Increase dodge chance by 10% (requires BattleManager integration)
        CharacterStatsData updatedStats = target.Stats;
        updatedStats.defense += 2f; // Temporary DEF boost
        target.SetStats(updatedStats);
    }
}