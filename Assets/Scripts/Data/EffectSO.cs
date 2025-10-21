using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "EffectSO", menuName = "VirulentVentures/EffectSO")]
    public abstract class EffectSO : ScriptableObject
    {
        [SerializeField] private string effectId;
        public string EffectId => effectId;

        // Return (changed vector, delta) or (null, 0) if no stat change
        public abstract (TransmissionVector? changedVector, float delta) Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene);
    }
}