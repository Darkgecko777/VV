using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "EncounterData", menuName = "VirulentVentures/EncounterData", order = 10)]
    public class EncounterData : ScriptableObject
    {
        [SerializeField] private List<string> monsterIds = new List<string>();
        [SerializeField] private bool isCombatNode = true;
        [SerializeField] private CharacterPositions positions;

        public bool IsCombatNode => isCombatNode;
        public CharacterPositions Positions { get => positions; set => positions = value; }

        public List<CharacterStats> SpawnMonsters()
        {
            List<CharacterStats> monsters = new List<CharacterStats>();

            if (monsterIds == null || monsterIds.Count == 0 || monsterIds.Count > 4)
            {
                Debug.LogWarning($"EncounterData: Invalid monster setup! MonsterIds count: {monsterIds?.Count ?? 0}, must be 1-4");
                return monsters;
            }

            if (positions == null)
            {
                Debug.LogWarning($"EncounterData: CharacterPositions not assigned for {name}. Using default positions.");
                positions = ScriptableObject.CreateInstance<CharacterPositions>();
            }

            if (positions.monsterPositions == null || positions.monsterPositions.Length < monsterIds.Count)
            {
                Debug.LogWarning($"EncounterData: Invalid monster positions for {name}. Expected {monsterIds.Count} positions, got {positions?.monsterPositions?.Length ?? 0}. Using defaults.");
                positions.monsterPositions = new Vector3[]
                {
                    new Vector3(1.5f, 0f, 0f),
                    new Vector3(3.5f, 0f, 0f),
                    new Vector3(5.5f, 0f, 0f),
                    new Vector3(7.5f, 0f, 0f)
                };
            }

            for (int i = 0; i < monsterIds.Count; i++)
            {
                if (string.IsNullOrEmpty(monsterIds[i]))
                {
                    Debug.LogWarning($"EncounterData: Empty MonsterId at index {i}");
                    continue;
                }

                var monsterData = CharacterLibrary.GetMonsterData(monsterIds[i]);
                if (monsterData == null)
                {
                    Debug.LogWarning($"EncounterData: CharacterSO for MonsterId {monsterIds[i]} not found, skipping.");
                    continue;
                }

                GameObject monsterObj = new GameObject($"Monster{i + 1}_{monsterData.Id}");
                monsterObj.transform.position = positions.monsterPositions[i];
                var renderer = monsterObj.AddComponent<SpriteRenderer>();
                renderer.sortingLayerName = "Characters";
                renderer.transform.localScale = new Vector3(2f, 2f, 1f);
                var monsterStats = new CharacterStats(monsterData, positions.monsterPositions[i]);
                monsters.Add(monsterStats);
            }

            return monsters;
        }

        public void InitializeEncounter(List<string> monsterIds)
        {
            if (monsterIds == null || monsterIds.Count < 1 || monsterIds.Count > 4)
            {
                Debug.LogError($"EncounterData: Invalid monsterIds count for initialization: {monsterIds?.Count ?? 0}, must be 1-4");
                return;
            }
            this.monsterIds = monsterIds;
        }
    }
}