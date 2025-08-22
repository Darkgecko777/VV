using UnityEngine;
using System.Collections.Generic;

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

        private bool isExpeditionGenerated = false;

        void Awake()
        {
            if (!ValidateReferences()) return;
            uiController.OnGenerateClicked += GenerateExpedition;
            uiController.OnLaunchClicked += LaunchExpedition;
            uiController.OnSeedVirusClicked += SeedVirus;

            Debug.Log($"TemplePlanningController.Awake: partyData.HeroSOs count: {partyData?.HeroSOs?.Count ?? 0}");
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
                visualController == null || uiController == null)
            {
                Debug.LogError($"TemplePlanningController: Missing references! ExpeditionData: {expeditionData != null}, " +
                    $"PartyData: {partyData != null}, AvailableViruses: {availableViruses != null}, " +
                    $"VisualConfig: {visualConfig != null}, VisualController: {visualController != null}, " +
                    $"UIController: {uiController != null}");
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

            ExpeditionManager.Instance.GenerateExpedition();
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