using UnityEngine;

[CreateAssetMenu(fileName = "GhoulSO", menuName = "VirulentVentures/GhoulSO", order = 8)]
public class GhoulSO : MonsterSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Ghoul,
        minHealth = 50f,
        maxHealth = 70f,
        minAttack = 12f,
        maxAttack = 18f,
        minDefense = 3f,
        maxDefense = 8f,
        morale = 80f,
        sanity = 0f,
        speed = CharacterStatsData.Speed.Normal,
        isInfected = false,
        isCultist = false, // Monsters don't use cultist mechanics
        rank = 2, // Default Rank 2 (100% stats)
        bogRotSpreadChance = 0.25f
    };

    void OnEnable()
    {
        // Ensure stats are initialized with Ghoul defaults
        defaultStats.characterType = CharacterStatsData.CharacterType.Ghoul;
        defaultStats.minHealth = 50f;
        defaultStats.maxHealth = 70f;
        defaultStats.minAttack = 12f;
        defaultStats.maxAttack = 18f;
        defaultStats.minDefense = 3f;
        defaultStats.maxDefense = 8f;
        defaultStats.morale = 80f;
        defaultStats.sanity = 0f;
        defaultStats.speed = CharacterStatsData.Speed.Normal;
        defaultStats.isInfected = false;
        defaultStats.isCultist = false;
        defaultStats.rank = 2;
        defaultStats.bogRotSpreadChance = 0.25f;
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
        newStats.bogRotSpreadChance = 0.25f;
        target.SetStats(newStats);
    }
}