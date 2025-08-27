using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace VirulentVentures
{
    public class TempleVisualController : MonoBehaviour
    {
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIDocument uiDocument;

        private VisualElement portraitContainer;
        private VisualElement nodeContainer;

        void Awake()
        {
            if (!ValidateReferences()) return;
            portraitContainer = uiDocument.rootVisualElement.Q<VisualElement>("PortraitContainer");
            nodeContainer = uiDocument.rootVisualElement.Q<VisualElement>("NodeContainer");
        }

        private bool ValidateReferences()
        {
            return visualConfig != null && uiDocument != null;
        }

        public void InitializeEmptyPortraits()
        {
            if (portraitContainer == null)
            {
                return;
            }

            portraitContainer.Clear();
            for (int i = 0; i < 4; i++)
            {
                VisualElement portrait = new VisualElement();
                portrait.AddToClassList("portrait");
                portraitContainer.Add(portrait);
            }
        }

        public void UpdatePartyVisuals(PartyData partyData)
        {
            if (partyData == null || portraitContainer == null)
            {
                Debug.LogWarning("TempleVisualController: PartyData or portraitContainer is null, skipping update.");
                return;
            }

            portraitContainer.Clear();
            var heroes = partyData.GetHeroes()
                .OrderByDescending(h => h.PartyPosition)
                .ToList();

            for (int i = 0; i < 4; i++)
            {
                VisualElement portrait = new VisualElement();
                portrait.AddToClassList("portrait");
                if (i < heroes.Count && heroes[i] != null)
                {
                    string characterID = heroes[i].HeroId; // Use public getter
                    if (string.IsNullOrEmpty(characterID))
                    {
                        Debug.LogWarning($"TempleVisualController: Hero {i + 1} has null/empty HeroId, skipping sprite.");
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
                            Debug.LogWarning($"TempleVisualController: No sprite found for '{characterID}' in VisualConfig.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"TempleVisualController: Hero {i + 1} is null or invalid.");
                }
                portraitContainer.Add(portrait);
            }
        }

        public void UpdateNodeVisuals(ExpeditionData expeditionData)
        {
            if (expeditionData == null || nodeContainer == null)
            {
                return;
            }

            nodeContainer.Clear();
            var nodes = expeditionData.NodeData;
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
        }
    }
}