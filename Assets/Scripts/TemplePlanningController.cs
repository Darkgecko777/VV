using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public class TemplePlanningController : MonoBehaviour
    {
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private List<VirusData> availableViruses;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private TempleVisualController visualController;
        [SerializeField] private TempleUIController uiController;
        [SerializeField] private CombatNodeGenerator combatNodeGenerator;
        [SerializeField] private NonCombatNodeGenerator nonCombatNodeGenerator;
        [SerializeField] private EncounterData combatEncounterData;
        [SerializeField] private bool testMode = true; // For testing: true = 3 nodes, false = 8-12 nodes
        [SerializeField] private List<string> fallbackHeroIds = new List<string> { "Fighter", "Healer", "Scout", "TreasureHunter" };
        [SerializeField] private CharacterPositions defaultPositions;

        private bool isExpeditionGenerated = false;

        void Awake()
        {
            if (!ValidateReferences()) return;
            uiController.OnGenerateClicked += GenerateExpedition;
            uiController.OnLaunchClicked += LaunchExpedition;
            uiController.OnSeedVirusClicked += SeedVirus;
        }

        void Start()
        {
            uiController.InitializeUI(availableViruses, expeditionData);
            visualController.InitializeEmptyPortraits();
            UpdateLaunchButtonState();
        }

        private bool ValidateReferences()
        {
            if (expeditionData == null || partyData == null || availableViruses == null || visualConfig == null ||
                visualController == null || uiController == null || combatNodeGenerator == null ||
                nonCombatNodeGenerator == null || combatEncounterData == null || defaultPositions == null)
            {
                Debug.LogError($"TemplePlanningController: Missing references! ExpeditionData: {expeditionData != null}, " +
                    $"PartyData: {partyData != null}, AvailableViruses: {availableViruses != null}, " +
                    $"VisualConfig: {visualConfig != null}, VisualController: {visualController != null}, " +
                    $"UIController: {uiController != null}, CombatNodeGenerator: {combatNodeGenerator != null}, " +
                    $"NonCombatNodeGenerator: {nonCombatNodeGenerator != null}, CombatEncounterData: {combatEncounterData != null}, " +
                    $"DefaultPositions: {defaultPositions != null}");
                return false;
            }
            return true;
        }

        public void GenerateExpedition()
        {
            if (isExpeditionGenerated)
            {
                Debug.LogWarning("TemplePlanningController: Expedition already generated!");
                return;
            }

            string[] biomes = { "Swamp", "Ruin", "HauntedForest" };
            int nodeCount = testMode ? 3 : Random.Range(8, 13);
            int combatNodes = Mathf.FloorToInt(nodeCount * 0.5f);
            // NEW: Ensure at least one non-combat node for alternation
            int nonCombatNodes = nodeCount - combatNodes - 1; // -1 for Temple node
            List<NodeData> nodes = new List<NodeData>();
            int level = 1;

            // Add Temple node first
            nodes.Add(new NodeData(null, "Temple", "Temple", false, "The Temple of Cleansing"));

            // NEW: Alternate non-combat and combat nodes
            int pairs = Mathf.Min(nonCombatNodes, combatNodes); // Number of alternating pairs
            for (int i = 0; i < pairs; i++)
            {
                string biome = biomes[Random.Range(0, biomes.Length)];
                nodes.Add(nonCombatNodeGenerator.GenerateNonCombatNode(biome, level));
                level++;
                biome = biomes[Random.Range(0, biomes.Length)];
                nodes.Add(combatNodeGenerator.GenerateCombatNode(biome, level, combatEncounterData));
                level++;
            }

            // NEW: Add remaining non-combat nodes if any
            for (int i = pairs; i < nonCombatNodes; i++)
            {
                string biome = biomes[Random.Range(0, biomes.Length)];
                nodes.Add(nonCombatNodeGenerator.GenerateNonCombatNode(biome, level));
                level++;
            }

            // NEW: Add remaining combat nodes if any
            for (int i = pairs; i < combatNodes; i++)
            {
                string biome = biomes[Random.Range(0, biomes.Length)];
                nodes.Add(combatNodeGenerator.GenerateCombatNode(biome, level, combatEncounterData));
                level++;
            }

            // Generate party if none exists or user didn't select heroes
            if (partyData.HeroStats == null || partyData.HeroStats.Count == 0)
            {
                partyData.HeroIds = new List<string>(fallbackHeroIds);
                partyData.GenerateHeroStats(defaultPositions.heroPositions);
                partyData.PartyID = System.Guid.NewGuid().ToString();
            }

            ExpeditionManager.Instance.SetExpedition(nodes, partyData);
            isExpeditionGenerated = expeditionData.IsValid();

            visualController.UpdatePartyVisuals(partyData);
            visualController.UpdateNodeVisuals(expeditionData);
            uiController.UpdateNodeDropdown(expeditionData);
            UpdateLaunchButtonState();
        }

        public void LaunchExpedition()
        {
            if (!isExpeditionGenerated || !expeditionData.IsValid())
            {
                Debug.LogWarning("TemplePlanningController: Cannot launch expedition, invalid or not generated!");
                return;
            }
            ExpeditionManager.Instance.TransitionToExpeditionScene();
        }

        public void SeedVirus(string virusID, int nodeIndex)
        {
            if (!isExpeditionGenerated || nodeIndex < 0 || nodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogWarning($"TemplePlanningController: Invalid virus seeding! Generated: {isExpeditionGenerated}, NodeIndex: {nodeIndex}");
                return;
            }

            VirusData virus = availableViruses.Find(v => v.VirusID == virusID);
            if (virus == null)
            {
                Debug.LogWarning($"TemplePlanningController: Virus {virusID} not found!");
                return;
            }

            expeditionData.NodeData[nodeIndex].SeededViruses.Add(virus);
            Debug.Log($"TemplePlanningController: Seeded {virus.VirusID} to Node {nodeIndex}");
            visualController.UpdateNodeVisuals(expeditionData);
        }

        private void UpdateLaunchButtonState()
        {
            uiController.SetLaunchButtonEnabled(isExpeditionGenerated && expeditionData.IsValid());
        }
    }
}