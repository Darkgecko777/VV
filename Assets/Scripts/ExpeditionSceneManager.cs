using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class ExpeditionSceneManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ExpeditionData expeditionData;

        private VisualElement nodeContainer;
        private Button continueButton;

        void Awake()
        {
            if (!ValidateReferences()) return;
            nodeContainer = uiDocument.rootVisualElement.Q<VisualElement>("NodeContainer");
            continueButton = uiDocument.rootVisualElement.Q<Button>("ContinueButton");
            continueButton.clicked += () => ExpeditionManager.Instance.OnContinueClicked();
        }

        void Start()
        {
            UpdateNodeVisuals();
            ExpeditionManager.Instance.OnNodeUpdated += UpdateNodeVisuals;
            ExpeditionManager.Instance.OnSceneTransitionCompleted += UpdateNodeVisuals;
        }

        void OnDestroy()
        {
            ExpeditionManager.Instance.OnNodeUpdated -= UpdateNodeVisuals;
            ExpeditionManager.Instance.OnSceneTransitionCompleted -= UpdateNodeVisuals;
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || expeditionData == null)
            {
                Debug.LogError($"ExpeditionSceneManager: Missing references! UIDocument: {uiDocument != null}, ExpeditionData: {expeditionData != null}");
                return false;
            }
            return true;
        }

        private void UpdateNodeVisuals()
        {
            // Placeholder: Update node visuals (similar to TempleVisualController.UpdateNodeVisuals)
            Debug.Log($"ExpeditionSceneManager: Updating visuals for node {expeditionData.CurrentNodeIndex}, Total nodes: {expeditionData.NodeData.Count}");
        }
    }
}