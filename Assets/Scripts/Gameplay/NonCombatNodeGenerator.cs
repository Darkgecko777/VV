using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VirulentVentures
{
    public class NonCombatNodeGenerator : MonoBehaviour
    {
        private static List<NonCombatEncounterSO> encounterCache = new List<NonCombatEncounterSO>();
        private static bool isInitialized = false;

        private readonly Dictionary<string, string[]> flavourTextPool = new Dictionary<string, string[]>
        {
            { "Swamp", new[] {
                "A foggy camp with eerie whispers.",
                "A murky pool hides ancient secrets.",
                "A rotting log stirs with unseen eyes.",
                "Mossy vines pulse faintly in the mist."
            } }
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeCache()
        {
            if (isInitialized) return;

            encounterCache.Clear();

#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:NonCombatEncounterSO", new[] { "Assets/ScriptableObjects/NonCombat" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var encounter = AssetDatabase.LoadAssetAtPath<NonCombatEncounterSO>(path);
                if (encounter != null) encounterCache.Add(encounter);
            }
#endif

            isInitialized = true;
        }

        public NodeData GenerateNonCombatNode(string biome, int level, int rating, bool isTempleNode = false)
        {
            if (isTempleNode)
            {
                return new NodeData(new List<CharacterStats>(), "Temple", biome, false, "", new List<VirusSO>(), rating);
            }

            if (encounterCache.Count == 0)
            {
                Debug.LogWarning("NonCombatNodeGenerator: No encounters cached. Using placeholder.");
                string[] texts = flavourTextPool.ContainsKey(biome) ? flavourTextPool[biome] : new[] { "A quiet rest spot." };
                return new NodeData(new List<CharacterStats>(), "NonCombat", biome, false, texts[Random.Range(0, texts.Length)], new List<VirusSO>(), rating);
            }

            NonCombatEncounterSO encounter = encounterCache.OrderBy(_ => Random.value).First(); // Random
            float virusChance = 0.05f + (rating - 3) * 0.05f; // Placeholder

            return new NodeData(
                monsters: new List<CharacterStats>(),
                nodeType: "NonCombat",
                biome: biome,
                isCombat: false,
                flavourText: encounter.Description,
                seededViruses: new List<VirusSO>(),
                challengeRating: rating,
                vector: encounter.Vector // NEW
            );
        }
    }
}