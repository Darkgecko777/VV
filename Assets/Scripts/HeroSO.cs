using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "HeroSO", menuName = "VirulentVentures/HeroSO", order = 1)]
    public class HeroSO : ScriptableObject
    {
        [SerializeField] private CharacterTypeSO characterType;
        [SerializeField] private CharacterStatsData stats;
        [SerializeField] private List<string> abilityIds = new List<string>();
        [SerializeField] private int partyPosition = 1;

        public CharacterTypeSO CharacterType => characterType;
        public CharacterStatsData Stats => stats;
        public List<string> AbilityIds => abilityIds;
        public int PartyPosition => partyPosition;

        private void Awake()
        {
            if (abilityIds.Count == 0 && characterType != null)
            {
                abilityIds.Add("BasicAttack");
                switch (characterType.Id)
                {
                    case "Fighter":
                        abilityIds.Add("FighterAttack");
                        break;
                    case "Healer":
                        abilityIds.Add("HealerHeal");
                        break;
                    case "Scout":
                        abilityIds.Add("ScoutDefend");
                        break;
                    case "TreasureHunter":
                        abilityIds.Add("TreasureHunterBoost");
                        break;
                }
                if (characterType.CanBeCultist)
                {
                    abilityIds.Add("CULT_Sabotage"); // Placeholder for cultist ability
                }
            }
        }

        public void ApplyStats(HeroStats target)
        {
            if (stats == null || characterType == null)
            {
                return;
            }

            target.Health = stats.Health;
            target.MaxHealth = stats.MaxHealth;
            target.Attack = stats.Attack;
            target.Defense = stats.Defense;
            target.SlowTickDelay = stats.SlowTickDelay;
        }

        public void ApplySpecialAbility(HeroStats target, PartyData partyData)
        {
            foreach (var abilityId in abilityIds)
            {
                AbilityData? ability = AbilityDatabase.GetHeroAbility(abilityId);
                if (ability.HasValue)
                {
                    ability.Value.Effect?.Invoke(target, partyData);
                }
            }
        }

        public bool TakeDamage(ref HeroStats stats, int damage)
        {
            int damageTaken = Mathf.Max(damage - stats.Defense, 0);
            stats.Health = Mathf.Max(stats.Health - damageTaken, 0);
            return stats.Health <= 0;
        }

        private void OnValidate()
        {
            if (characterType == null || string.IsNullOrEmpty(characterType.Id))
            {
                return;
            }
            if (stats.Type == null)
            {
                stats.Type = characterType;
            }
            if (partyPosition < 1 || partyPosition > 7)
            {
                partyPosition = Mathf.Clamp(partyPosition, 1, 7);
            }
        }
    }
}