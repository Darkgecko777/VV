using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIConfig uiConfig;
    [SerializeField] private PartyData partyData;
    [SerializeField] private EncounterData encounterData;
    private VisualElement root;
    private RectTransform canvasRectTransform;
    private Camera mainCamera;
    private Dictionary<CharacterRuntimeStats, VisualElement> unitPanels;
    private Label battleLog;

    void Start()
    {
        root = GetComponent<UIDocument>()?.rootVisualElement;
        canvasRectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        if (root == null || canvasRectTransform == null || uiConfig == null || mainCamera == null || partyData == null || encounterData == null)
        {
            Debug.LogError("UIManager: Missing required components or references!");
            return;
        }

        unitPanels = new Dictionary<CharacterRuntimeStats, VisualElement>();
        SetupUnitPanels(partyData.GetHeroes(), "HeroesContainer", "Hero");
        SetupUnitPanels(encounterData.SpawnMonsters(), "MonstersContainer", "Monster");

        foreach (var hero in partyData.GetHeroes())
        {
            if (hero != null && hero.Stats.characterType != CharacterStatsData.CharacterType.Ghoul && hero.Stats.characterType != CharacterStatsData.CharacterType.Wraith)
            {
                hero.OnInfected.AddListener((target) => ShowPopup(hero, $"{hero.Stats.characterType} Infected!"));
                UpdateUnitUI(null, hero);
            }
        }
        foreach (var monster in encounterData.SpawnMonsters())
        {
            if (monster != null && (monster.Stats.characterType == CharacterStatsData.CharacterType.Ghoul || monster.Stats.characterType == CharacterStatsData.CharacterType.Wraith))
            {
                monster.OnInfected.AddListener((target) => ShowPopup(monster, $"{monster.Stats.characterType} Infected!"));
                UpdateUnitUI(null, monster);
            }
        }

        // Setup battle log
        battleLog = root.Q<Label>("BattleLog");
        if (battleLog != null)
        {
            battleLog.style.display = DisplayStyle.Flex;
            battleLog.text = "";
        }
    }

    private void SetupUnitPanels(List<CharacterRuntimeStats> characters, string containerName, string panelPrefix)
    {
        VisualElement container = root.Q<VisualElement>(containerName);
        if (container == null)
        {
            Debug.LogError($"UIManager: {containerName} not found in UI Document!");
            return;
        }
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] != null)
            {
                string panelName = $"{panelPrefix}{i + 1}";
                VisualElement panel = container.Q<VisualElement>(panelName);
                if (panel != null)
                {
                    unitPanels[characters[i]] = panel;
                }
            }
        }
    }

    public void UpdateUnitUI(CharacterRuntimeStats attacker, CharacterRuntimeStats target)
    {
        if (target == null || !unitPanels.ContainsKey(target))
        {
            return;
        }

        VisualElement panel = unitPanels[target];
        Label hpLabel = panel.Q<Label>($"{panel.name}_HP");
        VisualElement healthBar = panel.Q<VisualElement>($"{panel.name}_HealthBar");
        VisualElement healthFill = healthBar?.Q<VisualElement>($"{panel.name}_HealthFill");

        if (hpLabel == null || healthFill == null)
        {
            return;
        }

        hpLabel.text = $"HP: {Mathf.Round(target.Stats.health)}/{target.Stats.maxHealth}";
        float healthPercent = target.Stats.health / target.Stats.maxHealth;
        healthFill.style.width = new StyleLength(new Length(healthPercent * 200, LengthUnit.Pixel));
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
    }

    public void ShowPopup(CharacterRuntimeStats character, string message)
    {
        if (character == null || mainCamera == null || root == null)
        {
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

        // Log to battle log
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
        if (battleLog != null)
        {
            battleLog.text += $"{message}\n";
            // Limit log to last 10 messages (adjustable)
            string[] lines = battleLog.text.Split('\n');
            if (lines.Length > 10)
            {
                battleLog.text = string.Join("\n", lines, lines.Length - 10, 10);
            }
        }
    }
}