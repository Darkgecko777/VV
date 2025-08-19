using UnityEngine;

[CreateAssetMenu(fileName = "ScoutSO", menuName = "VirulentVentures/ScoutSO", order = 5)]
public class ScoutSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Scout,
        minHealth = 50,
        maxHealth = 70,
        health = 50,
        minAttack = 12,
        maxAttack = 18,
        attack = 12,
        minDefense = 3,
        maxDefense = 6,
        defense = 3,
        morale = 100,
        sanity = 100,
        speed = CharacterStatsData.Speed.Fast,
        isInfected = false,
        isCultist = false,
        rank = 2,
        bogRotSpreadChance = 0.15f
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.Scout;
        defaultStats.minHealth = 50;
        defaultStats.maxHealth = 70;
        defaultStats.health = defaultStats.minHealth;
        defaultStats.minAttack = 12;
        defaultStats.maxAttack = 18;
        defaultStats.attack = 12;
        defaultStats.minDefense = 3;
        defaultStats.maxDefense = 6;
        defaultStats.defense = 3;
        defaultStats.morale = 100;
        defaultStats.sanity = 100;
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
        newStats.bogRotSpreadChance = newStats.isCultist ? 0.20f : 0.15f;
        target.SetStats(newStats);
    }

    public override void ApplySpecialAbility(CharacterRuntimeStats target, PartyData partyData)
    {
        CharacterStatsData updatedStats = target.Stats;
        updatedStats.defense += 2;
        target.SetStats(updatedStats);
    }
}