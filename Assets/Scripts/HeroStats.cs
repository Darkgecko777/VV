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
        [SerializeField] private int _morale;
        [SerializeField] private int _sanity;
        [SerializeField] private int _slowTickDelay;
        [SerializeField] private bool _isCultist;
        [SerializeField] private Vector3 _position;
        [SerializeField] private string _abilityId;

        public HeroStats(HeroSO heroSO, Vector3 position)
        {
            if (heroSO == null || heroSO.Stats == null || heroSO.CharacterType == null || string.IsNullOrEmpty(heroSO.CharacterType.Id))
            {
                return;
            }
            _heroSO = heroSO;
            _position = position;
            var stats = heroSO.Stats;
            _health = stats.Health;
            _maxHealth = stats.MaxHealth;
            _attack = stats.Attack;
            _defense = stats.Defense;
            _slowTickDelay = stats.SlowTickDelay;
            _morale = 100; // Placeholder default
            _sanity = 100; // Placeholder default
            _isCultist = false; // Default, set via meta-progression
            _abilityId = heroSO.AbilityIds.Count > 0 ? heroSO.AbilityIds[0] : "BasicAttack";
        }

        public ScriptableObject SO => _heroSO;
        public CharacterTypeSO Type => _heroSO?.Stats?.Type;
        public CharacterStatsData.Speed CharacterSpeed => _heroSO?.Stats.CharacterSpeed ?? CharacterStatsData.Speed.Normal;
        public int Health { get => _health; set => _health = Mathf.Max(0, value); }
        public int MaxHealth { get => _maxHealth; set => _maxHealth = value; }
        public int Attack { get => _attack; set => _attack = value; }
        public int Defense { get => _defense; set => _defense = value; }
        public int SlowTickDelay { get => _slowTickDelay; set => _slowTickDelay = value; }
        public Vector3 Position => _position;
        public string AbilityId { get => _abilityId; set => _abilityId = value; }
        public int PartyPosition => _heroSO?.PartyPosition ?? 1;
        public int Morale { get => _morale; set => _morale = value; }
        public int Sanity { get => _sanity; set => _sanity = value; }
        public bool IsCultist { get => _isCultist; set => _isCultist = value; }
    }
}