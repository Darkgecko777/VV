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
                //eventBus.OnPartyReturned -= ShowHealingPopup;
            }
        }

        private void SubscribeToEventBus()
        {
            //eventBus.OnPartyReturned += ShowHealingPopup;
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

            var healableHeroes = partyData.HeroStats.Where(h => !h.HasRetreated && h.Health > 0 && h.Health < h.MaxHealth).ToList();
            if (healableHeroes.Count == 0)
            {
                Debug.Log("HealingPopupComponent: No heroes to heal.");
                popupContainer.style.display = DisplayStyle.None;
                return;
            }

            StartCoroutine(ShowHeroPopups(healableHeroes));
        }

        private IEnumerator ShowHeroPopups(System.Collections.Generic.List<CharacterStats> heroes)
        {
            int totalFavour = 0;
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
                heroPanel.Add(healthBar);

                Label hpLabel = new Label($"HP: {hero.Health}/{hero.MaxHealth}");
                hpLabel.style.unityFont = uiConfig.PixelFont;
                hpLabel.style.color = uiConfig.TextColor;
                heroPanel.Add(hpLabel);

                popupContainer.Add(heroPanel);

                int startHealth = hero.Health;
                int targetHealth = hero.MaxHealth;
                int hpHealed = targetHealth - startHealth;
                float favour = healingConfig.HPFavourPerPoint * hpHealed;
                totalFavour += Mathf.RoundToInt(favour);

                float elapsed = 0f;
                float duration = 2f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration); // Ease-in-out
                    int currentHealth = Mathf.RoundToInt(Mathf.Lerp(startHealth, targetHealth, t));
                    float healthPercent = (float)currentHealth / hero.MaxHealth;
                    healthBar.style.width = new StyleLength(Length.Percent(healthPercent * 100));
                    hpLabel.text = $"HP: {currentHealth}/{hero.MaxHealth}";
                    yield return null;
                }
                hero.Health = targetHealth;
                yield return new WaitForSeconds(0.5f); // Brief pause between heroes
                popupContainer.Remove(heroPanel);
            }

            if (totalFavour > 0)
            {
                var playerProgress = ExpeditionManager.Instance.GetPlayerProgress();
                playerProgress.AddFavour(totalFavour);
                eventBus.RaisePlayerProgressUpdated();
            }
            eventBus.RaisePartyUpdated(partyData);
            popupContainer.style.display = DisplayStyle.None;
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || uiConfig == null || partyData == null || healingConfig == null || eventBus == null)
            {
                Debug.LogError($"HealingPopupComponent: Missing references! UIDocument: {uiDocument != null}, UIConfig: {uiConfig != null}, " +
                    $"PartyData: {partyData != null}, HealingConfig: {healingConfig != null}, EventBus: {eventBus != null}");
                return false;
            }
            return true;
        }
    }
}