using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
namespace VirulentVentures
{
    public class CombatUIElementsComponent : MonoBehaviour
    {
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private CombatConfig combatConfig;
        private VisualElement root;
        private VisualElement logContent;
        private ScrollView logScrollView;
        private List<Label> logMessages = new List<Label>();
        private Dictionary<ICombatUnit, VisualElement> unitPanels = new Dictionary<ICombatUnit, VisualElement>();
        private Dictionary<ICombatUnit, (Label atk, Label def, Label spd, Label eva, Label morale, Label rank)> unitStatLabels = new Dictionary<ICombatUnit, (Label, Label, Label, Label, Label, Label)>();
        private Dictionary<ICombatUnit, Label> infectedLabels = new Dictionary<ICombatUnit, Label>();
        private Dictionary<ICombatUnit, bool> retreatedUnits = new Dictionary<ICombatUnit, bool>(); // Track retreat state
        private Label speedLabel;
        private Button speedPlusButton;
        private Button speedMinusButton;
        private Button pauseButton;
        private Button playButton;
        private VisualElement endPanel;
        private Label endLabel;
        private Button continueButton;
        private float speedIncrement = 0.5f;
        private bool isPaused;
        void Awake()
        {
            if (!ValidateReferences()) return;
            SetupUI();
            eventBus.OnCombatInitialized += InitializeCombat;
            eventBus.OnLogMessage += HandleLogMessage;
            eventBus.OnUnitUpdated += HandleUnitUpdated;
            eventBus.OnCombatPaused += HandleCombatPaused;
            eventBus.OnCombatPlayed += HandleCombatPlayed;
            eventBus.OnCombatSpeedChanged += HandleCombatSpeedChanged;
            eventBus.OnAbilitySelected += HandleAbilitySelected;
            eventBus.OnCombatEnded += ShowEndPanel;
            eventBus.OnUnitRetreated += HandleUnitRetreated;
            eventBus.OnUnitDied += HandleUnitDied;
        }
        void OnDestroy()
        {
            eventBus.OnCombatInitialized -= InitializeCombat;
            eventBus.OnLogMessage -= HandleLogMessage;
            eventBus.OnUnitUpdated -= HandleUnitUpdated;
            eventBus.OnCombatPaused -= HandleCombatPaused;
            eventBus.OnCombatPlayed -= HandleCombatPlayed;
            eventBus.OnCombatSpeedChanged -= HandleCombatSpeedChanged;
            eventBus.OnAbilitySelected -= HandleAbilitySelected;
            eventBus.OnCombatEnded -= ShowEndPanel;
            eventBus.OnUnitRetreated -= HandleUnitRetreated;
            eventBus.OnUnitDied -= HandleUnitDied;
            if (speedPlusButton != null)
                speedPlusButton.clicked -= IncreaseSpeed;
            if (speedMinusButton != null)
                speedMinusButton.clicked -= DecreaseSpeed;
            if (pauseButton != null)
                pauseButton.clicked -= PauseCombat;
            if (playButton != null)
                playButton.clicked -= PlayCombat;
            if (continueButton != null)
                continueButton.clicked -= ContinueCombat;
        }
        private void SetupUI()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null)
            {
                Debug.LogError("CombatUIElementsComponent: Root VisualElement not found. Ensure UIDocument is assigned and CombatScene.uxml is set.");
                return;
            }
            var combatRoot = root.Q<VisualElement>("combat-root");
            if (combatRoot == null)
            {
                Debug.LogError("CombatUIElementsComponent: Combat root not found in CombatScene.uxml.");
                return;
            }
            var bottomPanel = combatRoot.Q<VisualElement>("bottom-panel");
            if (bottomPanel == null)
            {
                Debug.LogError("CombatUIElementsComponent: Bottom panel not found in CombatScene.uxml.");
                return;
            }
            logScrollView = bottomPanel.Q<ScrollView>("log-scrollview");
            if (logScrollView == null)
            {
                Debug.LogError("CombatUIElementsComponent: Log ScrollView not found in CombatScene.uxml under bottom-panel.");
                return;
            }
            logContent = logScrollView.Q<VisualElement>("log-content");
            if (logContent == null)
            {
                Debug.LogError("CombatUIElementsComponent: Log content not found in CombatScene.uxml under log-scrollview.");
                return;
            }
            var speedPanel = combatRoot.Q<VisualElement>("speed-control-panel");
            if (speedPanel == null)
            {
                Debug.LogError("CombatUIElementsComponent: Speed control panel not found in CombatScene.uxml.");
                return;
            }
            speedLabel = speedPanel.Q<Label>("speed-label");
            speedPlusButton = speedPanel.Q<Button>("speed-plus-button");
            speedMinusButton = speedPanel.Q<Button>("speed-minus-button");
            pauseButton = speedPanel.Q<Button>("pause-button");
            playButton = speedPanel.Q<Button>("play-button");
            endPanel = combatRoot.Q<VisualElement>("end-panel");
            endLabel = endPanel?.Q<Label>("end-label");
            continueButton = endPanel?.Q<Button>("continue-button");
            if (speedLabel == null || speedPlusButton == null || speedMinusButton == null || pauseButton == null || playButton == null)
            {
                Debug.LogError("CombatUIElementsComponent: Speed control UI elements not found: " +
                    $"speed-label: {speedLabel != null}, speed-plus-button: {speedPlusButton != null}, " +
                    $"speed-minus-button: {speedMinusButton != null}, pause-button: {pauseButton != null}, " +
                    $"play-button: {playButton != null}");
                return;
            }
            if (endPanel == null || endLabel == null || continueButton == null)
            {
                Debug.LogError("CombatUIElementsComponent: End panel UI elements not found: " +
                    $"end-panel: {endPanel != null}, end-label: {endLabel != null}, continue-button: {continueButton != null}");
                return;
            }
            speedLabel.text = $"Speed: {combatConfig.CombatSpeed:F1}x";
            speedPlusButton.clicked += IncreaseSpeed;
            speedMinusButton.clicked -= DecreaseSpeed;
            pauseButton.clicked += PauseCombat;
            playButton.clicked += PlayCombat;
            continueButton.clicked += ContinueCombat;
            endPanel.style.display = DisplayStyle.None;
            UpdateButtonStates();
        }
        private void IncreaseSpeed()
        {
            float newSpeed = combatConfig.CombatSpeed + speedIncrement;
            eventBus.RaiseCombatSpeedChanged(newSpeed);
        }
        private void DecreaseSpeed()
        {
            float newSpeed = combatConfig.CombatSpeed - speedIncrement;
            eventBus.RaiseCombatSpeedChanged(newSpeed);
        }
        private void PauseCombat()
        {
            eventBus.RaiseCombatPaused();
        }
        private void PlayCombat()
        {
            eventBus.RaiseCombatPlayed();
        }
        private void ContinueCombat()
        {
            bool isVictory = endLabel.text == "Victory!";
            endPanel.style.display = DisplayStyle.None;
            if (isVictory)
            {
                ExpeditionManager.Instance.TransitionToExpeditionScene();
            }
            else
            {
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
            }
        }
        private void ShowEndPanel(bool isVictory)
        {
            if (endPanel == null || endLabel == null)
            {
                Debug.LogError("CombatUIElementsComponent: endPanel or endLabel is null in ShowEndPanel. Ensure SetupUI completed successfully.");
                return;
            }
            endPanel.style.display = DisplayStyle.Flex;
            endLabel.text = isVictory ? "Victory!" : "Defeat!";
            endLabel.style.color = isVictory ? Color.green : Color.red;
            UpdateButtonStates();
        }
        private void HandleCombatPaused()
        {
            isPaused = true;
            eventBus.RaiseLogMessage("Combat paused", uiConfig.TextColor);
            UpdateButtonStates();
        }
        private void HandleCombatPlayed()
        {
            isPaused = false;
            eventBus.RaiseLogMessage("Combat resumed", uiConfig.TextColor);
            UpdateButtonStates();
        }
        private void HandleCombatSpeedChanged(EventBusSO.CombatSpeedData data)
        {
            speedLabel.text = $"Speed: {data.speed:F1}x";
            UpdateButtonStates();
        }
        private void HandleAbilitySelected(EventBusSO.AttackData data)
        {
            if (unitPanels.TryGetValue(data.attacker, out VisualElement panel) && (data.abilityId.Contains("Taunt") || data.abilityId.Contains("Thorns")))
                StartCoroutine(FlashPanel(panel, new Color(1f, 1f, 0f), 0.5f));
        }
        private void HandleUnitRetreated(ICombatUnit unit)
        {
            if (unitPanels.TryGetValue(unit, out var panel) && panel != null)
            {
                retreatedUnits[unit] = true;
                panel.AddToClassList("retreat-slide");
            }
        }
        private void UpdateButtonStates()
        {
            pauseButton.SetEnabled(!isPaused);
            playButton.SetEnabled(isPaused);
            speedPlusButton.SetEnabled(combatConfig.CombatSpeed < combatConfig.MaxCombatSpeed);
            speedMinusButton.SetEnabled(combatConfig.CombatSpeed > combatConfig.MinCombatSpeed);
        }
        private void InitializeCombat(EventBusSO.CombatInitData data)
        {
            unitPanels.Clear();
            unitStatLabels.Clear();
            infectedLabels.Clear();
            retreatedUnits.Clear(); // Clear retreat state on combat init
            logMessages.Clear();
            logContent?.Clear();
            var heroes = data.units.Where(u => u.stats.isHero).ToList();
            var infectedHeroCount = heroes.Count(h => h.stats.isInfected);
            var leftPanel = root.Q<VisualElement>("left-panel");
            float heroPanelHeight = heroes.Count > 0 ? 100f / Mathf.Min(heroes.Count + infectedHeroCount * 0.3f, 4) : 25f;
            for (int i = 0; i < heroes.Count && i < 4; i++)
            {
                var unit = heroes[i].unit;
                var stats = heroes[i].stats;
                var panel = CreateUnitPanel(unit, stats, true, heroPanelHeight);
                leftPanel.Add(panel);
                unitPanels[unit] = panel;
                retreatedUnits[unit] = (unit as CharacterStats)?.HasRetreated ?? false; // Initialize retreat state
            }
            var rightPanel = root.Q<VisualElement>("right-panel");
            float monsterPanelHeight = data.units.Count(u => !u.stats.isHero) > 0 ? 100f / Mathf.Min(data.units.Count(u => !u.stats.isHero), 4) : 25f;
            for (int i = 0; i < data.units.Count(u => !u.stats.isHero) && i < 4; i++)
            {
                var unit = data.units.Where(u => !u.stats.isHero).ToList()[i].unit;
                var stats = data.units.Where(u => !u.stats.isHero).ToList()[i].stats;
                var panel = CreateUnitPanel(unit, stats, false, monsterPanelHeight);
                rightPanel.Add(panel);
                unitPanels[unit] = panel;
                retreatedUnits[unit] = (unit as CharacterStats)?.HasRetreated ?? false; // Initialize retreat state
            }
        }
        private void HandleUnitUpdated(EventBusSO.UnitUpdateData data)
        {
            if (!unitPanels.TryGetValue(data.unit, out var panel) || panel == null) return;
            var rankSection = panel.Q<VisualElement>(className: "rank-section");
            if (rankSection == null) return; // Safety check
            if (data.displayStats.isInfected && !infectedLabels.ContainsKey(data.unit))
            {
                var infectedLabel = new Label("Infected");
                infectedLabel.AddToClassList("infected-label");
                rankSection.Add(infectedLabel);
                infectedLabels[data.unit] = infectedLabel;
            }
            else if (!data.displayStats.isInfected && infectedLabels.ContainsKey(data.unit))
            {
                rankSection.Remove(infectedLabels[data.unit]);
                infectedLabels.Remove(data.unit);
            }
            if (unitStatLabels.TryGetValue(data.unit, out var statLabels))
            {
                statLabels.atk.text = $"A: {data.displayStats.attack}";
                statLabels.def.text = $"D: {data.displayStats.defense}";
                statLabels.spd.text = $"S: {data.displayStats.speed}";
                statLabels.eva.text = $"E: {data.displayStats.evasion}";
                statLabels.rank.text = $"RANK: {data.displayStats.rank}";
                if (data.displayStats.isHero && statLabels.morale != null)
                    statLabels.morale.text = $"Morale: {data.displayStats.morale}/{data.displayStats.maxMorale}";
                var healthBar = panel.Q<VisualElement>(className: "health-bar");
                var healthFill = healthBar?.Q<VisualElement>(className: "health-fill");
                var healthLabel = healthBar?.Q<Label>(className: "health-label");
                if (healthFill != null && healthLabel != null)
                    UpdateHealthBar(healthFill, healthLabel, data.displayStats.health, data.displayStats.maxHealth);
                if (data.displayStats.isHero)
                {
                    var moraleBar = panel.Q<VisualElement>(className: "morale-bar");
                    var moraleFill = moraleBar?.Q<VisualElement>(className: "morale-fill");
                    var moraleLabel = moraleBar?.Q<Label>(className: "morale-label");
                    if (moraleFill != null && moraleLabel != null)
                        UpdateMoraleBar(moraleFill, moraleLabel, data.displayStats.morale, data.displayStats.maxMorale);
                }
                // Low morale: flash red, add yellow border via low-morale class
                if (data.displayStats.morale < data.displayStats.maxMorale * 0.3f && data.displayStats.isHero && !retreatedUnits.ContainsKey(data.unit))
                {
                    panel.AddToClassList("low-morale");
                    StartCoroutine(FlashPanel(panel, new Color(1f, 0.2f, 0.2f), 0.5f));
                }
                else if (panel.ClassListContains("low-morale"))
                {
                    panel.RemoveFromClassList("low-morale");
                }
            }
        }
        private IEnumerator FlashPanel(VisualElement panel, Color flashColor, float duration)
        {
            var originalColor = panel.style.backgroundColor;
            panel.style.backgroundColor = flashColor;
            yield return new WaitForSeconds(duration / combatConfig.CombatSpeed);
            panel.style.backgroundColor = originalColor;
        }
        private bool ValidateReferences()
        {
            if (uiConfig == null || eventBus == null || combatConfig == null)
            {
                Debug.LogError($"CombatUIElementsComponent: Missing references! UIConfig: {uiConfig != null}, EventBus: {eventBus != null}, CombatConfig: {combatConfig != null}");
                return false;
            }
            return true;
        }
        private VisualElement CreateUnitPanel(ICombatUnit unit, CharacterStats.DisplayStats stats, bool isHero, float heightPercent)
        {
            var panel = new VisualElement();
            panel.AddToClassList("unit-panel");
            panel.style.height = new StyleLength(new Length(heightPercent, LengthUnit.Percent));
            var nameLabel = new Label(stats.name);
            nameLabel.AddToClassList("unit-name-label");
            panel.Add(nameLabel);
            var unitContent = new VisualElement();
            unitContent.AddToClassList("unit-content");
            panel.Add(unitContent);
            var rankSection = new VisualElement();
            rankSection.AddToClassList("rank-section");
            var rankLabel = new Label($"RANK: {stats.rank}");
            rankLabel.AddToClassList("rank-label");
            rankSection.Add(rankLabel);
            if (stats.isInfected)
            {
                var infectedLabel = new Label("Infected");
                infectedLabel.AddToClassList("infected-label");
                rankSection.Add(infectedLabel);
                infectedLabels[unit] = infectedLabel;
            }
            unitContent.Add(rankSection);
            var statsSection = new VisualElement();
            statsSection.AddToClassList("stats-section");
            var statGrid = new VisualElement();
            statGrid.AddToClassList("stat-grid");
            var atkContainer = new VisualElement();
            atkContainer.AddToClassList("stat-container");
            var atkLabel = new Label($"A: {stats.attack}");
            atkContainer.Add(atkLabel);
            statGrid.Add(atkContainer);
            var defContainer = new VisualElement();
            defContainer.AddToClassList("stat-container");
            var defLabel = new Label($"D: {stats.defense}");
            defContainer.Add(defLabel);
            statGrid.Add(defContainer);
            var spdContainer = new VisualElement();
            spdContainer.AddToClassList("stat-container");
            var spdLabel = new Label($"S: {stats.speed}");
            spdContainer.Add(spdLabel);
            statGrid.Add(spdContainer);
            var evaContainer = new VisualElement();
            evaContainer.AddToClassList("stat-container");
            var evaLabel = new Label($"E: {stats.evasion}");
            evaContainer.Add(evaLabel);
            statGrid.Add(evaContainer);
            statsSection.Add(statGrid);
            unitContent.Add(statsSection);
            var barsSection = new VisualElement();
            barsSection.AddToClassList("bars-section");
            var healthBar = new VisualElement();
            healthBar.AddToClassList("health-bar");
            var healthFill = new VisualElement();
            healthFill.AddToClassList("health-fill");
            healthBar.Add(healthFill);
            var healthLabel = new Label($"{stats.health:F0}/{stats.maxHealth:F0}");
            healthLabel.AddToClassList("health-label");
            healthBar.Add(healthLabel);
            UpdateHealthBar(healthFill, healthLabel, stats.health, stats.maxHealth);
            barsSection.Add(healthBar);
            Label moraleLabel = null;
            if (isHero)
            {
                var moraleBar = new VisualElement();
                moraleBar.AddToClassList("morale-bar");
                var moraleFill = new VisualElement();
                moraleFill.AddToClassList("morale-fill");
                moraleBar.Add(moraleFill);
                moraleLabel = new Label($"Morale: {stats.morale:F0}/{stats.maxMorale:F0}");
                moraleLabel.AddToClassList("morale-label");
                moraleBar.Add(moraleLabel);
                UpdateMoraleBar(moraleFill, moraleLabel, stats.morale, stats.maxMorale);
                barsSection.Add(moraleBar);
            }
            unitContent.Add(barsSection);
            unitPanels[unit] = panel;
            unitStatLabels[unit] = (atkLabel, defLabel, spdLabel, evaLabel, moraleLabel, rankLabel);
            return panel;
        }
        private void UpdateHealthBar(VisualElement fill, Label label, float health, float maxHealth)
        {
            if (fill == null || label == null) return;
            float healthPercent = Mathf.Clamp(health / maxHealth, 0f, 1f);
            fill.style.width = new StyleLength(new Length(healthPercent * 100f, LengthUnit.Percent));
            label.text = $"{health:F0}/{maxHealth:F0}";
        }
        private void UpdateMoraleBar(VisualElement fill, Label label, float morale, float maxMorale)
        {
            if (fill == null || label == null) return;
            float moralePercent = Mathf.Clamp(morale / maxMorale, 0f, 1f);
            fill.style.width = new StyleLength(new Length(moralePercent * 100f, LengthUnit.Percent));
            label.text = $"Morale: {morale:F0}/{maxMorale:F0}";
        }
        private void HandleUnitDied(ICombatUnit unit)
        {
            if (unitPanels.TryGetValue(unit, out var panel) && panel != null)
            {
                panel.style.display = DisplayStyle.None; 
            }
        }
        private void HandleLogMessage(EventBusSO.LogData data)
        {
            if (logContent == null || logScrollView == null)
            {
                Debug.LogWarning("CombatUIElementsComponent: logContent or logScrollView is null in HandleLogMessage. Ensure SetupUI completed successfully.");
                return;
            }
            var label = new Label(data.message);
            label.style.color = data.color;
            logContent.Add(label);
            logMessages.Add(label);
            if (logMessages.Count > 100)
            {
                logContent.Remove(logMessages[0]);
                logMessages.RemoveAt(0);
            }
            logScrollView.verticalScroller.value = float.MaxValue;
        }
    }
}