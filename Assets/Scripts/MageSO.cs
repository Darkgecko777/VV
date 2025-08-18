using UnityEngine;

[CreateAssetMenu(fileName = "MageSO", menuName = "VirulentVentures/MageSO", order = 6)]
public class MageSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Mage,
        minHealth = 40f,
        maxHealth = 60f,
        minAttack = 15f,
        maxAttack = 25f,
        minDefense = 0f,
        maxDefense = 5f,
        morale = 100f,
        sanity = 100f,
        speed = CharacterStatsData.Speed.Normal,
        isInfected = false,
        isCultist = false,
        rank = 2, // Default Rank 2 (100% stats)
        bogRotSpreadChance = 0.15f // 0.20f if isCultist
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.Mage;
        defaultStats.minHealth = 40f;
        defaultStats.maxHealth = 60f;
        defaultStats.minAttack = 15f;
        defaultStats.maxAttack = 25f;
        defaultStats.minDefense = 0f;
        defaultStats.maxDefense = 5f;
        defaultStats.morale = 100f;
        defaultStats.sanity = 100f;
        defaultStats.speed = CharacterStatsData.Speed.Normal;
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
        // Placeholder: Deal 5 damage to all enemies (requires BattleManager integration)
        CharacterStatsData updatedStats = target.Stats;
        updatedStats.attack += 5f; // Temporary ATK boost
        target.SetStats(updatedStats);
    }
}