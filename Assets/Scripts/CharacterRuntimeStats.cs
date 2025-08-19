using UnityEngine;
using UnityEngine.Events;

public class CharacterRuntimeStats : MonoBehaviour
{
    [SerializeField] private ScriptableObject characterSO;
    [SerializeField] private CharacterStatsData stats;
    public UnityEvent<CharacterRuntimeStats> OnInfected = new UnityEvent<CharacterRuntimeStats>();

    public CharacterStatsData Stats => stats;
    public bool IsCultist => characterSO is HeroSO heroSO && heroSO.Stats.isCultist;
    public ScriptableObject CharacterSO => characterSO;

    public void SetCharacterSO(ScriptableObject so)
    {
        characterSO = so;
    }

    public void Initialize()
    {
        if (characterSO == null)
        {
            return;
        }
        if (characterSO is HeroSO heroSO)
        {
            heroSO.ApplyStats(this);
        }
        else if (characterSO is MonsterSO monsterSO)
        {
            monsterSO.ApplyStats(this);
        }
        gameObject.AddComponent<SpriteAnimation>();
    }

    public void SetStats(CharacterStatsData newStats)
    {
        stats = newStats;
    }

    public bool TakeDamage(float damage)
    {
        if (characterSO is MonsterSO monsterSO && monsterSO.CheckDodge())
        {
            return false;
        }
        if (characterSO is HeroSO heroSO)
        {
            bool isDead = heroSO.TakeDamage(ref stats, damage);
            SetStats(stats);
            return isDead;
        }
        if (characterSO is MonsterSO monsterSO2)
        {
            bool isDead = monsterSO2.TakeDamage(ref stats, damage);
            SetStats(stats);
            return isDead;
        }
        return false;
    }

    public void ApplyMoraleDamage(float amount)
    {
        if (characterSO is HeroSO heroSO)
        {
            heroSO.ApplyMoraleDamage(ref stats, amount);
            SetStats(stats);
            return;
        }
        if (characterSO is MonsterSO monsterSO)
        {
            monsterSO.ApplyMoraleDamage(ref stats, amount);
            SetStats(stats);
        }
    }

    public void ApplySlowEffect(int tickDelay)
    {
        if (characterSO is HeroSO heroSO)
        {
            heroSO.ApplySlowEffect(ref stats, tickDelay);
            SetStats(stats);
            return;
        }
        if (characterSO is MonsterSO monsterSO)
        {
            monsterSO.ApplySlowEffect(ref stats, tickDelay);
            SetStats(stats);
        }
    }

    public void ResetStats()
    {
        if (characterSO is HeroSO heroSO)
        {
            heroSO.ApplyStats(this);
            return;
        }
        if (characterSO is MonsterSO monsterSO)
        {
            monsterSO.ApplyStats(this);
        }
    }

    public bool TryInfect()
    {
        bool infected = false;
        if (characterSO is HeroSO heroSO)
        {
            infected = heroSO.TryInfect(ref stats, stats.morale);
        }
        else if (characterSO is MonsterSO monsterSO)
        {
            infected = monsterSO.TryInfect(ref stats, stats.morale);
        }
        if (infected)
        {
            OnInfected.Invoke(this);
            SetStats(stats);
        }
        return infected;
    }

    public bool CheckMurderCondition(CharacterRuntimeStats other, int aliveCount)
    {
        if (characterSO is HeroSO heroSO && stats.isCultist)
        {
            bool murdered = heroSO.CheckMurderCondition(ref stats, other, aliveCount);
            SetStats(stats);
            return murdered;
        }
        return false;
    }
}