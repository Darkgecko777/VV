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
        int Attack { get; set; }
        int Defense { get; set; }
        int SlowTickDelay { get; set; }
        Vector3 Position { get; }
        string AbilityId { get; set; }
        int PartyPosition { get; }
    }
}