using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class CombatNodeGenerator : MonoBehaviour
    {
        [SerializeField] private List<string> monsterPool;
        private readonly Dictionary<string, string[]> flavourTextPool = new Dictionary<string, string[]>
        {
            { "Swamp", new[] {
                "A horde emerges from the bog.",
                "Mire-dwellers lurk in the mist.",
                "Fetid claws rise from the swamp.",
                "Dark shapes stir in the muck."
            } },
            { "Ruins", new[] {
                "Spectral foes guard the ruins.",
                "Ancient sentinels awaken.",
                "Crumbled stone hides lurking fiends.",
                "Echoes summon shadowed beasts."
            } }
        };

        void Awake()
        {
            if (monsterPool == null || monsterPool.Count == 0)
            {
                monsterPool = CharacterLibrary.GetMonsterIds();
                if (monsterPool.Count == 0)
                {
                    Debug.LogWarning("CombatNodeGenerator: CharacterLibrary.GetMonsterIds returned empty, using fallback monster IDs.");
                    monsterPool = new List<string> { "Bog Fiend", "Wraith", "Mire Shambler", "Umbral Corvax" };
                }
            }
        }

        private bool ValidateReferences(EncounterData encounterData)
        {
            if (encounterData == null || encounterData.Positions == null || monsterPool == null || monsterPool.Count == 0)
            {
                Debug.LogError($"CombatNodeGenerator: Missing references! EncounterData: {encounterData != null}, EncounterData.Positions: {encounterData?.Positions != null}, MonsterPool: {monsterPool != null && monsterPool.Count > 0}");
                return false;
            }
            var validMonsterIds = CharacterLibrary.GetMonsterIds();
            if (!monsterPool.Any(id => validMonsterIds.Contains(id)))
            {
                Debug.LogWarning("CombatNodeGenerator: monsterPool contains no valid monster IDs. Using fallback.");
                monsterPool = validMonsterIds.Count > 0 ? validMonsterIds : new List<string> { "BogFiend", "Wraith", "MireShambler", "UmbralCorvax" };
            }
            return true;
        }

        public NodeData GenerateCombatNode(string biome, int level, EncounterData encounterData, int rating)
        {
            if (!ValidateReferences(encounterData)) return new NodeData(new List<CharacterStats>(), "Combat", biome, true, "", new List<VirusData>(), rating);

            List<string> selectedIds = SelectMonsterComposition(rating);
            encounterData.InitializeEncounter(selectedIds);
            List<CharacterStats> monsters = encounterData.SpawnMonsters();

            monsters = monsters
                .GroupBy(m => m.PartyPosition)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.GroupBy(m => m.Id).SelectMany(sg => sg.OrderBy(_ => Random.value)))
                .ToList();

            string[] texts = flavourTextPool.ContainsKey(biome) ? flavourTextPool[biome] : new[] { "A dangerous encounter." };
            string flavourText = texts[Random.Range(0, texts.Length)];

            return new NodeData(
                monsters: monsters,
                nodeType: "Combat",
                biome: biome,
                isCombat: true,
                flavourText: flavourText,
                seededViruses: new List<VirusData>(),
                challengeRating: rating
            );
        }

        private List<string> SelectMonsterComposition(int targetRating)
        {
            List<string> validIds = new List<string>();
            List<(string id, int rank)> monsterRanks = monsterPool
                .Select(id => (id, CharacterLibrary.GetMonsterData(id).Rank))
                .Where(t => t.Rank > 0)
                .ToList();

            void FindCompositions(List<string> current, int sum, int index, List<List<string>> results)
            {
                if (sum == targetRating && current.Count <= 4)
                {
                    results.Add(new List<string>(current));
                    return;
                }
                if (sum > targetRating || current.Count > 4 || index >= monsterRanks.Count) return;

                // Include current monster
                current.Add(monsterRanks[index].id);
                FindCompositions(current, sum + monsterRanks[index].rank, index, results);
                current.RemoveAt(current.Count - 1);

                // Skip current monster
                FindCompositions(current, sum, index + 1, results);
            }

            List<List<string>> compositions = new List<List<string>>();
            FindCompositions(new List<string>(), 0, 0, compositions);

            if (compositions.Count == 0)
            {
                Debug.LogWarning($"CombatNodeGenerator: No valid monster composition found for rating {targetRating}. Using fallback.");
                return new List<string> { "Wraith" }; // Fallback to rank 1
            }

            return compositions[Random.Range(0, compositions.Count)];
        }
    }
}