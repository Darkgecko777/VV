using System;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "CharacterSO", menuName = "VirulentVentures/CharacterSO")]
    public class CharacterSO : ScriptableObject
    {
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
        [SerializeField] private int infectivity;
        [SerializeField] private bool canBeCultist;
        [SerializeField] private int partyPosition;
        [SerializeField] private AbilitySO[] abilities; // Changed to AbilitySO[] for Rank 1
        [SerializeField] private int rank = 1; // Added rank field, default 1

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
        public int Infectivity => infectivity;
        public bool CanBeCultist => canBeCultist;
        public int PartyPosition => partyPosition;
        public AbilitySO[] Abilities => abilities;
        public int Rank => rank; // Added Rank getter

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
                infectivity: infectivity,
                isHero: isHero,
                isInfected: false,
                rank: rank // Added rank
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
        }
    }
}