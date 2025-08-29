using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class ExpeditionViewController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private EventBusSO eventBus;

        private VisualElement root;
        private VisualElement popoutContainer;
        private Label flavourText;
        private Button continueButton;
        private VisualElement portraitContainer;
        private VisualElement fadeOverlay;
        private VisualElement nodeContainer;
        private float fadeDuration = 0.5f;
        private bool isInitialized;

        void Awake()
        {
            if (!ValidateReferences())
            {
                isInitialized = false;
                return;
            }

            root = uiDocument.rootVisualElement;
            popoutContainer = root.Q<VisualElement>("PopoutContainer");
            flavourText = popoutContainer?.Q<Label>("FlavourText");
            continueButton = root.Q<Button>("ContinueButton");
            fadeOverlay = root.Q<VisualElement>("FadeOverlay");
            nodeContainer = root.Q<VisualElement>("NodeContainer");

            if (popoutContainer == null || flavourText == null || continueButton == null || fadeOverlay == null || nodeContainer == null)
            {
                Debug.LogError($"ExpeditionViewController: Missing UI elements! PopoutContainer: {popoutContainer != null}, FlavourText: {flavourText != null}, ContinueButton: {continueButton != null}, FadeOverlay: {fadeOverlay != null}, NodeContainer: {nodeContainer != null}");
                isInitialized = false;
                return;
            }

            isInitialized = true;
            InitializeUI();
        }

        void Start()
        {
            if (isInitialized)
            {
                SubscribeToEventBus();
                InitializePortraits();
                UpdateUI(new EventBusSO.NodeUpdateData { nodes = ExpeditionManager.Instance.GetExpedition().NodeData, currentIndex = ExpeditionManager.Instance.GetExpedition().CurrentNodeIndex });
            }
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                UnsubscribeFromEventBus();
            }
            if (root != null)
            {
                root.Clear();
                Debug.Log("ExpeditionViewController: Cleared root VisualElement on destroy");
            }
            popoutContainer = null;
            flavourText = null;
            continueButton = null;
            fadeOverlay = null;
            portraitContainer = null;
            nodeContainer = null;
        }

        private void InitializeUI()
        {
            flavourText.style.color = uiConfig.TextColor;
            flavourText.style.unityFont = uiConfig.PixelFont;
            continueButton.style.color = uiConfig.TextColor;
            continueButton.style.unityFont = uiConfig.PixelFont;

            continueButton.pickingMode = PickingMode.Position;
            fadeOverlay.pickingMode = PickingMode.Ignore;
            popoutContainer.style.display = DisplayStyle.None;
            fadeOverlay.style.display = DisplayStyle.None;

            continueButton.clicked += () =>
            {
                popoutContainer.style.display = DisplayStyle.None;
                eventBus.RaiseContinueClicked();
                Debug.Log("ExpeditionViewController: Continue button clicked");
            };
        }

        private void InitializePortraits()
        {
            portraitContainer = new VisualElement { name = "PortraitContainer" };
            root.Add(portraitContainer);
            portraitContainer.style.flexDirection = FlexDirection.Row;
            portraitContainer.style.position = Position.Absolute;
            portraitContainer.style.top = 200;
            portraitContainer.style.left = 50;

            UpdatePortraits(ExpeditionManager.Instance.GetExpedition().Party);
        }

        private void UpdateUI(EventBusSO.NodeUpdateData data)
        {
            if (!isInitialized) return;

            var nodes = data.nodes;
            var currentIndex = data.currentIndex;
            if (nodes == null || currentIndex < 0 || currentIndex >= nodes.Count)
            {
                Debug.LogWarning($"ExpeditionViewController: Invalid node data! Nodes: {nodes != null}, Index: {currentIndex}, Count: {nodes?.Count ?? 0}");
                return;
            }

            var node = nodes[currentIndex];
            if (node == null)
            {
                Debug.LogWarning("ExpeditionViewController: Current node is null!");
                return;
            }

            if (!node.IsCombat)
            {
                flavourText.text = node.FlavourText;
                flavourText.style.color = uiConfig.TextColor;
                flavourText.style.unityFont = uiConfig.PixelFont;
                popoutContainer.style.display = DisplayStyle.Flex;
                continueButton.style.display = DisplayStyle.Flex;
                continueButton.text = "Continue";
            }
            else if (node.IsCombat)
            {
                popoutContainer.style.display = DisplayStyle.None;
                continueButton.style.display = DisplayStyle.Flex;
                continueButton.text = "Continue";
                FadeToCombat();
            }
        }

        private void UpdatePortraits(PartyData partyData)
        {
            if (!isInitialized || portraitContainer == null) return;
            portraitContainer.Clear();

            var heroes = partyData.GetHeroes();
            for (int i = 0; i < heroes.Count; i++)
            {
                VisualElement portrait = new VisualElement();
                portrait.AddToClassList("portrait");
                Sprite sprite = visualConfig.GetPortrait(heroes[i].Id);
                if (sprite != null)
                {
                    portrait.style.backgroundImage = new StyleBackground(sprite);
                }
                portraitContainer.Add(portrait);
            }
            Debug.Log($"ExpeditionViewController: Updated portraits, count: {heroes.Count}");
        }

        private void UpdateNodeVisuals(EventBusSO.NodeUpdateData data)
        {
            if (!isInitialized || nodeContainer == null) return;

            var nodes = data.nodes;
            int currentNodeIndex = data.currentIndex;

            nodeContainer.Clear();
            if (nodes == null)
            {
                Debug.LogWarning("ExpeditionViewController: Nodes list is null in UpdateNodeVisuals!");
                return;
            }

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
            Debug.Log($"ExpeditionViewController: Updated node visuals, count: {nodes.Count}, current index: {currentNodeIndex}");
        }

        public void FadeToCombat()
        {
            if (fadeOverlay == null) return;
            StartCoroutine(FadeRoutine(() => { }));
        }

        private IEnumerator FadeRoutine(System.Action onComplete)
        {
            ExpeditionManager.Instance.SetTransitioning(true);
            fadeOverlay.style.display = DisplayStyle.Flex;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.style.opacity = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                yield return null;
            }
            fadeOverlay.style.opacity = 1;
            onComplete?.Invoke();
            fadeOverlay.style.opacity = 0;
            fadeOverlay.style.display = DisplayStyle.None;
            ExpeditionManager.Instance.SetTransitioning(false);
        }

        private void SubscribeToEventBus()
        {
            eventBus.OnNodeUpdated += UpdateUI;
            eventBus.OnNodeUpdated += UpdateNodeVisuals;
            eventBus.OnSceneTransitionCompleted += UpdateUI;
            eventBus.OnSceneTransitionCompleted += UpdateNodeVisuals;
            eventBus.OnPartyUpdated += UpdatePortraits;
        }

        private void UnsubscribeFromEventBus()
        {
            eventBus.OnNodeUpdated -= UpdateUI;
            eventBus.OnNodeUpdated -= UpdateNodeVisuals;
            eventBus.OnSceneTransitionCompleted -= UpdateUI;
            eventBus.OnSceneTransitionCompleted -= UpdateNodeVisuals;
            eventBus.OnPartyUpdated -= UpdatePortraits;
            Debug.Log("ExpeditionViewController: Unsubscribed from EventBusSO");
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || visualConfig == null || uiConfig == null || eventBus == null)
            {
                Debug.LogError($"ExpeditionViewController: Missing references! UIDocument: {uiDocument != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, EventBus: {eventBus != null}");
                return false;
            }
            return true;
        }
    }
}