using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        private VisualElement partyPanel;
        private VisualElement skillCheckPopup;
        private Label skillCheckLabel;
        private VisualElement virusIconsContainer;
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
            partyPanel = new VisualElement { name = "PartyPanel" };
            skillCheckPopup = new VisualElement { name = "SkillCheckPopup" };
            skillCheckLabel = new Label { name = "SkillCheckLabel" };
            virusIconsContainer = new VisualElement { name = "VirusIconsContainer" };
            skillCheckPopup.Add(skillCheckLabel);
            skillCheckPopup.Add(virusIconsContainer);
            root.Add(partyPanel);
            root.Add(skillCheckPopup);
            if (popoutContainer == null || flavourText == null || continueButton == null || nodeContainer == null)
            {
                Debug.LogError($"ExpeditionUIComponent: Missing critical UI elements! PopoutContainer: {popoutContainer != null}, FlavourText: {flavourText != null}, ContinueButton: {continueButton != null}, FadeOverlay: {fadeOverlay != null}, NodeContainer: {nodeContainer != null}");
                isInitialized = false;
                return;
            }
            if (fadeOverlay == null)
            {
                Debug.LogWarning("ExpeditionUIComponent: FadeOverlay not found in UXML, proceeding without fade effects.");
            }
            isInitialized = true;
        }

        void Start()
        {
            if (isInitialized)
            {
                Debug.Log($"ExpeditionUIComponent: Loaded UXML from {uiDocument.visualTreeAsset?.name}");
                StartCoroutine(InitializeUIAsync());
                SubscribeToEventBus();
                StartCoroutine(InitializeNodes());
            }
        }

        private IEnumerator InitializeUIAsync()
        {
            yield return null;
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
            partyPanel.style.position = Position.Absolute;
            partyPanel.style.bottom = 50;
            partyPanel.style.left = Length.Percent(50);
            partyPanel.style.marginLeft = -300;
            partyPanel.style.width = 600;
            partyPanel.style.height = 300;
            partyPanel.style.flexDirection = FlexDirection.Row;
            partyPanel.style.flexWrap = Wrap.Wrap;
            partyPanel.style.justifyContent = Justify.SpaceAround;
            partyPanel.style.alignItems = Align.Center;
            partyPanel.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.8f));
            partyPanel.style.borderTopWidth = 2;
            partyPanel.style.borderBottomWidth = 2;
            partyPanel.style.borderLeftWidth = 2;
            partyPanel.style.borderRightWidth = 2;
            partyPanel.style.borderTopColor = new StyleColor(Color.black);
            partyPanel.style.borderBottomColor = new StyleColor(Color.black);
            partyPanel.style.borderLeftColor = new StyleColor(Color.black);
            partyPanel.style.borderRightColor = new StyleColor(Color.black);
            partyPanel.style.borderTopLeftRadius = 10;
            partyPanel.style.borderTopRightRadius = 10;
            partyPanel.style.borderBottomLeftRadius = 10;
            partyPanel.style.borderBottomRightRadius = 10;
            skillCheckPopup.AddToClassList("skill-check-popup");
            skillCheckPopup.style.display = DisplayStyle.None;
            skillCheckPopup.style.position = Position.Absolute;
            skillCheckPopup.style.top = 200;
            skillCheckPopup.style.left = Length.Percent(50);
            skillCheckPopup.style.marginLeft = -300;
            skillCheckPopup.style.width = 600;
            skillCheckPopup.style.height = 300;
            skillCheckPopup.pickingMode = PickingMode.Position;
            skillCheckLabel.style.color = uiConfig.TextColor;
            virusIconsContainer.style.flexDirection = FlexDirection.Row;
        }

        public void SetContinueButtonEnabled(bool enabled)
        {
            var continueButton = root.Q<Button>("ContinueButton");
            if (continueButton != null)
            {
                continueButton.SetEnabled(enabled);
                continueButton.style.opacity = enabled ? 1f : 0.5f;
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
            nodeContainer = null;
            partyPanel = null;
            skillCheckPopup = null;
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
            if (fadeOverlay == null)
            {
                Debug.LogWarning("ExpeditionUIComponent: FadeOverlay is null, skipping fade effect.");
                onStartLoad?.Invoke();
                return;
            }
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
            eventBus.OnPartyUpdated += UpdatePartyPanel;
            eventBus.OnVirusSeeded += UpdateVirusIcons;
        }

        private void UnsubscribeFromEventBus()
        {
            eventBus.OnNodeUpdated -= UpdateUI;
            eventBus.OnNodeUpdated -= UpdateNodeVisuals;
            eventBus.OnSceneTransitionCompleted -= UpdateUI;
            eventBus.OnSceneTransitionCompleted -= UpdateNodeVisuals;
            eventBus.OnPartyUpdated -= UpdatePartyPanel;
            eventBus.OnVirusSeeded -= UpdateVirusIcons;
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
                if (!nodes[currentIndex].IsCombat && !nodes[currentIndex].Completed)
                {
                    skillCheckPopup.style.display = DisplayStyle.Flex;
                    skillCheckLabel.text = "Skill Check: " + nodes[currentIndex].FlavourText;
                }
                else
                {
                    skillCheckPopup.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdatePartyPanel(PartyData partyData)
        {
            if (!isInitialized || partyPanel == null) return;
            partyPanel.Clear();
            var heroes = partyData.GetHeroes()?.OrderBy(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition).ToList() ?? new List<CharacterStats>();
            for (int i = 0; i < 4; i++)
            {
                VisualElement heroCard = new VisualElement { name = $"HeroCard_{(i < heroes.Count ? heroes[i].Id : "Empty")}" };
                heroCard.AddToClassList("hero-card");

                VisualElement portrait = new VisualElement { name = $"Portrait_{(i < heroes.Count ? heroes[i].Id : "Empty")}" };
                portrait.AddToClassList("portrait");
                if (i < heroes.Count && heroes[i] != null)
                {
                    Sprite sprite = visualConfig.GetPortrait(heroes[i].Id);
                    if (sprite != null)
                    {
                        portrait.style.backgroundImage = new StyleBackground(sprite);
                    }
                    else
                    {
                        portrait.style.backgroundColor = new StyleColor(Color.gray);
                    }
                    if (heroes[i].Health <= 0) portrait.AddToClassList("portrait-dead");
                    portrait.tooltip = $"Health: {heroes[i].Health}/{heroes[i].MaxHealth}, Morale: {heroes[i].Morale}/{heroes[i].MaxMorale}";
                }
                else
                {
                    portrait.style.backgroundColor = new StyleColor(Color.gray);
                    portrait.tooltip = "Empty Slot";
                }
                portrait.style.width = 100;
                portrait.style.height = 100;
                heroCard.Add(portrait);

                VisualElement barsContainer = new VisualElement();
                barsContainer.AddToClassList("bars-container");
                VisualElement healthBar = new VisualElement();
                healthBar.AddToClassList("health-bar");
                float healthPct = i < heroes.Count && heroes[i].MaxHealth > 0 ? (float)heroes[i].Health / heroes[i].MaxHealth * 100f : 0f;
                healthBar.style.width = Length.Percent(healthPct);
                barsContainer.Add(healthBar);
                VisualElement moraleBar = new VisualElement();
                moraleBar.AddToClassList("morale-bar");
                float moralePct = i < heroes.Count && heroes[i].MaxMorale > 0 ? (float)heroes[i].Morale / heroes[i].MaxMorale * 100f : 0f;
                moraleBar.style.width = Length.Percent(moralePct);
                barsContainer.Add(moraleBar);
                heroCard.Add(barsContainer);

                VisualElement viruses = new VisualElement { name = "Viruses" };
                viruses.AddToClassList("viruses-container");
                viruses.style.flexDirection = FlexDirection.Row;
                viruses.style.flexWrap = Wrap.Wrap;
                if (i < heroes.Count && heroes[i] != null)
                {
                    foreach (var virus in heroes[i].Infections)
                    {
                        Image virusIcon = new Image();
                        virusIcon.image = virus.Sprite?.texture ?? Texture2D.grayTexture;
                        virusIcon.style.width = 20;
                        virusIcon.style.height = 20;
                        virusIcon.tooltip = $"{virus.DisplayName} ({virus.RarityString})";
                        viruses.Add(virusIcon);
                    }
                }
                heroCard.Add(viruses);

                partyPanel.Add(heroCard);
            }
        }

        private void UpdateVirusIcons(EventBusSO.VirusSeededData data)
        {
            if (!isInitialized || virusIconsContainer == null) return;
            virusIconsContainer.Clear();
            Image virusIcon = new Image();
            virusIcon.image = data.virus.Sprite?.texture;
            virusIcon.style.width = 50;
            virusIcon.style.height = 50;
            virusIcon.tooltip = data.virus.DisplayName + " (" + data.virus.RarityString + ")";
            virusIconsContainer.Add(virusIcon);
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