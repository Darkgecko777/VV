using UnityEngine;

[CreateAssetMenu(fileName = "WraithSO", menuName = "VirulentVentures/WraithSO", order = 9)]
public class WraithSO : MonsterSO
{
    [SerializeField]
    private CharacterStatsData defaultStats = new CharacterStatsData
    {
        characterType = CharacterStatsData.CharacterType.Wraith,
        minHealth = 40,
        maxHealth = 60,
        health = 40,
        minAttack = 15,
        maxAttack = 25,
        attack = 15,
        minDefense = 0,
        maxDefense = 5,
        defense = 0,
        morale = 60,
        sanity = 0,
        speed = CharacterStatsData.Speed.Fast,
        isInfected = false,
        isCultist = false,
        rank = 2
    };

    void OnEnable()
    {
        defaultStats.characterType = CharacterStatsData.CharacterType.Wraith;
        defaultStats.minHealth = 40;
        defaultStats.maxHealth = 60;
        defaultStats.health = defaultStats.minHealth;
        defaultStats.minAttack = 15;
        defaultStats.maxAttack = 25;
        defaultStats.attack = 15;
        defaultStats.minDefense = 0;
        defaultStats.maxDefense = 5;
        defaultStats.defense = 0;
        defaultStats.morale = 60;
        defaultStats.sanity = 0;
        defaultStats.speed = CharacterStatsData.Speed.Fast;
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

    public override bool CheckDodge()
    {
        return Random.value <= 0.2f;
    }
}