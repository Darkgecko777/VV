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

        public abstract bool Execute(CharacterStats user, List<ICombatUnit> targets, AbilitySO ability, string abilityId, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, Action<ICombatUnit> updateUnitCallback, UnitAttackState attackState, CombatSceneComponent combatScene);
    }
}