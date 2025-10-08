using UnityEngine;
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

        void Awake()
        {
            if (!ValidateReferences())
            {
                Debug.LogError("VirusCraftingComponent: Initialization failed due to missing references.");
            }
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
            if (playerProgress == null)
                Debug.LogError("VirusCraftingComponent: playerProgress is null.");
            if (partyData == null)
                Debug.LogError("VirusCraftingComponent: partyData is null.");
            if (eventBus == null)
                Debug.LogError("VirusCraftingComponent: eventBus is null.");
            if (healingConfig == null)
                Debug.LogError("VirusCraftingComponent: healingConfig is null.");
            if (virusConfig == null)
                Debug.LogError("VirusCraftingComponent: virusConfig is null.");
            return playerProgress != null && partyData != null && eventBus != null && healingConfig != null && virusConfig != null;
        }
    }
}