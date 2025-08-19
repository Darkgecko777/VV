using UnityEngine;

[CreateAssetMenu(fileName = "MageSO", menuName = "VirulentVentures/MageSO", order = 6)]
public class MageSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Mage,
        minHealth = 40,
        maxHealth = 60,
        health = 40,
        minAttack = 15,
        maxAttack = 25,
        attack = 15,
        minDefense = 0,
        maxDefense = 5,
        defense = 0,
        morale = 100,
        sanity = 100,
        speed = CharacterStatsData.Speed.Normal,
        isInfected = false,
        isCultist = false,
        rank = 2,
        bogRotSpreadChance = 0.15f
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.Mage;
        defaultStats.minHealth = 40;
        defaultStats.maxHealth = 60;
        defaultStats.health = defaultStats.minHealth;
        defaultStats.minAttack = 15;
        defaultStats.maxAttack = 25;
        defaultStats.attack = 15;
        defaultStats.minDefense = 0;
        defaultStats.maxDefense = 5;
        defaultStats.defense = 0;
        defaultStats.morale = 100;
        defaultStats.sanity = 100;
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
        updatedStats.attack += 5;
        target.SetStats(updatedStats);
    }
}