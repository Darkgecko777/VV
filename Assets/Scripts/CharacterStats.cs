using System;
using UnityEngine;

namespace VirulentVentures
{
    public class CharacterStats : ICombatUnit
    {
        public string Id { get; set; }
        public CharacterType Type { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        public int Evasion { get; set; }
        public int Morale { get; set; }
        public int MaxMorale { get; set; }
        public int Infectivity { get; set; }
        public bool HasRetreated { get; set; } = false;
        public bool IsInfected { get; set; } = false;
        public int PartyPosition { get; set; }
        public ScriptableObject[] Abilities { get; set; }
        public bool IsHero => Type == CharacterType.Hero;
        public int Rank { get; set; } // Added Rank property

        public CharacterStats(CharacterSO data, Vector3 position)
        {
            if (data == null)
            {
                Debug.LogError("CharacterStats: Null CharacterSO provided, using default values.");
                Id = "Default";
                Type = CharacterType.Hero;
                Health = 50;
                MaxHealth = 50;
                Attack = 10;
                Defense = 5;
                Speed = 3;
                Evasion = 10;
                Morale = 100;
                MaxMorale = 100;
                Infectivity = 20;
                PartyPosition = 1;
                Abilities = new ScriptableObject[0];
                Rank = 1; // Default Rank for null data
                return;
            }

            Id = data.Id;
            Type = data.Type;
            Health = Type == CharacterType.Hero ? data.Health : data.MaxHealth;
            MaxHealth = data.MaxHealth;
            Attack = data.Attack;
            Defense = data.Defense;
            Speed = data.Speed;
            Evasion = data.Evasion;
            Morale = Type == CharacterType.Hero ? data.Morale : 0;
            MaxMorale = Type == CharacterType.Hero ? data.MaxMorale : 0;
            Infectivity = data.Infectivity;
            PartyPosition = data.PartyPosition;
            Abilities = data.Abilities ?? new ScriptableObject[0];
            Rank = data.Rank; // Set Rank from CharacterSO
        }

        public struct DisplayStats
        {
            public string name;
            public int health;
            public int maxHealth;
            public int attack;
            public int defense;
            public int speed;
            public int evasion;
            public int morale;
            public int maxMorale;
            public int infectivity;
            public bool isHero;
            public bool isInfected;
            public int rank; // Added rank field

            public DisplayStats(string name, int health, int maxHealth, int attack, int defense, int speed, int evasion, int morale, int maxMorale, int infectivity, bool isHero, bool isInfected, int rank)
            {
                this.name = name;
                this.health = health;
                this.maxHealth = maxHealth;
                this.attack = attack;
                this.defense = defense;
                this.speed = speed;
                this.evasion = evasion;
                this.morale = morale;
                this.maxMorale = maxMorale;
                this.infectivity = infectivity;
                this.isHero = isHero;
                this.isInfected = isInfected;
                this.rank = rank;
            }
        }

        public DisplayStats GetDisplayStats()
        {
            return new DisplayStats(
                name: Id,
                health: Health,
                maxHealth: MaxHealth,
                attack: Attack,
                defense: Defense,
                speed: Mathf.Clamp(Speed, 1, 8),
                evasion: Mathf.Clamp(Evasion, 0, 100),
                morale: Morale,
                maxMorale: MaxMorale,
                infectivity: Infectivity,
                isHero: IsHero,
                isInfected: IsInfected,
                rank: Rank // Added rank
            );
        }
    }

    public enum CharacterType
    {
        Hero,
        Monster
    }
}