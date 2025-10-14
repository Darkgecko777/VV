using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace VirulentVentures
{
    public class VirusCraftingComponent : MonoBehaviour
    {
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private PartyData partyData;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private HealingConfig healingConfig;
        [SerializeField] private VirusConfigSO virusConfig;
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private Sprite virusTraitSprite;
        private VisualElement virusCraftingContainer;
        private VisualElement virusTraitIcon;
        private Vector2 originalPosition;
        private bool isDragging;

        void Awake()
        {
            if (!ValidateReferences()) return;
            virusCraftingContainer = uiDocument.rootVisualElement.Q<VisualElement>("VirusCraftingContainer");
            virusTraitIcon = virusCraftingContainer.Q<VisualElement>("VirusTraitIcon");
            if (virusTraitIcon == null)
            {
                Debug.LogError("VirusCraftingComponent: VirusTraitIcon not found in UI!");
                return;
            }
            virusTraitIcon.style.backgroundImage = new StyleBackground(virusTraitSprite);
            originalPosition = new Vector2(0, 0); // Top-left cell
            RegisterDragEvents();
        }

        private void RegisterDragEvents()
        {
            virusTraitIcon.RegisterCallback<PointerDownEvent>(evt =>
            {
                isDragging = true;
                virusTraitIcon.AddToClassList("dragging");
                virusTraitIcon.CapturePointer(evt.pointerId);
            });

            virusTraitIcon.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (isDragging)
                {
                    Vector2 localPos = virusCraftingContainer.WorldToLocal(evt.position);
                    virusTraitIcon.style.left = localPos.x - 32; // Center sprite on pointer
                    virusTraitIcon.style.top = localPos.y - 32;
                }
            });

            virusTraitIcon.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (isDragging)
                {
                    isDragging = false;
                    virusTraitIcon.RemoveFromClassList("dragging");
                    virusTraitIcon.ReleasePointer(evt.pointerId);
                    Vector2 localPos = virusCraftingContainer.WorldToLocal(evt.position);
                    if (IsInGrid(localPos))
                    {
                        virusTraitIcon.style.left = originalPosition.x;
                        virusTraitIcon.style.top = originalPosition.y;
                    }
                }
            });
        }

        private bool IsInGrid(Vector2 localPos)
        {
            // Grid is 192x192px (3x3 cells of 64x64) at (0, 0) in VirusCraftingContainer
            return localPos.x >= 0 && localPos.x <= 192 && localPos.y >= 0 && localPos.y <= 192;
        }

        public void CureInfections()
        {
            if (!ValidateReferences()) return;
            int totalFavour = 0;
            foreach (var hero in partyData.HeroStats.Where(h => h.Infections.Any() && h.Health > 0))
            {
                foreach (var virus in hero.Infections.ToList())
                {
                    playerProgress.VirusTokens.Add(virus.VirusID + "_Token");
                    if (healingConfig.VirusRarityFavour.TryGetValue(virus.Rarity, out float favour))
                    {
                        totalFavour += Mathf.RoundToInt(favour);
                    }
                    hero.Infections.Remove(virus);
                    string cureMessage = $"{hero.Id} cured of {virus.VirusID}, gained {virus.VirusID}_Token and {favour} Favour!";
                    eventBus.RaiseLogMessage(cureMessage, Color.green);
                }
                eventBus.RaiseUnitUpdated(hero, hero.GetDisplayStats());
            }
            if (totalFavour > 0)
            {
                playerProgress.AddFavour(totalFavour);
                eventBus.RaisePlayerProgressUpdated();
            }
            eventBus.RaisePartyUpdated(partyData);
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