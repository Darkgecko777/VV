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
        [SerializeField] private int _attack;
        [SerializeField] private int _defense;
        [SerializeField] private int _speed;
        [SerializeField] private int _evasion;
        [SerializeField] private int _morale;
        [SerializeField] private int _maxMorale;
        [SerializeField] private bool _isCultist;
        [SerializeField] private Vector3 _position;
        [SerializeField] private string _abilityId;

        public HeroStats(HeroSO heroSO, Vector3 position)
        {
            if (heroSO == null || heroSO.Stats == null || heroSO.CharacterType == null || string.IsNullOrEmpty(heroSO.CharacterType.Id))
            {
                Debug.LogWarning($"HeroStats: Cannot initialize for {heroSO?.CharacterType?.Id ?? "unknown"} - heroSO or stats invalid");
                return;
            }
            _heroSO = heroSO;
            _position = position;
            var stats = heroSO.Stats;
            _health = stats.Health;
            _maxHealth = stats.MaxHealth;
            _attack = stats.Attack;
            _defense = stats.Defense;
            _speed = stats.Speed;
            _evasion = stats.Evasion;
            _morale = stats.Morale;
            _maxMorale = stats.MaxMorale;
            _isCultist = false; // Default, set via meta-progression
            _abilityId = heroSO.AbilityIds.Count > 0 ? heroSO.AbilityIds[0] : "BasicAttack";
        }

        public ScriptableObject SO => _heroSO;
        public CharacterTypeSO Type => _heroSO?.Stats?.Type;
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
        public int PartyPosition => _heroSO?.PartyPosition ?? 1;
        public bool IsCultist { get => _isCultist; set => _isCultist = value; }

        public DisplayStats GetDisplayStats()
        {
            return new DisplayStats(
                name: Type?.Id ?? "Unknown",
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