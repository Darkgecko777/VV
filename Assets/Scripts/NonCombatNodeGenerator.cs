using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class NonCombatNodeGenerator : MonoBehaviour
    {
        private readonly Dictionary<string, string[]> flavourTextPool = new Dictionary<string, string[]>
        {
            { "Swamp", new[] {
                "A foggy camp with eerie whispers.",
                "A murky pool hides ancient secrets.",
                "A rotting log stirs with unseen eyes.",
                "Mossy vines pulse faintly in the mist."
            } }
        };

        public NodeData GenerateNonCombatNode(string biome, int level, int rating, bool isTempleNode = false)
        {
            string[] texts = isTempleNode ? new[] { "" } : (flavourTextPool.ContainsKey(biome) ? flavourTextPool[biome] : new[] { "A quiet rest spot." });
            string flavourText = texts[Random.Range(0, texts.Length)];

            // Placeholder: Scale virus seeding chance and loot tier based on rating
            float virusChance = 0.05f + (rating - 3) * 0.05f; // 5% at R=3, 20% at R=6
            int lootTier = Mathf.CeilToInt(rating / 2f); // R=3-4: tier 2, R=5-6: tier 3

            return new NodeData(
                monsters: new List<CharacterStats>(),
                nodeType: isTempleNode ? "Temple" : "NonCombat",
                biome: biome,
                isCombat: false,
                flavourText: flavourText,
                seededViruses: new List<VirusData>(),
                challengeRating: rating
            );
        }
    }
}