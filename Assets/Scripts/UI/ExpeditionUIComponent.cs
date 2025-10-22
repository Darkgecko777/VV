using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class ExpeditionUIComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private EventBusSO eventBus;
        private VisualElement root;
        private VisualElement popoutContainer;
        private Label flavourText;
        private Button continueButton;
        private VisualElement fadeOverlay;
        private VisualElement nodeContainer;
        private VisualElement portraitContainer;
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
                Debug.LogError($"ExpeditionUIComponent: Missing UI elements! PopoutContainer: {popoutContainer != null}, FlavourText: {flavourText != null}, ContinueButton: {continueButton != null}, FadeOverlay: {fadeOverlay != null}, NodeContainer: {nodeContainer != null}");
                isInitialized = false;
                return;
            }
            isInitialized = true;
        }

        void Start()
        {
            if (isInitialized)
            {
                StartCoroutine(InitializeUIAsync());
                SubscribeToEventBus();
                InitializePortraits();
                StartCoroutine(InitializeNodes());
            }
        }

        private IEnumerator InitializeUIAsync()
        {
            yield return null; // Wait one frame to ensure main thread
            if (uiConfig == null)
            {
                Debug.LogWarning("ExpeditionUIComponent: uiConfig is null, skipping style assignments.");
                yield break;
            }
            if (flavourText != null)
            {
                flavourText.style.color = uiConfig.TextColor;
            }
            if (continueButton != null)
            {
                continueButton.style.color = uiConfig.TextColor;
                continueButton.pickingMode = PickingMode.Position;
            }
            if (fadeOverlay != null)
            {
                fadeOverlay.pickingMode = PickingMode.Ignore;
                fadeOverlay.style.display = DisplayStyle.None;
            }
            if (popoutContainer != null)
            {
                popoutContainer.style.display = DisplayStyle.None;
            }
            if (continueButton != null)
            {
                continueButton.clicked += () =>
                {
                    if (popoutContainer != null)
                        popoutContainer.style.display = DisplayStyle.None;
                    eventBus.RaiseContinueClicked();
                };
            }
        }

        public void SetContinueButtonEnabled(bool enabled)
        {
            var continueButton = root.Q<Button>("ContinueButton");
            if (continueButton != null)
            {
                continueButton.SetEnabled(enabled);
                continueButton.style.opacity = enabled ? 1f : 0.5f;
                Debug.Log($"ExpeditionUIComponent: Continue button {(enabled ? "ENABLED" : "DISABLED")}");
            }
            else
            {
                Debug.LogWarning("ExpeditionUIComponent: ContinueButton not found in UXML!");
            }
        }

        private IEnumerator InitializeNodes()
        {
            yield return new WaitForEndOfFrame();
            var expedition = ExpeditionManager.Instance.GetExpedition();
            var nodeData = new EventBusSO.NodeUpdateData { nodes = expedition.NodeData, currentIndex = expedition.CurrentNodeIndex };
            UpdateUI(nodeData);
            UpdateNodeVisuals(nodeData);
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
            }
            popoutContainer = null;
            flavourText = null;
            continueButton = null;
            fadeOverlay = null;
            portraitContainer = null;
            nodeContainer = null;
        }

        private void InitializePortraits()
        {
            portraitContainer = new VisualElement { name = "PortraitContainer" };
            root.Add(portraitContainer);
            portraitContainer.style.flexDirection = FlexDirection.Row;
            var heroes = ExpeditionManager.Instance.GetExpedition().Party.HeroStats;
            foreach (var hero in heroes)
            {
                VisualElement portrait = new VisualElement { name = $"Portrait_{hero.Id}" };
                portrait.AddToClassList("portrait");
                Sprite sprite = visualConfig.GetPortrait(hero.Id);
                if (sprite != null)
                {
                    portrait.style.backgroundImage = new StyleBackground(sprite);
                }
                portraitContainer.Add(portrait);
            }
        }

        private void UpdateNodeVisuals(EventBusSO.NodeUpdateData data)
        {
            if (!isInitialized || nodeContainer == null)
            {
                Debug.LogWarning("ExpeditionUIComponent: UpdateNodeVisuals skipped - not initialized or NodeContainer missing");
                return;
            }
            var nodes = data.nodes;
            int currentNodeIndex = data.currentIndex;
            nodeContainer.Clear();
            if (nodes == null)
            {
                Debug.LogWarning("ExpeditionUIComponent: Nodes list is null in UpdateNodeVisuals!");
                return;
            }
            for (int i = 0; i < nodes.Count; i++)
            {
                VisualElement nodeBox = new VisualElement();
                nodeBox.AddToClassList("node-box");
                nodeBox.AddToClassList(nodes[i].IsCombat ? "node-combat" : "node-noncombat");
                if (i == 0)
                {
                    nodeBox.AddToClassList("node-temple");
                }
                if (i == currentNodeIndex)
                {
                    nodeBox.AddToClassList("node-current");
                }
                Color nodeColor = visualConfig.GetNodeColor(nodes[i].NodeType);
                nodeBox.style.backgroundColor = new StyleColor(nodeColor);
                Label nodeLabel = new Label($"Node {i + 1} ({nodes[i].NodeType})");
                nodeLabel.AddToClassList("node-label");
                if (uiConfig != null)
                {
                    nodeLabel.style.color = uiConfig.TextColor;
                }
                nodeBox.Add(nodeLabel);
                if (nodes[i].SeededViruses.Count > 0)
                {
                    nodeBox.tooltip = $"Seeded: {string.Join(", ", nodes[i].SeededViruses.ConvertAll(v => v.VirusID))}";
                }
                nodeContainer.Add(nodeBox);
            }
        }

        public void FadeToCombat(System.Action onStartLoad = null)
        {
            if (fadeOverlay == null) return;
            StartCoroutine(FadeRoutine(onStartLoad));
        }

        private IEnumerator FadeRoutine(System.Action onStartLoad)
        {
            fadeOverlay.style.display = DisplayStyle.Flex;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.style.opacity = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                yield return null;
            }
            fadeOverlay.style.opacity = 1f;
            onStartLoad?.Invoke();
            yield return null;
            var asyncOp = ExpeditionManager.Instance.CurrentAsyncOp;
            if (asyncOp == null)
            {
                while (ExpeditionManager.Instance.IsTransitioning)
                {
                    yield return null;
                }
            }
            else
            {
                while (asyncOp.progress < 0.8f)
                {
                    yield return null;
                }
                elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float targetOpacity = Mathf.Lerp(1, 0, elapsed / fadeDuration);
                    fadeOverlay.style.opacity = Mathf.Max(targetOpacity, 1f - asyncOp.progress);
                    yield return null;
                }
                fadeOverlay.style.opacity = 0f;
            }
            fadeOverlay.style.display = DisplayStyle.None;
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
        }

        private void UpdateUI(EventBusSO.NodeUpdateData data)
        {
            if (!isInitialized || flavourText == null) return;
            var nodes = data.nodes;
            int currentIndex = data.currentIndex;
            if (nodes != null && currentIndex >= 0 && currentIndex < nodes.Count)
            {
                flavourText.text = nodes[currentIndex].IsCombat && nodes[currentIndex].Completed ? "Combat Won!" : nodes[currentIndex].FlavourText;
                popoutContainer.style.display = (nodes[currentIndex].IsCombat && !nodes[currentIndex].Completed || currentIndex == 0) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private void UpdatePortraits(PartyData partyData)
        {
            if (!isInitialized || portraitContainer == null) return;
            portraitContainer.Clear();
            foreach (var hero in partyData.HeroStats)
            {
                VisualElement portrait = new VisualElement { name = $"Portrait_{hero.Id}" };
                portrait.AddToClassList("portrait");
                Sprite sprite = visualConfig.GetPortrait(hero.Id);
                if (sprite != null)
                {
                    portrait.style.backgroundImage = new StyleBackground(sprite);
                }
                portraitContainer.Add(portrait);
            }
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || visualConfig == null || uiConfig == null || eventBus == null)
            {
                Debug.LogError($"ExpeditionUIComponent: Missing references! UIDocument: {uiDocument != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, EventBus: {eventBus != null}");
                return false;
            }
            return true;
        }
    }
}