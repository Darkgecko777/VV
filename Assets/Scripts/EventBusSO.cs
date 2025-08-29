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
        public struct BattleInitData
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
        public struct NodeUpdateData
        {
            public List<NodeData> nodes;
            public int currentIndex;
        }

        public event Action<LogData> OnLogMessage;
        public event Action<UnitUpdateData> OnUnitUpdated;
        public event Action<DamagePopupData> OnDamagePopup;
        public event Action OnBattleEnded;
        public event Action OnRetreatTriggered;
        public event Action<BattleInitData> OnBattleInitialized;
        public event Action<ExpeditionGeneratedData> OnExpeditionGenerated;
        public event Action<VirusSeededData> OnVirusSeeded;
        public event Action<PartyData> OnPartyUpdated;
        public event Action OnLaunchExpedition;
        public event Action<ExpeditionGeneratedData> OnExpeditionUpdated;
        public event Action<NodeUpdateData> OnNodeUpdated;
        public event Action<NodeUpdateData> OnSceneTransitionCompleted;
        public event Action OnContinueClicked;

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

        public void RaiseBattleInitialized(List<(ICombatUnit unit, GameObject go, CharacterStats.DisplayStats stats)> units)
        {
            OnBattleInitialized?.Invoke(new BattleInitData { units = units });
        }

        public void RaiseBattleEnded()
        {
            OnBattleEnded?.Invoke();
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
    }
}