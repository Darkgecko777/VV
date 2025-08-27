using System;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public class MonsterStats : ICombatUnit
    {
        [SerializeField] private string _monsterId;
        [SerializeField] private int _health;
        [SerializeField] private int _maxHealth;
        [SerializeField] private int _attack;
        [SerializeField] private int _defense;
        [SerializeField] private int _speed;
        [SerializeField] private int _evasion;
        [SerializeField] private Vector3 _position;
        [SerializeField] private string _abilityId;

        public MonsterStats(string monsterId, Vector3 position)
        {
            var data = CharacterLibrary.GetMonsterData(monsterId);
            _monsterId = data.Id;
            _health = data.Health;
            _maxHealth = data.MaxHealth;
            _attack = data.Attack;
            _defense = data.Defense;
            _speed = data.Speed;
            _evasion = data.Evasion;
            _position = position;
            _abilityId = data.AbilityIds.Count > 0 ? data.AbilityIds[0] : "BasicAttack";
        }

        public string Id => _monsterId;

        public int Speed { get => _speed; set => _speed = Mathf.Clamp(value, 1, 8); }
        public int Health { get => _health; set => _health = Mathf.Max(0, value); }
        public int MaxHealth { get => _maxHealth; set => _maxHealth = value; }
        public int Attack { get => _attack; set => _attack = value; }
        public int Defense { get => _defense; set => _defense = value; }
        public int Evasion { get => _evasion; set => _evasion = Mathf.Clamp(value, 0, 100); }
        public int Morale { get => 0; set { } } // Monsters don't use Morale
        public int MaxMorale { get => 0; set { } } // Monsters don't use MaxMorale
        public Vector3 Position => _position;
        public string AbilityId { get => _abilityId; set => _abilityId = value; }
        public int PartyPosition => 0; // Monsters don't use party position

        public DisplayStats GetDisplayStats()
        {
            return new DisplayStats(
                name: _monsterId,
                health: Health,
                maxHealth: MaxHealth,
                attack: Attack,
                defense: Defense,
                speed: Speed,
                evasion: Evasion,
                morale: null,
                maxMorale: null,
                isHero: false
            );
        }
    }
}