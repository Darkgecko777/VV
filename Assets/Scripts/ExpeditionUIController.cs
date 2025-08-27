using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

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
        private VisualElement portraitContainer;
        private VisualElement fadeOverlay;
        private float fadeDuration = 0.5f;

        public event Action OnContinueClicked;

        void Awake()
        {
            if (!ValidateReferences()) return;

            root = uiDocument.rootVisualElement;
            popoutContainer = root.Q<VisualElement>("PopoutContainer");
            flavourText = popoutContainer?.Q<Label>("FlavourText");
            continueButton = root.Q<Button>("ContinueButton");
            fadeOverlay = root.Q<VisualElement>("FadeOverlay");

            if (popoutContainer == null || flavourText == null || continueButton == null || fadeOverlay == null)
            {
                Debug.LogError($"ExpeditionUIController: Missing UI elements! PopoutContainer: {popoutContainer != null}, FlavourText: {flavourText != null}, ContinueButton: {continueButton != null}, FadeOverlay: {fadeOverlay != null}");
                return;
            }
        }

        void Start()
        {
            // Create portrait container programmatically
            portraitContainer = new VisualElement { name = "PortraitContainer" };
            root.Add(portraitContainer);
            portraitContainer.style.flexDirection = FlexDirection.Row;
            portraitContainer.style.position = Position.Absolute;
            portraitContainer.style.top = 200;
            portraitContainer.style.left = 50;

            // Apply UIConfig styling
            flavourText.style.color = uiConfig.TextColor;
            flavourText.style.unityFont = uiConfig.PixelFont;
            continueButton.style.color = uiConfig.TextColor;
            continueButton.style.unityFont = uiConfig.PixelFont;

            // Ensure button and overlay are clickable/ignorable
            continueButton.pickingMode = PickingMode.Position;
            fadeOverlay.pickingMode = PickingMode.Ignore;
            popoutContainer.style.display = DisplayStyle.None;
            fadeOverlay.style.display = DisplayStyle.None;

            // Bind button event
            continueButton.clicked += () =>
            {
                popoutContainer.style.display = DisplayStyle.None;
                OnContinueClicked?.Invoke();
            };
        }

        public void UpdateUI()
        {
            ExpeditionData expeditionData = ExpeditionManager.Instance.GetExpedition();
            if (expeditionData == null)
            {
                Debug.LogWarning("ExpeditionUIController: ExpeditionData is null, skipping UI update.");
                popoutContainer.style.display = DisplayStyle.None;
                continueButton.style.display = DisplayStyle.Flex;
                continueButton.text = "Continue";
                return;
            }

            NodeData node = expeditionData.NodeData[expeditionData.CurrentNodeIndex];
            if (node.NodeType == "Temple")
            {
                popoutContainer.style.display = DisplayStyle.None;
                continueButton.style.display = DisplayStyle.Flex;
                continueButton.text = "Continue";
            }
            else if (node.IsCombat)
            {
                popoutContainer.style.display = DisplayStyle.None;
                continueButton.style.display = DisplayStyle.Flex;
                continueButton.text = "Continue";
            }
            else
            {
                flavourText.text = node.FlavourText;
                flavourText.style.color = uiConfig.TextColor;
                flavourText.style.unityFont = uiConfig.PixelFont;
                popoutContainer.style.display = DisplayStyle.Flex;
                continueButton.style.display = DisplayStyle.Flex;
                continueButton.text = "Continue";
            }
        }

        public void UpdatePortraits(PartyData partyData)
        {
            if (portraitContainer == null) return;
            portraitContainer.Clear();

            var heroes = partyData.GetHeroes();
            for (int i = 0; i < heroes.Count; i++)
            {
                VisualElement portrait = new VisualElement();
                portrait.AddToClassList("portrait");
                Sprite sprite = visualConfig.GetPortrait(heroes[i].Id);
                if (sprite != null)
                {
                    portrait.style.backgroundImage = new StyleBackground(sprite);
                }
                portraitContainer.Add(portrait);
            }
        }

        public void FadeToCombat()
        {
            if (fadeOverlay == null) return;
            StartCoroutine(FadeRoutine(() => { }));
        }

        private IEnumerator FadeRoutine(System.Action onComplete)
        {
            ExpeditionManager.Instance.SetTransitioning(true);
            fadeOverlay.style.display = DisplayStyle.Flex;
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
            fadeOverlay.style.opacity = 0;
            fadeOverlay.style.display = DisplayStyle.None;
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