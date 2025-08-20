using System;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public class HeroStats : ICombatUnit
    {
        [SerializeField] private HeroSO _heroSO;
        [SerializeField] private int _health;
        [SerializeField] private int _maxHealth;
        [SerializeField] private int _minHealth;
        [SerializeField] private int _attack;
        [SerializeField] private int _minAttack;
        [SerializeField] private int _maxAttack;
        [SerializeField] private int _defense;
        [SerializeField] private int _minDefense;
        [SerializeField] private int _maxDefense;
        [SerializeField] private int _morale;
        [SerializeField] private int _sanity;
        [SerializeField] private int _rank;
        [SerializeField] private bool _isInfected;
        [SerializeField] private int _slowTickDelay;
        [SerializeField] private bool _isCultist;
        [SerializeField] private Vector3 _position;

        public HeroStats(HeroSO heroSO, Vector3 position)
        {
            _heroSO = heroSO;
            _position = position;
            var stats = heroSO.Stats;
            _health = stats.Health;
            _maxHealth = stats.MaxHealth;
            _minHealth = stats.MinHealth;
            _attack = stats.Attack;
            _minAttack = stats.MinAttack;
            _maxAttack = stats.MaxAttack;
            _defense = stats.Defense;
            _minDefense = stats.MinDefense;
            _maxDefense = stats.MaxDefense;
            _morale = stats.Morale;
            _sanity = stats.Sanity;
            _rank = stats.Rank;
            _isInfected = stats.IsInfected;
            _slowTickDelay = stats.SlowTickDelay;
            _isCultist = stats.IsCultist;
        }

        public ScriptableObject SO => _heroSO;
        public CharacterTypeSO Type => _heroSO.Stats.Type;
        public CharacterStatsData.Speed CharacterSpeed => _heroSO.Stats.CharacterSpeed;
        public int Health { get => _health; set => _health = value; }
        public int MaxHealth { get => _maxHealth; set => _maxHealth = value; }
        public int MinHealth { get => _minHealth; set => _minHealth = value; }
        public int Attack { get => _attack; set => _attack = value; }
        public int MinAttack { get => _minAttack; set => _minAttack = value; }
        public int MaxAttack { get => _maxAttack; set => _maxAttack = value; }
        public int Defense { get => _defense; set => _defense = value; }
        public int MinDefense { get => _minDefense; set => _minDefense = value; }
        public int MaxDefense { get => _maxDefense; set => _maxDefense = value; }
        public int Morale { get => _morale; set => _morale = value; }
        public int Sanity { get => _sanity; set => _sanity = value; }
        public int Rank { get => _rank; set => _rank = value; }
        public bool IsInfected { get => _isInfected; set => _isInfected = value; }
        public int SlowTickDelay { get => _slowTickDelay; set => _slowTickDelay = value; }
        public bool IsCultist { get => _isCultist; set => _isCultist = value; }
        public Vector3 Position => _position;
    }
}