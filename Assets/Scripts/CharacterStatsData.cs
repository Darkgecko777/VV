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
        [SerializeField] private int attack;
        [SerializeField] private int defense;
        [SerializeField] private int slowTickDelay;
        [SerializeField] private Speed speed;

        public CharacterTypeSO Type { get => type; set => type = value; }
        public int Health { get => health; set => health = value; }
        public int MaxHealth { get => maxHealth; set => maxHealth = value; }
        public int Attack { get => attack; set => attack = value; }
        public int Defense { get => defense; set => defense = value; }
        public int SlowTickDelay { get => slowTickDelay; set => slowTickDelay = value; }
        public Speed CharacterSpeed { get => speed; set => speed = value; }

        public enum Speed
        {
            Slow,
            Normal,
            Fast,
            VeryFast
        }
    }
}