using UnityEngine;

[CreateAssetMenu(fileName = "TreasureHunterSO", menuName = "VirulentVentures/TreasureHunterSO", order = 4)]
public class TreasureHunterSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.TreasureHunter,
        minHealth = 60,
        maxHealth = 80,
        health = 60,
        minAttack = 10,
        maxAttack = 15,
        attack = 10,
        minDefense = 3,
        maxDefense = 7,
        defense = 3,
        morale = 100,
        sanity = 100,
        speed = CharacterStatsData.Speed.Normal,
        isInfected = false,
        isCultist = false,
        rank = 2
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.TreasureHunter;
        defaultStats.minHealth = 60;
        defaultStats.maxHealth = 80;
        defaultStats.health = defaultStats.minHealth;
        defaultStats.minAttack = 10;
        defaultStats.maxAttack = 15;
        defaultStats.attack = 10;
        defaultStats.minDefense = 3;
        defaultStats.maxDefense = 7;
        defaultStats.defense = 3;
        defaultStats.morale = 100;
        defaultStats.sanity = 100;
        defaultStats.speed = CharacterStatsData.Speed.Normal;
        defaultStats.isInfected = false;
        defaultStats.isCultist = false;
        defaultStats.rank = 2;
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

        newStats.maxHealth = Mathf.RoundToInt(Random.Range(newStats.minHealth, newStats.maxHealth) * rankMultiplier);
        newStats.health = newStats.maxHealth;
        newStats.attack = Mathf.RoundToInt(Random.Range(newStats.minAttack, newStats.maxAttack) * rankMultiplier);
        newStats.defense = Mathf.RoundToInt(Random.Range(newStats.minDefense, newStats.maxDefense) * rankMultiplier);
        newStats.isInfected = false;
        newStats.slowTickDelay = 0;
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
                    allyStats.morale = Mathf.Min(allyStats.morale + 3, 100);
                    ally.SetStats(allyStats);
                }
            }
        }
    }
}