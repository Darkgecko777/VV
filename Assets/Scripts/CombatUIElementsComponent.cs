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
        private List<Label> logMessages = new List<Label>();
        private Dictionary<ICombatUnit, VisualElement> unitPanels = new Dictionary<ICombatUnit, VisualElement>();
        private Dictionary<ICombatUnit, (Label atk, Label def, Label spd, Label eva, Label morale, Label rank)> unitStatLabels = new Dictionary<ICombatUnit, (Label, Label, Label, Label, Label, Label)>();
        private Dictionary<ICombatUnit, Label> infectedLabels = new Dictionary<ICombatUnit, Label>();
        private Label speedLabel;
        private Button speedPlusButton;
        private Button speedMinusButton;
        private Button pauseButton;
        private Button playButton;
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
            if (speedPlusButton != null)
                speedPlusButton.clicked -= IncreaseSpeed;
            if (speedMinusButton != null)
                speedMinusButton.clicked -= DecreaseSpeed;
            if (pauseButton != null)
                pauseButton.clicked -= PauseCombat;
            if (playButton != null)
                playButton.clicked -= PlayCombat;
        }
        private void SetupUI()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            if (root == null)
            {
                Debug.LogError("CombatUIElementsComponent: Root VisualElement not found.");
                return;
            }
            var combatRoot = root.Q<VisualElement>("combat-root");
            if (combatRoot == null)
            {
                Debug.LogError("CombatUIElementsComponent: Combat root not found.");
                return;
            }
            var bottomPanel = combatRoot.Q<VisualElement>("bottom-panel");
            if (bottomPanel == null)
            {
                Debug.LogError("CombatUIElementsComponent: Bottom panel not found.");
                return;
            }
            logContent = bottomPanel.Q<VisualElement>("log-content");
            if (logContent == null)
            {
                Debug.LogError("CombatUIElementsComponent: Log content not found.");
                return;
            }
            var speedPanel = combatRoot.Q<VisualElement>("speed-control-panel");
            if (speedPanel == null)
            {
                Debug.LogError("CombatUIElementsComponent: Speed control panel not found.");
                return;
            }
            speedLabel = speedPanel.Q<Label>("speed-label");
            speedPlusButton = speedPanel.Q<Button>("speed-plus-button");
            speedMinusButton = speedPanel.Q<Button>("speed-minus-button");
            pauseButton = speedPanel.Q<Button>("pause-button");
            playButton = speedPanel.Q<Button>("play-button");
            if (speedLabel == null || speedPlusButton == null || speedMinusButton == null || pauseButton == null || playButton == null)
            {
                Debug.LogError("CombatUIElementsComponent: Speed control UI elements not found.");
                return;
            }
            speedLabel.text = $"Speed: {combatConfig.CombatSpeed:F1}x";
            speedPlusButton.clicked += IncreaseSpeed;
            speedMinusButton.clicked -= DecreaseSpeed;
            pauseButton.clicked += PauseCombat;
            playButton.clicked += PlayCombat;
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
        private void UpdateButtonStates()
        {
            pauseButton.SetEnabled(!isPaused);
            playButton.SetEnabled(isPaused);
            speedPlusButton.SetEnabled(combatConfig.CombatSpeed < combatConfig.MaxCombatSpeed);
            speedMinusButton.SetEnabled(combatConfig.CombatSpeed > combatConfig.MinCombatSpeed);
        }
        private void HandleLogMessage(EventBusSO.LogData logData)
        {
            var label = new Label(logData.message);
            label.enableRichText = true;
            label.style.color = logData.color;
            if (logData.message.Contains("[Taunt]") || logData.message.Contains("[Thorns]"))
                label.style.color = new Color(1f, 1f, 0f);
            else if (logData.message.Contains("[Heal]"))
                label.style.color = new Color(0f, 1f, 0f);
            else if (logData.message.Contains("[Debuff]"))
                label.style.color = new Color(1f, 0f, 0f);
            logContent.Add(label);
            logMessages.Add(label);
            var scrollView = logContent.parent as ScrollView;
            if (scrollView != null)
            {
                scrollView.schedule.Execute(() =>
                {
                    float scrollThreshold = 300f;
                    float scrollPosition = scrollView.verticalScroller.value;
                    float maxScroll = scrollView.verticalScroller.highValue;
                    if (scrollPosition >= maxScroll - scrollThreshold || scrollPosition == 0)
                        scrollView.scrollOffset = new Vector2(0, maxScroll);
                }).ExecuteLater(20);
            }
            else
            {
                Debug.LogError("CombatUIElementsComponent: ScrollView not found for log-content.");
            }
        }
        private void UpdateHealthBar(VisualElement fill, Label label, int health, int maxHealth)
        {
            float healthPercent = maxHealth > 0 ? (float)health / maxHealth : 0;
            fill.style.width = Length.Percent(healthPercent * 100);
            fill.style.backgroundColor = new Color(0f, 1f, 0f); // Green fill
            fill.parent.style.backgroundColor = new Color(1f, 0f, 0f); // Red background
            label.text = $"{health}/{maxHealth}";
        }
        private void UpdateMoraleBar(VisualElement fill, Label label, int morale, int maxMorale)
        {
            float moralePercent = maxMorale > 0 ? (float)morale / maxMorale : 0;
            fill.style.width = Length.Percent(moralePercent * 100);
            fill.style.backgroundColor = new Color(0f, 0f, 1f); // Blue fill
            fill.parent.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f); // Grey background
            label.text = $"{morale}/{maxMorale}";
        }
        private VisualElement CreateUnitPanel(ICombatUnit unit, CharacterStats.DisplayStats stats, bool isHero, float heightPercent)
        {
            var panel = new VisualElement();
            panel.AddToClassList("unit-panel");
            panel.style.height = Length.Percent(heightPercent);
            var nameLabel = new Label(stats.name);
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(nameLabel);
            if (isHero && stats.isInfected)
            {
                var infectedLabel = new Label("Infected");
                infectedLabel.AddToClassList("infected-label");
                panel.Add(infectedLabel);
                infectedLabels[unit] = infectedLabel;
            }
            var healthBar = new VisualElement();
            healthBar.AddToClassList("health-bar");
            var healthFill = new VisualElement();
            healthFill.AddToClassList("health-fill");
            healthBar.Add(healthFill);
            var healthLabel = new Label();
            healthLabel.AddToClassList("health-label");
            healthBar.Add(healthLabel);
            UpdateHealthBar(healthFill, healthLabel, stats.health, stats.maxHealth);
            panel.Add(healthBar);
            VisualElement moraleBar = null;
            VisualElement moraleFill = null;
            Label moraleLabel = null;
            if (isHero)
            {
                moraleBar = new VisualElement();
                moraleBar.AddToClassList("morale-bar");
                moraleFill = new VisualElement();
                moraleFill.AddToClassList("morale-fill");
                moraleBar.Add(moraleFill);
                moraleLabel = new Label();
                moraleLabel.AddToClassList("morale-label");
                moraleBar.Add(moraleLabel);
                UpdateMoraleBar(moraleFill, moraleLabel, stats.morale, stats.maxMorale);
                panel.Add(moraleBar);
            }
            var rankLabel = new Label($"RANK: {stats.rank}");
            rankLabel.AddToClassList("rank-label");
            panel.Add(rankLabel);
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
            atkContainer.Add(evaLabel);
            statGrid.Add(evaContainer);
            panel.Add(statGrid);
            unitPanels[unit] = panel;
            unitStatLabels[unit] = (atkLabel, defLabel, spdLabel, evaLabel, moraleLabel, rankLabel);
            return panel;
        }
        private void InitializeCombat(EventBusSO.CombatInitData data)
        {
            unitPanels.Clear();
            unitStatLabels.Clear();
            infectedLabels.Clear();
            logMessages.Clear();
            logContent.Clear();
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
            }
        }
        private void HandleUnitUpdated(EventBusSO.UnitUpdateData data)
        {
            if (unitPanels.TryGetValue(data.unit, out VisualElement panel))
            {
                var healthBar = panel.Q<VisualElement>(className: "health-bar");
                if (healthBar != null)
                {
                    var fill = healthBar.Q<VisualElement>(className: "health-fill");
                    var label = healthBar.Q<Label>(className: "health-label");
                    if (fill != null && label != null)
                        UpdateHealthBar(fill, label, data.displayStats.health, data.displayStats.maxHealth);
                }
                if (data.displayStats.isHero)
                {
                    var moraleBar = panel.Q<VisualElement>(className: "morale-bar");
                    if (moraleBar != null)
                    {
                        var fill = moraleBar.Q<VisualElement>(className: "morale-fill");
                        var label = moraleBar.Q<Label>(className: "morale-label");
                        if (fill != null && label != null)
                            UpdateMoraleBar(fill, label, data.displayStats.morale, data.displayStats.maxMorale);
                    }
                    if (data.displayStats.isInfected && !infectedLabels.ContainsKey(data.unit))
                    {
                        var infectedLabel = new Label("Infected");
                        infectedLabel.AddToClassList("infected-label");
                        panel.Insert(1, infectedLabel);
                        infectedLabels[data.unit] = infectedLabel;
                    }
                    else if (!data.displayStats.isInfected && infectedLabels.ContainsKey(data.unit))
                    {
                        panel.Remove(infectedLabels[data.unit]);
                        infectedLabels.Remove(data.unit);
                    }
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
    }
}