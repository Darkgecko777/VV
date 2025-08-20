using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class ExpeditionUIManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private ExpeditionManager expeditionManager;
        private VisualElement root;
        private VisualElement nodeContainer;
        private VisualElement popoutContainer;
        private Label flavourText;
        private Button continueButton;
        private float fadeDuration = 0.5f;

        void Start()
        {
            if (uiDocument == null || fadeCanvasGroup == null || expeditionManager == null)
            {
                Debug.LogError($"ExpeditionUIManager: Missing references! UIDocument: {uiDocument != null}, FadeCanvasGroup: {fadeCanvasGroup != null}, ExpeditionManager: {expeditionManager != null}");
                return;
            }

            root = uiDocument.rootVisualElement;
            nodeContainer = root.Q<VisualElement>("NodeContainer");
            popoutContainer = root.Q<VisualElement>("PopoutContainer");
            flavourText = popoutContainer.Q<Label>("FlavourText");
            continueButton = popoutContainer.Q<Button>("ContinueButton");

            if (nodeContainer == null || popoutContainer == null || flavourText == null || continueButton == null)
            {
                Debug.LogError("ExpeditionUIManager: Missing UI elements!");
                return;
            }

            fadeCanvasGroup.alpha = 0;
            popoutContainer.style.display = DisplayStyle.None;
        }

        public void UpdateNodeUI(List<NodeData> nodeData, int currentNodeIndex)
        {
            if (nodeContainer == null) return;

            nodeContainer.Clear();

            for (int i = 0; i < nodeData.Count; i++)
            {
                VisualElement nodeBox = new VisualElement
                {
                    name = $"Node{i + 1}",
                    style = { width = 100, height = 100 }
                };
                nodeBox.AddToClassList("node-box");
                nodeBox.AddToClassList(nodeData[i].IsCombat ? "node-combat" : "node-noncombat");
                if (i == currentNodeIndex)
                {
                    nodeBox.AddToClassList("node-current");
                }

                Label nodeLabel = new Label
                {
                    name = $"Node{i + 1}_Label",
                    text = $"Node {i + 1}",
                    style = { unityTextAlign = TextAnchor.MiddleCenter }
                };
                nodeLabel.AddToClassList("node-label");

                nodeBox.Add(nodeLabel);
                nodeContainer.Add(nodeBox);
            }
        }

        public void ShowNonCombatPopout(string text, Action onContinue)
        {
            if (popoutContainer == null || flavourText == null || continueButton == null) return;

            flavourText.text = text;
            popoutContainer.style.display = DisplayStyle.Flex;
            continueButton.clicked += () =>
            {
                popoutContainer.style.display = DisplayStyle.None;
                onContinue?.Invoke();
            };
        }

        public void FadeToCombat(Action onComplete)
        {
            if (fadeCanvasGroup == null || expeditionManager == null) return;

            StartCoroutine(FadeRoutine(0, 1, () =>
            {
                onComplete?.Invoke();
            }));
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, Action onComplete)
        {
            expeditionManager.SetTransitioning(true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = endAlpha;
            onComplete?.Invoke();
            expeditionManager.SetTransitioning(false);
        }
    }
}