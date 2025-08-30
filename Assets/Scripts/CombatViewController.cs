using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using VirulentVentures;

public class CombatViewController : MonoBehaviour
{
    [SerializeField] private VisualConfig visualConfig;
    [SerializeField] private UIConfig uiConfig;
    [SerializeField] private EventBusSO eventBus;
    [SerializeField] private CharacterPositions characterPositions;
    private VisualElement root;
    private List<(VisualElement panel, Label health, Label morale, ICombatUnit unit)> heroPanels = new List<(VisualElement, Label, Label, ICombatUnit)>();
    private List<(VisualElement panel, Label health, ICombatUnit unit)> monsterPanels = new List<(VisualElement, Label, ICombatUnit)>();
    private ListView combatLog;
    private List<string> logMessages = new List<string>();
    private Dictionary<ICombatUnit, GameObject> unitGameObjects = new Dictionary<ICombatUnit, GameObject>();

    void Awake()
    {
        if (!ValidateReferences()) return;
        SetupUI();
        eventBus.OnCombatInitialized += InitializeCombat;
        eventBus.OnUnitUpdated += UpdateUnit;
        eventBus.OnDamagePopup += ShowDamagePopup;
        eventBus.OnLogMessage += AddLogMessage;
    }

    void OnDestroy()
    {
        eventBus.OnCombatInitialized -= InitializeCombat;
        eventBus.OnUnitUpdated -= UpdateUnit;
        eventBus.OnDamagePopup -= ShowDamagePopup;
        eventBus.OnLogMessage -= AddLogMessage;
    }

    private void SetupUI()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
        {
            Debug.LogError("CombatViewController: UIDocument rootVisualElement is null!");
            return;
        }

        // Load background for combat-visuals
        var combatVisuals = root.Q<VisualElement>("combat-visuals");
        if (combatVisuals != null)
        {
            Sprite background = visualConfig.GetCombatBackground();
            if (background != null)
            {
                combatVisuals.style.backgroundImage = new StyleBackground(background);
                // Scale mode set in USS (#combat-visuals)
                // TODO: For final version, implement programmatic background generation
            }
            else
            {
                Debug.LogWarning("CombatViewController: Failed to load combat background sprite!");
            }
        }
        else
        {
            Debug.LogWarning("CombatViewController: combat-visuals element not found in UXML!");
        }

        // Hero Panels (Left, 30%)
        var heroContainer = root.Q<VisualElement>("hero-container");
        heroContainer.style.width = Length.Percent(30);
        heroContainer.style.position = Position.Absolute;
        heroContainer.style.left = 0;
        heroContainer.style.bottom = 0;
        heroContainer.style.height = Length.Percent(50);

        // Monster Panels (Right, 30%)
        var monsterContainer = root.Q<VisualElement>("monster-container");
        monsterContainer.style.width = Length.Percent(30);
        monsterContainer.style.position = Position.Absolute;
        monsterContainer.style.right = 0;
        monsterContainer.style.bottom = 0;
        monsterContainer.style.height = Length.Percent(50);

        // Combat Log (Center, 40%)
        combatLog = root.Q<ListView>("combat-log");
        combatLog.itemsSource = logMessages;
        combatLog.makeItem = () => new Label();
        combatLog.bindItem = (element, i) => {
            var label = (Label)element;
            label.text = logMessages[i];
            if (logMessages[i].Contains("damage"))
            {
                label.AddToClassList("damage");
            }
            else
            {
                label.RemoveFromClassList("damage");
            }
        };
        combatLog.style.width = Length.Percent(40);
        combatLog.style.position = Position.Absolute;
        combatLog.style.left = Length.Percent(30);
        combatLog.style.bottom = 0;
        combatLog.style.height = Length.Percent(50);
        combatLog.style.fontSize = 14;
        combatLog.AddToClassList("combat-log");
    }

    private void InitializeCombat(EventBusSO.CombatInitData data)
    {
        heroPanels.Clear();
        monsterPanels.Clear();
        unitGameObjects.Clear();
        logMessages.Clear();
        combatLog.Rebuild();

        var heroes = data.units.Where(u => u.stats.isHero).ToList();
        var monsters = data.units.Where(u => !u.stats.isHero).ToList();

        // Setup Hero Panels
        var heroContainer = root.Q<VisualElement>("hero-container");
        for (int i = 0; i < heroes.Count && i < characterPositions.heroPositions.Length; i++)
        {
            var unit = heroes[i].unit;
            var stats = heroes[i].stats;
            var panel = CreateUnitPanel(stats, true);
            heroContainer.Add(panel);
            heroPanels.Add((panel, panel.Q<Label>("health"), panel.Q<Label>("morale"), unit));

            var go = CreateUnitGameObject(unit, stats, true, characterPositions.heroPositions[i]);
            unitGameObjects[unit] = go;
        }

        // Setup Monster Panels
        var monsterContainer = root.Q<VisualElement>("monster-container");
        for (int i = 0; i < monsters.Count && i < characterPositions.monsterPositions.Length; i++)
        {
            var unit = monsters[i].unit;
            var stats = monsters[i].stats;
            var panel = CreateUnitPanel(stats, false);
            monsterContainer.Add(panel);
            monsterPanels.Add((panel, panel.Q<Label>("health"), unit));

            var go = CreateUnitGameObject(unit, stats, false, characterPositions.monsterPositions[i]);
            unitGameObjects[unit] = go;
        }
    }

    private VisualElement CreateUnitPanel(CharacterStats.DisplayStats stats, bool isHero)
    {
        var panel = new VisualElement();
        panel.AddToClassList(isHero ? "hero-panel" : "monster-panel");

        var portrait = new VisualElement { style = { backgroundImage = new StyleBackground(isHero ? visualConfig.GetPortrait(stats.name) : visualConfig.GetEnemySprite(stats.name)) } };
        panel.Add(portrait);

        var statsContainer = new VisualElement();
        statsContainer.AddToClassList("stats-container");

        var nameLabel = new Label(stats.name);
        nameLabel.AddToClassList("name-label");
        statsContainer.Add(nameLabel);

        var healthLabel = new Label($"Health: {stats.health}/{stats.maxHealth}");
        healthLabel.name = "health";
        healthLabel.AddToClassList("health-label");
        statsContainer.Add(healthLabel);

        if (isHero)
        {
            var moraleLabel = new Label($"Morale: {stats.morale}/{stats.maxMorale}");
            moraleLabel.name = "morale";
            moraleLabel.AddToClassList("morale-label");
            statsContainer.Add(moraleLabel);
        }

        panel.Add(statsContainer);
        return panel;
    }

    private GameObject CreateUnitGameObject(ICombatUnit unit, CharacterStats.DisplayStats stats, bool isHero, Vector3 position)
    {
        var go = new GameObject(stats.name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = isHero ? visualConfig.GetCombatSprite(stats.name) : visualConfig.GetEnemySprite(stats.name);
        go.transform.position = position;
        go.AddComponent<SpriteAnimation>();
        return go;
    }

    private void UpdateUnit(EventBusSO.UnitUpdateData data)
    {
        var heroPanel = heroPanels.FirstOrDefault(p => p.unit == data.unit);
        if (heroPanel.unit != null)
        {
            heroPanel.health.text = $"Health: {data.displayStats.health}/{data.displayStats.maxHealth}";
            heroPanel.morale.text = $"Morale: {data.displayStats.morale}/{data.displayStats.maxMorale}";
            if (data.displayStats.health <= 0 && unitGameObjects.TryGetValue(data.unit, out var go))
                go.SetActive(false);
            return;
        }

        var monsterPanel = monsterPanels.FirstOrDefault(p => p.unit == data.unit);
        if (monsterPanel.unit != null)
        {
            monsterPanel.health.text = $"Health: {data.displayStats.health}/{data.displayStats.maxHealth}";
            if (data.displayStats.health <= 0 && unitGameObjects.TryGetValue(data.unit, out var go))
                go.SetActive(false);
        }
    }

    private void ShowDamagePopup(EventBusSO.DamagePopupData data)
    {
        if (unitGameObjects.TryGetValue(data.unit, out var go))
        {
            go.GetComponent<SpriteAnimation>().Jiggle(false);
            logMessages.Add(data.message);
            combatLog.Rebuild();
        }
    }

    private void AddLogMessage(EventBusSO.LogData data)
    {
        logMessages.Add(data.message);
        combatLog.Rebuild();
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