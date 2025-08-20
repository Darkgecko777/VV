using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "HealerSO", menuName = "VirulentVentures/HealerSO", order = 3)]
    public class HealerSO : HeroSO
    {
        [SerializeField]
        private CharacterStatsData defaultStats = new CharacterStatsData
        {
            Type = null, // Set in Inspector with Healer CharacterTypeSO
            MinHealth = 50,
            MaxHealth = 70,
            Health = 50,
            MinAttack = 5,
            MaxAttack = 10,
            Attack = 5,
            MinDefense = 5,
            MaxDefense = 8,
            Defense = 5,
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
            if (partyData != null)
            {
                HeroStats lowestAlly = partyData.FindLowestHealthAlly();
                if (lowestAlly != null && lowestAlly.Health > 0)
                {
                    lowestAlly.Health = Mathf.Min(lowestAlly.Health + 5, lowestAlly.MaxHealth);
                }
            }
        }
    }
}