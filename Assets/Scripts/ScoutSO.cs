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
        health = 50f,
        minAttack = 12f,
        maxAttack = 18f,
        attack = 12f,
        minDefense = 3f,
        maxDefense = 6f,
        defense = 3f,
        morale = 100f,
        sanity = 100f,
        speed = CharacterStatsData.Speed.Fast,
        isInfected = false,
        isCultist = false,
        rank = 2,
        bogRotSpreadChance = 0.15f
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.Scout;
        defaultStats.minHealth = 50f;
        defaultStats.maxHealth = 70f;
        defaultStats.health = defaultStats.minHealth;
        defaultStats.minAttack = 12f;
        defaultStats.maxAttack = 18f;
        defaultStats.attack = 12f;
        defaultStats.minDefense = 3f;
        defaultStats.maxDefense = 6f;
        defaultStats.defense = 3f;
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
        CharacterStatsData updatedStats = target.Stats;
        updatedStats.defense += 2f;
        target.SetStats(updatedStats);
    }
}