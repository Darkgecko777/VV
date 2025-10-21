using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class CombatNodeGenerator : MonoBehaviour
    {
        [SerializeField] private List<string> monsterPool;
        [SerializeField] private VirusConfigSO virusConfig; // Assign in Inspector
        private readonly Dictionary<string, string[]> flavourTextPool = new Dictionary<string, string[]>
        {
            { "Swamp", new[] { "A horde emerges from the bog.", "Mire-dwellers lurk in the mist.", "Fetid claws rise from the swamp.", "Dark shapes stir in the muck." } },
            { "Ruins", new[] { "Spectral foes guard the ruins.", "Ancient sentinels awaken.", "Crumbled stone hides lurking fiends.", "Echoes summon shadowed beasts." } }
        };

        void Awake()
        {
            if (monsterPool == null || monsterPool.Count == 0)
            {
                monsterPool = CharacterLibrary.GetMonsterIds();
                if (monsterPool.Count == 0)
                {
                    Debug.LogWarning("CombatNodeGenerator: CharacterLibrary.GetMonsterIds returned empty, using fallback monster IDs.");
                    monsterPool = new List<string> { "BogFiend", "Wraith", "MireShambler", "UmbralCorvax" };
                }
            }
            if (virusConfig == null)
            {
                Debug.LogError("CombatNodeGenerator: virusConfig not assigned in Inspector. Please assign VirusConfigSO.");
            }
        }

        private bool ValidateReferences(EncounterData encounterData)
        {
            if (encounterData == null || encounterData.Positions == null || monsterPool == null || monsterPool.Count == 0 || virusConfig == null)
            {
                Debug.LogError($"CombatNodeGenerator: Missing references! EncounterData: {encounterData != null}, EncounterData.Positions: {encounterData?.Positions != null}, MonsterPool: {monsterPool != null && monsterPool.Count > 0}, VirusConfig: {virusConfig != null}");
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
            if (!ValidateReferences(encounterData))
                return new NodeData(new List<CharacterStats>(), "Combat", biome, true, "", new List<VirusSO>(), rating);

            List<string> selectedIds = SelectMonsterComposition(rating);
            encounterData.InitializeEncounter(selectedIds);
            List<CharacterStats> monsters = encounterData.SpawnMonsters();

            // Guarantee BogRot on one random monster
            VirusSO bogRotVirus = virusConfig?.GetVirus("BogRot");
            if (monsters.Count > 0 && bogRotVirus != null)
            {
                int randomIndex = Random.Range(0, monsters.Count);
                monsters[randomIndex].Infections.Add(bogRotVirus);
            }
            else
            {
                Debug.LogError($"CombatNodeGenerator: Cannot seed virus, BogRot not found or no monsters. Virus: {bogRotVirus != null}, Monsters: {monsters.Count}");
            }

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
                seededViruses: bogRotVirus != null ? new List<VirusSO> { bogRotVirus } : new List<VirusSO>(),
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

                current.Add(monsterRanks[index].id);
                FindCompositions(current, sum + monsterRanks[index].rank, index, results);
                current.RemoveAt(current.Count - 1);
                FindCompositions(current, sum, index + 1, results);
            }

            List<List<string>> compositions = new List<List<string>>();
            FindCompositions(new List<string>(), 0, 0, compositions);

            if (compositions.Count == 0)
            {
                Debug.LogWarning($"CombatNodeGenerator: No valid monster composition found for rating {targetRating}. Using fallback.");
                return new List<string> { "Wraith" };
            }

            return compositions[Random.Range(0, compositions.Count)];
        }
    }
}