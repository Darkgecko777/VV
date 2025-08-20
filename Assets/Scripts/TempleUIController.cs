using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class TempleUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private TemplePlanningManager planningManager;

        private DropdownField virusDropdown;
        private DropdownField nodeDropdown;
        private Button generateButton;
        private Button launchButton;
        private Button seedVirusButton;

        public event Action OnGenerateClicked;
        public event Action OnLaunchClicked;
        public event Action<string, int> OnSeedVirusClicked;

        void Awake()
        {
            if (!ValidateReferences()) return;
            virusDropdown = uiDocument.rootVisualElement.Q<DropdownField>("VirusDropdown");
            nodeDropdown = uiDocument.rootVisualElement.Q<DropdownField>("NodeDropdown");
            generateButton = uiDocument.rootVisualElement.Q<Button>("GenerateButton");
            launchButton = uiDocument.rootVisualElement.Q<Button>("LaunchButton");
            seedVirusButton = uiDocument.rootVisualElement.Q<Button>("SeedVirusButton");
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || planningManager == null)
            {
                Debug.LogError($"TempleUIController: Missing references! UIDocument: {uiDocument != null}, PlanningManager: {planningManager != null}");
                return false;
            }
            return true;
        }

        public void InitializeUI(List<VirusData> availableViruses, ExpeditionData expeditionData)
        {
            if (virusDropdown == null || nodeDropdown == null || generateButton == null || launchButton == null || seedVirusButton == null)
            {
                Debug.LogError("TempleUIController: UI elements not found!");
                return;
            }

            // Populate virus dropdown
            virusDropdown.choices.Clear();
            foreach (var virus in availableViruses)
            {
                if (virus != null) virusDropdown.choices.Add(virus.VirusID);
            }
            virusDropdown.value = virusDropdown.choices.Count > 0 ? virusDropdown.choices[0] : null;

            // Populate node dropdown
            UpdateNodeDropdown(expeditionData);

            // Bind button events
            generateButton.clicked += () => OnGenerateClicked?.Invoke();
            launchButton.clicked += () => OnLaunchClicked?.Invoke();
            seedVirusButton.clicked += () =>
            {
                if (nodeDropdown.index >= 0 && virusDropdown.index >= 0)
                {
                    OnSeedVirusClicked?.Invoke(virusDropdown.value, nodeDropdown.index);
                }
                else
                {
                    Debug.LogWarning("TempleUIController: Invalid dropdown selection for virus seeding!");
                }
            };

            SetLaunchButtonEnabled(false);
        }

        public void UpdateNodeDropdown(ExpeditionData expeditionData)
        {
            if (nodeDropdown == null) return;
            nodeDropdown.choices.Clear();
            for (int i = 0; i < expeditionData.NodeData.Count; i++)
            {
                nodeDropdown.choices.Add($"Node {i + 1} ({expeditionData.NodeData[i].NodeType})");
            }
            nodeDropdown.value = nodeDropdown.choices.Count > 0 ? nodeDropdown.choices[0] : null;
        }

        public void SetLaunchButtonEnabled(bool enabled)
        {
            if (launchButton != null)
            {
                launchButton.SetEnabled(enabled);
            }
        }
    }
}