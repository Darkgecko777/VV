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
                StartCoroutine(InitializeNodes());
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
            };
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
                Debug.LogWarning("ExpeditionViewController: UpdateNodeVisuals skipped - not initialized or NodeContainer missing");
                return;
            }
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
            // Fade IN to black
            fadeOverlay.style.display = DisplayStyle.Flex;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.style.opacity = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                yield return null;
            }
            fadeOverlay.style.opacity = 1f;

            // Start the async load RIGHT NOW (black screen hides any hitch)
            onStartLoad?.Invoke();

            // Get the op from manager (assume we expose a getter or pass it— for now, we'll simulate by waiting a frame and polling manager if needed.
            // Better: Modify the callback to return/pass the op, but to keep simple, we'll use a public getter in Manager.
            yield return null;  // Tiny buffer to let load start

            // Wait for load progress (hook via completed for exactness, but progress for smooth fade-out)
            // Assuming we add a public AsyncOperation CurrentAsyncOp {get; private set;} in Manager, set on start.
            var asyncOp = ExpeditionManager.Instance.CurrentAsyncOp;  // You'd add this prop to Manager
            if (asyncOp == null)
            {
                // Fallback: Just wait a fixed time or until transitioning false
                while (ExpeditionManager.Instance.IsTransitioning)
                {
                    yield return null;
                }
            }
            else
            {
                // Progress-based fade-out: Start fading out when 80% done (tune as needed)
                while (asyncOp.progress < 0.8f)
                {
                    yield return null;
                }
                // Now fade OUT from black as load finishes
                elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    // Lerp from 1 (black) to 0 (reveal), but clamp if load finishes early
                    float targetOpacity = Mathf.Lerp(1, 0, elapsed / fadeDuration);
                    fadeOverlay.style.opacity = Mathf.Max(targetOpacity, 1f - asyncOp.progress);  // Stay black until fully loaded
                    yield return null;
                }
                fadeOverlay.style.opacity = 0f;
            }

            fadeOverlay.style.display = DisplayStyle.None;
            // No SetTransitioning(false) here—let Manager's completed handle it
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
                popoutContainer.style.display = (nodes[currentIndex].IsCombat && !nodes[currentIndex].Completed || currentIndex == 0) ? DisplayStyle.None : DisplayStyle.Flex; // Show popout for completed combat or non-combat
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
                Debug.LogError($"ExpeditionViewController: Missing references! UIDocument: {uiDocument != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, EventBus: {eventBus != null}");
                return false;
            }
            return true;
        }
    }
}