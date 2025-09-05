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
            public string virusID;
            public int nodeIndex;
        }

        [System.Serializable]
        public struct AttackData
        {
            public ICombatUnit attacker;
            public ICombatUnit target;
        }

        [System.Serializable]
        public struct NodeUpdateData
        {
            public List<VirulentVentures.NodeData> nodes;
            public int currentIndex;
        }

        public event Action<LogData> OnLogMessage;
        public event Action<UnitUpdateData> OnUnitUpdated;
        public event Action<DamagePopupData> OnDamagePopup;
        public event Action OnCombatEnded;
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
        public event Action OnHealParty; // New event for healing

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

        public void RaiseCombatEnded()
        {
            OnCombatEnded?.Invoke();
        }

        public void RaiseRetreatTriggered()
        {
            OnRetreatTriggered?.Invoke();
        }

        public void RaiseExpeditionGenerated(ExpeditionData expeditionData, PartyData partyData)
        {
            OnExpeditionGenerated?.Invoke(new ExpeditionGeneratedData { expeditionData = expeditionData, partyData = partyData });
        }

        public void RaiseVirusSeeded(string virusID, int nodeIndex)
        {
            OnVirusSeeded?.Invoke(new VirusSeededData { virusID = virusID, nodeIndex = nodeIndex });
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

        public void RaiseUnitAttacking(ICombatUnit attacker, ICombatUnit target)
        {
            OnUnitAttacking?.Invoke(new AttackData { attacker = attacker, target = target });
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

        public void RaiseHealParty()
        {
            OnHealParty?.Invoke();
        }
    }
}