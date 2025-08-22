using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class CombatNodeGenerator : MonoBehaviour
    {
        [SerializeField] private CharacterPositions positions;
        [SerializeField] private List<MonsterSO> monsterPool; // Loaded dynamically

        private void Awake()
        {
            monsterPool = Resources.LoadAll<MonsterSO>("SO's/Monsters").ToList();
        }

        private bool ValidateReferences()
        {
            if (positions == null || monsterPool == null || monsterPool.Count == 0)
            {
                Debug.LogError($"CombatNodeGenerator: Missing references! Positions: {positions != null}, MonsterPool: {monsterPool != null && monsterPool.Count > 0}");
                return false;
            }
            return true;
        }

        public NodeData GenerateCombatNode(string biome, int level)
        {
            if (!ValidateReferences()) return new NodeData(new List<MonsterStats>(), "Combat", biome, false, "", new List<VirusData>());

            var encounter = ScriptableObject.CreateInstance<EncounterData>();
            encounter.Positions = positions; // Use property
            int count = Random.Range(1, Mathf.Min(5, level + 2)); // Scale with level
            List<MonsterSO> selectedSOs = new List<MonsterSO>();
            for (int i = 0; i < count; i++)
            {
                // Future: Filter by biome
                MonsterSO monsterSO = monsterPool[Random.Range(0, monsterPool.Count)];
                selectedSOs.Add(monsterSO);
            }
            encounter.InitializeEncounter(selectedSOs);

            return new NodeData(
                monsters: encounter.SpawnMonsters(),
                nodeType: "Combat",
                biome: biome,
                isCombat: true,
                flavourText: $"A {biome.ToLower()}-infested encounter.",
                seededViruses: new List<VirusData>()
            );
        }
    }
}