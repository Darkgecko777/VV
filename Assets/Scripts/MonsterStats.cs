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
        [SerializeField] private int _slowTickDelay;
        [SerializeField] private Vector3 _position;
        [SerializeField] private string _abilityId;

        public MonsterStats(MonsterSO monsterSO, Vector3 position)
        {
            _monsterSO = monsterSO;
            _position = position;
            var stats = monsterSO.Stats;
            _health = stats.Health;
            _maxHealth = stats.MaxHealth;
            _attack = stats.Attack;
            _defense = stats.Defense;
            _slowTickDelay = stats.SlowTickDelay;
            _abilityId = monsterSO.AbilityIds.Count > 0 ? monsterSO.AbilityIds[0] : "BasicAttack";
        }

        public ScriptableObject SO => _monsterSO;
        public CharacterTypeSO Type => _monsterSO.Stats.Type;
        public CharacterStatsData.Speed CharacterSpeed => _monsterSO.Stats.CharacterSpeed;
        public int Health { get => _health; set => _health = Mathf.Max(0, value); }
        public int MaxHealth { get => _maxHealth; set => _maxHealth = value; }
        public int Attack { get => _attack; set => _attack = value; }
        public int Defense { get => _defense; set => _defense = value; }
        public int SlowTickDelay { get => _slowTickDelay; set => _slowTickDelay = value; }
        public Vector3 Position => _position;
        public string AbilityId { get => _abilityId; set => _abilityId = value; }
        public int PartyPosition => 0; // Monsters don't use party position
    }
}