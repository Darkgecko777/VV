using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class VirusCraftingComponent : MonoBehaviour
    {
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private PartyData partyData;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private HealingConfig healingConfig;
        [SerializeField] private VirusConfigSO virusConfig;
        [SerializeField] private VirusTraitDatabaseSO virusTraitDatabase;
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private Sprite virusTraitSprite; // Fallback sprite
        private VisualElement virusCraftingContainer;
        private Dictionary<string, VisualElement> tokenCells;
        private Vector2 originalPosition;
        private bool isDragging;

        void Awake()
        {
            if (!ValidateReferences()) return;
            virusCraftingContainer = uiDocument.rootVisualElement.Q<VisualElement>("VirusCraftingContainer");
            tokenCells = new Dictionary<string, VisualElement>();
            originalPosition = new Vector2(0, 0);
            UpdateCraftingGrid();
            RegisterDragEvents();
            eventBus.OnCureInfections += UpdateCraftingGrid;
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.OnCureInfections -= UpdateCraftingGrid;
            }
        }

        private void UpdateCraftingGrid()
        {
            tokenCells.Clear();
            virusCraftingContainer.Clear();

            var tokenGroups = playerProgress.VirusTokens.GroupBy(t => t).Select(g => new { Token = g.Key, Count = g.Count() }).ToList();
            int discoveredCount = playerProgress.DiscoveredVirusIDs.Count;
            int gridSize = Mathf.Max(3, Mathf.CeilToInt(Mathf.Sqrt(discoveredCount)));
            virusCraftingContainer.style.width = gridSize * 64;
            virusCraftingContainer.style.height = gridSize * 64;

            int index = 0;
            foreach (var group in tokenGroups.OrderBy(g => g.Token))
            {
                if (index >= gridSize * gridSize) break;
                int row = index / gridSize;
                int col = index % gridSize;
                string virusID = group.Token.Replace("_Token", "");
                var trait = virusTraitDatabase.GetTrait(virusID);

                VisualElement cell = new VisualElement();
                cell.style.width = 64;
                cell.style.height = 64;
                cell.style.position = Position.Absolute;
                cell.style.left = col * 64;
                cell.style.top = row * 64;
                cell.style.backgroundImage = new StyleBackground(trait.Sprite ?? virusTraitSprite);
                cell.style.opacity = trait.IsCrafted ? 0.8f : 1f;

                Label countLabel = new Label($"x{group.Count}");
                countLabel.style.unityFont = uiDocument.rootVisualElement.Q<Label>().style.unityFont; // Use UI font
                countLabel.style.color = new StyleColor(Color.white);
                countLabel.style.fontSize = 12;
                countLabel.style.unityTextAlign = TextAnchor.LowerRight;
                cell.Add(countLabel);

                tokenCells[group.Token] = cell;
                virusCraftingContainer.Add(cell);
                index++;
            }
        }

        private void RegisterDragEvents()
        {
            // Drag logic will be expanded later for crafting
            foreach (var cell in tokenCells.Values)
            {
                cell.RegisterCallback<PointerDownEvent>(evt =>
                {
                    isDragging = true;
                    cell.AddToClassList("dragging");
                    cell.CapturePointer(evt.pointerId);
                });

                cell.RegisterCallback<PointerMoveEvent>(evt =>
                {
                    if (isDragging)
                    {
                        Vector2 localPos = virusCraftingContainer.WorldToLocal(evt.position);
                        cell.style.left = localPos.x - 32;
                        cell.style.top = localPos.y - 32;
                    }
                });

                cell.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (isDragging)
                    {
                        isDragging = false;
                        cell.RemoveFromClassList("dragging");
                        cell.ReleasePointer(evt.pointerId);
                        cell.style.left = originalPosition.x;
                        cell.style.top = originalPosition.y;
                    }
                });
            }
        }

        private bool ValidateReferences()
        {
            bool isValid = true;
            if (playerProgress == null)
            {
                Debug.LogError("VirusCraftingComponent: playerProgress is null.");
                isValid = false;
            }
            if (partyData == null)
            {
                Debug.LogError("VirusCraftingComponent: partyData is null.");
                isValid = false;
            }
            if (eventBus == null)
            {
                Debug.LogError("VirusCraftingComponent: eventBus is null.");
                isValid = false;
            }
            if (healingConfig == null)
            {
                Debug.LogError("VirusCraftingComponent: healingConfig is null.");
                isValid = false;
            }
            if (virusConfig == null)
            {
                Debug.LogError("VirusCraftingComponent: virusConfig is null.");
                isValid = false;
            }
            if (virusTraitDatabase == null)
            {
                Debug.LogError("VirusCraftingComponent: virusTraitDatabase is null.");
                isValid = false;
            }
            if (uiDocument == null)
            {
                Debug.LogError("VirusCraftingComponent: uiDocument is null.");
                isValid = false;
            }
            if (virusTraitSprite == null)
            {
                Debug.LogError("VirusCraftingComponent: virusTraitSprite is null.");
                isValid = false;
            }
            return isValid;
        }
    }
}