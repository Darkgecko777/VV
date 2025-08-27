using UnityEngine;

namespace VirulentVentures
{
    public interface ICombatUnit
    {
        ScriptableObject SO { get; }
        CharacterTypeSO Type { get; }
        int Speed { get; set; } // Int for 1-8 range
        int Health { get; set; }
        int MaxHealth { get; set; }
        int Attack { get; set; }
        int Defense { get; set; }
        int Evasion { get; set; } // 0-100% dodge chance
        int Morale { get; set; } // 0-100% for retreat (heroes; monsters return 0)
        int MaxMorale { get; set; } // Cap for Morale
        Vector3 Position { get; }
        string AbilityId { get; set; }
        int PartyPosition { get; }
        DisplayStats GetDisplayStats();
    }
}