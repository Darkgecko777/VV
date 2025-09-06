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
        public int Immunity { get; set; }
        public bool HasRetreated { get; set; } = false;
        public bool IsInfected { get; set; } = false;
        public int PartyPosition { get; set; }
        public bool IsHero => Type == CharacterType.Hero;

        public CharacterStats(string id, Vector3 position, CharacterType type)
        {
            Id = id;
            Type = type;
            var data = type == CharacterType.Hero
                ? CharacterLibrary.GetHeroData(id)
                : CharacterLibrary.GetMonsterData(id);
            Health = type == CharacterType.Hero ? data.Health : data.MaxHealth;
            MaxHealth = data.MaxHealth;
            Attack = data.Attack;
            Defense = data.Defense;
            Speed = data.Speed;
            Evasion = data.Evasion;
            Morale = type == CharacterType.Hero ? data.Morale : 0;
            MaxMorale = type == CharacterType.Hero ? data.MaxMorale : 0;
            Immunity = data.Immunity;
            // Map Vector3 position to PartyPosition (1-4 based on index or x-coordinate)
            PartyPosition = data.PartyPosition; // Use CharacterLibrary's PartyPosition
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
            public int immunity;
            public bool isHero;

            public DisplayStats(string name, int health, int maxHealth, int attack, int defense, int speed, int evasion, int morale, int maxMorale, int immunity, bool isHero)
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
                this.immunity = immunity;
                this.isHero = isHero;
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
                immunity: Immunity,
                isHero: IsHero
            );
        }
    }

    public enum CharacterType
    {
        Hero,
        Monster
    }
}