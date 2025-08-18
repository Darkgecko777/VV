using UnityEngine;

[CreateAssetMenu(fileName = "FighterSO", menuName = "VirulentVentures/FighterSO", order = 2)]
public class FighterSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Fighter,
        minHealth = 80f,
        maxHealth = 100f,
        health = 80f, // Initialize to minHealth
        minAttack = 15f,
        maxAttack = 20f,
        attack = 15f,
        minDefense = 5f,
        maxDefense = 10f,
        defense = 5f,
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
        defaultStats.characterType = CharacterStatsData.CharacterType.Fighter;
        defaultStats.minHealth = 80f;
        defaultStats.maxHealth = 100f;
        defaultStats.health = defaultStats.minHealth; // Ensure initial health
        defaultStats.minAttack = 15f;
        defaultStats.maxAttack = 20f;
        defaultStats.attack = 15f;
        defaultStats.minDefense = 5f;
        defaultStats.maxDefense = 10f;
        defaultStats.defense = 5f;
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
        if (target.Stats.health < target.Stats.maxHealth * 0.3f)
        {
            CharacterStatsData updatedStats = target.Stats;
            updatedStats.attack += 3f;
            target.SetStats(updatedStats);
        }
    }
}