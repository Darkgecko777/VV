using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class ExpeditionUIManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private ExpeditionManager expeditionManager;
        [SerializeField] private VisualConfig visualConfig;
        private VisualElement root;
        private VisualElement nodeContainer;
        private VisualElement popoutContainer;
        private Label flavourText;
        private Button continueButton;
        private VisualElement portraitContainer;
        private float fadeDuration = 0.5f;

        void Start()
        {
            if (uiDocument == null || fadeCanvasGroup == null || expeditionManager == null || visualConfig == null)
            {
                Debug.LogError($"ExpeditionUIManager: Missing references! UIDocument: {uiDocument != null}, FadeCanvasGroup: {fadeCanvasGroup != null}, ExpeditionManager: {expeditionManager != null}, VisualConfig: {visualConfig != null}");
                return;
            }

            root = uiDocument.rootVisualElement;
            nodeContainer = root.Q<VisualElement>("NodeContainer");
            popoutContainer = root.Q<VisualElement>("PopoutContainer");
            flavourText = popoutContainer.Q<Label>("FlavourText");
            continueButton = popoutContainer.Q<Button>("ContinueButton");
            portraitContainer = new VisualElement { name = "PortraitContainer" };
            root.Add(portraitContainer);

            if (nodeContainer == null || popoutContainer == null || flavourText == null || continueButton == null)
            {
                Debug.LogError("ExpeditionUIManager: Missing UI elements from UXML!");
                return;
            }

            portraitContainer.style.flexDirection = FlexDirection.Row;
            portraitContainer.style.position = Position.Absolute;
            portraitContainer.style.top = 200;
            portraitContainer.style.left = 50;
            popoutContainer.style.display = DisplayStyle.None;
            fadeCanvasGroup.alpha = 0;

            expeditionManager.OnExpeditionGenerated += UpdateUI;
            expeditionManager.OnNodeUpdated += UpdateNodeVisuals;
            expeditionManager.OnCombatStarted += FadeToCombat;
            continueButton.clicked += () =>
            {
                popoutContainer.style.display = DisplayStyle.None;
                expeditionManager.OnContinueClicked();
            };

            UpdateUI();
        }

        void OnDestroy()
        {
            expeditionManager.OnExpeditionGenerated -= UpdateUI;
            expeditionManager.OnNodeUpdated -= UpdateNodeVisuals;
            expeditionManager.OnCombatStarted -= FadeToCombat;
            continueButton.clicked -= null;
        }

        private void UpdateUI()
        {
            var expeditionData = expeditionManager.GetExpedition();
            UpdatePortraits(expeditionData?.Party);
            UpdateNodeVisuals(expeditionData?.NodeData, expeditionData?.CurrentNodeIndex ?? 0);
            UpdateFlavourText(expeditionData);
        }

        private void UpdatePortraits(PartyData party)
        {
            portraitContainer.Clear();
            if (party == null || party.HeroStats.Count == 0) return;

            for (int i = 0; i < party.HeroStats.Count; i++)
            {
                VisualElement portrait = new VisualElement
                {
                    name = $"Portrait{i + 1}",
                    style = { width = 64, height = 64 }
                };
                //Sprite sprite = visualConfig.GetPortrait(party.HeroStats[i].Type.Id);
                //if (sprite != null)
                //{
                //    portrait.style.backgroundImage = new StyleBackground(sprite);
                //}
                portrait.AddToClassList("portrait");
                portraitContainer.Add(portrait);
            }
        }

        private void UpdateNodeVisuals(List<NodeData> nodes, int currentIndex)
        {
            nodeContainer.Clear();
            if (nodes == null) return;

            for (int i = 0; i < nodes.Count; i++)
            {
                VisualElement nodeBox = new VisualElement
                {
                    name = $"Node{i + 1}",
                    style = { width = 100, height = 100 }
                };
                string nodeType = (i == currentIndex) ? "Active" : (i < currentIndex) ? "Passed" : nodes[i].NodeType;
                Color color = visualConfig.GetNodeColor(nodeType);
                nodeBox.style.backgroundColor = color;
                nodeBox.AddToClassList("node-box");

                Label nodeLabel = new Label
                {
                    name = $"Node{i + 1}_Label",
                    text = $"Node {i + 1}",
                    style = { unityTextAlign = TextAnchor.MiddleCenter }
                };
                nodeLabel.AddToClassList("node-label");
                nodeBox.Add(nodeLabel);
                nodeContainer.Add(nodeBox);
            }
        }

        private void UpdateFlavourText(ExpeditionData expeditionData)
        {
            if (expeditionData == null || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                popoutContainer.style.display = DisplayStyle.None;
                return;
            }

            NodeData node = expeditionData.NodeData[expeditionData.CurrentNodeIndex];
            flavourText.text = node.IsCombat ? "" : node.FlavourText;
            popoutContainer.style.display = node.IsCombat ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void FadeToCombat()
        {
            StartCoroutine(FadeRoutine(() => { }));
        }

        private IEnumerator FadeRoutine(System.Action onComplete)
        {
            expeditionManager.SetTransitioning(true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1;
            onComplete?.Invoke();
            expeditionManager.SetTransitioning(false);
        }
    }
}