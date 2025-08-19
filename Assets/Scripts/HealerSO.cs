using UnityEngine;

[CreateAssetMenu(fileName = "HealerSO", menuName = "VirulentVentures/HealerSO", order = 3)]
public class HealerSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Healer,
        minHealth = 50,
        maxHealth = 70,
        health = 50,
        minAttack = 5,
        maxAttack = 10,
        attack = 5,
        minDefense = 5,
        maxDefense = 8,
        defense = 5,
        morale = 100,
        sanity = 100,
        speed = CharacterStatsData.Speed.Normal,
        isInfected = false,
        isCultist = false,
        rank = 2
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.Healer;
        defaultStats.minHealth = 50;
        defaultStats.maxHealth = 70;
        defaultStats.health = defaultStats.minHealth;
        defaultStats.minAttack = 5;
        defaultStats.maxAttack = 10;
        defaultStats.attack = 5;
        defaultStats.minDefense = 5;
        defaultStats.maxDefense = 8;
        defaultStats.defense = 5;
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
            CharacterRuntimeStats lowestAlly = partyData.FindLowestHealthAlly();
            if (lowestAlly != null && lowestAlly.Stats.health > 0)
            {
                CharacterStatsData allyStats = lowestAlly.Stats;
                allyStats.health = Mathf.Min(allyStats.health + 5, allyStats.maxHealth);
                lowestAlly.SetStats(allyStats);
            }
        }
    }
}