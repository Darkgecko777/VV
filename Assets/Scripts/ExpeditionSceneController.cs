using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class ExpeditionSceneController : MonoBehaviour
    {
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private List<string> fallbackHeroIds = new List<string> { "Fighter", "Healer", "Scout", "TreasureHunter" };
        [SerializeField] private CharacterPositions defaultPositions;
        [SerializeField] private EncounterData combatEncounterData;

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
            partyData.AllowCultist = false; // Prototype

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
            // Existing code
        }

        private void HandleContinueClicked()
        {
            ExpeditionManager.Instance.OnContinueClicked();
        }

        private bool ValidateReferences()
        {
            // Add partyData, fallbackHeroIds, defaultPositions, combatEncounterData to check
            if (eventBus == null || expeditionData == null || partyData == null || defaultPositions == null || combatEncounterData == null)
            {
                return false;
            }
            return true;
        }
    }
}