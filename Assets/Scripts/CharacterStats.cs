using System;
using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    public enum CharacterType { Hero, Monster }

    [Serializable]
    public class CharacterStats : ICombatUnit
    {
        [Serializable]
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
            public int immunity; // New immunity stat
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

        [SerializeField] private string _id;
        [SerializeField] private int _health;
        [SerializeField] private int _maxHealth;
        [SerializeField] private int _attack;
        [SerializeField] private int _defense;
        [SerializeField] private int _speed;
        [SerializeField] private int _evasion;
        [SerializeField] private int _morale;
        [SerializeField] private int _maxMorale;
        [SerializeField] private int _immunity; // New immunity stat
        [SerializeField] private Vector3 _position;
        [SerializeField] private string _abilityId;
        [SerializeField] private bool _isCultist;
        [SerializeField] private CharacterType _type;
        [SerializeField] private int _partyPosition;
        [SerializeField] private bool _hasRetreated;

        public CharacterStats(string id, Vector3 position, CharacterType type)
        {
            var data = type == CharacterType.Hero ? CharacterLibrary.GetHeroData(id) : CharacterLibrary.GetMonsterData(id);
            _id = data.Id;
            _health = type == CharacterType.Hero ? data.Health : data.MaxHealth;
            _maxHealth = data.MaxHealth;
            _attack = data.Attack;
            _defense = data.Defense;
            _speed = data.Speed;
            _evasion = data.Evasion;
            _morale = type == CharacterType.Hero ? data.Morale : data.MaxMorale;
            _maxMorale = data.MaxMorale;
            _immunity = data.Immunity; // Initialize immunity
            _position = position;
            _abilityId = data.AbilityIds.Count > 0 ? data.AbilityIds[0] : "BasicAttack";
            _isCultist = type == CharacterType.Hero && data.CanBeCultist;
            _type = type;
            _partyPosition = data.PartyPosition;
            _hasRetreated = false;
        }

        public string Id => _id;
        public int Speed { get => _speed; set => _speed = Mathf.Clamp(value, 1, 8); }
        public int Health { get => _health; set => _health = Mathf.Max(0, value); }
        public int MaxHealth { get => _maxHealth; set => _maxHealth = value; }
        public int Attack { get => _attack; set => _attack = value; }
        public int Defense { get => _defense; set => _defense = value; }
        public int Evasion { get => _evasion; set => _evasion = Mathf.Clamp(value, 0, 100); }
        public int Morale { get => _morale; set => _morale = Mathf.Clamp(value, 0, _maxMorale); }
        public int MaxMorale { get => _maxMorale; set => _maxMorale = value; }
        public int Immunity { get => _immunity; set => _immunity = Mathf.Clamp(value, 0, 100); } // New immunity property
        public Vector3 Position => _position;
        public string AbilityId { get => _abilityId; set => _abilityId = value; }
        public int PartyPosition => _partyPosition;
        public bool IsCultist { get => _type == CharacterType.Hero && _isCultist; set => _isCultist = _type == CharacterType.Hero && value; }
        public bool IsHero => _type == CharacterType.Hero;
        public CharacterType Type => _type;
        public bool HasRetreated { get => _hasRetreated; set => _hasRetreated = value; }

        public DisplayStats GetDisplayStats()
        {
            return new DisplayStats(
                name: _id,
                health: Health,
                maxHealth: MaxHealth,
                attack: Attack,
                defense: Defense,
                speed: Speed,
                evasion: Evasion,
                morale: Morale,
                maxMorale: MaxMorale,
                immunity: Immunity, // Add immunity to display stats
                isHero: _type == CharacterType.Hero
            );
        }

        public CharacterLibrary.CharacterData SerializeToData()
        {
            return new CharacterLibrary.CharacterData
            {
                Id = _id,
                Health = _type == CharacterType.Hero ? _health : _maxHealth,
                MaxHealth = _maxHealth,
                Attack = _attack,
                Defense = _defense,
                Speed = _speed,
                Evasion = _evasion,
                Morale = _type == CharacterType.Hero ? _morale : _maxMorale,
                MaxMorale = _maxMorale,
                Immunity = _immunity, // Add immunity to serialized data
                AbilityIds = new List<string> { _abilityId },
                CanBeCultist = _isCultist,
                PartyPosition = _partyPosition
            };
        }

        public static CharacterStats DeserializeFromData(CharacterLibrary.CharacterData data, Vector3 position, CharacterType type)
        {
            var stats = new CharacterStats(data.Id, position, type);
            stats.Health = type == CharacterType.Hero ? data.Health : data.MaxHealth;
            stats.Morale = type == CharacterType.Hero ? data.Morale : data.MaxMorale;
            stats.Attack = data.Attack;
            stats.Defense = data.Defense;
            stats.Speed = data.Speed;
            stats.Evasion = data.Evasion;
            stats.MaxHealth = data.MaxHealth;
            stats.MaxMorale = data.MaxMorale;
            stats._immunity = data.Immunity; // Add immunity deserialization
            stats._abilityId = data.AbilityIds.Count > 0 ? data.AbilityIds[0] : "BasicAttack";
            stats._isCultist = type == CharacterType.Hero && data.CanBeCultist;
            stats._partyPosition = data.PartyPosition;
            stats._hasRetreated = false; // Reset on deserialize
            return stats;
        }
    }
}