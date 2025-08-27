using System;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public class MonsterStats : ICombatUnit
    {
        [SerializeField] private MonsterSO _monsterSO;
        [SerializeField] private int _health;
        [SerializeField] private int _maxHealth;
        [SerializeField] private int _attack;
        [SerializeField] private int _defense;
        [SerializeField] private int _speed;
        [SerializeField] private int _evasion;
        [SerializeField] private Vector3 _position;
        [SerializeField] private string _abilityId;

        public MonsterStats(MonsterSO monsterSO, Vector3 position)
        {
            if (monsterSO == null || monsterSO.Stats == null || monsterSO.CharacterType == null || string.IsNullOrEmpty(monsterSO.CharacterType.Id))
            {
                Debug.LogWarning($"MonsterStats: Cannot initialize for {monsterSO?.CharacterType?.Id ?? "unknown"} - monsterSO or stats invalid");
                return;
            }
            _monsterSO = monsterSO;
            _position = position;
            var stats = monsterSO.Stats;
            _health = stats.Health;
            _maxHealth = stats.MaxHealth;
            _attack = stats.Attack;
            _defense = stats.Defense;
            _speed = stats.Speed;
            _evasion = stats.Evasion;
            _abilityId = monsterSO.AbilityIds.Count > 0 ? monsterSO.AbilityIds[0] : "BasicAttack";
        }

        public ScriptableObject SO => _monsterSO;
        public CharacterTypeSO Type => _monsterSO?.Stats?.Type;
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
                name: Type?.Id ?? "Unknown",
                health: Health,
                maxHealth: MaxHealth,
                attack: Attack,
                defense: Defense,
                speed: Speed,
                evasion: Evasion,
                morale: null, // Monsters don't use Morale
                maxMorale: null,
                isHero: false
            );
        }
    }
}