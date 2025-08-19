using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIConfig uiConfig;
    [SerializeField] private PartyData partyData;
    [SerializeField] private EncounterData encounterData;
    private VisualElement root;
    private RectTransform canvasRectTransform;
    private Camera mainCamera;
    private Dictionary<CharacterRuntimeStats, VisualElement> unitPanels;
    private Label combatLog;

    void Start()
    {
        root = GetComponent<UIDocument>()?.rootVisualElement;
        canvasRectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        if (root == null || canvasRectTransform == null || uiConfig == null || mainCamera == null || partyData == null || encounterData == null)
        {
            Debug.LogError($"UIManager: Missing required components! Root: {root != null}, Canvas: {canvasRectTransform != null}, UIConfig: {uiConfig != null}, Camera: {mainCamera != null}, PartyData: {partyData != null}, EncounterData: {encounterData != null}");
            return;
        }

        unitPanels = new Dictionary<CharacterRuntimeStats, VisualElement>();
        List<CharacterRuntimeStats> heroes = partyData.GetHeroes();
        List<CharacterRuntimeStats> monsters = encounterData.SpawnMonsters();

        if (heroes == null || monsters == null)
        {
            Debug.LogError($"UIManager: Heroes or Monsters list is null! Heroes: {heroes == null}, Monsters: {monsters == null}");
            return;
        }

        Debug.Log($"UIManager: Initializing {heroes.Count} heroes and {monsters.Count} monsters");
        SetupUnitPanels(heroes, "HeroesContainer", "Hero");
        SetupUnitPanels(monsters, "MonstersContainer", "Monster");

        foreach (var hero in heroes)
        {
            if (hero != null && hero.Stats.characterType != CharacterStatsData.CharacterType.Ghoul && hero.Stats.characterType != CharacterStatsData.CharacterType.Wraith)
            {
                UpdateUnitUI(null, hero);
                Debug.Log($"UIManager: Initialized UI for hero {hero.gameObject.name} (Type: {hero.Stats.characterType})");
            }
            else
            {
                Debug.LogWarning($"UIManager: Skipping invalid hero: {hero?.gameObject?.name ?? "null"} (Type: {hero?.Stats.characterType.ToString() ?? "null"})");
            }
        }
        foreach (var monster in monsters)
        {
            if (monster != null && (monster.Stats.characterType == CharacterStatsData.CharacterType.Ghoul || monster.Stats.characterType == CharacterStatsData.CharacterType.Wraith))
            {
                UpdateUnitUI(null, monster);
                Debug.Log($"UIManager: Initialized UI for monster {monster.gameObject.name} (Type: {monster.Stats.characterType})");
            }
            else
            {
                Debug.LogWarning($"UIManager: Skipping invalid monster: {monster?.gameObject?.name ?? "null"} (Type: {monster?.Stats.characterType.ToString() ?? "null"})");
            }
        }

        combatLog = root.Q<Label>("CombatLog");
        if (combatLog == null)
        {
            Debug.LogError("UIManager: CombatLog label not found!");
            return;
        }
        combatLog.style.display = DisplayStyle.Flex;
        combatLog.text = "";
    }

    private void SetupUnitPanels(List<CharacterRuntimeStats> characters, string containerName, string panelPrefix)
    {
        VisualElement container = root.Q<VisualElement>(containerName);
        if (container == null)
        {
            Debug.LogError($"UIManager: Container {containerName} not found in BattleSceneUI.uxml!");
            // Log all available containers for debugging
            var containers = root.Query<VisualElement>().ToList();
            Debug.Log($"UIManager: Available containers: {string.Join(", ", containers.Select(c => c.name))}");
            return;
        }
        Debug.Log($"UIManager: Found container {containerName}, searching for panels with prefix {panelPrefix}");
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] == null)
            {
                Debug.LogWarning($"UIManager: Null character at index {i} in {containerName}");
                continue;
            }
            string panelName = $"{panelPrefix}{i + 1}";
            VisualElement panel = container.Q<VisualElement>(panelName);
            if (panel == null)
            {
                Debug.LogWarning($"UIManager: Panel {panelName} not found in {containerName} for Character: {characters[i].gameObject.name} (Type: {characters[i].Stats.characterType})");
                // Log all available panels in container
                var panels = container.Query<VisualElement>().ToList();
                Debug.Log($"UIManager: Available panels in {containerName}: {string.Join(", ", panels.Select(p => p.name))}");
                continue;
            }
            unitPanels[characters[i]] = panel;
            Debug.Log($"UIManager: Mapped {panelName} to {characters[i].gameObject.name} (Type: {characters[i].Stats.characterType})");
        }
    }

    public void UpdateUnitUI(CharacterRuntimeStats attacker, CharacterRuntimeStats target)
    {
        if (target == null)
        {
            Debug.LogWarning($"UIManager: Invalid target (null) in UpdateUnitUI");
            return;
        }

        VisualElement panel;
        if (!unitPanels.TryGetValue(target, out panel))
        {
            Debug.LogWarning($"UIManager: Missing panel for {target.gameObject.name} (Type: {target.Stats.characterType})");
            return;
        }

        Label hpLabel = panel.Q<Label>($"{panel.name}_HP");
        VisualElement healthBar = panel.Q<VisualElement>($"{panel.name}_HealthBar");
        VisualElement healthFill = healthBar?.Q<VisualElement>($"{panel.name}_HealthFill");
        Label atkLabel = panel.Q<Label>($"{panel.name}_ATK");
        Label defLabel = panel.Q<Label>($"{panel.name}_DEF");
        Label moraleLabel = panel.Q<Label>($"{panel.name}_Morale");
        Label sanityLabel = panel.Q<Label>($"{panel.name}_Sanity");

        if (hpLabel == null || healthBar == null || healthFill == null || atkLabel == null || defLabel == null || moraleLabel == null)
        {
            Debug.LogWarning($"UIManager: Missing UI elements for {panel.name} (Type: {target.Stats.characterType}). HP: {hpLabel != null}, HealthBar: {healthBar != null}, HealthFill: {healthFill != null}, ATK: {atkLabel != null}, DEF: {defLabel != null}, Morale: {moraleLabel != null}, Sanity: {sanityLabel != null}");
            return;
        }

        int currentHealth = (int)Mathf.Round(target.Stats.health);
        int maxHealth = (int)Mathf.Round(target.Stats.maxHealth);
        hpLabel.text = $"HP: {currentHealth}/{maxHealth}";
        float healthPercent = (float)currentHealth / maxHealth;
        healthFill.style.width = new StyleLength(new Length(healthPercent * 180, LengthUnit.Pixel));
        healthFill.style.height = new StyleLength(new Length(30, LengthUnit.Pixel));
        healthFill.style.backgroundColor = new StyleColor(
            target.Stats.characterType == CharacterStatsData.CharacterType.Ghoul ||
            target.Stats.characterType == CharacterStatsData.CharacterType.Wraith ?
            new Color(1, 0, 0) : new Color(0, 1, 0)
        );
        healthFill.style.display = DisplayStyle.Flex;
        healthFill.style.opacity = 1;
        healthFill.style.position = Position.Absolute;
        healthFill.style.left = 0;
        healthFill.style.top = 0;

        atkLabel.text = $"ATK: {(int)Mathf.Round(target.Stats.attack)}";
        defLabel.text = $"DEF: {(int)Mathf.Round(target.Stats.defense)}";
        moraleLabel.text = $"Morale: {(int)Mathf.Round(target.Stats.morale)}";

        if (sanityLabel != null)
        {
            sanityLabel.text = $"Sanity: {(int)Mathf.Round(target.Stats.sanity)}";
        }
    }

    public void ShowPopup(CharacterRuntimeStats character, string message)
    {
        if (character == null || mainCamera == null || root == null)
        {
            Debug.LogWarning($"UIManager: Cannot show popup. Character: {character != null}, Camera: {mainCamera != null}, Root: {root != null}");
            return;
        }

        Vector3 worldPos = character.transform.position + Vector3.up * 1.5f;
        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        Vector2 panelPos = new Vector2(
            (screenPos.x / Screen.width) * 1920,
            ((Screen.height - screenPos.y) / Screen.height) * 540
        );
        panelPos.x = Mathf.Clamp(panelPos.x - 50, 0, 1920);
        panelPos.y = Mathf.Clamp(panelPos.y - 20, 0, 540);
        Label popup = new Label
        {
            text = message,
            style = {
                position = Position.Absolute,
                left = panelPos.x,
                top = panelPos.y,
                color = uiConfig != null ? uiConfig.TextColor : Color.white,
                unityTextOutlineColor = uiConfig != null ? uiConfig.TextOutlineColor : Color.black,
                fontSize = 16,
                unityFont = uiConfig != null && uiConfig.PixelFont != null ? uiConfig.PixelFont : null,
                unityTextAlign = TextAnchor.MiddleCenter,
                backgroundColor = new StyleColor(Color.black),
                paddingLeft = 10,
                paddingRight = 10,
                paddingTop = 5,
                paddingBottom = 5,
                unityBackgroundImageTintColor = new StyleColor(Color.white),
                display = DisplayStyle.Flex
            }
        };
        root.Add(popup);
        StartCoroutine(AnimatePopup(popup));

        LogMessage(message);
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
        if (combatLog != null)
        {
            combatLog.text += $"{message}\n";
            string[] lines = combatLog.text.Split('\n');
            if (lines.Length > 10)
            {
                combatLog.text = string.Join("\n", lines, lines.Length - 10, 10);
            }
        }
    }
}