using UnityEngine;

namespace VirulentVentures
{
    public interface ICombatUnit
    {
        ScriptableObject SO { get; }
        CharacterTypeSO Type { get; }
        CharacterStatsData.Speed CharacterSpeed { get; } // Explicitly uses CharacterSpeed
        int Health { get; set; }
        int MaxHealth { get; }
        int MinHealth { get; }
        int Attack { get; }
        int MinAttack { get; }
        int MaxAttack { get; }
        int Defense { get; }
        int MinDefense { get; }
        int MaxDefense { get; }
        int Morale { get; set; }
        int Sanity { get; set; }
        int Rank { get; }
        bool IsInfected { get; set; }
        int SlowTickDelay { get; set; }
        bool IsCultist { get; }
        Vector3 Position { get; }
    }
}