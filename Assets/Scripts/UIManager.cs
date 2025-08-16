using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIConfig uiConfig;
    [SerializeField] private List<CharacterStats> fighters;
    [SerializeField] private List<CharacterStats> ghouls;
    private VisualElement root;
    private RectTransform canvasRectTransform;
    private Camera mainCamera;
    private Dictionary<CharacterStats, VisualElement> unitPanels;

    void Start()
    {
        root = GetComponent<UIDocument>()?.rootVisualElement;
        canvasRectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        if (root == null || canvasRectTransform == null || uiConfig == null || mainCamera == null)
        {
            return;
        }

        unitPanels = new Dictionary<CharacterStats, VisualElement>();
        SetupUnitPanels(fighters, "HeroesContainer", "Hero");
        SetupUnitPanels(ghouls, "MonstersContainer", "Monster");

        foreach (var fighter in fighters)
        {
            if (fighter != null && fighter.Type == CharacterStats.CharacterType.Fighter)
            {
                fighter.OnInfected.AddListener((target) => ShowPopup(fighter, "Bog Rot Infected!"));
                UpdateUnitUI(null, fighter);
            }
        }
        foreach (var ghoul in ghouls)
        {
            if (ghoul != null && ghoul.Type == CharacterStats.CharacterType.Ghoul)
            {
                ghoul.OnInfected.AddListener((target) => ShowPopup(ghoul, "Bog Rot Infected!"));
                UpdateUnitUI(null, ghoul);
            }
        }
    }

    private void SetupUnitPanels(List<CharacterStats> characters, string containerName, string panelPrefix)
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

    public void UpdateUnitUI(CharacterStats attacker, CharacterStats target)
    {
        if (target == null || !unitPanels.ContainsKey(target))
        {
            return;
        }

        VisualElement panel = unitPanels[target];
        Label hpLabel = panel.Q<Label>($"{panel.name}_HP");
        VisualElement healthFill = panel.Q<VisualElement>($"{panel.name}_HealthFill");

        if (hpLabel != null)
        {
            hpLabel.text = $"HP: {Mathf.Round(target.health)}/{target.maxHealth}";
        }

        if (healthFill != null)
        {
            float healthPercent = target.health / target.maxHealth;
            healthFill.style.width = new StyleLength(new Length(healthPercent * 200, LengthUnit.Pixel));
            healthFill.style.height = new StyleLength(new Length(30, LengthUnit.Pixel));
            healthFill.style.backgroundColor = new StyleColor(target.Type == CharacterStats.CharacterType.Fighter ? new Color(0, 1, 0) : new Color(1, 0, 0));
            healthFill.style.display = DisplayStyle.Flex;
            healthFill.style.opacity = 1;
            healthFill.style.position = Position.Absolute;
            healthFill.style.left = 0;
            healthFill.style.top = 0;
        }
    }

    public void ShowPopup(CharacterStats character, string message)
    {
        if (character == null || mainCamera == null || canvasRectTransform == null) return;

        Vector3 worldPos = character.transform.position + Vector3.up * 1.5f;
        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        Vector2 canvasPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, screenPos, mainCamera, out canvasPos))
        {
            Label popup = new Label
            {
                text = message,
                style = {
                    position = Position.Absolute,
                    left = canvasPos.x,
                    top = canvasPos.y,
                    color = uiConfig.TextColor,
                    unityTextOutlineColor = uiConfig.TextOutlineColor,
                    fontSize = 16,
                    unityFont = uiConfig.PixelFont,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            root.Add(popup);
            StartCoroutine(AnimatePopup(popup));
        }
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