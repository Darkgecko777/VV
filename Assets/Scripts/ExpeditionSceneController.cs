using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class ExpeditionSceneController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private ExpeditionUIController uiController;

        void Awake()
        {
            if (!ValidateReferences()) return;
        }

        void Start()
        {
            // Subscribe to UIController events
            uiController.OnContinueClicked += HandleContinueClicked;
            ExpeditionManager.Instance.OnExpeditionGenerated += UpdateScene;
            ExpeditionManager.Instance.OnCombatStarted += () => { /* Optional: Handle combat start visuals/logic */ };

            // NEW: Subscribe to node updates and scene transitions for in-scene UI refreshes
            ExpeditionManager.Instance.OnNodeUpdated += HandleNodeUpdate;
            ExpeditionManager.Instance.OnSceneTransitionCompleted += HandleNodeUpdate;

            // Initialize UI with current expedition data
            UpdateScene();
        }

        void OnDestroy()
        {
            if (uiController != null)
            {
                uiController.OnContinueClicked -= HandleContinueClicked;
            }
            if (ExpeditionManager.Instance != null)
            {
                ExpeditionManager.Instance.OnExpeditionGenerated -= UpdateScene;
                ExpeditionManager.Instance.OnCombatStarted -= null;

                // NEW: Unsubscribe from node updates and scene transitions
                ExpeditionManager.Instance.OnNodeUpdated -= HandleNodeUpdate;
                ExpeditionManager.Instance.OnSceneTransitionCompleted -= HandleNodeUpdate;
            }
        }

        private void HandleContinueClicked()
        {
            ExpeditionManager.Instance.OnContinueClicked();
        }

        private void UpdateScene()
        {
            uiController.UpdateUI();
        }

        // NEW: Handler for node changes (advances or post-transition)
        private void HandleNodeUpdate(System.Collections.Generic.List<NodeData> nodes, int currentIndex)
        {
            UpdateScene();
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || expeditionData == null || visualConfig == null || uiConfig == null || uiController == null)
            {
                Debug.LogError($"ExpeditionSceneController: Missing references! UIDocument: {uiDocument != null}, ExpeditionData: {expeditionData != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, UIController: {uiController != null}");
                return false;
            }
            return true;
        }
    }
}