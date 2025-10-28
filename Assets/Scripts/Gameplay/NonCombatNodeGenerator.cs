using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class NonCombatNodeGenerator : MonoBehaviour
    {
        [SerializeField] private List<NonCombatEncounterSO> encounters = new List<NonCombatEncounterSO>();

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

        public void InitializeCache()
        {
            if (isInitialized) return;

            encounterCache.Clear();
            if (encounters != null && encounters.Count > 0)
                encounterCache.AddRange(encounters);

            if (encounterCache.Count == 0)
                Debug.LogWarning("NonCombatNodeGenerator: No encounters assigned. Using fallback.");

            isInitialized = true;
        }

        public NodeData GenerateNonCombatNode(string biome, int level, int rating, bool isTempleNode = false)
        {
            if (isTempleNode)
                return new NodeData(new List<CharacterStats>(), "Temple", biome, false, "", new List<VirusSO>(), rating);

            if (encounterCache.Count == 0)
            {
                string[] texts = flavourTextPool.ContainsKey(biome) ? flavourTextPool[biome] : new[] { "A quiet rest spot." };
                return new NodeData(new List<CharacterStats>(), "NonCombat", biome, false,
                                      texts[Random.Range(0, texts.Length)], new List<VirusSO>(), rating);
            }

            NonCombatEncounterSO encounter = encounterCache.OrderBy(_ => Random.value).First();
            List<VirusSO> seeded = new List<VirusSO>();

            if (encounter.NaturalVirusPool != null && encounter.NaturalVirusPool.Length > 0)
            {
                float chance = encounter.NaturalVirusChance + (rating - 3) * 0.05f;
                if (Random.value < chance)
                    seeded.Add(encounter.NaturalVirusPool[Random.Range(0, encounter.NaturalVirusPool.Length)]);
            }

            return new NodeData(
                monsters: new List<CharacterStats>(),
                nodeType: "NonCombat",
                biome: biome,
                isCombat: false,
                flavourText: encounter.Description,
                seededViruses: seeded,
                challengeRating: rating,
                vector: encounter.Vector,
                encounter: encounter);
        }
    }
}