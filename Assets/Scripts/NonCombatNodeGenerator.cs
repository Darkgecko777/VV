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

        public NodeData GenerateNonCombatNode(string biome, int level, bool isTempleNode = false)
        {
            string[] texts = isTempleNode ? new[] { "" } : (flavourTextPool.ContainsKey(biome) ? flavourTextPool[biome] : new[] { "A quiet rest spot." });
            string flavourText = texts[Random.Range(0, texts.Length)];

            return new NodeData(
                monsters: new List<CharacterStats>(),
                nodeType: isTempleNode ? "Temple" : "NonCombat",
                biome: biome,
                isCombat: false,
                flavourText: flavourText,
                seededViruses: new List<VirusData>()
            );
        }
    }
}