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

        private VisualElement root;
        private VisualElement fadePanel;
        private RectTransform canvasRectTransform;
        private Camera mainCamera;
        private Dictionary<ICombatUnit, VisualElement> unitPanels;
        private Label combatLog;
        private float fadeDuration = 0.5f;

        public event Action OnContinueClicked;

        void Awake()
        {
            root = GetComponent<UIDocument>()?.rootVisualElement;
            canvasRectTransform = GetComponent<RectTransform>();
            combatLog = root?.Q<Label>("CombatLog");
            if (!ValidateReferences()) return;

            fadePanel = new VisualElement { name = "FadePanel" };
            fadePanel.style.backgroundColor = Color.black;
            fadePanel.style.position = Position.Absolute;
            fadePanel.style.width = Length.Percent(100);
            fadePanel.style.height = Length.Percent(100);
            fadePanel.style.opacity = 0;
            root.Add(fadePanel);

            combatLog.style.display = DisplayStyle.Flex;
            combatLog.text = "";

            var continueButton = root.Q<Button>("ContinueButton");
            if (continueButton != null)
            {
                continueButton.clicked += () => OnContinueClicked?.Invoke();
            }
        }

        private bool ValidateReferences()
        {
            return uiConfig != null && root != null && canvasRectTransform != null && combatLog != null;
        }

        public void InitializeUI(List<(ICombatUnit unit, GameObject go)> units)
        {
            if (!ValidateReferences()) return;
            mainCamera = Camera.main;
            if (mainCamera == null) return;

            unitPanels = new Dictionary<ICombatUnit, VisualElement>();
            SetupUnitPanels(units.Where(u => u.unit is HeroStats).Select(u => u.unit).ToList(), true);
            SetupUnitPanels(units.Where(u => u.unit is MonsterStats).Select(u => u.unit).ToList(), false);
        }

        public void SubscribeToModel(CombatModel model)
        {
            model.OnLogMessage += LogMessage;
            model.OnUnitUpdated += UpdateUnitPanel;
            model.OnDamagePopup += ShowDamagePopup;
        }

        private void SetupUnitPanels(List<ICombatUnit> statsList, bool isHero)
        {
            string containerName = isHero ? "HeroesContainer" : "MonstersContainer";
            VisualElement container = root.Q<VisualElement>(containerName);
            if (container == null) return;

            container.Clear();

            for (int i = 0; i < statsList.Count; i++)
            {
                ICombatUnit unit = statsList[i];
                VisualElement panel = new VisualElement { name = $"Panel_{unit.Type.Id}" };
                panel.AddToClassList("unit-panel");
                panel.style.position = Position.Relative;

                ProgressBar healthBar = new ProgressBar { name = "HealthBar" };
                healthBar.AddToClassList("health-bar");
                healthBar.value = (float)unit.Health / unit.MaxHealth * 100;
                healthBar.title = $"HP: {unit.Health}/{unit.MaxHealth}";
                panel.Add(healthBar);

                if (isHero) // Morale only for heroes
                {
                    ProgressBar moraleBar = new ProgressBar { name = "MoraleBar" };
                    moraleBar.AddToClassList("morale-bar");
                    var hero = unit as HeroStats;
                    moraleBar.value = hero.Morale;
                    moraleBar.title = $"Morale: {hero.Morale}";
                    panel.Add(moraleBar);
                }

                container.Add(panel);
                unitPanels[unit] = panel;
            }
        }

        public void UpdateUnitPanel(ICombatUnit unit)
        {
            if (!unitPanels.TryGetValue(unit, out VisualElement panel))
            {
                return;
            }

            ProgressBar healthBar = panel.Q<ProgressBar>("HealthBar");
            if (healthBar != null)
            {
                healthBar.value = (float)unit.Health / unit.MaxHealth * 100;
                healthBar.title = $"HP: {unit.Health}/{unit.MaxHealth}";
            }

            if (unit is HeroStats hero) // Morale update only for heroes
            {
                ProgressBar moraleBar = panel.Q<ProgressBar>("MoraleBar");
                if (moraleBar != null)
                {
                    moraleBar.value = hero.Morale;
                    moraleBar.title = $"Morale: {hero.Morale}";
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
            combatLog.text += $"{message}\n";
            string[] lines = combatLog.text.Split('\n');
            if (lines.Length > 10)
            {
                combatLog.text = string.Join("\n", lines, lines.Length - 10, 10);
            }
        }

        public void FadeToScene(Action onComplete)
        {
            StartCoroutine(FadeToExpedition(onComplete));
        }

        private IEnumerator FadeToExpedition(Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.style.opacity = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                yield return null;
            }
            fadePanel.style.opacity = 1;
            onComplete?.Invoke();
        }
    }
}