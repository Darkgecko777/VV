using System;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public class CharacterStatsData
    {
        [SerializeField] private int health;
        [SerializeField] private int maxHealth;
        [SerializeField] private int attack;
        [SerializeField] private int defense;
        [SerializeField] private int speed;
        [SerializeField] private int evasion; // New: 0-100% dodge chance
        [SerializeField] private int morale; // New: 0-100% for retreat (heroes only)
        [SerializeField] private int maxMorale; // New: Cap for Morale

        public int Health { get => health; set => health = value; }
        public int MaxHealth { get => maxHealth; set => maxHealth = value; }
        public int Attack { get => attack; set => attack = value; }
        public int Defense { get => defense; set => defense = value; }
        public int Speed { get => speed; set => speed = Mathf.Clamp(value, 1, 8); }
        public int Evasion { get => evasion; set => evasion = Mathf.Clamp(value, 0, 100); }
        public int Morale { get => morale; set => morale = Mathf.Clamp(value, 0, 100); }
        public int MaxMorale { get => maxMorale; set => maxMorale = value; }
    }
}