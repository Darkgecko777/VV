using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "TreasureHunterSO", menuName = "VirulentVentures/TreasureHunterSO", order = 4)]
    public class TreasureHunterSO : HeroSO
    {
        [SerializeField]
        private CharacterStatsData defaultStats = new CharacterStatsData
        {
            Type = null, // Set in Inspector with TreasureHunter CharacterTypeSO
            MinHealth = 60,
            MaxHealth = 80,
            Health = 60,
            MinAttack = 10,
            MaxAttack = 15,
            Attack = 10,
            MinDefense = 3,
            MaxDefense = 7,
            Defense = 3,
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
                HeroStats[] allies = partyData.FindAllies();
                foreach (var ally in allies)
                {
                    if (ally.Health > 0)
                    {
                        ally.Morale = Mathf.Min(ally.Morale + 3, 100);
                    }
                }
            }
        }
    }
}