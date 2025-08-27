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
                Debug.LogWarning($"HeroSO: Cannot apply stats for {characterType?.Id ?? "unknown"} - stats or characterType is null");
                return;
            }

            target.Health = stats.Health;
            target.MaxHealth = stats.MaxHealth;
            target.Attack = stats.Attack;
            target.Defense = stats.Defense;
            target.Speed = stats.Speed;
            target.Evasion = stats.Evasion;
            target.Morale = stats.Morale;
            target.MaxMorale = stats.MaxMorale;
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
            int damageTaken = Mathf.Max(damage - stats.Defense, 0); // Negative Defense handled later
            stats.Health = Mathf.Max(stats.Health - damageTaken, 0);
            return stats.Health <= 0;
        }

        private void OnValidate()
        {
            if (characterType == null || string.IsNullOrEmpty(characterType.Id))
            {
                Debug.LogWarning("HeroSO: CharacterType or its Id is null or empty");
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
            // Clamp stats to valid ranges
            stats.Health = Mathf.Clamp(stats.Health, 0, stats.MaxHealth);
            stats.Evasion = Mathf.Clamp(stats.Evasion, 0, 100);
            stats.Morale = Mathf.Clamp(stats.Morale, 0, stats.MaxMorale);
            stats.Speed = Mathf.Clamp(stats.Speed, 1, 8);
        }
    }
}