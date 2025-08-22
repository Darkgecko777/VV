using UnityEngine;

namespace VirulentVentures
{
    public interface ICombatUnit
    {
        ScriptableObject SO { get; }
        CharacterTypeSO Type { get; }
        CharacterStatsData.Speed CharacterSpeed { get; }
        int Health { get; set; }
        int MaxHealth { get; set; }
        int MinHealth { get; set; }
        int Attack { get; set; }
        int MinAttack { get; set; }
        int MaxAttack { get; set; }
        int Defense { get; set; }
        int MinDefense { get; set; }
        int MaxDefense { get; set; }
        int Morale { get; set; }
        int Sanity { get; set; }
        int Rank { get; }
        bool IsInfected { get; set; }
        int SlowTickDelay { get; set; }
        bool IsCultist { get; }
        Vector3 Position { get; }
        string AbilityId { get; set; }
        int PartyPosition { get; }
    }
}