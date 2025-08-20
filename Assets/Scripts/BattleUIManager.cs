using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace VirulentVentures
{
    public class BattleUIManager : MonoBehaviour
    {
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private PartyData partyData;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        private VisualElement root;
        private RectTransform canvasRectTransform;
        private Camera mainCamera;
        private Dictionary<ICombatUnit, VisualElement> unitPanels;
        private Label combatLog;
        private float fadeDuration = 0.5f;

        void Start()
        {
            root = GetComponent<UIDocument>()?.rootVisualElement;
            canvasRectTransform = GetComponent<RectTransform>();
            mainCamera = Camera.main;

            if (root == null || canvasRectTransform == null || uiConfig == null || mainCamera == null || partyData == null || expeditionData == null || fadeCanvasGroup == null)
            {
                Debug.LogError($"BattleUIManager: Missing references! Root: {root != null}, CanvasRectTransform: {canvasRectTransform != null}, UIConfig: {uiConfig != null}, MainCamera: {mainCamera != null}, PartyData: {partyData != null}, ExpeditionData: {expeditionData != null}, FadeCanvasGroup: {fadeCanvasGroup != null}");
                return;
            }

            if (expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogError($"BattleUIManager: Invalid node index {expeditionData.CurrentNodeIndex}, node count: {expeditionData.NodeData.Count}");
                return;
            }

            unitPanels = new Dictionary<ICombatUnit, VisualElement>();
            combatLog = root.Q<Label>("CombatLog");
            if (combatLog == null)
            {
                Debug.LogError("BattleUIManager: Missing CombatLog!");
                return;
            }
            combatLog.style.display = DisplayStyle.Flex;
            combatLog.text = "";

            fadeCanvasGroup.alpha = 0;
        }

        public void InitializeUI(List<(ICombatUnit unit, GameObject go)> units)
        {
            SetupUnitPanels(units.Where(u => u.unit is HeroStats).Select(u => u.unit).ToList(), true);
            SetupUnitPanels(units.Where(u => u.unit is MonsterStats).Select(u => u.unit).ToList(), false);
        }

        private void SetupUnitPanels(List<ICombatUnit> statsList, bool isHero)
        {
            if (statsList == null || statsList.Count == 0)
            {
                Debug.LogWarning($"BattleUIManager: Empty { (isHero ? "hero" : "monster") } stats");
                return;
            }

            string containerName = isHero ? "HeroPanelContainer" : "MonsterPanelContainer";
            VisualElement container = root.Q<VisualElement>(containerName);
            if (container == null)
            {
                Debug.LogError($"BattleUIManager: Missing {containerName}");
                return;
            }

            container.Clear();

            for (int i = 0; i < statsList.Count; i++)
            {
                ICombatUnit unit = statsList[i];
                VisualElement panel = new VisualElement { name = $"Panel_{unit.Type.Id}" };
                panel.AddToClassList("unit-panel");
                panel.style.position = Position.Absolute;

                Label typeLabel = new Label { text = unit.Type.Id, name = "TypeLabel" };
                typeLabel.AddToClassList("unit-type");
                panel.Add(typeLabel);

                ProgressBar healthBar = new ProgressBar { name = "HealthBar", value = 100 };
                healthBar.AddToClassList("unit-bar");
                panel.Add(healthBar);

                ProgressBar moraleBar = new ProgressBar { name = "MoraleBar", value = 100 };
                moraleBar.AddToClassList("unit-bar");
                panel.Add(moraleBar);

                container.Add(panel);
                unitPanels.Add(unit, panel);

                UpdatePanelPosition(panel, unit.Position, isHero);
                UpdateUnitPanel(unit);
            }
        }

        private void UpdatePanelPosition(VisualElement panel, Vector3 worldPosition, bool isHero)
        {
            Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPosition, null, out Vector2 localPoint);

            panel.style.left = localPoint.x + (isHero ? -50 : 50);
            panel.style.top = -localPoint.y + 50; // Adjust for UI orientation
        }

        public void UpdateUnitPanel(ICombatUnit unit)
        {
            if (unitPanels.TryGetValue(unit, out VisualElement panel))
            {
                ProgressBar healthBar = panel.Q<ProgressBar>("HealthBar");
                if (healthBar != null)
                {
                    healthBar.value = (float)unit.Health / unit.MaxHealth * 100;
                    healthBar.title = $"HP: {unit.Health}/{unit.MaxHealth}";
                }

                ProgressBar moraleBar = panel.Q<ProgressBar>("MoraleBar");
                if (moraleBar != null)
                {
                    moraleBar.value = unit.Morale;
                    moraleBar.title = $"Morale: {unit.Morale}";
                }
            }
        }

        public void ShowDamagePopup(ICombatUnit unit, string message)
        {
            VisualElement popup = new VisualElement { name = "DamagePopup" };
            popup.AddToClassList("damage-popup");
            Label messageLabel = new Label { text = message };
            messageLabel.AddToClassList("damage-text");
            popup.Add(messageLabel);

            Vector2 screenPosition = mainCamera.WorldToScreenPoint(unit.Position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPosition, null, out Vector2 localPoint);

            popup.style.left = localPoint.x;
            popup.style.top = -localPoint.y;
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

        public IEnumerator FadeToExpedition(System.Action onComplete)
        {
            if (fadeCanvasGroup == null)
            {
                Debug.LogWarning("BattleUIManager: Missing fadeCanvasGroup!");
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1;
            onComplete?.Invoke();
        }
    }
}