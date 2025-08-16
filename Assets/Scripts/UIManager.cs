using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIConfig uiConfig; // Assign UIConfig asset in Inspector
    [SerializeField] private List<CharacterStats> fighters; // Assign 4 fighters
    [SerializeField] private List<CharacterStats> ghouls;   // Assign 4 ghouls
    private VisualElement root;
    private RectTransform canvasRectTransform;
    private Camera mainCamera;
    private Dictionary<CharacterStats, VisualElement> unitPanels;

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        canvasRectTransform = GetComponent<RectTransform>(); // Assumes UIDocument is on Canvas
        mainCamera = Camera.main;

        if (uiConfig == null)
        {
            Debug.LogError("UIConfig asset not assigned in Inspector for UIManager!");
            return;
        }

        unitPanels = new Dictionary<CharacterStats, VisualElement>();
        SetupUnitPanels(fighters, "HeroesContainer");
        SetupUnitPanels(ghouls, "MonstersContainer");

        foreach (var fighter in fighters)
        {
            if (fighter != null && fighter.Type == CharacterStats.CharacterType.Fighter)
            {
                fighter.OnInfected.AddListener((target) => ShowPopup(fighter, "Bog Rot Infected!"));
            }
        }
        foreach (var ghoul in ghouls)
        {
            if (ghoul != null && ghoul.Type == CharacterStats.CharacterType.Ghoul)
            {
                ghoul.OnInfected.AddListener((target) => ShowPopup(ghoul, "Bog Rot Infected!"));
            }
        }
    }

    private void SetupUnitPanels(List<CharacterStats> characters, string containerName)
    {
        VisualElement container = root.Q<VisualElement>(containerName);
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] != null)
            {
                VisualElement panel = container.Q<VisualElement>($"{containerName[..^9]}{i + 1}");
                if (panel != null)
                {
                    unitPanels[characters[i]] = panel;
                }
            }
        }
    }

    public void UpdateUnitUI(CharacterStats attacker, CharacterStats target)
    {
        if (unitPanels.ContainsKey(target))
        {
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
                healthFill.style.width = new StyleLength(new Length(healthPercent * 100, LengthUnit.Percent));
            }
        }
    }

    public void ShowPopup(CharacterStats character, string message)
    {
        Vector3 worldPos = character.transform.position + Vector3.up * 1.5f; // Above sprite
        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, screenPos, mainCamera, out canvasPos);

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