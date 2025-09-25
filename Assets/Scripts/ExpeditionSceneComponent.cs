using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class ExpeditionSceneComponent : MonoBehaviour
    {
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private List<string> fallbackHeroIds = new List<string> { "Fighter", "Healer", "Scout", "TreasureHunter" };
        [SerializeField] private CharacterPositions defaultPositions;
        [SerializeField] private EncounterData combatEncounterData;
        [SerializeField] private ExpeditionUIComponent viewComponent;

        void Awake()
        {
            if (!ValidateReferences()) return;
        }

        void Start()
        {
            eventBus.OnNodeUpdated += HandleNodeUpdate;
            eventBus.OnSceneTransitionCompleted += HandleNodeUpdate;
            eventBus.OnContinueClicked += HandleContinueClicked;

            var expedition = ExpeditionManager.Instance.GetExpedition();
            if (!expedition.IsValid())
            {
                GenerateExpedition();
            }
            HandleNodeUpdate(new EventBusSO.NodeUpdateData { nodes = expedition.NodeData, currentIndex = expedition.CurrentNodeIndex });
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.OnNodeUpdated -= HandleNodeUpdate;
                eventBus.OnSceneTransitionCompleted -= HandleNodeUpdate;
                eventBus.OnContinueClicked -= HandleContinueClicked;
            }
        }

        private void GenerateExpedition()
        {
            partyData.Reset();
            partyData.HeroIds = fallbackHeroIds;
            partyData.GenerateHeroStats(defaultPositions.heroPositions);
            partyData.AllowCultist = false;

            var nodes = new List<NodeData>();
            var nonCombatGenerator = gameObject.AddComponent<NonCombatNodeGenerator>();
            var combatGenerator = gameObject.AddComponent<CombatNodeGenerator>();
            var nonCombatNode = nonCombatGenerator.GenerateNonCombatNode("Swamp", 1);
            var combatNode = combatGenerator.GenerateCombatNode("Swamp", 1, combatEncounterData);
            nodes.Add(nonCombatNode);
            nodes.Add(combatNode);
            Destroy(nonCombatGenerator);
            Destroy(combatGenerator);

            expeditionData.SetNodes(nodes);
            expeditionData.CurrentNodeIndex = 0;
            expeditionData.SetParty(partyData);

            eventBus.RaiseExpeditionGenerated(expeditionData, partyData);
            eventBus.RaisePartyUpdated(partyData);
            eventBus.RaiseNodeUpdated(nodes, 0);
        }

        private void HandleNodeUpdate(EventBusSO.NodeUpdateData data)
        {
            if (data.nodes == null || data.currentIndex < 0 || data.currentIndex >= data.nodes.Count)
            {
                Debug.LogError("ExpeditionSceneComponent: Invalid node data or index!");
                return;
            }

            var currentNode = data.nodes[data.currentIndex];
            if (currentNode.IsCombat && !currentNode.Completed)
            {
                if (viewComponent != null)
                {
                    viewComponent.FadeToCombat(() => {
                        var asyncOp = ExpeditionManager.Instance.TransitionToCombatScene();
                    });
                }
                else
                {
                    ExpeditionManager.Instance.TransitionToCombatScene();
                }
            }
            else
            {
                eventBus.RaiseLogMessage(currentNode.Completed ? "Combat Won!" : currentNode.FlavourText, Color.white);
                eventBus.RaisePartyUpdated(partyData);
                // Check for hero deaths after node processing
                if (CheckForHeroDeaths())
                {
                    Debug.Log($"ExpeditionSceneComponent: Hero death detected after node {data.currentIndex}, will transition to Temple on Continue");
                    return;
                }
                CheckExpeditionFailure();
            }
        }

        private void HandleContinueClicked()
        {
            var expedition = ExpeditionManager.Instance.GetExpedition();
            if (expedition == null || expedition.NodeData == null)
            {
                Debug.LogError("ExpeditionSceneComponent: Expedition or NodeData is null in HandleContinueClicked!");
                return;
            }

            // Check for hero deaths before advancing
            if (CheckForHeroDeaths())
            {
                Debug.Log("ExpeditionSceneComponent: Hero death detected on Continue, transitioning to Temple");
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
                return;
            }

            if (expedition.CurrentNodeIndex >= expedition.NodeData.Count - 1)
            {
                Debug.Log("ExpeditionSceneComponent: Expedition completed, transitioning to Temple");
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
            }
            else
            {
                expedition.CurrentNodeIndex++;
                eventBus.RaiseNodeUpdated(expedition.NodeData, expedition.CurrentNodeIndex);
                ExpeditionManager.Instance.SaveProgress();
            }
        }

        private bool CheckForHeroDeaths()
        {
            if (partyData.HeroStats == null || partyData.HeroStats.Count == 0)
            {
                Debug.LogWarning("ExpeditionSceneComponent: HeroStats is null or empty in CheckForHeroDeaths");
                return false;
            }
            bool hasDeadHero = partyData.HeroStats.Any(h => h.Type == CharacterType.Hero && h.Health <= 0);
            if (hasDeadHero)
            {
                Debug.Log($"ExpeditionSceneComponent: Found dead hero(s): {string.Join(", ", partyData.HeroStats.Where(h => h.Type == CharacterType.Hero && h.Health <= 0).Select(h => h.Id))}");
            }
            return hasDeadHero;
        }

        private void CheckExpeditionFailure()
        {
            if (partyData.HeroStats == null || partyData.HeroStats.Count == 0)
            {
                Debug.LogWarning("ExpeditionSceneComponent: HeroStats is null or empty in CheckExpeditionFailure, transitioning to Temple");
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
                return;
            }
            if (partyData.HeroStats.All(h => h.HasRetreated || h.Health <= 0))
            {
                Debug.Log("ExpeditionSceneComponent: All heroes dead or retreated, transitioning to Temple");
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
            }
        }

        private bool ValidateReferences()
        {
            if (eventBus == null || expeditionData == null || partyData == null || defaultPositions == null || combatEncounterData == null || viewComponent == null)
            {
                Debug.LogError($"ExpeditionSceneComponent: Missing references! EventBus: {eventBus != null}, ExpeditionData: {expeditionData != null}, PartyData: {partyData != null}, DefaultPositions: {defaultPositions != null}, CombatEncounterData: {combatEncounterData != null}, ViewComponent: {viewComponent != null}");
                return false;
            }
            return true;
        }
    }
}