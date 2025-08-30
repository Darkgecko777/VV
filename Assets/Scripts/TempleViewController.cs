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

        private DropdownField virusDropdown;
        private DropdownField nodeDropdown;
        private Button generateButton;
        private Button launchButton;
        private Button seedVirusButton;
        private VisualElement portraitContainer;
        private VisualElement nodeContainer;
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
            portraitContainer = root.Q<VisualElement>("PortraitContainer");
            nodeContainer = root.Q<VisualElement>("NodeContainer");

            if (virusDropdown == null || nodeDropdown == null || generateButton == null || launchButton == null || seedVirusButton == null || portraitContainer == null || nodeContainer == null)
            {
                Debug.LogError($"TempleViewController: Missing UI elements! VirusDropdown: {virusDropdown != null}, NodeDropdown: {nodeDropdown != null}, GenerateButton: {generateButton != null}, LaunchButton: {launchButton != null}, SeedVirusButton: {seedVirusButton != null}, PortraitContainer: {portraitContainer != null}, NodeContainer: {nodeContainer != null}");
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
                root.Clear(); // Explicitly clear UI Toolkit elements
            }
            virusDropdown = null;
            nodeDropdown = null;
            generateButton = null;
            launchButton = null;
            seedVirusButton = null;
            portraitContainer = null;
            nodeContainer = null;
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

            launchButton.SetEnabled(false);
        }

        private void InitializeEmptyPortraits()
        {
            portraitContainer.Clear();
            for (int i = 0; i < 4; i++)
            {
                VisualElement portrait = new VisualElement();
                portrait.AddToClassList("portrait");
                portraitContainer.Add(portrait);
            }
        }

        private void UpdatePartyVisuals(PartyData partyData)
        {
            if (!isInitialized || partyData == null || portraitContainer == null)
            {
                Debug.LogWarning("TempleViewController: PartyData or portraitContainer is null, skipping update.");
                return;
            }

            portraitContainer.Clear();
            var heroes = partyData.GetHeroes().OrderByDescending(h => h.PartyPosition).ToList();

            for (int i = 0; i < 4; i++)
            {
                VisualElement portrait = new VisualElement();
                portrait.AddToClassList("portrait");
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
                            portrait.style.backgroundImage = new StyleBackground(sprite);
                            portrait.tooltip = $"Health: {heroes[i].Health}, ATK: {heroes[i].Attack}, DEF: {heroes[i].Defense}, Morale: {heroes[i].Morale}";
                        }
                        else
                        {
                            Debug.LogWarning($"TempleViewController: No sprite found for '{characterID}' in VisualConfig.");
                        }
                    }
                }
                portraitContainer.Add(portrait);
            }
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

        private void SubscribeToEventBus()
        {
            eventBus.OnExpeditionUpdated += UpdateNodeVisuals;
            eventBus.OnVirusSeeded += UpdateVirusNode;
            eventBus.OnPartyUpdated += UpdatePartyVisuals;
        }

        private void UnsubscribeFromEventBus()
        {
            eventBus.OnExpeditionUpdated -= UpdateNodeVisuals;
            eventBus.OnVirusSeeded -= UpdateVirusNode;
            eventBus.OnPartyUpdated -= UpdatePartyVisuals;
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || uiConfig == null || visualConfig == null || eventBus == null || availableViruses == null)
            {
                Debug.LogError($"TempleViewController: Missing references! UIDocument: {uiDocument != null}, UIConfig: {uiConfig != null}, VisualConfig: {visualConfig != null}, EventBus: {eventBus != null}, AvailableViruses: {availableViruses != null}");
                return false;
            }
            return true;
        }
    }
}