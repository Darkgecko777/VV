using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class BattleUIController : MonoBehaviour
    {
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private BattleVisualController visualController;

        private VisualElement root;
        private VisualElement fadePanel;
        private VisualElement heroesContainer;
        private VisualElement monstersContainer;
        private Label combatLog;
        private Button continueButton;
        private Dictionary<ICombatUnit, VisualElement> unitPanels;
        private float fadeDuration = 0.5f;

        public event Action OnContinueClicked;

        void Awake()
        {
            if (!ValidateReferences()) return;

            root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("BattleUIController: rootVisualElement is null in Awake, will retry in Start.");
                return;
            }

            InitializeUIElements();
        }

        void Start()
        {
            if (root == null && uiDocument != null)
            {
                root = uiDocument.rootVisualElement;
                if (root == null)
                {
                    Debug.LogError("BattleUIController: Failed to initialize rootVisualElement in Start!");
                    return;
                }
                InitializeUIElements();
            }
        }

        private void InitializeUIElements()
        {
            heroesContainer = root.Q<VisualElement>("HeroesContainer");
            monstersContainer = root.Q<VisualElement>("MonstersContainer");
            combatLog = root.Q<Label>("CombatLog");
            continueButton = root.Q<Button>("ContinueButton");
            fadePanel = root.Q<VisualElement>("FadePanel") ?? new VisualElement { name = "FadePanel" };

            if (heroesContainer == null || monstersContainer == null || combatLog == null || continueButton == null)
            {
                Debug.LogError($"BattleUIController: Missing UI elements! HeroesContainer: {heroesContainer != null}, MonstersContainer: {monstersContainer != null}, CombatLog: {combatLog != null}, ContinueButton: {continueButton != null}");
                return;
            }

            fadePanel.style.backgroundColor = uiConfig.BogRotColor;
            fadePanel.style.opacity = 0;
            fadePanel.style.position = Position.Absolute;
            fadePanel.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            fadePanel.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            root.Add(fadePanel);

            unitPanels = new Dictionary<ICombatUnit, VisualElement>();

            combatLog.text = "";
            combatLog.style.color = uiConfig.TextColor;
            combatLog.style.unityFont = uiConfig.PixelFont;
            continueButton.style.color = uiConfig.TextColor;
            continueButton.style.unityFont = uiConfig.PixelFont;
            continueButton.clicked += () => OnContinueClicked?.Invoke();
            continueButton.SetEnabled(false);
        }

        public void InitializeUnitPanels(List<(ICombatUnit unit, GameObject go, DisplayStats stats)> units)
        {
            if (heroesContainer == null || monstersContainer == null) return;

            foreach (var (unit, go, stats) in units)
            {
                if (unit == null) continue;

                var panel = CreateUnitPanel(stats);
                unitPanels[unit] = panel;

                if (stats.isHero)
                {
                    heroesContainer.Add(panel);
                }
                else
                {
                    monstersContainer.Add(panel);
                }
            }
        }

        private VisualElement CreateUnitPanel(DisplayStats stats)
        {
            VisualElement panel = new VisualElement();
            panel.AddToClassList("unit-panel");

            Label nameLabel = new Label(stats.name);
            nameLabel.AddToClassList("unit-name");
            nameLabel.style.color = uiConfig.TextColor;
            nameLabel.style.unityFont = uiConfig.PixelFont;
            panel.Add(nameLabel);

            ProgressBar healthBar = new ProgressBar();
            healthBar.AddToClassList("health-bar");
            healthBar.lowValue = 0;
            healthBar.highValue = stats.maxHealth;
            healthBar.value = stats.health;
            healthBar.title = $"Health: {stats.health}/{stats.maxHealth}";
            panel.Add(healthBar);

            if (stats.morale.HasValue)
            {
                ProgressBar moraleBar = new ProgressBar();
                moraleBar.AddToClassList("morale-bar");
                moraleBar.lowValue = 0;
                moraleBar.highValue = stats.maxMorale.Value;
                moraleBar.value = stats.morale.Value;
                moraleBar.title = $"Morale: {stats.morale.Value}/{stats.maxMorale.Value}";
                panel.Add(moraleBar);
            }

            return panel;
        }

        public void UpdateUnitPanel(ICombatUnit unit, DisplayStats stats)
        {
            if (unitPanels.TryGetValue(unit, out var panel))
            {
                var healthBar = panel.Q<ProgressBar>("health-bar");
                if (healthBar != null)
                {
                    healthBar.value = stats.health;
                    healthBar.title = $"Health: {stats.health}/{stats.maxHealth}";
                }

                var moraleBar = panel.Q<ProgressBar>("morale-bar");
                if (moraleBar != null && stats.morale.HasValue)
                {
                    moraleBar.value = stats.morale.Value;
                    moraleBar.title = $"Morale: {stats.morale.Value}/{stats.maxMorale.Value}";
                }

                if (stats.health <= 0)
                {
                    panel.style.display = DisplayStyle.None;
                }
            }
        }

        public void SubscribeToModel(CombatModel model)
        {
            if (model == null) return;
            model.OnLogMessage += LogMessage;
            model.OnUnitUpdated += UpdateUnitPanel;
            model.OnDamagePopup += ShowDamagePopup;
            model.OnBattleEnded += EnableContinueButton;
        }

        public void ShowDamagePopup(ICombatUnit unit, string message)
        {
            if (root == null || unit == null) return;

            var units = visualController.GetUnits();
            var unitEntry = units.Find(u => u.unit == unit);
            if (unitEntry.go == null) return;

            Vector3 worldPosition = unitEntry.go.transform.position;
            Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            float screenHeight = mainCamera.pixelHeight;

            VisualElement popup = new Label(message);
            popup.AddToClassList("damage-popup");
            popup.style.position = Position.Absolute;
            popup.style.left = screenPosition.x;
            popup.style.top = screenHeight - screenPosition.y - 50;
            root.Add(popup);
            StartCoroutine(AnimatePopup(popup, screenHeight));
        }

        private IEnumerator AnimatePopup(VisualElement popup, float screenHeight)
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

        public void LogMessage(string message, Color color)
        {
            combatLog.text += $"{message}\n";
            combatLog.style.color = color;
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
            fadePanel.style.display = DisplayStyle.Flex;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.style.opacity = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                yield return null;
            }
            fadePanel.style.opacity = 1;
            onComplete?.Invoke();
            fadePanel.style.opacity = 0;
            fadePanel.style.display = DisplayStyle.None;
        }

        private void EnableContinueButton()
        {
            continueButton.SetEnabled(true);
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || uiConfig == null || mainCamera == null || visualController == null)
            {
                Debug.LogError($"BattleUIController: Missing critical references! UIDocument: {uiDocument != null}, UIConfig: {uiConfig != null}, MainCamera: {mainCamera != null}, VisualController: {visualController != null}");
                return false;
            }
            return true;
        }
    }
}