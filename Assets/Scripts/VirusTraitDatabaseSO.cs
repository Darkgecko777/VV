using UnityEngine;
using System;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "VirusTraitDatabaseSO", menuName = "VirulentVentures/VirusTraitDatabaseSO", order = 17)]
    public class VirusTraitDatabaseSO : ScriptableObject
    {
        [Serializable]
        public struct VirusTrait
        {
            public string VirusID; // e.g., "BogRot"
            public Biome Biome; // Swamps
            public string Rarity; // Common
            public VirusSO.Modifier Modifier; // e.g., { "Speed", -1f }
            public Sprite Sprite; // Token visual
            public bool IsCrafted; // Natural or crafted
        }

        [SerializeField] private List<VirusTrait> traits;

        public VirusTrait GetTrait(string virusID)
        {
            var trait = traits.Find(t => t.VirusID == virusID);
            if (trait.VirusID == null)
            {
                Debug.LogWarning($"VirusTraitDatabaseSO: Trait for {virusID} not found.");
            }
            return trait;
        }

        public List<VirusTrait> GetAllTraits()
        {
            return traits;
        }
    }
}