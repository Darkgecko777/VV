using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace VirulentVentures
{
    public class BattleUIController : MonoBehaviour
    {
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private PartyData partyData;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        private VisualElement root;
        private RectTransform canvasRectTransform;
        private Camera mainCamera;
        private Dictionary<ICombatUnit, VisualElement> unitPanels;
        private Label combatLog;
        private float fadeDuration = 0.5f;

        public event Action OnContinueClicked;

        void Awake()
        {
            if (!ValidateReferences()) return;
            root = GetComponent<UIDocument>()?.rootVisualElement;
            canvasRectTransform = GetComponent<RectTransform>();
            combatLog = root.Q<Label>("CombatLog");
            if (combatLog == null)
            {
                Debug.LogError("BattleUIController: Missing CombatLog!");
                return;
            }
            combatLog.style.display = DisplayStyle.Flex;
            combatLog.text = "";
            fadeCanvasGroup.alpha = 0;

            // Bind ContinueButton
            var continueButton = root.Q<Button>("ContinueButton");
            if (continueButton != null)
            {
                continueButton.clicked += () => OnContinueClicked?.Invoke();
            }
            else
            {
                Debug.LogWarning("BattleUIController: ContinueButton not found!");
            }
        }

        private bool ValidateReferences()
        {
            if (uiConfig == null || partyData == null || expeditionData == null || fadeCanvasGroup == null)
            {
                Debug.LogError($"BattleUIController: Missing references! UIConfig: {uiConfig != null}, PartyData: {partyData != null}, ExpeditionData: {expeditionData != null}, FadeCanvasGroup: {fadeCanvasGroup != null}");
                return false;
            }
            return true;
        }

        public void InitializeUI(List<(ICombatUnit unit, GameObject go)> units)
        {
            if (root == null || canvasRectTransform == null || mainCamera == null)
            {
                Debug.LogWarning($"BattleUIController: Invalid setup! Root: {root != null}, CanvasRectTransform: {canvasRectTransform != null}, MainCamera: {mainCamera != null}");
                return;
            }

            unitPanels = new Dictionary<ICombatUnit, VisualElement>();
            SetupUnitPanels(units.Where(u => u.unit is HeroStats).Select(u => u.unit).ToList(), true);
            SetupUnitPanels(units.Where(u => u.unit is MonsterStats).Select(u => u.unit).ToList(), false);
        }

        private void SetupUnitPanels(List<ICombatUnit> statsList, bool isHero)
        {
            if (statsList == null || statsList.Count == 0)
            {
                Debug.LogWarning($"BattleUIController: Empty {(isHero ? "hero" : "monster")} stats");
                return;
            }

            string containerName = isHero ? "HeroesContainer" : "MonstersContainer";
            VisualElement container = root.Q<VisualElement>(containerName);
            if (container == null)
            {
                Debug.LogError($"BattleUIController: Missing {containerName}");
                return;
            }

            container.Clear();

            for (int i = 0; i < statsList.Count; i++)
            {
                ICombatUnit unit = statsList[i];
                VisualElement panel = new VisualElement { name = $"Panel_{unit.Type.Id}" };
                panel.AddToClassList("unit-panel");
                panel.style.position = Position.Absolute;

                Label typeLabel = new Label { text = unit.Type.Id, name = "TypeLabel" };
                typeLabel.AddToClassList("stat-label");
                panel.Add(typeLabel);

                ProgressBar healthBar = new ProgressBar { name = "HealthBar" };
                healthBar.value = (float)unit.Health / unit.MaxHealth * 100;
                healthBar.title = $"HP: {unit.Health}/{unit.MaxHealth}";
                healthBar.AddToClassList(isHero ? "health-fill-hero" : "health-fill-monster");
                panel.Add(healthBar);

                ProgressBar moraleBar = new ProgressBar { name = "MoraleBar" };
                moraleBar.value = unit.Morale;
                moraleBar.title = $"Morale: {unit.Morale}";
                panel.Add(moraleBar);

                container.Add(panel);
                unitPanels[unit] = panel;

                UpdateUnitPanelPosition(unit, panel, isHero);
            }
        }

        private void UpdateUnitPanelPosition(ICombatUnit unit, VisualElement panel, bool isHero)
        {
            Vector2 screenPosition = mainCamera.WorldToScreenPoint(unit.Position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPosition, null, out Vector2 localPoint);

            panel.style.left = localPoint.x + (isHero ? -50 : 50);
            panel.style.top = -localPoint.y + 50;
        }

        public void UpdateUnitPanel(ICombatUnit unit)
        {
            if (unitPanels.TryGetValue(unit, out VisualElement panel))
            {
                ProgressBar healthBar = panel.Q<ProgressBar>("HealthBar");
                if (healthBar != null)
                {
                    healthBar.value = (float)unit.Health / unit.MaxHealth * 100;
                    healthBar.title = $"HP: {unit.Health}/{unit.MaxHealth}";
                }

                ProgressBar moraleBar = panel.Q<ProgressBar>("MoraleBar");
                if (moraleBar != null)
                {
                    moraleBar.value = unit.Morale;
                    moraleBar.title = $"Morale: {unit.Morale}";
                }
            }
        }

        public void ShowDamagePopup(ICombatUnit unit, string message)
        {
            VisualElement popup = new VisualElement { name = "DamagePopup" };
            popup.AddToClassList("damage-popup");
            Label messageLabel = new Label { text = message };
            messageLabel.AddToClassList("damage-text");
            popup.Add(messageLabel);

            Vector2 screenPosition = mainCamera.WorldToScreenPoint(unit.Position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPosition, null, out Vector2 localPoint);

            popup.style.left = localPoint.x;
            popup.style.top = -localPoint.y;
            root.Add(popup);
            StartCoroutine(AnimatePopup(popup));
        }

        private IEnumerator AnimatePopup(VisualElement popup)
        {
            float riseDistance = 100f;
            float riseDuration = 1f;
            float fadeDuration = 0.5f;
            float startY = popup.style.top.value.value;
            float elapsed = 0f;

            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / riseDuration;
                popup.style.top = startY - (riseDistance * t);
                if (elapsed > riseDuration - fadeDuration)
                {
                    float fadeT = (elapsed - (riseDuration - fadeDuration)) / fadeDuration;
                    popup.style.opacity = 1f - fadeT;
                }
                yield return null;
            }
            root.Remove(popup);
        }

        public void LogMessage(string message)
        {
            if (combatLog != null)
            {
                combatLog.text += $"{message}\n";
                string[] lines = combatLog.text.Split('\n');
                if (lines.Length > 10)
                {
                    combatLog.text = string.Join("\n", lines, lines.Length - 10, 10);
                }
            }
        }

        public void FadeToScene(Action onComplete)
        {
            StartCoroutine(FadeToExpedition(onComplete));
        }

        private IEnumerator FadeToExpedition(Action onComplete)
        {
            if (fadeCanvasGroup == null)
            {
                Debug.LogWarning("BattleUIController: Missing fadeCanvasGroup!");
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1;
            onComplete?.Invoke();
        }

        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("BattleUIController: MainCamera not found!");
            }
        }
    }
}