using UnityEngine;
using UnityEngine.UIElements;

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
            if (visualConfig == null || uiDocument == null)
            {
                Debug.LogError($"TempleVisualController: Missing references! VisualConfig: {visualConfig != null}, UIDocument: {uiDocument != null}");
                return false;
            }
            return true;
        }

        public void UpdatePartyVisuals(PartyData partyData)
        {
            if (partyData == null || portraitContainer == null)
            {
                Debug.LogWarning($"TempleVisualController: Invalid partyData or portraitContainer!");
                return;
            }

            portraitContainer.Clear();
            var heroes = partyData.GetHeroes();
            for (int i = 0; i < 4; i++)
            {
                VisualElement portrait = new VisualElement();
                portrait.AddToClassList("portrait");
                if (i < heroes.Count && heroes[i] != null && heroes[i].SO is HeroSO heroSO && heroSO.Stats != null)
                {
                    string characterID = heroSO.Stats.Type?.Id;
                    if (string.IsNullOrEmpty(characterID))
                    {
                        Debug.LogWarning($"TempleVisualController: Missing Type.Id for hero at index {i}");
                        portraitContainer.Add(portrait);
                        continue;
                    }
                    Sprite sprite = visualConfig.GetPortrait(characterID, heroes[i].Rank);
                    if (sprite != null)
                    {
                        portrait.style.backgroundImage = new StyleBackground(sprite);
                        portrait.tooltip = $"Health: {heroes[i].Health}, ATK: {heroes[i].Attack}, DEF: {heroes[i].Defense}, Morale: {heroes[i].Morale}";
                    }
                    else
                    {
                        Debug.LogWarning($"TempleVisualController: No portrait found for characterID {characterID}_Rank{heroes[i].Rank}");
                    }
                }
                portraitContainer.Add(portrait);
            }
        }

        public void UpdateNodeVisuals(ExpeditionData expeditionData)
        {
            if (expeditionData == null || nodeContainer == null)
            {
                Debug.LogWarning($"TempleVisualController: Invalid expeditionData or nodeContainer!");
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