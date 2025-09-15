using UnityEngine;

namespace VirulentVentures
{
    [System.Serializable]
    public enum AbilityTag
    {
        TargetEnemies,
        TargetAllies,
        TargetSelf,
        Melee,
        Ranged,
        Damage,
        Heal,
        Buff,
        Debuff,
        Infection,
        Morale,
        StandardDefense,
        IgnoreDefense,
        PartialIgnoreDefense,
        NoDefenseCheck,
        Dodgeable,
        Undodgeable,
        Cooldown,
        NoEvasionCheck,
        AOE,
        FixedDamage,
        SelfDamage,
        SkipNextAttack,
        ThornsFixed,
        ThornsInfection,
        PriorityLowHealth,
        Common
    }
}