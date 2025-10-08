using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace VirulentVentures
{
    public class HealingPopupComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private PartyData partyData;
        [SerializeField] private HealingConfig healingConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private VirusCraftingComponent virusCraftingComponent; // Added serialized field
        private VisualElement root;
        private VisualElement popupContainer;
        private bool isInitialized;

        void Awake()
        {
            if (!ValidateReferences())
            {
                isInitialized = false;
                return;
            }
            root = uiDocument.rootVisualElement;
            isInitialized = true;
            SubscribeToEventBus();
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                UnsubscribeFromEventBus();
            }
        }

        private void SubscribeToEventBus()
        {
            eventBus.OnTempleEnteredFromExpedition += ShowHealingPopup;
        }

        private void UnsubscribeFromEventBus()
        {
            eventBus.OnTempleEnteredFromExpedition -= ShowHealingPopup;
        }

        private void ShowHealingPopup()
        {
            if (!isInitialized || partyData == null || partyData.HeroStats == null)
            {
                Debug.LogWarning("HealingPopupComponent: Cannot show popup, missing references or party data.");
                return;
            }

            popupContainer = root.Q<VisualElement>("HealingPopupContainer");
            if (popupContainer == null)
            {
                Debug.LogError("HealingPopupComponent: HealingPopupContainer not found in UI Document!");
                return;
            }
            popupContainer.style.display = DisplayStyle.Flex;

            var healableHeroes = partyData.HeroStats.Where(h => !h.HasRetreated && h.Health > 0 &&
                (h.Health < h.MaxHealth || h.Morale < h.MaxMorale || h.Infections.Any())).ToList();
            if (healableHeroes.Count == 0)
            {
                Debug.Log("HealingPopupComponent: No heroes to heal or cure.");
                popupContainer.style.display = DisplayStyle.None;
                return;
            }

            StartCoroutine(ShowHeroPopups(healableHeroes));
        }

        private IEnumerator ShowHeroPopups(System.Collections.Generic.List<CharacterStats> heroes)
        {
            int totalFavour = 0;
            Label totalFavourLabel = new Label("Total Favour: 0");
            totalFavourLabel.style.unityFont = uiConfig.PixelFont;
            totalFavourLabel.style.color = new StyleColor(Color.green);
            totalFavourLabel.style.fontSize = 16;
            totalFavourLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            totalFavourLabel.style.marginTop = 10;
            popupContainer.Add(totalFavourLabel);

            foreach (var hero in heroes)
            {
                VisualElement heroPanel = new VisualElement();
                heroPanel.AddToClassList("hero-healing-panel");

                var heroData = CharacterLibrary.GetHeroData(hero.Id);
                Image portrait = new Image { image = heroData?.Portrait?.texture, style = { width = 100, height = 100 } };
                portrait.AddToClassList("portrait");
                if (portrait.image == null)
                {
                    Debug.LogWarning($"HealingPopupComponent: No portrait for {hero.Id}, using fallback.");
                    portrait.style.backgroundColor = new StyleColor(Color.gray);
                }
                heroPanel.Add(portrait);

                VisualElement healthBar = new VisualElement();
                healthBar.AddToClassList("health-bar");
                VisualElement moraleBar = new VisualElement();
                moraleBar.AddToClassList("morale-bar");
                heroPanel.Add(healthBar);
                heroPanel.Add(moraleBar);

                Label hpLabel = new Label($"HP: {hero.Health}/{hero.MaxHealth}");
                hpLabel.style.unityFont = uiConfig.PixelFont;
                hpLabel.style.color = uiConfig.TextColor;
                heroPanel.Add(hpLabel);

                Label moraleLabel = new Label($"Morale: {hero.Morale}/{hero.MaxMorale}");
                moraleLabel.style.unityFont = uiConfig.PixelFont;
                moraleLabel.style.color = uiConfig.TextColor;
                heroPanel.Add(moraleLabel);

                string virusText = hero.Infections.Any() ? string.Join(", ", hero.Infections.Select(v => v.VirusID)) : "None";
                Label virusLabel = new Label($"Viruses: {virusText}");
                virusLabel.style.unityFont = uiConfig.PixelFont;
                virusLabel.style.color = uiConfig.TextColor;
                heroPanel.Add(virusLabel);

                Label favourLabel = new Label("Favour: 0");
                favourLabel.style.unityFont = uiConfig.PixelFont;
                favourLabel.style.color = new StyleColor(Color.green);
                heroPanel.Add(favourLabel);

                popupContainer.Add(heroPanel);

                int startHealth = hero.Health;
                int targetHealth = hero.MaxHealth;
                int hpHealed = targetHealth - startHealth;
                int startMorale = hero.Morale;
                int targetMorale = hero.MaxMorale;
                int moraleRestored = targetMorale - startMorale;
                float virusFavour = hero.Infections.Sum(v => healingConfig.VirusRarityFavour.TryGetValue(v.Rarity, out float f) ? f : 0f);
                float favour = (healingConfig.HPFavourPerPoint * hpHealed) + (healingConfig.MoraleFavourPerPoint * moraleRestored) + virusFavour;
                totalFavour += Mathf.RoundToInt(favour);

                float elapsed = 0f;
                float duration = 1.5f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                    int currentHealth = Mathf.RoundToInt(Mathf.Lerp(startHealth, targetHealth, t));
                    float healthPercent = (float)currentHealth / hero.MaxHealth;
                    healthBar.style.width = new StyleLength(Length.Percent(healthPercent * 100));
                    hpLabel.text = $"HP: {currentHealth}/{hero.MaxHealth}";

                    int currentMorale = Mathf.RoundToInt(Mathf.Lerp(startMorale, targetMorale, t));
                    float moralePercent = (float)currentMorale / hero.MaxMorale;
                    moraleBar.style.width = new StyleLength(Length.Percent(moralePercent * 100));
                    moraleLabel.text = $"Morale: {currentMorale}/{hero.MaxMorale}";

                    float currentFavour = Mathf.Lerp(0, favour, t);
                    favourLabel.text = $"Favour: {Mathf.RoundToInt(currentFavour)}";
                    totalFavourLabel.text = $"Total Favour: {totalFavour - Mathf.RoundToInt(favour) + Mathf.RoundToInt(currentFavour)}";

                    yield return null;
                }
                hero.Health = targetHealth;
                hero.Morale = targetMorale;
                if (hero.Infections.Any())
                {
                    eventBus.RaiseCureInfections();
                    virusLabel.text = "Viruses: None";
                }
                favourLabel.text = $"Favour: {Mathf.RoundToInt(favour)}";
                totalFavourLabel.text = $"Total Favour: {totalFavour}";
                yield return new WaitForSeconds(0.5f);
                popupContainer.Remove(heroPanel);
            }

            if (totalFavour > 0)
            {
                var playerProgress = ExpeditionManager.Instance.GetPlayerProgress();
                playerProgress.AddFavour(totalFavour);
                eventBus.RaisePlayerProgressUpdated();
            }
            eventBus.RaisePartyUpdated(partyData);

            Button continueButton = new Button();
            continueButton.text = "Continue";
            continueButton.AddToClassList("button");
            continueButton.style.color = uiConfig.TextColor;
            continueButton.clicked += () =>
            {
                popupContainer.style.display = DisplayStyle.None;
                eventBus.RaiseContinueClicked();
            };
            popupContainer.Add(continueButton);
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || uiConfig == null || partyData == null || healingConfig == null ||
                eventBus == null || virusCraftingComponent == null)
            {
                Debug.LogError($"HealingPopupComponent: Missing references! UIDocument: {uiDocument != null}, " +
                    $"UIConfig: {uiConfig != null}, PartyData: {partyData != null}, HealingConfig: {healingConfig != null}, " +
                    $"EventBus: {eventBus != null}, VirusCraftingComponent: {virusCraftingComponent != null}");
                return false;
            }
            return true;
        }
    }
}