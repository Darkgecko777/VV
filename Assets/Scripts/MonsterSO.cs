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
                return;
            }

            target.Health = stats.Health;
            target.MaxHealth = stats.MaxHealth;
            target.Attack = stats.Attack;
            target.Defense = stats.Defense;
            target.SlowTickDelay = stats.SlowTickDelay;
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
            int damageTaken = Mathf.Max(damage - stats.Defense, 0);
            stats.Health = Mathf.Max(stats.Health - damageTaken, 0);
            return stats.Health <= 0;
        }

        public bool CheckDodge()
        {
            foreach (var abilityId in abilityIds)
            {
                AbilityData? ability = AbilityDatabase.GetMonsterAbility(abilityId);
                if (ability.HasValue && ability.Value.CanDodge)
                {
                    return true;
                }
            }
            return false;
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
        }
    }
}