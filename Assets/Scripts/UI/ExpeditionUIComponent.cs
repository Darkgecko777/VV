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

        // Non-combat UI
        private VisualElement skillCheckPanel;
        private Label skillCheckLabel;
        private VisualElement outcomePreview;
        private Label successLabel, failureLabel;
        private Button resolveButton;
        private VisualElement resultPanel;
        private Label resultText;

        private float fadeDuration = 0.5f;
        private bool isInitialized;
        private NonCombatEncounterSO currentEncounter;
        private NodeData currentNode;

        void Awake()
        {
            if (!ValidateReferences()) { isInitialized = false; return; }

            root = uiDocument.rootVisualElement;
            popoutContainer = root.Q<VisualElement>("PopoutContainer");
            flavourText = popoutContainer?.Q<Label>("FlavourText");
            continueButton = root.Q<Button>("ContinueButton");
            fadeOverlay = root.Q<VisualElement>("FadeOverlay");
            nodeContainer = root.Q<VisualElement>("NodeContainer");
            partyPanel = new VisualElement { name = "PartyPanel" };
            root.Add(partyPanel);

            skillCheckPanel = popoutContainer.Q<VisualElement>("SkillCheckPanel") ?? CreateSkillCheckPanel();
            skillCheckLabel = skillCheckPanel.Q<Label>("SkillCheckLabel");
            outcomePreview = popoutContainer.Q<VisualElement>("OutcomePreview") ?? CreateOutcomePreview();
            successLabel = outcomePreview.Q<Label>("SuccessLabel");
            failureLabel = outcomePreview.Q<Label>("FailureLabel");
            resolveButton = popoutContainer.Q<Button>("ResolveButton") ?? CreateResolveButton();
            resultPanel = popoutContainer.Q<VisualElement>("ResultPanel") ?? CreateResultPanel();
            resultText = resultPanel.Q<Label>("ResultText");

            if (popoutContainer == null || flavourText == null || continueButton == null || nodeContainer == null)
            {
                Debug.LogError("ExpeditionUIComponent: Missing critical UI elements!");
                isInitialized = false;
                return;
            }
            if (fadeOverlay == null) Debug.LogWarning("FadeOverlay missing.");
            isInitialized = true;
        }

        void Start()
        {
            if (isInitialized)
            {
                StartCoroutine(InitializeUIAsync());
                SubscribeToEventBus();
                StartCoroutine(InitializeNodes());
            }
        }

        private IEnumerator InitializeUIAsync()
        {
            yield return null;
            if (uiConfig == null) { Debug.LogWarning("uiConfig null."); yield break; }

            flavourText.style.color = uiConfig.TextColor;
            continueButton.style.color = uiConfig.TextColor;
            continueButton.pickingMode = PickingMode.Position;

            if (fadeOverlay != null) { fadeOverlay.pickingMode = PickingMode.Ignore; fadeOverlay.style.display = DisplayStyle.None; }
            popoutContainer.style.display = DisplayStyle.None;

            continueButton.clicked += () =>
            {
                popoutContainer.style.display = DisplayStyle.None;
                eventBus.RaiseContinueClicked();
            };

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
            partyPanel.style.borderTopWidth = partyPanel.style.borderBottomWidth = partyPanel.style.borderLeftWidth = partyPanel.style.borderRightWidth = 2;
            partyPanel.style.borderTopColor = partyPanel.style.borderBottomColor = partyPanel.style.borderLeftColor = partyPanel.style.borderRightColor = new StyleColor(Color.black);
            partyPanel.style.borderTopLeftRadius = partyPanel.style.borderTopRightRadius = partyPanel.style.borderBottomLeftRadius = partyPanel.style.borderBottomRightRadius = 10;
        }

        public void SetContinueButtonEnabled(bool enabled)
        {
            if (continueButton != null)
            {
                continueButton.SetEnabled(enabled);
                continueButton.style.opacity = enabled ? 1f : 0.5f;
            }
        }

        private IEnumerator InitializeNodes()
        {
            yield return new WaitForEndOfFrame();
            var expedition = ExpeditionManager.Instance.GetExpedition();
            var data = new EventBusSO.NodeUpdateData { nodes = expedition.NodeData, currentIndex = expedition.CurrentNodeIndex };
            UpdateUI(data);
            UpdateNodeVisuals(data);
        }

        void OnDestroy()
        {
            if (eventBus != null) UnsubscribeFromEventBus();
            root?.Clear();
        }

        private void UpdateNodeVisuals(EventBusSO.NodeUpdateData data)
        {
            if (!isInitialized || nodeContainer == null) return;
            nodeContainer.Clear();
            var nodes = data.nodes;
            int currentIndex = data.currentIndex;
            if (nodes == null) return;

            for (int i = 0; i < nodes.Count; i++)
            {
                var nodeBox = new VisualElement();
                nodeBox.AddToClassList("node-box");
                nodeBox.AddToClassList(nodes[i].IsCombat ? "node-combat" : "node-noncombat");
                if (i == 0) nodeBox.AddToClassList("node-temple");
                if (i == currentIndex) nodeBox.AddToClassList("node-current");

                Color nodeColor = visualConfig.GetNodeColor(nodes[i].NodeType);
                nodeBox.style.backgroundColor = new StyleColor(nodeColor);

                var label = new Label($"Node {i + 1} ({nodes[i].NodeType})");
                label.AddToClassList("node-label");
                if (uiConfig != null) label.style.color = uiConfig.TextColor;
                nodeBox.Add(label);

                if (nodes[i].SeededViruses.Count > 0)
                    nodeBox.tooltip = $"Seeded: {string.Join(", ", nodes[i].SeededViruses.ConvertAll(v => v.VirusID))}";

                nodeContainer.Add(nodeBox);
            }
        }

        public void FadeToCombat(System.Action onStartLoad = null)
        {
            if (fadeOverlay == null) { onStartLoad?.Invoke(); return; }
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
                while (ExpeditionManager.Instance.IsTransitioning) yield return null;
            }
            else
            {
                while (asyncOp.progress < 0.8f) yield return null;
                elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float target = Mathf.Lerp(1, 0, elapsed / fadeDuration);
                    fadeOverlay.style.opacity = Mathf.Max(target, 1f - asyncOp.progress);
                    yield return null;
                }
            }
            fadeOverlay.style.opacity = 0f;
            fadeOverlay.style.display = DisplayStyle.None;
        }

        private void SubscribeToEventBus()
        {
            eventBus.OnNodeUpdated += UpdateUI;
            eventBus.OnNodeUpdated += UpdateNodeVisuals;
            eventBus.OnSceneTransitionCompleted += UpdateUI;
            eventBus.OnSceneTransitionCompleted += UpdateNodeVisuals;
            eventBus.OnPartyUpdated += UpdatePartyPanel;
            eventBus.OnNonCombatEncounter += ShowNonCombatEncounter;
            eventBus.OnNonCombatResolved += ShowNonCombatResult;
        }

        private void UnsubscribeFromEventBus()
        {
            eventBus.OnNodeUpdated -= UpdateUI;
            eventBus.OnNodeUpdated -= UpdateNodeVisuals;
            eventBus.OnSceneTransitionCompleted -= UpdateUI;
            eventBus.OnSceneTransitionCompleted -= UpdateNodeVisuals;
            eventBus.OnPartyUpdated -= UpdatePartyPanel;
            eventBus.OnNonCombatEncounter -= ShowNonCombatEncounter;
            eventBus.OnNonCombatResolved -= ShowNonCombatResult;
        }

        private void UpdateUI(EventBusSO.NodeUpdateData data)
        {
            if (!isInitialized || flavourText == null) return;

            var nodes = data.nodes;
            int currentIndex = data.currentIndex;
            if (nodes == null || currentIndex < 0 || currentIndex >= nodes.Count) return;

            var node = nodes[currentIndex];

            // === DECLARE showContinue once at the top ===
            bool showContinue;

            // === PRESERVE RESULT TEXT AFTER NON-COMBAT RESOLVE ===
            if (!node.IsCombat && node.Completed)
            {
                showContinue = true;
                continueButton.style.display = showContinue ? DisplayStyle.Flex : DisplayStyle.None;
                return;
            }

            // === NORMAL FLOW ===
            if (node.IsCombat && !node.Completed)
            {
                popoutContainer.style.display = DisplayStyle.None;
                return;
            }

            flavourText.text = node.IsCombat && node.Completed ? "Combat Won!" : node.FlavourText;

            bool showPopout = !node.IsCombat && currentIndex != 0;
            popoutContainer.style.display = showPopout ? DisplayStyle.Flex : DisplayStyle.None;

            skillCheckPanel.style.display = DisplayStyle.None;
            outcomePreview.style.display = DisplayStyle.None;
            resolveButton.style.display = DisplayStyle.None;
            resultPanel.style.display = DisplayStyle.None;

            showContinue = !node.IsCombat || (node.IsCombat && node.Completed);
            continueButton.style.display = showContinue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ---- RESOLVE BUTTON WIRING ----
        public void ShowNonCombatEncounter(EventBusSO.NonCombatEncounterData data)
        {
            currentEncounter = data.encounter;
            currentNode = data.node;
            flavourText.text = data.encounter.Description;
            popoutContainer.style.display = DisplayStyle.Flex;
            skillCheckLabel.text = $"Skill Check: {data.encounter.SkillType} ({data.encounter.CheckMode}) vs DC {data.encounter.DifficultyCheck}";
            skillCheckPanel.style.display = DisplayStyle.Flex;
            successLabel.text = FormatOutcome(data.encounter.SuccessOutcome);
            failureLabel.text = FormatOutcome(data.encounter.FailureOutcome);
            outcomePreview.style.display = DisplayStyle.Flex;

            // Wire Resolve button
            resolveButton.style.display = DisplayStyle.Flex;
            resolveButton.clicked -= OnResolveClicked;
            resolveButton.clicked += OnResolveClicked;

            resultPanel.style.display = DisplayStyle.None;
            continueButton.style.display = DisplayStyle.None;
        }

        private void OnResolveClicked()
        {
            if (currentEncounter == null || currentNode == null) return;
            eventBus.RaiseNonCombatResolveRequested(currentEncounter, currentNode);
        }

        public void ShowNonCombatResult(EventBusSO.NonCombatResultData data)
        {
            // Hide encounter UI
            skillCheckPanel.style.display = DisplayStyle.None;
            outcomePreview.style.display = DisplayStyle.None;
            resolveButton.style.display = DisplayStyle.None;
            resultPanel.style.display = DisplayStyle.None;

            // Replace description with narrative + any effects/viruses
            flavourText.text = data.narrative;
            if (!string.IsNullOrEmpty(data.result))
                flavourText.text += " " + data.result;

            flavourText.style.color = data.success
                ? new StyleColor(Color.green)
                : new StyleColor(Color.red);

            continueButton.style.display = DisplayStyle.Flex;
        }

        private string FormatOutcome(string outcome)
        {
            if (string.IsNullOrEmpty(outcome)) return "—";
            return outcome.Replace(";", " | ").Replace(":", " ");
        }

        private void UpdatePartyPanel(PartyData partyData)
        {
            if (!isInitialized || partyPanel == null) return;
            partyPanel.Clear();
            var heroes = partyData.GetHeroes()?.OrderBy(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition).ToList() ?? new List<CharacterStats>();

            for (int i = 0; i < 4; i++)
            {
                var heroCard = new VisualElement { name = $"HeroCard_{(i < heroes.Count ? heroes[i].Id : "Empty")}" };
                heroCard.AddToClassList("hero-card");

                var portrait = new VisualElement { name = $"Portrait_{(i < heroes.Count ? heroes[i].Id : "Empty")}" };
                portrait.AddToClassList("portrait");
                if (i < heroes.Count && heroes[i] != null)
                {
                    var sprite = visualConfig.GetPortrait(heroes[i].Id);
                    if (sprite != null) portrait.style.backgroundImage = new StyleBackground(sprite);
                    else portrait.style.backgroundColor = new StyleColor(Color.gray);
                    if (heroes[i].Health <= 0) portrait.AddToClassList("portrait-dead");
                    portrait.tooltip = $"Health: {heroes[i].Health}/{heroes[i].MaxHealth}, Morale: {heroes[i].Morale}/{heroes[i].MaxMorale}";
                }
                else
                {
                    portrait.style.backgroundColor = new StyleColor(Color.gray);
                    portrait.tooltip = "Empty Slot";
                }
                portrait.style.width = 100; portrait.style.height = 100;
                heroCard.Add(portrait);

                var barsContainer = new VisualElement();
                barsContainer.AddToClassList("bars-container");
                var healthBar = new VisualElement();
                healthBar.AddToClassList("health-bar");
                float healthPct = i < heroes.Count && heroes[i].MaxHealth > 0 ? (float)heroes[i].Health / heroes[i].MaxHealth * 100f : 0f;
                healthBar.style.width = Length.Percent(healthPct);
                barsContainer.Add(healthBar);

                var moraleBar = new VisualElement();
                moraleBar.AddToClassList("morale-bar");
                float moralePct = i < heroes.Count && heroes[i].MaxMorale > 0 ? (float)heroes[i].Morale / heroes[i].MaxMorale * 100f : 0f;
                moraleBar.style.width = Length.Percent(moralePct);
                barsContainer.Add(moraleBar);
                heroCard.Add(barsContainer);

                var viruses = new VisualElement { name = "Viruses" };
                viruses.AddToClassList("viruses-container");
                viruses.style.flexDirection = FlexDirection.Row;
                viruses.style.flexWrap = Wrap.Wrap;
                if (i < heroes.Count && heroes[i] != null)
                {
                    foreach (var virus in heroes[i].Infections)
                    {
                        var icon = new Image();
                        icon.image = virus.Sprite?.texture ?? Texture2D.grayTexture;
                        icon.style.width = icon.style.height = 20;
                        icon.tooltip = $"{virus.DisplayName} ({virus.RarityString})";
                        viruses.Add(icon);
                    }
                }
                heroCard.Add(viruses);
                partyPanel.Add(heroCard);
            }
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || visualConfig == null || uiConfig == null || eventBus == null)
            {
                Debug.LogError("ExpeditionUIComponent: Missing refs!");
                return false;
            }
            return true;
        }

        // Fallback UI creation
        private VisualElement CreateSkillCheckPanel()
        {
            var panel = new VisualElement { name = "SkillCheckPanel" };
            panel.style.display = DisplayStyle.None;
            panel.style.marginTop = 15;
            var label = new Label { name = "SkillCheckLabel" };
            label.AddToClassList("skill-check-label");
            panel.Add(label);
            popoutContainer.Add(panel);
            return panel;
        }

        private VisualElement CreateOutcomePreview()
        {
            var panel = new VisualElement { name = "OutcomePreview" };
            panel.style.display = DisplayStyle.None;
            panel.style.marginTop = 15;
            var success = new Label { name = "SuccessLabel" };
            success.AddToClassList("outcome-success");
            var failure = new Label { name = "FailureLabel" };
            failure.AddToClassList("outcome-failure");
            panel.Add(success); panel.Add(failure);
            popoutContainer.Add(panel);
            return panel;
        }

        private Button CreateResolveButton()
        {
            var btn = new Button { name = "ResolveButton", text = "Resolve Encounter" };
            btn.style.display = DisplayStyle.None;
            btn.style.marginTop = 20;
            btn.AddToClassList("resolve-button");
            popoutContainer.Add(btn);
            return btn;
        }

        private VisualElement CreateResultPanel()
        {
            var panel = new VisualElement { name = "ResultPanel" };
            panel.style.display = DisplayStyle.None;
            panel.style.marginTop = 20;
            var label = new Label { name = "ResultText" };
            label.AddToClassList("result-text");
            panel.Add(label);
            popoutContainer.Add(panel);
            return panel;
        }
    }
}