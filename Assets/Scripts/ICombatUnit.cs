using UnityEngine;

namespace VirulentVentures
{
    public interface ICombatUnit
    {
        string Id { get; }
        int Health { get; set; }
        int MaxHealth { get; }
        int Attack { get; set; }
        int Defense { get; set; }
        int Speed { get; set; }
        int Evasion { get; set; }
        int Morale { get; set; }
        int MaxMorale { get; }
        int Immunity { get; }
        bool HasRetreated { get; set; }
        bool IsHero { get; }
        int PartyPosition { get; }
        CharacterStats.DisplayStats GetDisplayStats();
    }
}