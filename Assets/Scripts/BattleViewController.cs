using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class BattleViewController : MonoBehaviour
    {
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CharacterPositions characterPositions;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private EventBusSO eventBus;

        private VisualElement root;
        private VisualElement fadePanel;
        private VisualElement heroesContainer;
        private VisualElement monstersContainer;
        private VisualElement combatLogContainer;
        private Button continueButton;
        private Dictionary<ICombatUnit, VisualElement> unitPanels;
        private List<(ICombatUnit unit, GameObject go, SpriteAnimation animator)> units;
        private GameObject backgroundObject;
        private float fadeDuration = 0.5f;
        private bool isInitialized;

        void Awake()
        {
            if (!ValidateReferences())
            {
                isInitialized = false;
                return;
            }

            mainCamera.orthographic = true;
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.transform.position = new Vector3(0f, 0f, -8f);

            root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogWarning("BattleViewController: rootVisualElement is null in Awake, will retry in Start.");
                return;
            }

            units = new List<(ICombatUnit, GameObject, SpriteAnimation)>();
            unitPanels = new Dictionary<ICombatUnit, VisualElement>();
            isInitialized = true;

            InitializeUIElements();
            InitializeBackground();
        }

        void Start()
        {
            if (!isInitialized && uiDocument != null)
            {
                root = uiDocument.rootVisualElement;
                if (root == null)
                {
                    Debug.LogError("BattleViewController: Failed to initialize rootVisualElement in Start!");
                    return;
                }
                InitializeUIElements();
                InitializeBackground();
            }

            if (isInitialized)
            {
                SubscribeToEventBus();
            }
        }

        void OnDestroy()
        {
            if (backgroundObject != null)
            {
                Destroy(backgroundObject);
                Debug.Log("BattleViewController: Destroyed backgroundObject");
            }
            if (eventBus != null)
            {
                UnsubscribeFromEventBus();
            }
            if (root != null)
            {
                root.Clear(); // Explicitly clear UI Toolkit elements
                Debug.Log("BattleViewController: Cleared root VisualElement on destroy");
            }
            unitPanels.Clear();
            units.Clear();
            heroesContainer = null;
            monstersContainer = null;
            combatLogContainer = null;
            continueButton = null;
            fadePanel = null;
        }

        private void InitializeUIElements()
        {
            heroesContainer = root.Q<VisualElement>("HeroesContainer");
            monstersContainer = root.Q<VisualElement>("MonstersContainer");
            combatLogContainer = root.Q<VisualElement>("CombatLogContainer");
            continueButton = root.Q<Button>("ContinueButton");
            fadePanel = root.Q<VisualElement>("FadePanel") ?? new VisualElement { name = "FadePanel" };

            if (heroesContainer == null || monstersContainer == null || combatLogContainer == null || continueButton == null)
            {
                Debug.LogError($"BattleViewController: Missing UI elements! HeroesContainer: {heroesContainer != null}, MonstersContainer: {monstersContainer != null}, CombatLogContainer: {combatLogContainer != null}, ContinueButton: {continueButton != null}");
                isInitialized = false;
                return;
            }

            fadePanel.style.backgroundColor = uiConfig.BogRotColor;
            fadePanel.style.opacity = 0;
            fadePanel.style.position = Position.Absolute;
            fadePanel.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            fadePanel.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            root.Add(fadePanel);

            combatLogContainer.Clear();
            continueButton.style.color = uiConfig.TextColor;
            continueButton.style.unityFont = uiConfig.PixelFont;
            continueButton.clicked += () => eventBus.RaiseBattleEnded();
            continueButton.SetEnabled(false);
        }

        private void InitializeBackground()
        {
            if (backgroundSprite != null)
            {
                backgroundObject = new GameObject("BattleBackground");
                var renderer = backgroundObject.AddComponent<SpriteRenderer>();
                renderer.sprite = backgroundSprite;
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = -10;
                backgroundObject.transform.localScale = new Vector3(2.25f, 0.625f, 1f);
                backgroundObject.transform.position = new Vector3(0f, 0f, 0f);
            }
            else
            {
                Debug.LogWarning("BattleViewController: No backgroundSprite assigned!");
            }
        }

        public void InitializeUnits(EventBusSO.BattleInitData data)
        {
            var combatUnits = data.units;
            if (!isInitialized) return;

            heroesContainer.Clear();
            monstersContainer.Clear();
            unitPanels.Clear();
            units.Clear();

            var heroPositions = characterPositions.heroPositions;
            var monsterPositions = characterPositions.monsterPositions;
            int heroIndex = 0;
            int monsterIndex = 0;

            foreach (var (unit, _, stats) in combatUnits)
            {
                if (unit.Health <= 0) continue;

                if (string.IsNullOrEmpty(stats.name))
                {
                    Debug.LogWarning($"BattleViewController: Invalid DisplayStats for unit {unit.Id}");
                    continue;
                }

                VisualElement panel = CreateUnitPanel(stats);
                unitPanels[unit] = panel;
                if (stats.isHero)
                    heroesContainer.Add(panel);
                else
                    monstersContainer.Add(panel);

                GameObject unitObj = new GameObject(unit.Id);
                var renderer = unitObj.AddComponent<SpriteRenderer>();
                renderer.sortingLayerName = stats.isHero ? "Heroes" : "Monsters";
                renderer.sortingOrder = stats.isHero ? heroIndex++ : monsterIndex++;
                var animator = unitObj.AddComponent<SpriteAnimation>();
                Vector3 position = stats.isHero ? heroPositions[unit.PartyPosition - 1] : monsterPositions[unit.PartyPosition];
                unitObj.transform.position = position;
                units.Add((unit, unitObj, animator));
            }
        }

        private VisualElement CreateUnitPanel(CharacterStats.DisplayStats stats)
        {
            if (string.IsNullOrEmpty(stats.name))
            {
                Debug.LogWarning($"BattleViewController: Invalid DisplayStats for unit, name is empty");
                return new VisualElement();
            }

            VisualElement panel = new VisualElement();
            panel.AddToClassList(stats.isHero ? "hero-panel" : "monster-panel");

            VisualElement healthBar = new VisualElement();
            healthBar.AddToClassList("health-bar");
            VisualElement healthFill = new VisualElement();
            healthFill.AddToClassList(stats.isHero ? "health-fill-hero" : "health-fill-monster");
            healthFill.style.width = new StyleLength(new Length((float)stats.health / stats.maxHealth * 100, LengthUnit.Percent));
            healthBar.Add(healthFill);
            panel.Add(healthBar);

            Label nameLabel = new Label(stats.name);
            nameLabel.AddToClassList("name-label");
            panel.Add(nameLabel);

            if (stats.isHero)
            {
                Label moraleLabel = new Label($"Morale: {stats.morale}/{stats.maxMorale}");
                moraleLabel.AddToClassList("stat-label");
                moraleLabel.name = "morale-label";
                panel.Add(moraleLabel);
            }

            Label attackLabel = new Label($"Attack: {stats.attack}");
            attackLabel.AddToClassList("stat-label");
            attackLabel.name = "attack-label";
            panel.Add(attackLabel);

            Label defenseLabel = new Label($"Defense: {stats.defense}");
            defenseLabel.AddToClassList("stat-label");
            defenseLabel.name = "defense-label";
            panel.Add(defenseLabel);

            Label speedLabel = new Label($"Speed: {stats.speed}");
            speedLabel.AddToClassList("stat-label");
            speedLabel.name = "speed-label";
            panel.Add(speedLabel);

            Label evasionLabel = new Label($"Evasion: {stats.evasion}");
            evasionLabel.AddToClassList("stat-label");
            evasionLabel.name = "evasion-label";
            panel.Add(evasionLabel);

            return panel;
        }

        private void UpdateUnitPanel(EventBusSO.UnitUpdateData data)
        {
            if (!isInitialized || !unitPanels.TryGetValue(data.unit, out var panel)) return;

            var stats = data.displayStats;
            if (string.IsNullOrEmpty(stats.name))
            {
                Debug.LogWarning($"BattleViewController: Invalid DisplayStats for unit {data.unit.Id} in UpdateUnitPanel");
                return;
            }

            var healthFill = panel.Q<VisualElement>(className: stats.isHero ? "health-fill-hero" : "health-fill-monster");
            healthFill.style.width = new StyleLength(new Length((float)stats.health / stats.maxHealth * 100, LengthUnit.Percent));

            var nameLabel = panel.Q<Label>("name-label");
            nameLabel.text = stats.name;

            if (stats.isHero)
            {
                var moraleLabel = panel.Q<Label>("morale-label");
                moraleLabel.text = $"Morale: {stats.morale}/{stats.maxMorale}";
            }

            var attackLabel = panel.Q<Label>("attack-label");
            attackLabel.text = $"Attack: {stats.attack}";

            var defenseLabel = panel.Q<Label>("defense-label");
            defenseLabel.text = $"Defense: {stats.defense}";

            var speedLabel = panel.Q<Label>("speed-label");
            speedLabel.text = $"Speed: {stats.speed}";

            var evasionLabel = panel.Q<Label>("evasion-label");
            evasionLabel.text = $"Evasion: {stats.evasion}";
        }

        private void ShowDamagePopup(EventBusSO.DamagePopupData data)
        {
            if (!isInitialized || root == null || data.unit == null) return;

            var unitEntry = units.Find(u => u.unit == data.unit);
            if (unitEntry.go == null) return;

            Vector3 worldPosition = unitEntry.go.transform.position;
            Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            float screenHeight = mainCamera.pixelHeight;

            VisualElement popup = new Label(data.message);
            popup.AddToClassList("damage-popup");
            popup.style.position = Position.Absolute;
            popup.style.left = screenPosition.x;
            popup.style.top = screenHeight - screenPosition.y - 50;
            root.Add(popup);
            StartCoroutine(AnimatePopup(popup, screenHeight));
        }

        private void TriggerUnitAnimation(EventBusSO.DamagePopupData data)
        {
            if (!isInitialized) return;
            var unitEntry = units.Find(u => u.unit == data.unit);
            if (unitEntry.animator != null)
            {
                unitEntry.animator.Jiggle(data.unit is CharacterStats cs ? cs.IsHero : false);
            }
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

        private void LogMessage(EventBusSO.LogData data)
        {
            if (!isInitialized) return;

            Label logEntry = new Label(data.message);
            logEntry.AddToClassList("combat-log");
            logEntry.style.color = data.color;
            combatLogContainer.Add(logEntry);

            var children = combatLogContainer.Children().ToList();
            if (children.Count > 10)
            {
                combatLogContainer.Remove(children.First());
            }
        }

        public void FadeToScene()
        {
            StartCoroutine(FadeToExpedition());
        }

        private IEnumerator FadeToExpedition()
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
            fadePanel.style.opacity = 0;
            fadePanel.style.display = DisplayStyle.None;
        }

        private void EnableContinueButton()
        {
            continueButton.SetEnabled(true);
        }

        private void SubscribeToEventBus()
        {
            eventBus.OnLogMessage += LogMessage;
            eventBus.OnUnitUpdated += UpdateUnitPanel;
            eventBus.OnDamagePopup += ShowDamagePopup;
            eventBus.OnDamagePopup += TriggerUnitAnimation;
            eventBus.OnBattleEnded += EnableContinueButton;
            eventBus.OnBattleEnded += FadeToScene;
            eventBus.OnBattleInitialized += InitializeUnits;
        }

        private void UnsubscribeFromEventBus()
        {
            eventBus.OnLogMessage -= LogMessage;
            eventBus.OnUnitUpdated -= UpdateUnitPanel;
            eventBus.OnDamagePopup -= ShowDamagePopup;
            eventBus.OnDamagePopup -= TriggerUnitAnimation;
            eventBus.OnBattleEnded -= EnableContinueButton;
            eventBus.OnBattleEnded -= FadeToScene;
            eventBus.OnBattleInitialized -= InitializeUnits;
            Debug.Log("BattleViewController: Unsubscribed from EventBusSO");
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || uiConfig == null || visualConfig == null || mainCamera == null || characterPositions == null || eventBus == null)
            {
                Debug.LogError($"BattleViewController: Missing references! UIDocument: {uiDocument != null}, UIConfig: {uiConfig != null}, VisualConfig: {visualConfig != null}, MainCamera: {mainCamera != null}, CharacterPositions: {characterPositions != null}, EventBus: {eventBus != null}");
                return false;
            }
            return true;
        }
    }
}