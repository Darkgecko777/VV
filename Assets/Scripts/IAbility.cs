using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public interface IAbility
    {
        string Id { get; }
        int Priority { get; }
        int Cooldown { get; }
        CombatTypes.CooldownType CooldownType { get; }
        int Rank { get; }
        string AnimationTrigger { get; }
        string LogTemplate { get; }
        List<CombatTypes.AbilityCondition> Conditions { get; }

        CombatTypes.TargetingRule GetTargetingRule();
        List<(string tag, int value, int duration)> GetEffects();
        void Execute(CharacterStats user, PartyData party, List<ICombatUnit> targets, CombatSceneComponent context);
    }
}