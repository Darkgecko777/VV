using UnityEngine;

[CreateAssetMenu(fileName = "FighterSO", menuName = "VirulentVentures/FighterSO", order = 2)]
public class FighterSO : HeroSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Fighter,
        minHealth = 80,
        maxHealth = 100,
        health = 80,
        minAttack = 15,
        maxAttack = 20,
        attack = 15,
        minDefense = 5,
        maxDefense = 10,
        defense = 5,
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
        defaultStats.characterType = CharacterStatsData.CharacterType.Fighter;
        defaultStats.minHealth = 80;
        defaultStats.maxHealth = 100;
        defaultStats.health = defaultStats.minHealth;
        defaultStats.minAttack = 15;
        defaultStats.maxAttack = 20;
        defaultStats.attack = 15;
        defaultStats.minDefense = 5;
        defaultStats.maxDefense = 10;
        defaultStats.defense = 5;
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
        if (target.Stats.health < Mathf.RoundToInt(target.Stats.maxHealth * 0.3f))
        {
            CharacterStatsData updatedStats = target.Stats;
            updatedStats.attack += 3;
            target.SetStats(updatedStats);
        }
    }
}