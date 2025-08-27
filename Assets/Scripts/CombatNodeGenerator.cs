using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class CombatNodeGenerator : MonoBehaviour
    {
        [SerializeField] private List<string> monsterPool = new List<string> { "Ghoul", "Wraith", "Skeleton", "Vampire" }; // Populated with CharacterLibrary IDs

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

            return new NodeData(
                monsters: encounterData.SpawnMonsters(),
                nodeType: "Combat",
                biome: biome,
                isCombat: true,
                flavourText: $"A {biome.ToLower()}-infested encounter.",
                seededViruses: new List<VirusData>()
            );
        }
    }
}