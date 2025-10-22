using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "VirusEffectLibrary", menuName = "VirulentVentures/VirusEffectLibrary")]
    public class VirusEffectLibrary : ScriptableObject
    {
        [System.Serializable]
        public struct VirusEffect
        {
            public string VirusID;
            public string Stat;
            public float Value;  // BASE (Common 1x)
        }

        [SerializeField] private VirusEffect[] effects = new VirusEffect[0];

        private static Dictionary<string, VirusEffect> effectCache = new Dictionary<string, VirusEffect>();
        // NEW: Cache for VirusSOs by biome
        private static Dictionary<Biome, List<VirusSO>> virusCache = new Dictionary<Biome, List<VirusSO>>();
        private static bool isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeCache()
        {
            if (isInitialized) return;

            effectCache.Clear();
            virusCache.Clear();

#if UNITY_EDITOR
            // Scan for VirusEffectLibrary assets (effects)
            string[] effectGuids = AssetDatabase.FindAssets("t:VirusEffectLibrary", new[] { "Assets/ScriptableObjects/Viruses" });
            foreach (string guid in effectGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var library = AssetDatabase.LoadAssetAtPath<VirusEffectLibrary>(path);
                if (library != null && library.effects != null && library.effects.Length > 0)
                {
                    foreach (var effect in library.effects)
                    {
                        if (!string.IsNullOrEmpty(effect.VirusID))
                            effectCache[effect.VirusID] = effect;
                    }
                }
            }

            // NEW: Scan for all VirusSO assets (including subfolders like Swamps/)
            string[] virusGuids = AssetDatabase.FindAssets("t:VirusSO", new[] { "Assets/ScriptableObjects/Viruses" });
            foreach (string guid in virusGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var virus = AssetDatabase.LoadAssetAtPath<VirusSO>(path);
                if (virus != null)
                {
                    if (!virusCache.ContainsKey(virus.Biome))
                        virusCache[virus.Biome] = new List<VirusSO>();
                    virusCache[virus.Biome].Add(virus);
                }
            }
#endif

            isInitialized = true;
        }

        private static float GetRarityMultiplier(VirusRarity rarity) => rarity switch
        {
            VirusRarity.Common => 1f,
            VirusRarity.Uncommon => 2f,
            VirusRarity.Rare => 3f,
            VirusRarity.Epic => 3f,
            _ => 1f
        };

        public static VirusEffect GetEffect(string virusID, VirusRarity rarity)
        {
            if (effectCache.TryGetValue(virusID, out var baseEffect))
            {
                float multiplier = GetRarityMultiplier(rarity);
                return new VirusEffect
                {
                    VirusID = virusID,
                    Stat = baseEffect.Stat,
                    Value = baseEffect.Value * multiplier
                };
            }

            Debug.LogWarning($"VirusEffectLibrary: VirusID '{virusID}' not found in cache");
            return new VirusEffect { VirusID = virusID, Stat = "Speed", Value = -1f * GetRarityMultiplier(rarity) };
        }

        // NEW: Get viruses for a biome (all rarities)
        public static List<VirusSO> GetBiomeViruses(Biome biome)
        {
            if (virusCache.TryGetValue(biome, out var viruses))
                return viruses;
            Debug.LogWarning($"VirusEffectLibrary: No viruses found for biome {biome}");
            return new List<VirusSO>();
        }
    }
}