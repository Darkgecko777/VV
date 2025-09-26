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
        [SerializeField] private List<VirusData> availableViruses;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private CombatNodeGenerator combatNodeGenerator;
        [SerializeField] private NonCombatNodeGenerator nonCombatNodeGenerator;
        [SerializeField] private EncounterData combatEncounterData;
        [SerializeField] private bool testMode = true;
        [SerializeField] private CharacterPositions defaultPositions;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private HealingConfig healingConfig;
        private bool isExpeditionGenerated = false;

        void Awake()
        {
            if (!ValidateReferences()) return;
            eventBus.OnExpeditionGenerated += GenerateExpedition;
            eventBus.OnVirusSeeded += SeedVirus;
            eventBus.OnLaunchExpedition += LaunchExpedition;
            eventBus.OnTempleEnteredFromExpedition += AutoHealParty;
        }

        void Start()
        {
            isExpeditionGenerated = expeditionData.IsValid();
            if (partyData.HeroStats == null || partyData.HeroStats.Count == 0)
            {
                Debug.Log("TempleSceneComponent: HeroStats is empty on Start, attempting to load from SaveManager");
                SaveManager.Instance.LoadProgress(expeditionData, partyData, playerProgress);
                if (partyData.HeroStats == null || partyData.HeroStats.Count == 0)
                {
                    Debug.LogWarning("TempleSceneComponent: HeroStats still empty after load, may need to generate new party");
                }
                else
                {
                    Debug.Log($"TempleSceneComponent: Loaded {partyData.HeroStats.Count} heroes: {string.Join(", ", partyData.HeroStats.Select(h => h.Id))}");
                }
            }
            eventBus.RaisePartyUpdated(partyData);
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.OnExpeditionGenerated -= GenerateExpedition;
                eventBus.OnVirusSeeded -= SeedVirus;
                eventBus.OnLaunchExpedition -= LaunchExpedition;
                eventBus.OnTempleEnteredFromExpedition -= AutoHealParty;
            }
        }

        private void AutoHealParty()
        {
            if (partyData == null || partyData.HeroStats == null || partyData.HeroStats.Count == 0)
            {
                Debug.LogWarning("TempleSceneComponent: No party to heal!");
                return;
            }
            int totalFavour = 0;
            foreach (var hero in partyData.HeroStats)
            {
                if (hero.Health <= 0 || hero.HasRetreated) continue;
                int hpHealed = hero.MaxHealth - hero.Health;
                int moraleRestored = hero.MaxMorale - hero.Morale;
                float favour = (healingConfig.HPFavourPerPoint * hpHealed) + (healingConfig.MoraleFavourPerPoint * moraleRestored);
                hero.Health = hero.MaxHealth;
                hero.Morale = hero.MaxMorale;
                totalFavour += Mathf.RoundToInt(favour);
            }
            if (totalFavour > 0)
            {
                playerProgress.AddFavour(totalFavour);
                Debug.Log($"TempleSceneComponent: Auto-healed party, earned {totalFavour} favour");
                eventBus.RaisePlayerProgressUpdated();
            }
            eventBus.RaisePartyUpdated(partyData);
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
            for (int i = 0; i < combatNodeCount; i++)
            {
                nodes.Add(combatNodeGenerator.GenerateCombatNode("Swamp", level, combatEncounterData));
            }
            for (int i = 0; i < nonCombatNodeCount; i++)
            {
                nodes.Add(nonCombatNodeGenerator.GenerateNonCombatNode("Swamp", level));
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
            VirusData virus = availableViruses.Find(v => v.VirusID == data.virusID);
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
                defaultPositions == null || eventBus == null || healingConfig == null)
            {
                Debug.LogError($"TempleSceneComponent: Missing references! ExpeditionData: {expeditionData != null}, " +
                    $"PartyData: {partyData != null}, PlayerProgress: {playerProgress != null}, AvailableViruses: {availableViruses != null}, " +
                    $"VisualConfig: {visualConfig != null}, CombatNodeGenerator: {combatNodeGenerator != null}, " +
                    $"NonCombatNodeGenerator: {nonCombatNodeGenerator != null}, CombatEncounterData: {combatEncounterData != null}, " +
                    $"DefaultPositions: {defaultPositions != null}, EventBus: {eventBus != null}, HealingConfig: {healingConfig != null}");
                return false;
            }
            return true;
        }
    }
}