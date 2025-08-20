using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "MageSO", menuName = "VirulentVentures/MageSO", order = 6)]
    public class MageSO : HeroSO
    {
        [SerializeField]
        private CharacterStatsData defaultStats = new CharacterStatsData
        {
            Type = null, // Set in Inspector with Mage CharacterTypeSO
            MinHealth = 40,
            MaxHealth = 60,
            Health = 40,
            MinAttack = 15,
            MaxAttack = 25,
            Attack = 15,
            MinDefense = 0,
            MaxDefense = 5,
            Defense = 0,
            Morale = 100,
            Sanity = 100,
            CharacterSpeed = CharacterStatsData.Speed.Normal,
            IsInfected = false,
            IsCultist = false,
            Rank = 2
        };

        private void OnValidate()
        {
            SetStats(defaultStats);
        }

        public override void ApplyStats(HeroStats target)
        {
            CharacterStatsData newStats = defaultStats;
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

        public override void ApplySpecialAbility(HeroStats target, PartyData partyData)
        {
            target.Attack += 5;
        }
    }
}