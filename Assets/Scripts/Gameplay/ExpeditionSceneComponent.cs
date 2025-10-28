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
        [SerializeField] private List<string> fallbackHeroIds = new List<string> { "Fighter", "Monk", "Scout", "Healer" };
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
                GenerateExpedition();

            HandleNodeUpdate(new EventBusSO.NodeUpdateData
            {
                nodes = expedition.NodeData,
                currentIndex = expedition.CurrentNodeIndex
            });
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
            partyData.PartyID = System.Guid.NewGuid().ToString();
            partyData.GenerateHeroStats(defaultPositions.heroPositions);
            foreach (var h in partyData.HeroStats)
            {
                h.Health = h.MaxHealth;
                h.Morale = h.MaxMorale;
            }

            int totalDifficulty = Random.Range(24, 37);
            List<NodeData> nodes = new List<NodeData>();
            int remainingDifficulty = totalDifficulty;
            bool isCombat = Random.value > 0.5f;

            var nonCombatGen = gameObject.AddComponent<NonCombatNodeGenerator>();
            nonCombatGen.InitializeCache();                     // ensure cache is ready
            nodes.Add(nonCombatGen.GenerateNonCombatNode("", 1, 0, isTempleNode: true));

            var combatGen = gameObject.AddComponent<CombatNodeGenerator>();
            while (remainingDifficulty >= 2 && nodes.Count < 8)
            {
                int rating = Random.Range(3, 7);
                if (remainingDifficulty < rating)
                    rating = Mathf.Max(2, remainingDifficulty);

                NodeData node = isCombat
                    ? combatGen.GenerateCombatNode("", 1, combatEncounterData, rating)
                    : nonCombatGen.GenerateNonCombatNode("", 1, rating);

                nodes.Add(node);
                remainingDifficulty -= rating;
                isCombat = !isCombat;
            }
            Destroy(nonCombatGen);
            Destroy(combatGen);

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
                return;

            var node = data.nodes[data.currentIndex];

            if (node.IsCombat && !node.Completed)
            {
                viewComponent?.SetContinueButtonEnabled(false);
                viewComponent?.FadeToCombat(() => ExpeditionManager.Instance.TransitionToCombatScene());
                return;
            }

            viewComponent?.SetContinueButtonEnabled(true);

            // === FIX: Skip temple node (index 0) ===
            if (data.currentIndex == 0) // Temple node
            {
                // Show flavour text only
                eventBus.RaiseLogMessage(node.FlavourText, Color.white);
                return;
            }

            if (!node.IsCombat && !node.Completed)
            {
                if (node.NonCombatEncounter == null)
                {
                    Debug.LogError($"Non-combat node {data.currentIndex} has no encounter SO!");
                    return;
                }
                eventBus.RaiseNonCombatEncounter(node.NonCombatEncounter, node);
                return;
            }

            eventBus.RaiseLogMessage(node.Completed ? "Combat Won!" : node.FlavourText, Color.white);
            eventBus.RaisePartyUpdated(partyData);

            if (CheckForHeroDeaths()) return;
            CheckExpeditionFailure();
        }

        private void HandleContinueClicked()
        {
            var expedition = ExpeditionManager.Instance.GetExpedition();
            if (expedition == null || expedition.NodeData == null) return;

            if (CheckForHeroDeaths())
            {
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
                return;
            }

            if (expedition.CurrentNodeIndex >= expedition.NodeData.Count - 1)
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
            else
            {
                expedition.CurrentNodeIndex++;
                eventBus.RaiseNodeUpdated(expedition.NodeData, expedition.CurrentNodeIndex);
                ExpeditionManager.Instance.SaveProgress();
            }
        }

        private bool CheckForHeroDeaths()
        {
            bool dead = partyData.HeroStats.Any(h => h.Type == CharacterType.Hero && h.Health <= 0);
            if (dead) Debug.Log("Hero death → return to Temple");
            return dead;
        }

        private void CheckExpeditionFailure()
        {
            if (partyData.HeroStats.All(h => h.HasRetreated || h.Health <= 0))
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
        }

        private bool ValidateReferences()
        {
            if (eventBus == null || expeditionData == null || partyData == null ||
                defaultPositions == null || combatEncounterData == null || viewComponent == null)
            {
                Debug.LogError("ExpeditionSceneComponent missing references!");
                return false;
            }
            return true;
        }
    }
}