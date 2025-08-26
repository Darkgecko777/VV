using UnityEngine;

namespace VirulentVentures
{
    [System.Serializable]
    public struct DisplayStats
    {
        public string Name;
        public int Health;
        public int MaxHealth;
        public int Attack;
        public int Defense;
        public int? Morale; // Null for monsters
        public int? Sanity; // Null for monsters
        public bool IsHero;

        public DisplayStats(string name, int health, int maxHealth, int attack, int defense, int? morale, int? sanity, bool isHero)
        {
            Name = name;
            Health = health;
            MaxHealth = maxHealth;
            Attack = attack;
            Defense = defense;
            Morale = morale;
            Sanity = sanity;
            IsHero = isHero;
        }
    }
}