using UnityEngine;

[CreateAssetMenu(fileName = "TreasureHunterSO", menuName = "VirulentVentures/TreasureHunterSO", order = 4)]
public class TreasureHunterSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.TreasureHunter,
        minHealth = 60f,
        maxHealth = 80f,
        minAttack = 10f,
        maxAttack = 15f,
        minDefense = 3f,
        maxDefense = 7f,
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
        // Ensure stats are initialized with Treasure Hunter defaults
        defaultStats.characterType = CharacterStatsData.CharacterType.TreasureHunter;
        defaultStats.minHealth = 60f;
        defaultStats.maxHealth = 80f;
        defaultStats.minAttack = 10f;
        defaultStats.maxAttack = 15f;
        defaultStats.minDefense = 3f;
        defaultStats.maxDefense = 7f;
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
        // Boost morale of all living allies by 3
        if (partyData != null)
        {
            CharacterRuntimeStats[] allies = partyData.FindAllies();
            foreach (var ally in allies)
            {
                if (ally.Stats.health > 0)
                {
                    CharacterStatsData allyStats = ally.Stats;
                    allyStats.morale = Mathf.Min(allyStats.morale + 3f, 100f); // +3 morale
                    ally.SetStats(allyStats);
                }
            }
        }
    }
}