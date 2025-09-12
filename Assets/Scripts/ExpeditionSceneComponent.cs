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
            eventBus.OnUnitDied += HandleUnitDied;
            eventBus.OnCombatEnded += HandleCombatEnded;

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
                eventBus.OnUnitDied -= HandleUnitDied;
                eventBus.OnCombatEnded -= HandleCombatEnded;
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
            Debug.Log($"ExpeditionSceneComponent: Processing node {data.currentIndex}, IsCombat: {currentNode.IsCombat}, Completed: {currentNode.Completed}");
            if (currentNode.IsCombat && !currentNode.Completed)
            {
                Debug.Log("ExpeditionSceneComponent: Attempting combat scene transition");
                if (viewComponent != null)
                {
                    viewComponent.FadeToCombat(() => ExpeditionManager.Instance.TransitionToCombatScene());
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
                CheckExpeditionFailure();
            }
        }

        private void HandleContinueClicked()
        {
            var expedition = ExpeditionManager.Instance.GetExpedition();
            if (expedition == null || expedition.NodeData == null) return;

            if (partyData.HeroStats.All(h => h.HasRetreated || h.Health <= 0) || expedition.CurrentNodeIndex >= expedition.NodeData.Count - 1)
            {
                Debug.Log("ExpeditionSceneComponent: Expedition failed or completed, transitioning to Temple");
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
            }
            else
            {
                expedition.CurrentNodeIndex++;
                eventBus.RaiseNodeUpdated(expedition.NodeData, expedition.CurrentNodeIndex);
                ExpeditionManager.Instance.SaveProgress();
            }
        }

        private void HandleUnitDied(ICombatUnit unit)
        {
            if (unit is CharacterStats stats && stats.Type == CharacterType.Hero)
            {
                CheckExpeditionFailure();
            }
        }

        private void HandleCombatEnded()
        {
            CheckExpeditionFailure();
        }

        private void CheckExpeditionFailure()
        {
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