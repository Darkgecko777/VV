using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class ExpeditionUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIConfig uiConfig;

        private VisualElement root;
        private VisualElement popoutContainer;
        private Label flavourText;
        private Button continueButton;
        private Button advanceNodeButton;
        private VisualElement portraitContainer;
        private VisualElement fadeOverlay;
        private float fadeDuration = 0.5f;

        void Start()
        {
            if (!ValidateReferences()) return;

            root = uiDocument.rootVisualElement;
            popoutContainer = root.Q<VisualElement>("PopoutContainer");
            flavourText = popoutContainer?.Q<Label>("FlavourText");
            continueButton = popoutContainer?.Q<Button>("ContinueButton");
            advanceNodeButton = root.Q<Button>("AdvanceNodeButton");
            fadeOverlay = root.Q<VisualElement>("FadeOverlay");

            if (popoutContainer == null || flavourText == null || continueButton == null || advanceNodeButton == null || fadeOverlay == null)
            {
                Debug.LogError($"ExpeditionUIController: Missing UI elements! PopoutContainer: {popoutContainer != null}, FlavourText: {flavourText != null}, ContinueButton: {continueButton != null}, AdvanceNodeButton: {advanceNodeButton != null}, FadeOverlay: {fadeOverlay != null}");
                return;
            }

            portraitContainer = new VisualElement { name = "PortraitContainer" };
            root.Add(portraitContainer);

            portraitContainer.style.flexDirection = FlexDirection.Row;
            portraitContainer.style.position = Position.Absolute;
            portraitContainer.style.top = 200;
            portraitContainer.style.left = 50;
            popoutContainer.style.display = DisplayStyle.None;
            fadeOverlay.style.opacity = 0;

            // Apply UIConfig styling
            flavourText.style.color = uiConfig.TextColor;
            flavourText.style.unityFont = uiConfig.PixelFont;
            continueButton.style.color = uiConfig.TextColor;
            continueButton.style.unityFont = uiConfig.PixelFont;
            advanceNodeButton.style.color = uiConfig.TextColor;
            advanceNodeButton.style.unityFont = uiConfig.PixelFont;

            ExpeditionManager.Instance.OnExpeditionGenerated += UpdateUI;
            ExpeditionManager.Instance.OnCombatStarted += FadeToCombat;
            continueButton.clicked += () =>
            {
                popoutContainer.style.display = DisplayStyle.None;
                ExpeditionManager.Instance.OnContinueClicked();
            };
            advanceNodeButton.clicked += () =>
            {
                var expeditionData = ExpeditionManager.Instance.GetExpedition();
                if (expeditionData != null && expeditionData.CurrentNodeIndex < expeditionData.NodeData.Count - 1)
                {
                    ExpeditionManager.Instance.ProcessCurrentNode();
                }
            };

            UpdateUI();
        }

        void OnDestroy()
        {
            if (ExpeditionManager.Instance != null)
            {
                ExpeditionManager.Instance.OnExpeditionGenerated -= UpdateUI;
                ExpeditionManager.Instance.OnCombatStarted -= FadeToCombat;
            }
            if (continueButton != null)
            {
                continueButton.clicked -= null;
            }
            if (advanceNodeButton != null)
            {
                advanceNodeButton.clicked -= null;
            }
        }

        private void UpdateUI()
        {
            var expeditionData = ExpeditionManager.Instance.GetExpedition();
            if (visualConfig != null) UpdatePortraits(expeditionData?.Party);
            UpdateFlavourText(expeditionData);
        }

        private void UpdatePortraits(PartyData party)
        {
            if (portraitContainer == null) return;
            portraitContainer.Clear();
            if (party == null || party.HeroStats.Count == 0) return;

            for (int i = 0; i < party.HeroStats.Count; i++)
            {
                VisualElement portrait = new VisualElement
                {
                    name = $"Portrait{i + 1}",
                    style = { width = 64, height = 64 }
                };
                if (visualConfig != null)
                {
                    Sprite sprite = visualConfig.GetPortrait(party.HeroStats[i].Type.Id);
                    if (sprite != null)
                    {
                        portrait.style.backgroundImage = new StyleBackground(sprite);
                    }
                }
                portrait.AddToClassList("portrait");
                portraitContainer.Add(portrait);
            }
        }

        private void UpdateFlavourText(ExpeditionData expeditionData)
        {
            if (expeditionData == null || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count || popoutContainer == null || flavourText == null)
            {
                if (popoutContainer != null) popoutContainer.style.display = DisplayStyle.None;
                return;
            }

            NodeData node = expeditionData.NodeData[expeditionData.CurrentNodeIndex];
            flavourText.text = node.IsCombat ? "" : node.FlavourText;
            flavourText.style.color = uiConfig.TextColor;
            flavourText.style.unityFont = uiConfig.PixelFont;
            popoutContainer.style.display = node.IsCombat ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void FadeToCombat()
        {
            if (fadeOverlay == null) return;
            StartCoroutine(FadeRoutine(() => { }));
        }

        private IEnumerator FadeRoutine(System.Action onComplete)
        {
            ExpeditionManager.Instance.SetTransitioning(true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeOverlay.style.opacity = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                fadeOverlay.style.backgroundColor = uiConfig.BogRotColor;
                yield return null;
            }
            fadeOverlay.style.opacity = 1;
            onComplete?.Invoke();
            ExpeditionManager.Instance.SetTransitioning(false);
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || ExpeditionManager.Instance == null || uiConfig == null)
            {
                Debug.LogError($"ExpeditionUIController: Critical references missing! UIDocument: {uiDocument != null}, ExpeditionManager: {ExpeditionManager.Instance != null}, UIConfig: {uiConfig != null}");
                return false;
            }
            if (visualConfig == null)
            {
                Debug.LogWarning($"ExpeditionUIController: Non-critical reference missing! VisualConfig: {visualConfig != null}");
            }
            return true;
        }
    }
}