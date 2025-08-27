using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "MonsterSO", menuName = "VirulentVentures/MonsterSO", order = 7)]
    public class MonsterSO : ScriptableObject
    {
        [SerializeField] private CharacterTypeSO characterType;
        [SerializeField] private CharacterStatsData stats;
        [SerializeField] private List<string> abilityIds = new List<string>();

        public CharacterTypeSO CharacterType => characterType;
        public CharacterStatsData Stats => stats;
        public List<string> AbilityIds => abilityIds;

        private void Awake()
        {
            if (abilityIds.Count == 0 && characterType != null)
            {
                abilityIds.Add("BasicAttack");
                switch (characterType.Id)
                {
                    case "Ghoul":
                        abilityIds.Add("GhoulClaw");
                        abilityIds.Add("GhoulRend");
                        break;
                    case "Wraith":
                        abilityIds.Add("WraithStrike");
                        break;
                    default:
                        abilityIds.Add("DefaultMonsterAbility");
                        break;
                }
            }
        }

        public void ApplyStats(MonsterStats target)
        {
            if (stats == null || characterType == null)
            {
                Debug.LogWarning($"MonsterSO: Cannot apply stats for {characterType?.Id ?? "unknown"} - stats or characterType is null");
                return;
            }

            target.Health = stats.Health;
            target.MaxHealth = stats.MaxHealth;
            target.Attack = stats.Attack;
            target.Defense = stats.Defense; 
            target.Speed = stats.Speed;
            target.Evasion = stats.Evasion;
            target.Morale = 0; // Monsters don't use Morale
            target.MaxMorale = 0; // Monsters don't use MaxMorale
        }

        public void ApplySpecialAbility(MonsterStats target, PartyData partyData)
        {
            foreach (var abilityId in abilityIds)
            {
                AbilityData? ability = AbilityDatabase.GetMonsterAbility(abilityId);
                if (ability.HasValue)
                {
                    ability.Value.Effect?.Invoke(target, partyData);
                }
            }
        }

        public bool TakeDamage(ref MonsterStats stats, int damage)
        {
            int damageTaken = Mathf.Max(damage - stats.Defense, 0); // Negative Defense handled later
            stats.Health = Mathf.Max(stats.Health - damageTaken, 0);
            return stats.Health <= 0;
        }

        private void OnValidate()
        {
            if (characterType == null || string.IsNullOrEmpty(characterType.Id))
            {
                Debug.LogWarning("MonsterSO: CharacterType or its Id is null or empty");
                return;
            }
            if (stats.Type == null)
            {
                stats.Type = characterType;
            }
            // Clamp stats to valid ranges
            stats.Health = Mathf.Clamp(stats.Health, 0, stats.MaxHealth);
            stats.Evasion = Mathf.Clamp(stats.Evasion, 0, 100);
            stats.Speed = Mathf.Clamp(stats.Speed, 1, 8);
            stats.Morale = 0; // Monsters don't use Morale
            stats.MaxMorale = 0; // Monsters don't use MaxMorale
        }
    }
}