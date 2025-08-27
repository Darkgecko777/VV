using System;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public class HeroStats : ICombatUnit
    {
        [SerializeField] private string _heroId;
        [SerializeField] private int _health;
        [SerializeField] private int _maxHealth;
        [SerializeField] private int _attack;
        [SerializeField] private int _defense;
        [SerializeField] private int _speed;
        [SerializeField] private int _evasion;
        [SerializeField] private int _morale;
        [SerializeField] private int _maxMorale;
        [SerializeField] private bool _isCultist;
        [SerializeField] private Vector3 _position;
        [SerializeField] private string _abilityId;

        public HeroStats(string heroId, Vector3 position)
        {
            var data = CharacterLibrary.GetHeroData(heroId);
            _heroId = data.Id;
            _health = data.Health;
            _maxHealth = data.MaxHealth;
            _attack = data.Attack;
            _defense = data.Defense;
            _speed = data.Speed;
            _evasion = data.Evasion;
            _morale = data.Morale;
            _maxMorale = data.MaxMorale;
            _isCultist = data.CanBeCultist;
            _position = position;
            _abilityId = data.AbilityIds.Count > 0 ? data.AbilityIds[0] : "BasicAttack";
        }

        public ScriptableObject SO => null; // No SO used
        public CharacterTypeSO Type => null; // Replace with string ID
        public int Speed { get => _speed; set => _speed = Mathf.Clamp(value, 1, 8); }
        public int Health { get => _health; set => _health = Mathf.Max(0, value); }
        public int MaxHealth { get => _maxHealth; set => _maxHealth = value; }
        public int Attack { get => _attack; set => _attack = value; }
        public int Defense { get => _defense; set => _defense = value; }
        public int Evasion { get => _evasion; set => _evasion = Mathf.Clamp(value, 0, 100); }
        public int Morale { get => _morale; set => _morale = Mathf.Clamp(value, 0, _maxMorale); }
        public int MaxMorale { get => _maxMorale; set => _maxMorale = value; }
        public Vector3 Position => _position;
        public string AbilityId { get => _abilityId; set => _abilityId = value; }
        public int PartyPosition => CharacterLibrary.GetHeroData(_heroId).PartyPosition;
        public bool IsCultist { get => _isCultist; set => _isCultist = value; }

        public DisplayStats GetDisplayStats()
        {
            return new DisplayStats(
                name: _heroId,
                health: Health,
                maxHealth: MaxHealth,
                attack: Attack,
                defense: Defense,
                speed: Speed,
                evasion: Evasion,
                morale: Morale,
                maxMorale: MaxMorale,
                isHero: true
            );
        }
    }
}