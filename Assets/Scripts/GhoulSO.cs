using UnityEngine;

[CreateAssetMenu(fileName = "GhoulSO", menuName = "VirulentVentures/GhoulSO", order = 8)]
public class GhoulSO : MonsterSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Ghoul,
        minHealth = 50,
        maxHealth = 70,
        health = 50,
        minAttack = 12,
        maxAttack = 18,
        attack = 12,
        minDefense = 3,
        maxDefense = 8,
        defense = 3,
        morale = 80,
        sanity = 0,
        speed = CharacterStatsData.Speed.Normal,
        isInfected = false,
        isCultist = false,
        rank = 2,
        bogRotSpreadChance = 0.25f
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.Ghoul;
        defaultStats.minHealth = 50;
        defaultStats.maxHealth = 70;
        defaultStats.health = defaultStats.minHealth;
        defaultStats.minAttack = 12;
        defaultStats.maxAttack = 18;
        defaultStats.attack = 12;
        defaultStats.minDefense = 3;
        defaultStats.maxDefense = 8;
        defaultStats.defense = 3;
        defaultStats.morale = 80;
        defaultStats.sanity = 0;
        defaultStats.speed = CharacterStatsData.Speed.Normal;
        defaultStats.isInfected = false;
        defaultStats.isCultist = false;
        defaultStats.rank = 2;
        defaultStats.bogRotSpreadChance = 0.25f;
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
        newStats.bogRotSpreadChance = 0.25f;
        target.SetStats(newStats);
    }
}