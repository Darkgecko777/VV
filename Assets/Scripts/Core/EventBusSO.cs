using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "EventBus", menuName = "VirulentVentures/EventBus", order = 1)]
    public class EventBusSO : ScriptableObject
    {
        [System.Serializable]
        public struct LogData
        {
            public string message;
            public Color color;
        }

        [System.Serializable]
        public struct UnitUpdateData
        {
            public ICombatUnit unit;
            public CharacterStats.DisplayStats displayStats;
        }

        [System.Serializable]
        public struct DamagePopupData
        {
            public ICombatUnit unit;
            public string message;
        }

        [System.Serializable]
        public struct CombatInitData
        {
            public List<(ICombatUnit unit, GameObject go, CharacterStats.DisplayStats stats)> units;
        }

        [System.Serializable]
        public struct ExpeditionGeneratedData
        {
            public ExpeditionData expeditionData;
            public PartyData partyData;
        }

        [System.Serializable]
        public struct VirusSeededData
        {
            public VirusSO virus;
            public CharacterStats unit;
        }

        [System.Serializable]
        public struct AttackData
        {
            public ICombatUnit attacker;
            public ICombatUnit target;
            public string abilityId;
        }

        [System.Serializable]
        public struct InfectionData
        {
            public ICombatUnit unit;
            public string virusId;
        }

        [System.Serializable]
        public struct NodeUpdateData
        {
            public List<NodeData> nodes;
            public int currentIndex;
        }

        [System.Serializable]
        public struct CombatSpeedData
        {
            public float speed;
        }

        // === NEW: NON-COMBAT EVENT STRUCTS ===
        [System.Serializable]
        public struct NonCombatEncounterData
        {
            public NonCombatEncounterSO encounter;
            public NodeData node;
        }

        [System.Serializable]
        public struct NonCombatResolveData
        {
            public NonCombatEncounterSO encounter;
            public NodeData node;
        }

        [System.Serializable]
        public struct NonCombatResultData
        {
            public string result;
            public bool success;
        }

        public event Action<LogData> OnLogMessage;
        public event Action<UnitUpdateData> OnUnitUpdated;
        public event Action<DamagePopupData> OnDamagePopup;
        public event Action<bool> OnCombatEnded;
        public event Action OnRetreatTriggered;
        public event Action<CombatInitData> OnCombatInitialized;
        public event Action<ExpeditionGeneratedData> OnExpeditionGenerated;
        public event Action<VirusSeededData> OnVirusSeeded;
        public event Action<PartyData> OnPartyUpdated;
        public event Action OnLaunchExpedition;
        public event Action<ExpeditionGeneratedData> OnExpeditionUpdated;
        public event Action<NodeUpdateData> OnNodeUpdated;
        public event Action<NodeUpdateData> OnSceneTransitionCompleted;
        public event Action OnContinueClicked;
        public event Action<AttackData> OnUnitAttacking;
        public event Action<DamagePopupData> OnUnitDamaged;
        public event Action<ICombatUnit> OnUnitDied;
        public event Action<ICombatUnit> OnUnitRetreated;
        public event Action<InfectionData> OnUnitInfected;
        public event Action OnTempleEnteredFromExpedition;
        public event Action OnPlayerProgressUpdated;
        public event Action OnCombatPaused;
        public event Action OnCombatPlayed;
        public event Action<CombatSpeedData> OnCombatSpeedChanged;
        public event Action<AttackData> OnAbilitySelected;
        public event Action OnCureInfections;
        public event Action<float> OnRequestSetCombatSpeed;

        // === NEW: NON-COMBAT EVENTS ===
        public event Action<NonCombatEncounterData> OnNonCombatEncounter;
        public event Action<NonCombatResolveData> OnNonCombatResolveRequested;
        public event Action<NonCombatResultData> OnNonCombatResolved;

        // === RAISE METHODS ===
        public void RaiseLogMessage(string message, Color color)
        {
            OnLogMessage?.Invoke(new LogData { message = message, color = color });
        }

        public void RaiseUnitUpdated(ICombatUnit unit, CharacterStats.DisplayStats displayStats)
        {
            OnUnitUpdated?.Invoke(new UnitUpdateData { unit = unit, displayStats = displayStats });
        }

        public void RaiseDamagePopup(ICombatUnit unit, string message)
        {
            OnDamagePopup?.Invoke(new DamagePopupData { unit = unit, message = message });
        }

        public void RaiseCombatInitialized(List<(ICombatUnit unit, GameObject go, CharacterStats.DisplayStats stats)> units)
        {
            OnCombatInitialized?.Invoke(new CombatInitData { units = units });
        }

        public void RaiseCombatEnded(bool isVictory)
        {
            OnCombatEnded?.Invoke(isVictory);
        }

        public void RaiseRetreatTriggered()
        {
            OnRetreatTriggered?.Invoke();
        }

        public void RaiseExpeditionGenerated(ExpeditionData expeditionData, PartyData partyData)
        {
            OnExpeditionGenerated?.Invoke(new ExpeditionGeneratedData { expeditionData = expeditionData, partyData = partyData });
        }

        public void RaiseVirusSeeded(VirusSO virus, CharacterStats unit)
        {
            OnVirusSeeded?.Invoke(new VirusSeededData { virus = virus, unit = unit });
        }

        public void RaisePartyUpdated(PartyData partyData)
        {
            OnPartyUpdated?.Invoke(partyData);
        }

        public void RaiseLaunchExpedition()
        {
            OnLaunchExpedition?.Invoke();
        }

        public void RaiseExpeditionUpdated(ExpeditionData expeditionData, PartyData partyData)
        {
            OnExpeditionUpdated?.Invoke(new ExpeditionGeneratedData { expeditionData = expeditionData, partyData = partyData });
        }

        public void RaiseNodeUpdated(List<NodeData> nodes, int currentIndex)
        {
            OnNodeUpdated?.Invoke(new NodeUpdateData { nodes = nodes, currentIndex = currentIndex });
        }

        public void RaiseSceneTransitionCompleted(List<NodeData> nodes, int currentIndex)
        {
            OnSceneTransitionCompleted?.Invoke(new NodeUpdateData { nodes = nodes, currentIndex = currentIndex });
        }

        public void RaiseContinueClicked()
        {
            OnContinueClicked?.Invoke();
        }

        public void RaiseUnitAttacking(ICombatUnit attacker, ICombatUnit target, string abilityId)
        {
            OnUnitAttacking?.Invoke(new AttackData { attacker = attacker, target = target, abilityId = abilityId });
        }

        public void RaiseUnitDamaged(ICombatUnit unit, string message)
        {
            OnUnitDamaged?.Invoke(new DamagePopupData { unit = unit, message = message });
        }

        public void RaiseUnitDied(ICombatUnit unit)
        {
            OnUnitDied?.Invoke(unit);
        }

        public void RaiseUnitRetreated(ICombatUnit unit)
        {
            OnUnitRetreated?.Invoke(unit);
        }

        public void RaiseUnitInfected(ICombatUnit unit, string virusId)
        {
            OnUnitInfected?.Invoke(new InfectionData { unit = unit, virusId = virusId });
        }

        public void RaiseTempleEnteredFromExpedition()
        {
            OnTempleEnteredFromExpedition?.Invoke();
        }

        public void RaisePlayerProgressUpdated()
        {
            OnPlayerProgressUpdated?.Invoke();
        }

        public void RaiseCombatPaused()
        {
            OnCombatPaused?.Invoke();
        }

        public void RaiseCombatPlayed()
        {
            OnCombatPlayed?.Invoke();
        }

        public void RaiseCombatSpeedChanged(float speed)
        {
            OnCombatSpeedChanged?.Invoke(new CombatSpeedData { speed = speed });
        }

        public void RaiseAbilitySelected(AttackData data)
        {
            OnAbilitySelected?.Invoke(data);
        }

        public void RaiseCureInfections()
        {
            OnCureInfections?.Invoke();
        }

        public void RaiseRequestSetCombatSpeed(float speed)
        {
            OnRequestSetCombatSpeed?.Invoke(speed);
        }

        // === NEW: NON-COMBAT RAISE METHODS ===
        public void RaiseNonCombatEncounter(NonCombatEncounterSO encounter, NodeData node)
        {
            OnNonCombatEncounter?.Invoke(new NonCombatEncounterData { encounter = encounter, node = node });
        }

        public void RaiseNonCombatResolveRequested(NonCombatEncounterSO encounter, NodeData node)
        {
            OnNonCombatResolveRequested?.Invoke(new NonCombatResolveData { encounter = encounter, node = node });
        }

        public void RaiseNonCombatResolved(string result, bool success)
        {
            OnNonCombatResolved?.Invoke(new NonCombatResultData { result = result, success = success });
        }
    }
}