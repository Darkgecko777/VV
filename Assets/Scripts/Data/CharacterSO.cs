using System.Collections.Generic;
using UnityEngine;
using System; // NEW: For Serializable

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "CharacterSO", menuName = "VirulentVentures/CharacterSO")]
    public class CharacterSO : ScriptableObject
    {
        // NEW: Capabilities for virus transmission (monsters only)
        [Serializable]
        public struct MonsterCapabilities
        {
            public bool canTransmitHealth;
            public bool canTransmitMorale;
            public bool canTransmitEnvironment;
            public bool canTransmitObstacle;
        }

        [SerializeField] private string id;
        [SerializeField] private CharacterType type;
        [SerializeField] private int health; // For heroes; monsters use maxHealth
        [SerializeField] private int maxHealth;
        [SerializeField] private int attack;
        [SerializeField] private int defense;
        [SerializeField] private int speed;
        [SerializeField] private int evasion;
        [SerializeField] private int morale; // Heroes only
        [SerializeField] private int maxMorale; // Heroes only
        [SerializeField] private int immunity; // Renamed from infectivity
        [SerializeField] private bool canBeCultist;
        [SerializeField] private int partyPosition;
        [SerializeField] private int rank = 1; // Default 1
        [SerializeField] private Sprite portrait; // For Temple scene
        [SerializeField] private Sprite combatSprite; // For Combat scene
        [SerializeField] private AbilitySO[] abilities = new AbilitySO[0];
        [SerializeField] private MonsterCapabilities capabilities; // NEW: For monsters

        public string Id => id;
        public CharacterType Type => type;
        public int Health => health;
        public int MaxHealth => maxHealth;
        public int Attack => attack;
        public int Defense => defense;
        public int Speed => speed;
        public int Evasion => evasion;
        public int Morale => morale;
        public int MaxMorale => maxMorale;
        public int Immunity => immunity; // Renamed
        public bool CanBeCultist => canBeCultist;
        public int PartyPosition => partyPosition;
        public int Rank => rank;
        public Sprite Portrait => portrait;
        public Sprite CombatSprite => combatSprite;
        public AbilitySO[] Abilities => abilities;
        public MonsterCapabilities Capabilities => capabilities; // NEW

        public CharacterStats.DisplayStats GetDisplayStats(bool isHero)
        {
            return new CharacterStats.DisplayStats(
                name: id,
                health: isHero ? health : maxHealth,
                maxHealth: maxHealth,
                attack: attack,
                defense: defense,
                speed: Mathf.Clamp(speed, 1, 8),
                evasion: Mathf.Clamp(evasion, 0, 100),
                morale: isHero ? morale : 0,
                maxMorale: isHero ? maxMorale : 0,
                immunity: immunity, // Renamed
                isHero: isHero,
                isInfected: false,
                infections: new List<VirusSO>(),
                rank: rank,
                combatSprite: combatSprite
            );
        }

        private void OnValidate()
        {
            if (partyPosition < 0 || partyPosition > 4)
            {
                Debug.LogWarning($"CharacterSO {id}: Invalid PartyPosition {partyPosition}. Must be 0-4.");
            }
            if (type == CharacterType.Hero && (health <= 0 || maxMorale <= 0))
            {
                Debug.LogWarning($"CharacterSO {id}: Hero must have positive Health and MaxMorale.");
            }
            if (rank < 1 || rank > 3)
            {
                Debug.LogWarning($"CharacterSO {id}: Invalid Rank {rank}. Must be 1-3.");
            }
            if (portrait == null)
            {
                Debug.LogWarning($"CharacterSO {id}: Portrait sprite is null.");
            }
            if (combatSprite == null)
            {
                Debug.LogWarning($"CharacterSO {id}: Combat sprite is null.");
            }
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"CharacterSO: Id is empty or null.");
            }
            if (abilities == null || abilities.Length == 0)
            {
                Debug.LogWarning($"CharacterSO {id}: No Abilities assigned; will use defaults from AbilityDatabase.");
            }
            // NEW: Validate capabilities for monsters
            if (type == CharacterType.Monster && !capabilities.canTransmitHealth && !capabilities.canTransmitMorale && !capabilities.canTransmitEnvironment && !capabilities.canTransmitObstacle)
            {
                Debug.LogWarning($"CharacterSO {id}: Monster has no transmission capabilities—will not seed viruses.");
            }
        }
    }
}