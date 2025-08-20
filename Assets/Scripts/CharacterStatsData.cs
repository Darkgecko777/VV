using System;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public class CharacterStatsData
    {
        [SerializeField] private CharacterTypeSO type;
        [SerializeField] private int health;
        [SerializeField] private int maxHealth;
        [SerializeField] private int minHealth;
        [SerializeField] private int attack;
        [SerializeField] private int minAttack;
        [SerializeField] private int maxAttack;
        [SerializeField] private int defense;
        [SerializeField] private int minDefense;
        [SerializeField] private int maxDefense;
        [SerializeField] private int morale;
        [SerializeField] private int sanity;
        [SerializeField] private int rank;
        [SerializeField] private bool isInfected;
        [SerializeField] private int slowTickDelay;
        [SerializeField] private Speed speed;
        [SerializeField] private bool isCultist;

        public CharacterTypeSO Type { get => type; set => type = value; }
        public int Health { get => health; set => health = value; }
        public int MaxHealth { get => maxHealth; set => maxHealth = value; }
        public int MinHealth { get => minHealth; set => minHealth = value; }
        public int Attack { get => attack; set => attack = value; }
        public int MinAttack { get => minAttack; set => minAttack = value; }
        public int MaxAttack { get => maxAttack; set => maxAttack = value; }
        public int Defense { get => defense; set => defense = value; }
        public int MinDefense { get => minDefense; set => minDefense = value; }
        public int MaxDefense { get => maxDefense; set => maxDefense = value; }
        public int Morale { get => morale; set => morale = value; }
        public int Sanity { get => sanity; set => sanity = value; }
        public int Rank { get => rank; set => rank = value; }
        public bool IsInfected { get => isInfected; set => isInfected = value; }
        public int SlowTickDelay { get => slowTickDelay; set => slowTickDelay = value; }
        public Speed CharacterSpeed { get => speed; set => speed = value; } // Renamed from Speed to CharacterSpeed
        public bool IsCultist { get => isCultist; set => isCultist = value; }

        public enum Speed
        {
            Slow,
            Normal,
            Fast,
            VeryFast
        }
    }
}