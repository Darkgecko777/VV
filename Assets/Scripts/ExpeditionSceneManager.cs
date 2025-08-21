using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class ExpeditionSceneManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private VisualConfig visualConfig;

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
            ExpeditionManager.Instance.OnNodeUpdated += UpdateNodeVisuals;
            ExpeditionManager.Instance.OnSceneTransitionCompleted += UpdateNodeVisuals;
            UpdateNodeVisuals(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
        }

        void OnDestroy()
        {
            ExpeditionManager.Instance.OnNodeUpdated -= UpdateNodeVisuals;
            ExpeditionManager.Instance.OnSceneTransitionCompleted -= UpdateNodeVisuals;
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || expeditionData == null || visualConfig == null)
            {
                Debug.LogError($"ExpeditionSceneManager: Missing references! UIDocument: {uiDocument != null}, ExpeditionData: {expeditionData != null}, VisualConfig: {visualConfig != null}");
                return false;
            }
            return true;
        }

        private void UpdateNodeVisuals(List<NodeData> nodes, int currentNodeIndex)
        {
            if (nodeContainer == null)
            {
                Debug.LogWarning("ExpeditionSceneManager.UpdateNodeVisuals: nodeContainer is null!");
                return;
            }

            nodeContainer.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                VisualElement nodeBox = new VisualElement();
                nodeBox.AddToClassList("node-box");
                nodeBox.AddToClassList(nodes[i].IsCombat ? "node-combat" : "node-noncombat");
                if (i == currentNodeIndex)
                {
                    nodeBox.AddToClassList("node-active");
                }
                Color nodeColor = visualConfig.GetNodeColor(nodes[i].NodeType);
                nodeBox.style.backgroundColor = new StyleColor(nodeColor);

                Label nodeLabel = new Label($"Node {i + 1}");
                nodeLabel.AddToClassList("node-label");
                nodeBox.Add(nodeLabel);

                if (nodes[i].SeededViruses.Count > 0)
                {
                    nodeBox.tooltip = $"Seeded: {string.Join(", ", nodes[i].SeededViruses.ConvertAll(v => v.VirusID))}";
                }

                nodeContainer.Add(nodeBox);
            }
        }
    }
}