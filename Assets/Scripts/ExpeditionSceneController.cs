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
            uiController.OnAdvanceClicked += HandleAdvanceClicked;
            ExpeditionManager.Instance.OnExpeditionGenerated += UpdateScene;
            ExpeditionManager.Instance.OnCombatStarted += () => { /* Optional: Handle combat start visuals/logic */ };

            // Initialize UI with current expedition data
            UpdateScene();
        }

        void OnDestroy()
        {
            if (uiController != null)
            {
                uiController.OnContinueClicked -= HandleContinueClicked;
                uiController.OnAdvanceClicked -= HandleAdvanceClicked;
            }
            if (ExpeditionManager.Instance != null)
            {
                ExpeditionManager.Instance.OnExpeditionGenerated -= UpdateScene;
                ExpeditionManager.Instance.OnCombatStarted -= null;
            }
        }

        private void HandleContinueClicked()
        {
            Debug.Log("ExpeditionSceneController: Continue button clicked, triggering ExpeditionManager.OnContinueClicked");
            ExpeditionManager.Instance.OnContinueClicked();
        }

        private void HandleAdvanceClicked()
        {
            Debug.Log("ExpeditionSceneController: AdvanceNode button clicked");
            if (expeditionData != null && expeditionData.CurrentNodeIndex < expeditionData.NodeData.Count - 1)
            {
                ExpeditionManager.Instance.ProcessCurrentNode();
            }
            else
            {
                Debug.LogWarning("ExpeditionSceneController: Cannot advance, invalid node index or expedition data!");
            }
        }

        private void UpdateScene()
        {
            Debug.Log("ExpeditionSceneController: Updating scene with current expedition data");
            uiController.UpdateUI();
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