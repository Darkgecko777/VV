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

            // Setup fade panel
            fadePanel.style.backgroundColor = uiConfig.BogRotColor;
            fadePanel.style.position = Position.Absolute;
            fadePanel.style.width = Length.Percent(100);
            fadePanel.style.height = Length.Percent(100);
            fadePanel.style.opacity = 0;
            fadePanel.style.display = DisplayStyle.None;
            fadePanel.pickingMode = PickingMode.Ignore;
            root.Add(fadePanel);

            // Style UI elements
            combatLog.style.display = DisplayStyle.Flex;
            combatLog.style.color = uiConfig.TextColor;
            combatLog.style.unityFont = uiConfig.PixelFont;
            combatLog.text = "";
            continueButton.style.color = uiConfig.TextColor;
            continueButton.style.unityFont = uiConfig.PixelFont;
            continueButton.pickingMode = PickingMode.Position;
            continueButton.SetEnabled(true);

            // Bind button
            continueButton.clicked += () =>
            {
                Debug.Log("BattleUIController: ContinueButton clicked");
                OnContinueClicked?.Invoke();
            };
        }

        public void InitializeUI(List<(ICombatUnit unit, GameObject go)> units)
        {
            if (!ValidateReferences() || root == null) return;

            // Initialize unit panels
            unitPanels = new Dictionary<ICombatUnit, VisualElement>();
            heroesContainer.Clear();
            monstersContainer.Clear();
            foreach (var (unit, _) in units)
            {
                if (unit.Health <= 0) continue;
                VisualElement panel = new VisualElement { name = $"{unit.Type.Id}_Panel" };
                panel.AddToClassList("unit-panel");
                var healthBar = new VisualElement { name = $"{unit.Type.Id}_HealthBar" };
                healthBar.AddToClassList(unit is HeroStats ? "health-fill-hero" : "health-fill-monster");
                healthBar.style.width = 180; // Fixed width, no Slots property
                panel.Add(healthBar);
                var statLabel = new Label { text = $"{unit.Type.Id}\nHealth: {unit.Health}" };
                statLabel.AddToClassList("stat-label");
                panel.Add(statLabel);
                if (unit is HeroStats)
                    heroesContainer.Add(panel);
                else
                    monstersContainer.Add(panel);
                unitPanels[unit] = panel;
            }
        }

        public void SubscribeToModel(CombatModel model)
        {
            if (root == null) return; // Skip if UI not initialized
            model.OnUnitUpdated += UpdateUnitPanel;
            model.OnDamagePopup += ShowDamagePopup;
            model.OnLogMessage += LogMessage;
        }

        public void UpdateUnitPanel(ICombatUnit unit)
        {
            if (!unitPanels.TryGetValue(unit, out var panel)) return;
            var statLabel = panel.Q<Label>();
            if (statLabel != null)
            {
                statLabel.text = $"{unit.Type.Id}\nHealth: {unit.Health}";
            }
            if (unit.Health <= 0)
            {
                panel.RemoveFromHierarchy();
                unitPanels.Remove(unit);
            }
        }

        public void ShowDamagePopup(ICombatUnit unit, string message)
        {
            if (!unitPanels.ContainsKey(unit)) return;
            VisualElement popup = new VisualElement { name = "DamagePopup" };
            popup.AddToClassList("damage-popup");
            Label messageLabel = new Label { text = message };
            messageLabel.AddToClassList("damage-text");
            messageLabel.style.color = uiConfig.TextColor;
            messageLabel.style.unityFont = uiConfig.PixelFont;
            popup.Add(messageLabel);
            popup.style.position = Position.Absolute;
            popup.style.left = unitPanels[unit].layout.x;
            popup.style.top = unitPanels[unit].layout.y - 50;
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
                popup.style.top = startY - (riseDistance * (elapsed / riseDuration));
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

        private bool ValidateReferences()
        {
            if (uiDocument == null || uiConfig == null)
            {
                Debug.LogError($"BattleUIController: Missing critical references! UIDocument: {uiDocument != null}, UIConfig: {uiConfig != null}");
                return false;
            }
            return true;
        }
    }
}