using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "HealingConfig", menuName = "VirulentVentures/HealingConfig", order = 16)]
    public class HealingConfig : ScriptableObject
    {
        public float HPFavourPerPoint = 0.5f;
        public float MoraleFavourPerPoint = 0.3f;
        public Dictionary<string, float> VirusRarityFavour = new Dictionary<string, float>
        {
            { "Common", 10f },
            { "Rare", 20f },
            { "Epic", 30f }
        };
    }
}
