using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace VirulentVentures
{
    public class CombatViewController : MonoBehaviour
    {
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private CharacterPositions characterPositions;
        [SerializeField] private CombatConfig combatConfig;
        private VisualElement root;
        private Dictionary<ICombatUnit, GameObject> unitGameObjects = new Dictionary<ICombatUnit, GameObject>();
        private Dictionary<ICombatUnit, VisualElement> unitPanels = new Dictionary<ICombatUnit, VisualElement>();
        private Dictionary<ICombatUnit, (Label atk, Label def, Label spd, Label eva, Label morale)> unitStatLabels = new Dictionary<ICombatUnit, (Label, Label, Label, Label, Label)>();
        private Dictionary<ICombatUnit, Label> infectedLabels = new Dictionary<ICombatUnit, Label>();
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
            eventBus.OnUnitRetreated += HandleUnitRetreated;
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
            eventBus.OnUnitRetreated -= HandleUnitRetreated;
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
                return;
            }
            var combatRoot = root.Q<VisualElement>("combat-root");
            if (combatRoot == null)
            {
                return;
            }
            var bottomPanel = combatRoot.Q<VisualElement>("bottom-panel");
            if (bottomPanel == null)
            {
                return;
            }
            logContent = bottomPanel.Q<VisualElement>("log-content");
            if (logContent == null)
            {
                return;
            }
        }

        private void SetupBackground()
        {
            if (backgroundGameObject != null) Destroy(backgroundGameObject);
            var backgroundSprite = visualConfig.GetCombatBackground();
            if (backgroundSprite == null)
            {
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

        private void UpdateHealthBar(VisualElement fill, Label label, int health, int maxHealth)
        {
            float healthPercent = maxHealth > 0 ? (float)health / maxHealth : 0;
            fill.style.width = Length.Percent(healthPercent * 100);
            label.text = $"{health}/{maxHealth}";
        }

        private void UpdateMoraleBar(VisualElement fill, Label label, int morale, int maxMorale)
        {
            float moralePercent = maxMorale > 0 ? (float)morale / maxMorale : 0;
            fill.style.width = Length.Percent(moralePercent * 100);
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

            panel.Add(statGrid);

            unitPanels[unit] = panel;
            unitStatLabels[unit] = (atkLabel, defLabel, spdLabel, evaLabel, moraleLabel);

            return panel;
        }

        private void InitializeCombat(EventBusSO.CombatInitData data)
        {
            unitGameObjects.Clear();
            unitPanels.Clear();
            unitStatLabels.Clear();
            infectedLabels.Clear();
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
                if (data.displayStats.isHero)
                {
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
                    // Update infected label
                    if (data.displayStats.isInfected && !infectedLabels.ContainsKey(data.unit))
                    {
                        var infectedLabel = new Label("Infected");
                        infectedLabel.AddToClassList("infected-label");
                        panel.Insert(1, infectedLabel); // Insert below name
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
                    if (data.displayStats.isHero && statLabels.morale != null)
                    {
                        statLabels.morale.text = $"Morale: {data.displayStats.morale}/{data.displayStats.maxMorale}";
                    }
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
                    animator.TiltForward(isHero, combatConfig.CombatSpeed);
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
                    animator.Jiggle(combatConfig.CombatSpeed);
                }
            }
        }

        private void HandleUnitDied(ICombatUnit unit)
        {
            if (unitPanels.TryGetValue(unit, out VisualElement panel))
            {
                panel.style.opacity = 0.5f;
                if (unit is CharacterStats stats && stats.Type == CharacterType.Hero && stats.Morale <= stats.MaxMorale * 0.2f)
                {
                    panel.AddToClassList("low-morale");
                }
            }
            if (unitGameObjects.TryGetValue(unit, out GameObject go))
            {
                StartCoroutine(DeactivateAfterJiggle(go));
            }
        }

        private void HandleUnitRetreated(ICombatUnit unit)
        {
            if (unitPanels.TryGetValue(unit, out VisualElement panel))
            {
                panel.style.opacity = 0.5f;
                if (unit is CharacterStats stats && stats.Type == CharacterType.Hero)
                {
                    panel.AddToClassList("retreat-slide");
                }
            }
            if (unitGameObjects.TryGetValue(unit, out GameObject go))
            {
                StartCoroutine(DeactivateAfterFade(go));
            }
        }

        private IEnumerator DeactivateAfterJiggle(GameObject go)
        {
            yield return new WaitForSeconds(0.3f / combatConfig.CombatSpeed);
            if (go != null)
            {
                go.SetActive(false);
            }
        }

        private IEnumerator DeactivateAfterFade(GameObject go)
        {
            yield return new WaitForSeconds(0.3f / combatConfig.CombatSpeed);
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
            if (visualConfig == null || uiConfig == null || eventBus == null || characterPositions == null || combatConfig == null)
            {
                Debug.LogError("CombatViewController: Missing required reference(s). Please assign in the Inspector.");
                return false;
            }
            return true;
        }
    }
}