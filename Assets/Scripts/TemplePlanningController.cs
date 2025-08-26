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
                nonCombatNodeGenerator == null || combatEncounterData == null)
            {
                Debug.LogError($"TemplePlanningController: Missing references! ExpeditionData: {expeditionData != null}, " +
                    $"PartyData: {partyData != null}, AvailableViruses: {availableViruses != null}, " +
                    $"VisualConfig: {visualConfig != null}, VisualController: {visualController != null}, " +
                    $"UIController: {uiController != null}, CombatNodeGenerator: {combatNodeGenerator != null}, " +
                    $"NonCombatNodeGenerator: {nonCombatNodeGenerator != null}, CombatEncounterData: {combatEncounterData != null}");
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

            // Generate 8-12 nodes (per vision doc)
            List<NodeData> nodes = new List<NodeData>();
            nodes.Add(new NodeData(new List<MonsterStats>(), "Temple", "Temple", false, "")); // Starting Temple node
            int totalNodes = Random.Range(8, 13); // 8-12 nodes
            int combatNodes = Mathf.FloorToInt(totalNodes * 0.4f); // ~40% combat
            int nonCombatNodes = totalNodes - combatNodes - 1; // -1 for Temple
            string[] biomes = { "Swamp", "Ruins", "HauntedForest" }; // From vision doc
            int level = 1; // Starting difficulty

            // Generate non-combat nodes
            for (int i = 0; i < nonCombatNodes; i++)
            {
                string biome = biomes[Random.Range(0, biomes.Length)];
                nodes.Add(nonCombatNodeGenerator.GenerateNonCombatNode(biome, level));
                level++; // Incremental difficulty
            }

            // Generate combat nodes
            for (int i = 0; i < combatNodes; i++)
            {
                string biome = biomes[Random.Range(0, biomes.Length)];
                nodes.Add(combatNodeGenerator.GenerateCombatNode(biome, level, combatEncounterData));
                level++;
            }

            // Shuffle nodes (except Temple at start)
            var shuffledNodes = new List<NodeData> { nodes[0] }; // Keep Temple first
            var otherNodes = nodes.Skip(1).OrderBy(_ => Random.value).ToList();
            shuffledNodes.AddRange(otherNodes);

            // Pass nodes to ExpeditionManager
            ExpeditionManager.Instance.GenerateExpedition(shuffledNodes);
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