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
        [SerializeField] private List<string> fallbackHeroIds = new List<string> { "Fighter", "Healer", "Scout", "TreasureHunter" };
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

        private void GenerateExpedition(EventBusSO.ExpeditionGeneratedData _)
        {
            List<NodeData> nodes = new List<NodeData>();
            int nodeCount = testMode ? 3 : Random.Range(8, 13);
            string[] biomes = { "Ruins", "Swamp", "Forest" };
            int level = 1;

            for (int i = 0; i < nodeCount; i++)
            {
                string biome = biomes[Random.Range(0, biomes.Length)];
                if (Random.Range(0, 2) == 0)
                {
                    nodes.Add(combatNodeGenerator.GenerateCombatNode(biome, level, combatEncounterData));
                }
                else
                {
                    nodes.Add(nonCombatNodeGenerator.GenerateNonCombatNode(biome, level));
                }
                level++;
            }

            if (partyData.HeroStats == null || partyData.HeroStats.Count == 0)
            {
                partyData.HeroIds = new List<string>(fallbackHeroIds);
                partyData.GenerateHeroStats(defaultPositions.heroPositions);
                partyData.PartyID = System.Guid.NewGuid().ToString();
            }

            foreach (var hero in partyData.HeroStats)
            {
                hero.Health = hero.MaxHealth;
                hero.Morale = hero.MaxMorale;
            }

            ExpeditionManager.Instance.SetExpedition(nodes, partyData);
            isExpeditionGenerated = expeditionData.IsValid();

            eventBus.RaisePartyUpdated(partyData);
            eventBus.RaiseExpeditionGenerated(expeditionData, partyData);
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
            eventBus.RaiseExpeditionGenerated(expeditionData, partyData);
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