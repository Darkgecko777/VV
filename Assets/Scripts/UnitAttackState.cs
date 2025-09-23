using System.Collections.Generic;

namespace VirulentVentures
{
    public class UnitAttackState
    {
        public ICombatUnit Unit { get; set; }
        public int AttacksThisRound { get; set; }
        public int RoundCounter { get; set; }
        public Dictionary<string, int> AbilityCooldowns { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> RoundCooldowns { get; set; } = new Dictionary<string, int>();
        public bool SkipNextAttack { get; set; } = false;
        public Dictionary<string, (int value, int duration)> TempStats { get; set; } = new Dictionary<string, (int value, int duration)>();
    }
}