using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "HeroSO", menuName = "VirulentVentures/HeroSO", order = 1)]
    public class HeroSO : ScriptableObject
    {
        [SerializeField] private CharacterTypeSO characterType; // For ID, isHero, etc.
        [SerializeField] private CharacterStatsData stats;
        [SerializeField] private SpecialAbilitySO specialAbility;
        private const int lowMoraleThreshold = 50;

        public CharacterTypeSO CharacterType => characterType;
        public CharacterStatsData Stats => stats;
        public SpecialAbilitySO SpecialAbility => specialAbility;

        public void ApplyStats(HeroStats target)
        {
            if (stats == null || characterType == null)
            {
                Debug.LogError($"HeroSO.ApplyStats: Stats or CharacterType is null for {name}");
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

        public void ApplySpecialAbility(HeroStats target, PartyData partyData)
        {
            if (specialAbility != null)
            {
                specialAbility.Apply(target, partyData);
            }
        }

        public bool TakeDamage(ref HeroStats stats, int damage)
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

        public bool CheckMurderCondition(ref CharacterStatsData stats, HeroStats other, int aliveCount)
        {
            if (!stats.IsCultist || aliveCount != 2 || other == null || other.IsCultist)
            {
                return false;
            }
            int hpThreshold = other.MaxHealth / 5; // 20% HP
            if (other.Health <= hpThreshold)
            {
                other.Health = 0;
                return true;
            }
            return false;
        }

        private void OnValidate()
        {
            if (characterType == null || string.IsNullOrEmpty(characterType.Id))
            {
                Debug.LogWarning($"HeroSO.OnValidate: CharacterType or CharacterType.Id is missing for {name}! This will break VisualConfig sprite lookup.");
            }
            if (stats.Type == null)
            {
                stats.Type = characterType; // Sync stats.Type with characterType
            }
            if (specialAbility == null)
            {
                Debug.LogWarning($"HeroSO.OnValidate: SpecialAbility is missing for {name}!");
            }
        }
    }
}