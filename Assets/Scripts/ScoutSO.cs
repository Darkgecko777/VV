using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "ScoutSO", menuName = "VirulentVentures/ScoutSO", order = 5)]
    public class ScoutSO : HeroSO
    {
        [SerializeField]
        private CharacterStatsData defaultStats = new CharacterStatsData
        {
            Type = null, // Set in Inspector with Scout CharacterTypeSO
            MinHealth = 50,
            MaxHealth = 70,
            Health = 50,
            MinAttack = 12,
            MaxAttack = 18,
            Attack = 12,
            MinDefense = 3,
            MaxDefense = 6,
            Defense = 3,
            Morale = 100,
            Sanity = 100,
            CharacterSpeed = CharacterStatsData.Speed.Fast,
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
            target.Defense += 2;
        }
    }
}