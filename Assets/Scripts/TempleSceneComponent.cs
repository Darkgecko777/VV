using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public class TempleSceneComponent : MonoBehaviour
    {
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private List<VirusSO> availableViruses;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private CombatNodeGenerator combatNodeGenerator;
        [SerializeField] private NonCombatNodeGenerator nonCombatNodeGenerator;
        [SerializeField] private EncounterData combatEncounterData;
        [SerializeField] private CharacterPositions defaultPositions;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private HealingConfig healingConfig;
        [SerializeField] private VirusCraftingComponent virusCraftingComponent;
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
            if (partyData.HeroStats == null || partyData.HeroStats.Count == 0)
            {
                SaveManager.Instance.LoadProgress(expeditionData, partyData, playerProgress);
            }
            eventBus.RaisePartyUpdated(partyData);
            if (ExpeditionManager.Instance != null && ExpeditionManager.Instance.IsReturningFromExpedition)
            {
                Debug.Log("TempleSceneComponent: Returning from expedition, triggering healing popup.");
                eventBus.RaiseTempleEnteredFromExpedition();
                ExpeditionManager.Instance.IsReturningFromExpedition = false;
            }
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
            int totalDifficulty = Random.Range(24, 37);
            List<NodeData> nodes = new List<NodeData>();
            int remainingDifficulty = totalDifficulty;
            bool isCombat = Random.value > 0.5f;
            nodes.Add(nonCombatNodeGenerator.GenerateNonCombatNode("", 1, 0, isTempleNode: true));
            while (remainingDifficulty >= 2 && nodes.Count < 8)
            {
                int rating = Random.Range(3, 7);
                if (remainingDifficulty < rating)
                {
                    if (remainingDifficulty < 2) break;
                    rating = Mathf.Max(2, remainingDifficulty);
                }
                NodeData node;
                if (isCombat)
                {
                    node = combatNodeGenerator.GenerateCombatNode("", 1, combatEncounterData, rating);
                }
                else
                {
                    node = nonCombatNodeGenerator.GenerateNonCombatNode("", 1, rating);
                }
                nodes.Add(node);
                remainingDifficulty -= rating;
                isCombat = !isCombat;
            }
            partyData.Reset();
            partyData.HeroIds = new List<string>();
            var positionMap = new Dictionary<int, string>
            {
                { 1, "Fighter" },
                { 2, "Monk" },
                { 3, "Scout" },
                { 4, "Healer" }
            };
            foreach (var pos in positionMap.Keys)
            {
                string selectedHero = positionMap[pos];
                if (playerProgress.UnlockedHeroes.Contains(selectedHero))
                {
                    partyData.HeroIds.Add(selectedHero);
                }
                else
                {
                    Debug.LogWarning($"TempleSceneComponent: Hero {selectedHero} not unlocked, skipping position {pos}");
                }
            }
            partyData.PartyID = System.Guid.NewGuid().ToString();
            partyData.GenerateHeroStats(defaultPositions.heroPositions);
            foreach (var hero in partyData.HeroStats)
            {
                hero.Health = hero.MaxHealth;
                hero.Morale = hero.MaxMorale;
            }
            if (expeditionData == null)
            {
                Debug.LogError("TempleSceneComponent: expeditionData is null in GenerateExpedition!");
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
                Debug.LogWarning("TempleSceneComponent: Cannot launch expedition, invalid or not generated!");
                return;
            }
            ExpeditionManager.Instance.TransitionToExpeditionScene();
        }

        private void SeedVirus(EventBusSO.VirusSeededData data)
        {
            if (!isExpeditionGenerated || data.nodeIndex < 0 || data.nodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogWarning($"TempleSceneComponent: Invalid virus seeding! Generated: {isExpeditionGenerated}, NodeIndex: {data.nodeIndex}");
                return;
            }
            VirusSO virus = availableViruses.Find(v => v.VirusID == data.virusID);
            if (virus == null)
            {
                Debug.LogWarning($"TempleSceneComponent: Virus {data.virusID} not found!");
                return;
            }
            expeditionData.NodeData[data.nodeIndex].SeededViruses.Add(virus);
            Debug.Log($"TempleSceneComponent: Seeded {virus.VirusID} to Node {data.nodeIndex}");
            eventBus.RaiseExpeditionUpdated(expeditionData, partyData);
        }

        private bool ValidateReferences()
        {
            if (expeditionData == null || partyData == null || playerProgress == null || availableViruses == null || visualConfig == null ||
                combatNodeGenerator == null || nonCombatNodeGenerator == null || combatEncounterData == null ||
                defaultPositions == null || eventBus == null || healingConfig == null || virusCraftingComponent == null)
            {
                Debug.LogError($"TempleSceneComponent: Missing references! ExpeditionData: {expeditionData != null}, " +
                    $"PartyData: {partyData != null}, PlayerProgress: {playerProgress != null}, AvailableViruses: {availableViruses != null}, " +
                    $"VisualConfig: {visualConfig != null}, CombatNodeGenerator: {combatNodeGenerator != null}, " +
                    $"NonCombatNodeGenerator: {nonCombatNodeGenerator != null}, CombatEncounterData: {combatEncounterData != null}, " +
                    $"DefaultPositions: {defaultPositions != null}, EventBus: {eventBus != null}, HealingConfig: {healingConfig != null}, " +
                    $"VirusCraftingComponent: {virusCraftingComponent != null}");
                return false;
            }
            return true;
        }
    }
}