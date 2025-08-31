using UnityEngine;

namespace VirulentVentures
{
    public interface ICombatUnit
    {
        string Id { get; }
        int Speed { get; set; }
        int Health { get; set; }
        int MaxHealth { get; set; }
        int Attack { get; set; }
        int Defense { get; set; }
        int Evasion { get; set; }
        int Morale { get; set; }
        int MaxMorale { get; set; }
        Vector3 Position { get; }
        string AbilityId { get; set; }
        int PartyPosition { get; }
        bool HasRetreated { get; set; }
        CharacterStats.DisplayStats GetDisplayStats();
    }
}