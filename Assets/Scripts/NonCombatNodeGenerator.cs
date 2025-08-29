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
            } },
            { "Ruins", new[] {
                "Crumbling stones echo with lost voices.",
                "A forgotten altar pulses faintly.",
                "Faded runes glow faintly.",
                "Broken arches whisper of past glories."
            } }
        };

        public NodeData GenerateNonCombatNode(string biome, int level)
        {
            string[] texts = flavourTextPool.ContainsKey(biome) ? flavourTextPool[biome] : new[] { "A quiet rest spot." };
            string flavourText = texts[Random.Range(0, texts.Length)];

            return new NodeData(
                monsters: new List<CharacterStats>(),
                nodeType: "NonCombat",
                biome: biome,
                isCombat: false,
                flavourText: flavourText,
                seededViruses: new List<VirusData>()
            );
        }
    }
}