using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIConfig uiConfig;
    [SerializeField] private List<BaseCharacterStats> fighters;
    [SerializeField] private List<BaseCharacterStats> ghouls;
    private VisualElement root;
    private RectTransform canvasRectTransform;
    private Camera mainCamera;
    private Dictionary<BaseCharacterStats, VisualElement> unitPanels;

    void Start()
    {
        root = GetComponent<UIDocument>()?.rootVisualElement;
        canvasRectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        if (root == null || canvasRectTransform == null || uiConfig == null || mainCamera == null)
        {
            return;
        }

        unitPanels = new Dictionary<BaseCharacterStats, VisualElement>();
        SetupUnitPanels(fighters, "HeroesContainer", "Hero");
        SetupUnitPanels(ghouls, "MonstersContainer", "Monster");

        foreach (var fighter in fighters)
        {
            if (fighter != null && fighter.characterType == BaseCharacterStats.CharacterType.Fighter)
            {
                fighter.OnInfected.AddListener((target) => ShowPopup(fighter, "Hero Infected!"));
                UpdateUnitUI(null, fighter);
            }
        }
        foreach (var ghoul in ghouls)
        {
            if (ghoul != null && ghoul.characterType == BaseCharacterStats.CharacterType.Ghoul)
            {
                ghoul.OnInfected.AddListener((target) => ShowPopup(ghoul, "Ghoul Infected!"));
                UpdateUnitUI(null, ghoul);
            }
        }
    }

    private void SetupUnitPanels(List<BaseCharacterStats> characters, string containerName, string panelPrefix)
    {
        VisualElement container = root.Q<VisualElement>(containerName);
        if (container == null)
        {
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

    public void UpdateUnitUI(BaseCharacterStats attacker, BaseCharacterStats target)
    {
        if (target == null || !unitPanels.ContainsKey(target))
        {
            return;
        }

        VisualElement panel = unitPanels[target];
        Label hpLabel = panel.Q<Label>($"{panel.name}_HP");
        if (hpLabel == null)
        {
            return;
        }

        hpLabel.text = $"HP: {Mathf.Round(target.health)}/{target.maxHealth}";
        VisualElement healthFill = panel.Q<VisualElement>($"{panel.name}_HealthFill");
        if (healthFill == null)
        {
            return;
        }

        float healthPercent = target.health / target.maxHealth;
        healthFill.style.width = new StyleLength(new Length(healthPercent * 200, LengthUnit.Pixel));
        healthFill.style.height = new StyleLength(new Length(30, LengthUnit.Pixel));
        healthFill.style.backgroundColor = new StyleColor(target.characterType == BaseCharacterStats.CharacterType.Fighter ? new Color(0, 1, 0) : new Color(1, 0, 0));
        healthFill.style.display = DisplayStyle.Flex;
        healthFill.style.opacity = 1;
        healthFill.style.position = Position.Absolute;
        healthFill.style.left = 0;
        healthFill.style.top = 0;
    }

    public void ShowPopup(BaseCharacterStats character, string message)
    {
        if (character == null || mainCamera == null || root == null)
        {
            return;
        }

        Vector3 worldPos = character.transform.position + Vector3.up * 1.5f;
        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        // Normalize screen position to [0,1], flip Y, scale to canvas (1920x540)
        Vector2 panelPos = new Vector2(
            (screenPos.x / Screen.width) * 1920,
            ((Screen.height - screenPos.y) / Screen.height) * 540
        );
        // Clamp to canvas bounds
        panelPos.x = Mathf.Clamp(panelPos.x - 50, 0, 1920); // Offset left for centering
        panelPos.y = Mathf.Clamp(panelPos.y - 20, 0, 540); // Offset up for above sprite
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
}