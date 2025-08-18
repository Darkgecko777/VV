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
        health = 60f,
        minAttack = 10f,
        maxAttack = 15f,
        attack = 10f,
        minDefense = 3f,
        maxDefense = 7f,
        defense = 3f,
        morale = 100f,
        sanity = 100f,
        speed = CharacterStatsData.Speed.Normal,
        isInfected = false,
        isCultist = false,
        rank = 2,
        bogRotSpreadChance = 0.15f
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.TreasureHunter;
        defaultStats.minHealth = 60f;
        defaultStats.maxHealth = 80f;
        defaultStats.health = defaultStats.minHealth;
        defaultStats.minAttack = 10f;
        defaultStats.maxAttack = 15f;
        defaultStats.attack = 10f;
        defaultStats.minDefense = 3f;
        defaultStats.maxDefense = 7f;
        defaultStats.defense = 3f;
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
        newStats.bogRotSpreadChance = newStats.isCultist ? 0.20f : 0.15f;
        target.SetStats(newStats);
    }

    public override void ApplySpecialAbility(CharacterRuntimeStats target, PartyData partyData)
    {
        if (partyData != null)
        {
            CharacterRuntimeStats[] allies = partyData.FindAllies();
            foreach (var ally in allies)
            {
                if (ally.Stats.health > 0)
                {
                    CharacterStatsData allyStats = ally.Stats;
                    allyStats.morale = Mathf.Min(allyStats.morale + 3f, 100f);
                    ally.SetStats(allyStats);
                }
            }
        }
    }
}