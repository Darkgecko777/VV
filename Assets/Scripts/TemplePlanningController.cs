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
        [SerializeField] private CombatNodeGenerator combatNodeGenerator;
        [SerializeField] private NonCombatNodeGenerator nonCombatNodeGenerator;
        [SerializeField] private EncounterData combatEncounterData;
        [SerializeField] private bool testMode = true;
        [SerializeField] private List<string> fallbackHeroIds = new List<string> { "Fighter", "Healer", "Ranger", "TreasureHunter" };
        [SerializeField] private CharacterPositions defaultPositions;
        [SerializeField] private EventBusSO eventBus;

        private bool isExpeditionGenerated = false;

        void Awake()
        {
            if (!ValidateReferences()) return;
            eventBus.OnExpeditionGenerated += GenerateExpedition;
            eventBus.OnVirusSeeded += SeedVirus;
            eventBus.OnLaunchExpedition += LaunchExpedition;
        }

        void Start()
        {
            isExpeditionGenerated = expeditionData.IsValid();
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.OnExpeditionGenerated -= GenerateExpedition;
                eventBus.OnVirusSeeded -= SeedVirus;
                eventBus.OnLaunchExpedition -= LaunchExpedition;
            }
        }

        private void GenerateExpedition(EventBusSO.ExpeditionGeneratedData data)
        {
            if (data.expeditionData != null || data.partyData != null) return;

            List<NodeData> nodes = new List<NodeData>();
            nodes.Add(nonCombatNodeGenerator.GenerateNonCombatNode("Swamp", 1, isTempleNode: true));

            int totalNodeCount = testMode ? 8 : Random.Range(0, 3) * 2 + 8;
            int combatNodeCount = totalNodeCount / 2;
            int nonCombatNodeCount = totalNodeCount / 2;
            int level = 2;

            List<NodeData> expeditionNodes = new List<NodeData>();
            for (int i = 0; i < combatNodeCount; i++)
            {
                expeditionNodes.Add(combatNodeGenerator.GenerateCombatNode("Swamp", level, combatEncounterData));
                level++;
            }
            for (int i = 0; i < nonCombatNodeCount; i++)
            {
                expeditionNodes.Add(nonCombatNodeGenerator.GenerateNonCombatNode("Swamp", level));
                level++;
            }

            for (int i = expeditionNodes.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                NodeData temp = expeditionNodes[i];
                expeditionNodes[i] = expeditionNodes[j];
                expeditionNodes[j] = temp;
            }

            nodes.AddRange(expeditionNodes);

            // Initialize party if needed
            if (partyData.HeroStats == null || partyData.HeroStats.Count == 0)
            {
                partyData.HeroIds = new List<string>(fallbackHeroIds);
                partyData.PartyID = System.Guid.NewGuid().ToString();
            }

            // Generate HeroStats with PartyPosition-based placement
            partyData.GenerateHeroStats(defaultPositions.heroPositions);
            foreach (var hero in partyData.HeroStats)
            {
                hero.Health = hero.MaxHealth;
                hero.Morale = hero.MaxMorale;
            }

            if (expeditionData == null)
            {
                Debug.LogError("TemplePlanningController: expeditionData is null in GenerateExpedition!");
                return;
            }
            expeditionData.SetNodes(nodes);
            expeditionData.SetParty(partyData);
            expeditionData.CurrentNodeIndex = 0;
            isExpeditionGenerated = expeditionData.IsValid();

            eventBus.RaisePartyUpdated(partyData);
            eventBus.RaiseExpeditionUpdated(expeditionData, partyData);
        }

        private void LaunchExpedition()
        {
            if (!isExpeditionGenerated || !expeditionData.IsValid())
            {
                Debug.LogWarning("TemplePlanningController: Cannot launch expedition, invalid or not generated!");
                return;
            }
            ExpeditionManager.Instance.TransitionToExpeditionScene();
        }

        private void SeedVirus(EventBusSO.VirusSeededData data)
        {
            if (!isExpeditionGenerated || data.nodeIndex < 0 || data.nodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogWarning($"TemplePlanningController: Invalid virus seeding! Generated: {isExpeditionGenerated}, NodeIndex: {data.nodeIndex}");
                return;
            }

            VirusData virus = availableViruses.Find(v => v.VirusID == data.virusID);
            if (virus == null)
            {
                Debug.LogWarning($"TemplePlanningController: Virus {data.virusID} not found!");
                return;
            }

            expeditionData.NodeData[data.nodeIndex].SeededViruses.Add(virus);
            Debug.Log($"TemplePlanningController: Seeded {virus.VirusID} to Node {data.nodeIndex}");
            eventBus.RaiseExpeditionUpdated(expeditionData, partyData);
        }

        private bool ValidateReferences()
        {
            if (expeditionData == null || partyData == null || availableViruses == null || visualConfig == null ||
                combatNodeGenerator == null || nonCombatNodeGenerator == null || combatEncounterData == null ||
                defaultPositions == null || eventBus == null)
            {
                Debug.LogError($"TemplePlanningController: Missing references! ExpeditionData: {expeditionData != null}, " +
                    $"PartyData: {partyData != null}, AvailableViruses: {availableViruses != null}, " +
                    $"VisualConfig: {visualConfig != null}, CombatNodeGenerator: {combatNodeGenerator != null}, " +
                    $"NonCombatNodeGenerator: {nonCombatNodeGenerator != null}, CombatEncounterData: {combatEncounterData != null}, " +
                    $"DefaultPositions: {defaultPositions != null}, EventBus: {eventBus != null}");
                return false;
            }
            return true;
        }
    }
}