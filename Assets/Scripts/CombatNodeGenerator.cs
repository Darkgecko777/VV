using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class CombatNodeGenerator : MonoBehaviour
    {
        [SerializeField] private List<string> monsterPool; // Populated in Awake to avoid Inspector override
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
                    monsterPool = new List<string> { "Ghoul", "Wraith", "Skeleton", "Vampire" };
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
            return true;
        }

        public NodeData GenerateCombatNode(string biome, int level, EncounterData encounterData)
        {
            if (!ValidateReferences(encounterData)) return new NodeData(new List<MonsterStats>(), "Combat", biome, false, "", new List<VirusData>());

            int count = Random.Range(1, Mathf.Min(5, level + 2)); // Scale with level
            List<string> selectedIds = new List<string>();
            for (int i = 0; i < count; i++)
            {
                // Future: Filter by biome
                string monsterId = monsterPool[Random.Range(0, monsterPool.Count)];
                selectedIds.Add(monsterId);
            }
            encounterData.InitializeEncounter(selectedIds);

            string[] texts = flavourTextPool.ContainsKey(biome) ? flavourTextPool[biome] : new[] { $"A {biome.ToLower()}-infested encounter." };
            string flavourText = texts[Random.Range(0, texts.Length)];

            return new NodeData(
                monsters: encounterData.SpawnMonsters(),
                nodeType: "Combat",
                biome: biome,
                isCombat: true,
                flavourText: flavourText,
                seededViruses: new List<VirusData>()
            );
        }
    }
}