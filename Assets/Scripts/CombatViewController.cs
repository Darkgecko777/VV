using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using VirulentVentures;

public class CombatViewController : MonoBehaviour
{
    [SerializeField] private VisualConfig visualConfig;
    [SerializeField] private UIConfig uiConfig;
    [SerializeField] private EventBusSO eventBus;
    [SerializeField] private CharacterPositions characterPositions;
    private VisualElement root;
    private Dictionary<ICombatUnit, GameObject> unitGameObjects = new Dictionary<ICombatUnit, GameObject>();
    private Dictionary<ICombatUnit, VisualElement> unitPanels = new Dictionary<ICombatUnit, VisualElement>();
    private Dictionary<ICombatUnit, (Label atk, Label def, Label spd, Label eva, Label morale)> unitStatLabels = new Dictionary<ICombatUnit, (Label, Label, Label, Label, Label)>();
    private GameObject backgroundGameObject;
    private VisualElement logContent;
    private List<Label> logMessages = new List<Label>();
    private const int MAX_LOG_MESSAGES = 20;

    void Awake()
    {
        if (!ValidateReferences()) return;
        SetupUI();
        eventBus.OnCombatInitialized += InitializeCombat;
        eventBus.OnUnitAttacking += HandleUnitAttacking;
        eventBus.OnUnitDamaged += HandleUnitDamaged;
        eventBus.OnLogMessage += HandleLogMessage;
        eventBus.OnUnitUpdated += HandleUnitUpdated;
        eventBus.OnUnitDied += HandleUnitDied;
        SetupBackground();
    }

    void OnDestroy()
    {
        eventBus.OnCombatInitialized -= InitializeCombat;
        eventBus.OnUnitAttacking -= HandleUnitAttacking;
        eventBus.OnUnitDamaged -= HandleUnitDamaged;
        eventBus.OnLogMessage -= HandleLogMessage;
        eventBus.OnUnitUpdated -= HandleUnitUpdated;
        eventBus.OnUnitDied -= HandleUnitDied;
        if (backgroundGameObject != null)
        {
            Destroy(backgroundGameObject);
        }
    }

    private void SetupUI()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
        {
            Debug.LogError("CombatViewController: UIDocument rootVisualElement is null! Ensure CombatScene.uxml is assigned to UIDocument in the Inspector.");
            return;
        }
        var combatRoot = root.Q<VisualElement>("combat-root");
        if (combatRoot == null)
        {
            Debug.LogError("CombatViewController: combat-root not found in UXML!");
            return;
        }
        var bottomPanel = combatRoot.Q<VisualElement>("bottom-panel");
        if (bottomPanel == null)
        {
            Debug.LogError("CombatViewController: bottom-panel not found in UXML!");
            return;
        }
        logContent = bottomPanel.Q<VisualElement>("log-content");
        if (logContent == null)
        {
            Debug.LogError("CombatViewController: log-content not found in UXML!");
            return;
        }
    }

    private void SetupBackground()
    {
        if (backgroundGameObject != null) Destroy(backgroundGameObject);
        var backgroundSprite = visualConfig.GetCombatBackground();
        if (backgroundSprite == null)
        {
            Debug.LogError("CombatViewController: Failed to load combat background sprite!");
            return;
        }
        backgroundGameObject = new GameObject("CombatBackground");
        var sr = backgroundGameObject.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;
        sr.sortingLayerName = "Background";
        sr.sortingOrder = 0;
        backgroundGameObject.transform.localScale = new Vector3(2.24f, 0.65f, 1f);
    }

    private void HandleLogMessage(EventBusSO.LogData logData)
    {
        var label = new Label(logData.message);
        label.style.color = logData.color;
        logContent.Add(label);
        logMessages.Add(label);
        if (logMessages.Count > MAX_LOG_MESSAGES)
        {
            var oldest = logMessages[0];
            logContent.Remove(oldest);
            logMessages.RemoveAt(0);
        }
        var scrollView = logContent.parent as ScrollView;
        if (scrollView != null)
        {
            scrollView.scrollOffset = new Vector2(0, float.MaxValue);
        }
    }

    private void InitializeCombat(EventBusSO.CombatInitData data)
    {
        unitGameObjects.Clear();
        unitPanels.Clear();
        unitStatLabels.Clear();
        var heroes = data.units.Where(u => u.stats.isHero).ToList();
        var monsters = data.units.Where(u => !u.stats.isHero).ToList();
        var leftPanel = root.Q<VisualElement>("left-panel");
        float heroPanelHeight = heroes.Count > 0 ? 100f / Mathf.Min(heroes.Count, 4) : 25f;
        for (int i = 0; i < heroes.Count && i < 4; i++)
        {
            var unit = heroes[i].unit;
            var stats = heroes[i].stats;
            var panel = CreateUnitPanel(unit, stats, true, heroPanelHeight);
            leftPanel.Add(panel);
            unitPanels[unit] = panel;
        }
        var rightPanel = root.Q<VisualElement>("right-panel");
        float monsterPanelHeight = monsters.Count > 0 ? 100f / Mathf.Min(monsters.Count, 4) : 25f;
        for (int i = 0; i < monsters.Count && i < 4; i++)
        {
            var unit = monsters[i].unit;
            var stats = monsters[i].stats;
            var panel = CreateUnitPanel(unit, stats, false, monsterPanelHeight);
            rightPanel.Add(panel);
            unitPanels[unit] = panel;
        }
        for (int i = 0; i < heroes.Count && i < characterPositions.heroPositions.Length; i++)
        {
            var unit = heroes[i].unit;
            var stats = heroes[i].stats;
            var go = CreateUnitGameObject(unit, stats, true, characterPositions.heroPositions[i]);
            unitGameObjects[unit] = go;
        }
        for (int i = 0; i < monsters.Count && i < characterPositions.monsterPositions.Length; i++)
        {
            var unit = monsters[i].unit;
            var stats = monsters[i].stats;
            var go = CreateUnitGameObject(unit, stats, false, characterPositions.monsterPositions[i]);
            unitGameObjects[unit] = go;
        }
    }

    private VisualElement CreateUnitPanel(ICombatUnit unit, CharacterStats.DisplayStats stats, bool isHero, float panelHeight)
    {
        var panel = new VisualElement();
        panel.AddToClassList("unit-panel");
        panel.style.height = new StyleLength(Length.Percent(panelHeight));
        panel.style.width = new StyleLength(Length.Percent(100));
        var nameLabel = new Label(stats.name);
        nameLabel.style.unityFont = uiConfig.PixelFont;
        nameLabel.style.color = uiConfig.TextColor;
        panel.Add(nameLabel);
        var healthBar = new VisualElement();
        healthBar.AddToClassList("health-bar");
        panel.Add(healthBar);
        var healthFill = new VisualElement();
        healthFill.AddToClassList("health-fill");
        healthBar.Add(healthFill);
        var healthLabel = new Label($"HP: {stats.health}/{stats.maxHealth}");
        healthLabel.AddToClassList("health-label");
        healthBar.Add(healthLabel);
        UpdateHealthBar(healthFill, healthLabel, stats.health, stats.maxHealth);
        var moraleBar = new VisualElement();
        moraleBar.AddToClassList("morale-bar");
        var moraleFill = new VisualElement();
        moraleFill.AddToClassList("morale-fill");
        moraleBar.Add(moraleFill);
        var moraleLabel = new Label($"Morale: {stats.morale}/{stats.maxMorale}");
        moraleLabel.AddToClassList("morale-label");
        moraleBar.Add(moraleLabel);
        UpdateMoraleBar(moraleFill, moraleLabel, stats.morale, stats.maxMorale);
        panel.Add(moraleBar);
        var statGrid = new VisualElement();
        statGrid.AddToClassList("stat-grid");
        panel.Add(statGrid);
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
        unitStatLabels[unit] = (atkLabel, defLabel, spdLabel, evaLabel, moraleLabel);
        return panel;
    }

    private void UpdateHealthBar(VisualElement fill, Label label, int current, int max)
    {
        float percent = max > 0 ? (float)current / max * 100f : 0f;
        fill.style.width = new StyleLength(Length.Percent(percent));
        label.text = $"HP: {current}/{max}";
    }

    private void UpdateMoraleBar(VisualElement fill, Label label, int current, int max)
    {
        float percent = max > 0 ? (float)current / max * 100f : 0f;
        fill.style.width = new StyleLength(Length.Percent(percent));
        label.text = $"Morale: {current}/{max}";
        fill.style.backgroundColor = new StyleColor(new Color(0.6f, 0.8f, 1f)); // Light blue
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
                {
                    UpdateHealthBar(fill, label, data.displayStats.health, data.displayStats.maxHealth);
                }
            }
            var moraleBar = panel.Q<VisualElement>(className: "morale-bar");
            if (moraleBar != null)
            {
                var fill = moraleBar.Q<VisualElement>(className: "morale-fill");
                var label = moraleBar.Q<Label>(className: "morale-label");
                if (fill != null && label != null)
                {
                    UpdateMoraleBar(fill, label, data.displayStats.morale, data.displayStats.maxMorale);
                }
            }
            if (unitStatLabels.TryGetValue(data.unit, out var statLabels))
            {
                statLabels.atk.text = $"A: {data.displayStats.attack}";
                statLabels.def.text = $"D: {data.displayStats.defense}";
                statLabels.spd.text = $"S: {data.displayStats.speed}";
                statLabels.eva.text = $"E: {data.displayStats.evasion}";
                statLabels.morale.text = $"Morale: {data.displayStats.morale}/{data.displayStats.maxMorale}";
            }
        }
    }

    private void HandleUnitAttacking(EventBusSO.AttackData data)
    {
        if (unitGameObjects.TryGetValue(data.attacker, out GameObject attackerGo))
        {
            var animator = attackerGo.GetComponent<SpriteAnimation>();
            if (animator != null)
            {
                bool isHero = data.attacker is CharacterStats charStats && charStats.Type == CharacterType.Hero;
                animator.TiltForward(isHero);
            }
        }
    }

    private void HandleUnitDamaged(EventBusSO.DamagePopupData data)
    {
        if (unitGameObjects.TryGetValue(data.unit, out GameObject targetGo))
        {
            var animator = targetGo.GetComponent<SpriteAnimation>();
            if (animator != null)
            {
                animator.Jiggle();
            }
        }
    }

    private void HandleUnitDied(ICombatUnit unit)
    {
        if (unitPanels.TryGetValue(unit, out VisualElement panel))
        {
            panel.style.opacity = 0.5f;
            var moraleBar = panel.Q<VisualElement>(className: "morale-bar");
            if (moraleBar != null && unit is CharacterStats stats && stats.Morale <= stats.MaxMorale * 0.2f)
            {
                panel.AddToClassList("low-morale");
            }
        }
        if (unitGameObjects.TryGetValue(unit, out GameObject go))
        {
            StartCoroutine(DeactivateAfterJiggle(go));
        }
    }

    private IEnumerator DeactivateAfterJiggle(GameObject go)
    {
        yield return new WaitForSeconds(0.3f);
        if (go != null)
        {
            go.SetActive(false);
        }
    }

    private GameObject CreateUnitGameObject(ICombatUnit unit, CharacterStats.DisplayStats stats, bool isHero, Vector3 position)
    {
        var go = new GameObject(stats.name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = isHero ? visualConfig.GetCombatSprite(stats.name) : visualConfig.GetEnemySprite(stats.name);
        sr.sortingLayerName = "Characters";
        sr.sortingOrder = 1;
        go.transform.position = position;
        go.transform.localScale = new Vector3(2f, 2f, 1f);
        go.AddComponent<SpriteAnimation>();
        return go;
    }

    private bool ValidateReferences()
    {
        if (visualConfig == null || uiConfig == null || eventBus == null || characterPositions == null)
        {
            Debug.LogError($"CombatViewController: Missing references! VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, EventBus: {eventBus != null}, CharacterPositions: {characterPositions != null}");
            return false;
        }
        return true;
    }
}