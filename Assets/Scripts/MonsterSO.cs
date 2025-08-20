using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "MonsterSO", menuName = "VirulentVentures/MonsterSO", order = 7)]
    public class MonsterSO : ScriptableObject
    {
        [SerializeField] private CharacterStatsData stats;
        [SerializeField] private Sprite sprite;

        public CharacterStatsData Stats => stats;
        public Sprite Sprite => sprite;

        protected void SetStats(CharacterStatsData newStats)
        {
            stats = newStats;
        }

        public virtual void ApplyStats(MonsterStats target)
        {
            CharacterStatsData newStats = stats;
            int rankMultiplier = newStats.Rank switch
            {
                1 => 80,
                3 => 120,
                _ => 100
            };

            target.Health = (newStats.MaxHealth * rankMultiplier) / 100;
            target.MaxHealth = target.Health;
            target.MinHealth = (newStats.MinHealth * rankMultiplier) / 100;
            target.Attack = (newStats.MaxAttack * rankMultiplier) / 100;
            target.MinAttack = (newStats.MinAttack * rankMultiplier) / 100;
            target.MaxAttack = (newStats.MaxAttack * rankMultiplier) / 100;
            target.Defense = (newStats.MaxDefense * rankMultiplier) / 100;
            target.MinDefense = (newStats.MinDefense * rankMultiplier) / 100;
            target.MaxDefense = (newStats.MaxDefense * rankMultiplier) / 100;
            target.IsInfected = false;
            target.SlowTickDelay = 0;
        }

        public virtual bool TakeDamage(ref MonsterStats stats, int damage)
        {
            int damageTaken = Mathf.Max(damage - stats.Defense, 0);
            stats.Health = Mathf.Max(stats.Health - damageTaken, 0);
            return stats.Health <= 0;
        }

        public virtual void ApplyMoraleDamage(ref CharacterStatsData stats, int amount)
        {
            stats.Morale = Mathf.Max(stats.Morale - amount, 0);
        }

        public virtual void ApplySlowEffect(ref CharacterStatsData stats, int tickDelay)
        {
            stats.SlowTickDelay += tickDelay;
        }

        public virtual void ResetStats(ref CharacterStatsData stats)
        {
            int rankMultiplier = stats.Rank switch
            {
                1 => 80,
                3 => 120,
                _ => 100
            };
            stats.MaxHealth = (stats.MaxHealth * rankMultiplier) / 100;
            stats.Health = stats.MaxHealth;
            stats.MinHealth = (stats.MinHealth * rankMultiplier) / 100;
            stats.Attack = (stats.MaxAttack * rankMultiplier) / 100;
            stats.MinAttack = (stats.MinAttack * rankMultiplier) / 100;
            stats.MaxAttack = (stats.MaxAttack * rankMultiplier) / 100;
            stats.Defense = (stats.MaxDefense * rankMultiplier) / 100;
            stats.MinDefense = (stats.MinDefense * rankMultiplier) / 100;
            stats.MaxDefense = (stats.MaxDefense * rankMultiplier) / 100;
            stats.IsInfected = false;
            stats.SlowTickDelay = 0;
        }

        public virtual bool CheckDodge()
        {
            return false;
        }
    }
}