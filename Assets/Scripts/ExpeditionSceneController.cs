using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class ExpeditionSceneController : MonoBehaviour
    {
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private ExpeditionData expeditionData;

        void Awake()
        {
            if (!ValidateReferences()) return;
        }

        void Start()
        {
            eventBus.OnNodeUpdated += HandleNodeUpdate;
            eventBus.OnSceneTransitionCompleted += HandleNodeUpdate;
            eventBus.OnContinueClicked += HandleContinueClicked; // Handle continue button

            var expeditionData = ExpeditionManager.Instance.GetExpedition();
            HandleNodeUpdate(new EventBusSO.NodeUpdateData { nodes = expeditionData.NodeData, currentIndex = expeditionData.CurrentNodeIndex });
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.OnNodeUpdated -= HandleNodeUpdate;
                eventBus.OnSceneTransitionCompleted -= HandleNodeUpdate;
                eventBus.OnContinueClicked -= HandleContinueClicked;
                Debug.Log("ExpeditionSceneController: Unsubscribed from EventBusSO");
            }
        }

        private void HandleNodeUpdate(EventBusSO.NodeUpdateData data)
        {
            // Removed RaiseNodeUpdated to prevent recursive loop
            Debug.Log($"ExpeditionSceneController: Handled node update for index {data.currentIndex}, nodes count: {data.nodes?.Count ?? 0}");
        }

        private void HandleContinueClicked()
        {
            ExpeditionManager.Instance.OnContinueClicked();
            Debug.Log("ExpeditionSceneController: Handled continue click");
        }

        private bool ValidateReferences()
        {
            if (eventBus == null || expeditionData == null)
            {
                Debug.LogError($"ExpeditionSceneController: Missing references! EventBus: {eventBus != null}, ExpeditionData: {expeditionData != null}");
                return false;
            }
            return true;
        }
    }
}