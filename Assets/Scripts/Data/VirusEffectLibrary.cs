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
        private static bool isInitialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeCache()
        {
            if (isInitialized) return;

            effectCache.Clear();

#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:VirusEffectLibrary", new[] { "Assets/ScriptableObjects/Viruses" });
            foreach (string guid in guids)
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
#endif

            isInitialized = true;
        }

        // FIXED: ALL STATIC METHODS
        private static float GetRarityMultiplier(VirusRarity rarity) => rarity switch
        {
            VirusRarity.Common => 1f,
            VirusRarity.Uncommon => 2f,
            VirusRarity.Rare => 3f,
            VirusRarity.Epic => 3f,
            _ => 1f
        };

        // FIXED: STATIC - NO INSTANCE
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
    }
}