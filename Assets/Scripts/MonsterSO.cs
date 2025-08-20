using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "MonsterSO", menuName = "VirulentVentures/MonsterSO", order = 7)]
    public class MonsterSO : ScriptableObject
    {
        [SerializeField] private CharacterTypeSO characterType; // For ID, isMonster, etc.
        [SerializeField] private CharacterStatsData stats;
        [SerializeField] private MonsterAbilitySO specialAbility;

        public CharacterTypeSO CharacterType => characterType;
        public CharacterStatsData Stats => stats;
        public MonsterAbilitySO SpecialAbility => specialAbility;

        public void ApplyStats(MonsterStats target)
        {
            if (stats == null || characterType == null)
            {
                Debug.LogError($"MonsterSO.ApplyStats: Stats or CharacterType is null for {name}");
                return;
            }

            int rankMultiplier = stats.Rank switch
            {
                1 => 80,
                3 => 120,
                _ => 100
            };

            target.Health = (stats.MaxHealth * rankMultiplier) / 100;
            target.MaxHealth = target.Health;
            target.MinHealth = (stats.MinHealth * rankMultiplier) / 100;
            target.Attack = (stats.MaxAttack * rankMultiplier) / 100;
            target.MinAttack = (stats.MinAttack * rankMultiplier) / 100;
            target.MaxAttack = (stats.MaxAttack * rankMultiplier) / 100;
            target.Defense = (stats.MaxDefense * rankMultiplier) / 100;
            target.MinDefense = (stats.MinDefense * rankMultiplier) / 100;
            target.MaxDefense = (stats.MaxDefense * rankMultiplier) / 100;
            target.IsInfected = false;
            target.SlowTickDelay = 0;
        }

        public bool TakeDamage(ref MonsterStats stats, int damage)
        {
            int damageTaken = Mathf.Max(damage - stats.Defense, 0);
            stats.Health = Mathf.Max(stats.Health - damageTaken, 0);
            return stats.Health <= 0;
        }

        public void ApplyMoraleDamage(ref CharacterStatsData stats, int amount)
        {
            stats.Morale = Mathf.Max(stats.Morale - amount, 0);
        }

        public void ApplySlowEffect(ref CharacterStatsData stats, int tickDelay)
        {
            stats.SlowTickDelay += tickDelay;
        }

        public void ResetStats(ref CharacterStatsData stats)
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

        public bool CheckDodge()
        {
            return specialAbility != null && specialAbility.CheckDodge();
        }

        private void OnValidate()
        {
            if (characterType == null || string.IsNullOrEmpty(characterType.Id))
            {
                Debug.LogWarning($"MonsterSO.OnValidate: CharacterType or CharacterType.Id is missing for {name}! This will break VisualConfig sprite lookup.");
            }
            if (stats.Type == null)
            {
                stats.Type = characterType; // Sync stats.Type with characterType for VisualConfig
            }
        }
    }
}