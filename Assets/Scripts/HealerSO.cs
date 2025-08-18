using UnityEngine;

[CreateAssetMenu(fileName = "HealerSO", menuName = "VirulentVentures/HealerSO", order = 3)]
public class HealerSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Healer,
        minHealth = 50f,
        maxHealth = 70f,
        minAttack = 5f,
        maxAttack = 10f,
        minDefense = 5f,
        maxDefense = 8f,
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
        // Ensure stats are initialized with Healer defaults
        defaultStats.characterType = CharacterStatsData.CharacterType.Healer;
        defaultStats.minHealth = 50f;
        defaultStats.maxHealth = 70f;
        defaultStats.minAttack = 5f;
        defaultStats.maxAttack = 10f;
        defaultStats.minDefense = 5f;
        defaultStats.maxDefense = 8f;
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
        // Heal lowest-HP ally by 5 HP if alive
        if (partyData != null)
        {
            CharacterRuntimeStats lowestAlly = partyData.FindLowestHealthAlly();
            if (lowestAlly != null && lowestAlly.Stats.health > 0)
            {
                CharacterStatsData allyStats = lowestAlly.Stats;
                allyStats.health = Mathf.Min(allyStats.health + 5f, allyStats.maxHealth);
                lowestAlly.SetStats(allyStats);
            }
        }
    }
}