using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class TempleViewController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private List<VirusData> availableViruses;
        [SerializeField] private PartyData partyData;

        private DropdownField virusDropdown;
        private DropdownField nodeDropdown;
        private Button generateButton;
        private Button launchButton;
        private Button seedVirusButton;
        private Button healButton;
        private Label favourLabel;
        private VisualElement recruitPortraitContainer;
        private VisualElement expeditionPortraitContainer;
        private VisualElement healPortraitContainer;
        private VisualElement nodeContainer;
        private VisualElement tabContentContainer;
        private Button recruitTabButton;
        private Button expeditionTabButton;
        private Button virusTabButton;
        private Button healTabButton;
        private List<VisualElement> tabContents;
        private List<Button> tabButtons;
        private bool isInitialized;
        private VisualElement root;

        void Awake()
        {
            if (!ValidateReferences())
            {
                isInitialized = false;
                return;
            }

            root = uiDocument.rootVisualElement;
            virusDropdown = root.Q<DropdownField>("VirusDropdown");
            nodeDropdown = root.Q<DropdownField>("NodeDropdown");
            generateButton = root.Q<Button>("GenerateButton");
            launchButton = root.Q<Button>("LaunchButton");
            seedVirusButton = root.Q<Button>("SeedVirusButton");
            healButton = root.Q<Button>("HealButton");
            favourLabel = root.Q<Label>("FavourLabel");
            recruitPortraitContainer = root.Q<VisualElement>("RecruitPortraitContainer");
            expeditionPortraitContainer = root.Q<VisualElement>("ExpeditionPortraitContainer");
            healPortraitContainer = root.Q<VisualElement>("HealPortraitContainer");
            nodeContainer = root.Q<VisualElement>("NodeContainer");
            tabContentContainer = root.Q<VisualElement>("TabContentContainer");
            recruitTabButton = root.Q<Button>("RecruitTab");
            expeditionTabButton = root.Q<Button>("ExpeditionTab");
            virusTabButton = root.Q<Button>("VirusTab");
            healTabButton = root.Q<Button>("HealTab");

            tabContents = new List<VisualElement>
            {
                root.Q<VisualElement>("RecruitTabContent"),
                root.Q<VisualElement>("ExpeditionTabContent"),
                root.Q<VisualElement>("VirusTabContent"),
                root.Q<VisualElement>("HealTabContent")
            };
            tabButtons = new List<Button> { recruitTabButton, expeditionTabButton, virusTabButton, healTabButton };

            if (virusDropdown == null || nodeDropdown == null || generateButton == null || launchButton == null || seedVirusButton == null || healButton == null || favourLabel == null ||
                recruitPortraitContainer == null || expeditionPortraitContainer == null || healPortraitContainer == null || nodeContainer == null ||
                tabContentContainer == null || recruitTabButton == null || expeditionTabButton == null || virusTabButton == null || healTabButton == null)
            {
                Debug.LogError($"TempleViewController: Missing UI elements! VirusDropdown: {virusDropdown != null}, NodeDropdown: {nodeDropdown != null}, " +
                    $"GenerateButton: {generateButton != null}, LaunchButton: {launchButton != null}, SeedVirusButton: {seedVirusButton != null}, HealButton: {healButton != null}, FavourLabel: {favourLabel != null}, " +
                    $"RecruitPortraitContainer: {recruitPortraitContainer != null}, ExpeditionPortraitContainer: {expeditionPortraitContainer != null}, " +
                    $"HealPortraitContainer: {healPortraitContainer != null}, NodeContainer: {nodeContainer != null}, " +
                    $"TabContentContainer: {tabContentContainer != null}, RecruitTab: {recruitTabButton != null}, " +
                    $"ExpeditionTab: {expeditionTabButton != null}, VirusTab: {virusTabButton != null}, HealTab: {healTabButton != null}");
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
                InitializeEmptyPortraits();
                SwitchTab(0);
                UpdateFavourDisplay();
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
            }
            virusDropdown = null;
            nodeDropdown = null;
            generateButton = null;
            launchButton = null;
            seedVirusButton = null;
            healButton = null;
            favourLabel = null;
            recruitPortraitContainer = null;
            expeditionPortraitContainer = null;
            healPortraitContainer = null;
            nodeContainer = null;
            tabContentContainer = null;
            recruitTabButton = null;
            expeditionTabButton = null;
            virusTabButton = null;
            healTabButton = null;
        }

        private void InitializeUI()
        {
            virusDropdown.choices.Clear();
            foreach (var virus in availableViruses)
            {
                if (virus != null) virusDropdown.choices.Add(virus.VirusID);
            }
            virusDropdown.value = virusDropdown.choices.Count > 0 ? virusDropdown.choices[0] : null;
            virusDropdown.style.color = uiConfig.TextColor;
            virusDropdown.style.unityFont = uiConfig.PixelFont;

            nodeDropdown.choices.Clear();
            nodeDropdown.value = null;
            nodeDropdown.style.color = uiConfig.TextColor;
            nodeDropdown.style.unityFont = uiConfig.PixelFont;

            generateButton.style.color = uiConfig.TextColor;
            generateButton.style.unityFont = uiConfig.PixelFont;
            launchButton.style.color = uiConfig.TextColor;
            launchButton.style.unityFont = uiConfig.PixelFont;
            seedVirusButton.style.color = uiConfig.TextColor;
            seedVirusButton.style.unityFont = uiConfig.PixelFont;
            healButton.style.color = uiConfig.TextColor;
            healButton.style.unityFont = uiConfig.PixelFont;
            favourLabel.style.color = uiConfig.TextColor;
            favourLabel.style.unityFont = uiConfig.PixelFont;

            foreach (var button in tabButtons)
            {
                button.style.color = uiConfig.TextColor;
                button.style.unityFont = uiConfig.PixelFont;
            }

            generateButton.SetEnabled(partyData.HeroStats == null || partyData.HeroStats.Count == 0);
            healButton.SetEnabled(partyData.CanHealParty());

            generateButton.clicked += () => eventBus.RaiseExpeditionGenerated(null, null);
            launchButton.clicked += () => eventBus.RaiseLaunchExpedition();
            seedVirusButton.clicked += () =>
            {
                if (nodeDropdown.index >= 0 && virusDropdown.index >= 0)
                {
                    eventBus.RaiseVirusSeeded(virusDropdown.value, nodeDropdown.index);
                }
                else
                {
                    Debug.LogWarning("TempleViewController: Invalid dropdown selection for virus seeding!");
                }
            };
            healButton.clicked += () => eventBus.RaiseHealParty();

            recruitTabButton.clicked += () => SwitchTab(0);
            expeditionTabButton.clicked += () => SwitchTab(1);
            virusTabButton.clicked += () => SwitchTab(2);
            healTabButton.clicked += () => SwitchTab(3);

            launchButton.SetEnabled(false);
        }

        private void SwitchTab(int index)
        {
            for (int i = 0; i < tabContents.Count; i++)
            {
                tabContents[i].style.display = i == index ? DisplayStyle.Flex : DisplayStyle.None;
                tabButtons[i].RemoveFromClassList("active");
                if (i == index)
                {
                    tabButtons[i].AddToClassList("active");
                }
            }
        }

        private void InitializeEmptyPortraits()
        {
            recruitPortraitContainer.Clear();
            expeditionPortraitContainer.Clear();
            healPortraitContainer.Clear();
            for (int i = 0; i < 4; i++)
            {
                VisualElement recruitPortrait = new VisualElement();
                recruitPortrait.AddToClassList("portrait");
                VisualElement recruitHealthBar = new VisualElement();
                recruitHealthBar.AddToClassList("health-bar");
                recruitHealthBar.name = "HealthBar";
                VisualElement recruitMoraleBar = new VisualElement();
                recruitMoraleBar.AddToClassList("morale-bar");
                recruitMoraleBar.name = "MoraleBar";
                VisualElement recruitBarsContainer = new VisualElement();
                recruitBarsContainer.AddToClassList("bars-container");
                recruitBarsContainer.Add(recruitHealthBar);
                recruitBarsContainer.Add(recruitMoraleBar);
                recruitPortrait.Add(recruitBarsContainer);
                recruitPortraitContainer.Add(recruitPortrait);

                VisualElement expeditionPortrait = new VisualElement();
                expeditionPortrait.AddToClassList("portrait");
                VisualElement expeditionHealthBar = new VisualElement();
                expeditionHealthBar.AddToClassList("health-bar");
                expeditionHealthBar.name = "HealthBar";
                VisualElement expeditionMoraleBar = new VisualElement();
                expeditionMoraleBar.AddToClassList("morale-bar");
                expeditionMoraleBar.name = "MoraleBar";
                VisualElement expeditionBarsContainer = new VisualElement();
                expeditionBarsContainer.AddToClassList("bars-container");
                expeditionBarsContainer.Add(expeditionHealthBar);
                expeditionBarsContainer.Add(expeditionMoraleBar);
                expeditionPortrait.Add(expeditionBarsContainer);
                expeditionPortraitContainer.Add(expeditionPortrait);

                VisualElement healPortrait = new VisualElement();
                healPortrait.AddToClassList("portrait");
                VisualElement healHealthBar = new VisualElement();
                healHealthBar.AddToClassList("health-bar");
                healHealthBar.name = "HealthBar";
                VisualElement healMoraleBar = new VisualElement();
                healMoraleBar.AddToClassList("morale-bar");
                healMoraleBar.name = "MoraleBar";
                VisualElement healBarsContainer = new VisualElement();
                healBarsContainer.AddToClassList("bars-container");
                healBarsContainer.Add(healHealthBar);
                healBarsContainer.Add(healMoraleBar);
                healPortrait.Add(healBarsContainer);
                healPortraitContainer.Add(healPortrait);
            }
        }

        private void UpdatePartyVisuals(PartyData partyData)
        {
            if (!isInitialized || partyData == null || recruitPortraitContainer == null || expeditionPortraitContainer == null || healPortraitContainer == null)
            {
                Debug.LogWarning("TempleViewController: PartyData or portrait containers are null, skipping update.");
                return;
            }

            recruitPortraitContainer.Clear();
            expeditionPortraitContainer.Clear();
            healPortraitContainer.Clear();
            var heroes = partyData.GetHeroes().OrderByDescending(h => h.PartyPosition).ToList();

            bool hasActiveParty = partyData.HeroStats != null && partyData.HeroStats.Count > 0;

            for (int i = 0; i < 4; i++)
            {
                VisualElement recruitPortrait = new VisualElement();
                recruitPortrait.AddToClassList("portrait");
                VisualElement expeditionPortrait = new VisualElement();
                expeditionPortrait.AddToClassList("portrait");
                VisualElement healPortrait = new VisualElement();
                healPortrait.AddToClassList("portrait");

                if (hasActiveParty)
                {
                    VisualElement recruitHealthBar = new VisualElement();
                    recruitHealthBar.AddToClassList("health-bar");
                    recruitHealthBar.name = "HealthBar";
                    VisualElement recruitMoraleBar = new VisualElement();
                    recruitMoraleBar.AddToClassList("morale-bar");
                    recruitMoraleBar.name = "MoraleBar";
                    VisualElement recruitBarsContainer = new VisualElement();
                    recruitBarsContainer.AddToClassList("bars-container");
                    recruitBarsContainer.Add(recruitHealthBar);
                    recruitBarsContainer.Add(recruitMoraleBar);
                    recruitPortrait.Add(recruitBarsContainer);

                    VisualElement expeditionHealthBar = new VisualElement();
                    expeditionHealthBar.AddToClassList("health-bar");
                    expeditionHealthBar.name = "HealthBar";
                    VisualElement expeditionMoraleBar = new VisualElement();
                    expeditionMoraleBar.AddToClassList("morale-bar");
                    expeditionMoraleBar.name = "MoraleBar";
                    VisualElement expeditionBarsContainer = new VisualElement();
                    expeditionBarsContainer.AddToClassList("bars-container");
                    expeditionBarsContainer.Add(expeditionHealthBar);
                    expeditionBarsContainer.Add(expeditionMoraleBar);
                    expeditionPortrait.Add(expeditionBarsContainer);

                    VisualElement healHealthBar = new VisualElement();
                    healHealthBar.AddToClassList("health-bar");
                    healHealthBar.name = "HealthBar";
                    VisualElement healMoraleBar = new VisualElement();
                    healMoraleBar.AddToClassList("morale-bar");
                    healMoraleBar.name = "MoraleBar";
                    VisualElement healBarsContainer = new VisualElement();
                    healBarsContainer.AddToClassList("bars-container");
                    healBarsContainer.Add(healHealthBar);
                    healBarsContainer.Add(healMoraleBar);
                    healPortrait.Add(healBarsContainer);
                }

                if (i < heroes.Count && heroes[i] != null)
                {
                    string characterID = heroes[i].Id;
                    if (string.IsNullOrEmpty(characterID))
                    {
                        Debug.LogWarning($"TempleViewController: Hero {i + 1} has null/empty Id, skipping sprite.");
                    }
                    else
                    {
                        Sprite sprite = visualConfig.GetPortrait(characterID);
                        if (sprite != null)
                        {
                            recruitPortrait.style.backgroundImage = new StyleBackground(sprite);
                            expeditionPortrait.style.backgroundImage = new StyleBackground(sprite);
                            healPortrait.style.backgroundImage = new StyleBackground(sprite);
                            string tooltip = $"Health: {heroes[i].Health}/{heroes[i].MaxHealth}, Morale: {heroes[i].Morale}/{heroes[i].MaxMorale}";
                            recruitPortrait.tooltip = tooltip;
                            expeditionPortrait.tooltip = tooltip;
                            healPortrait.tooltip = tooltip;

                            if (hasActiveParty)
                            {
                                float healthPercent = heroes[i].MaxHealth > 0 ? (float)heroes[i].Health / heroes[i].MaxHealth : 0f;
                                float moralePercent = heroes[i].MaxMorale > 0 ? (float)heroes[i].Morale / heroes[i].MaxMorale : 0f;
                                recruitPortrait.Q<VisualElement>("HealthBar").style.width = new StyleLength(Length.Percent(healthPercent * 100));
                                recruitPortrait.Q<VisualElement>("MoraleBar").style.width = new StyleLength(Length.Percent(moralePercent * 100));
                                expeditionPortrait.Q<VisualElement>("HealthBar").style.width = new StyleLength(Length.Percent(healthPercent * 100));
                                expeditionPortrait.Q<VisualElement>("MoraleBar").style.width = new StyleLength(Length.Percent(moralePercent * 100));
                                healPortrait.Q<VisualElement>("HealthBar").style.width = new StyleLength(Length.Percent(healthPercent * 100));
                                healPortrait.Q<VisualElement>("MoraleBar").style.width = new StyleLength(Length.Percent(moralePercent * 100));
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"TempleViewController: No sprite found for '{characterID}' in VisualConfig.");
                        }
                    }
                }

                recruitPortraitContainer.Add(recruitPortrait);
                expeditionPortraitContainer.Add(expeditionPortrait);
                healPortraitContainer.Add(healPortrait);
            }

            generateButton.SetEnabled(false);
            healButton.SetEnabled(partyData.CanHealParty());
            UpdateFavourDisplay();
        }

        private void UpdateFavourDisplay()
        {
            favourLabel.text = $"Favour: {ExpeditionManager.Instance.GetPlayerProgress().Favour}";
        }

        private void HandleExpeditionEnded()
        {
            generateButton.SetEnabled(partyData.HeroStats == null || partyData.HeroStats.Count == 0);
            healButton.SetEnabled(partyData.CanHealParty());
            UpdateFavourDisplay();
        }

        private void HandlePlayerProgressUpdated()
        {
            UpdateFavourDisplay();
        }

        private void SubscribeToEventBus()
        {
            eventBus.OnExpeditionUpdated += UpdateNodeVisuals;
            eventBus.OnVirusSeeded += UpdateVirusNode;
            eventBus.OnPartyUpdated += UpdatePartyVisuals;
            eventBus.OnExpeditionEnded += HandleExpeditionEnded;
            eventBus.OnPlayerProgressUpdated += HandlePlayerProgressUpdated;
        }

        private void UnsubscribeFromEventBus()
        {
            eventBus.OnExpeditionUpdated -= UpdateNodeVisuals;
            eventBus.OnVirusSeeded -= UpdateVirusNode;
            eventBus.OnPartyUpdated -= UpdatePartyVisuals;
            eventBus.OnExpeditionEnded -= HandleExpeditionEnded;
            eventBus.OnPlayerProgressUpdated -= HandlePlayerProgressUpdated;
        }

        private void UpdateNodeVisuals(EventBusSO.ExpeditionGeneratedData data)
        {
            if (!isInitialized || data.expeditionData == null || nodeContainer == null)
            {
                Debug.LogWarning("TempleViewController: ExpeditionData or nodeContainer is null, skipping node update.");
                return;
            }

            nodeDropdown.choices.Clear();
            var nodes = data.expeditionData.NodeData;
            for (int i = 0; i < nodes.Count; i++)
            {
                nodeDropdown.choices.Add($"Node {i + 1} ({nodes[i].NodeType})");
            }
            nodeDropdown.value = nodeDropdown.choices.Count > 0 ? nodeDropdown.choices[0] : null;
            nodeDropdown.style.color = uiConfig.TextColor;
            nodeDropdown.style.unityFont = uiConfig.PixelFont;

            nodeContainer.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                VisualElement nodeBox = new VisualElement();
                nodeBox.AddToClassList("node-box");
                nodeBox.AddToClassList(nodes[i].IsCombat ? "node-combat" : "node-noncombat");
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

            launchButton.SetEnabled(data.expeditionData.IsValid());
        }

        private void UpdateVirusNode(EventBusSO.VirusSeededData data)
        {
            if (!isInitialized || nodeContainer == null) return;

            var nodes = nodeContainer.Children().ToList();
            if (data.nodeIndex >= 0 && data.nodeIndex < nodes.Count())
            {
                var nodeBox = nodes[data.nodeIndex];
                var virus = availableViruses.Find(v => v.VirusID == data.virusID);
                if (virus != null)
                {
                    nodeBox.tooltip = nodeBox.tooltip + (string.IsNullOrEmpty(nodeBox.tooltip) ? "" : ", ") + virus.VirusID;
                }
            }
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || uiConfig == null || visualConfig == null || eventBus == null || availableViruses == null || partyData == null)
            {
                Debug.LogError($"TempleViewController: Missing references! UIDocument: {uiDocument != null}, UIConfig: {uiConfig != null}, " +
                    $"VisualConfig: {visualConfig != null}, EventBus: {eventBus != null}, AvailableViruses: {availableViruses != null}, " +
                    $"PartyData: {partyData != null}");
                return false;
            }
            return true;
        }
    }
}