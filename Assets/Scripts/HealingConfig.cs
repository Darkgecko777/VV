using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "HealingConfig", menuName = "VirulentVentures/HealingConfig", order = 16)]
    public class HealingConfig : ScriptableObject
    {
        public float HPFavourPerPoint = 0.5f;
        public float MoraleFavourPerPoint = 0.3f;
        public float FavourPerTrait = 5f; // Favour per virus trait
        public float FirstDiscoveryFavourBonus = 10f; // Bonus for first-time virus discovery
        public bool AllowCraftedTokenRecycling = false; // Toggle for crafted virus tokens
    }
}