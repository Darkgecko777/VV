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
            public DisplayStats displayStats;
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
            public List<(ICombatUnit unit, GameObject go, DisplayStats stats)> units;
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

        public void RaiseLogMessage(string message, Color color)
        {
            OnLogMessage?.Invoke(new LogData { message = message, color = color });
        }

        public void RaiseUnitUpdated(ICombatUnit unit, DisplayStats displayStats)
        {
            OnUnitUpdated?.Invoke(new UnitUpdateData { unit = unit, displayStats = displayStats });
        }

        public void RaiseDamagePopup(ICombatUnit unit, string message)
        {
            OnDamagePopup?.Invoke(new DamagePopupData { unit = unit, message = message });
        }

        public void RaiseBattleEnded()
        {
            OnBattleEnded?.Invoke();
        }

        public void RaiseRetreatTriggered()
        {
            OnRetreatTriggered?.Invoke();
        }

        public void RaiseBattleInitialized(List<(ICombatUnit unit, GameObject go, DisplayStats stats)> units)
        {
            OnBattleInitialized?.Invoke(new BattleInitData { units = units });
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
    }
}