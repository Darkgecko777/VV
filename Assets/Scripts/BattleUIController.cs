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
            fadePanel.style.position = Position.Absolute;
            fadePanel.style.width = Length.Percent(100);
            fadePanel.style.height = Length.Percent(100);
            fadePanel.style.opacity = 0;
            fadePanel.style.display = DisplayStyle.None;
            fadePanel.pickingMode = PickingMode.Ignore;
            root.Add(fadePanel);

            combatLog.style.display = DisplayStyle.Flex;
            combatLog.style.color = uiConfig.TextColor;
            combatLog.style.unityFont = uiConfig.PixelFont;
            combatLog.text = "";
            continueButton.style.color = uiConfig.TextColor;
            continueButton.style.unityFont = uiConfig.PixelFont;
            continueButton.pickingMode = PickingMode.Position;
            continueButton.SetEnabled(true);

            continueButton.clicked += () => OnContinueClicked?.Invoke();
        }

        public void InitializeUI(List<(ICombatUnit unit, GameObject go, DisplayStats displayStats)> combatUnits)
        {
            unitPanels = new Dictionary<ICombatUnit, VisualElement>();
            heroesContainer.Clear();
            monstersContainer.Clear();

            foreach (var (unit, _, displayStats) in combatUnits)
            {
                VisualElement panel = new VisualElement { name = $"{displayStats.name}-panel" };
                panel.AddToClassList("unit-panel");

                VisualElement healthBar = new VisualElement { name = $"{displayStats.name}-health" };
                healthBar.AddToClassList("health-bar");
                VisualElement healthFill = new VisualElement { name = $"{displayStats.name}-health-fill" };
                healthFill.AddToClassList(displayStats.isHero ? "health-fill-hero" : "health-fill-monster");
                healthBar.Add(healthFill);
                panel.Add(healthBar);

                Label nameLabel = new Label { text = displayStats.name, name = $"{displayStats.name}-name" };
                nameLabel.AddToClassList("stat-label");
                panel.Add(nameLabel);

                Label healthLabel = new Label { text = $"Health: {displayStats.health}/{displayStats.maxHealth}", name = $"{displayStats.name}-health-label" };
                healthLabel.AddToClassList("stat-label");
                panel.Add(healthLabel);

                Label attackLabel = new Label { text = $"Attack: {displayStats.attack}", name = $"{displayStats.name}-attack" };
                attackLabel.AddToClassList("stat-label");
                panel.Add(attackLabel);

                Label defenseLabel = new Label { text = $"Defense: {displayStats.defense}", name = $"{displayStats.name}-defense" };
                defenseLabel.AddToClassList("stat-label");
                panel.Add(defenseLabel);

                Label speedLabel = new Label { text = $"Speed: {displayStats.speed}", name = $"{displayStats.name}-speed" };
                speedLabel.AddToClassList("stat-label");
                panel.Add(speedLabel);

                Label evasionLabel = new Label { text = $"Evasion: {displayStats.evasion}%", name = $"{displayStats.name}-evasion" };
                evasionLabel.AddToClassList("stat-label");
                panel.Add(evasionLabel);

                if (displayStats.isHero && displayStats.morale.HasValue && displayStats.maxMorale.HasValue)
                {
                    Label moraleLabel = new Label { text = $"Morale: {displayStats.morale.Value}/{displayStats.maxMorale.Value}", name = $"{displayStats.name}-morale" };
                    moraleLabel.AddToClassList("stat-label");
                    panel.Add(moraleLabel);
                }

                if (displayStats.isHero)
                {
                    heroesContainer.Add(panel);
                }
                else
                {
                    monstersContainer.Add(panel);
                }

                unitPanels.Add(unit, panel);
                UpdateUnitPanel(unit, displayStats);
            }
        }

        public void SubscribeToModel(CombatModel model)
        {
            model.OnUnitUpdated += UpdateUnitPanel;
            model.OnLogMessage += LogMessage;
            model.OnDamagePopup += ShowDamagePopup;
        }

        private void UpdateUnitPanel(ICombatUnit unit, DisplayStats displayStats)
        {
            if (!unitPanels.ContainsKey(unit)) return;

            var panel = unitPanels[unit];
            var healthLabel = panel.Q<Label>($"{displayStats.name}-health-label");
            var attackLabel = panel.Q<Label>($"{displayStats.name}-attack");
            var defenseLabel = panel.Q<Label>($"{displayStats.name}-defense");
            var speedLabel = panel.Q<Label>($"{displayStats.name}-speed");
            var evasionLabel = panel.Q<Label>($"{displayStats.name}-evasion");
            var moraleLabel = panel.Q<Label>($"{displayStats.name}-morale");

            if (healthLabel != null)
            {
                healthLabel.text = $"Health: {displayStats.health}/{displayStats.maxHealth}";
            }
            if (attackLabel != null)
            {
                attackLabel.text = $"Attack: {displayStats.attack}";
            }
            if (defenseLabel != null)
            {
                defenseLabel.text = $"Defense: {displayStats.defense}";
            }
            if (speedLabel != null)
            {
                speedLabel.text = $"Speed: {displayStats.speed}";
            }
            if (evasionLabel != null)
            {
                evasionLabel.text = $"Evasion: {displayStats.evasion}%";
            }
            if (moraleLabel != null && displayStats.isHero && displayStats.morale.HasValue && displayStats.maxMorale.HasValue)
            {
                moraleLabel.text = $"Morale: {displayStats.morale.Value}/{displayStats.maxMorale.Value}";
            }

            var healthFill = panel.Q<VisualElement>($"{displayStats.name}-health-fill");
            if (healthFill != null)
            {
                float healthPercent = displayStats.maxHealth > 0 ? (float)displayStats.health / displayStats.maxHealth : 0f;
                healthFill.style.width = Length.Percent(healthPercent * 100);
            }

            if (displayStats.health <= 0)
            {
                panel.style.display = DisplayStyle.None;
            }
        }

        private void ShowDamagePopup(ICombatUnit unit, string message)
        {
            if (!unitPanels.ContainsKey(unit) || visualController == null)
            {
                Debug.LogError($"BattleUIController: No panel or VisualController for unit {unit.Type?.Id ?? "unknown"}!");
                return;
            }
            var unitEntry = visualController.GetUnits().Find(u => u.unit == unit);
            if (unitEntry.go == null)
            {
                Debug.LogError($"BattleUIController: No GameObject for unit {unit.Type?.Id ?? "unknown"}!");
                return;
            }

            Vector3 worldPos = unitEntry.go.transform.position + Vector3.up * 0.5f;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            float screenHeight = Screen.height;

            VisualElement popup = new VisualElement { name = "DamagePopup" };
            popup.AddToClassList("damage-popup");
            Label messageLabel = new Label { text = message };
            messageLabel.AddToClassList("damage-text");
            messageLabel.style.color = uiConfig.TextColor;
            messageLabel.style.unityFont = uiConfig.PixelFont;
            popup.Add(messageLabel);
            popup.style.position = Position.Absolute;
            popup.style.left = screenPos.x;
            popup.style.top = screenHeight - screenPos.y - 50;
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