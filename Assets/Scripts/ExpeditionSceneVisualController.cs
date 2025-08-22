using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class ExpeditionSceneVisualController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualConfig visualConfig;
        private VisualElement nodeContainer;

        // Initialize visual elements and validate references
        void Awake()
        {
            if (uiDocument == null || visualConfig == null)
            {
                Debug.LogError($"ExpeditionSceneVisualController: Missing references! UIDocument: {uiDocument != null}, VisualConfig: {visualConfig != null}");
                return;
            }
            nodeContainer = uiDocument.rootVisualElement.Q<VisualElement>("NodeContainer");
        }

        // Subscribe to node updates and render initial visuals
        void Start()
        {
            ExpeditionManager.Instance.OnNodeUpdated += UpdateNodeVisuals;
            ExpeditionManager.Instance.OnSceneTransitionCompleted += UpdateNodeVisuals;
            var expeditionData = ExpeditionManager.Instance.GetExpedition();
            UpdateNodeVisuals(expeditionData?.NodeData, expeditionData?.CurrentNodeIndex ?? 0);
        }

        // Unsubscribe from events to prevent memory leaks
        void OnDestroy()
        {
            ExpeditionManager.Instance.OnNodeUpdated -= UpdateNodeVisuals;
            ExpeditionManager.Instance.OnSceneTransitionCompleted -= UpdateNodeVisuals;
        }

        // Render node map visuals with styles and tooltips
        private void UpdateNodeVisuals(List<NodeData> nodes, int currentNodeIndex)
        {
            if (nodeContainer == null)
            {
                Debug.LogError("ExpeditionSceneVisualController: nodeContainer is null!");
                return;
            }

            nodeContainer.Clear();
            if (nodes == null) return;

            for (int i = 0; i < nodes.Count; i++)
            {
                VisualElement nodeBox = new VisualElement();
                nodeBox.AddToClassList("node-box");
                nodeBox.AddToClassList(nodes[i].IsCombat ? "node-combat" : "node-noncombat");
                if (i == currentNodeIndex)
                {
                    nodeBox.AddToClassList("node-current");
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